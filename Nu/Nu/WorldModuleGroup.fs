﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Generic
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldModuleGroup =

    /// Dynamic property getters / setters.
    let internal GroupGetters = Dictionary<string, Group -> World -> Property> StringComparer.Ordinal
    let internal GroupSetters = Dictionary<string, Property -> Group -> World -> struct (bool * World)> StringComparer.Ordinal

    type World with
    
        static member private groupStateFinder (group : Group) world =
            UMap.tryFind group world.GroupStates

        static member private groupStateAdder groupState (group : Group) world =
            let screenDirectory =
                match Address.getNames group.GroupAddress with
                | [|screenName; groupName|] ->
                    match UMap.tryFind screenName world.ScreenDirectory with
                    | Some groupDirectory ->
                        match UMap.tryFind groupName groupDirectory.Value with
                        | Some entityDirectory ->
                            let groupDirectory' = UMap.add groupName (KeyValuePair (entityDirectory.Key, entityDirectory.Value)) groupDirectory.Value
                            let entityDirectory' = KeyValuePair (groupDirectory.Key, groupDirectory')
                            UMap.add screenName entityDirectory' world.ScreenDirectory
                        | None ->
                            let config = World.getCollectionConfig world
                            let entityDirectory' = (KeyValuePair (group, UMap.makeEmpty StringComparer.Ordinal config))
                            let groupDirectory' = UMap.add groupName entityDirectory' groupDirectory.Value
                            UMap.add screenName (KeyValuePair (groupDirectory.Key, groupDirectory')) world.ScreenDirectory
                    | None -> failwith ("Cannot add group '" + scstring group + "' to non-existent screen.")
                | _ -> failwith ("Invalid group address '" + scstring group.GroupAddress + "'.")
            let groupStates = UMap.add group groupState world.GroupStates
            World.choose { world with ScreenDirectory = screenDirectory; GroupStates = groupStates }

        static member private groupStateRemover (group : Group) world =
            let screenDirectory =
                match Address.getNames group.GroupAddress with
                | [|screenName; groupName|] ->
                    match UMap.tryFind screenName world.ScreenDirectory with
                    | Some groupDirectory ->
                        let groupDirectory' = UMap.remove groupName groupDirectory.Value
                        UMap.add screenName (KeyValuePair (groupDirectory.Key, groupDirectory')) world.ScreenDirectory
                    | None -> failwith ("Cannot remove group '" + scstring group + "' from non-existent screen.")
                | _ -> failwith ("Invalid group address '" + scstring group.GroupAddress + "'.")
            let groupStates = UMap.remove group world.GroupStates
            World.choose { world with ScreenDirectory = screenDirectory; GroupStates = groupStates }

        static member private groupStateSetter groupState (group : Group) world =
#if DEBUG
            if not (UMap.containsKey group world.GroupStates) then
                failwith ("Cannot set the state of a non-existent group '" + scstring group + "'")
#endif
            let groupStates = UMap.add group groupState world.GroupStates
            World.choose { world with GroupStates = groupStates }

        static member private addGroupState groupState group world =
            World.groupStateAdder groupState group world

        static member private removeGroupState group world =
            World.groupStateRemover group world

        static member private publishGroupChange propertyName (propertyValue : obj) (group : Group) world =

            // publish change binding
            let world =
                World.publishChangeBinding propertyName group world

            // publish event binding
            let world =
                let changeData = { Name = propertyName; Value = propertyValue }
                let groupNames = Address.getNames group.GroupAddress
                let changeEventAddress = rtoa<ChangeData> [|"Change"; propertyName; "Event"; groupNames.[0]; groupNames.[1]|]
                let eventTrace = EventTrace.debug "World" "publishGroupChange" "" EventTrace.empty
                World.publishPlus changeData changeEventAddress eventTrace group false false world

            // fin
            world

        static member private getGroupStateOpt group world =
            World.groupStateFinder group world

        static member internal getGroupState group world =
            match World.getGroupStateOpt group world with
            | Some groupState -> groupState
            | None -> failwith ("Could not find group '" + scstring group + "'.")

        static member internal getGroupXtensionProperties group world =
            let groupState = World.getGroupState group world
            groupState.Xtension |> Xtension.toSeq |> Seq.toList

        static member private setGroupState groupState group world =
            World.groupStateSetter groupState group world

        static member private updateGroupStateWithoutEvent updater group world =
            let groupStateOpt = updater (World.getGroupState group world)
            match groupStateOpt :> obj with
            | null -> struct (false, world)
            | _ -> struct (true, World.setGroupState groupStateOpt group world)

        static member private updateGroupState updater propertyName propertyValue group world =
            let struct (changed, world) = World.updateGroupStateWithoutEvent updater group world
            let world =
                if changed
                then World.publishGroupChange propertyName propertyValue group world
                else world
            struct (changed, world)

        /// Check that a group exists in the world.
        static member internal getGroupExists group world =
            Option.isSome (World.getGroupStateOpt group world)

        static member internal getGroupModelProperty group world = (World.getGroupState group world).Model
        static member internal getGroupModel<'a> group world = (World.getGroupState group world).Model.DesignerValue :?> 'a
        static member internal getGroupDispatcher group world = (World.getGroupState group world).Dispatcher
        static member internal getGroupVisible group world = (World.getGroupState group world).Visible
        static member internal setGroupVisible value group world = World.updateGroupState (fun groupState -> if value <> groupState.Visible then { groupState with Visible = value } else Unchecked.defaultof<_>) Property? Visible value group world
        static member internal getGroupPersistent group world = (World.getGroupState group world).Persistent
        static member internal setGroupPersistent value group world = World.updateGroupState (fun groupState -> if value <> groupState.Persistent then { groupState with Persistent = value } else Unchecked.defaultof<_>) Property? Persistent value group world
        static member internal getGroupDestroying (group : Group) world = List.exists ((=) (group :> Simulant)) world.WorldExtension.DestructionListRev
        static member internal getGroupCreationTimeStamp group world = (World.getGroupState group world).CreationTimeStamp
        static member internal getGroupScriptFrame group world = (World.getGroupState group world).ScriptFrame
        static member internal setGroupScriptFrame value group world = World.updateGroupState (fun groupState -> if value <> groupState.ScriptFrame then { groupState with ScriptFrame = value } else Unchecked.defaultof<_>) Property? ScriptFrame value group world
        static member internal getGroupName group world = (World.getGroupState group world).Name
        static member internal getGroupId group world = (World.getGroupState group world).Id
        
        static member internal setGroupModelProperty (value : DesignerProperty) group world =
            World.updateGroupState
                (fun groupState ->
                    if value.DesignerValue =/= groupState.Model.DesignerValue
                    then { groupState with Model = { groupState.Model with DesignerValue = value.DesignerValue }}
                    else Unchecked.defaultof<_>)
                Property? Model value.DesignerValue group world

        static member internal setGroupModel<'a> (value : 'a) group world =
            World.updateGroupState
                (fun groupState ->
                    let valueObj = value :> obj
                    if valueObj =/= groupState.Model.DesignerValue
                    then { groupState with Model = { DesignerType = typeof<'a>; DesignerValue = valueObj }}
                    else Unchecked.defaultof<_>)
                Property? Model value group world

        static member internal tryGetGroupProperty (propertyName, group, world, property : _ outref) =
            if World.getGroupExists group world then
                match GroupGetters.TryGetValue propertyName with
                | (true, getter) -> property <- getter group world; true
                | (false, _) -> GroupState.tryGetProperty (propertyName, World.getGroupState group world, &property)
            else false

        static member internal getGroupProperty propertyName group world =
            match GroupGetters.TryGetValue propertyName with
            | (true, getter) -> getter group world
            | (false, _) ->
                let mutable property = Unchecked.defaultof<_>
                match GroupState.tryGetProperty (propertyName, World.getGroupState group world, &property) with
                | true -> property
                | false -> failwithf "Could not find property '%s'." propertyName

        static member internal trySetGroupPropertyFast propertyName property group world =
            if World.getGroupExists group world then
                match GroupSetters.TryGetValue propertyName with
                | (true, setter) -> setter property group world |> snd'
                | (false, _) ->
                    let mutable success = false // bit of a hack to get additional state out of the lambda
                    let struct (_, world) =
                        World.updateGroupState
                            (fun groupState ->
                                let mutable propertyOld = Unchecked.defaultof<_>
                                match GroupState.tryGetProperty (propertyName, groupState, &propertyOld) with
                                | true ->
                                    if property.PropertyValue =/= propertyOld.PropertyValue then
                                        let struct (successInner, gameState) = GroupState.trySetProperty propertyName property groupState
                                        success <- successInner
                                        gameState
                                    else Unchecked.defaultof<_>
                                | false -> Unchecked.defaultof<_>)
                            propertyName property.PropertyValue group world
                    world
            else world

        static member internal trySetGroupProperty propertyName property group world =
            if World.getGroupExists group world then
                match GroupSetters.TryGetValue propertyName with
                | (true, setter) ->
                    let struct (changed, world) = setter property group world
                    struct (true, changed, world)
                | (false, _) ->
                    let mutable success = false // bit of a hack to get additional state out of the lambda
                    let struct (changed, world) =
                        World.updateGroupState
                            (fun groupState ->
                                let mutable propertyOld = Unchecked.defaultof<_>
                                match GroupState.tryGetProperty (propertyName, groupState, &propertyOld) with
                                | true ->
                                    if property.PropertyValue =/= propertyOld.PropertyValue then
                                        let struct (successInner, gameState) = GroupState.trySetProperty propertyName property groupState
                                        success <- successInner
                                        gameState
                                    else Unchecked.defaultof<_>
                                | false -> Unchecked.defaultof<_>)
                            propertyName property.PropertyValue group world
                    struct (success, changed, world)
            else (false, false, world)

        static member internal setGroupProperty propertyName property group world =
            if World.getGroupExists group world then
                match GroupSetters.TryGetValue propertyName with
                | (true, setter) -> setter property group world
                | (false, _) ->
                    World.updateGroupState
                        (fun groupState ->
                            let propertyOld = GroupState.getProperty propertyName groupState
                            if property.PropertyValue =/= propertyOld.PropertyValue
                            then GroupState.setProperty propertyName property groupState
                            else Unchecked.defaultof<_>)
                        propertyName property.PropertyValue group world
            else struct (false, world)

        static member internal attachGroupProperty propertyName property group world =
            if World.getGroupExists group world then
                let struct (_, world) =
                    World.updateGroupState
                        (fun groupState -> GroupState.attachProperty propertyName property groupState)
                        propertyName property.PropertyValue group world
                world
            else failwith ("Cannot attach group property '" + propertyName + "'; group '" + group.Name + "' is not found.")

        static member internal detachGroupProperty propertyName group world =
            if World.getGroupExists group world then
                let struct (_, world) =
                    World.updateGroupStateWithoutEvent
                        (fun groupState -> GroupState.detachProperty propertyName groupState)
                        group world
                world
            else failwith ("Cannot detach group property '" + propertyName + "'; group '" + group.Name + "' is not found.")

        static member internal registerGroup group world =
            let dispatcher = World.getGroupDispatcher group world
            let world = dispatcher.Register (group, world)
            let eventTrace = EventTrace.debug "World" "registerGroup" "" EventTrace.empty
            World.publishPlus () (rtoa<unit> [|"Register"; "Event"|] --> group) eventTrace group true false world

        static member internal unregisterGroup group world =
            let dispatcher = World.getGroupDispatcher group world
            let eventTrace = EventTrace.debug "World" "unregisteringGroup" "" EventTrace.empty
            let world = World.publishPlus () (rtoa<unit> [|"Unregistering"; "Event"|] --> group) eventTrace group true false world
            dispatcher.Unregister (group, world)

        static member internal addGroup mayReplace groupState group world =
            let isNew = not (World.getGroupExists group world)
            if isNew || mayReplace then
                let world = World.addGroupState groupState group world
                if isNew then World.registerGroup group world else world
            else failwith ("Adding a group that the world already contains '" + scstring group + "'.")

        static member internal removeGroup3 removeEntities group world =
            if World.getGroupExists group world then
                let world = World.unregisterGroup group world
                let world = removeEntities group world
                World.removeGroupState group world
            else world

        static member internal writeGroup4 writeEntities group groupDescriptor world =
            let groupState = World.getGroupState group world
            let groupDispatcherName = getTypeName groupState.Dispatcher
            let groupDescriptor = { groupDescriptor with GroupDispatcherName = groupDispatcherName }
            let getGroupProperties = Reflection.writePropertiesFromTarget tautology3 groupDescriptor.GroupProperties groupState
            let groupDescriptor = { groupDescriptor with GroupProperties = getGroupProperties }
            writeEntities group groupDescriptor world

        static member internal readGroup5 readEntities groupDescriptor nameOpt (screen : Screen) world =

            // make the dispatcher
            let dispatcherName = groupDescriptor.GroupDispatcherName
            let dispatchers = World.getGroupDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None ->
                    Log.info ("Could not find GroupDispatcher '" + dispatcherName + "'.")
                    let dispatcherName = typeof<GroupDispatcher>.Name
                    Map.find dispatcherName dispatchers

            // make the group state and populate its properties
            let groupState = GroupState.make None dispatcher
            let groupState = Reflection.attachProperties GroupState.copy groupState.Dispatcher groupState world
            let groupState = Reflection.readPropertiesToTarget GroupState.copy groupDescriptor.GroupProperties groupState

            // apply the name if one is provided
            let groupState =
                match nameOpt with
                | Some name -> { groupState with Name = name }
                | None -> groupState

            // add the group's state to the world
            let group = Group (screen.ScreenAddress <-- ntoa<Group> groupState.Name)
            let world = World.addGroup true groupState group world

            // read the group's entities
            let world = readEntities groupDescriptor group world |> snd
            (group, world)

        /// View all of the properties of a group.
        static member internal viewGroupProperties group world =
            let state = World.getGroupState group world
            let properties = World.getProperties state
            properties |> Array.ofList |> Array.map a_c

    /// Initialize property getters.
    let private initGetters () =
        GroupGetters.Add ("Dispatcher", fun group world -> { PropertyType = typeof<GroupDispatcher>; PropertyValue = World.getGroupDispatcher group world })
        GroupGetters.Add ("Model", fun group world -> let designerProperty = World.getGroupModelProperty group world in { PropertyType = designerProperty.DesignerType; PropertyValue = designerProperty.DesignerValue })
        GroupGetters.Add ("Visible", fun group world -> { PropertyType = typeof<single>; PropertyValue = World.getGroupVisible group world })
        GroupGetters.Add ("Persistent", fun group world -> { PropertyType = typeof<bool>; PropertyValue = World.getGroupPersistent group world })
        GroupGetters.Add ("Destroying", fun group world -> { PropertyType = typeof<bool>; PropertyValue = World.getGroupDestroying group world })
        GroupGetters.Add ("ScriptFrame", fun group world -> { PropertyType = typeof<Scripting.ProceduralFrame list>; PropertyValue = World.getGroupScriptFrame group world })
        GroupGetters.Add ("CreationTimeStamp", fun group world -> { PropertyType = typeof<int64>; PropertyValue = World.getGroupCreationTimeStamp group world })
        GroupGetters.Add ("Name", fun group world -> { PropertyType = typeof<string>; PropertyValue = World.getGroupName group world })
        GroupGetters.Add ("Id", fun group world -> { PropertyType = typeof<Guid>; PropertyValue = World.getGroupId group world })
        
    /// Initialize property setters.
    let private initSetters () =
        GroupSetters.Add ("Model", fun property group world -> World.setGroupModelProperty { DesignerType = property.PropertyType; DesignerValue = property.PropertyValue } group world)
        GroupSetters.Add ("Visible", fun property group world -> World.setGroupVisible (property.PropertyValue :?> bool) group world)
        GroupSetters.Add ("Persistent", fun property group world -> World.setGroupPersistent (property.PropertyValue :?> bool) group world)
        
    /// Initialize getters and setters
    let internal init () =
        initGetters ()
        initSetters ()