﻿namespace Twenty48
open System
open Prime
open Nu

[<AutoOpen>]
module GameplayDispatcher =

    // this is our MMCC message type.
    type GameplayMessage =
        | TryShift of Direction
        | StartQuitting
        | FinishQuitting
        | Nil
        interface Message

    // this extends the Screen API to expose the above Gameplay model.
    type Screen with
        member this.GetGameplay world = this.GetModelGeneric<Gameplay> world
        member this.SetGameplay value world = this.SetModelGeneric<Gameplay> value world
        member this.Gameplay = this.ModelGeneric<Gameplay> ()

    // this is the screen dispatcher that defines the screen where gameplay takes place
    type GameplayDispatcher () =
        inherit ScreenDispatcher<Gameplay, GameplayMessage, Command> (Gameplay.empty)

        // here we define the screen's properties and event handling
        override this.Initialize (_, _) =
            [Screen.DeselectingEvent => FinishQuitting
             Game.KeyboardKeyDownEvent =|> fun evt ->
                if not evt.Data.Repeated then
                    match evt.Data.KeyboardKey with
                    | KeyboardKey.Up -> TryShift Upward
                    | KeyboardKey.Down -> TryShift Downward
                    | KeyboardKey.Left -> TryShift Leftward
                    | KeyboardKey.Right -> TryShift Rightward
                    | _ -> Nil
                else Nil]

        // here we handle the above messages
        override this.Message (gameplay, message, _, world) =

            match message with
            | TryShift direction ->
                if world.Advancing && gameplay.State = Playing then
                    let gameplay' =
                        match direction with
                        | Upward -> Gameplay.shiftUp gameplay
                        | Rightward -> Gameplay.shiftRight gameplay
                        | Downward -> Gameplay.shiftDown gameplay
                        | Leftward -> Gameplay.shiftLeft gameplay
                    if Gameplay.detectTileChange gameplay gameplay' then
                        let gameplay = Gameplay.addTile gameplay'
                        if not (Gameplay.detectMoveAvailability gameplay)
                        then just { gameplay with State = GameOver }
                        else just gameplay
                    else just gameplay
                else just gameplay

            | StartQuitting ->
                match gameplay.State with
                | Playing | GameOver -> just { gameplay with State = Quitting }
                | Quitting | Quit -> just gameplay

            | FinishQuitting ->
                just { gameplay with State = Quit }

            | Nil ->
                just gameplay

        // here we describe the content of the game including the level, the hud, and the player
        override this.Content (gameplay, _) =

            [// the gui group
             Content.group Simulants.GameplayGui.Name []
                [Content.button Simulants.GameplayQuit.Name
                    [Entity.Position == v3 336.0f -216.0f 0.0f
                     Entity.Elevation == 10.0f
                     Entity.Text == "Quit"
                     Entity.ClickEvent => StartQuitting]]

             // the scene group
             Content.group Simulants.GameplayScene.Name []

                [// background
                 Content.staticSprite "Background"
                    [Entity.Size == Constants.Render.VirtualResolution.V3
                     Entity.StaticImage == Assets.Default.White
                     Entity.Color == Color.GhostWhite]

                 // score
                 Content.text "Score"
                    [Entity.Position == v3 350.0f 232.0f 0.0f
                     Entity.Elevation == 10.0f
                     Entity.Text := "Score: " + string gameplay.Score]

                 // game over
                 match gameplay.State with
                 | GameOver | Quitting ->
                    Content.text "GameOver"
                        [Entity.Position == v3 0.0f 232.0f 0.0f
                         Entity.Elevation == 10.0f
                         Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                         Entity.Text == "Game Over!"]
                 | Playing | Quit -> ()

                 // board
                 let gutter = v3 5.0f 5.0f 0.0f
                 let tileSize = v3 48.0f 48.0f 0.0f
                 let tileOffset = (gameplay.BoardSize.V3 * tileSize + gutter * (gameplay.BoardSize.V3 - v3One)) * -0.5f
                 Content.panel Simulants.GameplayBoard.Name
                    [Entity.Size == v3 217.0f 217.0f 0.0f
                     Entity.Elevation == 1.0f
                     Entity.LabelImage == Assets.Gameplay.BoardImage]
                    [for tile in gameplay.Tiles do
                        Content.text ("Tile+" + string tile.TileId)
                            [Entity.PositionLocal := tile.Position.V3 * (tileSize + gutter) + tileSize * 0.5f + tileOffset
                             Entity.Size == tileSize
                             Entity.ElevationLocal == 1.0f
                             Entity.Text := string tile.Value
                             Entity.Justification == Justified (JustifyCenter, JustifyMiddle)
                             Entity.Font == Assets.Gui.ClearSansFont
                             Entity.FontSizing := if tile.Value < 16384 then Some 18 else Some 12
                             Entity.TextColor := if tile.Value < 8 then Color.Gray else Color.GhostWhite
                             Entity.BackdropImageOpt := Some (Assets.Gameplay.TileImage tile.Value)]]]]