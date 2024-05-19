// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2023.

namespace OmniBlade
open System
open System.Numerics
open Prime
open Nu

type Title =
    { TitleTime : int64 }

    static member empty =
        { TitleTime = 0L }

    static member initial =
        { TitleTime = 0L }

type TitleMessage =
    | Scroll
    interface Message

[<AutoOpen>]
module TitleExtensions =
    type Screen with
        member this.GetTitle world = this.GetModelGeneric<Title> world
        member this.SetTitle value world = this.SetModelGeneric<Title> value world
        member this.Title = this.ModelGeneric<Title> ()

type TitleDispatcher () =
    inherit ScreenDispatcher<Title, TitleMessage, Command> (Title.empty)

    let scroll index which title =
        let offsetX = single Constants.Render.VirtualResolution.X * 0.5f * if which then -1.0f else 1.0f
        let offsetY = single Constants.Render.VirtualResolution.Y * -0.5f
        let progress = single (title.TitleTime / inc (int64 index)) % single Constants.Render.VirtualResolution.X / single Constants.Render.VirtualResolution.X
        let scroll = progress * single Constants.Render.VirtualResolution.X
        v3 (offsetX - scroll) offsetY 0.0f

    override this.Definitions (_, _) =
        [Screen.UpdateEvent => Scroll]

    override this.Message (title, message, _, _) =

        match message with
        | Scroll ->
            let title = { title with TitleTime = inc title.TitleTime }
            just title

    override this.Content (title, _) =
        [Content.group "TitleScene" []
            [Content.staticSprite "TitleForeground" [Entity.StaticImage == asset "Gui" "TitleForeground"; Entity.Position := scroll 0 false title; Entity.Elevation == 0.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleForeground2" [Entity.StaticImage == asset "Gui" "TitleForeground"; Entity.Position := scroll 0 true title; Entity.Elevation == 0.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleGround" [Entity.StaticImage == asset "Gui" "TitleGround"; Entity.Position := scroll 1 false title; Entity.Elevation == -1.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleGround2" [Entity.StaticImage == asset "Gui" "TitleGround"; Entity.Position := scroll 1 true title; Entity.Elevation == -1.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleForest" [Entity.StaticImage == asset "Gui" "TitleForest"; Entity.Position := scroll 2 false title; Entity.Elevation == -2.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleForest2" [Entity.StaticImage == asset "Gui" "TitleForest"; Entity.Position := scroll 2 true title; Entity.Elevation == -2.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleMountains" [Entity.StaticImage == asset "Gui" "TitleMountains"; Entity.Position := scroll 3 false title; Entity.Elevation == -3.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleMountains2" [Entity.StaticImage == asset "Gui" "TitleMountains"; Entity.Position := scroll 3 true title; Entity.Elevation == -3.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleClouds" [Entity.StaticImage == asset "Gui" "TitleClouds"; Entity.Position := scroll 4 false title; Entity.Elevation == -4.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleClouds2" [Entity.StaticImage == asset "Gui" "TitleClouds"; Entity.Position := scroll 4 true title; Entity.Elevation == -4.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleSky" [Entity.StaticImage == asset "Gui" "TitleSky"; Entity.Position := scroll 5 false title; Entity.Elevation == -5.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]
             Content.staticSprite "TitleSky2" [Entity.StaticImage == asset "Gui" "TitleSky"; Entity.Position := scroll 5 true title; Entity.Elevation == -5.0f; Entity.Size == Constants.Render.VirtualResolution.V3; Entity.Absolute == true]]]