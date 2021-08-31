﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.IO
open Prime
open Nu
open Nu.Declarative
open OmniBlade

[<AutoOpen>]
module GameDispatcher =

    type [<StructuralEquality; NoComparison>] Gui =
        | Splashing
        | Title
        | Credits
        | Pick
        | Intro of SaveSlot

    type [<StructuralEquality; NoComparison>] Omni =
        | Gui of Gui
        | Field of Field

    type [<NoEquality; NoComparison>] OmniMessage =
        | Change of Omni
        | ChangeField of Field
        | ChangeBattle of Battle
        | Update
        | ToIntro of SaveSlot
        | FromIntro
        | Nop

    type [<NoEquality; NoComparison>] OmniCommand =
        | Picks
        | TryFullScreen of KeyboardKeyData
        | Show of Screen
        | Exit

    type Game with
        member this.GetOmni = this.GetModel<Omni>
        member this.SetOmni = this.SetModel<Omni>
        member this.Omni = this.Model<Omni> ()

    type OmniDispatcher () =
        inherit GameDispatcher<Omni, OmniMessage, OmniCommand> (Gui Splashing)

        override this.Register (game, world) =
            let world = World.hintRenderPackageUse Assets.Gui.PackageName world
            let world = World.hintAudioPackageUse Assets.Gui.PackageName world
#if DEV
            let world = World.setMasterSongVolume 0.0f world
#endif
            base.Register (game, world)

        override this.Channel (_, _) =
            [Simulants.Field.Screen.Field.ChangeEvent =|> fun evt -> msg (ChangeField (evt.Data.Value :?> Field))
             Simulants.Battle.Screen.Battle.ChangeEvent =|> fun evt -> msg (ChangeBattle (evt.Data.Value :?> Battle))
             Simulants.Game.UpdateEvent => msg Update
             Simulants.Game.UpdateEvent => cmd Picks
             Simulants.Title.Gui.Play.ClickEvent =|> fun _ -> msg (Change (Gui Pick))
             Simulants.Title.Gui.Credits.ClickEvent => msg (Change (Gui Credits))
             Simulants.Pick.Gui.NewGame1.ClickEvent => msg (ToIntro Slot1)
             Simulants.Pick.Gui.NewGame2.ClickEvent => msg (ToIntro Slot2)
             Simulants.Pick.Gui.NewGame3.ClickEvent => msg (ToIntro Slot3)
             Simulants.Pick.Gui.LoadGame1.ClickEvent =|> fun _ -> msg (match Field.tryLoad Slot1 with Some loaded -> Change (Field loaded) | None -> Nop)
             Simulants.Pick.Gui.LoadGame2.ClickEvent =|> fun _ -> msg (match Field.tryLoad Slot2 with Some loaded -> Change (Field loaded) | None -> Nop)
             Simulants.Pick.Gui.LoadGame3.ClickEvent =|> fun _ -> msg (match Field.tryLoad Slot3 with Some loaded -> Change (Field loaded) | None -> Nop)
             Simulants.Pick.Gui.Back.ClickEvent => msg (Change (Gui Title))
             Simulants.Intro5.Screen.DeselectEvent => msg FromIntro
             Simulants.Credits.Gui.Back.ClickEvent => msg (Change (Gui Title))
             Simulants.Title.Gui.Exit.ClickEvent => cmd Exit
             Events.KeyboardKeyDown =|> fun keyEvent -> cmd (TryFullScreen keyEvent.Data)]

        override this.Message (omni, message, _, world) =

            match message with
            | Change omni ->
                just omni

            | ChangeField field ->
                match omni with
                | Gui _ -> just omni
                | Field _ -> just (Field field)

            | ChangeBattle battle ->
                match omni with
                | Gui _ -> just omni
                | Field field ->
                    match field.BattleOpt with
                    | None -> just omni
                    | Some _ -> just (Field (Field.updateBattleOpt (constant (Some battle)) field))

            | Update ->
                match omni with
                | Gui gui ->
                    match gui with
                    | Splashing -> just omni
                    | Title -> withCmd (Show Simulants.Title.Screen) omni
                    | Credits -> withCmd (Show Simulants.Credits.Screen) omni
                    | Pick -> withCmd (Show Simulants.Pick.Screen) omni
                    | Intro _ -> just omni
                | Field field ->
                    match field.BattleOpt with
                    | Some battle ->
                        match battle.BattleState with
                        | BattleCease (result, consequents, time) ->
                            let localTime = World.getTickTime world - time
                            if localTime = 60L then
                                let field = Field (Field.synchronizeFromBattle consequents battle field)
                                if result
                                then withCmd (Show Simulants.Field.Screen) field
                                else withCmd (Show Simulants.Title.Screen) field
                            elif localTime = 60L + Constants.Gui.Dissolve.OutgoingTime then
                                if result
                                then just (Field (Field.updateBattleOpt (constant None) field))
                                else just (Gui Title)
                            else withCmd (Show Simulants.Battle.Screen) omni
                        | _ -> withCmd (Show Simulants.Battle.Screen) omni
                    | None -> withCmd (Show Simulants.Field.Screen) omni

            | ToIntro saveSlot ->
                let introMsg = msg (Change (Gui (Intro saveSlot)))
                let introCmd = cmd (Show Simulants.Intro.Screen)
                withSigs [introMsg; introCmd] omni

            | FromIntro ->
                match omni with
                | Gui gui ->
                    match gui with
                    | Intro saveSlot -> withMsg (Change (Field (Field.initial saveSlot Gen.randomul))) omni
                    | _ -> just omni
                | Field _ -> just omni

            | Nop -> just omni

        override this.Command (_, command, _, world) =
            match command with
            | Picks ->
                if Simulants.Pick.Screen.IsSelected world then
                    let world = Simulants.Pick.Gui.NewGame1.SetVisible (not (File.Exists Assets.Global.SaveFilePath1)) world
                    let world = Simulants.Pick.Gui.NewGame2.SetVisible (not (File.Exists Assets.Global.SaveFilePath2)) world
                    let world = Simulants.Pick.Gui.NewGame3.SetVisible (not (File.Exists Assets.Global.SaveFilePath3)) world
                    let world = Simulants.Pick.Gui.LoadGame1.SetVisible (File.Exists Assets.Global.SaveFilePath1) world
                    let world = Simulants.Pick.Gui.LoadGame2.SetVisible (File.Exists Assets.Global.SaveFilePath2) world
                    let world = Simulants.Pick.Gui.LoadGame3.SetVisible (File.Exists Assets.Global.SaveFilePath3) world
                    just world
                else just world
            | TryFullScreen keyData ->
                if KeyboardState.isAltDown () && keyData.Down && keyData.KeyboardKey = KeyboardKey.Return then
                    match World.tryGetWindowFullScreen world with
                    | Some fullScreen -> just (World.trySetWindowFullScreen (not fullScreen) world)
                    | None -> just world
                else just world
            | Show screen ->
                World.transitionScreen screen world |> just
            | Exit ->
                World.exit world |> just

        override this.Content (omni, _) =

            [// splash
             Content.screen Simulants.Splash.Screen.Name (Splash (Constants.Gui.Dissolve, Constants.Gui.Splash, None, Simulants.Title.Screen)) [] []

             // title
             Content.screenFromGroupFile Simulants.Title.Screen.Name (Dissolve (Constants.Gui.Dissolve, Some Assets.Gui.TitleSong)) Assets.Gui.TitleGroupFilePath

             // pick
             Content.screenFromGroupFile Simulants.Pick.Screen.Name (Dissolve ({ Constants.Gui.Dissolve with OutgoingTime = 90L }, Some Assets.Gui.TitleSong)) Assets.Gui.PickGroupFilePath

             // intros
             Content.screenFromGroupFile Simulants.Intro.Screen.Name (Splash (Constants.Intro.Dissolve, Constants.Intro.Splash, Some Assets.Gui.IntroSong, Simulants.Intro2.Screen)) Assets.Gui.IntroGroupFilePath
             Content.screenFromGroupFile Simulants.Intro2.Screen.Name (Splash (Constants.Intro.Dissolve, Constants.Intro.Splash, Some Assets.Gui.IntroSong, Simulants.Intro3.Screen)) Assets.Gui.Intro2GroupFilePath
             Content.screenFromGroupFile Simulants.Intro3.Screen.Name (Splash (Constants.Intro.Dissolve, Constants.Intro.Splash, Some Assets.Gui.IntroSong, Simulants.Intro4.Screen)) Assets.Gui.Intro3GroupFilePath
             Content.screenFromGroupFile Simulants.Intro4.Screen.Name (Splash (Constants.Intro.Dissolve, Constants.Intro.Splash, Some Assets.Gui.IntroSong, Simulants.Intro5.Screen)) Assets.Gui.Intro4GroupFilePath
             Content.screenFromGroupFile Simulants.Intro5.Screen.Name (Splash (Constants.Intro.Dissolve, Constants.Intro.Splash, Some Assets.Gui.IntroSong, Simulants.Field.Screen)) Assets.Gui.Intro5GroupFilePath

             // credits
             Content.screenFromGroupFile Simulants.Credits.Screen.Name (Dissolve (Constants.Gui.Dissolve, Some Assets.Gui.TitleSong)) Assets.Gui.CreditsGroupFilePath

             // field
             Content.screen<FieldDispatcher> Simulants.Field.Screen.Name (Dissolve (Constants.Gui.Dissolve, None))
                [Screen.Field <== omni --> fun omni ->
                    match omni with
                    | Gui _ -> Field.empty
                    | Field field -> field] []

             // battle
             Content.screen<BattleDispatcher> Simulants.Battle.Screen.Name (Dissolve (Constants.Gui.Dissolve, None))
                [Screen.Battle <== omni --> fun omni ->
                    match omni with
                    | Gui _ -> Battle.empty
                    | Field field -> Option.getOrDefault Battle.empty field.BattleOpt] []]