﻿namespace BlazeVector
open Prime
open Nu
open BlazeVector

[<AutoOpen>]
module BlazeDispatcher =

    // this is our Elm-style command type. To learn about the Elm-style, read this article here -
    // https://vsyncronicity.com/2020/03/01/a-game-engine-in-the-elm-style/
    type BlazeCommand =
        | PlaySplashSound
        | ShowTitle
        | ShowCredits
        | ShowGameplay
        | ExitGame

    // this is our main game type implemented in the Elm-style. 
    type BlazeDispatcher () =
        inherit GameDispatcher<unit, unit, BlazeCommand> (())

        // here we channel from events to signals
        override this.Channel (_, _) =
            [Simulants.Splash.Screen.SelectEvent => cmd PlaySplashSound
             Simulants.Title.Gui.Credits.ClickEvent => cmd ShowCredits
             Simulants.Title.Gui.Play.ClickEvent => cmd ShowGameplay
             Simulants.Title.Gui.Exit.ClickEvent => cmd ExitGame
             Simulants.Credits.Gui.Back.ClickEvent => cmd ShowTitle
             Simulants.Gameplay.Gui.Back.ClickEvent => cmd ShowTitle]

        // here we handle the above commands
        override this.Command (_, command, _, world) =
            let world =
                match command with
                | PlaySplashSound -> World.playSound Constants.Audio.SoundVolumeDefault Assets.Gui.SplashSound world
                | ShowTitle -> World.transitionScreen Simulants.Title.Screen world
                | ShowCredits -> World.transitionScreen Simulants.Credits.Screen world
                | ShowGameplay -> World.transitionScreen Simulants.Gameplay.Screen world
                | ExitGame -> World.exit world
            just world

        // here we describe the content of the game including all of its screens.
        override this.Content (_, _) =
            [Content.screen Simulants.Splash.Screen.Name (Splash (Constants.Dissolve.Default, Constants.Splash.Default, None, Simulants.Title.Screen)) [] []
             Content.screenFromGroupFile Simulants.Title.Screen.Name (Dissolve (Constants.Dissolve.Default, Some Assets.Gui.MachinerySong)) Assets.Gui.TitleGroupFilePath
             Content.screenFromGroupFile Simulants.Credits.Screen.Name (Dissolve (Constants.Dissolve.Default, Some Assets.Gui.MachinerySong)) Assets.Gui.CreditsGroupFilePath
             Content.screenFromGroupFile<GameplayDispatcher> Simulants.Gameplay.Screen.Name (Dissolve (Constants.Dissolve.Default, (Some Assets.Gameplay.DeadBlazeSong))) Assets.Gui.GameplayGroupFilePath]

        // here we hint to the renderer and audio system that the 'Gui' package should be loaded
        override this.Register (game, world) =
            let world = World.hintRenderPackageUse Assets.Gui.PackageName world
            let world = World.hintAudioPackageUse Assets.Gui.PackageName world
            base.Register (game, world)