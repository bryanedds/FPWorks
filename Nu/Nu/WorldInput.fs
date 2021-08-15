﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldInputModule =

    type World with

        /// Convert a MouseButton to SDL's representation.
        static member internal toSdlMouseButton mouseButton =
            MouseState.toSdlButton mouseButton

        /// Convert SDL's representation of a mouse button to a MouseButton.
        static member internal toNuMouseButton mouseButton =
            MouseState.toNuButton mouseButton

        /// Check that the given mouse button is down.
        [<FunctionBinding>]
        static member isMouseButtonDown mouseButton world =
            ignore (world : World)
            MouseState.isButtonDown mouseButton

        /// Get the position of the mouse.
        [<FunctionBinding>]
        static member getMousePosition world =
            let eyeMargin = World.getEyeMargin world
            MouseState.getPosition () - eyeMargin

        /// Check that the given keyboard key is down.
        [<FunctionBinding>]
        static member isKeyboardKeyDown key world =
            ignore (world : World)
            KeyboardState.isKeyDown key

        // TODO: implement isKeyboardModifierActive.