﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2017.

namespace Nu
open System
open System.Collections.Generic
open OpenTK
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldModuleScreen =

    type World with
    
        static member private screenStateKeyEquality
            (screenStateKey : KeyValuePair<Screen Address, UMap<Screen Address, ScreenState>>)
            (screenStateKey2 : KeyValuePair<Screen Address, UMap<Screen Address, ScreenState>>) =
            refEq screenStateKey.Key screenStateKey2.Key &&
            refEq screenStateKey.Value screenStateKey2.Value

        static member private screenGetFreshKeyAndValue (screen : Screen) world =
            let screenStateOpt = UMap.tryFindFast screen.ScreenAddress world.ScreenStates
            KeyValuePair (KeyValuePair (screen.ScreenAddress, world.ScreenStates), screenStateOpt)

        static member private screenStateFinder (screen : Screen) world =
            KeyedCache.getValue
                World.screenStateKeyEquality
                (fun () -> World.screenGetFreshKeyAndValue screen world)
                (KeyValuePair (screen.ScreenAddress, world.ScreenStates))
                (World.getScreenCachedOpt world)

        static member private screenStateAdder screenState (screen : Screen) world =
            let screenDirectory =
                match Address.getNames screen.ScreenAddress with
                | [screenName] ->
                    let layerDirectoryOpt = UMap.tryFindFast screenName world.ScreenDirectory
                    if FOption.isSome layerDirectoryOpt then
                        let layerDirectory = FOption.get layerDirectoryOpt
                        // NOTE: this is logically a redundant operation...
                        let layerDirectory = KeyValuePair (screen.ScreenAddress, layerDirectory.Value)
                        UMap.add screenName layerDirectory world.ScreenDirectory
                    else
                        let layerDirectory = KeyValuePair (screen.ScreenAddress, UMap.makeEmpty Constants.Engine.SimulantMapConfig)
                        UMap.add screenName layerDirectory world.ScreenDirectory
                | _ -> failwith ^ "Invalid screen address '" + scstring screen.ScreenAddress + "'."
            let screenStates = UMap.add screen.ScreenAddress screenState world.ScreenStates
            World.choose { world with ScreenDirectory = screenDirectory; ScreenStates = screenStates }

        static member private screenStateRemover (screen : Screen) world =
            let screenDirectory =
                match Address.getNames screen.ScreenAddress with
                | [screenName] -> UMap.remove screenName world.ScreenDirectory
                | _ -> failwith ^ "Invalid screen address '" + scstring screen.ScreenAddress + "'."
            let screenStates = UMap.remove screen.ScreenAddress world.ScreenStates
            World.choose { world with ScreenDirectory = screenDirectory; ScreenStates = screenStates }

        static member private screenStateSetter screenState (screen : Screen) world =
#if DEBUG
            if not ^ UMap.containsKey screen.ScreenAddress world.ScreenStates then
                failwith ^ "Cannot set the state of a non-existent screen '" + scstring screen.ScreenAddress + "'"
            if not ^ World.qualifyEventContext (atooa screen.ScreenAddress) world then
                failwith ^ "Cannot set the state of a screen in an unqualifed event context."
#endif
            let screenStates = UMap.add screen.ScreenAddress screenState world.ScreenStates
            World.choose { world with ScreenStates = screenStates }

        static member private addScreenState screenState screen world =
            World.screenStateAdder screenState screen world

        static member private removeScreenState screen world =
            World.screenStateRemover screen world

        static member private publishScreenChange (propertyName : string) (screen : Screen) oldWorld world =
            let changeEventAddress = ltoa ["Screen"; "Change"; propertyName; "Event"] ->>- screen.ScreenAddress
            let eventTrace = EventTrace.record "World" "publishScreenChange" EventTrace.empty
            World.publishPlus World.sortSubscriptionsByHierarchy { Participant = screen; PropertyName = propertyName; OldWorld = oldWorld } changeEventAddress eventTrace screen false world

        static member private getScreenStateOpt screen world =
             World.screenStateFinder screen world

        static member internal getScreenState screen world =
            let screenStateOpt = World.getScreenStateOpt screen world
            if FOption.isSome screenStateOpt then FOption.get screenStateOpt
            else failwith ^ "Could not find screen with address '" + scstring screen.ScreenAddress + "'."

        static member internal setScreenState screenState screen world =
            World.screenStateSetter screenState screen world

        static member private updateScreenStateWithoutEvent updater screen world =
            let screenState = World.getScreenState screen world
            let screenState = updater screenState
            World.setScreenState screenState screen world

        static member private updateScreenState updater propertyName screen world =
            let oldWorld = world
            let world = World.updateScreenStateWithoutEvent updater screen world
            World.publishScreenChange propertyName screen oldWorld world

        /// Check that a screen exists in the world.
        static member internal screenExists screen world =
            FOption.isSome ^ World.getScreenStateOpt screen world

        static member internal getScreenId screen world = (World.getScreenState screen world).Id
        static member internal getScreenName screen world = (World.getScreenState screen world).Name
        static member internal getScreenDispatcherNp screen world = (World.getScreenState screen world).DispatcherNp
        static member internal getScreenSpecialization screen world = (World.getScreenState screen world).Specialization
        static member internal getScreenPersistent screen world = (World.getScreenState screen world).Persistent
        static member internal setScreenPersistent value screen world = World.updateScreenState (fun screenState -> { screenState with Persistent = value }) Property? Persistent screen world
        static member internal getScreenCreationTimeStampNp screen world = (World.getScreenState screen world).CreationTimeStampNp
        static member internal getScreenImperative screen world = Xtension.getImperative (World.getScreenState screen world).Xtension
        static member internal getScreenScriptOpt screen world = (World.getScreenState screen world).ScriptOpt
        static member internal setScreenScriptOpt value screen world = World.updateScreenState (fun screenState -> { screenState with ScriptOpt = value }) Property? ScriptOpt screen world
        static member internal getScreenScript screen world = (World.getScreenState screen world).Script
        static member internal setScreenScript value screen world =
            let scriptFrame = Scripting.DeclarationFrame HashIdentity.Structural
            let world = World.updateScreenState (fun screenState -> { screenState with Script = value }) Property? Script screen world
            let world = World.setScreenScriptFrameNp scriptFrame screen world
            evalManyWithLogging value scriptFrame screen world |> snd
        static member internal getScreenScriptFrameNp screen world = (World.getScreenState screen world).ScriptFrameNp
        static member internal setScreenScriptFrameNp value screen world = World.updateScreenState (fun screenState -> { screenState with ScriptFrameNp = value }) Property? ScriptFrameNp screen world
        static member internal getScreenOnRegister screen world = (World.getScreenState screen world).OnRegister
        static member internal setScreenOnRegister value screen world = World.updateScreenState (fun screenState -> { screenState with OnRegister = value }) Property? OnRegister screen world
        static member internal getScreenOnUnregister screen world = (World.getScreenState screen world).OnUnregister
        static member internal setScreenOnUnregister value screen world = World.updateScreenState (fun screenState -> { screenState with OnUnregister = value }) Property? OnUnregister screen world
        static member internal getScreenOnUpdate screen world = (World.getScreenState screen world).OnUpdate
        static member internal setScreenOnUpdate value screen world = World.updateScreenState (fun screenState -> { screenState with OnUpdate = value }) Property? OnUpdate screen world
        static member internal getScreenOnPostUpdate screen world = (World.getScreenState screen world).OnPostUpdate
        static member internal setScreenOnPostUpdate value screen world = World.updateScreenState (fun screenState -> { screenState with OnPostUpdate = value }) Property? OnPostUpdate screen world
        static member internal getScreenEntityTreeNp screen world = (World.getScreenState screen world).EntityTreeNp
        static member internal setScreenEntityTreeNpNoEvent value screen world = World.updateScreenStateWithoutEvent (fun screenState -> { screenState with EntityTreeNp = value }) screen world
        static member internal getScreenTransitionStateNp screen world = (World.getScreenState screen world).TransitionStateNp
        static member internal setScreenTransitionStateNp value screen world = World.updateScreenState (fun screenState -> { screenState with TransitionStateNp = value }) Property? TransitionStateNp screen world
        static member internal getScreenTransitionTicksNp screen world = (World.getScreenState screen world).TransitionTicksNp
        static member internal setScreenTransitionTicksNp value screen world = World.updateScreenState (fun screenState -> { screenState with TransitionTicksNp = value }) Property? TransitionTicksNp screen world
        static member internal getScreenIncoming screen world = (World.getScreenState screen world).Incoming
        static member internal setScreenIncoming value screen world = World.updateScreenState (fun screenState -> { screenState with Incoming = value }) Property? Incoming screen world
        static member internal getScreenOutgoing screen world = (World.getScreenState screen world).Outgoing
        static member internal setScreenOutgoing value screen world = World.updateScreenState (fun screenState -> { screenState with Outgoing = value }) Property? Outgoing screen world

        static member internal tryGetScreenCalculatedProperty propertyName screen world =
            let dispatcher = World.getScreenDispatcherNp screen world
            dispatcher.TryGetCalculatedProperty (propertyName, screen, world)

        static member internal tryGetScreenProperty propertyName screen world =
            if World.screenExists screen world then
                match propertyName with // OPTIMIZATION: string match for speed
                | "Id" -> Some { PropertyType = typeof<Guid>; PropertyValue = World.getScreenId screen world }
                | "Name" -> Some { PropertyType = typeof<string>; PropertyValue = World.getScreenName screen world }
                | "DispatcherNp" -> Some { PropertyType = typeof<ScreenDispatcher>; PropertyValue = World.getScreenDispatcherNp screen world }
                | "Specialization" -> Some { PropertyType = typeof<string>; PropertyValue = World.getScreenSpecialization screen world }
                | "Persistent" -> Some { PropertyType = typeof<bool>; PropertyValue = World.getScreenPersistent screen world }
                | "CreationTimeStampNp" -> Some { PropertyType = typeof<int64>; PropertyValue = World.getScreenCreationTimeStampNp screen world }
                | "Imperative" -> Some { PropertyType = typeof<bool>; PropertyValue = World.getScreenImperative screen world }
                | "ScriptOpt" -> Some { PropertyType = typeof<AssetTag option>; PropertyValue = World.getScreenScriptOpt screen world }
                | "Script" -> Some { PropertyType = typeof<Scripting.Expr array>; PropertyValue = World.getScreenScript screen world }
                | "ScriptFrameNp" -> Some { PropertyType = typeof<Scripting.ProceduralFrame list>; PropertyValue = World.getScreenScriptFrameNp screen world }
                | "OnRegister" -> Some { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnRegister screen world }
                | "OnUnregister" -> Some { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnUnregister screen world }
                | "OnUpdate" -> Some { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnUpdate screen world }
                | "OnPostUpdate" -> Some { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnPostUpdate screen world }
                | "TransitionStateNp" -> Some { PropertyType = typeof<TransitionState>; PropertyValue = World.getScreenTransitionStateNp screen world }
                | "TransitionTicksNp" -> Some { PropertyType = typeof<int64>; PropertyValue = World.getScreenTransitionTicksNp screen world }
                | "Incoming" -> Some { PropertyType = typeof<Transition>; PropertyValue = World.getScreenIncoming screen world }
                | "Outgoing" -> Some { PropertyType = typeof<Transition>; PropertyValue = World.getScreenOutgoing screen world }
                | _ ->
                    match ScreenState.tryGetProperty propertyName (World.getScreenState screen world) with
                    | None -> World.tryGetScreenCalculatedProperty propertyName screen world
                    | Some _ as propertyOpt -> propertyOpt
            else None

        static member internal getScreenProperty propertyName screen world =
            match propertyName with // OPTIMIZATION: string match for speed
            | "Id" -> { PropertyType = typeof<Guid>; PropertyValue = World.getScreenId screen world }
            | "Name" -> { PropertyType = typeof<string>; PropertyValue = World.getScreenName screen world }
            | "DispatcherNp" -> { PropertyType = typeof<ScreenDispatcher>; PropertyValue = World.getScreenDispatcherNp screen world }
            | "Specialization" -> { PropertyType = typeof<string>; PropertyValue = World.getScreenSpecialization screen world }
            | "Persistent" -> { PropertyType = typeof<bool>; PropertyValue = World.getScreenPersistent screen world }
            | "CreationTimeStampNp" -> { PropertyType = typeof<int64>; PropertyValue = World.getScreenCreationTimeStampNp screen world }
            | "Imperative" -> { PropertyType = typeof<bool>; PropertyValue = World.getScreenImperative screen world }
            | "ScriptOpt" -> { PropertyType = typeof<AssetTag option>; PropertyValue = World.getScreenScriptOpt screen world }
            | "Script" -> { PropertyType = typeof<Scripting.Expr array>; PropertyValue = World.getScreenScript screen world }
            | "ScriptFrameNp" -> { PropertyType = typeof<Scripting.ProceduralFrame list>; PropertyValue = World.getScreenScriptFrameNp screen world }
            | "OnRegister" -> { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnRegister screen world }
            | "OnUnregister" -> { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnUnregister screen world }
            | "OnUpdate" -> { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnUpdate screen world }
            | "OnPostUpdate" -> { PropertyType = typeof<Scripting.Expr>; PropertyValue = World.getScreenOnPostUpdate screen world }
            | "TransitionStateNp" -> { PropertyType = typeof<TransitionState>; PropertyValue = World.getScreenTransitionStateNp screen world }
            | "TransitionTicksNp" -> { PropertyType = typeof<int64>; PropertyValue = World.getScreenTransitionTicksNp screen world }
            | "Incoming" -> { PropertyType = typeof<Transition>; PropertyValue = World.getScreenIncoming screen world }
            | "Outgoing" -> { PropertyType = typeof<Transition>; PropertyValue = World.getScreenOutgoing screen world }
            | _ ->
                match ScreenState.tryGetProperty propertyName (World.getScreenState screen world) with
                | None ->
                    match World.tryGetScreenCalculatedProperty propertyName screen world with
                    | None -> failwithf "Could not find property '%s'." propertyName
                    | Some property -> property
                | Some property -> property

        static member internal trySetScreenProperty propertyName property screen world =
            if World.screenExists screen world then
                match propertyName with // OPTIMIZATION: string match for speed
                | "Id" -> (false, world)
                | "Name" -> (false, world)
                | "DispatcherNp" -> (false, world)
                | "Specialization" -> (false, world)
                | "Persistent" -> (true, World.setScreenPersistent (property.PropertyValue :?> bool) screen world)
                | "CreationTimeStampNp" -> (false, world)
                | "Imperative" -> (false, world)
                | "ScriptOpt" -> (true, World.setScreenScriptOpt (property.PropertyValue :?> AssetTag option) screen world)
                | "Script" -> (true, World.setScreenScript (property.PropertyValue :?> Scripting.Expr array) screen world)
                | "ScriptFrameNp" -> (false, world)
                | "OnRegister" -> (true, World.setScreenOnRegister (property.PropertyValue :?> Scripting.Expr) screen world)
                | "OnUnregister" -> (true, World.setScreenOnUnregister (property.PropertyValue :?> Scripting.Expr) screen world)
                | "OnUpdate" -> (true, World.setScreenOnUpdate (property.PropertyValue :?> Scripting.Expr) screen world)
                | "OnPostUpdate" -> (true, World.setScreenOnPostUpdate (property.PropertyValue :?> Scripting.Expr) screen world)
                | "TransitionStateNp" -> (true, World.setScreenTransitionStateNp (property.PropertyValue :?> TransitionState) screen world)
                | "TransitionTicksNp" -> (true, World.setScreenTransitionTicksNp (property.PropertyValue :?> int64) screen world)
                | "Incoming" -> (true, World.setScreenIncoming (property.PropertyValue :?> Transition) screen world)
                | "Outgoing" -> (true, World.setScreenOutgoing (property.PropertyValue :?> Transition) screen world)
                | _ ->
                    // HACK: needed to mutate a flag to get the success state out of an updateScreenState callback...
                    let mutable success = false
                    let world =
                        World.updateScreenState (fun screenState ->
                            let (successInner, screenState) = ScreenState.trySetProperty propertyName property screenState
                            success <- successInner; screenState)
                            propertyName screen world
                    (success, world)
            else (false, world)

        static member internal setScreenProperty propertyName property screen world =
            match propertyName with // OPTIMIZATION: string match for speed
            | "Id" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "Name" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "DispatcherNp" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "Specialization" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "Persistent" -> World.setScreenPersistent (property.PropertyValue :?> bool) screen world
            | "CreationTimeStampNp" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "Imperative" -> failwith ^ "Cannot change screen " + propertyName + "."
            | "ScriptOpt" -> World.setScreenScriptOpt (property.PropertyValue :?> AssetTag option) screen world
            | "Script" -> World.setScreenScript (property.PropertyValue :?> Scripting.Expr array) screen world
            | "ScriptFrameNp" -> world
            | "OnRegister" -> World.setScreenOnRegister (property.PropertyValue :?> Scripting.Expr) screen world
            | "OnUnregister" -> World.setScreenOnUnregister (property.PropertyValue :?> Scripting.Expr) screen world
            | "OnUpdate" -> World.setScreenOnUpdate (property.PropertyValue :?> Scripting.Expr) screen world
            | "OnPostUpdate" -> World.setScreenOnPostUpdate (property.PropertyValue :?> Scripting.Expr) screen world
            | "TransitionStateNp" -> World.setScreenTransitionStateNp (property.PropertyValue :?> TransitionState) screen world
            | "TransitionTicksNp" -> World.setScreenTransitionTicksNp (property.PropertyValue :?> int64) screen world
            | "Incoming" -> World.setScreenIncoming (property.PropertyValue :?> Transition) screen world
            | "Outgoing" -> World.setScreenOutgoing (property.PropertyValue :?> Transition) screen world
            | _ -> World.updateScreenState (ScreenState.setProperty propertyName property) propertyName screen world

        static member private screenOnRegisterChanged evt world =
            let screen = evt.Subscriber : Screen
            let world = World.unregisterScreen screen world
            World.registerScreen screen world

        static member private screenScriptOptChanged evt world =
            let screen = evt.Subscriber : Screen
            match World.getScreenScriptOpt screen world with
            | Some script ->
                match World.assetTagToValueOpt<Scripting.Expr array> true script world with
                | (Some script, world) -> World.setScreenScript script screen world
                | (None, world) -> world
            | None -> world

        static member internal registerScreen screen world =
            let world = World.monitor World.screenOnRegisterChanged (ltoa<ParticipantChangeData<Screen, World>> ["Screen"; "Change"; (Property? OnRegister); "Event"] ->- screen) screen world
            let world = World.monitor World.screenScriptOptChanged (ltoa<ParticipantChangeData<Screen, World>> ["Screen"; "Change"; (Property? ScriptOpt); "Event"] ->- screen) screen world
            let world =
                World.withEventContext (fun world ->
                    let dispatcher = World.getScreenDispatcherNp screen world
                    let world = dispatcher.Register (screen, world)
                    let eventTrace = EventTrace.record "World" "registerScreen" EventTrace.empty
                    let world = World.publish () (ltoa<unit> ["Screen"; "Register"; "Event"] ->- screen) eventTrace screen world
                    eval (World.getScreenOnUnregister screen world) (World.getScreenScriptFrameNp screen world) screen world |> snd)
                    screen
                    world
            World.choose world

        static member internal unregisterScreen screen world =
            let world =
                World.withEventContext (fun world ->
                    let world = eval (World.getScreenOnRegister screen world) (World.getScreenScriptFrameNp screen world) screen world |> snd
                    let dispatcher = World.getScreenDispatcherNp screen world
                    let eventTrace = EventTrace.record "World" "unregisterScreen" EventTrace.empty
                    let world = World.publish () (ltoa<unit> ["Screen"; "Unregistering"; "Event"] ->- screen) eventTrace screen world
                    dispatcher.Unregister (screen, world))
                    screen
                    world
            World.choose world

        static member internal addScreen mayReplace screenState screen world =
            let isNew = not ^ World.screenExists screen world
            if isNew || mayReplace then
                let world = World.addScreenState screenState screen world
                if isNew then World.registerScreen screen world else world
            else failwith ^ "Adding a screen that the world already contains at address '" + scstring screen.ScreenAddress + "'."

        static member internal removeScreen3 removeLayers screen world =
            if World.screenExists screen world then
                let world = World.unregisterScreen screen world
                let world = removeLayers screen world
                World.removeScreenState screen world
            else world

        static member internal writeScreen4 writeLayers screen screenDescriptor world =
            let screenState = World.getScreenState screen world
            let screenDispatcherName = getTypeName screenState.DispatcherNp
            let screenDescriptor = { screenDescriptor with ScreenDispatcher = screenDispatcherName }
            let getScreenProperties = Reflection.writePropertiesFromTarget tautology3 screenDescriptor.ScreenProperties screenState
            let screenDescriptor = { screenDescriptor with ScreenProperties = getScreenProperties }
            writeLayers screen screenDescriptor world

        static member internal readScreen4 readLayers screenDescriptor nameOpt world =
            
            // create the dispatcher
            let dispatcherName = screenDescriptor.ScreenDispatcher
            let dispatchers = World.getScreenDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None ->
                    Log.info ^ "Could not find ScreenDispatcher '" + dispatcherName + "'. Did you forget to provide this dispatcher from your NuPlugin?"
                    let dispatcherName = typeof<ScreenDispatcher>.Name
                    Map.find dispatcherName dispatchers

            // make the screen state and populate its properties
            let screenState = ScreenState.make None None dispatcher
            let screenState = Reflection.attachProperties ScreenState.copy screenState.DispatcherNp screenState
            let screenState = Reflection.readPropertiesToTarget ScreenState.copy screenDescriptor.ScreenProperties screenState

            // apply the name if one is provided
            let screenState =
                match nameOpt with
                | Some name -> { screenState with Name = name }
                | None -> screenState

            // add the screen's state to the world
            let screen = Screen (ntoa screenState.Name)
            let screenState =
                if World.screenExists screen world
                then { screenState with EntityTreeNp = World.getScreenEntityTreeNp screen world }
                else screenState
            let world = World.addScreen true screenState screen world
            
            // read the screen's layers
            let world = readLayers screenDescriptor screen world |> snd
            (screen, world)

        /// View all of the properties of a screen.
        static member internal viewScreenProperties screen world =
            let state = World.getScreenState screen world
            let properties = World.getProperties state
            Array.ofList properties