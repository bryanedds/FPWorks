﻿namespace Metrics
open System
open System.Collections.Generic
open System.IO
open System.Numerics
open Prime
open Nu

type MetricsEntityDispatcher () =
    inherit Entity3dDispatcher<StaticModel AssetTag, Message, Command> (false, Assets.Default.StaticModel)

#if !MMCC
    override this.Update (entity, world) =
        entity.SetAngles (v3 0.0f 0.0f ((entity.GetAngles world).Z + 0.05f)) world
#endif

    override this.Render (renderPass, entity, world) =
        let staticModel = entity.GetModelGeneric world
        let mutable transform = entity.GetTransform world
        let affineMatrix = transform.AffineMatrix
        let presence = transform.Presence
        let properties = MaterialProperties.empty
        World.renderStaticModelFast (false, &affineMatrix, presence, ValueNone, &properties, staticModel, DeferredRenderType, renderPass, world)

    override this.GetAttributesInferred (entity, world) =
        let staticModel = entity.GetModelGeneric world
        let bounds = (Metadata.getStaticModelMetadata staticModel).Bounds
        AttributesInferred.make bounds.Size bounds.Center

#if !MMCC
type MyGameDispatcher () =
    inherit GameDispatcher ()

    override this.Register (game, world) =
        let world = base.Register (game, world)
        let (screen, world) = World.createScreen (Some "Screen") world
        let (group, world) = World.createGroup (Some "Default") screen world
        let (fps, world) = World.createEntity<FpsDispatcher> DefaultOverlay (Some [|"Fps"|]) group world
        let world = World.createEntity<SkyBoxDispatcher> DefaultOverlay None group world |> snd
        let world = fps.SetPosition (v3 200.0f -250.0f 0.0f) world
        let positions =
            [|for i in 0 .. dec 50 do
                for j in 0 .. dec 50 do
                    for k in 0 .. dec 20 do
                        yield v3 (single i * 0.5f) (single j * 0.5f) (single k * 0.5f)|]
        let world =
            Array.fold (fun world position ->
                let (entity, world) = World.createEntity<MetricsEntityDispatcher> NoOverlay (Some [|string Gen.id64|]) group world
                let world = entity.SetPresence Omnipresent world
                let world = entity.SetPosition (position + v3 -12.5f -12.5f -20.0f) world
                let world = entity.SetScale (v3Dup 0.1f) world
                world)
                world positions
        let world = World.selectScreen (IdlingState world.GameTime) screen world
        world
#else
type [<ReferenceEquality>] Ints =
    { Ints : Map<int, int> }
    static member init n =
        { Ints = Seq.init n (fun a -> (a, a)) |> Map.ofSeq }
    static member inc ints =
        { Ints = ints.Ints |> Map.map (fun _ v -> inc v) }

type [<ReferenceEquality>] Intss =
    { Intss : Map<int, Ints> }
    static member init n =
        { Intss = Seq.init n (fun a -> (a, Ints.init n)) |> Map.ofSeq }
    static member inc intss =
        { Intss = intss.Intss |> Map.map (fun k v -> if k % 1 = 0 then Ints.inc v else v) }

type Message =
    | Inc
    | Nop
    interface Nu.Message

type MmccGameDispatcher () =
    inherit GameDispatcher<Intss, Message, Command> (Intss.init 120) // 14,400 MMCC entities (goal: steady 60FPS, current: steady 60FPS)

    override this.Initialize (_, _) =
        [Game.UpdateEvent => Inc]

    override this.Message (intss, message, _, _) =
        match message with
        | Inc -> just (Intss.inc intss)
        | Nop -> just intss

    override this.Content (intss, _) =
        [Content.screen "Screen" Vanilla []
            [|for (i, ints) in intss.Intss.Pairs' do
                Content.group (string i) []
                    [|for (j, int) in ints.Ints.Pairs' do
                        Content.entity<MetricsEntityDispatcher> (string j)
                            [Entity.Position == v3 (single i * 4.25f - 250.0f) (single j * 2.25f - 125.0f) -250.0f
                             Entity.Scale := v3Dup (single (int % 10)) * 0.5f
                             Entity.Presence == Omnipresent]|]
              Content.group "Fps" []
                [Content.fps "Fps" [Entity.Position := v3 200.0f -250.0f 0.0f]]|]]
#endif

type MetricsPlugin () =
    inherit NuPlugin ()

/// This program exists to take metrics on Nu's performance.
module Program =

    let [<EntryPoint; STAThread>] main _ =
        Nu.init ()
        Directory.SetCurrentDirectory AppContext.BaseDirectory
        let sdlWindowConfig = { SdlWindowConfig.defaultConfig with WindowTitle = "MyGame" }
        let sdlConfig = { SdlConfig.defaultConfig with ViewConfig = NewWindow sdlWindowConfig }
#if FUNCTIONAL
        let worldConfig = { WorldConfig.defaultConfig with Imperative = false; SdlConfig = sdlConfig }
#else
        let worldConfig = { WorldConfig.defaultConfig with Imperative = true; SdlConfig = sdlConfig }
#endif
        World.run worldConfig (MetricsPlugin ())