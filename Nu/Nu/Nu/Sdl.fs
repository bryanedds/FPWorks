﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2015.

namespace Nu
open System
open System.Diagnostics
open System.Threading
open SDL2
open Prime
open Nu

/// Describes the initial configuration of a window created via SDL.
type SdlWindowConfig =
    { WindowTitle : string
      WindowX : int
      WindowY : int
      WindowFlags : SDL.SDL_WindowFlags }

/// Describes the view that SDL will use to render.
type SdlViewConfig =
    | NewWindow of SdlWindowConfig
    | ExistingWindow of nativeint
    //| FullScreen TODO: implement

/// Describes the general configuration of SDL.
type SdlConfig =
    { ViewConfig : SdlViewConfig
      ViewW : int
      ViewH : int
      RendererFlags : SDL.SDL_RendererFlags
      AudioChunkSize : int }

/// The dependencies needed to initialize SDL.
type [<ReferenceEquality>] SdlDeps =
    private
        { OptRenderContext : nativeint option
          OptWindow : nativeint option
          Config : SdlConfig
          Destroy : unit -> unit }
    interface IDisposable with
        member this.Dispose () =
            this.Destroy ()

[<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module SdlViewConfig =

    /// A default SdlViewConfig.
    let defaultConfig =
        NewWindow
            { WindowTitle = "Nu Game"
              WindowX = SDL.SDL_WINDOWPOS_UNDEFINED
              WindowY = SDL.SDL_WINDOWPOS_UNDEFINED
              WindowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN }

[<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module SdlConfig =

    /// A default SdlConfig.
    let defaultConfig =
        { ViewConfig = SdlViewConfig.defaultConfig
          ViewW = Constants.Render.ResolutionX
          ViewH = Constants.Render.ResolutionY
          RendererFlags = enum<SDL.SDL_RendererFlags> (int SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED ||| int SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC)
          AudioChunkSize = Constants.Audio.AudioBufferSizeDefault }

[<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module SdlDeps =

    /// An empty SdlDeps.
    let empty =
        { OptRenderContext = None
          OptWindow = None
          Config = SdlConfig.defaultConfig
          Destroy = id }

    /// Get an sdlDep's opt render context.
    let getOptRenderContext sdlDeps =
        sdlDeps.OptRenderContext

    /// Get an sdlDep's opt window.
    let getOptWindow sdlDeps =
        sdlDeps.OptWindow

    /// Get an sdlDep's config.
    let getConfig sdlDeps =
        sdlDeps.Config

    /// Attempt to initalize an SDL module.
    let internal attemptPerformSdlInit create destroy =
        let initResult = create ()
        let error = SDL.SDL_GetError ()
        if initResult = 0 then Right ((), destroy)
        else Left error

    /// Attempt to initalize an SDL resource.
    let internal attemptMakeSdlResource create destroy =
        let resource = create ()
        if resource <> IntPtr.Zero then Right (resource, destroy)
        else
            let error = "SDL2# resource creation failed due to '" + SDL.SDL_GetError () + "'."
            Left error

    /// Attempt to initalize a global SDL resource.
    let internal attemptMakeSdlGlobalResource create destroy =
        let resource = create ()
        if resource = 0 then Right ((), destroy)
        else
            let error = "SDL2# global resource creation failed due to '" + SDL.SDL_GetError () + "'."
            Left error

    /// Attempt to make an SdlDeps instance.
    let attemptMake sdlConfig =
        // TODO: define either { } cexpr...
        match attemptPerformSdlInit
            (fun () -> SDL.SDL_Init SDL.SDL_INIT_EVERYTHING)
            (fun () -> SDL.SDL_Quit ()) with
        | Left error -> Left error
        | Right ((), destroy) ->
            match attemptMakeSdlResource
                (fun () ->
                    match sdlConfig.ViewConfig with
                    | NewWindow windowConfig -> SDL.SDL_CreateWindow (windowConfig.WindowTitle, windowConfig.WindowX, windowConfig.WindowY, sdlConfig.ViewW, sdlConfig.ViewH, windowConfig.WindowFlags)
                    | ExistingWindow hwindow -> SDL.SDL_CreateWindowFrom hwindow)
                (fun window -> SDL.SDL_DestroyWindow window; destroy ()) with
            | Left error -> Left error
            | Right (window, destroy) ->
                match attemptMakeSdlResource
                    (fun () -> SDL.SDL_CreateRenderer (window, -1, uint32 sdlConfig.RendererFlags))
                    (fun renderContext -> SDL.SDL_DestroyRenderer renderContext; destroy window) with
                | Left error -> Left error
                | Right (renderContext, destroy) ->
                    match attemptMakeSdlGlobalResource
                        (fun () -> SDL_ttf.TTF_Init ())
                        (fun () -> SDL_ttf.TTF_Quit (); destroy renderContext) with
                    | Left error -> Left error
                    | Right ((), destroy) ->
                        match attemptMakeSdlGlobalResource
#if MIX_INIT_OGG
                            (fun () -> SDL_mixer.Mix_Init SDL_mixer.MIX_InitFlags.MIX_INIT_OGG) // NOTE: for some reason this line fails on 32-bit builds.. WHY?
#else
                            (fun () -> SDL_mixer.Mix_Init ^ enum<SDL_mixer.MIX_InitFlags> 0)
#endif
                            (fun () -> SDL_mixer.Mix_Quit (); destroy ()) with
                        | Left error -> Left error
                        | Right ((), destroy) ->
                            match attemptMakeSdlGlobalResource
                                (fun () -> SDL_mixer.Mix_OpenAudio (Constants.Audio.AudioFrequency, SDL_mixer.MIX_DEFAULT_FORMAT, SDL_mixer.MIX_DEFAULT_CHANNELS, sdlConfig.AudioChunkSize))
                                (fun () -> SDL_mixer.Mix_CloseAudio (); destroy ()) with
                            | Left error -> Left error
                            | Right ((), destroy) ->
                                let sdlDeps = { OptRenderContext = Some renderContext; OptWindow = Some window; Config = sdlConfig; Destroy = destroy }
                                Right sdlDeps

[<RequireQualifiedAccess>]
module Sdl =

    let private resourceNop (_ : nativeint) = ()

    /// Update the game engine's state.
    let update handleEvent handleUpdate world =
        if SDL.SDL_WasInit SDL.SDL_INIT_TIMER <> 0u then
            let mutable result = (Running, world)
            let polledEvent = ref ^ SDL.SDL_Event ()
            while
                SDL.SDL_PollEvent polledEvent <> 0 &&
                (match fst result with Running -> true | Exiting -> false) do
                result <- handleEvent !polledEvent (snd result)
            match fst result with
            | Exiting -> ()
            | Running -> result <- handleUpdate (snd result)
            result
        else handleUpdate world

    /// Render the game engine's current frame.
    let render handleRender sdlDeps world =
        match sdlDeps.OptRenderContext with
        | Some renderContext ->
            match Constants.Render.ScreenClearing with
            | NoClear -> ()
            | ColorClear (r, g, b) ->
                SDL.SDL_SetRenderDrawColor (renderContext, r, g, b, 255uy) |> ignore
                SDL.SDL_RenderClear renderContext |> ignore
            let world = handleRender world
            SDL.SDL_RenderPresent renderContext
            world
        | None -> handleRender world

    /// Play the game engine's current audio.
    let play handlePlay world =
        if SDL.SDL_WasInit SDL.SDL_INIT_AUDIO <> 0u
        then handlePlay world // doesn't need any extra sdl processing here
        else handlePlay world

    /// Run the game engine with the given handlers, but don't clean up at the end, and return the world.
    let rec runWithoutCleanUp runWhile handleEvent handleUpdate handleRender handlePlay sdlDeps liveness world =
        if runWhile world then
            match liveness with
            | Running ->
                let (liveness, world) = update handleEvent handleUpdate world
                match liveness with
                | Running ->
                    let world = render handleRender sdlDeps world
                    let world = play handlePlay world
                    runWithoutCleanUp runWhile handleEvent handleUpdate handleRender handlePlay sdlDeps liveness world
                | Exiting -> world
            | Exiting -> world
        else world

    /// Run the game engine with the given handlers.
    let run8 runWhile handleEvent handleUpdate handleRender handlePlay handleExit sdlDeps liveness world =
        try let world = runWithoutCleanUp runWhile handleEvent handleUpdate handleRender handlePlay sdlDeps liveness world
            handleExit world
            Constants.Engine.SuccessExitCode
        with exn ->
            trace ^ acstring exn
            handleExit world
            Constants.Engine.FailureExitCode

    /// Run the game engine with the given handlers.
    let run handleAttemptMakeWorld handleEvent handleUpdate handleRender handlePlay handleExit sdlConfig =
        match SdlDeps.attemptMake sdlConfig with
        | Right sdlDeps ->
            use sdlDeps = sdlDeps // bind explicitly to dispose automatically
            match handleAttemptMakeWorld sdlDeps with
            | Right world -> run8 tautology handleEvent handleUpdate handleRender handlePlay handleExit sdlDeps Running world
            | Left error -> trace error; Constants.Engine.FailureExitCode
        | Left error -> trace error; Constants.Engine.FailureExitCode