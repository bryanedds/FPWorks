﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.ComponentModel
open System.Reflection
open System.Runtime.CompilerServices
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldSimulantModule =

    type World with

        static member internal addSimulantScriptUnsubscription =
            WorldModule.addSimulantScriptUnsubscription

        static member internal unsubscribeSimulantScripts =
            WorldModule.unsubscribeSimulantScripts

        static member internal tryGetState (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> World.getEntityState entity world :> SimulantState |> Some
            | :? Group as group -> World.getGroupState group world :> SimulantState |> Some
            | :? Screen as screen -> World.getScreenState screen world :> SimulantState |> Some
            | :? Game -> World.getGameState world :> SimulantState |> Some
            | _ -> None

        static member internal getState (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> World.getEntityState entity world :> SimulantState
            | :? Group as group -> World.getGroupState group world :> SimulantState
            | :? Screen as screen -> World.getScreenState screen world :> SimulantState
            | :? Game -> World.getGameState world :> SimulantState
            | _ -> failwithumf ()

        static member internal tryGetProperty (name, simulant : Simulant, world, property : Property outref) =
            let namesLength = simulant.SimulantAddress.Names.Length
            if namesLength >= 3
            then World.tryGetEntityProperty (name, simulant :?> Entity, world, &property)
            else
                match namesLength with
                | 0 -> World.tryGetGameProperty (name, world, &property)
                | 1 -> World.tryGetScreenProperty (name, simulant :?> Screen, world, &property)
                | 2 -> World.tryGetGroupProperty (name, simulant :?> Group, world, &property)
                | _ -> failwithumf ()

        static member internal getProperty name (simulant : Simulant) world =
            let namesLength = simulant.SimulantAddress.Names.Length
            if namesLength >= 3
            then World.getEntityProperty name (simulant :?> Entity) world
            else
                match namesLength with
                | 0 -> World.getGameProperty name world
                | 1 -> World.getScreenProperty name (simulant :?> Screen) world
                | 2 -> World.getGroupProperty name (simulant :?> Group) world
                | _ -> failwithumf ()

        static member internal trySetPropertyFast name property (simulant : Simulant) world =
            let namesLength = simulant.SimulantAddress.Names.Length
            if namesLength >= 3
            then World.trySetEntityPropertyFast name property (simulant :?> Entity) world
            else
                match namesLength with
                | 0 -> World.trySetGamePropertyFast name property world
                | 1 -> World.trySetScreenPropertyFast name property (simulant :?> Screen) world
                | 2 -> World.trySetGroupPropertyFast name property (simulant :?> Group) world
                | _ -> failwithumf ()

        static member internal trySetProperty name property (simulant : Simulant) world =
            let namesLength = simulant.SimulantAddress.Names.Length
            if namesLength >= 3
            then World.trySetEntityProperty name property (simulant :?> Entity) world
            else
                match namesLength with
                | 0 -> World.trySetGameProperty name property world
                | 1 -> World.trySetScreenProperty name property (simulant :?> Screen) world
                | 2 -> World.trySetGroupProperty name property (simulant :?> Group) world
                | _ -> failwithumf ()

        static member internal setProperty name property (simulant : Simulant) world =
            let namesLength = simulant.SimulantAddress.Names.Length
            if namesLength >= 3
            then World.setEntityProperty name property (simulant :?> Entity) world
            else
                match namesLength with
                | 0 -> World.setGameProperty name property world
                | 1 -> World.setScreenProperty name property (simulant :?> Screen) world
                | 2 -> World.setGroupProperty name property (simulant :?> Group) world
                | _ -> failwithumf ()

        static member internal attachProperty name property (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> World.attachEntityProperty name property entity world
            | :? Group as group -> World.attachGroupProperty name property group world
            | :? Screen as screen -> World.attachScreenProperty name property screen world
            | :? Game -> World.attachGameProperty name property world
            | _ -> failwithumf ()

        static member internal detachProperty name (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> World.detachEntityProperty name entity world
            | :? Group as group -> World.detachGroupProperty name group world
            | :? Screen as screen -> World.detachScreenProperty name screen world
            | :? Game -> World.detachGameProperty name world
            | _ -> failwithumf ()

        /// Get the given simulant's dispatcher.
        static member getDispatcher (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> entity.GetDispatcher world :> Dispatcher
            | :? Group as group -> group.GetDispatcher world :> Dispatcher
            | :? Screen as screen -> screen.GetDispatcher world :> Dispatcher
            | :? Game -> Simulants.Game.GetDispatcher world :> Dispatcher
            | _ -> failwithumf ()

        static member internal unregister (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> World.unregisterEntity entity world
            | :? Group as group -> World.unregisterGroup group world
            | :? Screen as screen -> World.unregisterScreen screen world
            | :? Game -> World.unregisterGame world
            | _ -> failwithumf ()

        static member internal register (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> World.registerEntity entity world
            | :? Group as group -> World.registerGroup group world
            | :? Screen as screen -> World.registerScreen screen world
            | :? Game -> World.registerGame world
            | _ -> failwithumf ()

        /// Expand the given simulant content.
        [<FunctionBinding>]
        static member expandContent setScreenSplash (content : SimulantContent) origin owner (parent : Simulant) (world : World) =
            match (content, parent) with
            | ((:? EntityContent as entityContent), (:? Group as group)) -> World.expandEntityContent entityContent origin owner group world |> mapFst (Option.map cast<Simulant>)
            | ((:? GroupContent as groupContent), (:? Screen as screen)) -> World.expandGroupContent groupContent origin screen world |> mapFst (Option.map cast<Simulant>)
            | ((:? ScreenContent as screenContent), (:? Game as game)) -> World.expandScreenContent setScreenSplash screenContent origin game world |> mapFst (Some << cast<Simulant>)
            | _ -> failwithumf ()

        /// Destroy the given simulant.
        [<FunctionBinding>]
        static member destroyImmediate (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> World.destroyEntityImmediate entity world
            | :? Group as group -> World.destroyGroupImmediate group world
            | :? Screen as screen -> World.destroyScreenImmediate screen world
            | _ -> failwithumf ()

        /// Destroy the given simulant.
        [<FunctionBinding>]
        static member destroy (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> World.destroyEntity entity world
            | :? Group as group -> World.destroyGroup group world
            | :? Screen as screen -> World.destroyScreen screen world
            | _ -> failwithumf ()

        /// Get the script frame in which the given simulant's script code will run.
        static member internal tryGetScriptFrame (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> Some (World.getEntityScriptFrame entity world)
            | :? Group as group -> Some (World.getGroupScriptFrame group world)
            | :? Screen as screen -> Some (World.getScreenScriptFrame screen world)
            | :? Game -> Some (World.getGameScriptFrame world)
            | _ -> failwithumf ()

        /// Attempt to get the parent of the given simulant.
        [<FunctionBinding>]
        static member tryGetParent (simulant : Simulant) world =
            ignore (world : World)
            match simulant with
            | :? Entity as entity -> Some entity.Parent
            | :? Group as group -> Some (group.Screen :> Simulant)
            | :? Screen -> Some (Simulants.Game :> Simulant)
            | :? Game -> None
            | _ -> failwithumf ()

        /// Get the parent of the given simulant.
        [<FunctionBinding>]
        static member getParent (simulant : Simulant) world =
            ignore (world : World)
            match simulant with
            | :? Entity as entity -> entity.Parent
            | :? Group as group -> group.Screen :> Simulant
            | :? Screen -> Simulants.Game :> Simulant
            | :? Game -> failwithumf ()
            | _ -> failwithumf ()

        /// Get the existing children of the given simulant.
        [<FunctionBinding>]
        static member getChildren (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> enumerable<Simulant> (World.getEntityEntities entity world)
            | :? Group as group -> enumerable<Simulant> (World.getEntities group world)
            | :? Screen as screen -> enumerable<Simulant> (World.getGroups screen world)
            | :? Game -> enumerable<Simulant> (World.getScreens world)
            | _ -> failwithumf ()

        /// Check that a simulant exists in the world.
        [<FunctionBinding>]
        static member getExists (simulant : Simulant) (world : World) =
            (world :> World EventSystem).GetSimulantExists simulant

        /// Determine if a simulant is contained by, or is the same as, the currently selected screen or the omni-screen.
        /// Game is always considered 'selected' as well.
        [<FunctionBinding>]
        static member isSelected (simulant : Simulant) world =
            match Address.getNames simulant.SimulantAddress with
            | [||] -> true
            | names ->
                let screenName = Array.head names
                match World.getOmniScreenOpt world with
                | Some omniScreen when omniScreen.Name = screenName -> true
                | _ ->
                    match World.getSelectedScreenOpt world with
                    | Some screen when screen.Name = screenName -> true
                    | _ -> false

        /// Check that a simulant is ignoring bindings.
        [<FunctionBinding>]
        static member ignorePropertyBindings (simulant : Simulant) (world : World) =
            match simulant with
            | :? Entity as entity -> entity.GetIgnorePropertyBindings world
            | :? Group -> false
            | :? Screen -> false
            | :? Game -> false
            | _ -> failwithumf ()

        /// Convert an address to a concrete simulant reference.
        static member derive address =
            let namesLength = address |> Address.getNames |> Array.length
            if namesLength >= 3
            then Entity (Address.changeType<obj, Entity> address) :> Simulant
            else
                match namesLength with
                | 0 -> Simulants.Game :> Simulant
                | 1 -> Screen (Address.changeType<obj, Screen> address) :> Simulant
                | 2 -> Group (Address.changeType<obj, Group> address) :> Simulant
                | _ -> failwithumf ()

        /// Convert an event address to the concrete simulant that it targets.
        static member deriveFromEvent event =
            let eventAddressNames = Address.getNames event
            let eventTargetIndex = Array.findIndex (fun name -> name = "Event") eventAddressNames + 1
            if eventTargetIndex < Array.length eventAddressNames then
                let eventTarget = eventAddressNames |> Array.skip eventTargetIndex |> Address.makeFromArray
                World.derive eventTarget
            else failwithumf ()

        /// Take only one event from a stream per update.
        static member internal noMoreThanOncePerUpdate (stream : Stream<'a, World>) =
            stream |>
            Stream.trackEvent4
                (fun (a, current) _ world ->
                    let previous = current
                    let current = World.getClockTime world
                    ((a, current), previous < current))
                id (Unchecked.defaultof<'a>, DateTimeOffset.MinValue) |>
            Stream.first

        /// Bind the left property to the right property.
        static member bind (left : Lens<'a, World>) (right : Lens<'a, World>) world =
            if isNull (left.This :> obj) then failwithumf ()
            WorldModule.bind5 left.This left right world

        /// Bind the right property to the left property.
        static member dnib left right world =
            World.bind right left world

        /// Link the left property with the right and the right property with the left (two-way bind).
        static member link left right world =
            let world = World.bind left right world
            let world = World.bind right left world
            world

[<AutoOpen>]
module WorldSimulantOperators =

    /// Bind the left property to the right property.
    let bind<'a> (left : Lens<'a, World>) right world = World.bind left right world

    /// Bind the right property to the left property.
    let dnib<'a> (left : Lens<'a, World>) right world = World.dnib left right world

    /// Link the left property with the right and the right property (two-way bind).
    let link<'a> (left : Lens<'a, World>) right world = World.link left right world

    /// Bind the left property to the right property.
    let inline (<=<) left right = bind left right

    /// Bind the right property to the left property.
    let inline (>=>) left right = bind right left

    /// Link the left property with the right property (two-way bind).
    let inline (>=<) left right = link left right

[<RequireQualifiedAccess>]
module PropertyDescriptor =

    /// Check that an entity contains the given property.
    let containsProperty<'s when 's :> Simulant> (property : PropertyInfo) =
        let properties = typeof<'s>.GetProperties property.Name
        Seq.exists (fun item -> item = property) properties

    /// Attempt to get the simulant's property value.
    let tryGetValue propertyDescriptor simulant world =
        let propertyName = propertyDescriptor.PropertyName
        match World.tryGetProperty (propertyName, simulant, world) with
        | (true, property) -> Some property.PropertyValue
        | (false, _) -> None

    /// Attempt to set the simulant's property value.
    let trySetValue propertyDescriptor propertyValue simulant world =
        let (propertyName, propertyType) = (propertyDescriptor.PropertyName, propertyDescriptor.PropertyType)
        let property = { PropertyType = propertyType; PropertyValue = propertyValue }
        World.trySetProperty propertyName property simulant world

    /// Get the property descriptors as constructed from the given function in the given context.
    let getPropertyDescriptors<'s when 's :> SimulantState> makePropertyDescriptor contextOpt =
        match contextOpt with
        | Some (simulant, world) ->
            // OPTIMIZATION: seqs used for speed.
            let properties = typeof<'s>.GetProperties ()
            let typeConverterAttribute = TypeConverterAttribute typeof<SymbolicConverter>
            let properties = Seq.filter (fun (property : PropertyInfo) -> property.Name <> Property? Xtension) properties
            let properties = Seq.filter (fun (property : PropertyInfo) -> property.Name <> Property? Transform) properties
            let properties = Seq.filter (fun (property : PropertyInfo) -> property.Name <> Property? Flags) properties
            let properties = Seq.filter (fun (property : PropertyInfo) -> Seq.isEmpty (property.GetCustomAttributes<ExtensionAttribute> ())) properties
            let properties = Seq.filter (fun (property : PropertyInfo) -> not (Reflection.isPropertyNonPersistentByName property.Name)) properties
            let propertyDescriptors =
                Seq.map (fun (property : PropertyInfo) ->
                    let propertyName = property.Name
                    let property = World.getProperty propertyName simulant world
                    let property = { PropertyType = property.PropertyType; PropertyName = propertyName }
                    makePropertyDescriptor (property, [|typeConverterAttribute|]))
                    properties
            let propertyDescriptors =
                match simulant :> obj with
                | :? Entity as entity ->
                    let properties' = World.getEntityXtensionProperties entity world
                    let propertyDescriptors' =
                        Seq.fold
                            (fun propertyDescriptors' (propertyName, property : Property) ->
                                if property.PropertyType = typeof<ComputedProperty> then
                                    propertyDescriptors'
                                elif not (Reflection.isPropertyNonPersistentByName propertyName) then
                                    let propertyType = property.PropertyType
                                    let propertyDescriptor = { PropertyName = propertyName; PropertyType = propertyType }
                                    let propertyDescriptor = makePropertyDescriptor (propertyDescriptor, [|typeConverterAttribute|]) : System.ComponentModel.PropertyDescriptor
                                    propertyDescriptor :: propertyDescriptors'
                                else propertyDescriptors')
                            []
                            properties'
                    Seq.append propertyDescriptors' propertyDescriptors
                | _ -> propertyDescriptors
            List.ofSeq propertyDescriptors
        | None -> []