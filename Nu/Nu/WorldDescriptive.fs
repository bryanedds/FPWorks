﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open Prime
open Nu

/// Describes a generalized simulant value independent of the engine.
/// Not used for serialization.
type [<StructuralEquality; NoComparison>] SimulantDescriptor =
    { SimulantNames : string array
      SimulantDispatcherName : string
      SimulantProperties : (string * Property) list
      SimulantChildren : SimulantDescriptor list }

/// Describes an entity value independent of the engine.
/// Used to directly serialize an entity.
type [<StructuralEquality; NoComparison>] EntityDescriptor =
    { EntityDispatcherName : string
      EntityProperties : Map<string, Symbol> }

[<RequireQualifiedAccess>]
module EntityDescriptor =

    /// Derive names from the dispatcher.
    let getNamesOpt dispatcher =
        dispatcher.EntityProperties |>
        Map.tryFind (Property? Names) |>
        Option.map symbolToValue<string array>

    /// The empty entity descriptor.
    let empty =
        { EntityDispatcherName = String.Empty
          EntityProperties = Map.empty }

/// Describes a group value independent of the engine.
/// Used to directly serialize a group.
type [<StructuralEquality; NoComparison>] GroupDescriptor =
    { GroupDispatcherName : string
      GroupProperties : Map<string, Symbol>
      EntitieDescriptors : EntityDescriptor list }

[<RequireQualifiedAccess>]
module GroupDescriptor =

    /// Derive a name from the dispatcher.
    let getNameOpt dispatcher =
        dispatcher.GroupProperties |>
        Map.tryFind (Property? Name) |>
        Option.map symbolToValue<string>

    /// The empty group descriptor.
    let empty =
        { GroupDispatcherName = String.Empty
          GroupProperties = Map.empty
          EntitieDescriptors = [] }

/// Describes a screen value independent of the engine.
/// Used to directly serialize a screen.
type [<StructuralEquality; NoComparison>] ScreenDescriptor =
    { ScreenDispatcherName : string
      ScreenProperties : Map<string, Symbol>
      GroupDescriptors : GroupDescriptor list }

[<RequireQualifiedAccess>]
module ScreenDescriptor =

    /// Derive a name from the dispatcher.
    let getNameOpt dispatcher =
        dispatcher.ScreenProperties |>
        Map.tryFind (Property? Name) |>
        Option.map symbolToValue<string>

    /// The empty screen descriptor.
    let empty =
        { ScreenDispatcherName = String.Empty
          ScreenProperties = Map.empty
          GroupDescriptors = [] }

/// Describes a game value independent of the engine.
/// Used to directly serialize a game.
type [<StructuralEquality; NoComparison>] GameDescriptor =
    { GameDispatcherName : string
      GameProperties : Map<string, Symbol>
      ScreenDescriptors : ScreenDescriptor list }

[<RequireQualifiedAccess>]
module GameDescriptor =

    /// The empty game descriptor.
    let empty =
        { GameDispatcherName = String.Empty
          GameProperties = Map.empty
          ScreenDescriptors = [] }

/// Initializes a property.
/// TODO: see if we can find a better name for this.
type [<NoEquality; NoComparison>] PropertyInitializer =
    | PropertyDefinition of PropertyDefinition
    | EventHandlerDefinition of (Event -> obj) * obj Address
    | BindDefinition of World Lens * World Lens
    | LinkDefinition of World Lens * World Lens

/// Contains primitives for describing simulants.
[<RequireQualifiedAccess>]
module Describe =

    let private initializersToProperties initializers world =
        initializers |>
        List.map (fun initializer ->
            match initializer with
            | PropertyDefinition def -> Some (def.PropertyType, def.PropertyName, def.PropertyExpr)
            | EventHandlerDefinition _ -> None
            | BindDefinition _ -> None
            | LinkDefinition _ -> None) |>
        List.definitize |>
        List.map (fun (ty, name, expr) ->
            let valueOpt =
                match expr with
                | DefineExpr value -> Some value
                | VariableExpr fn -> Some (fn world)
                | ComputedExpr _ -> None // computed property cannot be an initializer...
            match valueOpt with
            | Some value -> Some (name, { PropertyType = ty; PropertyValue = value })
            | None -> None) |>
        List.definitize

    let private initializersToEventHandlers initializers (simulant : Simulant) =
        initializers |>
        List.map (fun initializer ->
            match initializer with
            | PropertyDefinition _ -> None
            | EventHandlerDefinition (handler, partialAddress) -> Some (handler, partialAddress --> simulant.SimulantAddress, simulant)
            | BindDefinition _ -> None
            | LinkDefinition _ -> None) |>
        List.definitize

    let private initializersToBinds initializers (simulant : Simulant) =
        initializers |>
        List.map (fun initializer ->
            match initializer with
            | PropertyDefinition _ -> None
            | EventHandlerDefinition _ -> None
            | BindDefinition (left, right) -> Some (simulant, left, right, false)
            | LinkDefinition (left, right) -> Some (simulant, left, right, true)) |>
        List.definitize

    /// Describe a simulant with the given initializers and contained children.
    let simulant5 dispatcherName names (initializers : PropertyInitializer list) children simulant world =
        let properties = initializersToProperties initializers world
        let eventHandlers = initializersToEventHandlers initializers simulant
        let binds = initializersToBinds initializers simulant
        let descriptor = { SimulantNames = names; SimulantDispatcherName = dispatcherName; SimulantProperties = properties; SimulantChildren = children }
        (descriptor, eventHandlers, binds)

    /// Describe a simulant with the given initializers and contained children.
    let simulant<'d when 'd :> GameDispatcher> nameOpt initializers children simulant world =
        simulant5 typeof<'d>.Name nameOpt initializers children simulant world

    /// Describe a game with the given initializers and contained screens.
    let game5 dispatcherName (initializers : PropertyInitializer list) (screens : SimulantDescriptor list) (game : Game) world =
        simulant5 dispatcherName [||] initializers screens game world

    /// Describe a game with the given initializers and contained screens.
    let game<'d when 'd :> GameDispatcher> initializers screens game world =
        game5 typeof<'d>.Name initializers screens game world

    /// Describe a screen with the given initializers and contained groups.
    let screen5 dispatcherName nameOpt (initializers : PropertyInitializer list) (groups : SimulantDescriptor list) (screen : Screen) world =
        simulant5 dispatcherName nameOpt initializers groups screen world

    /// Describe a screen with the given initializers and contained groups.
    let screen<'d when 'd :> ScreenDispatcher> nameOpt initializers groups screen world =
        screen5 typeof<'d>.Name nameOpt initializers groups screen world

    /// Describe a group with the given initializers and contained entities.
    let group5 dispatcherName nameOpt (initializers : PropertyInitializer list) (entities : SimulantDescriptor list) (group : Group) world =
        simulant5 dispatcherName nameOpt initializers entities group world

    /// Describe a group with the given initializers and contained entities.
    let group<'d when 'd :> GroupDispatcher> initializers entities world =
        group5 typeof<'d>.Name initializers entities world

    /// Describe an entity with the given initializers.
    let entity4 dispatcherName nameOpt (initializers : PropertyInitializer list) (entity : Entity) world =
        simulant5 dispatcherName nameOpt initializers [] entity world

    /// Describe an entity with the given initializers.
    let entity<'d when 'd :> EntityDispatcher> nameOpt initializers world =
        entity4 typeof<'d>.Name nameOpt initializers world