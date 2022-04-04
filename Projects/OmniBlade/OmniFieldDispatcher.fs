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
module FieldDispatcher =

    type [<NoEquality; NoComparison>] FieldMessage =
        | Update
        | UpdateAvatar of Avatar
        | UpdateFieldTransition
        | MenuTeamOpen
        | MenuTeamAlly of int
        | MenuItemsOpen
        | MenuItemsPageUp
        | MenuInventoryPageDown
        | MenuItemSelect of Lens<int * (ItemType * int Option), World>
        | MenuItemUse of int
        | MenuItemCancel
        | MenuTechOpen
        | MenuTechAlly of int
        | MenuTechSelect of int
        | MenuOptionsOpen
        | MenuOptionsSelectBattleSpeed of BattleSpeed
        | MenuClose
        | ShopBuy
        | ShopSell
        | ShopPageUp
        | ShopPageDown
        | ShopSelect of Lens<int * (ItemType * int Option), World>
        | ShopConfirmAccept
        | ShopConfirmDecline
        | ShopLeave
        | PromptLeft
        | PromptRight
        | TryBattle of BattleType * Advent Set
        | Interact

    type [<NoEquality; NoComparison>] FieldCommand =
        | ProcessKeyInput
        | ProcessTouchInput of Vector2
        | UpdateEye
        | PlayFieldSong
        | PlaySound of int64 * single * Sound AssetTag
        | PlaySong of int * int * single * double * Song AssetTag
        | FadeOutSong of int
        | Nop

    type Screen with
        member this.GetField world = this.GetModelGeneric<Field> world
        member this.SetField value world = this.SetModelGeneric<Field> value world
        member this.Field = this.ModelGeneric<Field> ()

    [<RequireQualifiedAccess>]
    module Cue =

        let rec run
            (cue : Cue)
            (definitions : CueDefinitions)
            (field : Field)
            (world : World) :
            Cue * CueDefinitions * (Signal<FieldMessage, FieldCommand> list * Field) =

            match cue with
            | Cue.Nil ->
                (cue, definitions, just field)

            | Cue.PlaySound (volume, sound) ->
                (Cue.Nil, definitions, withCmd (PlaySound (0L, volume, sound)) field)

            | Cue.PlaySong (fadeIn, fadeOut, volume, start, song) ->
                (Cue.Nil, definitions, withCmd (PlaySong (fadeIn, fadeOut, volume, start, song)) field)

            | Cue.FadeOutSong fade ->
                (Cue.Nil, definitions, withCmd (FadeOutSong fade) field)

            | Cue.Face (target, direction) ->
                match target with
                | AvatarTarget ->
                    let field = Field.updateAvatar (Avatar.updateDirection (constant direction)) field
                    (Cue.Nil, definitions, just field)
                | CharacterTarget characterType ->
                    let propIdOpt =
                        Field.tryGetPropIdByData
                            (function
                             | Character (characterType2, _, _, _, _, requirements) -> characterType = characterType2 && field.Advents.IsSupersetOf requirements
                             | _ -> false)
                            field
                    match propIdOpt with
                    | Some propId ->
                        let field =
                            Field.updatePropState
                                (function
                                 | CharacterState (color, animationState) ->
                                    let animationState = CharacterAnimationState.face direction animationState
                                    CharacterState (color, animationState)
                                 | propState -> propState)
                                propId
                                field
                        (Cue.Nil, definitions, just field)
                    | None ->
                        (Cue.Nil, definitions, just field)
                | NpcTarget _ | ShopkeepTarget _ | CharacterIndexTarget _ | SpriteTarget _ ->
                    (Cue.Nil, definitions, just field)

            | Cue.ClearSpirits ->
                let field = Field.clearSpirits field
                (Cue.Nil, definitions, just field)

            | Recruit allyType ->
                let fee = Field.getRecruitmentFee field
                if field.Inventory.Gold >= fee then
                    let advent =
                        match allyType with
                        | Jinn -> failwithumf ()
                        | Shade -> ShadeRecruited
                        | Mael -> MaelRecruited
                        | Riain -> RiainRecruited
                        | Peric -> PericRecruited
                    let field = Field.recruit allyType field
                    let field = Field.updateAdvents (Set.add advent) field
                    let field = Field.updateInventory (Inventory.updateGold (fun gold -> gold - fee)) field
                    (Cue.Nil, definitions, withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.PurchaseSound)) field)
                else run (Parallel [Dialog ("Tu n'as pas assez...", false); Cue.PlaySound (Constants.Audio.SoundVolumeDefault, Assets.Gui.MistakeSound)]) definitions field world

            | Unseal (fee, advent) ->
                if field.Inventory.Gold >= fee then
                    let field = Field.updateInventory (Inventory.updateGold (fun gold -> gold - fee)) field
                    let field = Field.updateAdvents (Set.remove advent) field
                    (Cue.Nil, definitions, withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.SealedSound)) field) // TODO: P1: rename sound to Unsealed.
                else run (Parallel [Dialog ("Tu n'as pas assez...", false); Cue.PlaySound (Constants.Audio.SoundVolumeDefault, Assets.Gui.MistakeSound)]) definitions field world

            | AddItem itemType ->
                (Cue.Nil, definitions, just (Field.updateInventory (Inventory.tryAddItem itemType >> snd) field))

            | RemoveItem itemType ->
                (Cue.Nil, definitions, just (Field.updateInventory (Inventory.tryRemoveItem itemType >> snd) field))

            | AddAdvent advent ->
                (Cue.Nil, definitions, just (Field.updateAdvents (Set.add advent) field))

            | RemoveAdvent advent ->
                (Cue.Nil, definitions, just (Field.updateAdvents (Set.remove advent) field))

            | ReplaceAdvent (remove, add) ->
                (Cue.Nil, definitions, just (Field.updateAdvents (Set.remove remove >> Set.add add) field))

            | Wait time ->
                (WaitState (World.getUpdateTime world + time), definitions, just field)

            | WaitState time ->
                if World.getUpdateTime world < time
                then (cue, definitions, just field)
                else (Cue.Nil, definitions, just field)

            | Fade (target, length, fadeIn) ->
                (FadeState (World.getUpdateTime world, target, length, fadeIn), definitions, just field)

            | FadeState (startTime, target, length, fadeIn) ->
                let time = World.getUpdateTime world
                let localTime = time - startTime
                let progress = single localTime / single length
                let progress = if fadeIn then progress else 1.0f - progress
                let field =
                    match target with
                    | CharacterTarget characterType ->
                        let propIdOpt =
                            Field.tryGetPropIdByData
                                (function
                                 | Character (characterType2, _, _, _, _, requirements) -> characterType = characterType2 && field.Advents.IsSupersetOf requirements
                                 | _ -> false)
                                field
                        match propIdOpt with
                        | Some propId ->
                            Field.updatePropState
                                (function
                                 | CharacterState (_, animationState) -> CharacterState (colWhite.MapA (single >> (*) progress >> byte), animationState)
                                 | propState -> propState)
                                propId
                                field
                        | None -> field
                    | SpriteTarget spriteName ->
                        match Field.tryGetPropIdByData (function Sprite (spriteName2, _, _, _, _, _, _) -> spriteName = spriteName2 | _ -> false) field with
                        | Some propId ->
                            Field.updateProp
                                (fun prop ->
                                    match prop.PropData with
                                    | Sprite (_, _, color, _, _, _, _) ->
                                        Prop.updatePropState
                                            (function
                                             | SpriteState (image, _, blend, glow, flip, _) -> SpriteState (image, color.MapA (single >> (*) progress >> byte), blend, glow, flip, true)
                                             | propState -> propState)
                                            prop
                                    | _ -> prop)
                                propId
                                field
                        | None -> field
                    | _ -> field
                if  fadeIn && progress >= 1.0f ||
                    not fadeIn && progress <= 0.0f then
                    (Cue.Nil, definitions, just field)
                else (cue, definitions, just field)

            | Animate (target, characterAnimationType, wait) ->
                match target with
                | AvatarTarget ->
                    let field = Field.updateAvatar (Avatar.animate (World.getUpdateTime world) characterAnimationType) field
                    match wait with
                    | Timed 0L | NoWait -> (Cue.Nil, definitions, just field)
                    | CueWait.Wait | Timed _ -> (AnimateState (World.getUpdateTime world, wait), definitions, just field)
                | CharacterTarget characterType ->
                    let propIdOpt =
                        Field.tryGetPropIdByData
                            (function
                             | Character (characterType2, _, _, _, _, requirements) -> characterType = characterType2 && field.Advents.IsSupersetOf requirements
                             | _ -> false)
                            field
                    match propIdOpt with
                    | Some propId ->
                        let field =
                            Field.updatePropState
                                (function
                                 | CharacterState (color, animationState) ->
                                    let animationState = CharacterAnimationState.setCharacterAnimationType (Some (World.getUpdateTime world)) characterAnimationType animationState
                                    CharacterState (color, animationState)
                                 | propState -> propState)
                                propId
                                field
                        (Cue.Nil, definitions, just field)
                    | None ->
                        (Cue.Nil, definitions, just field)
                | NpcTarget _ | ShopkeepTarget _ | CharacterIndexTarget _ | SpriteTarget _ ->
                    (Cue.Nil, definitions, just field)

            | AnimateState (startTime, wait) ->
                let time = World.getUpdateTime world
                match wait with
                | CueWait.Wait ->
                    if Avatar.getAnimationFinished time field.Avatar
                    then (Cue.Nil, definitions, just field)
                    else (cue, definitions, just field)
                | Timed waitTime ->
                    let localTime = time - startTime
                    if localTime < waitTime
                    then (cue, definitions, just field)
                    else (Cue.Nil, definitions, just field)
                | NoWait ->
                    (Cue.Nil, definitions, just field)

            | Move (target, destination, moveType) ->
                match target with
                | AvatarTarget ->
                    let cue = MoveState (World.getUpdateTime world, target, field.Avatar.Bottom, destination, moveType)
                    (cue, definitions, just field)
                | CharacterTarget characterType ->
                    let propIdOpt =
                        Field.tryGetPropIdByData
                            (function
                             | Character (characterType2, _, _, _, _, requirements) -> characterType = characterType2 && field.Advents.IsSupersetOf requirements
                             | _ -> false)
                            field
                    match propIdOpt with
                    | Some propId ->
                        let prop = field.Props.[propId]
                        let cue = MoveState (World.getUpdateTime world, target, prop.Bounds.Bottom, destination, moveType)
                        (cue, definitions, just field)
                    | None -> (Cue.Nil, definitions, just field)
                | NpcTarget _ | ShopkeepTarget _ | CharacterIndexTarget _ | SpriteTarget _ ->
                    (Cue.Nil, definitions, just field)

            | MoveState (startTime, target, origin, translation, moveType) ->
                match target with
                | AvatarTarget ->
                    let time = World.getUpdateTime world
                    let localTime = time - startTime
                    let (step, stepCount) = MoveType.computeStepAndStepCount translation moveType
                    let totalTime = int64 (dec stepCount)
                    if localTime < totalTime then
                        let field = Field.updateAvatar (Avatar.updateBottom ((+) step)) field
                        (cue, definitions, just field)
                    else
                        let field = Field.updateAvatar (Avatar.updateBottom (constant (origin + translation))) field
                        (Cue.Nil, definitions, just field)
                | CharacterTarget characterType ->
                    let propIdOpt =
                        Field.tryGetPropIdByData
                            (function
                             | Character (characterType2, _, _, _, _, requirements) -> characterType = characterType2 && field.Advents.IsSupersetOf requirements
                             | _ -> false)
                            field
                    match propIdOpt with
                    | Some propId ->
                        let prop = field.Props.[propId]
                        let time = World.getUpdateTime world
                        let localTime = time - startTime
                        let (step, stepCount) = MoveType.computeStepAndStepCount translation moveType
                        let finishTime = int64 (dec stepCount)
                        if localTime < finishTime then
                            let bounds = prop.Bounds.Translate step
                            let field = Field.updateProp (Prop.updateBounds (constant bounds)) propId field
                            (cue, definitions, just field)
                        else
                            let bounds = prop.Bounds.WithBottom (origin + translation)
                            let field = Field.updateProp (Prop.updateBounds (constant bounds)) propId field
                            (Cue.Nil, definitions, just field)
                    | None -> (Cue.Nil, definitions, just field)
                | NpcTarget _ | ShopkeepTarget _ | CharacterIndexTarget _ | SpriteTarget _ ->
                    (Cue.Nil, definitions, just field)

            | Warp (fieldType, fieldDestination, fieldDirection) ->
                match field.FieldTransitionOpt with
                | Some _ ->
                    (cue, definitions, just field)
                | None ->
                    let fieldTransition =
                        { FieldType = fieldType
                          FieldDestination = fieldDestination
                          FieldDirection = fieldDirection
                          FieldTransitionTime = World.getUpdateTime world + Constants.Field.TransitionTime }
                    let field = Field.updateFieldTransitionOpt (constant (Some fieldTransition)) field
                    (WarpState, definitions, just field)

            | WarpState ->
                match field.FieldTransitionOpt with
                | Some _ -> (cue, definitions, just field)
                | None -> (Cue.Nil, definitions, just field)

            | Battle (battleType, consequents) ->
                match field.BattleOpt with
                | Some _ -> (cue, definitions, just field)
                | None -> (BattleState, definitions, withMsg (TryBattle (battleType, consequents)) field)

            | BattleState ->
                match field.BattleOpt with
                | Some _ -> (cue, definitions, just field)
                | None -> (Cue.Nil, definitions, just field)

            | Dialog (text, isNarration) ->
                match field.DialogOpt with
                | Some _ ->
                    (cue, definitions, just field)
                | None ->
                    let dialogForm = if isNarration then DialogNarration else DialogThick
                    let dialog = { DialogForm = dialogForm; DialogTokenized = text; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None }
                    let field = Field.updateDialogOpt (constant (Some dialog)) field
                    (DialogState, definitions, just field)

            | DialogState ->
                match field.DialogOpt with
                | None -> (Cue.Nil, definitions, just field)
                | Some _ -> (cue, definitions, just field)

            | Prompt (text, leftPrompt, rightPrompt) ->
                match field.DialogOpt with
                | Some _ ->
                    (cue, definitions, just field)
                | None ->
                    let dialog = { DialogForm = DialogThick; DialogTokenized = text; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = Some (leftPrompt, rightPrompt); DialogBattleOpt = None }
                    let field = Field.updateDialogOpt (constant (Some dialog)) field
                    (PromptState, definitions, just field)

            | PromptState ->
                match field.DialogOpt with
                | None -> (Cue.Nil, definitions, just field)
                | Some _ -> (cue, definitions, just field)

            | If (p, c, a) ->
                match p with
                | Gold gold -> if field.Inventory.Gold >= gold then (c, definitions, just field) else (a, definitions, just field)
                | Item itemType -> if Inventory.containsItem itemType field.Inventory then (c, definitions, just field) else (a, definitions, just field)
                | Items itemTypes -> if Inventory.containsItems itemTypes field.Inventory then (c, definitions, just field) else (a, definitions, just field)
                | Advent advent -> if field.Advents.Contains advent then (c, definitions, just field) else (a, definitions, just field)
                | Advents advents -> if field.Advents.IsSupersetOf advents then (c, definitions, just field) else (a, definitions, just field)

            | Not (p, c, a) ->
                match p with
                | Gold gold -> if field.Inventory.Gold < gold then (c, definitions, just field) else (a, definitions, just field)
                | Item itemType -> if not (Inventory.containsItem itemType field.Inventory) then (c, definitions, just field) else (a, definitions, just field)
                | Items itemTypes -> if not (Inventory.containsItems itemTypes field.Inventory) then (c, definitions, just field) else (a, definitions, just field)
                | Advent advent -> if not (field.Advents.Contains advent) then (c, definitions, just field) else (a, definitions, just field)
                | Advents advents -> if not (field.Advents.IsSupersetOf advents) then (c, definitions, just field) else (a, definitions, just field)

            | Define (name, body) ->
                if not (Map.containsKey name definitions) then
                    (Cue.Nil, Map.add name body definitions, just field)
                else
                    Log.debug ("Cue definition '" + name + "' already found.")
                    (Cue.Nil, definitions, just field)

            | Assign (name, body) ->
                if Map.containsKey name definitions then
                    (Cue.Nil, Map.add name body definitions, just field)
                else
                    Log.debug ("Cue definition '" + name + "' not found.")
                    (Cue.Nil, definitions, just field)

            | Expand name ->
                match Map.tryFind name definitions with
                | Some body ->
                    run body definitions field world
                | None ->
                    Log.debug ("Cue definition '" + name + "' not found.")
                    (Cue.Nil, definitions, ([], field))

            | Parallel cues ->
                let (cues, definitions, (signals, field)) =
                    List.fold (fun (cues, definitions, (signals, field)) cue ->
                        let (cue, definitions, (signals2, field)) = run cue definitions field world
                        if Cue.isNil cue
                        then (cues, definitions, (signals @ signals2, field))
                        else (cues @ [cue], definitions, (signals @ signals2, field)))
                        ([], definitions, ([], field))
                        cues
                match cues with
                | _ :: _ -> (Parallel cues, definitions, (signals, field))
                | [] -> (Cue.Nil, definitions, (signals, field))

            | Sequence cues ->
                let (_, haltedCues, definitions, (signals, field)) =
                    List.fold (fun (halted, haltedCues, definitions, (signals, field)) cue ->
                        if halted
                        then (halted, haltedCues @ [cue], definitions, (signals, field))
                        else
                            let (cue, definitions, (signals2, field)) = run cue definitions field world
                            if Cue.isNil cue
                            then (false, [], definitions, (signals @ signals2, field))
                            else (true, [cue], definitions, (signals @ signals2, field)))
                        (false, [], definitions, ([], field))
                        cues
                match haltedCues with
                | _ :: _ -> (Sequence haltedCues, definitions, (signals, field))
                | [] -> (Cue.Nil, definitions, (signals, field))

    [<RequireQualifiedAccess>]
    module Content =

        let private pageItems5 pageSize pageIndex filter sort (items : Map<ItemType, int>) =
            let items =
                items |>
                Map.toSeq |>
                (fun items -> if filter then ItemType.filterSellableItems items else items) |>
                (fun items -> if sort then ItemType.sortItems items else items)
            let itemsPaged =
                items |>
                Seq.index |>
                Seq.chunkBySize pageSize |>
                Seq.trySkip pageIndex |>
                Seq.map List.ofArray |>
                Seq.tryHead |>
                Option.defaultValue [] |>
                Seq.indexed |>
                Map.ofSeq |>
                Map.map (fun _ (i, (item, count)) -> (i, (item, Some count)))
            let pageUp =
                if itemsPaged.Count <> 0 then
                    let firstItemPaged = Seq.head itemsPaged
                    fst (snd firstItemPaged.Value) <> fst (Seq.head items)
                else false
            let pageDown =
                if itemsPaged.Count <> 0 then
                    let lastItemPaged = Seq.last itemsPaged
                    fst (snd lastItemPaged.Value) <> fst (Seq.last items)
                else false
            (pageUp, pageDown, itemsPaged)

        let pageItems rows (field : Field) =
            match field.Menu.MenuState with
            | MenuItem menu -> pageItems5 rows menu.ItemPage false true field.Inventory.Items
            | _ ->
                match field.ShopOpt with
                | Some shop ->
                    match shop.ShopState with
                    | ShopBuying ->
                        match Map.tryFind shop.ShopType Data.Value.Shops with
                        | Some shopData -> pageItems5 rows shop.ShopPage false false (Map.ofListBy (flip Pair.make 1) shopData.ShopItems)
                        | None -> (false, false, Map.empty)
                    | ShopSelling -> pageItems5 rows shop.ShopPage true true field.Inventory.Items
                | None -> (false, false, Map.empty)

        let sidebar position elevation (field : Lens<Field, World>) =
            Content.association Gen.name []
                [Content.button Gen.name
                    [Entity.PositionLocal == position; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "TeamButtonUp"
                     Entity.DownImage == asset "Field" "TeamButtonDown"
                     Entity.EnabledLocal <== field --> fun field -> match field.Menu.MenuState with MenuTeam _ -> false | _ -> true
                     Entity.ClickEvent ==> msg MenuTeamOpen]
                 Content.button Gen.name
                    [Entity.PositionLocal == position - v2 0.0f 81.0f; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "InventoryButtonUp"
                     Entity.DownImage == asset "Field" "InventoryButtonDown"
                     Entity.EnabledLocal <== field --> fun field -> match field.Menu.MenuState with MenuItem _ -> false | _ -> true
                     Entity.ClickEvent ==> msg MenuItemsOpen]
                 Content.button Gen.name
                    [Entity.PositionLocal == position - v2 0.0f 162.0f; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "TechButtonUp"
                     Entity.DownImage == asset "Field" "TechButtonDown"
                     Entity.EnabledLocal <== field --> fun field -> match field.Menu.MenuState with MenuTech _ -> false | _ -> true
                     Entity.ClickEvent ==> msg MenuTechOpen]
                 Content.button Gen.name
                    [Entity.PositionLocal == position - v2 0.0f 243.0f; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "OptionButtonUp"
                     Entity.DownImage == asset "Field" "OptionButtonDown"
                     Entity.EnabledLocal <== field --> fun field -> match field.Menu.MenuState with MenuOptions -> false | _ -> true
                     Entity.ClickEvent ==> msg MenuOptionsOpen]
                 Content.button Gen.name
                    [Entity.PositionLocal == position - v2 0.0f 324.0f; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "HelpButtonUp"
                     Entity.DownImage == asset "Field" "HelpButtonDown"]
                 Content.button Gen.name
                    [Entity.PositionLocal == position - v2 0.0f 405.0f; Entity.ElevationLocal == elevation; Entity.Size == v2 72.0f 72.0f
                     Entity.UpImage == asset "Field" "CloseButtonUp"
                     Entity.DownImage == asset "Field" "CloseButtonDown"
                     Entity.ClickEvent ==> msg MenuClose]]

        let team (position : Vector2) elevation rows (field : Lens<Field, World>) filter fieldMsg =
            Content.entities field
                (fun field _ -> Map.map (fun _ teammate -> (teammate, field.Menu)) field.Team)
                (fun index teammateAndMenu _ ->
                    let x = position.X + if index < rows then 0.0f else 252.0f + 48.0f
                    let y = position.Y - single (index % rows) * 81.0f
                    Content.button Gen.name
                        [Entity.PositionLocal == v2 x y; Entity.ElevationLocal == elevation; Entity.Size == v2 252.0f 72.0f
                         Entity.EnabledLocal <== teammateAndMenu --> fun (teammate, menu) -> filter teammate menu
                         Entity.Text <== teammateAndMenu --> fun (teammate, _) -> CharacterType.getName teammate.CharacterType
                         Entity.UpImage == Assets.Gui.ButtonBigUpImage
                         Entity.DownImage == Assets.Gui.ButtonBigDownImage
                         Entity.ClickEvent ==> msg (fieldMsg index)])

        let items (position : Vector2) elevation rows columns field fieldMsg =
            Content.entities field
                (fun (field : Field) _ -> pageItems rows field |> __c)
                (fun i selectionLens _ ->
                    let x = if i < columns then position.X else position.X + 375.0f
                    let y = position.Y - single (i % columns) * 81.0f
                    Content.button Gen.name
                        [Entity.PositionLocal == v2 x y; Entity.ElevationLocal == elevation; Entity.Size == v2 336.0f 72.0f
                         Entity.Justification == Justified (JustifyLeft, JustifyMiddle); Entity.Margins == v2 16.0f 0.0f
                         Entity.Text <== selectionLens --> fun (_, (itemType, countOpt)) ->
                            let itemName = ItemType.getName itemType
                            match countOpt with
                            | Some count when count > 1 -> itemName + String (Array.create (17 - itemName.Length) ' ') + "x" + string count
                            | _ -> itemName
                         Entity.EnabledLocal <== selectionLens --> fun (_, (itemType, _)) ->
                            match itemType with
                            | Consumable _ | Equipment _ -> true
                            | KeyItem _ | Stash _ -> false
                         Entity.UpImage == Assets.Gui.ButtonLongUpImage
                         Entity.DownImage == Assets.Gui.ButtonLongDownImage
                         Entity.ClickEvent ==> msg (fieldMsg selectionLens)])

        let techs (position : Vector2) elevation field fieldMsg =
            Content.entities field
                (fun (field : Field) _ ->
                    match field.Menu.MenuState with
                    | MenuTech menuTech ->
                        match Map.tryFind menuTech.TeammateIndex field.Team with
                        | Some teammate -> teammate.Techs |> Seq.index |> Map.ofSeq
                        | None -> Map.empty
                    | _ -> Map.empty)
                (fun i techLens _ ->
                    let x = position.X
                    let y = position.Y - single i * 60.0f
                    Content.button Gen.name
                        [Entity.PositionLocal == v2 x y; Entity.ElevationLocal == elevation; Entity.Size == v2 336.0f 60.0f
                         Entity.Justification == Justified (JustifyLeft, JustifyMiddle); Entity.Margins == v2 16.0f 0.0f
                         Entity.Text <== techLens --> TechType.frenchName
                         Entity.EnabledLocal == false
                         Entity.UpImage == Assets.Gui.ButtonSquishedUpImage
                         Entity.DownImage == Assets.Gui.ButtonSquishedDownImage
                         Entity.ClickEvent ==> msg (fieldMsg i)])

    type FieldDispatcher () =
        inherit ScreenDispatcher<Field, FieldMessage, FieldCommand> (Field.empty)

        static let detokenize (text : string) (field : Field) =
            text
                .Replace("$FEE", scstring (Field.getRecruitmentFee field))
                .Replace("$GOLD", scstring field.Inventory.Gold)

        static let isFacingBodyShape bodyShape (avatar : Avatar) world =
            if bodyShape.Entity.Is<PropDispatcher> world then
                let v = bodyShape.Entity.GetBottom world - avatar.Bottom
                let direction = Direction.ofVector2 v
                direction <> avatar.Direction.Opposite
            else false

        static let getFacingBodyShapes (avatar : Avatar) world =
            List.filter
                (fun bodyShape -> isFacingBodyShape bodyShape avatar world)
                avatar.IntersectedBodyShapes

        static let tryGetFacingProp (avatar : Avatar) world =
            match getFacingBodyShapes avatar world with
            | head :: _ -> Some head.Entity
            | [] -> None

        static let isTouchingSavePoint (avatar : Avatar) world =
            List.exists (fun shape ->
                match (shape.Entity.GetProp world).PropData with
                | SavePoint -> true
                | _ -> false)
                avatar.IntersectedBodyShapes

        static let tryGetFacingInteraction avatarPositionY advents (prop : Prop) =
            match prop.PropData with
            | Sprite _ -> None
            | Portal (_, _, _, _, _, _, _) -> None
            | Door _ -> Some "Ouvrir"
            | Chest (_, _, chestId, _, _, _) -> if Set.contains (Opened chestId) advents then None else Some "Open"
            | Switch (_, _, _, _) -> Some "Utiliser"
            | Sensor (_, _, _, _, _) -> None
            | Character (_, _, _, isRising, _, _) ->
                if isRising then
                    if prop.Bottom.Y - avatarPositionY > 40.0f // NOTE: just a bit of hard-coding to ensure player is interacting with the character from the south.
                    then Some "Parler"
                    else None
                else Some "Parler"
            | Npc _ | NpcBranching _ -> Some "Parler"
            | Shopkeep _ -> Some "Vendeur"
            | Seal _ -> Some "Toucher"
            | Flame _ -> None
            | SavePoint -> None
            | ChestSpawn -> None
            | EmptyProp -> None

        static let tryGetInteraction dialogOpt advents (avatar : Avatar) (field : Field) world =
            match dialogOpt with
            | Some dialog ->
                if  Dialog.canAdvance (flip detokenize field) dialog &&
                    not
                        (Dialog.isExhausted (flip detokenize field) dialog &&
                            Option.isSome dialog.DialogPromptOpt)
                then Some "Suivant"
                else None
            | None ->
                if isTouchingSavePoint avatar world then
                    Some "Sauveg."
                else
                    match tryGetFacingProp avatar world with
                    | Some prop -> tryGetFacingInteraction avatar.Position.Y advents (prop.GetProp world)
                    | None -> None

        static let tryGetTouchingPortal omniSeedState (advents : Advent Set) (avatar : Avatar) world =
            avatar.IntersectedBodyShapes |>
            List.choose (fun shape ->
                match (shape.Entity.GetProp world).PropData with
                | Portal (portalType, _, _, fieldType, portalIndex, _, requirements) ->
                    if advents.IsSupersetOf requirements then
                        match Map.tryFind fieldType Data.Value.Fields with
                        | Some fieldData ->
                            match FieldData.tryGetPortal omniSeedState portalIndex fieldData world with
                            | Some portal ->
                                match portal.PropData with
                                | Portal (_, _, direction, _, _, extended, _) ->
                                    let destination =
                                        match direction with
                                        | Upward -> portal.PropBounds.Top + v2 0.0f 8.0f + if extended then v2 0.0f 48.0f else v2Zero
                                        | Rightward -> portal.PropBounds.Right + v2 32.0f 0.0f + if extended then v2 48.0f 0.0f else v2Zero
                                        | Downward -> portal.PropBounds.Bottom + v2 0.0f -54.0f - if extended then v2 0.0f 48.0f else v2Zero
                                        | Leftward -> portal.PropBounds.Left + v2 -32.0f 0.0f - if extended then v2 48.0f 0.0f else v2Zero
                                    let isWarp = match portalType with AirPortal | StairsPortal _ -> false | WarpPortal -> true
                                    Some (fieldType, destination, direction, isWarp)
                                | _ -> None
                            | None -> None
                        | None -> None
                    else None
                | _ -> None) |>
            List.tryHead

        static let getTouchedSensors (avatar : Avatar) world =
            List.choose (fun shape ->
                match (shape.Entity.GetProp world).PropData with
                | Sensor (sensorType, _, cue, _, requirements) -> Some (sensorType, cue, requirements)
                | _ -> None)
                avatar.CollidedBodyShapes

        static let getUntouchedSensors (avatar : Avatar) world =
            List.choose (fun shape ->
                match (shape.Entity.GetProp world).PropData with
                | Sensor (sensorType, _, _, cue, requirements) -> Some (sensorType, cue, requirements)
                | _ -> None)
                avatar.SeparatedBodyShapes
        do ignore getUntouchedSensors // NOTE: suppressing warning.

        static let interactDialog dialog field =
            match Dialog.tryAdvance (flip detokenize field) dialog with
            | (true, dialog) ->
                let field = Field.updateDialogOpt (constant (Some dialog)) field
                just field
            | (false, dialog) ->
                let field = Field.updateDialogOpt (constant None) field
                match dialog.DialogBattleOpt with
                | Some (battleType, consequence) -> withMsg (TryBattle (battleType, consequence)) field
                | None -> just field

        static let interactChest itemType chestId battleTypeOpt cue requirements (prop : Prop) (field : Field) =
            if field.Advents.IsSupersetOf requirements then
                let field = Field.updateAvatar (Avatar.lookAt prop.Position) field
                let field = Field.updateAdvents (Set.add (Opened chestId)) field
                let field = Field.updateInventory (Inventory.tryAddItem itemType >> snd) field
                let field =
                    match battleTypeOpt with
                    | Some battleType -> Field.updateDialogOpt (constant (Some { DialogForm = DialogThin; DialogTokenized = "Tu as trouve " + ItemType.frenchWithQuantity itemType + "! ^Mais un truc s'approche!"; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = Some (battleType, Set.empty) })) field
                    | None -> Field.updateDialogOpt (constant (Some { DialogForm = DialogThin; DialogTokenized = "Tu as trouve " + ItemType.frenchWithQuantity itemType + "!"; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None })) field
                let field = Field.updateCue (constant cue) field
                withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.ChestOpenSound)) field
            else
                let field = Field.updateAvatar (Avatar.lookAt prop.Center) field
                let field = Field.updateDialogOpt (constant (Some { DialogForm = DialogThin; DialogTokenized = "Ferme a cle!"; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None })) field
                just field

        static let interactDoor keyItemTypeOpt cue requirements (prop : Prop) (field : Field) =
            match prop.PropState with
            | DoorState false ->
                if  field.Advents.IsSupersetOf requirements &&
                    Option.mapOrDefault (fun keyItemType -> Map.containsKey (KeyItem keyItemType) field.Inventory.Items) true keyItemTypeOpt then
                    let field = Field.updateAvatar (Avatar.lookAt prop.Center) field
                    let field = Field.updateCue (constant cue) field
                    let field = Field.updatePropState (constant (DoorState true)) prop.PropId field
                    withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.DoorOpenSound)) field
                else
                    let field = Field.updateAvatar (Avatar.lookAt prop.Center) field
                    // TODO: P1: add jiggle locked sound.
                    let field = Field.updateDialogOpt (constant (Some { DialogForm = DialogThin; DialogTokenized = "Ferme a cle!"; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None })) field
                    just field
            | _ -> failwithumf ()

        static let interactSwitch cue cue2 requirements (prop : Prop) (field : Field) =
            match prop.PropState with
            | SwitchState on ->
                if field.Advents.IsSupersetOf requirements then
                    let on = not on
                    let field = Field.updateAvatar (Avatar.lookAt prop.Center) field
                    let field = Field.updatePropState (constant (SwitchState on)) prop.PropId field
                    let field = Field.updateCue (constant (if on then cue else cue2)) field
                    withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.UseSwitchSound)) field
                else
                    let field = Field.updateAvatar (Avatar.lookAt prop.Center) field
                    // TODO: P1: add jiggle locked sound.
                    let field = Field.updateDialogOpt (constant (Some { DialogForm = DialogThin; DialogTokenized = "Won't budge!"; DialogProgress = 0; DialogPage = 0; DialogPromptOpt = None; DialogBattleOpt = None })) field
                    just field
            | _ -> failwithumf ()

        static let interactCharacter cue (prop : Prop) (field : Field) =
            let field = Field.updateAvatar (Avatar.lookAt prop.BottomInset) field
            let field = Field.updateCue (constant cue) field
            withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Gui.AffirmSound)) field
        
        static let interactNpc branches requirements (prop : Prop) (field : Field) =
            if field.Advents.IsSupersetOf requirements then
                let field = Field.updateAvatar (Avatar.lookAt prop.Position) field
                let branchesFiltered = branches |> List.choose (fun branch -> if field.Advents.IsSupersetOf branch.Requirements then Some branch.Cue else None) |> List.rev
                let branchCue = match List.tryHead branchesFiltered with Some cue -> cue | None -> Dialog ("...", false)
                let field = Field.updateCue (constant branchCue) field
                withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Gui.AffirmSound)) field
            else just field

        static let interactShopkeep shopType (prop : Prop) (field : Field) =
            let field = Field.updateAvatar (Avatar.lookAt prop.Position) field
            let shop = { ShopType = shopType; ShopState = ShopBuying; ShopPage = 0; ShopConfirmOpt = None }
            let field = Field.updateShopOpt (constant (Some shop)) field
            withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Gui.AffirmSound)) field

        static let interactSeal cue (field : Field) =
            let field = Field.updateCue (constant cue) field
            withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.SealedSound)) field

        static let interactSavePoint (field : Field) =
            let field = Field.restoreTeam field
            Field.save field
            withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Gui.SlotSound)) field

        override this.Channel (_, field) =
            [Simulants.Field.Scene.Avatar.Avatar.ChangeEvent =|> fun evt -> msg (UpdateAvatar (evt.Data.Value :?> Avatar))
             field.UpdateEvent => cmd ProcessKeyInput
             field.UpdateEvent => msg Update
             field.PostUpdateEvent => msg UpdateFieldTransition
             field.PostUpdateEvent => cmd UpdateEye
             field.SelectEvent => cmd PlayFieldSong]

        override this.Message (field, message, _, world) =

            match message with
            | Update ->

                // update cue, resetting definitions if finished
                let (cue, definitions, (signals, field)) = Cue.run field.Cue field.Definitions field world
                let field =
                    match cue with
                    | Cue.Nil -> Field.updateDefinitions (constant field.DefinitionsOriginal) field
                    | _ -> Field.updateDefinitions (constant definitions) field
                let field = Field.updateCue (constant cue) field

                // update dialog
                let field =
                    match field.DialogOpt with
                    | Some dialog ->
                        let dialog = Dialog.update (flip detokenize field) dialog world
                        Field.updateDialogOpt (constant (Some dialog)) field
                    | None -> field

                // update portal
                let (signals, field) =
                    match field.FieldTransitionOpt with
                    | None ->
                        match tryGetTouchingPortal field.OmniSeedState field.Advents field.Avatar world with
                        | Some (fieldType, destination, direction, isWarp) ->
                            if Option.isNone field.BattleOpt then // make sure we don't teleport if a battle is started earlier in the frame
                                let transition =
                                    { FieldType = fieldType
                                      FieldDestination = destination
                                      FieldDirection = direction
                                      FieldTransitionTime = World.getUpdateTime world + Constants.Field.TransitionTime }
                                let field = Field.updateFieldTransitionOpt (constant (Some transition)) field
                                let playSound =
                                    if isWarp
                                    then PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.StepWarpSound)
                                    else PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.StepStairSound)
                                (cmd playSound :: signals, field)
                            else (signals, field)
                        | None -> (signals, field)
                    | Some _ -> (signals, field)

                // update sensor
                let (signals, field) =
                    match field.FieldTransitionOpt with
                    | None ->
                        let sensors = getTouchedSensors field.Avatar world
                        let results =
                            List.fold (fun (signals : Signal<FieldMessage, FieldCommand> list, field : Field) (sensorType, cue, requirements) ->
                                if field.Advents.IsSupersetOf requirements then
                                    let field = Field.updateCue (constant cue) field
                                    match sensorType with
                                    | AirSensor -> (signals, field)
                                    | HiddenSensor | StepPlateSensor -> (Command (PlaySound (0L,  Constants.Audio.SoundVolumeDefault, Assets.Field.StepPlateSound)) :: signals, field)
                                else (signals, field))
                                (signals, field) sensors
                        results
                    | Some _ -> (signals, field)

                // update spirits
                let (signals, field) =
                    if  field.Menu.MenuState = MenuClosed &&
                        Cue.notInterrupting field.Inventory field.Advents field.Cue &&
                        Option.isNone field.DialogOpt &&
                        Option.isNone field.BattleOpt &&
                        Option.isNone field.ShopOpt &&
                        Option.isNone field.FieldTransitionOpt then
                        match Field.updateSpirits field world with
                        | Left (battleData, field) ->
                            let clockTime = let t = World.getClockTime world in t.ToUnixTimeMilliseconds ()
                            let playTime = Option.getOrDefault clockTime field.FieldSongTimeOpt
                            let startTime = clockTime - playTime
                            let prizePool = { Consequents = Set.empty; Items = []; Gold = 0; Exp = 0 }
                            let battle = Battle.makeFromTeam field.Inventory prizePool field.Team field.Options.BattleSpeed battleData (World.getUpdateTime world)
                            let field = Field.clearSpirits field
                            let field = Field.updateFieldSongTimeOpt (constant (Some startTime)) field
                            let field = Field.updateBattleOpt (constant (Some battle)) field
                            let fade = cmd (FadeOutSong 1000)
                            let beastGrowl = cmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BeastGrowlSound))
                            (fade :: beastGrowl :: signals, field)
                        | Right field -> (signals, field)
                    else (signals, field)

                // fin
                (signals, field)

            | UpdateAvatar avatar ->
                let field = Field.updateAvatar (constant avatar) field
                just field

            | UpdateFieldTransition ->

                // check if transitioning
                match field.FieldTransitionOpt with
                | Some fieldTransition ->

                    // handle field transition
                    let time = World.getUpdateTime world
                    let currentSongOpt = world |> World.getCurrentSongOpt |> Option.map (fun song -> song.Song)
                    let (signals, field) =

                        // start transition
                        if time = fieldTransition.FieldTransitionTime - Constants.Field.TransitionTime then
                            match Data.Value.Fields.TryGetValue fieldTransition.FieldType with
                            | (true, fieldData) ->
                                match (currentSongOpt, fieldData.FieldSongOpt) with
                                | (Some song, Some song2) when assetEq song song2 -> just field
                                | (_, _) -> withCmd (FadeOutSong Constants.Audio.FadeOutMsDefault) field
                            | (false, _) -> just field

                        // just past half-way point of transition
                        elif time = fieldTransition.FieldTransitionTime - Constants.Field.TransitionTime / 2L + 1L then
                            let field = Field.updateFieldType (constant fieldTransition.FieldType) field world
                            let field =
                                Field.updateAvatar (fun avatar ->
                                    let avatar = Avatar.updateDirection (constant fieldTransition.FieldDirection) avatar
                                    let avatar = Avatar.updateIntersectedBodyShapes (constant []) avatar
                                    let avatar = Avatar.updateBottom (constant fieldTransition.FieldDestination) avatar
                                    avatar)
                                    field
                            let songCmd =
                                match Field.getFieldSongOpt field with
                                | Some fieldSong ->
                                    match currentSongOpt with
                                    | Some song when assetEq song fieldSong -> Nop
                                    | _ -> PlaySong (0, Constants.Audio.FadeOutMsDefault, Constants.Audio.SongVolumeDefault, 0.0, fieldSong)
                                | None -> Nop
                            withCmd songCmd field

                        // finish transition
                        elif time = fieldTransition.FieldTransitionTime then
                            let field = Field.updateFieldTransitionOpt (constant None) field
                            just field

                        // intermediate state
                        else just field

                    // update field reference to make sure transition binding actuates
                    let field = Field.updateReference field
                    (signals, field)

                // no transition
                | None -> just field

            | MenuTeamOpen ->
                let state = MenuTeam { TeamIndex = 0; TeamIndices = Map.toKeyList field.Team }
                let field = Field.updateMenu (fun menu -> { menu with MenuState = state }) field
                just field

            | MenuTeamAlly index ->
                let field =
                    Field.updateMenu (fun menu ->
                        let state =
                            match menu.MenuState with
                            | MenuTeam menuTeam -> MenuTeam { menuTeam with TeamIndex = index }
                            | state -> state
                        { menu with MenuState = state })
                        field
                just field

            | MenuItemsOpen ->
                let itemState = MenuItem { ItemPage = 0 }
                let field = Field.updateMenu (fun menu -> { menu with MenuState = itemState }) field
                just field

            | MenuItemsPageUp ->
                let field =
                    Field.updateMenu (fun menu ->
                        match menu.MenuState with
                        | MenuItem menuItem -> { menu with MenuState = MenuItem { ItemPage = max 0 (dec menuItem.ItemPage) }}
                        | _ -> menu)
                        field
                just field

            | MenuInventoryPageDown ->
                let field =
                    Field.updateMenu (fun menu ->
                        match menu.MenuState with
                        | MenuItem menuItem -> { menu with MenuState = MenuItem { ItemPage = inc menuItem.ItemPage }}
                        | _ -> menu)
                        field
                just field

            | MenuItemSelect selectionLens ->
                let selection = Lens.get selectionLens world
                let selection = (fst selection, fst (snd selection))
                let field = Field.updateMenu (fun menu -> { menu with MenuUseOpt = MenuUse.tryMakeFromSelection selection }) field
                just field

            | MenuItemUse index ->
                match Map.tryFind index field.Team with
                | Some teammate ->
                    match field.Menu.MenuUseOpt with
                    | Some menuUse ->
                        let itemType = snd menuUse.MenuUseSelection
                        let (result, displacedOpt, teammate) = Teammate.tryUseItem itemType teammate
                        let field = if result then Field.updateInventory (Inventory.tryRemoveItem itemType >> snd) field else field
                        let field = match displacedOpt with Some displaced -> Field.updateInventory (Inventory.tryAddItem displaced >> snd) field | None -> field
                        let field = Field.updateTeam (Map.add index teammate) field
                        let field = Field.updateMenu (constant { field.Menu with MenuUseOpt = None }) field
                        if result then withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.HealSound)) field
                        else just field
                    | None -> just field
                | None -> just field

            | MenuItemCancel ->
                let field = Field.updateMenu (fun menu -> { menu with MenuUseOpt = None }) field
                just field

            | MenuTechOpen ->
                let state = MenuTech { TeammateIndex = 0 }
                let field = Field.updateMenu (fun menu -> { menu with MenuState = state }) field
                just field
            
            | MenuTechAlly index ->
                let field =
                    Field.updateMenu (fun menu ->
                        let state =
                            match menu.MenuState with
                            | MenuTech menuTech -> MenuTech { menuTech with TeammateIndex = index }
                            | state -> state
                        { menu with MenuState = state })
                        field
                just field
            
            | MenuTechSelect _ ->
                just field

            | MenuOptionsOpen ->
                let state = MenuOptions
                let field = Field.updateMenu (fun menu -> { menu with MenuState = state }) field
                just field

            | MenuOptionsSelectBattleSpeed battleSpeed ->
                let field = Field.updateOptions (constant { BattleSpeed = battleSpeed }) field
                just field
            
            | MenuClose ->
                let field = Field.updateMenu (fun menu -> { menu with MenuState = MenuClosed }) field
                just field

            | ShopBuy ->
                let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopState = ShopBuying; ShopPage = 0 })) field
                just field

            | ShopSell ->
                let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopState = ShopSelling; ShopPage = 0 })) field
                just field

            | ShopPageUp ->
                let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopPage = max 0 (dec shop.ShopPage) })) field
                just field

            | ShopPageDown ->
                let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopPage = inc shop.ShopPage })) field
                just field

            | ShopSelect selectionLens ->
                let selection = Lens.get selectionLens world
                let selection = (fst selection, fst (snd selection))
                let field =
                    Field.updateShopOpt (Option.map (fun shop ->
                        let buying = match shop.ShopState with ShopBuying -> true | ShopSelling -> false
                        let shopConfirmOpt = ShopConfirm.tryMakeFromSelection buying field.Inventory selection
                        { shop with ShopConfirmOpt = shopConfirmOpt }))
                        field
                just field

            | ShopConfirmAccept ->
                match field.ShopOpt with
                | Some shop ->
                    match shop.ShopConfirmOpt with
                    | Some shopConfirm ->
                        let itemType = snd shopConfirm.ShopConfirmSelection
                        let valid =
                            match shop.ShopState with
                            | ShopBuying ->
                                Inventory.canAddItem itemType field.Inventory &&
                                field.Inventory.Gold >= shopConfirm.ShopConfirmPrice
                            | ShopSelling -> true
                        if valid then
                            let field = Field.updateInventory (match shop.ShopState with ShopBuying -> Inventory.tryAddItem itemType >> snd | ShopSelling -> Inventory.tryRemoveItem itemType >> snd) field
                            let field = Field.updateInventory (match shop.ShopState with ShopBuying -> Inventory.updateGold (fun gold -> gold - shopConfirm.ShopConfirmPrice) | ShopSelling -> Inventory.updateGold (fun gold -> gold + shopConfirm.ShopConfirmPrice)) field
                            let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopConfirmOpt = None })) field
                            withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.PurchaseSound)) field
                        else withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Gui.MistakeSound)) field
                    | None -> just field
                | None -> just field

            | ShopConfirmDecline ->
                let field = Field.updateShopOpt (Option.map (fun shop -> { shop with ShopConfirmOpt = None })) field
                just field

            | ShopLeave ->
                let field = Field.updateShopOpt (constant None) field
                just field

            | PromptLeft ->
                match field.DialogOpt with
                | Some dialog ->
                    match dialog.DialogPromptOpt with
                    | Some ((_, promptCue), _) ->
                        let field = Field.updateDialogOpt (constant None) field
                        let field = Field.updateCue (constant promptCue) field
                        just field
                    | None -> just field
                | None -> just field

            | PromptRight ->
                match field.DialogOpt with
                | Some dialog ->
                    match dialog.DialogPromptOpt with
                    | Some (_, (_, promptCue)) ->
                        let field = Field.updateDialogOpt (constant None) field
                        let field = Field.updateCue (constant promptCue) field
                        just field
                    | None -> just field
                | None -> just field

            | TryBattle (battleType, consequents) ->
                match Map.tryFind battleType Data.Value.Battles with
                | Some battleData ->
                    let time = World.getUpdateTime world
                    let clockTime = let t = World.getClockTime world in t.ToUnixTimeMilliseconds ()
                    let playTime = Option.getOrDefault clockTime field.FieldSongTimeOpt
                    let startTime = clockTime - playTime
                    let prizePool = { Consequents = consequents; Items = []; Gold = 0; Exp = 0 }
                    let battle = Battle.makeFromTeam field.Inventory prizePool (Field.getParty field) field.Options.BattleSpeed battleData time
                    let field = Field.clearSpirits field
                    let field = Field.updateFieldSongTimeOpt (constant (Some startTime)) field
                    let field = Field.updateBattleOpt (constant (Some battle)) field
                    withCmd (PlaySound (0L, Constants.Audio.SoundVolumeDefault, Assets.Field.BeastGrowlSound)) field
                | None -> just field

            | Interact ->
                match field.DialogOpt with
                | None ->
                    if isTouchingSavePoint field.Avatar world then
                        interactSavePoint field
                    else
                        match tryGetFacingProp field.Avatar world with
                        | Some prop ->
                            let prop = prop.GetProp world
                            match prop.PropData with
                            | Sprite _ -> just field
                            | Portal _ -> just field
                            | Door (_, keyItemTypeOpt, cue, _, requirements) -> interactDoor keyItemTypeOpt cue requirements prop field
                            | Chest (_, itemType, chestId, battleTypeOpt, cue, requirements) -> interactChest itemType chestId battleTypeOpt cue requirements prop field
                            | Switch (_, cue, cue2, requirements) -> interactSwitch cue cue2 requirements prop field
                            | Sensor _ -> just field
                            | Character (_, _, _, _, cue, _) -> interactCharacter cue prop field
                            | Npc (_, _, cue, requirements) -> interactNpc [{ Cue = cue; Requirements = Set.empty }] requirements prop field
                            | NpcBranching (_, _, branches, requirements) -> interactNpc branches requirements prop field
                            | Shopkeep (_, _, shopType, _) -> interactShopkeep shopType prop field
                            | Seal (_, cue, _) -> interactSeal cue field
                            | Flame _ -> just field
                            | SavePoint -> just field
                            | ChestSpawn -> just field
                            | EmptyProp -> just field
                        | None -> just field
                | Some dialog ->
                    interactDialog dialog field

        override this.Command (field, command, screen, world) =

            match command with
            | ProcessKeyInput ->
                if not (Simulants.Field.Scene.Feeler.GetTouched world) then
                    let avatar = Simulants.Field.Scene.Avatar
                    let force = v2Zero
                    let force = if KeyboardState.isKeyDown KeyboardKey.Right || KeyboardState.isKeyDown KeyboardKey.D then v2 Constants.Field.AvatarWalkForce 0.0f + force else force
                    let force = if KeyboardState.isKeyDown KeyboardKey.Left || KeyboardState.isKeyDown KeyboardKey.A  then v2 -Constants.Field.AvatarWalkForce 0.0f + force else force
                    let force = if KeyboardState.isKeyDown KeyboardKey.Up || KeyboardState.isKeyDown KeyboardKey.W then v2 0.0f Constants.Field.AvatarWalkForce + force else force
                    let force = if KeyboardState.isKeyDown KeyboardKey.Down || KeyboardState.isKeyDown KeyboardKey.S then v2 0.0f -Constants.Field.AvatarWalkForce + force else force
                    let world = avatar.Signal<Avatar, AvatarMessage, AvatarCommand> (cmd (TryTravel force)) world
                    let signal =
                        if KeyboardState.isKeyDown KeyboardKey.Right || KeyboardState.isKeyDown KeyboardKey.D then msg (TryFace Rightward)
                        elif KeyboardState.isKeyDown KeyboardKey.Left || KeyboardState.isKeyDown KeyboardKey.A then msg (TryFace Leftward)
                        elif KeyboardState.isKeyDown KeyboardKey.Up || KeyboardState.isKeyDown KeyboardKey.W then msg (TryFace Upward)
                        elif KeyboardState.isKeyDown KeyboardKey.Down || KeyboardState.isKeyDown KeyboardKey.S then msg (TryFace Downward)
                        else msg Nil
                    let world = avatar.Signal<Avatar, AvatarMessage, AvatarCommand> signal world
                    just world
                else just world

            | ProcessTouchInput position ->
                if  KeyboardState.isKeyUp KeyboardKey.Right && KeyboardState.isKeyUp KeyboardKey.D &&
                    KeyboardState.isKeyUp KeyboardKey.Left && KeyboardState.isKeyUp KeyboardKey.A &&
                    KeyboardState.isKeyUp KeyboardKey.Up && KeyboardState.isKeyUp KeyboardKey.W &&
                    KeyboardState.isKeyUp KeyboardKey.Down && KeyboardState.isKeyUp KeyboardKey.S then
                    let avatar = Simulants.Field.Scene.Avatar
                    let lowerCenter = field.Avatar.LowerCenter
                    let positionAbsolute = World.mouseToWorld false position world
                    let heading = positionAbsolute - lowerCenter
                    if heading.Length () >= 6.0f then // TODO: make constant DeadZoneRadius.
                        let goalNormalized = Vector2.Normalize heading
                        let force = goalNormalized * Constants.Field.AvatarWalkForceMouse
                        let world = avatar.Signal<Avatar, AvatarMessage, AvatarCommand> (cmd (TryTravel force)) world
                        let world = avatar.Signal<Avatar, AvatarMessage, AvatarCommand> (msg (TryFace (Direction.ofVector2 heading))) world
                        just world
                    else just world
                else just world

            | UpdateEye ->
                if World.getUpdateRate world <> 0L then
                    let world = World.setEyeCenter field.Avatar.Center world
                    let tileMapBounds = Simulants.Field.Scene.TileMap.GetBounds world
                    let eyeBounds = tileMapBounds.WithPosition (tileMapBounds.Position + v2Dup 48.0f)
                    let eyeBounds = eyeBounds.WithSize (tileMapBounds.Size - v2Dup 96.0f)
                    let world = World.constrainEyeBounds eyeBounds world
                    just world
                else just world

            | PlayFieldSong ->
                match Data.Value.Fields.TryGetValue field.FieldType with
                | (true, fieldData) ->
                    match (fieldData.FieldSongOpt, World.getCurrentSongOpt world) with
                    | (Some fieldSong, Some currentSong) ->
                        if not (AssetTag.equals fieldSong currentSong.Song) then
                            let (playTime, startTime) =
                                let clockTime = let t = World.getClockTime world in t.ToUnixTimeMilliseconds ()
                                match field.FieldSongTimeOpt with
                                | Some playTime ->
                                    let deltaTime = clockTime - playTime
                                    if playTime < Constants.Audio.SongResumptionMaximum
                                    then (playTime, deltaTime)
                                    else (0L, clockTime)
                                | None -> (0L, clockTime)
                            let fadeIn = if playTime <> 0L then Constants.Field.FieldSongFadeInMs else 0
                            let field = Field.updateFieldSongTimeOpt (constant (Some startTime)) field
                            let world = screen.SetField field world
                            withCmd (PlaySong (fadeIn, Constants.Audio.FadeOutMsDefault, Constants.Audio.SongVolumeDefault, double playTime / 1000.0, fieldSong)) world
                        else just world
                    | (Some fieldSong, None) ->
                        let (playTime, startTime) =
                            let clockTime = let t = World.getClockTime world in t.ToUnixTimeMilliseconds ()
                            match field.FieldSongTimeOpt with
                            | Some playTime ->
                                let deltaTime = clockTime - playTime
                                if playTime < Constants.Audio.SongResumptionMaximum
                                then (playTime, deltaTime)
                                else (0L, clockTime)
                            | None -> (0L, clockTime)
                        let fadeIn = if playTime <> 0L then Constants.Field.FieldSongFadeInMs else 0
                        let field = Field.updateFieldSongTimeOpt (constant (Some startTime)) field
                        let world = screen.SetField field world
                        withCmd (PlaySong (fadeIn, Constants.Audio.FadeOutMsDefault, Constants.Audio.SongVolumeDefault, double playTime / 1000.0, fieldSong)) world
                    | (None, _) -> just world
                | (false, _) -> just world

            | PlaySound (delay, volume, sound) ->
                let world = World.schedule (World.playSound volume sound) (World.getUpdateTime world + delay) world
                just world

            | PlaySong (fadeIn, fadeOut, volume, start, assetTag) ->
                let world = World.playSong fadeIn fadeOut volume start assetTag world
                just world

            | FadeOutSong fade ->
                let world = World.fadeOutSong fade world
                just world

            | Nop -> just world

        override this.Content (field, _) =

            [// scene group
             Content.group Simulants.Field.Scene.Group.Name []

                [// avatar
                 Content.entity<AvatarDispatcher> Simulants.Field.Scene.Avatar.Name
                    [Entity.Position == v2Dup 144.0f; Entity.Elevation == Constants.Field.ForegroundElevation; Entity.Size == Constants.Gameplay.CharacterSize
                     Entity.Enabled <== field --> fun field ->
                        field.Menu.MenuState = MenuClosed &&
                        Cue.notInterrupting field.Inventory field.Advents field.Cue &&
                        Option.isNone field.DialogOpt &&
                        Option.isNone field.ShopOpt &&
                        Option.isNone field.FieldTransitionOpt
                     Entity.LinearDamping == Constants.Field.LinearDamping
                     Entity.Avatar <== field --> fun field -> field.Avatar]

                 // props
                 Content.entities field
                    (fun field _ -> field.Props)
                    (fun _ prop _ -> Content.entity<PropDispatcher> Gen.name [Entity.Prop <== prop])

                 // spirit orb
                 Content.entityIf field (fun field -> Field.hasEncounters field && Cue.isNil field.Cue) $ fun field _ ->
                    Content.entity<SpiritOrbDispatcher> Gen.name
                        [Entity.Position == v2 -448.0f 48.0f; Entity.Elevation == Constants.Field.SpiritOrbElevation; Entity.Size == v2 192.0f 192.0f
                         Entity.SpiritOrb <== field --> fun field -> { AvatarLowerCenter = field.Avatar.LowerCenter; Spirits = field.Spirits; Chests = Field.getChests field; Portals = Field.getNonWarpPortals field }]

                 // backdrop sprite
                 Content.staticSprite Gen.name
                    [Entity.Bounds <== field --|> (fun _ world -> World.getViewBoundsAbsolute world); Entity.Elevation == Single.MinValue; Entity.Absolute == true
                     Entity.StaticImage == Assets.Default.Image9
                     Entity.Color <== field --> fun field ->
                        match Data.Value.Fields.TryGetValue field.FieldType with
                        | (true, fieldData) -> fieldData.FieldBackgroundColor
                        | (false, _) -> Color.Black]

                 // transition fade sprite
                 Content.staticSprite Gen.name
                    [Entity.Bounds <== field --|> (fun _ world -> World.getViewBoundsAbsolute world); Entity.Elevation == Single.MaxValue; Entity.Absolute == true
                     Entity.StaticImage == Assets.Default.Image9
                     Entity.Visible <== field --> fun field -> Option.isSome field.FieldTransitionOpt
                     Entity.Color <== field --|> fun field world ->
                        match field.FieldTransitionOpt with
                        | Some transition ->
                            let time = World.getUpdateTime world
                            let deltaTime = single transition.FieldTransitionTime - single time
                            let halfTransitionTime = single Constants.Field.TransitionTime * 0.5f
                            let progress =
                                if deltaTime < halfTransitionTime
                                then deltaTime / halfTransitionTime
                                else 1.0f - (deltaTime - halfTransitionTime) / halfTransitionTime
                            Color.Black.WithA (byte (progress * 255.0f))
                        | None -> Color.Zero]

                 // tmx map
                 Content.tmxMap Simulants.Field.Scene.TileMap.Name
                    [Entity.Elevation == Constants.Field.BackgroundElevation
                     Entity.TmxMap <== field --|> fun field world ->
                        match Map.tryFind field.FieldType Data.Value.Fields with
                        | Some fieldData ->
                            match FieldData.tryGetTileMap field.OmniSeedState fieldData world with
                            | Some tileMapChc ->
                                match tileMapChc with
                                | Choice1Of3 tileMap
                                | Choice2Of3 (tileMap, _)
                                | Choice3Of3 (tileMap, _) -> tileMap
                            | None -> failwithumf ()
                        | None -> failwithumf ()
                     Entity.TileIndexOffset <== field --> fun field ->
                         match Map.tryFind field.FieldType Data.Value.Fields with
                         | Some fieldData -> fieldData.FieldTileIndexOffset
                         | None -> failwithumf ()
                     Entity.TileIndexOffsetRange <== field --> fun field ->
                         match Map.tryFind field.FieldType Data.Value.Fields with
                         | Some fieldData -> fieldData.FieldTileIndexOffsetRange
                         | None -> failwithumf ()
                     Entity.TileLayerClearance == 10.0f]

                 // tmx map fade
                 Content.tmxMap Gen.name
                    [Entity.Elevation == Constants.Field.BackgroundElevation + 0.5f
                     Entity.Color <== field --> fun field ->
                        let progress = 1.0f - (Constants.Field.ConnectorFadeYMax - field.Avatar.Bottom.Y) / Constants.Field.ConnectorFadeYMax
                        let fade = min 1.0f progress
                        colWhite.ScaleA fade
                     Entity.TmxMap <== field --|> fun field world ->
                        match Map.tryFind field.FieldType Data.Value.Fields with
                        | Some fieldData ->
                            match FieldData.tryGetTileMap field.OmniSeedState fieldData world with
                            | Some tileMapChc ->
                                match tileMapChc with
                                | Choice1Of3 _ -> World.getTileMapMetadata Assets.Default.TileMapEmpty world |> __c
                                | Choice2Of3 (_, tileMapFade) -> tileMapFade
                                | Choice3Of3 (_, _) ->  World.getTileMapMetadata Assets.Default.TileMapEmpty world |> __c
                            | None -> World.getTileMapMetadata Assets.Default.TileMapEmpty world |> __c
                        | None -> World.getTileMapMetadata Assets.Default.TileMapEmpty world |> __c
                     Entity.TileLayerClearance == 10.0f]

                 // feeler
                 Content.feeler Simulants.Field.Scene.Feeler.Name
                    [Entity.Position == -Constants.Render.ResolutionF * 0.5f; Entity.Elevation == Constants.Field.FeelerElevation; Entity.Size == Constants.Render.ResolutionF
                     Entity.TouchingEvent ==|> fun evt -> cmd (ProcessTouchInput evt.Data)]

                 // menu button
                 Content.button Gen.name
                    [Entity.Position == v2 -456.0f -246.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 144.0f 48.0f
                     Entity.UpImage == Assets.Gui.ButtonShortUpImage; Entity.DownImage == Assets.Gui.ButtonShortDownImage
                     Entity.Text == "Menu"
                     Entity.Visible <== field --> fun field ->
                        field.Menu.MenuState = MenuClosed &&
                        Cue.notInterrupting field.Inventory field.Advents field.Cue &&
                        Option.isNone field.DialogOpt &&
                        Option.isNone field.ShopOpt &&
                        Option.isNone field.FieldTransitionOpt
                     Entity.ClickEvent ==> msg MenuTeamOpen]

                 // interact button
                 Content.button Gen.name
                    [Entity.Position == v2 306.0f -246.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 144.0f 48.0f
                     Entity.UpImage == Assets.Gui.ButtonShortUpImage; Entity.DownImage == Assets.Gui.ButtonShortDownImage
                     Entity.Visible <== field --|> fun field world ->
                        field.Menu.MenuState = MenuClosed &&
                        (Cue.notInterrupting field.Inventory field.Advents field.Cue || Option.isSome field.DialogOpt) &&
                        Option.isNone field.BattleOpt &&
                        Option.isNone field.ShopOpt &&
                        Option.isNone field.FieldTransitionOpt &&
                        Option.isSome (tryGetInteraction field.DialogOpt field.Advents field.Avatar field world)
                     Entity.Text <== field --|> fun field world ->
                        match tryGetInteraction field.DialogOpt field.Advents field.Avatar field world with
                        | Some interaction -> interaction
                        | None -> ""
                     Entity.ClickSoundOpt == None
                     Entity.ClickEvent ==> msg Interact]

                 // dialog
                 Content.entityIf field (fun field -> Option.isSome field.DialogOpt) $ fun field _ ->
                    Dialog.content Gen.name
                       Constants.Field.GuiElevation PromptLeft PromptRight
                       (field --> fun field -> (flip detokenize field, field.DialogOpt))

                 // team
                 Content.entityIf field (fun field -> match field.Menu.MenuState with MenuTeam _ -> true | _ -> false) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -255.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 900.0f 510.0f
                        Entity.LabelImage == Assets.Gui.DialogXXLImage]
                       [Content.sidebar (v2 24.0f 417.0f) 1.0f field
                        Content.team (v2 138.0f 417.0f) 1.0f Int32.MaxValue field tautology2 MenuTeamAlly
                        Content.label Gen.name
                           [Entity.PositionLocal == v2 438.0f 288.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 192.0f 192.0f
                            Entity.LabelImage <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeamData field.Team menu with
                                   | Some characterData ->
                                       match characterData.PortraitOpt with
                                       | Some portrait -> portrait
                                       | None -> Assets.Default.ImageEmpty
                                   | None -> Assets.Default.ImageEmpty
                               | _ -> Assets.Default.ImageEmpty]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 650.0f 372.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeamData field.Team menu with
                                   | Some characterData -> CharacterType.getName characterData.CharacterType
                                   | None -> ""
                               | _ -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 650.0f 336.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeammate field.Team menu with
                                   | Some teammate -> ArchetypeType.frenchName teammate.ArchetypeType + " Nv." + string (Algorithms.expPointsToLevel teammate.ExpPoints)
                                   | None -> ""
                               | _ -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 444.0f 234.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeammate field.Team menu with
                                   | Some teammate -> "Arme: " + Option.mapOrDefault WeaponType.frenchName "Aucune" teammate.WeaponOpt
                                   | None -> ""
                               | _ -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 444.0f 204.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeammate field.Team menu with
                                   | Some teammate -> "Armr: " + Option.mapOrDefault ArmorType.frenchName "Aucune" teammate.ArmorOpt
                                   | None -> ""
                               | _ -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 444.0f 174.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeammate field.Team menu with
                                   | Some teammate -> "Acc:  " + Option.mapOrDefault AccessoryType.frenchName "Aucun" (List.tryHead teammate.Accessories)
                                   | None -> ""
                               | _ -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 444.0f -84.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 512.0f 256.0f
                            Entity.Justification == Unjustified true
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuState with
                               | MenuTeam menu ->
                                   match MenuTeam.tryGetTeammateAndTeamData field.Team menu with
                                   | Some (teammate, characterData) ->
                                       let level = Algorithms.expPointsToLevel teammate.ExpPoints
                                       let hpm = Algorithms.hitPointsMax teammate.ArmorOpt characterData.ArchetypeType level
                                       let tpm = Algorithms.techPointsMax teammate.ArmorOpt characterData.ArchetypeType level
                                       let pow = Algorithms.power teammate.WeaponOpt Map.empty characterData.ArchetypeType level // no statuses outside battle
                                       let mag = Algorithms.magic teammate.WeaponOpt Map.empty characterData.ArchetypeType level // no statuses outside battle
                                       let def = Algorithms.shield Physical teammate.Accessories Map.empty characterData.ArchetypeType level // no statuses outside battle
                                       let abs = Algorithms.shield Magical teammate.Accessories Map.empty characterData.ArchetypeType level // no statuses outside battle
                                       "PV   "   + (string teammate.HitPoints).PadLeft 3 + " /" + (string hpm).PadLeft 3 +
                                       "\nPT   " + (string teammate.TechPoints).PadLeft 3 + " /" + (string tpm).PadLeft 3 +
                                       "\nPouv " + (string pow).PadLeft 3 + "    Mag " + (string mag).PadLeft 3 +
                                       "\nDef  " + (string def).PadLeft 3 + "    Abs " + (string abs).PadLeft 3 +
                                       "\nExp  " + (string teammate.ExpPoints).PadLeft 3 + " /" + (string (Algorithms.expPointsForNextLevel teammate.ExpPoints)).PadLeft 3
                                   | None -> ""
                               | _ -> ""]]

                 // inventory
                 Content.entityIf field (fun field -> match field.Menu.MenuState with MenuItem _ -> true | _ -> false) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -255.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 900.0f 510.0f
                        Entity.LabelImage == Assets.Gui.DialogXXLImage
                        Entity.Enabled <== field --> fun field -> Option.isNone field.Menu.MenuUseOpt]
                       [Content.sidebar (v2 24.0f 417.0f) 1.0f field
                        Content.items (v2 138.0f 417.0f) 1.0f 10 5 field MenuItemSelect
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 399.0f 24.0f; Entity.ElevationLocal == 1.0f
                            Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                            Entity.Text <== field --> (fun field -> string field.Inventory.Gold + " ors")]
                        Content.button Gen.name
                          [Entity.PositionLocal == v2 138.0f 12.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 72.0f 72.0f
                           Entity.Text == "<"
                           Entity.VisibleLocal <== field --> fun field -> Content.pageItems 10 field |> a__
                           Entity.UpImage == Assets.Gui.ButtonSmallUpImage
                           Entity.DownImage == Assets.Gui.ButtonSmallDownImage
                           Entity.ClickEvent ==> msg MenuItemsPageUp]
                        Content.button Gen.name
                          [Entity.PositionLocal == v2 777.0f 12.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 72.0f 72.0f
                           Entity.Text == ">"
                           Entity.VisibleLocal <== field --> fun field -> Content.pageItems 10 field |> _b_
                           Entity.UpImage == Assets.Gui.ButtonSmallUpImage
                           Entity.DownImage == Assets.Gui.ButtonSmallDownImage
                           Entity.ClickEvent ==> msg MenuInventoryPageDown]]

                 // tech team
                 Content.entityIf field (fun field -> match field.Menu.MenuState with MenuTech _ -> true | _ -> false) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -255.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 900.0f 510.0f
                        Entity.LabelImage == Assets.Gui.DialogXXLImage]
                       [Content.sidebar (v2 24.0f 417.0f) 1.0f field
                        Content.team (v2 138.0f 417.0f) 1.0f Int32.MaxValue field tautology2 MenuTechAlly
                        Content.techs (v2 466.0f 429.0f) 1.0f field MenuTechSelect]

                 // use
                 Content.entityIf field (fun field -> Option.isSome field.Menu.MenuUseOpt) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -216.0f; Entity.Elevation == Constants.Field.GuiElevation + 10.0f; Entity.Size == v2 900.0f 432.0f
                        Entity.LabelImage == Assets.Gui.DialogXLImage]
                       [Content.team (v2 160.0f 183.0f) 1.0f 3 field
                           (fun teammate menu ->
                               match menu.MenuUseOpt with
                               | Some menuUse -> Teammate.canUseItem (snd menuUse.MenuUseSelection) teammate
                               | None -> false)
                           MenuItemUse
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 810.0f 342.0f; Entity.ElevationLocal == 2.0f; Entity.Size == v2 72.0f 72.0f
                            Entity.UpImage == asset "Field" "CloseButtonUp"
                            Entity.DownImage == asset "Field" "CloseButtonDown"
                            Entity.ClickEvent ==> msg MenuItemCancel]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 36.0f 354.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== field --> fun field ->
                               match field.Menu.MenuUseOpt with
                               | Some menu -> menu.MenuUseLine1
                               | None -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 66.0f 312.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== field --> fun field ->
                                match field.Menu.MenuUseOpt with
                                | Some menu -> menu.MenuUseLine2
                                | None -> ""]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 66.0f 270.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== field --> fun field ->
                                match field.Menu.MenuUseOpt with
                                | Some menu -> menu.MenuUseLine3
                                | None -> ""]]

                 // options
                 Content.entityIf field (fun field -> match field.Menu.MenuState with MenuOptions -> true | _ -> false) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -255.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 900.0f 510.0f
                        Entity.LabelImage == Assets.Gui.DialogXXLImage]
                       [Content.sidebar (v2 24.0f 417.0f) 1.0f field
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 384.0f 432.0f; Entity.ElevationLocal == 1.0f
                            Entity.Text == "Mode de Combat"]
                        Content.radioButton Gen.name
                           [Entity.PositionLocal == v2 180.0f 372.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 144.0f 48.0f
                            Entity.UndialedImage == Assets.Gui.ButtonShortUpImage; Entity.DialedImage == Assets.Gui.ButtonShortDownImage
                            Entity.Text == "Tours"
                            Entity.Dialed <== field --> fun field -> match field.Options.BattleSpeed with WaitSpeed -> true | _ -> false
                            Entity.DialedEvent ==> msg (MenuOptionsSelectBattleSpeed WaitSpeed)]
                        Content.radioButton Gen.name
                           [Entity.PositionLocal == v2 408.0f 372.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 144.0f 48.0f
                            Entity.UndialedImage == Assets.Gui.ButtonShortUpImage; Entity.DialedImage == Assets.Gui.ButtonShortDownImage
                            Entity.Text == "Normal"
                            Entity.Dialed <== field --> fun field -> match field.Options.BattleSpeed with PacedSpeed -> true | _ -> false
                            Entity.DialedEvent ==> msg (MenuOptionsSelectBattleSpeed PacedSpeed)]
                        Content.radioButton Gen.name
                           [Entity.PositionLocal == v2 636.0f 372.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 144.0f 48.0f
                            Entity.UndialedImage == Assets.Gui.ButtonShortUpImage; Entity.DialedImage == Assets.Gui.ButtonShortDownImage
                            Entity.Text == "Rapide"
                            Entity.Dialed <== field --> fun field -> match field.Options.BattleSpeed with SwiftSpeed -> true | _ -> false
                            Entity.DialedEvent ==> msg (MenuOptionsSelectBattleSpeed SwiftSpeed)]]

                 // shop
                 Content.entityIf field (fun field -> Option.isSome field.ShopOpt) $ fun field _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -255.0f; Entity.Elevation == Constants.Field.GuiElevation; Entity.Size == v2 900.0f 510.0f
                        Entity.LabelImage == Assets.Gui.DialogXXLImage
                        Entity.Enabled <== field --> fun field -> match field.ShopOpt with Some shop -> Option.isNone shop.ShopConfirmOpt | None -> true]
                       [Content.items (v2 96.0f 347.0f) 1.0f 8 4 field ShopSelect
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 24.0f 438.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text == "Acheter"
                            Entity.VisibleLocal <== field --> fun field -> match field.ShopOpt with Some shop -> shop.ShopState = ShopSelling | None -> false
                            Entity.ClickEvent ==> msg ShopBuy]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 24.0f 438.0f; Entity.ElevationLocal == 1.0f
                            Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                            Entity.Text == "Que veux-tu?"
                            Entity.VisibleLocal <== field --> fun field -> match field.ShopOpt with Some shop -> shop.ShopState = ShopBuying | None -> false]
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 352.0f 438.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text == "Vendre"
                            Entity.VisibleLocal <== field --> fun field -> match field.ShopOpt with Some shop -> shop.ShopState = ShopBuying | None -> false
                            Entity.ClickEvent ==> msg ShopSell]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 352.0f 438.0f; Entity.ElevationLocal == 1.0f
                            Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                            Entity.Text == "Que vends-tu?"
                            Entity.VisibleLocal <== field --> fun field -> match field.ShopOpt with Some shop -> shop.ShopState = ShopSelling | None -> false]
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 678.0f 438.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text == "Quitter"
                            Entity.ClickEvent ==> msg ShopLeave]
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 24.0f 15.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 72.0f 72.0f
                            Entity.Text == "<"
                            Entity.VisibleLocal <== field --> fun field -> Content.pageItems 8 field |> a__
                            Entity.UpImage == Assets.Gui.ButtonSmallUpImage
                            Entity.DownImage == Assets.Gui.ButtonSmallDownImage
                            Entity.ClickEvent ==> msg ShopPageUp]
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 804.0f 15.0f; Entity.ElevationLocal == 1.0f; Entity.Size == v2 72.0f 72.0f
                            Entity.Text == ">"
                            Entity.VisibleLocal <== field --> fun field -> Content.pageItems 8 field |> _b_
                            Entity.UpImage == Assets.Gui.ButtonSmallUpImage
                            Entity.DownImage == Assets.Gui.ButtonSmallDownImage
                            Entity.ClickEvent ==> msg ShopPageDown]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 352.0f 3.0f; Entity.ElevationLocal == 1.0f
                            Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                            Entity.Text <== field --> (fun field -> string field.Inventory.Gold + " ors")]]

                 // confirm
                 Content.entityOpt (field --> fun field -> match field.ShopOpt with Some shop -> shop.ShopConfirmOpt | None -> None) $ fun shopConfirm _ ->
                    Content.panel Gen.name
                       [Entity.Position == v2 -450.0f -128.0f; Entity.Elevation == Constants.Field.GuiElevation + 10.0f; Entity.Size == v2 900.0f 252.0f
                        Entity.LabelImage == Assets.Gui.DialogFatImage]
                       [Content.button Gen.name
                           [Entity.PositionLocal == v2 198.0f 36.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text == "Accepter"
                            Entity.ClickEvent ==> msg ShopConfirmAccept]
                        Content.button Gen.name
                           [Entity.PositionLocal == v2 498.0f 36.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text == "Refuser"
                            Entity.ClickEvent ==> msg ShopConfirmDecline]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 30.0f 180.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== shopConfirm --> fun shopConfirm -> shopConfirm.ShopConfirmOffer]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 60.0f 138.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== shopConfirm --> fun shopConfirm -> shopConfirm.ShopConfirmLine1]
                        Content.text Gen.name
                           [Entity.PositionLocal == v2 60.0f 96.0f; Entity.ElevationLocal == 2.0f
                            Entity.Text <== shopConfirm --> fun shopConfirm -> shopConfirm.ShopConfirmLine2]]]]

    type DebugFieldDispatcher () =
        inherit FieldDispatcher ()
        override this.Prepare (_, world) = Field.debug world