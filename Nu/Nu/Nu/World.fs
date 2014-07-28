﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2014.

namespace Nu
open System
open System.IO
open System.Collections.Generic
open System.ComponentModel
open System.Reflection
open System.Xml
open System.Xml.Serialization
open FSharpx
open FSharpx.Lens.Operators
open SDL2
open OpenTK
open TiledSharp
open Prime
open Nu
open Nu.NuConstants

[<AutoOpen>]
module WorldModule =

    type World with

        static member makeSubscriptionKey =
            Guid.NewGuid

        static member handleEventAsSwallow (world : World) =
            (Handled, world)

        static member handleEventAsExit (world : World) =
            (Handled, { world with Liveness = Exiting })

    // NOTE: making these static members of type World causes them to be evaluated each time
    // they're referenced. I assume this is a bug in F#.
    let private ScreenTransitionDownMouseKey = World.makeSubscriptionKey ()
    let private ScreenTransitionUpMouseKey = World.makeSubscriptionKey ()
    let private SplashScreenTickKey = World.makeSubscriptionKey ()

    type World with

        static member private setScreenStatePlus address state world =
            // TODO: add swallowing for other types of input as well (keys, joy buttons, etc.)
            let world = World.withScreen (fun screen -> { screen with State = state }) address world
            match state with
            | IdlingState ->
                world |>
                    World.unsubscribe ScreenTransitionDownMouseKey |>
                    World.unsubscribe ScreenTransitionUpMouseKey
            | IncomingState | OutgoingState ->
                world |>
                    World.subscribe ScreenTransitionDownMouseKey (DownMouseEventName @ AnyEventName) address SwallowSub |>
                    World.subscribe ScreenTransitionUpMouseKey (UpMouseEventName @ AnyEventName) address SwallowSub

        static member selectScreen destination world =
            let world = World.setScreenStatePlus destination IncomingState world
            World.setOptSelectedScreenAddress (Some destination) world

        static member transitionScreen destination world =
            match World.getOptSelectedScreenAddress world with
            | None ->
                trace "Program Error: Could not handle screen transition due to no selected screen."
                { world with Liveness = Exiting }
            | Some selectedScreenAddress ->
                let subscriptionKey = World.makeSubscriptionKey ()
                let sub = CustomSub (fun _ world ->
                    let world = World.unsubscribe subscriptionKey world
                    let world = World.selectScreen destination world
                    (Unhandled, world))
                let world = World.setScreenStatePlus selectedScreenAddress OutgoingState world
                World.subscribe subscriptionKey (FinishOutgoingEventName @ selectedScreenAddress) selectedScreenAddress sub world

        static member handleEventAsScreenTransitionFromSplash destination world =
            let world = World.selectScreen destination world
            (Unhandled, world)

        static member handleEventAsScreenTransition destination world =
            let world = World.transitionScreen destination world
            (Unhandled, world)

        static member private sortFstDesc (priority, _) (priority2, _) =
            if priority = priority2 then 0
            elif priority > priority2 then -1
            else 1

        static member getSimulant (address : Address) world =
            match address with
            | [] -> Game <| world.Game
            | [_] as screenAddress -> Screen <| World.getScreen screenAddress world
            | [_; _] as groupAddress -> Group <| World.getGroup groupAddress world
            | [_; _; _] as entityAddress -> Entity <| World.getEntity entityAddress world
            | _ -> failwith <| "Invalid simulant address '" + addrToStr address + "'."

        static member getOptSimulant (address : Address) world =
            match address with
            | [] -> Some <| Game world.Game
            | [_] as screenAddress -> Option.map Screen <| World.getOptScreen screenAddress world
            | [_; _] as groupAddress -> Option.map Group <| World.getOptGroup groupAddress world
            | [_; _; _] as entityAddress -> Option.map Entity <| World.getOptEntity entityAddress world
            | _ -> failwith <| "Invalid simulant address '" + addrToStr address + "'."

        static member getPublishingPriority getEntityPublishingPriority simulant world =
            match simulant with
            | Game _ -> GamePublishingPriority
            | Screen _ -> ScreenPublishingPriority
            | Group _ -> GroupPublishingPriority
            | Entity entity -> getEntityPublishingPriority entity world

        static member getSubscriptionSortables getEntityPublishingPriority subscriptions world =
            let optSimulants =
                List.map
                    (fun (key, address, subscription) ->
                        let optSimulant = World.getOptSimulant address world
                        Option.map (fun simulant -> (World.getPublishingPriority getEntityPublishingPriority simulant world, (key, address, subscription))) optSimulant)
                    subscriptions
            List.definitize optSimulants

        static member sortSubscriptionsBy getEntityPublishingPriority (subscriptions : SubscriptionEntry list) world =
            let subscriptions = World.getSubscriptionSortables getEntityPublishingPriority subscriptions world
            let subscriptions = List.sortWith World.sortFstDesc subscriptions
            List.map snd subscriptions

        static member sortSubscriptionsByPickingPriority subscriptions world =
            World.sortSubscriptionsBy (fun (entity : Entity) world -> Entity.getPickingPriority entity world) subscriptions world

        static member sortSubscriptionsByHierarchy (subscriptions : SubscriptionEntry list) world =
            World.sortSubscriptionsBy (fun _ _ -> EntityPublishingPriority) subscriptions world

        /// Publish an event.
        static member publishDefinition publishSort eventName publisher eventData world =
            let eventNames = List.collapseLeft eventName
            let optSubLists = List.map (fun eventName -> Map.tryFind (eventName @ AnyEventName) world.Subscriptions) eventNames
            let optSubLists = Map.tryFind eventName world.Subscriptions :: optSubLists
            let subLists = List.definitize optSubLists
            let subList = List.concat subLists
            let subListSorted = publishSort subList world
            let (_, world) =
                List.foldWhile
                    (fun (eventHandled, world) (_, subscriber, subscription) ->
                        let event = { Name = eventName; Publisher = publisher; Subscriber = subscriber; Data = eventData }
                        if eventHandled = Handled || world.Liveness = Exiting then None
                        else
                            let result =
                                match subscription with
                                | ExitSub -> World.handleEventAsExit world
                                | SwallowSub -> World.handleEventAsSwallow world
                                | ScreenTransitionSub destination -> World.handleEventAsScreenTransition destination world
                                | ScreenTransitionFromSplashSub destination -> World.handleEventAsScreenTransitionFromSplash destination world
                                | CustomSub fn ->
                                    match World.getOptSimulant event.Subscriber world with
                                    | None -> (Unhandled, world)
                                    | Some _ -> fn event world
                            Some result)
                    (Unhandled, world)
                    subListSorted
            world

        /// Publish an event.
        static member publish4Definition eventName publisher eventData world =
            World.publish World.sortSubscriptionsByHierarchy eventName publisher eventData world

        /// Subscribe to an event.
        static member subscribeDefinition subscriptionKey eventName subscriber subscription world =
            let subscriptions = 
                match Map.tryFind eventName world.Subscriptions with
                | None -> Map.add eventName [(subscriptionKey, subscriber, subscription)] world.Subscriptions
                | Some subscriptionList -> Map.add eventName ((subscriptionKey, subscriber, subscription) :: subscriptionList) world.Subscriptions
            let unsubscriptions = Map.add subscriptionKey (eventName, subscriber) world.Unsubscriptions
            { world with Subscriptions = subscriptions; Unsubscriptions = unsubscriptions }

        /// Subscribe to an event.
        static member subscribe4Definition eventName subscriber subscription world =
            World.subscribe (World.makeSubscriptionKey ()) eventName subscriber subscription world

        /// Unsubscribe to an event.
        static member unsubscribeDefinition subscriptionKey world =
            match Map.tryFind subscriptionKey world.Unsubscriptions with
            | None -> world // TODO: consider failure signal
            | Some (eventName, subscriber) ->
                match Map.tryFind eventName world.Subscriptions with
                | None -> world // TODO: consider failure signal
                | Some subscriptionList ->
                    let subscriptionList =
                        List.remove
                            (fun (subscriptionKey', subscriber', _) -> subscriptionKey' = subscriptionKey && subscriber' = subscriber)
                            subscriptionList
                    let subscriptions = Map.add eventName subscriptionList world.Subscriptions
                    { world with Subscriptions = subscriptions }

        /// Execute a procedure within the context of a given subscription for the given event.
        static member withSubscriptionDefinition eventName subscriber subscription procedure world =
            let subscriptionKey = World.makeSubscriptionKey ()
            let world = World.subscribe subscriptionKey eventName subscriber subscription world
            let world = procedure world
            World.unsubscribe subscriptionKey world

        /// Subscribe to an event during the lifetime of the subscriber.
        static member observeDefinition eventName subscriber subscription world =
            if List.isEmpty subscriber then
                debug "Cannot observe events with an anonymous subscriber."
                world
            else
                let observationKey = World.makeSubscriptionKey ()
                let removalKey = World.makeSubscriptionKey ()
                let world = World.subscribe observationKey eventName subscriber subscription world
                let sub = CustomSub (fun _ world ->
                    let world = World.unsubscribe removalKey world
                    let world = World.unsubscribe observationKey world
                    (Unhandled, world))
                World.subscribe removalKey (RemovingEventName @ subscriber) subscriber sub world

        static member private updateTransition1 (transition : Transition) =
            if transition.TransitionTicks = transition.TransitionLifetime then (true, { transition with TransitionTicks = 0L })
            else (false, { transition with TransitionTicks = transition.TransitionTicks + 1L })

        static member internal updateTransition update world =
            let world =
                match World.getOptSelectedScreenAddress world with
                | None -> world
                | Some selectedScreenAddress ->
                    let selectedScreen = World.getScreen selectedScreenAddress world
                    match selectedScreen.State with
                    | IncomingState ->
                        let world =
                            if selectedScreen.Incoming.TransitionTicks = 0L
                            then World.publish4 (SelectEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                            else world
                        match world.Liveness with
                        | Exiting -> world
                        | Running ->
                            let world =
                                if selectedScreen.Incoming.TransitionTicks = 0L
                                then World.publish4 (StartIncomingEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                                else world
                            match world.Liveness with
                            | Exiting -> world
                            | Running ->
                                let (finished, incoming) = World.updateTransition1 selectedScreen.Incoming
                                let selectedScreen = { selectedScreen with Incoming = incoming }
                                let world = World.setScreen selectedScreenAddress selectedScreen world
                                if finished then
                                    let world = World.setScreenStatePlus selectedScreenAddress IdlingState world
                                    World.publish4 (FinishIncomingEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                                else world
                    | OutgoingState ->
                        let world =
                            if selectedScreen.Outgoing.TransitionTicks <> 0L then world
                            else World.publish4 (StartOutgoingEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                        match world.Liveness with
                        | Exiting -> world
                        | Running ->
                            let (finished, outgoing) = World.updateTransition1 selectedScreen.Outgoing
                            let selectedScreen = { selectedScreen with Outgoing = outgoing }
                            let world = World.setScreen selectedScreenAddress selectedScreen world
                            if finished then
                                let world = World.setScreenStatePlus selectedScreenAddress IdlingState world
                                let world = World.publish4 (DeselectEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                                match world.Liveness with
                                | Exiting -> world
                                | Running -> World.publish4 (FinishOutgoingEventName @ selectedScreenAddress) selectedScreenAddress NoData world
                            else world
                    | IdlingState -> world
            match world.Liveness with
            | Exiting -> world
            | Running -> update world

        static member private handleSplashScreenIdleTick idlingTime ticks event world =
            let world = World.unsubscribe SplashScreenTickKey world
            if ticks < idlingTime then
                let subscription = CustomSub <| World.handleSplashScreenIdleTick idlingTime (incL ticks)
                let world = World.subscribe SplashScreenTickKey event.Name event.Subscriber subscription world
                (Unhandled, world)
            else
                match World.getOptSelectedScreenAddress world with
                | None ->
                    trace "Program Error: Could not handle splash screen tick due to no selected screen."
                    (Handled, { world with Liveness = Exiting })
                | Some selectedScreenAddress ->
                    let world = World.setScreenStatePlus selectedScreenAddress OutgoingState world
                    (Unhandled, world)

        static member internal handleSplashScreenIdle idlingTime event world =
            let subscription = CustomSub <| World.handleSplashScreenIdleTick idlingTime 0L
            let world = World.subscribe SplashScreenTickKey TickEventName event.Subscriber subscription world
            (Handled, world)

        static member activateGameDispatcher assemblyFileName gameDispatcherFullName world =
            let assembly = Assembly.LoadFrom assemblyFileName
            let gameDispatcherType = assembly.GetType gameDispatcherFullName
            let gameDispatcherShortName = gameDispatcherType.Name
            let gameDispatcher = Activator.CreateInstance gameDispatcherType
            let dispatchers = Map.add gameDispatcherShortName gameDispatcher world.Dispatchers
            let world = { world with Dispatchers = dispatchers }
            let world = { world with Game = { world.Game with Xtension = { world.Game.Xtension with OptXDispatcherName = Some gameDispatcherShortName }}}
            world.Game.Register world

        static member saveGroupToFile group entities fileName (_ : World) =
            use file = File.Open (fileName, FileMode.Create)
            let writerSettings = XmlWriterSettings ()
            writerSettings.Indent <- true
            use writer = XmlWriter.Create (file, writerSettings)
            writer.WriteStartDocument ()
            writer.WriteStartElement "Root"
            Group.writeToXml writer group entities
            writer.WriteEndElement ()
            writer.WriteEndDocument ()

        static member loadGroupFromFile fileName world =
            let document = XmlDocument ()
            document.Load (fileName : string)
            let rootNode = document.["Root"]
            let groupNode = rootNode.["Group"]
            Group.readFromXml groupNode typeof<GroupDispatcher>.Name typeof<EntityDispatcher>.Name world

        static member private play world =
            let audioMessages = world.AudioMessages
            let world = { world with AudioMessages = [] }
            { world with AudioPlayer = Nu.Audio.play audioMessages world.AudioPlayer }

        static member private getGroupRenderDescriptors dispatcherContainer entities =
            Map.toValueListBy (fun entity -> Entity.getRenderDescriptors entity dispatcherContainer) entities

        static member private getTransitionRenderDescriptors camera transition =
            match transition.OptDissolveImage with
            | None -> []
            | Some dissolveImage ->
                let progress = single transition.TransitionTicks / single transition.TransitionLifetime
                let alpha = match transition.TransitionType with Incoming -> 1.0f - progress | Outgoing -> progress
                let color = Vector4 (Vector3.One, alpha)
                [LayerableDescriptor
                    { Depth = Single.MaxValue
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = -camera.EyeSize * 0.5f // negation for right-handedness
                              Size = camera.EyeSize
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = dissolveImage
                              Color = color }}]

        static member private getRenderDescriptors world =
            match World.getOptSelectedScreenAddress world with
            | None -> []
            | Some selectedScreenAddress ->
                let optGroupMap = Map.tryFind selectedScreenAddress.[0] world.Entities
                match optGroupMap with
                | None -> []
                | Some groupMap ->
                    let groupValues = Map.toValueList groupMap
                    let entityMaps = List.fold List.flipCons [] groupValues
                    let descriptors = List.map (World.getGroupRenderDescriptors world) entityMaps
                    let descriptors = List.concat descriptors
                    let descriptors = List.concat descriptors
                    let selectedScreen = World.getScreen selectedScreenAddress world
                    match selectedScreen.State with
                    | IncomingState -> descriptors @ World.getTransitionRenderDescriptors world.Camera selectedScreen.Incoming
                    | OutgoingState -> descriptors @ World.getTransitionRenderDescriptors world.Camera selectedScreen.Outgoing
                    | IdlingState -> descriptors

        static member private render world =
            let renderMessages = world.RenderMessages
            let renderDescriptors = World.getRenderDescriptors world
            let renderer = world.Renderer
            let renderer = Nu.Rendering.render world.Camera renderMessages renderDescriptors renderer
            { world with RenderMessages = []; Renderer = renderer }

        static member private handleIntegrationMessage world integrationMessage =
            match world.Liveness with
            | Exiting -> world
            | Running ->
                match integrationMessage with
                | BodyTransformMessage bodyTransformMessage ->
                    match World.getOptEntity bodyTransformMessage.EntityAddress world with
                    | None -> world
                    | Some entity -> Entity.handleBodyTransformMessage bodyTransformMessage.EntityAddress bodyTransformMessage entity world
                | BodyCollisionMessage bodyCollisionMessage ->
                    match World.getOptEntity bodyCollisionMessage.EntityAddress world with
                    | None -> world
                    | Some _ ->
                        let collisionAddress = CollisionEventName @ bodyCollisionMessage.EntityAddress
                        let collisionData =
                            EntityCollisionData
                                { Normal = bodyCollisionMessage.Normal
                                  Speed = bodyCollisionMessage.Speed
                                  Collidee = bodyCollisionMessage.EntityAddress2 }
                        World.publish4 collisionAddress [] collisionData world

        static member private handleIntegrationMessages integrationMessages world =
            List.fold World.handleIntegrationMessage world integrationMessages

        static member private integrate world =
            if World.physicsRunning world then
                let integrationMessages = Nu.Physics.integrate world.PhysicsMessages world.Integrator
                let world = { world with PhysicsMessages = [] }
                World.handleIntegrationMessages integrationMessages world
            else world

        static member private runNextTask world =
            let task = List.head world.Tasks
            if task.ScheduledTime = world.TickTime then
                let world = task.Operation world
                { world with Tasks = List.tail world.Tasks }
            else world

        static member private runTasks world =
            List.fold (fun world _ -> World.runNextTask world) world world.Tasks

        static member run4 tryMakeWorld handleUpdate handleRender sdlConfig =
            Sdl.run
                (fun sdlDeps -> tryMakeWorld sdlDeps)
                (fun refEvent world ->
                    let event = !refEvent
                    let world =
                        match event.``type`` with
                        | SDL.SDL_EventType.SDL_QUIT -> { world with Liveness = Exiting }
                        | SDL.SDL_EventType.SDL_MOUSEMOTION ->
                            let mousePosition = Vector2 (single event.button.x, single event.button.y)
                            let world = { world with MouseState = { world.MouseState with MousePosition = mousePosition }}
                            if Set.contains MouseLeft world.MouseState.MouseDowns
                            then World.publish World.sortSubscriptionsByPickingPriority MouseDragEventName [] (MouseMoveData { Position = mousePosition }) world
                            else World.publish World.sortSubscriptionsByPickingPriority MouseMoveEventName [] (MouseButtonData { Position = mousePosition; Button = MouseLeft }) world
                        | SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN ->
                            let mouseButton = Sdl.makeNuMouseButton event.button.button
                            let mouseEventName = addrstr DownMouseEventName <| string mouseButton
                            let world = { world with MouseState = { world.MouseState with MouseDowns = Set.add mouseButton world.MouseState.MouseDowns }}
                            let eventData = MouseButtonData { Position = world.MouseState.MousePosition; Button = mouseButton }
                            World.publish World.sortSubscriptionsByPickingPriority mouseEventName [] eventData world
                        | SDL.SDL_EventType.SDL_MOUSEBUTTONUP ->
                            let mouseState = world.MouseState
                            let mouseButton = Sdl.makeNuMouseButton event.button.button
                            let mouseEventName = addrstr UpMouseEventName <| string mouseButton
                            if Set.contains mouseButton mouseState.MouseDowns then
                                let world = { world with MouseState = { world.MouseState with MouseDowns = Set.remove mouseButton world.MouseState.MouseDowns }}
                                let eventData = MouseButtonData { Position = world.MouseState.MousePosition; Button = mouseButton }
                                World.publish World.sortSubscriptionsByPickingPriority mouseEventName [] eventData world
                            else world
                        | _ -> world
                    (world.Liveness, world))
                (fun world ->
                    let world = World.integrate world
                    match world.Liveness with
                    | Exiting -> (Exiting, world)
                    | Running ->
                        let world = World.publish4 TickEventName [] NoData world
                        match world.Liveness with
                        | Exiting -> (Exiting, world)
                        | Running ->
                            let world = World.updateTransition handleUpdate world
                            match world.Liveness with
                            | Exiting -> (Exiting, world)
                            | Running ->
                                let world = World.runTasks world
                                (world.Liveness, world))
                (fun world -> let world = World.render world in handleRender world)
                (fun world -> let world = World.play world in { world with TickTime = world.TickTime + 1L })
                (fun world -> { world with Renderer = Rendering.handleRenderExit world.Renderer })
                sdlConfig

        static member run tryMakeWorld handleUpdate sdlConfig =
            World.run4 tryMakeWorld handleUpdate id sdlConfig

        static member addSplashScreenFromData destination address screenDispatcherName incomingTime idlingTime outgoingTime image world =
            let splashScreen = Screen.makeDissolve screenDispatcherName incomingTime outgoingTime
            let splashGroup = Group.makeDefault typeof<GroupDispatcher>.Name world
            let splashLabel = Entity.makeDefault typeof<LabelDispatcher>.Name (Some "SplashLabel") world
            let splashLabel = Entity.setSize world.Camera.EyeSize splashLabel
            let splashLabel = Entity.setPosition (-world.Camera.EyeSize * 0.5f) splashLabel
            let splashLabel = Entity.setLabelImage image splashLabel
            let world = World.addScreen address splashScreen [("SplashGroup", splashGroup, [splashLabel])] world
            let world = World.observe (FinishIncomingEventName @ address) address (CustomSub <| World.handleSplashScreenIdle idlingTime) world
            World.observe (FinishOutgoingEventName @ address) address (ScreenTransitionFromSplashSub destination) world

        static member addDissolveScreenFromFile screenDispatcherName groupFileName groupName incomingTime outgoingTime screenAddress world =
            let screen = Screen.makeDissolve screenDispatcherName incomingTime outgoingTime
            let (group, entities) = World.loadGroupFromFile groupFileName world
            let world = World.addScreen screenAddress screen [(groupName, group, entities)] world
            world

        static member tryMakeEmpty sdlDeps gameDispatcher interactivity extData =
            match Metadata.tryGenerateAssetMetadataMap AssetGraphFileName with
            | Left errorMsg -> Left errorMsg
            | Right assetMetadataMap ->
                let gameDispatcherName = (gameDispatcher.GetType ()).Name
                let dispatchers =
                    Map.ofList
                        // TODO: see if we can reflectively generate this array
                        [typeof<EntityDispatcher>.Name, EntityDispatcher () :> obj
                         typeof<ButtonDispatcher>.Name, ButtonDispatcher () :> obj
                         typeof<LabelDispatcher>.Name, LabelDispatcher () :> obj
                         typeof<TextBoxDispatcher>.Name, TextBoxDispatcher () :> obj
                         typeof<ToggleDispatcher>.Name, ToggleDispatcher () :> obj
                         typeof<FeelerDispatcher>.Name, FeelerDispatcher () :> obj
                         typeof<FillBarDispatcher>.Name, FillBarDispatcher () :> obj
                         typeof<BlockDispatcher>.Name, BlockDispatcher () :> obj
                         typeof<AvatarDispatcher>.Name, AvatarDispatcher () :> obj
                         typeof<CharacterDispatcher>.Name, CharacterDispatcher () :> obj
                         typeof<TileMapDispatcher>.Name, TileMapDispatcher () :> obj
                         typeof<GroupDispatcher>.Name, GroupDispatcher () :> obj
                         typeof<ScreenDispatcher>.Name, ScreenDispatcher () :> obj
                         typeof<GameDispatcher>.Name, GameDispatcher () :> obj
                         gameDispatcherName, gameDispatcher]
                let aType = typeof<Entity>
                let ctorParams = [|typeof<Guid>; typeof<string>; typeof<bool>; typeof<Xtension>|]
                let constructors =
                    Map.ofList
                        [typeof<Entity>.Name, typeof<Entity>.GetConstructor ctorParams]
                let world =
                    { Game = { Id = NuCore.makeId (); OptSelectedScreenAddress = None; Xtension = { XFields = Map.empty; OptXDispatcherName = Some gameDispatcherName; CanDefault = true; Sealed = false }}
                      Screens = Map.empty
                      Groups = Map.empty
                      Entities = Map.empty
                      TickTime = 0L
                      Liveness = Running
                      Interactivity = interactivity
                      Camera = let eyeSize = Vector2 (single sdlDeps.Config.ViewW, single sdlDeps.Config.ViewH) in { EyeCenter = Vector2.Zero; EyeSize = eyeSize }
                      Tasks = []
                      Subscriptions = Map.empty
                      Unsubscriptions = Map.empty
                      MouseState = { MousePosition = Vector2.Zero; MouseDowns = Set.empty }
                      AudioPlayer = Audio.makeAudioPlayer ()
                      Renderer = Rendering.makeRenderer sdlDeps.RenderContext
                      Integrator = Physics.makeIntegrator Gravity
                      AssetMetadataMap = assetMetadataMap
                      AudioMessages = [HintAudioPackageUseMessage { FileName = AssetGraphFileName; PackageName = DefaultPackageName }]
                      RenderMessages = [HintRenderingPackageUseMessage { FileName = AssetGraphFileName; PackageName = DefaultPackageName }]
                      PhysicsMessages = []
                      Dispatchers = dispatchers
                      Constructors = constructors
                      ExtData = extData }
                let world = world.Game.Register world
                Right world

        static member rebuildPhysicsHack groupAddress world =
            let outstandingMessages = world.PhysicsMessages
            let world = { world with PhysicsMessages = [] }
            let entities = World.getEntities groupAddress world
            let world =
                Map.fold
                    (fun world _ (entity : Entity) -> Entity.propagatePhysics (groupAddress @ [entity.Name]) entity world)
                    world
                    entities
            { world with PhysicsMessages = outstandingMessages @ world.PhysicsMessages @ [RebuildPhysicsHackMessage]}

        static member init () =
            NuMath.initTypeConverters ()
            Audio.initTypeConverters ()
            Rendering.initTypeConverters ()
            Sim.publish <- World.publishDefinition
            Sim.publish4 <- World.publish4Definition
            Sim.subscribe <- World.subscribeDefinition
            Sim.subscribe4 <- World.subscribe4Definition
            Sim.unsubscribe <- World.unsubscribeDefinition
            Sim.withSubscription <- World.withSubscriptionDefinition
            Sim.observe <- World.observeDefinition