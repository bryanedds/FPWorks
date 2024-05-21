﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2023.

namespace OmniBlade
open System
open System.Numerics
open Prime
open Nu
open BattleInteractionSystem

type BattleMessage =
    | Update
    | UpdateRideTokens of Map<string, Effects.Slice>
    | TimeUpdate
    | InteractDialog
    | RegularItemSelect of CharacterIndex * string
    | RegularItemCancel of CharacterIndex
    | ConsumableItemSelect of CharacterIndex * string
    | ConsumableItemCancel of CharacterIndex
    | TechItemSelect of CharacterIndex * string
    | TechItemCancel of CharacterIndex
    | ReticlesSelect of CharacterIndex * CharacterIndex
    | ReticlesCancel of CharacterIndex
    | Nop
    interface Message

type BattleCommand =
    | UpdateEye
    | Concluding
    | Conclude
    | PlaySound of int64 * single * Sound AssetTag
    | PlaySong of GameTime * GameTime * GameTime * single * Song AssetTag
    | FadeOutSong of GameTime
    | DisplayHop of Vector3 * Vector3
    | DisplayCircle of Vector3 * single
    | DisplayHitPointsChange of CharacterIndex * int
    | DisplayCancel of CharacterIndex
    | DisplayCut of int64 * bool * CharacterIndex
    | DisplayCritical of int64 * CharacterIndex
    | DisplayHeavyCritical of int64 * CharacterIndex
    | DisplayPoisonCut of int64 * CharacterIndex
    | DisplayPowerCut of int64 * CharacterIndex
    | DisplayDispelCut of int64 * CharacterIndex
    | DisplayDoubleCut of int64 * CharacterIndex
    | DisplaySlashSpike of int64 * Vector3 * CharacterIndex
    | DisplaySlashTwister of int64 * Vector3 * CharacterIndex
    | DisplayCycloneBlur of int64 * CharacterIndex * single
    | DisplayBuff of int64 * StatusType * CharacterIndex
    | DisplayDebuff of int64 * StatusType * CharacterIndex
    | DisplayImpactSplash of int64 * CharacterIndex
    | DisplayArcaneCast of int64 * CharacterIndex
    | DisplayHolyCast of int64 * CharacterIndex
    | DisplayDimensionalCast of int64 * CharacterIndex
    | DisplayGenericCast of int64 * CharacterIndex
    | DisplayFire of int64 * CharacterIndex * CharacterIndex
    | DisplayFlame of int64 * CharacterIndex * CharacterIndex
    | DisplayIce of int64 * CharacterIndex
    | DisplaySnowball of int64 * CharacterIndex
    | DisplayBolt of int64 * CharacterIndex
    | DisplayCure of int64 * CharacterIndex
    | DisplayProtect of int64 * CharacterIndex
    | DisplayPurify of int64 * CharacterIndex
    | DisplaySilk of int64 * CharacterIndex
    | DisplayInferno of int64
    | DisplayScatterBolt of int64
    interface Command

type Positioning =
    | Position of Vector3
    | Center of Vector3
    | Bottom of Vector3

type Layering =
    | Under
    | Over

type BattleSpeed =
    | SwiftSpeed
    | PacedSpeed
    | WaitSpeed

type BattleState =
    | BattleReadying of int64
    | BattleRunning
    | BattleResult of int64 * bool
    | BattleConcluding of int64 * bool
    | BattleConclude

type ActionCommand =
    { Action : ActionType
      SourceIndex : CharacterIndex
      TargetIndexOpt : CharacterIndex option
      ObserverIndexOpt : CharacterIndex option }

    static member make action sourceIndex targetIndexOpt observerIndexOpt =
        { Action = action
          SourceIndex = sourceIndex
          TargetIndexOpt = targetIndexOpt
          ObserverIndexOpt = observerIndexOpt }

type CurrentCommand =
    { StartTime : int64
      ActionCommand : ActionCommand }

    static member make startTime actionCommand =
        { StartTime = startTime; ActionCommand = actionCommand }

type [<ReferenceEquality; SymbolicExpansion>] Battle =
    { BattleTime : int64
      BattleState : BattleState
      Inventory : Inventory
      Characters : Map<CharacterIndex, Character>
      PrizePool : PrizePool
      TileMap : TileMap AssetTag
      TileIndexOffset : int
      TileIndexOffsetRange : int * int
      BattleSongOpt : Song AssetTag option
      CurrentCommandOpt : CurrentCommand option
      ActionCommands : ActionCommand FQueue
      MessageOpt : (int64 * int64 * Dialog) option
      DialogOpt : Dialog option
      BattleSpeed : BattleSpeed }

[<RequireQualifiedAccess>]
module Battle =

    (* Low-Level Operations *)

    let private sortActionCommands battle =
        let actionCommands = Array.ofSeq battle.ActionCommands
        let actionCommandsSorted = Array.sortStableBy (fun command -> match command.Action with Wound -> 0 | Consequence _ -> 1 | _ -> 2) actionCommands
        { battle with ActionCommands = FQueue.ofSeq actionCommandsSorted }

    let appendActionCommand command battle =
        { battle with ActionCommands = FQueue.conj command battle.ActionCommands }

    let prependActionCommand command battle =
        { battle with ActionCommands = FQueue.rev battle.ActionCommands |> FQueue.conj command |> FQueue.rev }

    (* Multi-Character Operations *)

    let getCharacters battle =
        battle.Characters

    let getCharactersIf pred battle =
        battle.Characters|>
        Map.filter pred

    let getCharactersHealthy battle =
        getCharacters battle |>
        Map.filter (fun _ character -> character.Healthy)

    let getCharactersWounded battle =
        getCharacters battle |>
        Map.filter (fun _ character -> character.Wounded)

    let getCharactersHudded battle =
        getCharactersIf (fun _ (character : Character) ->
            character.Ally ||
            (character.Enemy && character.Standing && character.MaterializationOpt.IsNone))
            battle

    let getAllies battle =
        battle.Characters |> Map.toSeq |> Seq.filter (function (AllyIndex _, _) -> true | _ -> false) |> Map.ofSeq

    let getAlliesHealthy battle =
        getAllies battle |>
        Map.filter (fun _ character -> character.Healthy)

    let getAlliesWounded battle =
        getAllies battle |>
        Map.filter (fun _ character -> character.Wounded)

    let getJinnInParty battle =
        getAllies battle |>
        Seq.exists (fun entry -> entry.Value.CharacterType = Ally Jinn)

    let getEnemies battle =
        battle.Characters |> Map.toSeq |> Seq.filter (function (EnemyIndex _, _) -> true | _ -> false) |> Map.ofSeq

    let getEnemiesHealthy battle =
        getEnemies battle |>
        Map.filter (fun _ character -> character.Healthy)

    let getEnemiesWounded battle =
        getEnemies battle |>
        Map.filter (fun _ character -> character.Wounded)

    let getEnemiesStanding battle =
        battle.Characters |> Map.toSeq |> Seq.filter (function (EnemyIndex _, enemy) -> enemy.Standing | _ -> false) |> Map.ofSeq

    let getEnemiesSwooning battle =
        battle.Characters |> Map.toSeq |> Seq.filter (function (EnemyIndex _, enemy) -> enemy.Swooning | _ -> false) |> Map.ofSeq

    let getFriendlies ally battle =
        if ally then getAllies battle else getEnemies battle

    let getFriendliesHealthy ally battle =
        if ally then getAlliesHealthy battle else getEnemiesHealthy battle

    let getFriendliesWounded ally battle =
        if ally then getAlliesWounded battle else getEnemiesWounded battle

    let getUnfriendlies ally battle =
        if ally then getEnemies battle else getAllies battle

    let getUnfriendliesHealthy ally battle =
        if ally then getEnemiesHealthy battle else getAlliesHealthy battle

    let getUnfriendliesWounded ally battle =
        if ally then getEnemiesWounded battle else getAlliesWounded battle

    let getTargets aimType battle =
        match aimType with
        | EnemyAim _ ->
            getEnemiesStanding battle
        | AllyAim healthy ->
            if healthy
            then getAlliesHealthy battle
            else getAlliesWounded battle
        | AnyAim healthy ->
            let allies =
                if healthy
                then getAlliesHealthy battle
                else getAlliesWounded battle
            let enemies =
                if healthy
                then getEnemiesStanding battle
                else getEnemiesSwooning battle
            let characters = allies @@ enemies
            characters
        | NoAim -> Map.empty

    let getAllyIndices battle =
        getAllies battle |>
        Map.toKeyList

    let getEnemyIndices battle =
        getEnemies battle |>
        Map.toKeyList

    let nextEnemyIndex battle =
        let mutable lastIndex = 0
        for entry in getEnemies battle do
            if entry.Key.Subindex > lastIndex then
                lastIndex <- entry.Key.Subindex
        let enemySubindex = inc lastIndex
        EnemyIndex enemySubindex

    let getAlliesHealthyIndices battle =
        getAlliesHealthy battle |>
        Map.toKeyList

    let getAlliesWoundedIndices battle =
        getAlliesWounded battle |>
        Map.toKeyList

    let mapCharactersIf predicate updater (battle : Battle) =
        { battle with Characters = Map.map (fun i c -> if predicate i c then updater c else c) battle.Characters }

    let mapCharacters updater battle =
        mapCharactersIf tautology2 updater battle

    let mapCharactersHealthy updater battle =
        mapCharactersIf (fun _ character -> character.Healthy) updater battle

    let mapCharactersWounded updater battle =
        mapCharactersIf (fun _ character -> character.Wounded) updater battle

    let mapAlliesIf pred updater battle =
        mapCharactersIf (fun i c -> pred i c && match i with AllyIndex _ -> true | _ -> false) updater battle

    let mapAllies updater battle =
        mapAlliesIf tautology2 updater battle

    let mapEnemiesIf pred updater battle =
        mapCharactersIf (fun i c -> pred i c && match i with EnemyIndex _ -> true | _ -> false) updater battle

    let mapEnemies updater battle =
        mapEnemiesIf tautology2 updater battle

    let private finalizeMaterializations battle =
        mapCharacters (fun character ->
            match character.MaterializationOpt with
            | Some Materializing -> Character.materialized battle.BattleTime character
            | Some _ | None -> character)
            battle

    let private populateAlliesConjureCharges battle =
        mapAllies (fun ally ->
            if Character.hasConjureTechs ally
            then Character.mapConjureChargeOpt (constant (Some 0)) ally
            else ally)
            battle

    let private autoBattleEnemies battle =
        let jinnInParty = getJinnInParty battle
        let alliesHealthy = getAlliesHealthy battle
        let alliesWounded = getAlliesWounded battle
        let enemiesStanding = getEnemiesStanding battle
        let enemiesSwooning = getEnemiesSwooning battle
        mapEnemies (Character.autoBattle jinnInParty alliesHealthy alliesWounded enemiesStanding enemiesSwooning) battle

    (* Individual Character Operations *)

    let addCharacter index character (battle : Battle) =
        { battle with Characters = Map.add index character battle.Characters }

    let removeCharacter index (battle : Battle) =
        { battle with Characters = Map.remove index battle.Characters }

    let containsCharacter characterIndex battle =
        Map.containsKey characterIndex battle.Characters

    let containsCharacterHealthy characterIndex battle =
        match battle.Characters.TryGetValue characterIndex with
        | (true, character) -> character.Healthy
        | (false, _) -> false

    let containsCharacterWounded characterIndex battle =
        match battle.Characters.TryGetValue characterIndex with
        | (true, character) -> character.Wounded
        | (false, _) -> false

    let containsCharacterStanding characterIndex battle =
        match battle.Characters.TryGetValue characterIndex with
        | (true, character) -> character.Standing
        | (false, _) -> false

    let containsCharacterSwooning characterIndex battle =
        match battle.Characters.TryGetValue characterIndex with
        | (true, character) -> character.Swooning
        | (false, _) -> false

    let tryGetCharacter characterIndex battle =
        Map.tryFind characterIndex battle.Characters

    let tryGetCharacterBy by characterIndex battle =
        match Map.tryFind characterIndex battle.Characters with
        | Some character -> Some (by character)
        | None -> None

    let getCharacter characterIndex battle =
        tryGetCharacter characterIndex battle |> Option.get

    let getCharacterBy by characterIndex battle =
        tryGetCharacter characterIndex battle |> Option.get |> by

    let getCharacterHealthy characterIndex battle =
        (getCharacter characterIndex battle).Healthy

    let getCharacterWounded characterIndex battle =
        (getCharacter characterIndex battle).Wounded

    let getCharacterStanding characterIndex battle =
        (getCharacter characterIndex battle).Standing

    let getCharacterSwooning characterIndex battle =
        (getCharacter characterIndex battle).Swooning

    let getCharacterPerimeterOriginal characterIndex battle =
        (getCharacter characterIndex battle).PerimeterOriginal

    let getCharacterPerimeter characterIndex battle =
        (getCharacter characterIndex battle).Perimeter

    let getCharacterActionTime characterIndex battle =
        getCharacterBy (fun character -> character.ActionTime) characterIndex battle

    let getCharacterAnimationFinished characterIndex battle =
        getCharacterBy (Character.getAnimationFinished battle.BattleTime) characterIndex battle

    let getCharacterArchetypeType characterIndex battle =
        (getCharacter characterIndex battle).ArchetypeType

    let getCharacterVulnerabilities characterIndex battle =
        (getCharacter characterIndex battle).Vulnerabilities

    let getCharacterAppendedActionCommand characterIndex battle =
        seq battle.ActionCommands |>
        Seq.exists (fun command -> command.SourceIndex = characterIndex)

    let shouldCharacterCounter sourceIndex targetIndex battle =
        if CharacterIndex.unfriendly sourceIndex targetIndex
        then getCharacterBy Character.shouldCounter sourceIndex battle
        else false

    let private tryWithCharacter updater characterIndex battle =
        match tryGetCharacter characterIndex battle with
        | Some character ->
            let character = updater character
            { battle with Characters = Map.add characterIndex character battle.Characters }
        | None -> battle

    let private mapCharacter updater characterIndex battle =
        let character = getCharacter characterIndex battle
        let character = updater character
        { battle with Characters = Map.add characterIndex character battle.Characters }

    let mapCharacterInputState updater characterIndex battle =
        mapCharacter (Character.mapCharacterInputState updater) characterIndex battle

    let mapCharacterActionTime updater characterIndex battle =
        mapCharacter (Character.mapActionTime updater) characterIndex battle

    let mapCharacterTechChargeOpt updater characterIndex battle =
        mapCharacter (Character.mapTechChargeOpt updater) characterIndex battle

    let mapCharacterAutoBattleOpt updater characterIndex battle =
        mapCharacter (Character.mapAutoBattleOpt updater) characterIndex battle

    let mapCharacterAutoTechOpt updater characterIndex battle =
        mapCharacter (fun character ->
            let techTypeOpt = updater character.AutoBattleOpt
            Character.mapAutoBattleOpt (function Some autoBattle -> Some { autoBattle with AutoTechOpt = techTypeOpt; ChargeTech = techTypeOpt.IsSome } | None -> None) character)
            characterIndex
            battle

    let mapCharacterPerimeter updater characterIndex battle =
        mapCharacter (Character.mapPerimeter updater) characterIndex battle

    let mapCharacterHitPoints directAction cancelled affectsWounded hitPointsChange characterIndex battle =
        let alliesHealthy = getAlliesHealthy battle
        let character = getCharacter characterIndex battle
        let character = Character.mapHitPoints (fun hitPoints -> (cancelled, hitPoints + hitPointsChange)) affectsWounded alliesHealthy character
        let character = if directAction then Character.mapStatuses (Map.remove Sleep) character else character
        mapCharacter (constant character) characterIndex battle

    let mapCharacterTechPoints techPointsChange characterIndex battle =
        mapCharacter (Character.mapTechPoints ((+) techPointsChange)) characterIndex battle

    let applyCharacterStatuses added removed characterIndex battle =
        mapCharacter (Character.applyStatusChanges added removed) characterIndex battle

    let applyCharacterVulnerabilities added removed characterIndex battle =
        mapCharacter (Character.applyVulnerabilityChanges added removed) characterIndex battle

    let addCharacterInteraction interaction characterIndex battle =
        mapCharacter (Character.addInteraction interaction) characterIndex battle

    let clearCharacterInteractions characterIndex battle =
        mapCharacter Character.clearInteractions characterIndex battle

    let chargeCharacter chargeAmount characterIndex battle =
        mapCharacter
            (Character.mapTechChargeOpt
                (function
                 | Some (chargeRate, chargeAmount', techType) -> Some (chargeRate, max 0 (min Constants.Battle.ChargeMax (chargeAmount + chargeAmount')), techType)
                 | None -> None))
            characterIndex
            battle

    let defendCharacter characterIndex battle =
        mapCharacter Character.defend characterIndex battle

    let undefendCharacter characterIndex battle =
        mapCharacter Character.undefend characterIndex battle

    let materializeCharacter characterIndex battle =
        mapCharacter (Character.materialize battle.BattleTime) characterIndex battle

    let dematerializeCharacter characterIndex battle =
        mapCharacter (Character.dematerialize battle.BattleTime) characterIndex battle

    let faceCharacter direction characterIndex battle =
        mapCharacter (Character.face direction) characterIndex battle

    let faceCharacterAtAutoTarget characterIndex battle =
        let character = getCharacter characterIndex battle
        match character.AutoBattleOpt with
        | Some autoBattle ->
            let target = getCharacter autoBattle.AutoTarget battle
            let sourceToTarget = target.Perimeter.Bottom - character.Perimeter.Bottom
            let direction = if sourceToTarget.X >= 0.0f then Rightward else Leftward // only two directions in this game
            let source = Character.face direction character
            mapCharacter (constant source) characterIndex battle
        | None -> battle

    let animateCharacter animation characterIndex battle =
        mapCharacter (Character.animate battle.BattleTime animation) characterIndex battle

    let animationCharacterPoise characterIndex battle =
        mapCharacter (fun character ->
            let poiseType = Character.getPoiseType character
            let character = Character.animate battle.BattleTime (PoiseAnimation poiseType) character
            character)
            characterIndex
            battle

    let animateCharacterWound characterIndex battle =
        mapCharacter (fun character ->
            let character =
                if character.Ally
                then Character.mapCharacterInputState (constant NoInput) character
                else character
            let character = Character.animate battle.BattleTime WoundAnimation character
            character)
            characterIndex
            battle

    let animateCharactersReady battle =
        mapCharactersHealthy (Character.animate battle.BattleTime ReadyAnimation) battle

    let animateCharactersCelebrate outcome battle =
        if outcome
        then mapAlliesIf (fun _ ally -> ally.Healthy) (Character.animate battle.BattleTime CelebrateAnimation) battle
        else mapEnemiesIf (fun _ enemy -> enemy.Healthy) (Character.animate battle.BattleTime CelebrateAnimation) battle

    let animateCharactersPoised battle =
        mapCharactersHealthy (fun character ->
            let poiseType = Character.getPoiseType character
            let character = Character.animate battle.BattleTime (PoiseAnimation poiseType) character
            character)
            battle

    let characterCounterAttack sourceIndex targetIndex battle =
        let battle = prependActionCommand (ActionCommand.make Attack sourceIndex (Some targetIndex) None) battle
        let battle = prependActionCommand (ActionCommand.make (ActionType.Message ("Counter!", 45L)) sourceIndex (Some targetIndex) None) battle
        battle

    let halveCharacterActionTime characterIndex battle =
        mapCharacterActionTime (fun at -> min (at * 0.5f) (Constants.Battle.ActionTime * 0.5f)) characterIndex battle

    let resetCharacterActionTime characterIndex battle =
        mapCharacterActionTime (constant 0.0f) characterIndex battle

    let resetCharacterInput (characterIndex : CharacterIndex) battle =
        let battle =
            if characterIndex.Ally
            then mapCharacterInputState (constant NoInput) characterIndex battle
            else mapCharacterAutoBattleOpt (constant None) characterIndex battle
        battle

    let resetCharacterTechCharge characterIndex battle =
        mapCharacter Character.resetTechCharge characterIndex battle

    let resetCharacterConjureCharge characterIndex battle =
        mapCharacter Character.resetConjureCharge characterIndex battle

    let updateCharacterConjureCharge characterIndex battle =
        mapCharacter Character.updateConjureCharge characterIndex battle

    let updateCharacterStatusBurndown burndownTime characterIndex battle =
        mapCharacter (Character.burndownStatuses burndownTime) characterIndex battle

    let abortCharacterInteraction characterIndex battle =
        let battle =
            // HACK: tries to infer that this action is not coming from a battle interaction consequence when AT = 0.
            // There are some cases where this isn't the correct inference, tho.
            if getCharacterActionTime characterIndex battle = 0.0f
            then updateCharacterConjureCharge characterIndex battle
            else battle
        let battle = mapCharacterAutoBattleOpt (constant None) characterIndex battle
        let battle = mapCharacterActionTime (constant 0.0f) characterIndex battle
        let battle = animationCharacterPoise characterIndex battle
        let battle = resetCharacterInput characterIndex battle
        let battle = { battle with CurrentCommandOpt = None }
        battle

    let finishCharacterInteraction characterIndex battle =
        let battle =
            // HACK: tries to infer that this action is not coming from a battle interaction consequence when AT = 0.
            // There are some cases where this isn't the correct inference, tho.
            if getCharacterActionTime characterIndex battle = 0.0f
            then updateCharacterConjureCharge characterIndex battle
            else battle
        let battle = faceCharacterAtAutoTarget characterIndex battle
        battle

    let cancelCharacterInput characterIndex battle =
        tryWithCharacter (fun character ->
            match Character.getActionTypeOpt character with
            | Some actionType ->
                let inputState =
                    match actionType with
                    | Attack -> RegularMenu
                    | Defend -> RegularMenu
                    | Tech _ -> TechMenu
                    | Consume _ -> ItemMenu
                    | Consequence _ | ActionType.Message (_, _) | Wound -> failwithumf ()
                Character.mapCharacterInputState (constant inputState) character
            | None -> character)
            characterIndex
            battle

    let confirmCharacterInput sourceIndex targetIndex battle =
        let source = getCharacter sourceIndex battle
        match Character.getActionTypeOpt source with
        | Some actionType ->
            let command = ActionCommand.make actionType sourceIndex (Some targetIndex) None
            appendActionCommand command battle
        | None -> battle

    let retargetCharacter sourceIndex targetIndex battle =
        match tryGetCharacter targetIndex battle with
        | Some target when target.Healthy ->
            tryWithCharacter (fun source ->
                if source.Healthy
                then Character.mapAutoBattleOpt (function Some autoBattle -> Some { autoBattle with AutoTarget = targetIndex } | None -> None) source
                else source)
                sourceIndex
                battle
        | Some _ | None -> battle

    (* Evaluation Operations *)

    let evalRetarget affectingWounded targetIndexOpt battle =
        match targetIndexOpt with
        | Some targetIndex ->
            if affectingWounded then
                match tryGetCharacterBy (fun (target : Character) -> target.Healthy) targetIndex battle with
                | Some true | None ->
                    match targetIndex with
                    | AllyIndex _ -> Gen.randomItemOpt (Map.toKeyList (Map.remove targetIndex (getAlliesWounded battle)))
                    | EnemyIndex _ -> Gen.randomItemOpt (Map.toKeyList (Map.remove targetIndex (getEnemiesSwooning battle)))
                | Some false -> targetIndexOpt
            else
                match tryGetCharacterBy (fun (target : Character) -> target.Wounded) targetIndex battle with
                | Some true | None ->
                    match targetIndex with
                    | AllyIndex _ -> Gen.randomItemOpt (Map.toKeyList (Map.remove targetIndex (getAlliesHealthy battle)))
                    | EnemyIndex _ -> Gen.randomItemOpt (Map.toKeyList (Map.remove targetIndex (getEnemiesStanding battle)))
                | Some false -> targetIndexOpt
        | None -> targetIndexOpt

    let evalAttack effectType sourceIndex targetIndex battle =
        let source = getCharacter sourceIndex battle
        let target = getCharacter targetIndex battle
        Character.evalAttack effectType source target

    let evalTechUnary splash targetCount techData sourceIndex targetIndex battle =
        let source = getCharacter sourceIndex battle
        let target = getCharacter targetIndex battle
        (techData.TechCost, Character.evalTechUnary splash targetCount techData source target)

    let evalTech sourceIndex targetIndex techType battle =
        match Map.tryFind techType Data.Value.Techs with
        | Some techData ->
            let source = getCharacter sourceIndex battle
            let target = getCharacter targetIndex battle
            let characters = getCharacters battle
            Triple.prepend techData.TechCost (Character.evalTech techData source target characters)
        | None -> (0, None, Map.empty)

    let rec private evalSingleTargetType targetType (source : Character) (target : Character) (observer : Character) battle =
        match targetType with
        | Self -> observer.CharacterIndex = target.CharacterIndex
        | Other -> observer.CharacterIndex <> target.CharacterIndex
        | SelfOrFriendly -> let friendlies = getFriendlies observer.Ally battle in Map.containsKey target.CharacterIndex friendlies
        | Friendly -> let friendlies = getFriendlies observer.Ally battle in observer.CharacterIndex <> target.CharacterIndex && Map.containsKey target.CharacterIndex friendlies
        | Unfriendly -> let unfriendlies = getUnfriendlies observer.Ally battle in Map.containsKey target.CharacterIndex unfriendlies
        | Type ty -> target.CharacterType = ty
        | BattleTargetType.Any targetTypes -> List.exists (fun targetType -> evalSingleTargetType targetType source target observer battle) targetTypes
        | BattleTargetType.All targetTypes -> List.forall (fun targetType -> evalSingleTargetType targetType source target observer battle) targetTypes

    let rec private evalAttackAffectType affectType (source : Character) (target : Character) (observer : Character) battle =
        match affectType with
        | Physical | Touching -> source.ArchetypeType.AttackTouchingArchetype
        | Magical | Affinity _ | Item | OrbEmptied | OrbFilled | Cancelled | Uncancelled | Buffed | Debuffed -> false
        | Wounded -> target.Wounded
        | Random chance -> Gen.randomf < chance
        | HitPointsLessThanOrEqual ceiling -> target.Healthy && single target.HitPoints / single target.HitPointsMax <= ceiling
        | HitPointsGreaterThanOrEqual floor -> target.Healthy && single target.HitPoints / single target.HitPointsMax >= floor
        | TechPointsLessThanOrEqual ceiling -> single target.TechPoints / single target.TechPointsMax <= ceiling
        | TechPointsGreaterThanOrEqual floor -> single target.TechPoints / single target.TechPointsMax >= floor
        | Any affectTypes -> List.exists (fun affectType -> evalAttackAffectType affectType source target observer battle) affectTypes
        | All affectTypes -> List.forall (fun affectType -> evalAttackAffectType affectType source target observer battle) affectTypes

    let private evalAttackInteractions4 (source : Character) (target : Character) (observer : Character) battle =
        List.fold (fun consequences interaction ->
            let condition = interaction.BattleCondition
            let consequences' = interaction.BattleConsequences
            let satisfied =
                match condition with
                | LastSurviving ->
                    if observer.Healthy then
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendliesHealthy.Count = 1
                    else false
                | LastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesHealthyDifferent.Count = 0
                    else false
                | BecomeLastSurviving ->
                    if observer.Healthy then
                        let friendlies = getFriendlies observer.Ally battle
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendlies.Count > 1 && friendliesHealthy.Count = 1
                    else false
                | BecomeLastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesWoundedDifferent = battle |> getFriendliesWounded observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesWoundedDifferent.Count > 0 && friendliesHealthyDifferent.Count = 0
                    else false
                | AffectedTarget (affectType, targetType) ->
                    if observer.Healthy || affectType = Wounded then
                        evalAttackAffectType affectType source target observer battle &&
                        evalSingleTargetType targetType source target observer battle
                    else false
            if satisfied then consequences @ consequences' else consequences)
            [] observer.Interactions

    let evalAttackInteractions sourceIndex targetIndex battle =
        let source = getCharacter sourceIndex battle
        let target = getCharacter targetIndex battle
        let characters = getCharacters battle
        Seq.foldBack (fun (observerIndex, observer : Character) consequences ->
            let consequences' = evalAttackInteractions4 source target observer battle
            (sourceIndex, targetIndex, observerIndex, consequences') :: consequences)
            characters.Pairs []

    let rec private evalItemAffectType affectType (source : Character) (target : Character) (observer : Character) battle =
        match affectType with
        | Physical | Touching | Magical | Affinity _ | OrbEmptied | OrbFilled | Cancelled | Uncancelled | Buffed | Debuffed -> false
        | Item -> true
        | Wounded -> target.Wounded
        | Random chance -> Gen.randomf < chance
        | HitPointsLessThanOrEqual ceiling -> target.Healthy && single target.HitPoints / single target.HitPointsMax <= ceiling
        | HitPointsGreaterThanOrEqual floor -> target.Healthy && single target.HitPoints / single target.HitPointsMax >= floor
        | TechPointsLessThanOrEqual ceiling -> single target.TechPoints / single target.TechPointsMax <= ceiling
        | TechPointsGreaterThanOrEqual floor -> single target.TechPoints / single target.TechPointsMax >= floor
        | Any affectTypes -> List.exists (fun affectType -> evalItemAffectType affectType source target observer battle) affectTypes
        | All affectTypes -> List.forall (fun affectType -> evalItemAffectType affectType source target observer battle) affectTypes

    let private evalItemInteractions4 (source : Character) (target : Character) (observer : Character) battle =
        List.fold (fun consequences interaction ->
            let condition = interaction.BattleCondition
            let consequences' = interaction.BattleConsequences
            let satisfied =
                match condition with
                | LastSurviving ->
                    if observer.Healthy then
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendliesHealthy.Count = 1
                    else false
                | LastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesHealthyDifferent.Count = 0
                    else false
                | BecomeLastSurviving ->
                    if observer.Healthy then
                        let friendlies = getFriendlies observer.Ally battle
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendlies.Count > 1 && friendliesHealthy.Count = 1
                    else false
                | BecomeLastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesWoundedDifferent = battle |> getFriendliesWounded observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesWoundedDifferent.Count > 0 && friendliesHealthyDifferent.Count = 0
                    else false
                | AffectedTarget (affectType, targetType) ->
                    if observer.Healthy || affectType = Wounded then
                        evalItemAffectType affectType source target observer battle &&
                        evalSingleTargetType targetType source target observer battle
                    else false
            if satisfied then consequences @ consequences' else consequences)
            [] observer.Interactions

    let evalItemInteractions sourceIndex targetIndex battle =
        let source = getCharacter sourceIndex battle
        let target = getCharacter targetIndex battle
        let characters = getCharacters battle
        Seq.foldBack (fun (observerIndex, observer : Character) consequences ->
            let consequences' = evalItemInteractions4 source target observer battle
            (sourceIndex, targetIndex, observerIndex, consequences') :: consequences)
            characters.Pairs []

    let rec private evalTechAffectType affectType techType cancelled affectsWounded delta statusesAdded statusesRemoved (source : Character) (target : Character) (observer : Character) (battle : Battle) =
        match Data.Value.Techs.TryGetValue techType with
        | (true, tech) ->
            match affectType with
            | Physical -> tech.EffectType = EffectType.Physical
            | Magical -> tech.EffectType = EffectType.Magical
            | Touching -> tech.TechType.TouchingTech
            | Affinity affinity -> tech.AffinityOpt = Some affinity
            | Item -> false
            | OrbEmptied -> false
            | OrbFilled -> false
            | Cancelled -> cancelled
            | Uncancelled -> not cancelled && Option.isSome target.AutoBattleOpt
            | Debuffed -> Seq.exists StatusType.debuff statusesAdded
            | Buffed -> Seq.exists StatusType.buff statusesAdded
            | Wounded -> target.Wounded
            | Random chance -> Gen.randomf < chance
            | HitPointsLessThanOrEqual ceiling -> target.Healthy && single target.HitPoints / single target.HitPointsMax <= ceiling
            | HitPointsGreaterThanOrEqual floor -> target.Healthy && single target.HitPoints / single target.HitPointsMax >= floor
            | TechPointsLessThanOrEqual ceiling -> single target.TechPoints / single target.TechPointsMax <= ceiling
            | TechPointsGreaterThanOrEqual floor -> single target.TechPoints / single target.TechPointsMax >= floor
            | Any affectTypes -> List.exists (fun affectType -> evalTechAffectType affectType techType cancelled affectsWounded delta statusesAdded statusesRemoved source target observer battle) affectTypes
            | All affectTypes -> List.forall (fun affectType -> evalTechAffectType affectType techType cancelled affectsWounded delta statusesAdded statusesRemoved source target observer battle) affectTypes
        | (false, _) -> false

    let private evalTechInteractions4 (source : Character) (_ : Character) (observer : Character) (techType : TechType) (techResults : Map<CharacterIndex, bool * bool * int * StatusType Set * StatusType Set>) battle =
        List.fold (fun consequences interaction ->
            let condition = interaction.BattleCondition
            let consequences' = interaction.BattleConsequences
            let satisfied =
                match condition with
                | LastSurviving ->
                    if observer.Healthy then
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendliesHealthy.Count = 1
                    else false
                | LastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesHealthyDifferent.Count = 0
                    else false
                | BecomeLastSurviving ->
                    if observer.Healthy then
                        let friendlies = getFriendlies observer.Ally battle
                        let friendliesHealthy = getFriendliesHealthy observer.Ally battle
                        friendlies.Count > 1 && friendliesHealthy.Count = 1
                    else false
                | BecomeLastTypeSurviving ->
                    if observer.Healthy then
                        let friendliesWoundedDifferent = battle |> getFriendliesWounded observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        let friendliesHealthyDifferent = battle |> getFriendliesHealthy observer.Ally |> Map.filter (fun _ (ally : Character) -> ally.ArchetypeType <> observer.ArchetypeType)
                        friendliesWoundedDifferent.Count > 0 && friendliesHealthyDifferent.Count = 0
                    else false
                | AffectedTarget (affectType, targetType) ->
                    if observer.Healthy || affectType = Wounded then
                        techResults |>
                        Map.map (fun characterIndex result -> (result, tryGetCharacter characterIndex battle)) |>
                        Map.toValueList |>
                        List.exists (fun ((cancelled, affectsWounded, delta, statusesAdded, statusesRemoved), targetOpt) ->
                            match targetOpt with
                            | Some target ->
                                evalTechAffectType affectType techType cancelled affectsWounded delta statusesAdded statusesRemoved source target observer battle &&
                                evalSingleTargetType targetType source target observer battle
                            | None -> false)
                    else false
            if satisfied then consequences @ consequences' else consequences)
            [] observer.Interactions

    let evalTechInteractions sourceIndex targetIndex techType techResults battle =
        let source = getCharacter sourceIndex battle
        let target = getCharacter targetIndex battle
        let characters = getCharacters battle
        Seq.foldBack (fun (observerIndex, observer : Character) consequences ->
            let consequences' = evalTechInteractions4 source target observer techType techResults battle
            (sourceIndex, targetIndex, observerIndex, consequences') :: consequences)
            characters.Pairs []

    let evalConsequences consequences battle =
        let battle =
            (battle, consequences) ||> List.fold (fun battle (sourceIndex, targetIndex, observerIndex, consequences) ->
                (battle, consequences) ||> List.fold (fun battle consequence ->
                    appendActionCommand (ActionCommand.make (Consequence consequence) sourceIndex (Some targetIndex) (Some observerIndex)) battle))
        sortActionCommands battle

    (* Mid-Level Operations *)

    let rec private tryRandomizeEnemy attempts index enemy (layout : Either<unit, (int * StatureType * EnemyType) option> array array) =
        if attempts < 10000 then
            match Data.Value.Characters.TryFind (Enemy enemy) with
            | Some characterData ->
                match Data.Value.Archetypes.TryFind characterData.ArchetypeType with
                | Some archetypeData ->
                    let (w, h) = (layout.Length, layout.[0].Length)
                    let (x, y) =
                        if index = 0 && characterData.Boss
                        then (w / 2, h / 2 - 1) // HACK: put boss enemy 0 in center.
                        else (Gen.random1 w, Gen.random1 h)
                    let stature = archetypeData.Stature
                    match stature with
                    | SmallStature | NormalStature | LargeStature ->
                        if x > 0 && x < w - 1 && y < h - 1 then
                            match
                                (layout.[x-1].[y+1], layout.[x+0].[y+1], layout.[x+1].[y+1],
                                 layout.[x-1].[y+0], layout.[x+0].[y+0], layout.[x+1].[y+0]) with
                            |   (Left (), Left (), Left (),
                                 Left (), Left (), Left ()) ->
                                layout.[x-1].[y+1] <- Right None; layout.[x+0].[y+1] <- Right None; layout.[x+1].[y+1] <- Right None
                                layout.[x-1].[y+0] <- Right None; layout.[x+0].[y+0] <- Right (Some (index, stature, enemy)); layout.[x+1].[y+0] <- Right None
                            | _ -> tryRandomizeEnemy (inc attempts) index enemy layout
                        else tryRandomizeEnemy (inc attempts) index enemy layout
                    | BossStature ->
                        if x > 1 && x < w - 2 && y > 0 && y < h - 3 then
                            match
                                (layout.[x-2].[y+3], layout.[x-1].[y+3], layout.[x+0].[y+3], layout.[x+1].[y+3], layout.[x+2].[y+3],
                                 layout.[x-2].[y+2], layout.[x-1].[y+2], layout.[x+0].[y+2], layout.[x+1].[y+2], layout.[x+2].[y+2],
                                 layout.[x-2].[y+1], layout.[x-1].[y+1], layout.[x+0].[y+1], layout.[x+1].[y+1], layout.[x+2].[y+1],
                                 layout.[x-2].[y+0], layout.[x-1].[y+0], layout.[x+0].[y+0], layout.[x+1].[y+0], layout.[x+2].[y+0],
                                 layout.[x-2].[y-1], layout.[x-1].[y-1], layout.[x+0].[y-1], layout.[x+1].[y-1], layout.[x+2].[y-1]) with
                            |   (Left (), Left (), Left (), Left (), Left (),
                                 Left (), Left (), Left (), Left (), Left (),
                                 Left (), Left (), Left (), Left (), Left (),
                                 Left (), Left (), Left (), Left (), Left (),
                                 Left (), Left (), Left (), Left (), Left ()) ->
                                layout.[x-2].[y+3] <- Right None; layout.[x-1].[y+3] <- Right None; layout.[x+0].[y+3] <- Right None; layout.[x+1].[y+3] <- Right None; layout.[x+2].[y+3] <- Right None
                                layout.[x-2].[y+2] <- Right None; layout.[x-1].[y+2] <- Right None; layout.[x+0].[y+2] <- Right None; layout.[x+1].[y+2] <- Right None; layout.[x+2].[y+2] <- Right None
                                layout.[x-2].[y+1] <- Right None; layout.[x-1].[y+1] <- Right None; layout.[x+0].[y+1] <- Right None; layout.[x+1].[y+1] <- Right None; layout.[x+2].[y+1] <- Right None
                                layout.[x-2].[y+0] <- Right None; layout.[x-1].[y+0] <- Right None; layout.[x+0].[y+0] <- Right (Some (index, stature, enemy)); layout.[x+1].[y+0] <- Right None; layout.[x+2].[y+0] <- Right None
                                layout.[x-2].[y-1] <- Right None; layout.[x-1].[y-1] <- Right None; layout.[x+0].[y-1] <- Right None; layout.[x+1].[y-1] <- Right None; layout.[x+2].[y-1] <- Right None
                            | _ -> tryRandomizeEnemy (inc attempts) index enemy layout
                        else tryRandomizeEnemy (inc attempts) index enemy layout
                | None -> ()
            | None -> ()
        else Log.info ("No enemy fit found for '" + scstring enemy + "' in layout.")

    let private randomizeEnemyLayout w h (enemies : EnemyType list) =
        let layout = Array.init w (fun _ -> Array.init h (fun _ -> Left ()))
        layout.[0].[0] <- Left () // no one puts enemy in a corner
        layout.[w-1].[0] <- Left ()
        layout.[0].[h-1] <- Left ()
        layout.[w-1].[h-1] <- Left ()
        List.iteri (fun index enemy -> tryRandomizeEnemy 0 index enemy layout) enemies
        layout

    let private randomizeEnemies allyCount waitSpeed enemies =
        let origin = v2 -288.0f -240.0f // TODO: turn these duplicated vars into global consts.
        let tile = v2 48.0f 48.0f
        let (w, h) = (10, 8)
        let layout = randomizeEnemyLayout w h enemies
        let enemies =
            layout |>
            Array.mapi (fun x arr ->
                Array.mapi (fun y enemyOpt ->
                    match enemyOpt with
                    | Left () -> None
                    | Right None -> None
                    | Right (Some (enemyIndex, enemyStature, enemy)) ->
                        let position =
                            match enemyStature with
                            | SmallStature | NormalStature | LargeStature -> v3 (origin.X + single x * tile.X) (origin.Y + single y * tile.Y) 0.0f
                            | BossStature -> v3 (origin.X + single x * tile.X - 90.0f) (origin.Y + single y * tile.Y) 0.0f
                        Character.tryMakeEnemy allyCount enemyIndex waitSpeed true position enemy)
                    arr) |>
            Array.concat |>
            Array.definitize |>
            Array.toList
        enemies

    let spawnEnemies spawnTypes battle =
        let origin = v2 -288.0f -240.0f // TODO: turn these duplicated vars into global consts.
        let tile = v2 48.0f 48.0f
        let (w, h) = (10, 8)
        let waitSpeed = battle.BattleSpeed = WaitSpeed
        let allyCount = battle |> getAllies |> Map.count
        let battle =
            List.fold (fun battle (spawnType : SpawnType) ->
                let mutable battle = battle
                let mutable spawned = false
                let mutable tries = 0
                while not spawned && tries < 100 do
                    let (i, j) = (Gen.random1 w, Gen.random1 h)
                    let position = v3 (origin.X + single i * tile.X) (origin.Y + single j * tile.Y) 0.0f
                    let bottom = position + v3 72.0f 0.f 0.0f // HACK: assume spawning character has 144.0f width.
                    let bottoms = battle |> getEnemies |> Map.toValueArray |> Array.map (fun (enemy : Character) -> enemy.PerimeterOriginal.Bottom)
                    let notOnSides = i <> 0 && i <> w - 1
                    let notOverlapping = Array.notExists (fun bottom' -> Vector3.Distance (bottom, bottom') < tile.X * 1.5f) bottoms
                    if notOnSides && notOverlapping then
                        let enemyIndex = Option.mapOrDefaultValue EnemyIndex (nextEnemyIndex battle) spawnType.EnemyIndexOpt
                        let enemyPosition = Option.defaultValue position spawnType.PositionOpt
                        match Character.tryMakeEnemy allyCount enemyIndex.Subindex waitSpeed spawnType.ActionTimeAdvanced enemyPosition spawnType.EnemyType with
                        | Some enemy ->
                            let enemy =
                                match spawnType.SpawnEffectType with
                                | Materialize -> Character.materialize battle.BattleTime enemy
                                | Unearth -> Character.animate battle.BattleTime UnearthAnimation enemy
                                | Pop -> enemy
                            battle <- addCharacter enemyIndex enemy battle
                            spawned <- true
                        | None -> ()
                    tries <- inc tries
                battle)
                battle
                spawnTypes
        battle

    let spawnEnemy spawnType battle =
        spawnEnemies [spawnType] battle

    (* High-Level Operations (signal-producing) *)

    let private updateAttack sourceIndex (targetIndexOpt : CharacterIndex option) localTime battle =
        if getCharacterHealthy sourceIndex battle then
            match targetIndexOpt with
            | Some targetIndex ->
                if containsCharacter targetIndex battle then
                    match localTime with
                    | 0L ->
                        if getCharacterHealthy targetIndex battle then
                            let sourcePerimeter = getCharacterPerimeter sourceIndex battle
                            let targetPerimeter = getCharacterPerimeter targetIndex battle
                            let battle =
                                if sourcePerimeter.Bottom.X < targetPerimeter.Bottom.X then faceCharacter Rightward sourceIndex battle
                                elif sourcePerimeter.Bottom.X > targetPerimeter.Bottom.X then faceCharacter Leftward sourceIndex battle
                                else battle
                            let battle = animateCharacter AttackAnimation sourceIndex battle
                            let playHit = PlaySound (15L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                            withSignal playHit battle
                        else just (abortCharacterInteraction sourceIndex battle)
                    | 15L ->
                        let damage = evalAttack EffectType.Physical sourceIndex targetIndex battle
                        let battle = mapCharacterHitPoints true false false -damage targetIndex battle
                        let battle = animateCharacter DamageAnimation targetIndex battle
                        let battle =
                            if getCharacterWounded targetIndex battle then
                                let battle = halveCharacterActionTime targetIndex battle
                                resetCharacterInput targetIndex battle
                            else battle
                        withSignal (DisplayHitPointsChange (targetIndex, -damage)) battle
                    | _ when localTime > 15L && getCharacterAnimationFinished targetIndex battle ->
                        if getCharacterHealthy targetIndex battle then
                            let battle = animationCharacterPoise sourceIndex battle
                            let battle = animationCharacterPoise targetIndex battle
                            let battle = finishCharacterInteraction sourceIndex battle
                            let battle = { battle with CurrentCommandOpt = None }
                            let battle =
                                if shouldCharacterCounter targetIndex sourceIndex battle
                                then characterCounterAttack targetIndex sourceIndex battle
                                else battle
                            let consequences = evalAttackInteractions sourceIndex targetIndex battle
                            let battle = evalConsequences consequences battle
                            just battle
                        else
                            let woundCommand = CurrentCommand.make battle.BattleTime (ActionCommand.make Wound sourceIndex (Some targetIndex) None)
                            let battle = animationCharacterPoise sourceIndex battle
                            let battle = finishCharacterInteraction sourceIndex battle
                            let battle = { battle with CurrentCommandOpt = Some woundCommand }
                            let consequences = evalAttackInteractions sourceIndex targetIndex battle
                            let battle = evalConsequences consequences battle
                            just battle
                    | _ -> just battle
                else just (abortCharacterInteraction sourceIndex battle)
            | None -> just (abortCharacterInteraction sourceIndex battle)
        else just (abortCharacterInteraction sourceIndex battle)

    let private updateDefend sourceIndex localTime battle =
        if getCharacterHealthy sourceIndex battle then
            match localTime with
            | 0L ->
                let battle =
                    battle |>
                    resetCharacterActionTime sourceIndex |>
                    resetCharacterInput sourceIndex |>
                    animateCharacter (PoiseAnimation Defending) sourceIndex |>
                    defendCharacter sourceIndex
                let battle = finishCharacterInteraction sourceIndex battle
                let battle = { battle with CurrentCommandOpt = None }
                just battle
            | _ -> just battle
        else
            let battle = { battle with CurrentCommandOpt = None }
            just battle

    let private updateConsume consumable sourceIndex (targetIndexOpt : CharacterIndex option) localTime battle =
        if containsCharacterHealthy sourceIndex battle then
            match targetIndexOpt with
            | Some targetIndex ->
                if containsCharacter targetIndex battle then
                    match localTime with
                    | 0L ->
                        if getCharacterHealthy targetIndex battle || consumable = Revive then // HACK: should really be checked ConsumableData.
                            let sourcePerimeter = getCharacterPerimeter sourceIndex battle
                            let targetPerimeter = getCharacterPerimeter targetIndex battle
                            let battle =
                                if sourcePerimeter.Bottom.X < targetPerimeter.Bottom.X then faceCharacter Rightward sourceIndex battle
                                elif sourcePerimeter.Bottom.X > targetPerimeter.Bottom.X then faceCharacter Leftward sourceIndex battle
                                else battle
                            let battle = animateCharacter CastAnimation sourceIndex battle
                            let battle = { battle with Inventory = Inventory.tryRemoveItem (Consumable consumable) battle.Inventory |> snd }
                            just battle
                        else just (abortCharacterInteraction sourceIndex battle)
                    | 30L ->
                        match Data.Value.Consumables.TryGetValue consumable with
                        | (true, consumableData) ->
                            if consumableData.Curative then
                                let healing = int consumableData.Scalar
                                let battle =
                                    if consumableData.Techative
                                    then mapCharacterTechPoints healing targetIndex battle
                                    else mapCharacterHitPoints true false consumableData.Revive healing targetIndex battle
                                let battle = applyCharacterStatuses consumableData.StatusesAdded consumableData.StatusesRemoved targetIndex battle
                                let battle = animateCharacter SpinAnimation targetIndex battle
                                let displayHitPointsChange = DisplayHitPointsChange (targetIndex, healing)
                                let playHealSound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.HealSound)
                                withSignals [displayHitPointsChange; playHealSound] battle
                            else just battle // TODO: non-curative case
                        | (false, _) -> just battle
                    | _ when localTime > 30L && getCharacterAnimationFinished targetIndex battle ->
                        let battle = animationCharacterPoise sourceIndex battle
                        let battle = animationCharacterPoise targetIndex battle
                        let battle = finishCharacterInteraction sourceIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        let consequences = evalItemInteractions sourceIndex targetIndex battle
                        let battle = evalConsequences consequences battle
                        just battle
                    | _ -> just battle
                else just (abortCharacterInteraction sourceIndex battle)
            | None -> just (abortCharacterInteraction sourceIndex battle)
        else just (abortCharacterInteraction sourceIndex battle)

    let private updateMessage text lifeTime localTime battle =
        ignore<int64> localTime
        let dialog = Dialog.make DialogShort text
        let battle = { battle with MessageOpt = Some (battle.BattleTime, lifeTime, dialog) }
        let battle = { battle with CurrentCommandOpt = None }
        just battle

    let private updateTech techType sourceIndex (targetIndexOpt : CharacterIndex option) localTime battle =
        if containsCharacterHealthy sourceIndex battle then
            match targetIndexOpt with
            | Some targetIndex ->
                if containsCharacter targetIndex battle then
                    match (Map.tryFind techType Data.Value.Techs, Map.tryFind techType Data.Value.TechAnimations) with
                    | (Some techData, Some techAnimationData) ->
                        ignore techData // TODO: check for target.IsWounded case if techData is affecting wounded...
                        if getCharacterHealthy targetIndex battle then
                            let (sigs, battle) =
                                if localTime = techAnimationData.TechStart then
                                    let sourcePerimeter = getCharacterPerimeter sourceIndex battle
                                    let targetPerimeter = getCharacterPerimeter targetIndex battle
                                    let battle =
                                        if sourcePerimeter.Bottom.X < targetPerimeter.Bottom.X then faceCharacter Rightward sourceIndex battle
                                        elif sourcePerimeter.Bottom.X > targetPerimeter.Bottom.X then faceCharacter Leftward sourceIndex battle
                                        else battle
                                    let effectOpt =
                                        match techType with
                                        | Cyclone ->
                                            Left (DisplayHop (sourcePerimeter.Bottom, targetPerimeter.BottomOffset3))
                                        | _ when techType.TouchingTech ->
                                            let hopDirection = Direction.ofVector3 (v3 (targetPerimeter.Bottom.X - sourcePerimeter.Bottom.X) 0.0f 0.0f)
                                            let hopStop = targetPerimeter.Bottom - Direction.toVector3 hopDirection * Constants.Battle.StrikingDistance
                                            Left (DisplayHop (sourcePerimeter.Bottom, hopStop))
                                        | _ ->
                                            match getCharacterArchetypeType sourceIndex battle with
                                            | Cleric ->
                                                let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeHolySound)
                                                let displayCast = DisplayHolyCast (0L, sourceIndex)
                                                Right [signal playCharge; signal displayCast]
                                            | Wizard ->
                                                let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeDimensionSound)
                                                let displayCast = DisplayArcaneCast (0L, sourceIndex)
                                                Right [playCharge; displayCast]
                                            | Conjuror ->
                                                let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeDimensionSound)
                                                let displayCast = DisplayDimensionalCast (0L, sourceIndex)
                                                Right [playCharge; displayCast]
                                            | _ ->
                                                let playCharge = PlaySound (0L, Constants.Audio.SongVolumeDefault, Assets.Field.ChargeDimensionSound)
                                                let displayCast = DisplayGenericCast (0L, sourceIndex)
                                                Right [playCharge; displayCast]
                                    match effectOpt with
                                    | Left hopEffect ->
                                        let battle = animateCharacter (PoiseAnimation Poising) sourceIndex battle
                                        withSignal hopEffect battle
                                    | Right chargeEffects ->
                                        if getCharacterHealthy targetIndex battle then
                                            let battle = animateCharacter (PoiseAnimation Charging) sourceIndex battle
                                            withSignals chargeEffects battle
                                        else just (abortCharacterInteraction sourceIndex battle)
                                elif localTime = techAnimationData.TechingStart then
                                    match techType with
                                    | Critical ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let critical = DisplayCritical (13L, targetIndex)
                                        let displayCut = DisplayCut (20L, true, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; critical; displayCut] battle
                                    | Slash ->
                                        let playSlash = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.SlashSound)
                                        let playHit = PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let perimeter = getCharacterPerimeter sourceIndex battle
                                        let slashSpike = DisplaySlashSpike (10L, perimeter.Bottom, targetIndex)
                                        let impactSplashes = evalTech sourceIndex targetIndex techType battle |> Triple.thd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayImpactSplash (70L, targetIndex) |> signal)
                                        let battle = animateCharacter SlashAnimation sourceIndex battle
                                        withSignals (playSlash :: playHit :: slashSpike :: impactSplashes) battle
                                    | HeavyCritical ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let heavyCritical = DisplayHeavyCritical (10L, targetIndex)
                                        let displayCut = DisplayCut (20L, true, targetIndex)
                                        let impactSplash = DisplayImpactSplash (34L, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; heavyCritical; displayCut; impactSplash] battle
                                    | Cyclone ->
                                        let radius = 64.0f
                                        let perimeter = getCharacterPerimeter sourceIndex battle
                                        let position = perimeter.Bottom
                                        let playHits =
                                            [PlaySound (20L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound) |> signal
                                             PlaySound (40L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                             PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                             PlaySound (80L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)]
                                        let sigs =
                                            signal (DisplayCircle (position, radius)) ::
                                            signal (DisplayCycloneBlur (0L, sourceIndex, radius)) ::
                                            playHits
                                        let battle = animateCharacter WhirlAnimation sourceIndex battle
                                        withSignals sigs battle
                                    | CriticalSlash ->
                                        let playSlash = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.TwisterSound)
                                        let playHit = PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let perimeter = getCharacterPerimeter sourceIndex battle
                                        let slashTwister = DisplaySlashTwister (10L, perimeter.Bottom, targetIndex)
                                        let impactSplashes = evalTech sourceIndex targetIndex techType battle |> Triple.thd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayImpactSplash (70L, targetIndex) |> signal)
                                        let battle = animateCharacter SlashAnimation sourceIndex battle
                                        withSignals (playSlash :: playHit :: slashTwister :: impactSplashes) battle
                                    | PoisonCut ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let displayCut = DisplayCut (20L, false, targetIndex)
                                        let poisonCut = DisplayPoisonCut (25L, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; displayCut; poisonCut] battle
                                    | PowerCut ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let displayCut = DisplayCut (20L, false, targetIndex)
                                        let powerCut = DisplayPowerCut (20L, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; displayCut; powerCut] battle
                                    | DispelCut ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let displayCut = DisplayCut (20L, false, targetIndex)
                                        let dispelCut = DisplayDispelCut (25L, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; displayCut; dispelCut] battle
                                    | DoubleCut ->
                                        let playHit = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let playHit2 = PlaySound (20L, Constants.Audio.SoundVolumeDefault, Assets.Field.HitSound)
                                        let displayCut = DisplayCut (20L, false, targetIndex)
                                        let doubleCut = DisplayDoubleCut (20L, targetIndex)
                                        let battle = animateCharacter AttackAnimation sourceIndex battle
                                        withSignals [playHit; playHit2; displayCut; doubleCut] battle
                                    | Fire ->
                                        let playFire = PlaySound (60L, Constants.Audio.SoundVolumeDefault, Assets.Field.FireSound)
                                        let displayFire = DisplayFire (0L, sourceIndex, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playFire; displayFire] battle
                                    | TechType.Flame ->
                                        let playFlame = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.FlameSound)
                                        let displayFlame = DisplayFlame (0L, sourceIndex, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playFlame; displayFlame] battle
                                    | Ice ->
                                        let playIce = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.IceSound)
                                        let displayIce = DisplayIce (0L, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playIce; displayIce] battle
                                    | Snowball ->
                                        let playSnowball = PlaySound (15L, Constants.Audio.SoundVolumeDefault, Assets.Field.SnowballSound)
                                        let displaySnowball = DisplaySnowball (0L, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playSnowball; displaySnowball] battle
                                    | Cure ->
                                        let playCure = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.CureSound)
                                        let displayCures = evalTech sourceIndex targetIndex techType battle |> Triple.thd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayCure (0L, targetIndex) |> signal)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals (signal playCure :: displayCures) battle
                                    | Empower ->
                                        let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                                        let displayBuff = DisplayBuff (0L, Power (true, true), targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playBuff; displayBuff] battle
                                    | Aura ->
                                        let playCure = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.CureSound)
                                        let displayCures = evalTech sourceIndex targetIndex techType battle |> Triple.thd |> Map.toKeyList |> List.map (fun targetIndex -> DisplayCure (0L, targetIndex) |> signal)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals (signal playCure :: displayCures) battle
                                    | Enlighten ->
                                        let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                                        let displayBuff = DisplayBuff (0L, Magic (true, true), targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playBuff; displayBuff] battle
                                    | Protect ->
                                        let playBuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BuffSound)
                                        let displayBuff = DisplayBuff (0L, Shield (true, true), targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playBuff; displayBuff] battle
                                    | Purify ->
                                        let displayPurify = DisplayPurify (0L, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignal displayPurify battle
                                    | Muddle ->
                                        let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                                        let displayDebuff = DisplayDebuff (0L, Magic (false, false), targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playDebuff; displayDebuff] battle
                                    | Weaken ->
                                        let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                                        let displayDebuff = DisplayDebuff (0L, Power (false, false), targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playDebuff; displayDebuff] battle
                                    | Slow ->
                                        let playDebuff = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DebuffSound)
                                        let displayDebuff = DisplayDebuff (0L, Time false, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playDebuff; displayDebuff] battle
                                    | Bolt ->
                                        let playSound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.ExplosionSound)
                                        let displayBolt = DisplayBolt (0L, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playSound; displayBolt] battle
                                    | ConjureRamuh ->
                                        let playThunders =
                                            [PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.ThunderSound) |> signal
                                             PlaySound (15L, Constants.Audio.SoundVolumeDefault, Assets.Field.ExplosionSound)
                                             PlaySound (30L, Constants.Audio.SoundVolumeDefault, Assets.Field.ExplosionSound)
                                             PlaySound (45L, Constants.Audio.SoundVolumeDefault, Assets.Field.ExplosionSound)]
                                        let displayScatterBolts =
                                            [for i in 0L .. 15L .. 45L do
                                                for _ in 0L .. dec 2L do DisplayScatterBolt i |> signal]
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals (playThunders @ displayScatterBolts) battle
                                    | Inferno ->
                                        let playInferno = PlaySound (10L, Constants.Audio.SoundVolumeDefault, Assets.Field.InfernoSound)
                                        let displayInferno = DisplayInferno 0L
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playInferno; displayInferno] battle
                                    | Silk ->
                                        let playSilk = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.SilkSound)
                                        let displaySilk = DisplaySilk (0L, targetIndex)
                                        let battle = animateCharacter Cast2Animation sourceIndex battle
                                        withSignals [playSilk; displaySilk] battle
                                elif localTime = techAnimationData.AffectingStart then
                                    let (_, spawnOpt, results) = evalTech sourceIndex targetIndex techType battle
                                    let (battle, sigs) =
                                        Map.fold (fun (battle, sigs) characterIndex (cancelled, _, hitPointsChange, _, _) ->
                                            if hitPointsChange < 0 && getCharacterHealthy characterIndex battle then
                                                let battle = animateCharacter DamageAnimation characterIndex battle
                                                let displayCancel = DisplayCancel characterIndex
                                                let sigs = if cancelled then signal displayCancel :: sigs else sigs
                                                (battle, sigs)
                                            else (battle, sigs))
                                            (battle, [])
                                            results
                                    let battle =
                                        match spawnOpt with
                                        | Some spawn -> spawnEnemies spawn battle
                                        | _ -> battle
                                    withSignals sigs battle
                                elif localTime = techAnimationData.AffectingStop then
                                    let results = evalTech sourceIndex targetIndex techType battle |> Triple.thd
                                    let (battle, sigs) =
                                        Map.fold (fun (battle, sigs) _ (_, _, _, _, _) ->
                                            // TODO: emission effect
                                            (battle, sigs))
                                            (battle, [])
                                            results
                                    withSignals sigs battle
                                elif localTime = techAnimationData.TechingStop then
                                    let sourcePerimeterOriginal = getCharacterPerimeterOriginal sourceIndex battle
                                    let targetPerimeter = getCharacterPerimeter targetIndex battle
                                    let hopOpt =
                                        match techType with
                                        | Cyclone ->
                                            Some (targetPerimeter.BottomOffset3, sourcePerimeterOriginal.Bottom)
                                        | _ when techType.TouchingTech ->
                                            let hopDirection = Direction.ofVector3 (v3 (targetPerimeter.Bottom.X - sourcePerimeterOriginal.Bottom.X) 0.0f 0.0f)
                                            let hopStart = targetPerimeter.Bottom - Direction.toVector3 hopDirection * Constants.Battle.StrikingDistance
                                            Some (hopStart, sourcePerimeterOriginal.Bottom)
                                        | _ -> None
                                    match hopOpt with
                                    | Some (hopStart, hopStop) -> withSignal (DisplayHop (hopStart, hopStop)) battle
                                    | None -> just battle
                                elif localTime > techAnimationData.TechStop then
                                    let battle = if techData.SpawnOpt.IsSome then finalizeMaterializations battle else battle
                                    let (techCost, _, results) = evalTech sourceIndex targetIndex techType battle
                                    let source = getCharacter sourceIndex battle
                                    let (battle, sigs) =
                                        Map.fold (fun (battle, sigs) characterIndex (cancelled, affectsWounded, hitPointsChange, added, removed) ->
                                            let battle = mapCharacterHitPoints true cancelled affectsWounded hitPointsChange characterIndex battle
                                            let vulnerabilities = getCharacterVulnerabilities characterIndex battle
                                            let randomizer = if sourceIndex.Ally then StatusType.randomizeStrong vulnerabilities else StatusType.randomizeWeak vulnerabilities
                                            let added = added |> Set.toSeq |> Seq.filter randomizer |> Set.ofSeq
                                            let battle = applyCharacterStatuses added removed characterIndex battle
                                            let wounded = getCharacterWounded characterIndex battle
                                            let battle =
                                                if wounded then
                                                    let battle = halveCharacterActionTime characterIndex battle
                                                    resetCharacterInput characterIndex battle
                                                else battle
                                            let sigs =
                                                if hitPointsChange <> 0 then
                                                    let displayHpc = DisplayHitPointsChange (characterIndex, hitPointsChange) |> signal
                                                    displayHpc :: sigs
                                                else sigs
                                            let (battle, sigs) =
                                                if wounded then
                                                    let woundCommand = ActionCommand.make Wound sourceIndex (Some characterIndex) None
                                                    let battle = prependActionCommand woundCommand battle
                                                    (battle, sigs)
                                                else
                                                    let battle = animationCharacterPoise characterIndex battle
                                                    (battle, sigs)
                                            (battle, sigs))
                                            (battle, [])
                                            results
                                    let battle = mapCharacterTechPoints -techCost sourceIndex battle
                                    let battle = animationCharacterPoise sourceIndex battle
                                    let battle =
                                        match source.TechChargeOpt with
                                        | Some (_, _, chargeTechType) when techType = chargeTechType -> resetCharacterTechCharge sourceIndex battle
                                        | Some _ | None -> battle
                                    let battle =
                                        if techType.ConjureTech
                                        then resetCharacterConjureCharge sourceIndex battle
                                        else battle
                                    let battle = finishCharacterInteraction sourceIndex battle
                                    let battle = { battle with CurrentCommandOpt = None }
                                    let battle =
                                        if shouldCharacterCounter targetIndex sourceIndex battle
                                        then characterCounterAttack targetIndex sourceIndex battle
                                        else battle
                                    let consequences = evalTechInteractions sourceIndex targetIndex techType results battle
                                    let battle = evalConsequences consequences battle
                                    withSignals sigs battle
                                else just battle
                            withSignals sigs battle
                        else just (abortCharacterInteraction sourceIndex battle)
                    | (_, _) -> just (abortCharacterInteraction sourceIndex battle)
                else just (abortCharacterInteraction sourceIndex battle)
            | None -> just (abortCharacterInteraction sourceIndex battle)
        else just (abortCharacterInteraction sourceIndex battle)

    let private updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt consequence messageOpt localTime battle =
        let messageTime = 45L
        match messageOpt with
        | Some message ->
            if localTime = 0L then
                let dialog = Dialog.make DialogShort message
                let battle = { battle with MessageOpt = Some (battle.BattleTime, dec messageTime, dialog) }
                (false, battle)
            else
                let actionCommand = { Action = Consequence consequence; SourceIndex = sourceIndex; TargetIndexOpt = targetIndexOpt; ObserverIndexOpt = observerIndexOpt }
                let currentCommand = { StartTime = battle.BattleTime; ActionCommand = actionCommand }
                let battle = { battle with CurrentCommandOpt = Some currentCommand }
                (false, battle)
        | None -> (true, battle)

    let private updateConsequence sourceIndex targetIndexOpt observerIndexOpt consequence localTime (battle : Battle) =
        match (targetIndexOpt, observerIndexOpt) with
        | (Some targetIndex, Some observerIndex) ->
            match consequence with
            | Charge (chargeAmount, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (Charge (chargeAmount, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = chargeCharacter chargeAmount observerIndex battle
                        let battle =
                            match getCharacterBy (fun c -> c.TechChargeOpt) observerIndex battle with
                            | Some (_, chargeAmount, techType) when chargeAmount >= Constants.Battle.ChargeMax ->
                                let battle = mapCharacterAutoTechOpt (constant (Some techType)) observerIndex battle
                                let battle = retargetCharacter observerIndex sourceIndex battle // TODO: make this target self if healing tech.
                                battle
                            | Some _ | None -> battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | AddVulnerability (vulnerabilityType, vulnerabilityRank, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (AddVulnerability (vulnerabilityType, vulnerabilityRank, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = applyCharacterVulnerabilities (Map.singleton vulnerabilityType vulnerabilityRank) Set.empty observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | RemoveVulnerability (vulnerabilityType, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (RemoveVulnerability (vulnerabilityType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = applyCharacterVulnerabilities Map.empty (Set.singleton vulnerabilityType) observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | AddStatus (statusType, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (AddStatus (statusType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = applyCharacterStatuses (Set.singleton statusType) Set.empty observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | RemoveStatus (statusType, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (RemoveStatus (statusType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = applyCharacterStatuses Set.empty (Set.singleton statusType) observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | CounterAttack messageOpt ->
                if containsCharacterHealthy sourceIndex battle && containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (CounterAttack None) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = prependActionCommand (ActionCommand.make Attack observerIndex (Some sourceIndex) None) battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | CounterTech (techType, messageOpt) ->
                if containsCharacterHealthy sourceIndex battle && containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) && not ((getCharacter observerIndex battle).Statuses.ContainsKey Silence) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (CounterTech (techType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = prependActionCommand (ActionCommand.make (Tech techType) observerIndex (Some sourceIndex) None) battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | CounterConsumable (consumableType, messageOpt) ->
                if containsCharacterHealthy sourceIndex battle && containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (CounterConsumable (consumableType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = prependActionCommand (ActionCommand.make (Consume consumableType) observerIndex (Some sourceIndex) None) battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | AssistTech (techType, messageOpt) ->
                if containsCharacterHealthy targetIndex battle && containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (AssistTech (techType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = prependActionCommand (ActionCommand.make (Tech techType) observerIndex (Some targetIndex) None) battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | AssistConsumable (consumableType, messageOpt) ->
                if containsCharacterHealthy targetIndex battle && containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (AssistConsumable (consumableType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = prependActionCommand (ActionCommand.make (Consume consumableType) observerIndex (Some targetIndex) None) battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | PilferGold (gold, messageOpt) ->
                if containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (PilferGold (gold, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = { battle with Inventory = Inventory.removeGold gold battle.Inventory }
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | PilferConsumable (consumableType, messageOpt) ->
                if containsCharacterHealthy observerIndex battle && not ((getCharacter observerIndex battle).Statuses.ContainsKey Sleep) then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (PilferConsumable (consumableType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = { battle with Inventory = Inventory.tryRemoveItem (Consumable consumableType) battle.Inventory |> snd }
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | RetargetToSource messageOpt ->
                if containsCharacterHealthy sourceIndex battle && containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (RetargetToSource None) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = retargetCharacter observerIndex sourceIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | RetargetFriendliesToSource messageOpt ->
                if containsCharacterHealthy sourceIndex battle && containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (RetargetFriendliesToSource None) messageOpt localTime battle with
                    | (true, battle) ->
                        let friendlies = getFriendlies observerIndex.Ally battle
                        let battle = Map.fold (fun battle friendlyIndex _ -> retargetCharacter friendlyIndex sourceIndex battle) battle friendlies
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | ChangeAction (techTypeOpt, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (ChangeAction (techTypeOpt, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = mapCharacterAutoTechOpt (constant techTypeOpt) observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | ChangeFriendlyActions (techTypeOpt, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (ChangeFriendlyActions (techTypeOpt, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let friendlies = getFriendlies observerIndex.Ally battle
                        let battle = Map.fold (fun battle friendlyIndex _ -> mapCharacterAutoTechOpt (constant techTypeOpt) friendlyIndex battle) battle friendlies
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | Duplicate messageOpt ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (Duplicate None) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle =
                            match (getCharacter observerIndex battle).CharacterType with
                            | Enemy enemyType -> spawnEnemies [{ EnemyType = enemyType; SpawnEffectType = Materialize; ActionTimeAdvanced = false; PositionOpt = None; EnemyIndexOpt = None }] battle
                            | Ally _ -> battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | AddBattleInteraction (interaction, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (AddBattleInteraction (interaction, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = addCharacterInteraction interaction observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | ClearBattleInteractions messageOpt ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (ClearBattleInteractions None) messageOpt localTime battle with
                    | (true, battle) ->
                        let battle = clearCharacterInteractions observerIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | Replace (enemyType, messageOpt) ->
                if containsCharacterHealthy observerIndex battle then
                    match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (Replace (enemyType, None)) messageOpt localTime battle with
                    | (true, battle) ->
                        if localTime = 1L then // HACK: for some reason we can't get this on frame 0...
                            let battle = animateCharacter ReadyAnimation observerIndex battle
                            let battle = dematerializeCharacter observerIndex battle
                            just battle
                        elif localTime = Constants.Battle.CharacterDematerializeDuration then
                            let spawnPosition = (getCharacterPerimeter observerIndex battle).BottomLeft
                            let spawnType = { EnemyType = enemyType; SpawnEffectType = Materialize; ActionTimeAdvanced = false; PositionOpt = Some spawnPosition; EnemyIndexOpt = Some observerIndex.Subindex }
                            let battle = removeCharacter observerIndex battle
                            let battle = spawnEnemy spawnType battle
                            let battle = animateCharacter WalkAnimation observerIndex battle
                            let battle = faceCharacter Downward observerIndex battle
                            just battle
                        elif localTime = Constants.Battle.CharacterDematerializeDuration + Constants.Battle.CharacterMaterializeDuration then
                            let battle = animateCharacter (PoiseAnimation Poising) observerIndex battle
                            let battle = { battle with CurrentCommandOpt = None }
                            just battle
                        else just battle
                    | (false, battle) -> just battle
                else just ({ battle with CurrentCommandOpt = None })
            | Spawn (spawnTypes, messageOpt) ->
                match updateConsequenceMessageOpt sourceIndex targetIndexOpt observerIndexOpt (Spawn (spawnTypes, None)) messageOpt localTime battle with
                | (true, battle) ->
                    if localTime = 1L then // HACK: for some reason we can't get this on frame 0...
                        let battle = spawnEnemies spawnTypes battle
                        just battle
                    elif localTime = Constants.Battle.CharacterMaterializeDuration then
                        let battle = finalizeMaterializations battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    else just battle
                | (false, battle) -> just battle
            | Message (text, lifeTime) ->
                let battle =
                    if containsCharacterHealthy observerIndex battle then 
                        let lifeTime = if lifeTime <= 0L then 60L else lifeTime
                        let dialog = Dialog.make DialogShort text
                        { battle with MessageOpt = Some (battle.BattleTime, lifeTime, dialog) }
                    else battle
                let battle = { battle with CurrentCommandOpt = None }
                just battle
        | (_, _) -> { battle with CurrentCommandOpt = None } |> just

    let rec private updateWound targetIndexOpt battle =
        match targetIndexOpt with
        | Some targetIndex ->
            let character = getCharacter targetIndex battle
            let (sigs, battle) =
                if character.Ally then
                    match character.CharacterAnimationType with
                    | DamageAnimation ->
                        if Character.getAnimationFinished battle.BattleTime character then
                            let battle = animateCharacterWound targetIndex battle
                            let battle = { battle with CurrentCommandOpt = None }
                            just battle
                        else just battle
                    | PoiseAnimation _ -> // allies don't have a wound animation state but rather return to poise state
                        let battle = animateCharacterWound targetIndex battle
                        let battle = { battle with CurrentCommandOpt = None }
                        just battle
                    | _ -> failwithumf ()
                else
                    match character.CharacterAnimationType with
                    | DamageAnimation ->
                        if Character.getAnimationFinished battle.BattleTime character then
                            let battle = animateCharacterWound targetIndex battle
                            let playDeathSound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BeastDeathSound)
                            withSignal playDeathSound battle
                        else just battle
                    | WoundAnimation ->
                        if Character.getAnimationFinished battle.BattleTime character then
                            let battle = if targetIndex.Enemy then removeCharacter targetIndex battle else battle
                            let battle = { battle with CurrentCommandOpt = None }
                            just battle
                        else just battle
                    | _ -> failwithumf ()
            let (sigs, battle) =
                match battle.CurrentCommandOpt with
                | None ->
                    let allies = battle |> getAllies |> Map.toValueList
                    let enemies = battle |> getEnemies |> Map.toValueList
                    if List.forall (fun (character : Character) -> character.Wounded) allies then
                        // lost battle
                        let battle = animateCharactersCelebrate false battle
                        let battle = { battle with BattleState = BattleConcluding (battle.BattleTime, false) }
                        let (sigs2, battle) = update battle
                        (sigs @ sigs2, battle)
                    elif List.isEmpty enemies then
                        // won battle
                        let battle = animateCharactersCelebrate true battle
                        let battle = { battle with BattleState = BattleResult (battle.BattleTime, true) }
                        let (sigs2, battle) = update battle
                        (sigs @ sigs2, battle)
                    else (sigs, battle)
                | Some _ -> (sigs, battle)
            withSignals sigs battle
        | None -> just battle

    and private updateReadying startTime (battle : Battle) =
        let localTime = battle.BattleTime - startTime
        if localTime = 0L then // first frame after transitioning in
            match battle.BattleSongOpt with
            | Some battleSong -> withSignal (PlaySong (Constants.Audio.FadeOutTimeDefault, 0L, 0L, 0.5f, battleSong)) battle
            | None -> just battle
        elif localTime >= 30L && localTime < 100L then
            let battle = animateCharactersReady battle
            if localTime = 60L
            then withSignal (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.UnsheatheSound)) battle
            else just battle
        elif localTime = 100L then
            let battle = { battle with BattleState = BattleRunning }
            let battle = animateCharactersPoised battle
            let battle = populateAlliesConjureCharges battle
            let battle = autoBattleEnemies battle
            just battle
        else just battle

    and private updateCurrentCommand currentCommand battle =
        let localTime = battle.BattleTime - currentCommand.StartTime
        let sourceIndex = currentCommand.ActionCommand.SourceIndex
        let targetIndexOpt = currentCommand.ActionCommand.TargetIndexOpt
        let observerIndexOpt = currentCommand.ActionCommand.ObserverIndexOpt
        match currentCommand.ActionCommand.Action with
        | Attack -> updateAttack sourceIndex targetIndexOpt localTime battle
        | Defend -> updateDefend sourceIndex localTime battle
        | Tech techType -> updateTech techType sourceIndex targetIndexOpt localTime battle
        | Consume consumable -> updateConsume consumable sourceIndex targetIndexOpt localTime battle
        | Consequence consequence -> updateConsequence sourceIndex targetIndexOpt observerIndexOpt consequence localTime battle
        | ActionType.Message (text, lifeTime) -> updateMessage text lifeTime localTime battle
        | Wound -> updateWound targetIndexOpt battle

    and private updateNextCommand nextCommand futureCommands battle =
        let command = CurrentCommand.make battle.BattleTime nextCommand
        let sourceIndex = command.ActionCommand.SourceIndex
        let targetIndexOpt = command.ActionCommand.TargetIndexOpt
        let observerIndexOpt = command.ActionCommand.ObserverIndexOpt
        let source = getCharacter sourceIndex battle
        let battle =
            match command.ActionCommand.Action with
            | Attack | Defend ->
                if source.Healthy && not (Map.containsKey Sleep source.Statuses) then
                    let targetIndexOpt = evalRetarget false targetIndexOpt battle
                    let command = { command with ActionCommand = { command.ActionCommand with TargetIndexOpt = targetIndexOpt }}
                    { battle with CurrentCommandOpt = Some command }
                else battle
            | Consume consumableType ->
                match Data.Value.Consumables.TryGetValue consumableType with
                | (true, consumable) ->
                    if source.Healthy && not (Map.containsKey Sleep source.Statuses) then
                        let targetIndexOpt = evalRetarget consumable.Revive targetIndexOpt battle
                        let command = { command with ActionCommand = { command.ActionCommand with TargetIndexOpt = targetIndexOpt }}
                        { battle with CurrentCommandOpt = Some command }
                    else battle
                | (false, _) -> battle
            | Tech techType ->
                match Data.Value.Techs.TryGetValue techType with
                | (true, _) ->
                    if  source.Healthy &&
                        not (Map.containsKey Sleep source.Statuses) &&
                        (not (Map.containsKey Silence source.Statuses) || // NOTE: silence only blocks non-enemy, non-charge techs.
                         source.Enemy && match source.TechChargeOpt with Some (_, chargeAmount, _) -> chargeAmount >= Constants.Battle.ChargeMax | _ -> false) then
                        let targetIndexOpt = evalRetarget false targetIndexOpt battle // TODO: consider affecting wounded, such as for Revive tech.
                        let command = { command with ActionCommand = { command.ActionCommand with TargetIndexOpt = targetIndexOpt }}
                        { battle with CurrentCommandOpt = Some command }
                    else battle
                | (false, _) -> battle
            | Consequence consequence ->
                match observerIndexOpt with
                | Some observerIndex ->
                    let observerOpt = tryGetCharacter observerIndex battle
                    match (consequence, observerOpt) with
                    | (Spawn _, _) ->
                        let command = { command with ActionCommand = { command.ActionCommand with TargetIndexOpt = targetIndexOpt }}
                        { battle with CurrentCommandOpt = Some command }
                    | (_, Some observer) when observer.Healthy ->
                        let command = { command with ActionCommand = { command.ActionCommand with TargetIndexOpt = targetIndexOpt }}
                        { battle with CurrentCommandOpt = Some command }
                    | (_, _) -> battle
                | None -> battle
            | ActionType.Message (_, _) ->
                { battle with CurrentCommandOpt = Some command }
            | Wound ->
                { battle with CurrentCommandOpt = Some command }
        let battle = { battle with ActionCommands = futureCommands }
        update battle

    and private updateNoNextCommand battle =
        let (allySignalsRev, battle) =
            Map.fold (fun (signals : Signal list, battle) allyIndex (ally : Character) ->
                if  ally.ActionTime >= Constants.Battle.ActionTime &&
                    ally.CharacterInputState = NoInput then
                    let battle = mapCharacterInputState (constant RegularMenu) allyIndex battle
                    let playReadySound = PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.ReadySound)
                    (signal playReadySound :: signals, battle)
                else (signals, battle))
                (just battle)
                (getAllies battle)
        let battle =
            Map.fold (fun battle enemyIndex (enemy : Character) ->
                if  enemy.ActionTime >= Constants.Battle.ActionTime &&
                    not (getCharacterAppendedActionCommand enemyIndex battle) then
                    let battle =
                        match enemy.AutoBattleOpt with
                        | Some autoBattle ->
                            let actionCommand =
                                match autoBattle.AutoTechOpt with
                                | Some tech -> ActionCommand.make (Tech tech) enemyIndex (Some autoBattle.AutoTarget) None
                                | None -> ActionCommand.make Attack enemyIndex (Some autoBattle.AutoTarget) None
                            appendActionCommand actionCommand battle
                        | None -> battle    
                    let battle = resetCharacterActionTime enemyIndex battle
                    let battle = resetCharacterInput enemyIndex battle
                    battle
                else battle)
                battle
                (getEnemies battle)
        let battle =
            mapCharacters (fun character ->
                let actionTimeDelta =
                    if character.Ally || battle.BattleSpeed = WaitSpeed
                    then Constants.Battle.AllyActionTimeDelta
                    else Constants.Battle.EnemyActionTimeDelta
                let actionTimeDelta =
                    match Map.tryFindKey (function Time _ -> constant true | _ -> constant false) character.Statuses with
                    | Some (Time false) ->
                        let slowScalar =
                            if character.Ally then Constants.Battle.ActionTimeSlowScalar
                            elif character.Boss then Constants.Battle.ActionTimeSlowerScalar
                            else Constants.Battle.ActionTimeSlowestScalar
                        actionTimeDelta * slowScalar
                    | Some (Time true) -> actionTimeDelta * Constants.Battle.ActionTimeHasteScalar
                    | Some _ | None -> actionTimeDelta
                let actionTimeDelta =
                    let anyAlliesInputting = getAlliesHealthy battle |> Map.toValueList |> List.exists (fun ally -> ally.CharacterInputState <> CharacterInputState.NoInput)
                    if anyAlliesInputting then
                        match battle.BattleSpeed with
                        | SwiftSpeed -> actionTimeDelta * Constants.Battle.SwiftSpeedScalar
                        | PacedSpeed -> actionTimeDelta * Constants.Battle.PacedSpeedScalar
                        | WaitSpeed -> 0.0f
                    else actionTimeDelta * 1.0f
                let poisoning =
                    let actionTime = character.ActionTime + actionTimeDelta
                    Map.containsKey Poison character.Statuses &&
                    character.ActionTime % 500.0f < 250.0f &&
                    actionTime % 500.0f >= 250.0f
                let character =
                    if character.Healthy && not (Map.containsKey Sleep character.Statuses)
                    then Character.mapActionTime ((+) actionTimeDelta) character
                    else character
                let character =
                    if character.Healthy
                    then Character.burndownStatuses actionTimeDelta character
                    else character
                let character =
                    if character.Healthy && poisoning then
                        let poisonDrainRate =
                            if character.Ally then Constants.Battle.PoisonDrainRateMedium
                            elif character.Boss then Constants.Battle.PoisonDrainRateSlow
                            else Constants.Battle.PoisonDrainRateFast
                        let damage = single character.HitPointsMax * poisonDrainRate |> max 1.0f |> int
                        let alliesHealthy = getAlliesHealthy battle
                        Character.mapHitPoints (fun hp -> (false, max 1 (hp - damage))) false alliesHealthy character
                    else character
                let character =
                    if character.Healthy && Character.readyForAutoBattle character then
                        let jinnInParty = getJinnInParty battle
                        let alliesHealthy = getAlliesHealthy battle
                        let alliesWounded = getAlliesWounded battle
                        let enemiesStanding = getEnemiesStanding battle
                        let enemiesSwooning = getEnemiesSwooning battle
                        Character.autoBattle jinnInParty alliesHealthy alliesWounded enemiesStanding enemiesSwooning character
                    else character
                character)
                battle
        withSignals (List.rev allySignalsRev) battle

    and private updateNoCurrentCommand (battle : Battle) =
        match battle.ActionCommands with
        | FQueue.Cons (nextCommand, futureCommands) -> updateNextCommand nextCommand futureCommands battle
        | FQueue.Nil -> updateNoNextCommand battle

    and private updateRunning (battle : Battle) =
        if battle.MessageOpt.IsNone then
            match battle.CurrentCommandOpt with
            | Some currentCommand -> updateCurrentCommand currentCommand battle
            | None -> updateNoCurrentCommand battle
        else just battle

    and private updateResult startTime outcome (battle : Battle) =
        let localTime = battle.BattleTime - startTime
        if localTime = 0L then
            let alliesLevelingUp =
                battle |> getAllies |> Map.toValueList |>
                List.filter (fun ally -> ally.HitPoints > 0) |>
                List.filter (fun ally -> Algorithms.expPointsRemainingForNextLevel ally.ExpPoints <= battle.PrizePool.Exp)
            let textA =
                match alliesLevelingUp with
                | _ :: _ -> "Level up for " + (alliesLevelingUp |> List.map (fun c -> c.Name) |> String.join ", ") + "!^"
                | [] -> "Enemies defeated!^"
            let textB =
                alliesLevelingUp |>
                List.choose (fun ally ->
                    let techs = Algorithms.expPointsToTechs3 ally.ExpPoints battle.PrizePool.Exp ally.ArchetypeType
                    if Set.notEmpty techs then Some (ally, techs) else None) |>
                List.map (fun (ally, techs) ->
                    let text = techs |> Set.toList |> List.map scstring |> String.join ", "
                    ally.Name + " learned " + text + "!") |>
                function
                | _ :: _ as texts -> String.join "\n" texts + "^"
                | [] -> ""
            let textC = "Gained " + string battle.PrizePool.Exp + " Exp!\nGained " + string battle.PrizePool.Gold + " Gold!"
            let textD =
                match battle.PrizePool.Items with
                | _ :: _ as items -> "^Found " + (items |> List.map (fun i -> ItemType.getName i) |> String.join ", ") + "!"
                | [] -> ""
            let text = textA + textB + textC + textD
            let dialog = Dialog.make DialogThick text
            let battle = { battle with DialogOpt = Some dialog }
            let (sigs, battle) =
                if outcome then
                    let battle = mapAllies (fun ally -> if ally.Healthy then Character.mapExpPoints ((+) battle.PrizePool.Exp) ally else ally) battle
                    let battle =
                        mapAllies (fun ally ->
                            if List.exists (fun (ally' : Character) -> ally.CharacterIndex = ally'.CharacterIndex) alliesLevelingUp
                            then Character.restore ally
                            else ally)
                            battle
                    let battle = { battle with Inventory = { battle.Inventory with Gold = battle.Inventory.Gold + battle.PrizePool.Gold }}
                    let battle = { battle with Inventory = Inventory.tryAddItems battle.PrizePool.Items battle.Inventory |> snd }
                    if List.notEmpty alliesLevelingUp
                    then withSignal (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.GrowthSound)) battle
                    else just battle
                else just battle
            (signal (FadeOutSong 360L) :: sigs, battle)
        else
            match battle.DialogOpt with
            | None ->
                let battle = { battle with BattleState = BattleConcluding (battle.BattleTime, outcome) }
                update battle
            | Some _ -> just battle

    and private updateConcluding startTime (battle : Battle) =
        let localTime = battle.BattleTime - startTime
        if localTime = 0L
        then withSignal Concluding battle
        else just battle

    and update (battle : Battle) : Signal list * Battle =

        // update message
        let battle =
            { battle with
                MessageOpt =
                    match battle.MessageOpt with
                    | Some (startTime, lifeTime, message) when battle.BattleTime < startTime + lifeTime -> Some (startTime, lifeTime, Dialog.update id battle.BattleTime message)
                    | Some _ | None -> None }

        // update dialog
        let battle =
            { battle with
                DialogOpt =
                    match battle.DialogOpt with
                    | Some dialog -> Some (Dialog.update id battle.BattleTime dialog)
                    | None -> None }

        // update battle state
        let (signals, battle) =
            match battle.BattleState with
            | BattleReadying startTime -> updateReadying startTime battle
            | BattleRunning -> updateRunning battle
            | BattleResult (startTime, outcome) -> updateResult startTime outcome battle
            | BattleConcluding (startTime, _) -> updateConcluding startTime battle
            | BattleConclude -> just battle

        // fin
        (signals, battle)

    let updateBattleTime field =
        let field = { field with BattleTime = inc field.BattleTime }
        just field

    let makeFromParty inventory (party : Party) (prizePool : PrizePool) battleSpeed battleData =
        let enemies = randomizeEnemies party.Length (battleSpeed = WaitSpeed) battleData.BattleEnemies
        let characters = party @ enemies |> Map.ofListBy (fun (character : Character) -> (character.CharacterIndex, character))
        let prizePool = { prizePool with Gold = List.fold (fun gold (enemy : Character) -> gold + enemy.GoldPrize) prizePool.Gold enemies }
        let prizePool = { prizePool with Exp = List.fold (fun exp (enemy : Character) -> exp + enemy.ExpPrize) prizePool.Exp enemies }
        let prizePool = { prizePool with Items = List.fold (fun items (enemy : Character) -> match enemy.ItemPrizeOpt with Some item -> item :: items | None -> items) prizePool.Items enemies }
        let tileMap = battleData.BattleTileMap
        let tileIndexOffset = battleData.BattleTileIndexOffset
        let tileIndexOffsetRange = battleData.BattleTileIndexOffsetRange
        { BattleTime = 0L
          BattleState = BattleReadying 1L
          Inventory = inventory
          Characters = characters
          PrizePool = prizePool
          TileMap = tileMap
          TileIndexOffset = tileIndexOffset
          TileIndexOffsetRange = tileIndexOffsetRange
          BattleSongOpt = battleData.BattleSongOpt
          CurrentCommandOpt = None
          ActionCommands = FQueue.empty
          MessageOpt = None
          DialogOpt = None
          BattleSpeed = battleSpeed }

    let empty =
        match Map.tryFind EmptyBattle Data.Value.Battles with
        | Some battle ->
            { BattleTime = 0L
              BattleState = BattleConclude
              Inventory = Inventory.empty
              Characters = Map.empty
              PrizePool = PrizePool.empty
              TileMap = battle.BattleTileMap
              TileIndexOffset = 0
              TileIndexOffsetRange = (0, 0)
              BattleSongOpt = None
              CurrentCommandOpt = None
              ActionCommands = FQueue.empty
              MessageOpt = None
              DialogOpt = None
              BattleSpeed = PacedSpeed }
        | None -> failwith "Expected data for DebugBattle to be available."