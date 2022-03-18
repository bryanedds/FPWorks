﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.Numerics
open FSharpx.Collections
open Prime
open Nu
open Nu.Declarative
open OmniBlade

[<AutoOpen>]
module BattleDispatcher =

    type [<StructuralEquality; NoComparison>] Positioning =
        | Position of Vector2
        | Center of Vector2
        | Bottom of Vector2

    type [<ReferenceEquality; NoComparison>] Hop =
        { HopStart : Vector2
          HopStop : Vector2 }

    type BattleMessage =
        | Update
        | InteractDialog
        | RegularItemSelect of CharacterIndex * string
        | RegularItemCancel of CharacterIndex
        | ConsumableItemSelect of CharacterIndex * string
        | ConsumableItemCancel of CharacterIndex
        | TechItemSelect of CharacterIndex * string
        | TechItemCancel of CharacterIndex
        | ReticlesSelect of CharacterIndex * CharacterIndex
        | ReticlesCancel of CharacterIndex
        | ReadyCharacters of int64
        | PoiseCharacters
        | CelebrateCharacters of bool
        | AttackCharacter1 of CharacterIndex
        | AttackCharacter2 of CharacterIndex * CharacterIndex
        | ConsumeCharacter1 of ConsumableType * CharacterIndex
        | ConsumeCharacter2 of ConsumableType * CharacterIndex
        | TechCharacter1 of CharacterIndex * CharacterIndex * TechType
        | TechCharacter2 of CharacterIndex * CharacterIndex * TechType
        | TechCharacter3 of CharacterIndex * CharacterIndex * TechType
        | TechCharacter4 of CharacterIndex * CharacterIndex * TechType
        | TechCharacter5 of CharacterIndex * CharacterIndex * TechType
        | TechCharacter6 of CharacterIndex * CharacterIndex * TechType
        | TechCharacterAmbient of CharacterIndex * CharacterIndex * TechType
        | AutoBattleEnemies
        | ChargeCharacter of CharacterIndex
        | PoiseCharacter of CharacterIndex
        | WoundCharacter of CharacterIndex
        | ResetCharacter of CharacterIndex
        | DestroyCharacter of CharacterIndex
        | Nop

    type [<NoEquality; NoComparison>] BattleCommand =
        | UpdateEye
        | DisplayCancel of CharacterIndex
        | DisplayHitPointsChange of CharacterIndex * int
        | DisplayBolt of int64 * CharacterIndex
        | DisplayCycloneBlur of int64 * CharacterIndex * single
        | DisplayImpactSplash of int64 * CharacterIndex
        | DisplayCut of int64 * bool * CharacterIndex
        | DisplaySlashSpike of int64 * Vector2 * CharacterIndex
        | DisplayArcaneCast of int64 * CharacterIndex
        | DisplayFire of int64 * CharacterIndex * CharacterIndex
        | DisplayFlame of int64 * CharacterIndex * CharacterIndex
        | DisplayIce of int64 * CharacterIndex
        | DisplaySnowball of int64 * CharacterIndex
        | DisplayHolyCast of int64 * CharacterIndex
        | DisplayPurify of int64 * CharacterIndex
        | DisplayCure of int64 * CharacterIndex
        | DisplayProtect of int64 * CharacterIndex
        | DisplayDimensionalCast of int64 * CharacterIndex
        | DisplayBuff of int64 * StatusType * CharacterIndex
        | DisplayDebuff of int64 * StatusType * CharacterIndex
        | DisplayConjureIfrit of int64
        | DisplayHop of Hop
        | DisplayCircle of Vector2 * single
        | PlaySound of int64 * single * AssetTag<Sound>
        | PlaySong of int * int * single * double * Song AssetTag
        | FadeOutSong of int

    type Screen with
        member this.GetBattle world = this.GetModelGeneric<Battle> world
        member this.SetBattle value world = this.SetModelGeneric<Battle> value world
        member this.Battle = this.ModelGeneric<Battle> ()

    [<RequireQualifiedAccess>]
    module Battle =

        let private updateAttack sourceIndex (targetIndexOpt : CharacterIndex option) time localTime battle =
            match Battle.tryGetCharacter sourceIndex battle with
            | Some source when source.IsHealthy ->
                match targetIndexOpt with
                | Some targetIndex ->
                    match Battle.tryGetCharacter targetIndex battle with
                    | Some target ->
                        match localTime with
                        | 0L ->
                            if target.IsHealthy then
                                withMsg (AttackCharacter1 sourceIndex) battle
                            else
                                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                                withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                        | 15L ->
                            withMsg (AttackCharacter2 (sourceIndex, targetIndex)) battle
                        | _ when localTime > 15L && Character.getAnimationFinished time target ->
                            let target = Battle.getCharacter targetIndex battle
                            if target.IsHealthy then
                                let battle =
                                    if  (match source.CharacterType with Enemy MadMinotaur -> false | _ -> true) && // HACK: disallow countering mad minotaurs since it nerfs challenge of first battle.
                                        Battle.shouldCounter sourceIndex targetIndex battle
                                    then Battle.counterAttack sourceIndex targetIndex battle
                                    else battle
                                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                                withMsgs [PoiseCharacter sourceIndex; PoiseCharacter targetIndex] battle
                            else
                                let woundCommand = CurrentCommand.make time (ActionCommand.make Wound sourceIndex (Some targetIndex))
                                let battle = Battle.updateCurrentCommandOpt (constant (Some woundCommand)) battle
                                withMsg (PoiseCharacter sourceIndex) battle
                        | _ -> just battle
                    | None ->
                        let battle = Battle.updateCurrentCommandOpt (constant None) battle
                        withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                | None ->
                    let battle = Battle.updateCurrentCommandOpt (constant None) battle
                    withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
            | Some _ | None ->
                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                just battle

        let private updateDefend sourceIndex time localTime battle =
            match Battle.tryGetCharacter sourceIndex battle with
            | Some source when source.IsHealthy ->
                match localTime with
                | 0L ->
                    let battle =
                        battle |>
                        Battle.updateCharacterActionTime (constant 0.0f) sourceIndex |>
                        Battle.updateCharacterInputState (constant NoInput) sourceIndex |>
                        Battle.animateCharacter time (PoiseAnimation Defending) sourceIndex |>
                        Battle.defendCharacter sourceIndex
                    let battle = Battle.updateCurrentCommandOpt (constant None) battle
                    just battle
                | _ -> just battle
            | Some _ | None ->
                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                just battle

        let private updateConsume consumable sourceIndex (targetIndexOpt : CharacterIndex option) time localTime battle =
            match Battle.tryGetCharacter sourceIndex battle with
            | Some source when source.IsHealthy ->
                match targetIndexOpt with
                | Some targetIndex ->
                    match Battle.tryGetCharacter targetIndex battle with
                    | Some target ->
                        match localTime with
                        | 0L ->
                            if target.IsHealthy || consumable = Revive then // HACK: should really be checked ConsumableData.
                                withMsg (ConsumeCharacter1 (consumable, sourceIndex)) battle
                            else
                                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                                withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                        | 30L ->
                            withMsg (ConsumeCharacter2 (consumable, targetIndex)) battle
                        | _ when localTime > 30L && Character.getAnimationFinished time target ->
                            let battle = Battle.updateCurrentCommandOpt (constant None) battle
                            withMsgs [PoiseCharacter sourceIndex; PoiseCharacter targetIndex] battle
                        | _ -> just battle
                    | None ->
                        let battle = Battle.updateCurrentCommandOpt (constant None) battle
                        withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                | None ->
                    let battle = Battle.updateCurrentCommandOpt (constant None) battle
                    withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
            | Some _ | None ->
                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                just battle

        let private updateTech techType sourceIndex (targetIndexOpt : CharacterIndex option) (_ : int64) localTime battle =
            match targetIndexOpt with
            | Some targetIndex ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target ->
                    match (Map.tryFind techType Data.Value.Techs,  Map.tryFind techType Data.Value.TechAnimations) with
                    | (Some techData, Some techAnimationData) ->
                        ignore techData // TODO: check for target.IsWounded case if techData is affecting wounded...
                        if target.IsHealthy then
                            let (msgs, battle) =
                                if localTime = techAnimationData.TechStart then ([TechCharacter1 (sourceIndex, targetIndex, techType)], battle)
                                elif localTime = techAnimationData.TechingStart then ([TechCharacter2 (sourceIndex, targetIndex, techType)], battle)
                                elif localTime = techAnimationData.AffectingStart then ([TechCharacter3 (sourceIndex, targetIndex, techType)], battle)
                                elif localTime = techAnimationData.AffectingStop then ([TechCharacter4 (sourceIndex, targetIndex, techType)], battle)
                                elif localTime = techAnimationData.TechingStop then ([TechCharacter5 (sourceIndex, targetIndex, techType)], battle)
                                elif localTime = techAnimationData.TechStop then ([TechCharacter6 (sourceIndex, targetIndex, techType)], battle)
                                else ([], battle)
                            let (msgs, battle) = (msgs @ [TechCharacterAmbient (sourceIndex, targetIndex, techType)], battle)
                            withMsgs msgs battle
                        else
                            let battle = Battle.updateCurrentCommandOpt (constant None) battle
                            withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                    | (_, _) ->
                        let battle = Battle.updateCurrentCommandOpt (constant None) battle
                        withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                | None ->
                    let battle = Battle.updateCurrentCommandOpt (constant None) battle
                    withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
            | None ->
                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle

        let rec private updateWound targetIndexOpt time battle =
            match targetIndexOpt with
            | Some targetIndex ->
                let character = Battle.getCharacter targetIndex battle
                let (sigs, battle) =
                    if character.IsAlly then
                        match character.CharacterAnimationType with
                        | DamageAnimation ->
                            if Character.getAnimationFinished time character then
                                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                                ([Message (WoundCharacter targetIndex)], battle)
                            else ([], battle)
                        | PoiseAnimation _ -> // allies don't have a wound animation state but rather return to poise state
                            let battle = Battle.updateCurrentCommandOpt (constant None) battle
                            ([Message (WoundCharacter targetIndex)], battle)
                        | _ -> failwithumf ()
                    else
                        match character.CharacterAnimationType with
                        | DamageAnimation ->
                            if Character.getAnimationFinished time character then
                                let woundCharacter = WoundCharacter targetIndex
                                let playDeathSound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BeastDeathSound)
                                ([Message woundCharacter; Command playDeathSound], battle)
                            else ([], battle)
                        | WoundAnimation ->
                            if Character.getAnimationFinished time character then
                                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                                ([Message (DestroyCharacter targetIndex)], battle)
                            else ([], battle)
                        | _ -> failwithumf ()
                let (sigs, battle) =
                    match battle.CurrentCommandOpt with
                    | None ->
                        let allies = battle |> Battle.getAllies |> Map.toValueList
                        let enemies = battle |> Battle.getEnemies |> Map.toValueList
                        if List.forall (fun (character : Character) -> character.IsWounded) allies then
                            // lost battle
                            let battle = Battle.updateBattleState (constant (BattleQuitting (time, false, Set.empty))) battle
                            let (sigs2, battle) = update time battle
                            (msg (CelebrateCharacters false) :: sigs @ sigs2, battle)
                        elif
                            List.forall (fun (character : Character) -> character.IsWounded) enemies &&
                            List.hasAtMost 1 enemies then
                            // won battle
                            let battle = Battle.updateBattleState (constant (BattleResults (time, true))) battle
                            let (sigs2, battle) = update time battle
                            (msg (CelebrateCharacters true) :: sigs @ sigs2, battle)
                        else (sigs, battle)
                    | Some _ -> (sigs, battle)
                withSigs sigs battle
            | None -> just battle

        and private updateReady time startTime (battle : Battle) =
            let localTime = time - startTime
            if localTime = inc 62L then // first frame after transitioning in
                match battle.BattleSongOpt with
                | Some battleSong -> withCmd (PlaySong (0, Constants.Audio.FadeOutMsDefault, Constants.Audio.SongVolumeDefault, 0.0, battleSong)) battle
                | None -> just battle
            elif localTime >= 90L && localTime < 160L then
                let localTimeReady = localTime - 90L
                withMsg (ReadyCharacters localTimeReady) battle
            elif localTime = 160L then
                let battle = Battle.updateBattleState (constant BattleRunning) battle
                withMsgs [PoiseCharacters; AutoBattleEnemies] battle
            else just battle

        and private updateCurrentCommand time currentCommand battle =
            let localTime = time - currentCommand.StartTime
            match currentCommand.ActionCommand.Action with
            | Attack ->
                let source = currentCommand.ActionCommand.Source
                let targetOpt = currentCommand.ActionCommand.TargetOpt
                updateAttack source targetOpt time localTime battle
            | Defend ->
                let source = currentCommand.ActionCommand.Source
                updateDefend source time localTime battle
            | Tech techType ->
                let source = currentCommand.ActionCommand.Source
                let targetOpt = currentCommand.ActionCommand.TargetOpt
                updateTech techType source targetOpt time localTime battle
            | Consume consumable ->
                let source = currentCommand.ActionCommand.Source
                let targetOpt = currentCommand.ActionCommand.TargetOpt
                updateConsume consumable source targetOpt time localTime battle
            | Wound ->
                let targetOpt = currentCommand.ActionCommand.TargetOpt
                updateWound targetOpt time battle

        and private updateNextCommand time nextCommand futureCommands battle =
            let command = CurrentCommand.make time nextCommand
            let sourceIndex = command.ActionCommand.Source
            let targetIndexOpt = command.ActionCommand.TargetOpt
            let source = Battle.getCharacter sourceIndex battle
            let battle =
                match command.ActionCommand.Action with
                | Attack | Defend ->
                    if source.IsHealthy && not (Map.containsKey Sleep source.Statuses) then
                        let targetIndexOpt = Battle.tryRetargetIfNeeded false targetIndexOpt battle
                        let command = { command with ActionCommand = { command.ActionCommand with TargetOpt = targetIndexOpt }}
                        Battle.updateCurrentCommandOpt (constant (Some command)) battle
                    else battle
                | Consume consumableType ->
                    match Data.Value.Consumables.TryGetValue consumableType with
                    | (true, consumable) ->
                        if source.IsHealthy && not (Map.containsKey Sleep source.Statuses) then
                            let targetIndexOpt = Battle.tryRetargetIfNeeded consumable.Revive targetIndexOpt battle
                            let command = { command with ActionCommand = { command.ActionCommand with TargetOpt = targetIndexOpt }}
                            Battle.updateCurrentCommandOpt (constant (Some command)) battle
                        else battle
                    | (false, _) -> battle
                | Tech techType ->
                    match Data.Value.Techs.TryGetValue techType with
                    | (true, _) ->
                        if source.IsHealthy && not (Map.containsKey Sleep source.Statuses) && not (Map.containsKey Silence source.Statuses) then
                            let targetIndexOpt = Battle.tryRetargetIfNeeded false targetIndexOpt battle // TODO: consider affecting wounded.
                            let command = { command with ActionCommand = { command.ActionCommand with TargetOpt = targetIndexOpt }}
                            Battle.updateCurrentCommandOpt (constant (Some command)) battle
                        else battle
                    | (false, _) -> battle
                | Wound ->
                    Battle.updateCurrentCommandOpt (constant (Some command)) battle
            let battle = Battle.updateActionCommands (constant futureCommands) battle
            update time battle

        and private updateNoNextCommand (_ : int64) battle =
            let (allySignalsRev, battle) =
                Map.fold (fun (signals, battle) allyIndex (ally : Character) ->
                    if  ally.ActionTime >= Constants.Battle.ActionTime &&
                        ally.InputState = NoInput then
                        let battle = Battle.updateCharacterInputState (constant RegularMenu) allyIndex battle
                        let playReadySound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.ReadySound)
                        (Command playReadySound :: signals, battle)
                    else (signals, battle))
                    ([], battle)
                    (Battle.getAllies battle)
            let (enemySignalsRev, battle) =
                Map.fold (fun (signals, battle) enemyIndex (enemy : Character) ->
                    if  enemy.ActionTime >= Constants.Battle.ActionTime &&
                        not (Battle.characterAppendedActionCommand enemyIndex battle) then
                        match enemy.AutoBattleOpt with
                        | Some autoBattle ->
                            let actionCommand =
                                match autoBattle.AutoTechOpt with
                                | Some tech -> { Action = Tech tech; Source = enemyIndex; TargetOpt = Some autoBattle.AutoTarget }
                                | None -> { Action = Attack; Source = enemyIndex; TargetOpt = Some autoBattle.AutoTarget }
                            let battle = Battle.appendActionCommand actionCommand battle
                            (Message (ResetCharacter enemyIndex) :: signals, battle)
                        | None -> (Message (ResetCharacter enemyIndex) :: signals, battle)
                    else (signals, battle))
                    ([], battle)
                    (Battle.getEnemies battle)
            let battle =
                Battle.updateCharacters (fun character ->
                    let actionTimeDelta =
                        if character.IsAlly || battle.BattleSpeed = WaitSpeed
                        then Constants.Battle.AllyActionTimeDelta
                        else Constants.Battle.EnemyActionTimeDelta
                    let actionTimeDelta =
                        if Map.containsKey (Time false) character.Statuses then actionTimeDelta * Constants.Battle.ActionTimeSlowScalar
                        elif Map.containsKey (Time true) character.Statuses then actionTimeDelta * Constants.Battle.ActionTimeHasteScalar
                        else actionTimeDelta
                    let actionTimeDelta =
                        match battle.BattleSpeed with
                        | SwiftSpeed -> actionTimeDelta
                        | PacedSpeed -> actionTimeDelta * Constants.Battle.PacedSpeedScalar
                        | WaitSpeed ->
                            let anyAlliesInputting = Battle.getAlliesHealthy battle |> Map.toValueList |> List.exists (fun ally -> ally.InputState <> CharacterInputState.NoInput)
                            if anyAlliesInputting then 0.0f else actionTimeDelta
                    let poisoned =
                        let actionTime = character.ActionTime + actionTimeDelta
                        Map.containsKey Poison character.Statuses &&
                        character.ActionTime % 500.0f < 250.0f &&
                        actionTime % 500.0f >= 250.0f
                    let character =
                        if character.IsHealthy && not (Map.containsKey Sleep character.Statuses)
                        then Character.updateActionTime ((+) actionTimeDelta) character
                        else character
                    let character =
                        if character.IsHealthy
                        then Character.burndownStatuses actionTimeDelta character
                        else character
                    let character =
                        if character.IsHealthy && poisoned then
                            let damage = single character.HitPointsMax * Constants.Battle.PoisonDrainRate |> max 1.0f |> int
                            let alliesHealthy = Battle.getAlliesHealthy battle
                            Character.updateHitPoints (fun hp -> (false, max 1 (hp - damage))) false alliesHealthy character
                        else character
                    let character =
                        if character.IsHealthy && Character.isReadyForAutoBattle character then
                            let alliesHealthy = Battle.getAlliesHealthy battle
                            let alliesWounded = Battle.getAlliesWounded battle
                            let enemiesHealthy = Battle.getEnemiesHealthy battle
                            let enemiesWounded = Battle.getEnemiesWounded battle
                            Character.autoBattle alliesHealthy alliesWounded enemiesHealthy enemiesWounded character
                        else character
                    character)
                    battle
            withSigs (List.rev (allySignalsRev @ enemySignalsRev)) battle

        and private updateNoCurrentCommand time (battle : Battle) =
            match battle.ActionCommands with
            | Queue.Cons (nextCommand, futureCommands) -> updateNextCommand time nextCommand futureCommands battle
            | Queue.Nil -> updateNoNextCommand time battle

        and private updateRunning time (battle : Battle) =
            match battle.CurrentCommandOpt with
            | Some currentCommand -> updateCurrentCommand time currentCommand battle
            | None -> updateNoCurrentCommand time battle

        and private updateResults time startTime outcome (battle : Battle) =
            let localTime = time - startTime
            if localTime = 0L then
                let alliesLevelingUp =
                    battle |> Battle.getAllies |> Map.toValueList |>
                    List.filter (fun ally -> ally.HitPoints > 0) |>
                    List.filter (fun ally -> Algorithms.expPointsRemainingForNextLevel ally.ExpPoints <= battle.PrizePool.Exp)
                let textA =
                    match alliesLevelingUp with
                    | _ :: _ -> "" + (alliesLevelingUp |> List.map (fun c -> c.Name) |> String.join ", ") + " monte en niveau!^"
                    | [] -> "Victoire!^"
                let textB =
                    alliesLevelingUp |>
                    List.choose (fun ally ->
                        let techs = Algorithms.expPointsToTechs3 ally.ExpPoints battle.PrizePool.Exp ally.ArchetypeType
                        if Set.notEmpty techs then Some (ally, techs) else None) |>
                    List.map (fun (ally, techs) ->
                        let text = techs |> Set.toList |> List.map scstring |> String.join ", "
                        ally.Name + " apprend " + text + "!") |>
                    function
                    | _ :: _ as texts -> String.join "\n" texts + "^"
                    | [] -> ""
                let textC = "Tu gagnes " + string battle.PrizePool.Exp + " pts d'Experience! \nTu gagnes " + string battle.PrizePool.Gold + " Ors!"
                let textD =
                    match battle.PrizePool.Items with
                    | _ :: _ as items -> "^Tu trouves " + (items |> List.map (fun i -> ItemType.frenchWithQuantity i) |> String.join ", ") + "!"
                    | [] -> ""
                let text = textA + textB + textC + textD
                let dialog = { DialogForm = DialogThick; DialogTokenized = text; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None }
                let battle = Battle.updateDialogOpt (constant (Some dialog)) battle
                let (sigs, battle) =
                    if outcome then
                        let battle = Battle.updateAllies (fun ally -> if ally.IsHealthy then Character.updateExpPoints ((+) battle.PrizePool.Exp) ally else ally) battle
                        let battle = Battle.updateInventory (fun inv -> { inv with Gold = inv.Gold + battle.PrizePool.Gold }) battle
                        let battle = Battle.updateInventory (Inventory.tryAddItems battle.PrizePool.Items >> snd) battle
                        if List.notEmpty alliesLevelingUp
                        then ([cmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.GrowthSound))], battle)
                        else ([], battle)
                    else ([], battle)
                (cmd (FadeOutSong 6000) :: sigs, battle)
            else
                match battle.DialogOpt with
                | None -> just (Battle.updateBattleState (constant (BattleQuitting (time, outcome, battle.PrizePool.Consequents))) battle)
                | Some _ -> just battle

        and private updateCease time startTime battle =
            let localTime = time - startTime
            if localTime = 0L
            then withCmd (FadeOutSong Constants.Audio.FadeOutMsDefault) battle
            else just battle

        and update time (battle : Battle) =
            match battle.BattleState with
            | BattleReady startTime -> updateReady time startTime battle
            | BattleRunning -> updateRunning time battle
            | BattleResults (startTime, outcome) -> updateResults time startTime outcome battle
            | BattleQuitting (startTime, _, _) -> updateCease time startTime battle

    type BattleDispatcher () =
        inherit ScreenDispatcher<Battle, BattleMessage, BattleCommand> (Battle.empty)

        static let displayEffect delay size positioning effect world =
            World.delay (fun world ->
                let (entity, world) = World.createEntity<EffectDispatcher> None DefaultOverlay Simulants.Battle.Scene.Group world
                let world = entity.SetEffect effect world
                let world = entity.SetSize size world
                let world =
                    match positioning with
                    | Position position -> entity.SetPosition position world
                    | Center center -> entity.SetCenter center world
                    | Bottom bottom -> entity.SetBottom bottom world
                let world = entity.SetElevation Constants.Battle.EffectElevation world
                entity.SetSelfDestruct true world)
                delay
                world

        override this.Channel (_, battle) =
            [battle.UpdateEvent => msg Update
             battle.PostUpdateEvent => cmd UpdateEye]

        override this.Message (battle, message, _, world) =

            match message with
            | Update ->

                // update
                let (signals, battle) = 
                    if World.isAdvancing world
                    then Battle.update (World.getUpdateTime world) battle
                    else just battle

                // update dialog
                let battle =
                    match battle.DialogOpt with
                    | Some dialog ->
                        let dialog = Dialog.update id dialog world // TODO: P1: pass in a real detokenizer!
                        Battle.updateDialogOpt (constant (Some dialog)) battle
                    | None -> battle

                // fin
                (signals, battle)

            | InteractDialog ->
                match battle.DialogOpt with
                | Some dialog ->
                    match Dialog.tryAdvance id dialog with // TODO: P1: pass in a real detokenizer!
                    | (true, dialog) ->
                        let battle = Battle.updateDialogOpt (constant (Some dialog)) battle
                        just battle
                    | (false, _) ->
                        let battle = Battle.updateDialogOpt (constant None) battle
                        just battle
                | None -> just battle

            | RegularItemSelect (characterIndex, item) ->
                let battle =
                    match item with
                    | "Attack" ->
                        battle |>
                        Battle.updateCharacterInputState (constant (AimReticles (item, EnemyAim true))) characterIndex |>
                        Battle.undefendCharacter characterIndex
                    | "Defend" ->
                        let battle = Battle.updateCharacterInputState (constant NoInput) characterIndex battle
                        let command = ActionCommand.make Defend characterIndex None
                        let battle = Battle.appendActionCommand command battle
                        battle
                    | "Tech" ->
                        battle |>
                        Battle.updateCharacterInputState (constant TechMenu) characterIndex |>
                        Battle.undefendCharacter characterIndex
                    | "Consumable" ->
                        battle |>
                        Battle.updateCharacterInputState (constant ItemMenu) characterIndex |>
                        Battle.undefendCharacter characterIndex
                    | _ -> failwithumf ()
                just battle
            
            | RegularItemCancel characterIndex ->
                let battle = Battle.updateCharacterInputState (constant RegularMenu) characterIndex battle
                just battle
            
            | ConsumableItemSelect (characterIndex, item) ->
                let consumableType =
                    scvalue<ConsumableType> item
                let aimType =
                    match Data.Value.Consumables.TryGetValue consumableType with
                    | (true, consumableData) -> consumableData.AimType
                    | (false, _) -> NoAim
                let battle = Battle.updateCharacterInputState (constant (AimReticles (item, aimType))) characterIndex battle
                just battle

            | ConsumableItemCancel characterIndex ->
                let battle = Battle.updateCharacterInputState (constant RegularMenu) characterIndex battle
                just battle
            
            | TechItemSelect (characterIndex, item) ->
                let techType =
                    scvalue<TechType> item
                let aimType =
                    match Data.Value.Techs.TryGetValue techType with
                    | (true, techData) -> techData.AimType
                    | (false, _) -> NoAim
                let battle = Battle.updateCharacterInputState (constant (AimReticles (item, aimType))) characterIndex battle
                just battle
            
            | TechItemCancel characterIndex ->
                let battle = Battle.updateCharacterInputState (constant RegularMenu) characterIndex battle
                just battle

            | ReticlesSelect (sourceIndex, targetIndex) ->
                match battle.BattleState with
                | BattleRunning ->
                    let battle = Battle.confirmCharacterInput sourceIndex targetIndex battle
                    withMsg (ResetCharacter sourceIndex) battle
                | _ -> just battle

            | ReticlesCancel characterIndex ->
                let battle = Battle.cancelCharacterInput characterIndex battle
                just battle

            | ReadyCharacters localTime ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharactersReady time battle
                if localTime = 30L
                then withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.UnsheatheSound)) battle
                else just battle

            | PoiseCharacters ->
                let time = World.getUpdateTime world
                let battle = Battle.animatedCharactersPoised time battle
                just battle

            | CelebrateCharacters outcome ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharactersCelebrate time outcome battle
                just battle

            | AttackCharacter1 sourceIndex ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                let playHit = PlaySound (15L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                withCmd playHit battle

            | AttackCharacter2 (sourceIndex, targetIndex) ->
                let time = World.getUpdateTime world
                let damage = Battle.evalAttack Physical sourceIndex targetIndex battle
                let battle = Battle.updateCharacterHitPoints false false -damage targetIndex battle
                let battle = Battle.animateCharacter time DamageAnimation targetIndex battle
                let sigs = if Battle.isCharacterWounded targetIndex battle then [msg (ResetCharacter targetIndex)] else []
                withSigs (cmd (DisplayHitPointsChange (targetIndex, -damage)) :: sigs) battle

            | ConsumeCharacter1 (consumable, sourceIndex) ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharacter time CastAnimation sourceIndex battle
                let battle = Battle.updateInventory (Inventory.tryRemoveItem (Consumable consumable) >> snd) battle
                just battle

            | ConsumeCharacter2 (consumableType, targetIndex) ->
                let time = World.getUpdateTime world
                match Data.Value.Consumables.TryGetValue consumableType with
                | (true, consumableData) ->
                    if consumableData.Curative then
                        let healing = int consumableData.Scalar
                        let battle =
                            if consumableData.Techative
                            then Battle.updateCharacterTechPoints healing targetIndex battle
                            else Battle.updateCharacterHitPoints false consumableData.Revive healing targetIndex battle
                        let battle = Battle.applyCharacterStatuses consumableData.StatusesAdded consumableData.StatusesRemoved targetIndex battle
                        let battle = Battle.animateCharacter time SpinAnimation targetIndex battle
                        let displayHitPointsChange = DisplayHitPointsChange (targetIndex, healing)
                        let playHealSound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.HealSound)
                        withCmds [displayHitPointsChange; playHealSound] battle
                    else
                        // TODO: non-curative case
                        just battle
                | (false, _) -> just battle

            | TechCharacter1 (sourceIndex, targetIndex, techType) ->
                let sourceBounds = Battle.getCharacterBounds sourceIndex battle
                let targetBounds = Battle.getCharacterBounds targetIndex battle
                let effectOpt =
                    match techType with
                    | Critical | DarkCritical | PoisonCut | PowerCut | DispelCut | DoubleCut ->
                        let hopDirection = Direction.ofVector2 (v2 (targetBounds.Bottom.X - sourceBounds.Bottom.X) 0.0f)
                        let hopStop = targetBounds.Bottom - Direction.toVector2 hopDirection * Constants.Battle.StrikingDistance
                        Left (DisplayHop { HopStart = sourceBounds.Bottom; HopStop = hopStop })
                    | Cyclone ->
                        Left (DisplayHop { HopStart = sourceBounds.Bottom; HopStop = targetBounds.Bottom + Constants.Battle.CharacterBottomOffset3 })
                    | _ ->
                        match Battle.getCharacterArchetypeType sourceIndex battle with
                        | Cleric ->
                            let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeHolySound)
                            let displayCast = DisplayHolyCast (0L, sourceIndex)
                            Right [cmd playCharge; cmd displayCast]
                        | Wizard ->
                            let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeDimensionSound)
                            let displayCast = DisplayArcaneCast (0L, sourceIndex)
                            Right [cmd playCharge; cmd displayCast]
                        | _ ->
                            let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeDimensionSound)
                            let displayCast = DisplayDimensionalCast (0L, sourceIndex)
                            Right [cmd playCharge; cmd displayCast]
                match effectOpt with
                | Left hopEffect ->
                    withCmd hopEffect battle
                | Right chargeEffects ->
                    if Battle.isCharacterWounded targetIndex battle then
                        let battle = Battle.updateCurrentCommandOpt (constant None) battle
                        withMsgs [ResetCharacter sourceIndex; PoiseCharacter sourceIndex] battle
                    else withSigs (msg (ChargeCharacter sourceIndex) :: chargeEffects) battle

            | TechCharacter2 (sourceIndex, targetIndex, techType) ->
                match techType with
                | Critical ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let impactSplash = DisplayImpactSplash (30L, targetIndex)
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; impactSplash] battle
                | Cyclone ->
                    let time = World.getUpdateTime world
                    let radius = 64.0f
                    let position = (Battle.getCharacterBounds sourceIndex battle).Bottom
                    let playHits =
                        [PlaySound (20L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                         PlaySound (40L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                         PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                         PlaySound (80L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)]
                    let battle = Battle.animateCharacter time WhirlAnimation sourceIndex battle
                    withCmds (DisplayCircle (position, radius) :: DisplayCycloneBlur (0L, sourceIndex, radius) :: playHits) battle
                | DarkCritical ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let impactSplash = DisplayImpactSplash (30L, targetIndex) // TODO: darker impact splash to represent element.
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; impactSplash] battle
                | Slash ->
                    let time = World.getUpdateTime world
                    let playSlash = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.SlashSound)
                    let playHit = PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let slashSpike = DisplaySlashSpike (10L, (Battle.getCharacterBounds sourceIndex battle).Bottom, targetIndex)
                    let impactSplashes = Battle.evalTechMove sourceIndex targetIndex techType battle |> snd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayImpactSplash (70L, targetIndex))
                    let battle = Battle.animateCharacter time SlashAnimation sourceIndex battle
                    withCmds (playSlash :: playHit :: slashSpike :: impactSplashes) battle
                | PowerCut ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let cut = DisplayCut (30L, false, targetIndex)
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; cut] battle
                | PoisonCut ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let cut = DisplayCut (30L, false, targetIndex)
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; cut] battle
                | DoubleCut ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let cut = DisplayCut (30L, false, targetIndex)
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; cut] battle
                | DispelCut ->
                    let time = World.getUpdateTime world
                    let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                    let displayCut = DisplayCut (30L, true, targetIndex)
                    let battle = Battle.animateCharacter time AttackAnimation sourceIndex battle
                    withCmds [playHit; displayCut] battle
                | Fire ->
                    let time = World.getUpdateTime world
                    let playFire = PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.FireSound)
                    let displayFire = DisplayFire (0L, sourceIndex, targetIndex)
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmds [playFire; displayFire] battle
                | TechType.Flame ->
                    let time = World.getUpdateTime world
                    let playFlame = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.FlameSound)
                    let displayFlame = DisplayFlame (0L, sourceIndex, targetIndex)
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmds [playFlame; displayFlame] battle
                | Ice ->
                    let time = World.getUpdateTime world
                    let playIce = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.IceSound)
                    let displayIce = DisplayIce (0L, targetIndex)
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmds [playIce; displayIce] battle
                | Snowball ->
                    let time = World.getUpdateTime world
                    let playSnowball = PlaySound (15L, Constants.Audio.SoundVolumeDefault, Assets.Field.SnowballSound)
                    let displaySnowball = DisplaySnowball (0L, targetIndex)
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmds [playSnowball; displaySnowball] battle
                | Bolt ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmd (DisplayBolt (0L, targetIndex)) battle // TODO: use sound.
                | BoltBeam ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmd (DisplayBolt (0L, targetIndex)) battle // TODO: use new sound and effect.
                | Stone ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmd (DisplayIce (0L, targetIndex)) battle // TODO: use new sound and effect.
                | Quake ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmd (DisplayBolt (0L, targetIndex)) battle // TODO: use new sound and effect.
                | Cure ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playCure = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.CureSound)
                    let displayCures = Battle.evalTechMove sourceIndex targetIndex techType battle |> snd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayCure (0L, targetIndex))
                    withCmds (playCure :: displayCures) battle
                | Empower ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                    let displayBuff = DisplayBuff (0L, Power (true, true), targetIndex)
                    withCmds [playBuff; displayBuff] battle
                | Aura ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playCure = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.CureSound)
                    let displayCures = Battle.evalTechMove sourceIndex targetIndex techType battle |> snd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayCure (0L, targetIndex))
                    withCmds (playCure :: displayCures) battle
                | Enlighten ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                    let displayBuff = DisplayBuff (0L, Magic (true, true), targetIndex)
                    withCmds [playBuff; displayBuff] battle
                | Protect ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                    let displayBuff = DisplayBuff (0L, Shield (true, true), targetIndex)
                    withCmds [playBuff; displayBuff] battle
                | Weaken ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                    let displayDebuff = DisplayDebuff (0L, Power (false, false), targetIndex)
                    withCmds [playDebuff; displayDebuff] battle
                | Muddle ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                    let displayDebuff = DisplayDebuff (0L, Magic (false, false), targetIndex)
                    withCmds [playDebuff; displayDebuff] battle
                | ConjureIfrit ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playIfrit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.IfritSound)
                    let displayConjureIfrit = DisplayConjureIfrit 0L
                    withCmds [playIfrit; displayConjureIfrit] battle
                | Slow ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                    let displayDebuff = DisplayDebuff (0L, Time false, targetIndex)
                    withCmds [playDebuff; displayDebuff] battle
                | Purify ->
                    let time = World.getUpdateTime world
                    let battle = Battle.animateCharacter time Cast2Animation sourceIndex battle
                    withCmd (DisplayPurify (0L, targetIndex)) battle // TODO: use new sound and effect.

            | TechCharacter3 (sourceIndex, targetIndex, techType) ->
                let time = World.getUpdateTime world
                let results = Battle.evalTechMove sourceIndex targetIndex techType battle |> snd
                let (battle, cmds) =
                    Map.fold (fun (battle, cmds) characterIndex (cancelled, _, hitPointsChange, _, _) ->
                        if hitPointsChange < 0 && Battle.isCharacterHealthy characterIndex battle then
                            let battle = Battle.animateCharacter time DamageAnimation characterIndex battle
                            let cmds = if cancelled then DisplayCancel characterIndex :: cmds else cmds
                            (battle, cmds)
                        else (battle, cmds))
                        (battle, [])
                        results
                withCmds cmds battle

            | TechCharacter4 (sourceIndex, targetIndex, techType) ->
                let results = Battle.evalTechMove sourceIndex targetIndex techType battle |> snd 
                let (battle, sigs) =
                    Map.fold (fun (battle, sigs) _ (_, _, _, _, _) ->
                        // TODO: glow effect
                        (battle, sigs))
                        (battle, [])
                        results
                withSigs sigs battle

            | TechCharacter5 (sourceIndex, targetIndex, techType) ->
                let sourceBoundsOriginal = Battle.getCharacterBoundsOriginal sourceIndex battle
                let targetBounds = Battle.getCharacterBounds targetIndex battle
                let hopOpt =
                    match techType with
                    | Critical | DarkCritical | PoisonCut | PowerCut | DispelCut | DoubleCut ->
                        let hopDirection = Direction.ofVector2 (targetBounds.Bottom - sourceBoundsOriginal.Bottom)
                        let hopStart = targetBounds.Bottom - Direction.toVector2 hopDirection * Constants.Battle.StrikingDistance
                        Some
                            { HopStart = hopStart
                              HopStop = sourceBoundsOriginal.Bottom }
                    | Cyclone ->
                        Some
                            { HopStart = targetBounds.Bottom + Constants.Battle.CharacterBottomOffset3
                              HopStop = sourceBoundsOriginal.Bottom }
                    | _ -> None
                match hopOpt with
                | Some hop -> withCmd (DisplayHop hop) battle
                | None -> just battle

            | TechCharacter6 (sourceIndex, targetIndex, techType) ->
                let (techCost, results) = Battle.evalTechMove sourceIndex targetIndex techType battle
                let (battle, sigs) =
                    Map.fold (fun (battle, sigs) characterIndex (cancelled, affectsWounded, hitPointsChange, added, removed) ->
                        let battle = Battle.updateCharacterHitPoints cancelled affectsWounded hitPointsChange characterIndex battle
                        let randomizer = if sourceIndex.IsAlly then StatusType.randomizeStrong else StatusType.randomizeWeak
                        let added = added |> Set.toSeq |> Seq.filter randomizer |> Set.ofSeq
                        let battle = Battle.applyCharacterStatuses added removed characterIndex battle
                        let wounded = Battle.isCharacterWounded characterIndex battle
                        let sigs = if wounded then Message (ResetCharacter characterIndex) :: sigs else sigs
                        let sigs = if hitPointsChange <> 0 then Command (DisplayHitPointsChange (characterIndex, hitPointsChange)) :: sigs else sigs
                        let (battle, sigs) =
                            if wounded then
                                let woundCommand = ActionCommand.make Wound sourceIndex (Some characterIndex)
                                let battle = Battle.prependActionCommand woundCommand battle
                                (battle, sigs)
                            else
                                let sigs = Message (PoiseCharacter characterIndex) :: sigs
                                (battle, sigs)
                        (battle, sigs))
                        (battle, [])
                        results
                let battle = Battle.updateCharacterTechPoints -techCost sourceIndex battle
                let battle = Battle.advanceChargeTech sourceIndex battle
                let battle =
                    if Battle.shouldCounter sourceIndex targetIndex battle
                    then Battle.counterAttack sourceIndex targetIndex battle
                    else battle
                let battle = Battle.updateCurrentCommandOpt (constant None) battle
                let sigs = Message (PoiseCharacter sourceIndex) :: sigs
                withSigs sigs battle

            | TechCharacterAmbient (sourceIndex, _, _) ->
                if Simulants.Battle.Scene.Ride.Exists world then
                    let battle =
                        let tags = Simulants.Battle.Scene.Ride.GetEffectTags world
                        match Map.tryFind "Tag" tags with
                        | Some tag -> Battle.updateCharacterBottom (constant tag.Position) sourceIndex battle
                        | None -> battle
                    just battle
                else just battle

            | AutoBattleEnemies ->
                let battle = Battle.autoBattleEnemies battle
                just battle

            | ChargeCharacter sourceIndex ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharacter time (PoiseAnimation Charging) sourceIndex battle
                just battle

            | PoiseCharacter characterIndex ->
                let time = World.getUpdateTime world
                let battle = Battle.animationCharacterPoise time characterIndex battle
                just battle

            | WoundCharacter characterIndex ->
                let time = World.getUpdateTime world
                let battle = Battle.animateCharacterWound time characterIndex battle
                just battle

            | ResetCharacter characterIndex ->
                let battle = Battle.updateCharacterActionTime (constant 0.0f) characterIndex battle
                let battle =
                    if characterIndex.IsAlly
                    then Battle.updateCharacterInputState (constant NoInput) characterIndex battle
                    else Battle.updateCharacterAutoBattleOpt (constant None) characterIndex battle
                just battle

            | DestroyCharacter characterIndex ->
                let battle = if characterIndex.IsEnemy then Battle.removeCharacter characterIndex battle else battle
                just battle

            | Nop -> just battle

        override this.Command (battle, command, _, world) =

            match command with
            | UpdateEye ->
                let world = World.setEyeCenter v2Zero world
                just world
            
            | DisplayHop hop ->
                let effect = Effects.makeHopEffect hop.HopStart hop.HopStop
                let (entity, world) = World.createEntity<EffectDispatcher> (Some Simulants.Battle.Scene.Ride.Name) DefaultOverlay Simulants.Battle.Scene.Group world
                let world = entity.SetEffect effect world
                let world = entity.SetEffectOffset v2Zero world
                let world = entity.SetSelfDestruct true world
                just world

            | DisplayCircle (position, radius) ->
                let effect = Effects.makeCircleEffect radius
                let (entity, world) = World.createEntity<EffectDispatcher> (Some Simulants.Battle.Scene.Ride.Name) DefaultOverlay Simulants.Battle.Scene.Group world
                let world = entity.SetPosition position world
                let world = entity.SetEffect effect world
                let world = entity.SetEffectOffset v2Zero world
                let world = entity.SetSelfDestruct true world
                just world

            | DisplayCancel targetIndex ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target ->
                    let effect = Effects.makeCancelEffect ()
                    let (entity, world) = World.createEntity<EffectDispatcher> None DefaultOverlay Simulants.Battle.Scene.Group world
                    let world = entity.SetEffect effect world
                    let world = entity.SetCenter target.CenterOffset4 world
                    let world = entity.SetElevation (Constants.Battle.GuiEffectElevation + 1.0f) world
                    let world = entity.SetSelfDestruct true world
                    just world
                | None -> just world

            | DisplayHitPointsChange (targetIndex, delta) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target ->
                    let effect = Effects.makeHitPointsChangeEffect delta
                    let (entity, world) = World.createEntity<EffectDispatcher> None DefaultOverlay Simulants.Battle.Scene.Group world
                    let world = entity.SetEffect effect world
                    let world = entity.SetCenter target.CenterOffset3 world
                    let world = entity.SetElevation Constants.Battle.GuiEffectElevation world
                    let world = entity.SetSelfDestruct true world
                    just world
                | None -> just world

            | DisplayBolt (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 192.0f 758.0f) (Bottom target.Bottom) (Effects.makeBoltEffect ()) world |> just
                | None -> just world

            | DisplayCycloneBlur (delay, targetIndex, radius) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 234.0f 234.0f) (Center target.Center) (Effects.makeCycloneBlurEffect radius) world |> just
                | None -> just world

            | DisplayImpactSplash (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 192.0f 96.0f) (Bottom target.Bottom) (Effects.makeImpactSplashEffect ()) world |> just
                | None -> just world

            | DisplayCut (delay, light, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 144.0f) (Bottom target.Bottom) (Effects.makeCutEffect light) world |> just
                | None -> just world
            
            | DisplaySlashSpike (delay, bottom, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target ->
                    let projection = Vector2.Normalize (target.Bottom - bottom) * single Constants.Render.VirtualResolutionX + target.Bottom
                    let effect = (Effects.makeSlashSpikeEffect bottom projection)
                    let world = displayEffect delay (v2 96.0f 96.0f) (Bottom bottom) effect world
                    just world
                | None -> just world

            | DisplayArcaneCast (delay, sourceIndex) ->
                match Battle.tryGetCharacter sourceIndex battle with
                | Some source -> displayEffect delay (v2 300.0f 300.0f) (Bottom (source.Bottom - v2 0.0f 120.0f)) (Effects.makeArcaneCastEffect ()) world |> just
                | None -> just world
            
            | DisplayFire (delay, sourceIndex, targetIndex) ->
                match Battle.tryGetCharacter sourceIndex battle with
                | Some source ->
                    match Battle.tryGetCharacter targetIndex battle with
                    | Some target ->
                        let effect = Effects.makeFireEffect (source.Bottom + (v2 80.0f 80.0f)) (target.Bottom + (v2 0.0f 20.0f))
                        let world = displayEffect delay (v2 100.0f 100.0f) (Bottom (source.Bottom - v2 0.0f 50.0f)) effect world
                        just world
                    | None -> just world
                | None -> just world

            | DisplayFlame (delay, sourceIndex, targetIndex) ->
                match Battle.tryGetCharacter sourceIndex battle with
                | Some source ->
                    match Battle.tryGetCharacter targetIndex battle with
                    | Some target ->
                        let effect = Effects.makeFlameEffect source.CenterOffset target.CenterOffset
                        let world = displayEffect delay (v2 144.0f 144.0f) (Bottom source.Bottom) effect world
                        just world
                    | None -> just world
                | None -> just world
            
            | DisplayIce (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 48.0f) (Bottom target.Bottom) (Effects.makeIceEffect ()) world |> just
                | None -> just world
            
            | DisplaySnowball (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 432.0f 432.0f) (Bottom target.Bottom) (Effects.makeSnowballEffect ()) world |> just
                | None -> just world

            | DisplayHolyCast (delay, sourceIndex) ->
                match Battle.tryGetCharacter sourceIndex battle with
                | Some source -> displayEffect delay (v2 300.0f 300.0f) (Bottom (source.Bottom - v2 0.0f 100.0f)) (Effects.makeHolyCastEffect ()) world |> just
                | None -> just world
            
            | DisplayPurify (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 192.0f 192.0f) (Bottom (target.Bottom - v2 0.0f 100.0f)) (Effects.makePurifyEffect ()) world |> just
                | None -> just world

            | DisplayCure (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 48.0f) (Bottom target.Bottom) (Effects.makeCureEffect ()) world |> just
                | None -> just world
            
            | DisplayProtect (delay, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 48.0f) (Bottom target.Bottom) (Effects.makeProtectEffect ()) world |> just
                | None -> just world

            | DisplayDimensionalCast (delay, sourceIndex) ->
                match Battle.tryGetCharacter sourceIndex battle with
                | Some source -> displayEffect delay (v2 48.0f 48.0f) (Bottom source.Bottom) (Effects.makeDimensionalCastEffect ()) world |> just
                | None -> just world

            | DisplayBuff (delay, statusType, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 48.0f) (Bottom target.Bottom) (Effects.makeBuffEffect statusType) world |> just
                | None -> just world

            | DisplayDebuff (delay, statusType, targetIndex) ->
                match Battle.tryGetCharacter targetIndex battle with
                | Some target -> displayEffect delay (v2 48.0f 48.0f) (Bottom target.Bottom) (Effects.makeDebuffEffect statusType) world |> just
                | None -> just world

            | DisplayConjureIfrit delay ->
                displayEffect delay (v2 48.0f 48.0f) (Position (v2 0.0f 0.0f)) (Effects.makeConjureIfritEffect ()) world |> just

            | PlaySound (delay, volume, sound) ->
                let world = World.schedule (World.playSound volume sound) (World.getUpdateTime world + delay) world
                just world

            | PlaySong (fadeIn, fadeOut, volume, start, assetTag) ->
                let world = World.playSong fadeIn fadeOut volume start assetTag world
                just world

            | FadeOutSong fade ->
                let world = World.fadeOutSong fade world
                just world

        override this.Content (battle, _) =

            [// scene group
             Content.group Simulants.Battle.Scene.Group.Name []

                [// tile map
                 Content.tileMap Gen.name
                    [Entity.Position == v2 -480.0f -270.0f
                     Entity.Elevation == Constants.Battle.BackgroundElevation
                     Entity.TileMap <== battle --> fun battle -> battle.TileMap
                     Entity.TileIndexOffset <== battle --> fun battle -> battle.TileIndexOffset
                     Entity.TileIndexOffsetRange <== battle --> fun battle -> battle.TileIndexOffsetRange]

                 // dialog
                 Dialog.content Gen.name
                    (Constants.Battle.GuiElevation + 2.0f) Nop Nop
                    (battle --> fun battle -> (id, battle.DialogOpt)) // TODO: P1: pass in a real detokenizer!

                 // dialog interact button
                 Content.button Gen.name
                    [Entity.Position == v2 248.0f -240.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 144.0f 48.0f
                     Entity.UpImage == Assets.Gui.ButtonShortUpImage; Entity.DownImage == Assets.Gui.ButtonShortDownImage
                     Entity.Visible <== battle --> fun battle -> match battle.DialogOpt with Some dialog -> Dialog.canAdvance id dialog | None -> false // TODO: P1: pass in a real detokenizer!
                     Entity.Text == "Next"
                     Entity.ClickEvent ==> msg InteractDialog]

                 // allies
                 Content.entities battle
                    (fun battle _ -> Battle.getAllies battle)
                    (fun index ally _ -> Content.entity<CharacterDispatcher> (CharacterIndex.toEntityName index) [Entity.Character <== ally])

                 // enemies
                 Content.entities battle
                    (fun battle _ -> Battle.getEnemies battle)
                    (fun index enemy _ -> Content.entity<CharacterDispatcher> (CharacterIndex.toEntityName index) [Entity.Character <== enemy])]

             // input groups
             Content.groups battle (fun battle _ -> if battle.Running then Battle.getAllies battle else Map.empty) $ fun index ally _ ->

                // input group
                let inputName = "Input" + "+" + CharacterIndex.toEntityName index
                Content.group inputName [Group.Visible <== ally --> fun ally -> ally.IsHealthy]

                    [// health bar
                     Content.fillBar "HealthBar" 
                        [Entity.Size == v2 48.0f 6.0f
                         Entity.Center <== ally --> fun ally -> ally.BottomOffset
                         Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.Fill <== ally --> fun ally -> single ally.HitPoints / single ally.HitPointsMax]
                         
                     // tech bar
                     Content.fillBar "TechBar" 
                        [Entity.Size == v2 48.0f 6.0f
                         Entity.Center <== ally --> fun ally -> ally.BottomOffset2
                         Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.FillColor == Color (byte 74, byte 91, byte 169, byte 255)
                         Entity.Fill <== ally --> fun ally -> single ally.TechPoints / single ally.TechPointsMax]

                     // regular menu
                     Content.entity<RingMenuDispatcher> "RegularMenu"
                        [Entity.Position <== ally --> fun ally -> ally.CenterOffset
                         Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.Visible <== ally --> fun ally -> ally.InputState = RegularMenu
                         Entity.Enabled <== battle --> fun battle ->
                            let allies = battle |> Battle.getAllies |> Map.toValueList
                            let alliesPastRegularMenu =
                                Seq.notExists (fun (ally : Character) ->
                                    match ally.InputState with NoInput | RegularMenu -> false | _ -> true)
                                    allies
                            alliesPastRegularMenu
                         Entity.RingMenu == { Items = Map.ofList [("Attack", (0, true)); ("Tech", (1, true)); ("Consumable", (2, true)); ("Defend", (3, true))]; ItemCancelOpt = None }
                         Entity.ItemSelectEvent ==|> fun evt -> msg (RegularItemSelect (index, evt.Data))
                         Entity.CancelEvent ==> msg (RegularItemCancel index)]

                     // consumable menu
                     Content.entity<RingMenuDispatcher> "ConsumableMenu"
                        [Entity.Position <== ally --> fun ally -> ally.CenterOffset
                         Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.Visible <== ally --> fun ally -> ally.InputState = ItemMenu
                         Entity.RingMenu <== battle --> fun battle ->
                            let consumables =
                                battle.Inventory |>
                                Inventory.getConsumables |>
                                Map.ofSeqBy (fun kvp -> (scstringm kvp.Key, (getTag kvp.Key, true)))
                            { Items = consumables; ItemCancelOpt = Some "Cancel" }
                         Entity.ItemSelectEvent ==|> fun evt -> msg (ConsumableItemSelect (index, evt.Data))
                         Entity.CancelEvent ==> msg (ConsumableItemCancel index)]

                     // tech menu
                     Content.entity<RingMenuDispatcher> "TechMenu"
                        [Entity.Position <== ally --> fun ally -> ally.CenterOffset
                         Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.Visible <== ally --> fun ally -> ally.InputState = TechMenu
                         Entity.RingMenu <== ally --> fun ally ->
                            let techs =
                                ally.Techs |>
                                Map.ofSeqBy (fun tech ->
                                    let techUsable =
                                        match Map.tryFind tech Data.Value.Techs with
                                        | Some techData -> techData.TechCost <= ally.TechPoints && not (Map.containsKey Silence ally.Statuses)
                                        | None -> false
                                    (scstringm tech, (getTag tech, techUsable)))
                            { Items = techs; ItemCancelOpt = Some "Cancel" }
                         Entity.ItemSelectEvent ==|> fun evt -> msg (TechItemSelect (index, evt.Data))
                         Entity.CancelEvent ==> msg (TechItemCancel index)]

                     // reticles
                     Content.entity<ReticlesDispatcher> "Reticles"
                        [Entity.Elevation == Constants.Battle.GuiElevation
                         Entity.Visible <== ally --> fun ally -> match ally.InputState with AimReticles _ -> true | _ -> false
                         Entity.Reticles <== battle --> fun battle ->
                            let aimType =
                                match Battle.tryGetCharacter index battle with
                                | Some character -> character.InputState.AimType
                                | None -> NoAim
                            let characters = Battle.getTargets aimType battle
                            let reticles =
                                Map.map (fun _ (c : Character) ->
                                    match c.Stature with
                                    | BossStature -> c.CenterOffset2
                                    | _ -> c.CenterOffset)
                                    characters
                            reticles
                         Entity.TargetSelectEvent ==|> fun evt -> msg (ReticlesSelect (index, evt.Data))
                         Entity.CancelEvent ==> msg (ReticlesCancel index)]]]

    type DebugBattleDispatcher () =
        inherit BattleDispatcher ()
        override this.Prepare (_, _) = Battle.debug