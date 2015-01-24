﻿namespace InfinityRpg
open System
open System.IO
open SDL2
open OpenTK
open Prime
open Nu
open Nu.Constants
open Nu.WorldConstants
open Nu.Observation
open Nu.Chain
open AStar
open InfinityRpg
open InfinityRpg.Constants

[<AutoOpen>]
module GameplayDispatcherModule =

    type [<StructuralEquality; NoComparison>] PlayerInput =
        | TouchInput of Vector2
        | DetailInput of Direction
        | NoInput

    type Screen with

        member this.GetContentRandState world : uint64 = (this.GetXtension world)?ContentRandState
        member this.SetContentRandState (value : uint64) world = this.UpdateXtension (fun xtension -> xtension?ContentRandState <- value) world
        member this.GetOngoingRandState world : uint64 = (this.GetXtension world)?OngoingRandState
        member this.SetOngoingRandState (value : uint64) world = this.UpdateXtension (fun xtension -> xtension?OngoingRandState <- value) world
        member this.GetShallLoadGame world : bool = (this.GetXtension world)?ShallLoadGame
        member this.SetShallLoadGame (value : bool) world = this.UpdateXtension (fun xtension -> xtension?ShallLoadGame <- value) world

    type GameplayDispatcher () =
        inherit ScreenDispatcher ()

        (* Hud Addresses *)

        static let getHudAddress address = satoga address HudName
        static let getHudHaltAddress address = gatoea (getHudAddress address) HudHaltName
        static let getHudSaveGameAddress address = gatoea (getHudAddress address) HudSaveGameName
        static let getHudFeelerAddress address = gatoea (getHudAddress address) HudFeelerName
        static let getHudDetailUpAddress address = gatoea (getHudAddress address) HudDetailUpName
        static let getHudDetailRightAddress address = gatoea (getHudAddress address) HudDetailRightName
        static let getHudDetailDownAddress address = gatoea (getHudAddress address) HudDetailDownName
        static let getHudDetailLeftAddress address = gatoea (getHudAddress address) HudDetailLeftName

        (* Proxies *)

        static let proxyScene gameplay =
            Group.proxy <| satoga gameplay.ScreenAddress SceneName
        
        static let proxyField gameplay =
            Entity.proxy <| gatoea (proxyScene gameplay).GroupAddress FieldName
        
        static let proxyCharacters gameplay world =
            let entities = World.getEntities (proxyScene gameplay) world
            Seq.filter (fun (entity : Entity) -> entity.DispatchesAs typeof<CharacterDispatcher> world) entities

        static let proxyOptCharacterAtPosition position gameplay world =
            let characters = proxyCharacters gameplay world
            Seq.tryFind (fun (character : Entity) -> character.GetPosition world = position) characters
        
        static let proxyOptCharacterInDirection position direction gameplay world =
            proxyOptCharacterAtPosition (position + dtovf direction) gameplay world
        
        static let proxyCharacterInDirection position direction gameplay world =
            Option.get <| proxyOptCharacterInDirection position direction gameplay world

        static let proxyPlayer gameplay =
            Entity.proxy <| gatoea (proxyScene gameplay).GroupAddress PlayerName

        static let proxyEnemies gameplay world =
            let entities = World.getEntities (proxyScene gameplay) world
            Seq.filter (fun (entity : Entity) -> entity.DispatchesAs typeof<EnemyDispatcher> world) entities

        (* End of Proxies *)

        static let createField scene rand world =
            let pathEdgesM = [(Vector2i (1, 10), Vector2i (20, 10))]
            let (fieldMap, rand) = FieldMap.make FieldTileSheetImage (Vector2i 22) pathEdgesM rand
            let (field, world) = World.createEntity typeof<FieldDispatcher>.Name (Some FieldName) scene world
            let world = field.SetFieldMapNp fieldMap world
            let world = field.SetSize (World.getQuickSize field world) world
            let world = field.SetPersistent false world
            (field, rand, world)

        static let createEnemies scene rand world =
            let (randResult, rand) = Rand.nextIntUnder 5 rand
            let enemyCount = randResult + 1
            List.fold
                (fun (enemies, rand, world) i ->
                    let enemyPosition = single i * TileSize * 2.0f
                    let (enemy, world) = World.createEntity typeof<EnemyDispatcher>.Name None scene world
                    let world = enemy.SetDepth CharacterDepth world
                    let world = enemy.SetPosition enemyPosition world
                    let world = enemy.SetCharacterAnimationSheet GoopyImage world
                    (enemy :: enemies, rand, world))
                ([], rand, world)
                [0 .. enemyCount - 1]

        static let walk3 positive current destination =
            let walkSpeed = if positive then CharacterWalkSpeed else -CharacterWalkSpeed
            let next = current + walkSpeed
            let delta = if positive then destination - next else next - destination
            if delta < CharacterWalkSpeed then (destination, WalkFinished) else (next, WalkContinuing)

        static let walk walkDescriptor (position : Vector2) =
            let walkOrigin = vmtovf walkDescriptor.WalkOriginM
            let walkVector = dtovf walkDescriptor.WalkDirection
            let walkDestination = walkOrigin + walkVector
            match walkDescriptor.WalkDirection with
            | Upward -> let (newY, arrival) = walk3 true position.Y walkDestination.Y in (Vector2 (position.X, newY), arrival)
            | Rightward -> let (newX, arrival) = walk3 true position.X walkDestination.X in (Vector2 (newX, position.Y), arrival)
            | Downward -> let (newY, arrival) = walk3 false position.Y walkDestination.Y in (Vector2 (position.X, newY), arrival)
            | Leftward -> let (newX, arrival) = walk3 false position.X walkDestination.X in (Vector2 (newX, position.Y), arrival)

        static let getCharacterAnimationStateByActionBegin tickTime characterPosition characterAnimationState actionDescriptor =
            let currentDirection = characterAnimationState.Direction
            let direction = ActionDescriptor.getActionDirection characterPosition currentDirection actionDescriptor
            { characterAnimationState with
                Direction = direction
                AnimationType = CharacterAnimationActing
                StartTime = tickTime }

        static let getCharacterAnimationStateByActionEnd tickTime characterAnimationState =
            { characterAnimationState with
                AnimationType = CharacterAnimationFacing
                StartTime = tickTime }

        static let tryGetNavigationPath touchPosition occupationMap (character : Entity) world =
            let nodes = OccupationMap.makeNavigationNodes occupationMap
            let goalNode = Map.find (vftovm touchPosition) nodes
            let currentNode = Map.find (vftovm <| character.GetPosition world) nodes
            let optNavigationPath =
                AStar.FindPath (
                    currentNode,
                    goalNode,
                    (fun n n2 -> if n2.PositionM.Y <> n.PositionM.Y then 2.0f else 1.0f), // prefer horizontal walk to vertical for predictability
                    (fun n -> 0.0f))
            match optNavigationPath with
            | null -> None
            | navigationPath -> Some (navigationPath |> List.ofSeq |> List.rev |> List.tail)

        static let isPlayerNavigatingPath gameplay world =
            let player = proxyPlayer gameplay
            ActivityState.isNavigatingPath <| player.GetActivityState world

        static let cancelNavigation (character : Entity) world =
            let characterActivity =
                match character.GetActivityState world with
                | Action _ as action -> action
                | NoActivity -> NoActivity
                | Navigation navDescriptor -> Navigation { navDescriptor with OptNavigationPath = None }
            character.SetActivityState characterActivity world

        static let anyTurnsInProgress2 (player : Entity) enemies world =
            player.GetActivityState world <> NoActivity ||
            Seq.exists
                (fun (enemy : Entity) -> enemy.GetDesiredTurn world <> NoTurn || enemy.GetActivityState world <> NoActivity)
                enemies

        static let anyTurnsInProgress address world =
            let player = World.getEntity (getPlayerAddress address) world
            let enemies = World.getEntities (getEnemyAddresses address world) world
            anyTurnsInProgress2 player enemies

        static let updateCharacterByWalk walkDescriptor (character : Entity) =
            let (newPosition, walkState) = walk walkDescriptor character.Position
            let character = Entity.setPosition newPosition character
            let characterAnimationState = { character.CharacterAnimationState with Direction = walkDescriptor.WalkDirection }
            let character = Entity.setCharacterAnimationState characterAnimationState character
            (character, walkState)

        static let updateCharacterByWalkState walkState navigationDescriptor (character : Entity) =
            match walkState with
            | WalkFinished ->
                match navigationDescriptor.OptNavigationPath with
                | Some [] -> failwith "NavigationPath should never be empty here."
                | Some (_ :: []) -> Entity.setActivityState NoActivity character
                | Some (currentNode :: navigationPath) ->
                    let walkDirection = vmtod <| (List.head navigationPath).PositionM - currentNode.PositionM
                    let walkDescriptor = { WalkDirection = walkDirection; WalkOriginM = vftovm character.Position }
                    let navigationDescriptor = { WalkDescriptor = walkDescriptor; OptNavigationPath = Some navigationPath }
                    Entity.setActivityState (Navigation navigationDescriptor) character
                | None -> Entity.setActivityState NoActivity character
            | WalkContinuing -> character

        static let updateCharacterByNavigation navigationDescriptor characterAddress world =
            let character = World.getEntity characterAddress world
            let (character, walkState) = updateCharacterByWalk navigationDescriptor.WalkDescriptor character
            let character = updateCharacterByWalkState walkState navigationDescriptor character
            World.setEntity character characterAddress world

        static let updateCharacterByAction actionDescriptor characterAddress world =
            let tickTime = world.State.TickTime
            let character = World.getEntity characterAddress world
            let character =
                if actionDescriptor.ActionTicks = 0L then
                    character |>
                        Entity.setCharacterAnimationState (getCharacterAnimationStateByActionBegin tickTime character.Position character.CharacterAnimationState actionDescriptor) |>
                        Entity.setActivityState (Action <| ActionDescriptor.incActionTicks actionDescriptor)
                elif actionDescriptor.ActionTicks > 0L && actionDescriptor.ActionTicks < ActionTicksMax then
                    character |>
                        Entity.setActivityState (Action <| ActionDescriptor.incActionTicks actionDescriptor)
                else
                    character |>
                        Entity.setActivityState NoActivity |>
                        Entity.setCharacterAnimationState (getCharacterAnimationStateByActionEnd tickTime character.CharacterAnimationState)
            World.setEntity character characterAddress world

        static let determineCharacterTurnFromDirection direction occupationMap (character : Entity) opponents =
            match character.ActivityState with
            | Action _ -> NoTurn
            | Navigation _ -> NoTurn
            | NoActivity ->
                let openDirections = OccupationMap.getOpenDirectionsAtPositionM (vftovm character.Position) occupationMap
                if Set.contains direction openDirections then
                    let walkDescriptor = { WalkDirection = direction; WalkOriginM = vftovm character.Position }
                    NavigationTurn { WalkDescriptor = walkDescriptor; OptNavigationPath = None }
                else
                    let targetPosition = character.Position + dtovf direction
                    if Seq.exists (fun (opponent : Entity) -> opponent.Position = targetPosition) opponents
                    then makeAttackTurn <| vftovm targetPosition
                    else NoTurn

        static let determineCharacterTurnFromTouch touchPosition occupationMap (character : Entity) opponents =
            if character.ActivityState = NoActivity then
                match tryGetNavigationPath touchPosition occupationMap character with
                | Some navigationPath ->
                    match navigationPath with
                    | [] -> NoTurn
                    | _ ->
                        let characterPositionM = vftovm character.Position
                        let walkDirection = vmtod <| (List.head navigationPath).PositionM - characterPositionM
                        let walkDescriptor = { WalkDirection = walkDirection; WalkOriginM = characterPositionM }
                        NavigationTurn { WalkDescriptor = walkDescriptor; OptNavigationPath = Some navigationPath }
                | None ->
                    let targetPosition = touchPosition |> vftovm |> vmtovf
                    if Math.arePositionsAdjacent targetPosition character.Position then
                        if Seq.exists (fun (opponent : Entity) -> opponent.Position = targetPosition) opponents
                        then makeAttackTurn <| vftovm targetPosition
                        else NoTurn
                    else NoTurn
            else NoTurn

        static let determineDesiredEnemyTurn occupationMap (player : Entity) (enemy : Entity) rand =
            match enemy.ControlType with
            | Player ->
                debug <| "Invalid ControlType '" + acstring enemy.ControlType + "' for enemy"
                (NoTurn, rand)
            | Chaos ->
                let nextPlayerPosition =
                    match player.ActivityState with
                    | Action _ -> player.Position
                    | Navigation navigationDescriptor -> NavigationDescriptor.nextPosition navigationDescriptor
                    | NoActivity -> player.Position
                if Math.arePositionsAdjacent enemy.Position nextPlayerPosition then
                    let enemyTurn = makeAttackTurn <| vftovm nextPlayerPosition
                    (enemyTurn, rand)
                else
                    let (randResult, rand) = Rand.nextIntUnder 4 rand
                    let direction = Direction.fromInt randResult
                    let enemyTurn = determineCharacterTurnFromDirection direction occupationMap enemy [player]
                    (enemyTurn, rand)
            | Uncontrolled -> (NoTurn, rand)

        static let determineDesiredEnemyTurns occupationMap player enemies rand =
            let (_, enemyTurns, rand) =
                List.foldBack
                    (fun (enemy : Entity) (occupationMap, enemyTurns, rand) ->
                        let (enemyTurn, rand) = determineDesiredEnemyTurn occupationMap player enemy rand
                        let occupationMap = OccupationMap.transferByDesiredTurn enemyTurn enemy occupationMap
                        (occupationMap, enemyTurn :: enemyTurns, rand))
                    (List.ofSeq enemies)
                    (occupationMap, [], rand)
            (enemyTurns, rand)

        static let determinePlayerTurnFromTouch touchPosition address world =
            let field = World.getEntity (getFieldAddress address) world
            let player = World.getEntity (getPlayerAddress address) world
            let enemies = World.getEntities (getEnemyAddresses address world) world
            if not <| anyTurnsInProgress2 player enemies then
                let touchPositionW = Camera.mouseToWorld Relative touchPosition world.State.Camera
                let occupationMapWithAdjacentEnemies =
                    OccupationMap.makeFromFieldTilesAndAdjacentCharacters
                        (vftovm player.Position) field.FieldMapNp.FieldTiles enemies
                match determineCharacterTurnFromTouch touchPositionW occupationMapWithAdjacentEnemies player enemies with
                | ActionTurn _ as actionTurn -> actionTurn
                | NavigationTurn navigationDescriptor as navigationTurn ->
                    let headNavigationNode = navigationDescriptor.OptNavigationPath |> Option.get |> List.head
                    let occupationMapWithEnemies = OccupationMap.makeFromFieldTilesAndCharacters field.FieldMapNp.FieldTiles enemies
                    if Map.find headNavigationNode.PositionM occupationMapWithEnemies then CancelTurn
                    else navigationTurn
                | CancelTurn -> CancelTurn
                | NoTurn -> NoTurn
            else NoTurn

        static let determinePlayerTurnFromDetailNavigation direction address world =
            let field = World.getEntity (getFieldAddress address) world
            let player = World.getEntity (getPlayerAddress address) world
            let enemies = World.getEntities (getEnemyAddresses address world) world
            if not <| anyTurnsInProgress2 player enemies then
                let occupationMapWithEnemies = OccupationMap.makeFromFieldTilesAndCharacters field.FieldMapNp.FieldTiles enemies
                determineCharacterTurnFromDirection direction occupationMapWithEnemies player enemies
            else NoTurn

        static let determinePlayerTurnFromInput playerInput address world =
            match playerInput with
            | TouchInput touchPosition -> determinePlayerTurnFromTouch touchPosition address world
            | DetailInput direction -> determinePlayerTurnFromDetailNavigation direction address world
            | NoInput -> NoTurn

        static let determinePlayerTurn address world =
            let player = World.getEntity (getPlayerAddress address) world
            match player.ActivityState with
            | Action _ -> NoTurn
            | Navigation navigationDescriptor ->
                let walkDescriptor = navigationDescriptor.WalkDescriptor
                if player.Position = vmtovf walkDescriptor.WalkOriginM then
                    let field = World.getEntity (getFieldAddress address) world
                    let enemies = World.getEntities (getEnemyAddresses address world) world
                    let occupationMapWithEnemies = OccupationMap.makeFromFieldTilesAndCharacters field.FieldMapNp.FieldTiles enemies
                    let walkDestinationM = walkDescriptor.WalkOriginM + dtovm walkDescriptor.WalkDirection
                    if Map.find walkDestinationM occupationMapWithEnemies then CancelTurn
                    else NavigationTurn navigationDescriptor
                else NoTurn
            | NoActivity -> NoTurn

        static let determineEnemyActionActivities enemyAddresses world =
            let enemies = World.getEntities enemyAddresses world
            List.foldBack
                (fun (enemy : Entity) precedingEnemyActivities ->
                    let enemyActivity =
                        let noPrecedingEnemyActionActivity = Seq.notExists ActivityState.isActing precedingEnemyActivities
                        let noCurrentEnemyActionActivity = Seq.notExists (fun (enemy : Entity) -> ActivityState.isActing enemy.ActivityState) enemies
                        if noPrecedingEnemyActionActivity && noCurrentEnemyActionActivity then
                            match enemy.DesiredTurn with
                            | ActionTurn actionDescriptor -> Action actionDescriptor
                            | NavigationTurn _ -> NoActivity
                            | CancelTurn -> NoActivity
                            | NoTurn -> NoActivity
                        else NoActivity
                    enemyActivity :: precedingEnemyActivities)
                (List.ofSeq enemies)
                []

        static let determineEnemyNavigationActivities enemyAddresses world =
            let enemies = World.getEntities enemyAddresses world
            List.foldBack
                (fun (enemy : Entity) enemyActivities ->
                    let noCurrentEnemyActionActivity = Seq.notExists (fun (enemy : Entity) -> ActivityState.isActing enemy.ActivityState) enemies
                    let enemyActivity =
                        if noCurrentEnemyActionActivity then
                            match enemy.DesiredTurn with
                            | ActionTurn _ -> NoActivity
                            | NavigationTurn navigationDescriptor -> Navigation navigationDescriptor
                            | CancelTurn -> NoActivity
                            | NoTurn -> NoActivity
                        else NoActivity
                    enemyActivity :: enemyActivities)
                (List.ofSeq enemies)
                []

        static let runCharacterReaction actionDescriptor initiatorAddress address world =
            // TODO: implement animations
            let initiator = World.getEntity initiatorAddress world
            if actionDescriptor.ActionTicks = ActionTicksMax then
                let reactorAddress = getCharacterAddressInDirection initiator.Position initiator.CharacterAnimationState.Direction address world
                let reactor = World.getEntity reactorAddress world
                let reactorDamage = initiator.PowerBuff * 5.0f - reactor.ShieldBuff |> int
                let reactorHitPoints = reactor.HitPoints - reactorDamage
                let reactor = Entity.setHitPoints reactorHitPoints reactor
                let world = World.setEntity reactor reactorAddress world
                if reactor.HitPoints <= 0 then
                    if reactor.Name = PlayerName
                    then World.transitionScreen TitleAddress world
                    else World.removeEntity reactorAddress world
                else world
            else world

        static let runCharacterNavigation newNavigationDescriptor characterAddress address world =
            let chain = chain {
                do! updateEntity (Entity.setActivityState <| Navigation newNavigationDescriptor) characterAddress
                do! during (fun world ->
                    match World.getEntityBy Entity.getActivityState characterAddress world with
                    | Navigation navigationDescriptor -> newNavigationDescriptor.WalkDescriptor.WalkOriginM = navigationDescriptor.WalkDescriptor.WalkOriginM
                    | Action _ -> false
                    | NoActivity -> false) ^ chain {
                    do! update ^ fun world ->
                        let navigationDescriptor =
                            match World.getEntityBy Entity.getActivityState characterAddress world with
                            | Navigation navigationDescriptor -> navigationDescriptor
                            | _ -> failwith "Unexpected match failure in InfinityRpg.GameplayDispatcherModule.runCharacterNavigation."
                        updateCharacterByNavigation navigationDescriptor characterAddress world
                    do! pass }}
            let observation = observe TickEventAddress characterAddress |> until (DeselectEventAddress ->>- address)
            snd <| runAssumingCascade chain observation world

        static let runCharacterAction newActionDescriptor characterAddress address world =
            // NOTE: currently just implements attack
            let chain = chain {
                do! updateEntity (Entity.setActivityState <| Action newActionDescriptor) characterAddress
                do! during (World.getEntityBy (Entity.getActivityState >> ActivityState.isActing) characterAddress) ^ chain {
                    do! update ^ fun world ->
                        let actionDescriptor =
                            match World.getEntityBy Entity.getActivityState characterAddress world  with
                            | Action actionDescriptor -> actionDescriptor
                            | _ -> failwithumf ()
                        let world = updateCharacterByAction actionDescriptor characterAddress world
                        runCharacterReaction actionDescriptor characterAddress address world
                    do! pass }}
            let observation = observe TickEventAddress characterAddress |> until (DeselectEventAddress ->>- address)
            snd <| runAssumingCascade chain observation world

        static let runCharacterNoActivity characterAddress world =
            World.updateEntity (Entity.setActivityState NoActivity) characterAddress world

        static let runCharacterActivity newActivity characterAddress address world =
            match newActivity with
            | Action newActionDescriptor -> runCharacterAction newActionDescriptor characterAddress address world
            | Navigation newNavigationDescriptor -> runCharacterNavigation newNavigationDescriptor characterAddress address world
            | NoActivity -> runCharacterNoActivity characterAddress world

        static let tryRunEnemyActivity address world newActivity enemyAddress =
            if newActivity <> NoActivity then
                let enemy = World.getEntity enemyAddress world
                let enemy = Entity.setDesiredTurn NoTurn enemy
                let world = World.setEntity enemy enemyAddress world
                runCharacterActivity newActivity enemyAddress address world
            else world

        static let runEnemyNavigationActivities enemyNavigationActivities enemyAddresses address world =
            if Seq.exists ActivityState.isNavigating enemyNavigationActivities
            then Seq.fold2 (tryRunEnemyActivity address) world enemyNavigationActivities enemyAddresses
            else world

        static let runEnemyActivities enemyActionActivities enemyNavigationActivities enemyAddresses address world =
            let anyEnemyActionActivity = Seq.exists ActivityState.isActing enemyActionActivities
            let newEnemyActivities = if anyEnemyActionActivity then enemyActionActivities else enemyNavigationActivities
            Seq.fold2 (tryRunEnemyActivity address) world newEnemyActivities enemyAddresses

        static let runPlayerTurn playerTurn address world =

            // construct occupation map
            let occupationMap =
                let field = World.getEntity (getFieldAddress address) world
                let enemies = World.getEntities (getEnemyAddresses address world) world
                OccupationMap.makeFromFieldTilesAndCharactersAndDesiredTurn field.FieldMapNp.FieldTiles enemies playerTurn

            // determine player activity
            let optNewPlayerActivity =
                match playerTurn with
                | ActionTurn actionDescriptor -> Some <| Action actionDescriptor
                | NavigationTurn navigationDescriptor -> Some <| Navigation navigationDescriptor
                | CancelTurn -> Some NoActivity
                | NoTurn -> None

            // run player activity
            let world =
                match optNewPlayerActivity with
                | Some newPlayerActivity ->
                    let playerAddress = getPlayerAddress address
                    runCharacterActivity newPlayerActivity playerAddress address world
                | None -> world

            // determine (and set) enemy desired turns if applicable
            let world =
                // TODO: raise the level of abstraction up here (use addresses only instead of raw entity values)
                match optNewPlayerActivity with
                | Some (Action _)
                | Some (Navigation _) ->
                    let gameplay = World.getScreen address world
                    let rand = Rand.make gameplay.OngoingRandState
                    let player = World.getEntity (getPlayerAddress address) world
                    let enemyAddresses = getEnemyAddresses address world
                    let enemies = World.getEntities enemyAddresses world
                    let (enemyDesiredTurns, rand) = determineDesiredEnemyTurns occupationMap player enemies rand
                    let enemies = Seq.map2 Entity.setDesiredTurn enemyDesiredTurns enemies
                    let world = World.setEntities enemies enemyAddresses world
                    let gameplay = Screen.setOngoingRandState (Rand.getState rand) gameplay
                    World.setScreen gameplay address world
                | Some NoActivity
                | None -> world

            // run enemy activities in accordance with the player's current activity
            let world =
                let enemyAddresses = getEnemyAddresses address world
                let playerActivity = World.getEntityBy (fun player -> player.ActivityState) (getPlayerAddress address) world
                match playerActivity with
                | Action _ -> world
                | Navigation _ 
                | NoActivity ->
                    let newEnemyActionActivities = determineEnemyActionActivities enemyAddresses world
                    let newEnemyNavigationActivities = determineEnemyNavigationActivities enemyAddresses world
                    if List.exists ActivityState.isActing newEnemyActionActivities then
                        let world = runEnemyActivities newEnemyActionActivities newEnemyNavigationActivities enemyAddresses address world
                        World.updateEntity cancelNavigation (getPlayerAddress address) world
                    else runEnemyNavigationActivities newEnemyNavigationActivities enemyAddresses address world

            // teh world
            world

        static let tryRunPlayerTurn playerInput address world =
            if not <| anyTurnsInProgress address world then
                let hudSaveGameAddress = getHudSaveGameAddress address
                let hudHaltAddress = getHudHaltAddress address
                let playerAddress = getPlayerAddress address
                let chain = chain {
                    do! updateEntity (Entity.setEnabled false) hudSaveGameAddress
                    do! loop 0 inc (fun i world -> i = 0 || anyTurnsInProgress address world) ^ fun i -> chain {
                        let! event = next
                        do! match event.Data with
                            | Right _ -> chain {
                                let! playerTurn =
                                    if i = 0
                                    then getBy <| determinePlayerTurnFromInput playerInput address
                                    else getBy <| determinePlayerTurn address
                                do! update <| runPlayerTurn playerTurn address }
                            | Left _ -> updateEntity cancelNavigation playerAddress }
                    do! updateEntity (Entity.setEnabled true) hudSaveGameAddress }
                let observation =
                    observe (ClickEventAddress ->>- hudHaltAddress) address |>
                    sum TickEventAddress |>
                    until (DeselectEventAddress ->>- address)
                snd <| runAssumingCascade chain observation world
            else world

        static let handlePlayerChange event world =
            let address = event.SubscriberAddress
            let playerNavigatingPath = isPlayerNavigatingPath address world
            let world = World.updateEntity (Entity.setEnabled playerNavigatingPath) (getHudHaltAddress address) world
            (Cascade, world)

        static let handleTouchFeeler event world =
            let playerInput = TouchInput event.Data
            let world = tryRunPlayerTurn playerInput event.SubscriberAddress world
            (Cascade, world)

        static let handleDownDetail direction event world =
            let playerInput = DetailInput direction
            let world = tryRunPlayerTurn playerInput event.SubscriberAddress world
            (Cascade, world)

        static let handleNewGame gameplay world =

            // get common proxies
            let scene = proxyScene gameplay

            // generate non-deterministic random numbers
            let sysrandom = Random ()
            let contentSeedState = uint64 <| sysrandom.Next ()
            let ongoingSeedState = uint64 <| sysrandom.Next ()

            // initialize gameplay screen
            let world = gameplay.SetContentRandState contentSeedState world
            let world = gameplay.SetOngoingRandState ongoingSeedState world

            // make scene group
            let (scene, world) = World.createGroup typeof<GroupDispatcher>.Name (Some SceneName) gameplay world

            // make rand from gameplay
            let rand = Rand.make <| gameplay.GetContentRandState world

            // make field
            let (field, rand, world) = createField scene rand world

            // make player
            let (player, world) = World.createEntity typeof<PlayerDispatcher>.Name (Some PlayerName) scene world
            let world = player.SetDepth CharacterDepth world

            // make enemies
            __c <| createEnemies scene rand world

        static let handleLoadGame (gameplay : Screen) world =

            // get common proxies
            let scene = proxyScene gameplay

            // get and initialize gameplay screen from read
            let contentRandState = gameplay.GetContentRandState world
            let ongoingRandState = gameplay.GetOngoingRandState world
            let world = snd <| World.readScreenFromFile SaveFilePath (Some GameplayName) world
            let world = gameplay.SetContentRandState contentRandState world
            let world = gameplay.SetOngoingRandState ongoingRandState world

            // make rand from gameplay
            let rand = Rand.make <| gameplay.GetContentRandState world

            // make field from rand (field is not serialized, but generated deterministically with ContentRandState)
            __c <| createField scene rand world

        static let handleSelectTitle _ world =
            let world = World.playSong DefaultTimeToFadeOutSongMs 1.0f ButterflyGirlSong world
            (Cascade, world)

        static let handleSelectGameplay event  world =
            let gameplay = event.Subscriber : Screen
            let world =
                // NOTE: doing a File.Exists then loading the file is dangerous since the file can
                // always be deleted / moved between the two operations!
                if gameplay.GetShallLoadGame world && File.Exists SaveFilePath
                then handleLoadGame gameplay world
                else handleNewGame gameplay world
            let world = World.playSong DefaultTimeToFadeOutSongMs 1.0f HerosVengeanceSong world
            (Cascade, world)

        static let handleClickSaveGame event world =
            let gameplay = event.Subscriber
            World.writeScreenToFile SaveFilePath gameplay world
            (Cascade, world)

        static let handleDeselectGameplay event world =
            let scene = proxyScene event.Subscriber
            let world = World.destroyGroup scene world
            (Cascade, world)

        static member FieldDefinitions =
            [define? ContentRandState Rand.DefaultSeedState
             define? OngoingRandState Rand.DefaultSeedState
             define? ShallLoadGame false]

        override dispatcher.Register gameplay world =
            world |>
                (observe (EntityChangeEventAddress ->>- (proxyPlayer gameplay).EntityAddress) gameplay |> subscribe handlePlayerChange) |>
                (observe (TouchEventAddress ->>- getHudFeelerAddress gameplay.ScreenAddress) gameplay |> filter isObserverSelected |> monitor handleTouchFeeler) |>
                (observe (DownEventAddress ->>- getHudDetailUpAddress gameplay.ScreenAddress) gameplay |> filter isObserverSelected |> monitor (handleDownDetail Upward)) |>
                (observe (DownEventAddress ->>- getHudDetailRightAddress gameplay.ScreenAddress) gameplay |> filter isObserverSelected |> monitor (handleDownDetail Rightward)) |>
                (observe (DownEventAddress ->>- getHudDetailDownAddress gameplay.ScreenAddress) gameplay |> filter isObserverSelected |> monitor (handleDownDetail Downward)) |>
                (observe (DownEventAddress ->>- getHudDetailLeftAddress gameplay.ScreenAddress) gameplay |> filter isObserverSelected |> monitor (handleDownDetail Leftward)) |>
                (World.subscribe4 handleSelectTitle (SelectEventAddress ->>- Title.ScreenAddress) gameplay) |>
                (World.subscribe4 handleSelectGameplay (SelectEventAddress ->>- gameplay.ScreenAddress) gameplay) |>
                (World.subscribe4 handleClickSaveGame (ClickEventAddress ->>- getHudSaveGameAddress gameplay.ScreenAddress) gameplay) |>
                (World.subscribe4 handleDeselectGameplay (DeselectEventAddress ->>- gameplay.ScreenAddress) gameplay)