﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Generic
open System.IO
open FSharpx.Collections
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldGroupModule =

    type Group with
    
        member this.GetDispatcher world = World.getGroupDispatcher this world
        member this.Dispatcher = lensReadOnly Property? Dispatcher this.GetDispatcher this
        member this.GetModelGeneric<'a> world = World.getGroupModel<'a> this world
        member this.SetModelGeneric<'a> value world = World.setGroupModel<'a> value this world |> snd'
        member this.ModelGeneric<'a> () = lens Property? Model this.GetModelGeneric<'a> this.SetModelGeneric<'a> this
        member this.GetEcs world = World.getScreenEcs this.Parent world
        member this.Ecs = lensReadOnly Property? Ecs this.GetEcs this
        member this.GetVisible world = World.getGroupVisible this world
        member this.SetVisible value world = World.setGroupVisible value this world |> snd'
        member this.Visible = lens Property? Visible this.GetVisible this.SetVisible this
        member this.GetPersistent world = World.getGroupPersistent this world
        member this.SetPersistent value world = World.setGroupPersistent value this world |> snd'
        member this.Persistent = lens Property? Persistent this.GetPersistent this.SetPersistent this
        member this.GetDestroying world = World.getGroupDestroying this world
        member this.Destroying = lensReadOnly Property? Destroying this.GetDestroying this
        member this.GetScriptFrame world = World.getGroupScriptFrame this world
        member this.ScriptFrame = lensReadOnly Property? Script this.GetScriptFrame this
        member this.GetCreationTimeStamp world = World.getGroupCreationTimeStamp this world
        member this.CreationTimeStamp = lensReadOnly Property? CreationTimeStamp this.GetCreationTimeStamp this
        member this.GetId world = World.getGroupId this world
        member this.Id = lensReadOnly Property? Id this.GetId this

        member this.ChangeEvent propertyName = Events.Change propertyName --> this
        member this.RegisterEvent = Events.Register --> this
        member this.UnregisteringEvent = Events.Unregistering --> this
        member this.UpdateEvent = Events.Update --> this
        member this.PostUpdateEvent = Events.PostUpdate --> this

        /// Try to get a property value and type.
        member this.TryGetProperty propertyName world =
            let mutable property = Unchecked.defaultof<_>
            if World.tryGetGroupProperty (propertyName, this, world, &property)
            then Some property
            else None

        /// Get a property value and type.
        member this.GetProperty propertyName world = World.getGroupProperty propertyName this world

        /// Get a property value.
        member this.Get<'a> propertyName world : 'a = (World.getGroupProperty propertyName this world).PropertyValue :?> 'a

        /// Try to set a property value with explicit type.
        member this.TrySetProperty propertyName property world = World.trySetGroupProperty propertyName property this world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world = World.setGroupProperty propertyName property this world |> snd'

        /// Set a property value.
        member this.Set<'a> propertyName (value : 'a) world = World.setGroupProperty propertyName { PropertyType = typeof<'a>; PropertyValue = value } this world |> snd'

        /// Check that a group is selected.
        member this.IsSelected world =
            let gameState = World.getGameState world
            match gameState.OmniScreenOpt with
            | Some omniScreen when Address.head this.GroupAddress = Address.head omniScreen.ScreenAddress -> true
            | _ ->
                match gameState.SelectedScreenOpt with
                | Some screen when Address.head this.GroupAddress = Address.head screen.ScreenAddress -> true
                | _ -> false

        /// Check that a group exists in the world.
        member this.Exists world = World.getGroupExists this world

        /// Check that a group dispatches in the same manner as the dispatcher with the given type.
        member this.Is (dispatcherType, world) = Reflection.dispatchesAs dispatcherType (this.GetDispatcher world)

        /// Check that a group dispatches in the same manner as the dispatcher with the given type.
        member this.Is<'a> world = this.Is (typeof<'a>, world)

        /// Resolve a relation in the context of a group.
        member this.Resolve relation = resolve<Group> this relation

        /// Relate a group to a simulant.
        member this.Relate simulant = relate<Group> this simulant

        /// Get a group's change event address.
        member this.GetChangeEvent propertyName = Events.Change propertyName --> this.GroupAddress

        /// Try to signal a group.
        member this.TrySignal signal world = (this.GetDispatcher world).TrySignal (signal, this, world)

    type World with

        static member internal updateGroup (group : Group) world =

            // update via dispatcher
            let dispatcher = group.GetDispatcher world
            let world = dispatcher.Update (group, world)

            // publish update event
            let eventTrace = EventTrace.debug "World" "updateGroup" "" EventTrace.empty
            World.publishPlus () (Events.Update --> group) eventTrace Simulants.Game false false world

        static member internal postUpdateGroup (group : Group) world =

            // post-update via dispatcher
            let dispatcher = group.GetDispatcher world
            let world = dispatcher.PostUpdate (group, world)

            // publish post-update event
            let eventTrace = EventTrace.debug "World" "postUpdateGroup" "" EventTrace.empty
            World.publishPlus () (Events.PostUpdate --> group) eventTrace Simulants.Game false false world

        static member internal actualizeGroup (group : Group) world =
            let dispatcher = group.GetDispatcher world
            dispatcher.Actualize (group, world)

        /// Get all the groups in a screen.
        [<FunctionBinding>]
        static member getGroups (screen : Screen) world =
            match Address.getNames screen.ScreenAddress with
            | [|screenName|] ->
                match UMap.tryFind screenName (World.getScreenDirectory world) with
                | Some groupDirectory -> groupDirectory.Value |> UMap.toSeq |> Seq.map (fun (_, entry) -> entry.Key)
                | None -> failwith ("Invalid screen address '" + scstring screen.ScreenAddress + "'.")
            | _ -> failwith ("Invalid screen address '" + scstring screen.ScreenAddress + "'.")

        /// Create a group and add it to the world.
        [<FunctionBinding "createGroup">]
        static member createGroup4 dispatcherName nameOpt (screen : Screen) world =
            let dispatchers = World.getGroupDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ("Could not find a GroupDispatcher named '" + dispatcherName + "'.")
            let groupState = GroupState.make nameOpt dispatcher
            let groupState = Reflection.attachProperties GroupState.copy groupState.Dispatcher groupState world
            let group = Group (screen.ScreenAddress <-- ntoa<Group> groupState.Name)
            let world =
                if World.getGroupExists group world then
                    if group.GetDestroying world
                    then World.destroyGroupImmediate group world
                    else failwith ("Group '" + scstring group + " already exists and cannot be created."); world
                else world
            let world = World.addGroup false groupState group world
            (group, world)

        /// Create a group from a simulant descriptor.
        static member createGroup3 descriptor screen world =
            let (group, world) =
                World.createGroup4 descriptor.SimulantDispatcherName descriptor.SimulantNameOpt screen world
            let world =
                List.fold (fun world (propertyName, property) ->
                    World.setGroupProperty propertyName property group world |> snd')
                    world descriptor.SimulantProperties
            let world =
                List.fold (fun world childDescriptor ->
                    World.createEntity4 DefaultOverlay childDescriptor group world |> snd)
                    world descriptor.SimulantChildren
            (group, world)

        /// Create a group and add it to the world.
        static member createGroup<'d when 'd :> GroupDispatcher> nameOpt screen world =
            World.createGroup4 typeof<'d>.Name nameOpt screen world

        /// Destroy a group in the world immediately. Can be dangerous if existing in-flight publishing depends on the
        /// group's existence. Consider using World.destroyGroup instead.
        static member destroyGroupImmediate (group : Group) world =
            let world = World.tryRemoveSimulantFromDestruction group world
            let destroyEntitiesImmediate group world =
                let entities = World.getEntities group world
                World.destroyEntitiesImmediate entities world
            EventSystemDelegate.cleanEventAddressCache group.GroupAddress
            World.removeGroup3 destroyEntitiesImmediate group world

        /// Destroy a group in the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyGroup (group : Group) world =
            World.addSimulantToDestruction group world

        /// Destroy multiple groups in the world immediately. Can be dangerous if existing in-flight publishing depends
        /// on any of the groups' existences. Consider using World.destroyGroups instead.
        static member destroyGroupsImmediate (groups : Group seq) world =
            List.foldBack
                (fun group world -> World.destroyGroupImmediate group world)
                (List.ofSeq groups)
                world

        /// Destroy multiple groups from the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyGroups groups world =
            World.frame (World.destroyGroupsImmediate groups) world

        /// Write a group to a group descriptor.
        static member writeGroup group groupDescriptor world =
            let writeEntities group groupDescriptor world =
                let entities = World.getEntities group world
                World.writeEntities entities groupDescriptor world
            World.writeGroup4 writeEntities group groupDescriptor world

        /// Write multiple groups to a screen descriptor.
        static member writeGroups groups screenDescriptor world =
            groups |>
            Seq.sortBy (fun (group : Group) -> group.GetCreationTimeStamp world) |>
            Seq.filter (fun (group : Group) -> group.GetPersistent world) |>
            Seq.fold (fun groupDescriptors group -> World.writeGroup group GroupDescriptor.empty world :: groupDescriptors) screenDescriptor.GroupDescriptors |>
            fun groupDescriptors -> { screenDescriptor with GroupDescriptors = groupDescriptors }

        /// Write a group to a file.
        [<FunctionBinding>]
        static member writeGroupToFile (filePath : string) group world =
            let filePathTmp = filePath + ".tmp"
            let prettyPrinter = (SyntaxAttribute.getOrDefault typeof<GameDescriptor>).PrettyPrinter
            let groupDescriptor = World.writeGroup group GroupDescriptor.empty world
            let groupDescriptorStr = scstring groupDescriptor
            let groupDescriptorPretty = PrettyPrinter.prettyPrint groupDescriptorStr prettyPrinter
            File.WriteAllText (filePathTmp, groupDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a group from a group descriptor.
        static member readGroup groupDescriptor nameOpt screen world =
            World.readGroup5 World.readEntities groupDescriptor nameOpt screen world

        /// Read multiple groups from a screen descriptor.
        static member readGroups screenDescriptor screen world =
            List.foldBack
                (fun groupDescriptor (groups, world) ->
                    let groupNameOpt = GroupDescriptor.getNameOpt groupDescriptor
                    let (group, world) = World.readGroup groupDescriptor groupNameOpt screen world
                    (group :: groups, world))
                screenDescriptor.GroupDescriptors
                ([], world)

        /// Read a group from a file.
        [<FunctionBinding>]
        static member readGroupFromFile (filePath : string) nameOpt screen world =
            let groupDescriptorStr = File.ReadAllText filePath
            let groupDescriptor = scvalue<GroupDescriptor> groupDescriptorStr
            World.readGroup groupDescriptor nameOpt screen world

        /// Turn a groups lens into a series of live groups.
        static member expandGroups (lens : Lens<obj, World>) sieve unfold mapper origin screen world =
            let mapperGeneralized = fun i a w -> mapper i a w :> SimulantContent
            World.expandSimulants lens sieve unfold mapperGeneralized origin screen screen world

        /// Turn group content into a live group.
        static member expandGroupContent content origin screen world =
            if World.getScreenExists screen world then
                match GroupContent.expand content screen world with
                | Choice1Of3 (lens, sieve, unfold, mapper) ->
                    let world = World.expandGroups lens sieve unfold mapper origin screen world
                    (None, world)
                | Choice2Of3 (_, descriptor, handlers, binds, streams, entityFilePaths, entityContents) ->
                    let (group, world) =
                        World.createGroup3 descriptor screen world
                    let world =
                        List.fold (fun world (_, entityName, filePath) ->
                            World.readEntityFromFile filePath (Some entityName) group world |> snd)
                            world entityFilePaths
                    let world =
                        List.fold (fun world (simulant, left : World Lens, right, twoWay) ->
                            if twoWay then
                                let world = WorldModule.bind5 simulant left right world
                                WorldModule.bind5 simulant right left world
                            else WorldModule.bind5 simulant left right world)
                            world binds
                    let world =
                        List.fold (fun world (handler, address, simulant) ->
                            World.monitor (fun (evt : Event) world ->
                                let signal = handler evt
                                let owner = match origin with SimulantOrigin simulant -> simulant | FacetOrigin (simulant, _) -> simulant
                                let world = WorldModule.trySignal signal owner world
                                (Cascade, world))
                                address simulant world)
                            world handlers
                    let world =
                        List.fold (fun world (group, lens, sieve, unfold, mapper) ->
                            World.expandEntities lens sieve unfold mapper origin group group world)
                            world streams
                    let world =
                        List.fold (fun world (owner, entityContents) ->
                            List.fold (fun world entityContent ->
                                World.expandEntityContent entityContent origin owner group world |> snd)
                                world entityContents)
                            world entityContents
                    (Some group, world)
                | Choice3Of3 (groupName, filePath) ->
                    let (group, world) = World.readGroupFromFile filePath (Some groupName) screen world
                    (Some group, world)
            else (None, world)

namespace Debug
open Nu
type Group =

    /// Provides a full view of all the properties of a group. Useful for debugging such
    /// as with the Watch feature in Visual Studio.
    static member view group world = World.viewGroupProperties group world