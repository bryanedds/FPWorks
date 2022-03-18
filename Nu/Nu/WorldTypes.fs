﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Debug
open System
module internal World =

#if DEBUG
    let mutable internal Chosen = obj ()
#endif
    let mutable internal viewGame = fun (_ : obj) -> Array.create 0 (String.Empty, obj ())
    let mutable internal viewScreen = fun (_ : obj) (_ : obj) -> Array.create 0 (String.Empty, obj ())
    let mutable internal viewGroup = fun (_ : obj) (_ : obj) -> Array.create 0 (String.Empty, obj ())
    let mutable internal viewEntity = fun (_ : obj) (_ : obj) -> Array.create 0 (String.Empty, obj ())

namespace Nu
open System
open System.Collections.Generic
#if DEBUG
open System.Diagnostics
#endif
open System.Numerics
open FSharpx.Collections
open TiledSharp
open Prime
open Nu

/// Describes a Tiled tile.
type [<StructuralEquality; NoComparison; Struct>] TileDescriptor =
    { mutable Tile : TmxLayerTile
      mutable TileI : int
      mutable TileJ : int
      mutable TilePositionI : Vector2i
      mutable TilePositionF : Vector2
      mutable TileSetTileOpt : TmxTilesetTile option }

/// Describes a Tiled tile animation.
type [<StructuralEquality; NoComparison; Struct>] TileAnimationDescriptor =
    { TileAnimationRun : int
      TileAnimationDelay : int64 }

/// Describes a Tiled tile map.
type [<StructuralEquality; NoComparison>] TileMapDescriptor =
    { TileMap : TmxMap
      TileSizeI : Vector2i
      TileSizeF : Vector2
      TileMapSizeM : Vector2i
      TileMapSizeI : Vector2i
      TileMapSizeF : Vector2
      TileMapPosition : Vector2 }

/// The type of a screen transition. Incoming means a new screen is being shown, and Outgoing
/// means an existing screen being hidden.
type [<StructuralEquality; NoComparison; Struct>] TransitionType =
    | Incoming
    | Outgoing

/// The state of a screen's transition.
type [<StructuralEquality; NoComparison; Struct>] TransitionState =
    | IncomingState
    | OutgoingState
    | IdlingState

/// Describes one of a screen's transition processes.
/// TODO: figure out if this really needs to be CLIMutable.
type [<NoEquality; NoComparison; CLIMutable>] Transition =
    { TransitionType : TransitionType
      TransitionLifeTime : int64
      DissolveImageOpt : Image AssetTag option
      SongOpt : SongDescriptor option }

    /// Make a screen transition.
    static member make transitionType =
        { TransitionType = transitionType
          TransitionLifeTime = 0L
          DissolveImageOpt = None
          SongOpt = None }

/// Describes the behavior of the screen dissolving algorithm.
type [<NoEquality; NoComparison>] DissolveDescriptor =
    { IncomingTime : int64
      OutgoingTime : int64
      DissolveImage : Image AssetTag }

/// Describes the behavior of the screen splash algorithm.
type [<NoEquality; NoComparison>] SplashDescriptor =
    { DissolveDescriptor : DissolveDescriptor
      IdlingTime : int64
      SplashImageOpt : Image AssetTag option }

/// Describes the shape of a desired overlay.
type [<StructuralEquality; NoComparison>] OverlayNameDescriptor =
    | NoOverlay
    | RoutedOverlay
    | DefaultOverlay
    | ExplicitOverlay of string

/// Describes the origin of a piece of simulant content.
type [<StructuralEquality; NoComparison>] ContentOrigin =
    | SimulantOrigin of Simulant
    | FacetOrigin of Simulant * string

/// Describes the content of a simulant.
type SimulantContent =
    interface end

/// Specifies that a module contains functions that need to be considered for binding generation.
type [<AttributeUsage (AttributeTargets.Class); AllowNullLiteral>]
    ModuleBindingAttribute () =
    inherit Attribute ()

/// Specifies that a module contains functions that need to be considered for binding generation.
type [<AttributeUsage (AttributeTargets.Method); AllowNullLiteral>]
    FunctionBindingAttribute (bindingName : string) =
    inherit Attribute ()
    member this.BindingName = bindingName
    new () = FunctionBindingAttribute ""

/// Configuration parameters for Nu.
type [<StructuralEquality; NoComparison>] NuConfig =
    { RunSynchronously : bool }

    /// The default configuration for Nu.
    static member defaultConfig =
        let runSynchronously =
#if SYNCHRONOUS
            true
#else
            false
#endif
        { RunSynchronously = runSynchronously }

/// Configuration parameters for the world.
type [<StructuralEquality; NoComparison>] WorldConfig =
    { Imperative : bool
      StandAlone : bool
      UpdateRate : int64
      SdlConfig : SdlConfig
      NuConfig : NuConfig }

    /// The default configuration of the world.
    static member defaultConfig =
        { Imperative = true
          StandAlone = true
          UpdateRate = 1L
          SdlConfig = SdlConfig.defaultConfig
          NuConfig = NuConfig.defaultConfig }

/// Efficiently emulates root type casting of a Map.
/// Specialized to Nu's specific use case by not providing a TryGetValue but rather ContainsKey and GetValue since Nu
/// uses them separately. A more general implementation would only provide ToSeq and TryGetValue.
type [<NoEquality; NoComparison>] MapGeneralized =
    { Keys : IComparable List
      ContainsKey : IComparable -> bool
      GetValue : IComparable -> obj }

    /// Make a generalized map.
    static member make (map : Map<'k, 'v>) =
        { Keys =
            let list = List ()
            for key in Map.keys map do list.Add (key :> IComparable)
            list
          ContainsKey = fun (key : IComparable) ->
            Map.containsKey (key :?> 'k) map
          GetValue = fun (key : IComparable) ->
            match map.TryGetValue (key :?> 'k) with
            | (true, value) -> value :> obj
            | (false, _) -> failwithumf () }

/// Extensions for EventTrace.
module EventTrace =

    /// Record event only in debug mode.
    let debug (moduleName : string) (functionName : string) (moreInfo : string) (eventTrace : EventTrace) =
#if DEBUG
        EventTrace.record moduleName functionName moreInfo eventTrace
#else
        ignore moduleName
        ignore functionName
        ignore moreInfo
        eventTrace
#endif

    /// Record event only in all modes.
    let trace moduleName functionName moreInfo eventTrace =
        EventTrace.record moduleName functionName moreInfo eventTrace

[<AutoOpen>]
module WorldTypes =

    // EventSystem reach-arounds.
    let mutable internal handleUserDefinedCallback : obj -> obj -> obj -> Handling * obj = Unchecked.defaultof<_>
    let mutable internal handleSubscribeAndUnsubscribeEventHook : bool -> obj Address -> Simulant -> obj -> obj = Unchecked.defaultof<_>

    /// Represents an unsubscription operation for an event.
    type Unsubscription = World -> World

    /// The data required to execution screen splashing.
    and [<NoEquality; NoComparison>] Splash =
        { IdlingTime : int64
          Destination : Screen }

    /// The data for a change in the world's ambient state.
    and [<StructuralEquality; NoComparison>] AmbientChangeData = 
        { OldWorldWithOldState : World }

    /// Store origination information about a simulant physics body.
    and [<StructuralEquality; NoComparison>] BodySource =
        { Entity : Entity
          BodyId : Guid }
        static member internal fromInternal (internal_ : BodySourceInternal) =
            { Entity = internal_.Simulant :?> Entity
              BodyId = internal_.BodyId }
    
    /// Store origination information about a simulant physics shape body.
    and [<StructuralEquality; NoComparison>] BodyShapeSource =
        { Entity : Entity
          BodyId : Guid
          BodyShapeId : Guid }
        static member internal fromInternal (internal_ : BodyShapeSourceInternal) =
            { Entity = internal_.Simulant :?> Entity
              BodyId = internal_.BodyId
              BodyShapeId = internal_.ShapeId }

    /// Describes the information needed to sort simulants.
    /// OPTIMIZATION: carries related simulant to avoid GC pressure.
    /// NOTE: SortPriority can't be structified because it is currently cast to IComparable.
    and [<CustomEquality; CustomComparison>] SortPriority =
        { SortElevation : single
          SortPositionY : single
          SortTarget : Simulant }

        static member equals left right =
            left.SortElevation = right.SortElevation &&
            left.SortTarget = right.SortTarget

        static member compare left right =
            if left.SortElevation < right.SortElevation then 1
            elif left.SortElevation > right.SortElevation then -1
            elif left.SortPositionY < right.SortPositionY then -1
            elif left.SortPositionY > right.SortPositionY then 1
            else 0

        override this.GetHashCode () =
            this.SortElevation.GetHashCode ()

        override this.Equals that =
            match that with
            | :? SortPriority as that -> SortPriority.equals this that
            | _ -> failwithumf ()

        interface SortPriority IComparable with
            member this.CompareTo that =
                SortPriority.compare this that

        interface IComparable with
            member this.CompareTo that =
                match that with
                | :? SortPriority as that -> (this :> SortPriority IComparable).CompareTo that
                | _ -> failwithumf ()

    /// Generalized interface tag for dispatchers.
    and Dispatcher = interface end

    /// Generalized interface tag for simulant dispatchers.
    and SimulantDispatcher () = interface Dispatcher

    /// The default dispatcher for games.
    and GameDispatcher () =
        inherit SimulantDispatcher ()

        /// Register a game when adding it to the world.
        abstract Register : Game * World -> World
        default this.Register (_, world) = world

        /// Unregister a game when finished with the world.
        abstract Unregister : Game * World -> World
        default this.Unregister (_, world) = world

        /// Update a game.
        abstract Update : Game * World -> World
        default this.Update (_, world) = world

        /// Post-update a game.
        abstract PostUpdate : Game * World -> World
        default this.PostUpdate (_, world) = world

        /// Actualize a game.
        abstract Actualize : Game * World -> World
        default this.Actualize (_, world) = world

        /// Try to send a signal to a game.
        abstract TrySignal : obj * Game * World -> World
        default this.TrySignal (_, _, world) = world

    /// The default dispatcher for screens.
    and ScreenDispatcher () =
        inherit SimulantDispatcher ()

        /// Register a screen when adding it to the world.
        abstract Register : Screen * World -> World
        default this.Register (_, world) = world

        /// Unregister a screen when removing it from the world.
        abstract Unregister : Screen * World -> World
        default this.Unregister (_, world) = world

        /// Update a screen.
        abstract Update : Screen * World -> World
        default this.Update (_, world) = world

        /// Post-update a screen.
        abstract PostUpdate : Screen * World -> World
        default this.PostUpdate (_, world) = world

        /// Actualize a screen.
        abstract Actualize : Screen * World -> World
        default this.Actualize (_, world) = world

        /// Try to send a signal to a screen.
        abstract TrySignal : obj * Screen * World -> World
        default this.TrySignal (_, _, world) = world

    /// The default dispatcher for groups.
    and GroupDispatcher () =
        inherit SimulantDispatcher ()

        /// Register a group when adding it to a screen.
        abstract Register : Group * World -> World
        default this.Register (_, world) = world

        /// Unregister a group when removing it from a screen.
        abstract Unregister : Group * World -> World
        default this.Unregister (_, world) = world

        /// Update a group.
        abstract Update : Group * World -> World
        default this.Update (_, world) = world

        /// Post-update a group.
        abstract PostUpdate : Group * World -> World
        default this.PostUpdate (_, world) = world

        /// Actualize a group.
        abstract Actualize : Group * World -> World
        default this.Actualize (_, world) = world

        /// Try to send a signal to a group.
        abstract TrySignal : obj * Group * World -> World
        default this.TrySignal (_, _, world) = world

    /// The default dispatcher for entities.
    and EntityDispatcher () =
        inherit SimulantDispatcher ()

        static member Properties =
            [Define? Position Vector2.Zero
             Define? Size Constants.Engine.EntitySizeDefault
             Define? Angle 0.0f
             Define? Rotation 0.0f
             Define? Elevation 0.0f
             Define? Omnipresent false
             Define? Absolute false
             Define? Model { DesignerType = typeof<unit>; DesignerValue = () }
             Define? Overflow Vector2.Zero
             Define? PublishChangeBindings false
             Define? PublishChangeEvents false
             Define? Visible true
             Define? Enabled true
             Define? AlwaysUpdate false
             Define? PublishUpdates false
             Define? PublishPostUpdates false
             Define? Persistent true
             Define? IgnorePropertyBindings false]

        /// Register an entity when adding it to a group.
        abstract Register : Entity * World -> World
        default this.Register (_, world) = world

        /// Unregister an entity when removing it from a group.
        abstract Unregister : Entity * World -> World
        default this.Unregister (_, world) = world

        /// Update an entity.
        abstract Update : Entity * World -> World
        default this.Update (_, world) = world

#if !DISABLE_ENTITY_POST_UPDATE
        /// Post-update an entity.
        abstract PostUpdate : Entity * World -> World
        default this.PostUpdate (_, world) = world
#endif

        /// Actualize an entity.
        abstract Actualize : Entity * World -> World
        default this.Actualize (_, world) = world

        /// Get the quick size of an entity (the appropriate user-defined size for an entity).
        abstract GetQuickSize : Entity * World -> Vector2
        default this.GetQuickSize (_, _) = Constants.Engine.EntitySizeDefault

        /// Apply physics changes to an entity.
        abstract ApplyPhysics : Vector2 * single * Vector2 * single * Entity * World -> World
        default this.ApplyPhysics (_, _, _, _, _, world) = world

        /// Try to send a signal to an entity's facet.
        abstract TrySignalFacet : obj * string * Entity * World -> World
        default this.TrySignalFacet (_, _, _, world) = world

        /// Try to send a signal to an entity.
        abstract TrySignal : obj * Entity * World -> World
        default this.TrySignal (_, _, world) = world

    /// Dynamically augments an entity's behavior in a composable way.
    and Facet () =

        /// Register a facet when adding it to an entity.
        abstract Register : Entity * World -> World
        default this.Register (_, world) = world

        /// Unregister a facet when removing it from an entity.
        abstract Unregister : Entity * World -> World
        default this.Unregister (_, world) = world

        /// Participate in the registration of an entity's physics with the physics subsystem.
        abstract RegisterPhysics : Entity * World -> World
        default this.RegisterPhysics (_, world) = world

        /// Participate in the unregistration of an entity's physics from the physics subsystem.
        abstract UnregisterPhysics : Entity * World -> World
        default this.UnregisterPhysics (_, world) = world

        /// Update a facet.
        abstract Update : Entity * World -> World
        default this.Update (_, world) = world

#if !DISABLE_ENTITY_POST_UPDATE
        /// Post-update a facet.
        abstract PostUpdate : Entity * World -> World
        default this.PostUpdate (_, world) = world
#endif

        /// Actualize a facet.
        abstract Actualize : Entity * World -> World
        default this.Actualize (_, world) = world

        /// Participate in getting the priority with which an entity is picked in the editor.
        abstract GetQuickSize : Entity * World -> Vector2
        default this.GetQuickSize (_, _) = Constants.Engine.EntitySizeDefault

        /// Try to send a signal to a facet.
        abstract TrySignal : obj * Entity * World -> World
        default this.TrySignal (_, _, world) = world

    /// Generalized interface for simulant state.
    and SimulantState =
        interface
            abstract member GetXtension : unit -> Xtension
            end

    /// Hosts the ongoing state of a game.
    and [<NoEquality; NoComparison; CLIMutable>] GameState =
        { Dispatcher : GameDispatcher
          Xtension : Xtension
          Model : DesignerProperty
          OmniScreenOpt : Screen option
          SelectedScreenOpt : Screen option
          ScreenTransitionDestinationOpt : Screen option
          DesiredScreenOpt : Screen option
          EyeCenter : Vector2
          EyeSize : Vector2
          ScriptFrame : Scripting.DeclarationFrame
          CreationTimeStamp : int64
          Id : Guid }

        interface SimulantState with
            member this.GetXtension () = this.Xtension

        /// Make a game state value.
        static member make (dispatcher : GameDispatcher) =
            let eyeCenter = Vector2.Zero
            let eyeSize =
                v2
                    (single Constants.Render.VirtualResolutionX)
                    (single Constants.Render.VirtualResolutionY)
            { Id = Gen.id
              Dispatcher = dispatcher
              Xtension = Xtension.makeFunctional ()
              Model = { DesignerType = typeof<unit>; DesignerValue = () }
              OmniScreenOpt = None
              SelectedScreenOpt = None
              ScreenTransitionDestinationOpt = None
              DesiredScreenOpt = None
              EyeCenter = eyeCenter
              EyeSize = eyeSize
              ScriptFrame = Scripting.DeclarationFrame StringComparer.Ordinal
              CreationTimeStamp = Core.getUniqueTimeStamp () }

        /// Try to get an xtension property and its type information.
        static member tryGetProperty (propertyName, gameState, propertyRef : Property outref) =
            Xtension.tryGetProperty (propertyName, gameState.Xtension, &propertyRef)

        /// Get an xtension property and its type information.
        static member getProperty propertyName gameState =
            Xtension.getProperty propertyName gameState.Xtension

        /// Try to set an xtension property with explicit type information.
        static member trySetProperty propertyName property gameState =
            match Xtension.trySetProperty propertyName property gameState.Xtension with
            | struct (true, xtension) -> struct (true, { gameState with Xtension = xtension })
            | struct (false, _) -> struct (false, gameState)

        /// Set an xtension property with explicit type information.
        static member setProperty propertyName property gameState =
            { gameState with GameState.Xtension = Xtension.setProperty propertyName property gameState.Xtension }

        /// Attach an xtension property.
        static member attachProperty name property gameState =
            { gameState with GameState.Xtension = Xtension.attachProperty name property gameState.Xtension }

        /// Detach an xtension property.
        static member detachProperty name gameState =
            let xtension = Xtension.detachProperty name gameState.Xtension
            { gameState with GameState.Xtension = xtension }

        /// Copy a game such as when, say, you need it to be mutated with reflection but you need to preserve persistence.
        static member copy this =
            { this with GameState.Dispatcher = this.Dispatcher }

    /// Hosts the ongoing state of a screen.
    and [<NoEquality; NoComparison; CLIMutable>] ScreenState =
        { Dispatcher : ScreenDispatcher
          Xtension : Xtension
          Model : DesignerProperty
          Ecs : World Ecs
          TransitionState : TransitionState
          TransitionUpdates : int64
          Incoming : Transition
          Outgoing : Transition
          SplashOpt : Splash option
          Persistent : bool
          ScriptFrame : Scripting.DeclarationFrame
          CreationTimeStamp : int64
          Id : Guid
          Name : string }

        interface SimulantState with
            member this.GetXtension () = this.Xtension

        /// Make a screen state value.
        static member make nameOpt (dispatcher : ScreenDispatcher) ecs =
            let (id, name) = Gen.idAndNameIf nameOpt
            { Dispatcher = dispatcher
              Xtension = Xtension.makeFunctional ()
              Model = { DesignerType = typeof<unit>; DesignerValue = () }
              Ecs = ecs
              TransitionState = IdlingState
              TransitionUpdates = 0L // TODO: roll this field into Incoming/OutgoingState values
              Incoming = Transition.make Incoming
              Outgoing = Transition.make Outgoing
              SplashOpt = None
              Persistent = true
              ScriptFrame = Scripting.DeclarationFrame StringComparer.Ordinal
              CreationTimeStamp = Core.getUniqueTimeStamp ()
              Id = id
              Name = name }

        /// Try to get an xtension property and its type information.
        static member tryGetProperty (propertyName, screenState, propertyRef : Property outref) =
            Xtension.tryGetProperty (propertyName, screenState.Xtension, &propertyRef)

        /// Get an xtension property and its type information.
        static member getProperty propertyName screenState =
            Xtension.getProperty propertyName screenState.Xtension

        /// Try to set an xtension property with explicit type information.
        static member trySetProperty propertyName property screenState =
            match Xtension.trySetProperty propertyName property screenState.Xtension with
            | struct (true, xtension) -> struct (true, { screenState with Xtension = xtension })
            | struct (false, _) -> struct (false, screenState)

        /// Set an xtension property with explicit type information.
        static member setProperty propertyName property screenState =
            { screenState with ScreenState.Xtension = Xtension.setProperty propertyName property screenState.Xtension }

        /// Attach an xtension property.
        static member attachProperty name property screenState =
            { screenState with ScreenState.Xtension = Xtension.attachProperty name property screenState.Xtension }

        /// Detach an xtension property.
        static member detachProperty name screenState =
            let xtension = Xtension.detachProperty name screenState.Xtension
            { screenState with ScreenState.Xtension = xtension }

        /// Copy a screen such as when, say, you need it to be mutated with reflection but you need to preserve persistence.
        static member copy this =
            { this with ScreenState.Dispatcher = this.Dispatcher }

    /// Hosts the ongoing state of a group.
    and [<NoEquality; NoComparison; CLIMutable>] GroupState =
        { Dispatcher : GroupDispatcher
          Xtension : Xtension
          Model : DesignerProperty
          Visible : bool
          Persistent : bool
          ScriptFrame : Scripting.DeclarationFrame
          CreationTimeStamp : int64
          Id : Guid
          Name : string }

        interface SimulantState with
            member this.GetXtension () = this.Xtension

        /// Make a group state value.
        static member make nameOpt (dispatcher : GroupDispatcher) =
            let (id, name) = Gen.idAndNameIf nameOpt
            { Dispatcher = dispatcher
              Xtension = Xtension.makeFunctional ()
              Model = { DesignerType = typeof<unit>; DesignerValue = () }
              Visible = true
              Persistent = true
              ScriptFrame = Scripting.DeclarationFrame StringComparer.Ordinal
              CreationTimeStamp = Core.getUniqueTimeStamp ()
              Id = id
              Name = name }

        /// Try to get an xtension property and its type information.
        static member tryGetProperty (propertyName, groupState, propertyRef : Property outref) =
            Xtension.tryGetProperty (propertyName, groupState.Xtension, &propertyRef)

        /// Get an xtension property and its type information.
        static member getProperty propertyName groupState =
            Xtension.getProperty propertyName groupState.Xtension

        /// Try to set an xtension property with explicit type information.
        static member trySetProperty propertyName property groupState =
            match Xtension.trySetProperty propertyName property groupState.Xtension with
            | struct (true, xtension) -> struct (true, { groupState with Xtension = xtension })
            | struct (false, _) -> struct (false, groupState)

        /// Set an xtension property with explicit type information.
        static member setProperty propertyName property groupState =
            { groupState with GroupState.Xtension = Xtension.setProperty propertyName property groupState.Xtension }

        /// Attach an xtension property.
        static member attachProperty name property groupState =
            { groupState with GroupState.Xtension = Xtension.attachProperty name property groupState.Xtension }

        /// Detach an xtension property.
        static member detachProperty name groupState =
            let xtension = Xtension.detachProperty name groupState.Xtension
            { groupState with GroupState.Xtension = xtension }

        /// Copy a group such as when, say, you need it to be mutated with reflection but you need to preserve persistence.
        static member copy this =
            { this with GroupState.Dispatcher = this.Dispatcher }

    /// Hosts the ongoing state of an entity.
    /// OPTIMIZATION: ScriptFrameOpt is instantiated only when needed.
    and [<NoEquality; NoComparison; CLIMutable>] EntityState =
        { // cache line 1 (assuming 16 byte header)
          mutable Transform : Transform
          Dispatcher : EntityDispatcher
          // cache line 2
          mutable Facets : Facet array
          mutable Xtension : Xtension
          mutable Model : DesignerProperty
          mutable Overflow : Vector2
          mutable OverlayNameOpt : string option
          mutable FacetNames : string Set
          mutable ScriptFrameOpt : Scripting.DeclarationFrame
          // cache line 3
          CreationTimeStamp : int64 // just needed for ordering writes to reduce diff volumes
          Id : Guid
          Name : string }

        interface SimulantState with
            member this.GetXtension () = this.Xtension

        /// Make an entity state value.
        static member make imperative nameOpt overlayNameOpt (dispatcher : EntityDispatcher) =
            let mutable transform = Transform.makeDefault ()
            transform.Imperative <- imperative
            let (id, name) = Gen.idAndNameIf nameOpt
            { Transform = transform
              Dispatcher = dispatcher
              Facets = [||]
              Xtension = Xtension.makeEmpty imperative
              Model = { DesignerType = typeof<unit>; DesignerValue = () }
              Overflow = Vector2.Zero
              OverlayNameOpt = overlayNameOpt
              FacetNames = Set.empty
              ScriptFrameOpt = Unchecked.defaultof<_>
              CreationTimeStamp = Core.getUniqueTimeStamp ()
              Id = id
              Name = name }

        /// Copy an entity state.
        static member inline copy (entityState : EntityState) =
            { entityState with EntityState.Dispatcher = entityState.Dispatcher }

        /// Copy an entity state, invalidating the incoming reference.
        static member inline diverge (entityState : EntityState) =
            let entityState' = EntityState.copy entityState
            /// OPTIMIZATION: inlined invalidation masking for speed.
            entityState.Transform.Flags <- entityState.Transform.Flags ||| TransformMasks.InvalidatedMask
            entityState'

        /// Check that there exists an xtenstion proprty that is a runtime property.
        static member containsRuntimeProperties entityState =
            Xtension.containsRuntimeProperties entityState.Xtension

        /// Try to get an xtension property and its type information.
        static member tryGetProperty (propertyName, entityState, propertyRef : Property outref) =
            Xtension.tryGetProperty (propertyName, entityState.Xtension, &propertyRef)

        /// Get an xtension property and its type information.
        static member getProperty propertyName entityState =
            Xtension.getProperty propertyName entityState.Xtension

        /// Try to set an xtension property with explicit type information.
        static member trySetProperty propertyName property (entityState : EntityState) =
            let entityState = if entityState.Imperative then entityState else EntityState.diverge entityState
            match Xtension.trySetProperty propertyName property entityState.Xtension with
            | struct (true, xtension) ->
                entityState.Xtension <- xtension // redundant if xtension is imperative
                struct (true, entityState)
            | struct (false, _) -> struct (false, entityState)

        /// Set an xtension property with explicit type information.
        static member setProperty propertyName property (entityState : EntityState) =
            let entityState = if entityState.Imperative then entityState else EntityState.diverge entityState
            let xtension = Xtension.setProperty propertyName property entityState.Xtension
            entityState.Xtension <- xtension // redundant if xtension is imperative
            entityState

        /// Attach an xtension property.
        static member attachProperty name property (entityState : EntityState) =
            let entityState = if entityState.Imperative then entityState else EntityState.diverge entityState
            let xtension = Xtension.attachProperty name property entityState.Xtension
            entityState.Xtension <- xtension // redundant if xtension is imperative
            entityState

        /// Detach an xtension property.
        static member detachProperty name (entityState : EntityState) =
            let entityState = if entityState.Imperative then entityState else EntityState.diverge entityState
            let xtension = Xtension.detachProperty name entityState.Xtension
            entityState.Xtension <- xtension // redundant if xtension is imperative
            entityState

        /// Get an entity state's transform.
        static member getTransform entityState =
            entityState.Transform

        /// Set an entity state's transform.
        static member setTransformByRef (value : Transform inref, entityState : EntityState) =
            let entityState = if entityState.Imperative then entityState else EntityState.diverge entityState
            Transform.assignByRef (&value, &entityState.Transform)
            entityState

        // Member properties; only for use by internal reflection facilities.
        member this.Position with get () = this.Transform.Position and set value = this.Transform.Position <- value
        member this.Size with get () = this.Transform.Size and set value = this.Transform.Size <- value
        member this.Angle with get () = Math.radiansToDegrees this.Transform.Rotation and set value = this.Transform.Rotation <- Math.degreesToRadians value
        member this.Rotation with get () = this.Transform.Rotation and set value = this.Transform.Rotation <- value
        member this.Elevation with get () = this.Transform.Elevation and set value = this.Transform.Elevation <- value
        member internal this.Active with get () = this.Transform.Active and set value = this.Transform.Active <- value
        member internal this.Dirty with get () = this.Transform.Dirty and set value = this.Transform.Dirty <- value
        member internal this.Invalidated with get () = this.Transform.Invalidated and set value = this.Transform.Invalidated <- value
        member this.Omnipresent with get () = this.Transform.Omnipresent and set value = this.Transform.Omnipresent <- value
        member this.Absolute with get () = this.Transform.Absolute and set value = this.Transform.Absolute <- value
        member this.Imperative with get () = this.Transform.Imperative and set value = this.Transform.Imperative <- value
        member this.PublishChangeBindings with get () = this.Transform.PublishChangeBindings and set value = this.Transform.PublishChangeBindings <- value
        member this.PublishChangeEvents with get () = this.Transform.PublishChangeEvents and set value = this.Transform.PublishChangeEvents <- value
        member this.Enabled with get () = this.Transform.Enabled and set value = this.Transform.Enabled <- value
        member this.Visible with get () = this.Transform.Visible and set value = this.Transform.Visible <- value
        member this.AlwaysUpdate with get () = this.Transform.AlwaysUpdate and set value = this.Transform.AlwaysUpdate <- value
        member this.PublishUpdates with get () = this.Transform.PublishUpdates and set value = this.Transform.PublishUpdates <- value
        member this.PublishPostUpdates with get () = this.Transform.PublishPostUpdates and set value = this.Transform.PublishPostUpdates <- value
        member this.Persistent with get () = this.Transform.Persistent and set value = this.Transform.Persistent <- value
        member this.IgnorePropertyBindings with get () = this.Transform.IgnorePropertyBindings and set value = this.Transform.IgnorePropertyBindings <- value
        member this.Optimized with get () = this.Transform.Optimized
        member this.Bounds with get () = this.Transform.Bounds
        member this.Center with get () = this.Transform.Center
        member this.Bottom with get () = this.Transform.Bottom

    /// The game type that hosts the various screens used to navigate through a game.
    and Game (gameAddress) =

        // check that address is of correct length for a game
        do if Address.length gameAddress <> 0 then failwith "Game address must be length of 0."

        // cache the simulant address to avoid allocation
        let simulantAddress = atoa<Game, Simulant> gameAddress

        /// Create a game reference.
        new () = Game (Address.empty)

        /// The address of the game.
        member this.GameAddress = gameAddress

#if DEBUG
        /// Get the latest value of a game's properties.
        [<DebuggerBrowsable (DebuggerBrowsableState.RootHidden)>]
        member private this.View = Debug.World.viewGame Debug.World.Chosen
#endif
        
        /// Helper for accessing game lenses.
        static member Lens = Unchecked.defaultof<Game>

        /// Concatenate an address with a game's address, forcing the type of first address.
        static member (-->) (address : 'a Address, game : Game) =
            match box game with
            | null -> address // HACK: this case is a hack to be able to insert events into the elmish event handler
            | _ -> acatff address game.GameAddress

        override this.ToString () =
            scstring this.GameAddress

        override this.Equals that =
            match that with
            | :? Game as that -> this.GameAddress === that.GameAddress
            | _ -> false

        override this.GetHashCode () =
            Address.hash this.GameAddress

        interface Simulant with
            member this.SimulantAddress = simulantAddress
            end

        interface Game IComparable with
            member this.CompareTo that =
                Address.compare this.GameAddress that.GameAddress

        interface IComparable with
            member this.CompareTo that =
                match that with
                | :? Game as that -> (this :> Game IComparable).CompareTo that
                | _ -> failwith "Invalid Game comparison (comparee not of type Game)."

    /// The screen type that allows transitioning to and from other screens, and also hosts the
    /// currently interactive groups of entities.
    and Screen (screenAddress) =

        // check that address is of correct length for a screen
        do if Address.length screenAddress <> 1 then failwith "Screen address must be length of 1."

        // cache the simulant address to avoid allocation
        let simulantAddress = atoa<Screen, Simulant> screenAddress

        /// Create a screen reference from a name string.
        new (screenName : string) = Screen (ntoa screenName)

        /// The address of the screen.
        member this.ScreenAddress = screenAddress

        /// The parent game of the screen.
        member this.Parent = Game ()

        /// Get the name of a screen.
        member inline this.Name = this.ScreenAddress.Names.[0]

#if DEBUG
        /// Get the latest value of a screen's properties.
        [<DebuggerBrowsable (DebuggerBrowsableState.RootHidden)>]
        member private this.View = Debug.World.viewScreen (this :> obj) Debug.World.Chosen
#endif

        /// Helper for accessing screen lenses.
        static member Lens = Unchecked.defaultof<Screen>

        /// Derive a group from its screen.
        static member (/) (screen : Screen, groupName) = Group (atoa<Screen, Group> screen.ScreenAddress --> ntoa groupName)

        /// Concatenate an address with a screen's address, forcing the type of first address.
        static member (-->) (address : 'a Address, screen : Screen) =
            match box screen with
            | null -> address // HACK: this case is a hack to be able to insert events into the elmish event handler
            | _ -> acatff address screen.ScreenAddress

        override this.ToString () =
            scstring this.ScreenAddress

        override this.Equals that =
            match that with
            | :? Screen as that -> this.ScreenAddress === that.ScreenAddress
            | _ -> false

        override this.GetHashCode () =
            Address.hash this.ScreenAddress

        interface Simulant with
            member this.SimulantAddress = simulantAddress
            end

        interface Screen IComparable with
            member this.CompareTo that =
                Address.compare this.ScreenAddress that.ScreenAddress

        interface IComparable with
            member this.CompareTo that =
                match that with
                | :? Screen as that -> (this :> Screen IComparable).CompareTo that
                | _ -> failwith "Invalid Screen comparison (comparee not of type Screen)."

    /// Forms a logical group of entities.
    and Group (groupAddress) =

        // check that address is of correct length for a group
        do if Address.length groupAddress <> 2 then failwith "Group address must be length of 2."

        // cache the simulant address to avoid allocation
        let simulantAddress = atoa<Group, Simulant> groupAddress

        /// Create a group reference from an address string.
        new (groupAddressStr : string) = Group (stoa groupAddressStr)

        /// Create a group reference from a list of names.
        new (groupNames : string array) = Group (rtoa groupNames)

        /// Create a group reference from a list of names.
        new (groupNames : string list) = Group (ltoa groupNames)

        /// Create a group reference from a the required names.
        new (screenName : string, groupName : string) = Group [screenName; groupName]

        /// The address of the group.
        member this.GroupAddress = groupAddress

        /// The parent screen of the group.
        member this.Parent = let names = this.GroupAddress.Names in Screen names.[0]

        /// Get the name of a group.
        member this.Name = Address.getName this.GroupAddress

#if DEBUG
        /// Get the latest value of a group's properties.
        [<DebuggerBrowsable (DebuggerBrowsableState.RootHidden)>]
        member private this.View = Debug.World.viewGroup (this :> obj) Debug.World.Chosen
#endif

        /// Derive an entity from its group.
        static member (/) (group : Group, entityName) = Entity (atoa<Group, Entity> group.GroupAddress --> ntoa entityName)

        /// Concatenate an address with a group's address, forcing the type of first address.
        static member (-->) (address : 'a Address, group : Group) =
            match box group with
            | null -> address // HACK: this case is a hack to be able to insert events into the elmish event handler
            | _ -> acatff address group.GroupAddress

        /// Helper for accessing group lenses.
        static member Lens = Unchecked.defaultof<Group>

        override this.ToString () =
            scstring this.GroupAddress

        override this.Equals that =
            match that with
            | :? Group as that -> this.GroupAddress === that.GroupAddress
            | _ -> false

        override this.GetHashCode () =
            Address.hash this.GroupAddress

        interface Simulant with
            member this.SimulantAddress = simulantAddress
            end

        interface Group IComparable with
            member this.CompareTo that =
                Address.compare this.GroupAddress that.GroupAddress

        interface IComparable with
            member this.CompareTo that =
                match that with
                | :? Group as that -> (this :> Group IComparable).CompareTo that
                | _ -> failwith "Invalid Group comparison (comparee not of type Group)."

    /// The type around which the whole game engine is based! Used in combination with dispatchers to implement things
    /// like buttons, characters, blocks, and things of that sort.
    and Entity (entityAddress) =

        // check that address is of correct length for an entity
        do if Address.length entityAddress <> 3 then failwith "Entity address must be length of 3."

        /// The entity's cached state.
        let mutable entityStateOpt = Unchecked.defaultof<EntityState>

        /// Whether cidOpt has been populated with a valid value.
        let mutable cidPopulated = false

        /// The entity's cached correlation id.
        let mutable cidOpt = Unchecked.defaultof<Guid> // OPTIMIZATION: faster than Guid.Empty?

        // cache the simulant address to avoid allocation
        let simulantAddress = atoa<Entity, Simulant> entityAddress

        /// Create an entity reference from an address string.
        new (entityAddressStr : string) = Entity (stoa entityAddressStr)

        /// Create an entity reference from an array of names.
        new (entityNames : string array) = Entity (rtoa entityNames)

        /// Create an entity reference from a list of names.
        new (entityNames : string list) = Entity (ltoa entityNames)

        /// Create an entity reference from a the required names.
        new (screenName : string, groupName : string, entityName : string) = Entity [screenName; groupName; entityName]

        /// The address of the entity.
        member this.EntityAddress = entityAddress

        /// The parent group of the entity.
        member this.Parent = let names = this.EntityAddress.Names in Group [names.[0]; names.[1]]

        /// The cached entity state for imperative entities.
        member this.EntityStateOpt
            with get () = entityStateOpt
            and set value = entityStateOpt <- value

        /// The entity's correlation id.
        member this.Cid =
            if not cidPopulated then
                cidOpt <- entityAddress |> Address.getName |> Gen.correlate
                cidPopulated <- true
            cidOpt

        /// The entity's update event.
        member this.UpdateEvent =
            let entityNames = Address.getNames entityAddress
            rtoa<unit> [|"Update"; "Event"; entityNames.[0]; entityNames.[1]; entityNames.[2]|]

#if !DISABLE_ENTITY_POST_UPDATE
        /// The entity's post update event.
        member this.PostUpdateEvent =
            let entityNames = Address.getNames entityAddress
            rtoa<unit> [|"PostUpdate"; "Event"; entityNames.[0]; entityNames.[1]; entityNames.[2]|]
#endif

        /// Get the name of an entity.
        member this.Name = Address.getName this.EntityAddress

#if DEBUG
        /// Get the latest value of an entity's properties.
        [<DebuggerBrowsable (DebuggerBrowsableState.RootHidden)>]
        member private this.View = Debug.World.viewEntity (this :> obj) Debug.World.Chosen
#endif

        /// Helper for accessing entity lenses.
        static member Lens = Unchecked.defaultof<Entity>

        /// Concatenate an address with an entity, forcing the type of first address.
        static member (-->) (address : 'a Address, entity : Entity) =
            match box entity with
            | null -> address // HACK: this case is a hack to be able to insert events into the elmish event handler
            | _ -> acatff address entity.EntityAddress

        override this.ToString () =
            scstring this.EntityAddress

        override this.Equals that =
            match that with
            | :? Entity as that -> this.EntityAddress === that.EntityAddress
            | _ -> false

        override this.GetHashCode () =
            Address.hash this.EntityAddress

        interface Simulant with
            member this.SimulantAddress = simulantAddress
            end

        interface Entity IComparable with
            member this.CompareTo that =
                Address.compare this.EntityAddress that.EntityAddress

        interface IComparable with
            member this.CompareTo that =
                match that with
                | :? Entity as that -> (this :> Entity IComparable).CompareTo that
                | _ -> failwith "Invalid Entity comparison (comparee not of type Entity)."

    /// Describes a property's location in Nu's optimized Elmish implementation.
    and [<CustomEquality; NoComparison>] internal PropertyAddress =
        { PAName : string
          PASimulant : Simulant
          PAHash : int }

        static member equals left right =
            left.PAHash = right.PAHash &&
            left.PAName = right.PAName &&
            Address.equals left.PASimulant.SimulantAddress right.PASimulant.SimulantAddress

        static member make name (simulant : Simulant) =
            let hash = hash name ^^^ hash simulant.SimulantAddress
            { PAName = name
              PASimulant = simulant
              PAHash = hash }

        interface PropertyAddress IEquatable with
            member this.Equals that =
                PropertyAddress.equals this that

        override this.GetHashCode () =
            this.PAHash

        override this.Equals that =
            match that with
            | :? PropertyAddress as that -> PropertyAddress.equals this that
            | _ -> failwithumf ()

    /// Describes a property binding for Nu's optimized Elmish implementation.
    and [<NoEquality; NoComparison>] internal PropertyBinding =
        { PBLeft : World Lens
          PBRight : World Lens
          (*mutable PBPrevious : obj ValueOption*) }

    /// Describes a content binding for Nu's optimized Elmish implementation.
    and [<NoEquality; NoComparison>] internal ContentBinding =
        { CBMapper : IComparable -> Lens<obj, World> -> World -> SimulantContent
          CBSource : Lens<MapGeneralized, World>
          CBOrigin : ContentOrigin
          CBOwner : Simulant
          CBParent : Simulant
          CBSimulantKey : Guid
          CBContentKey : Guid }

    /// Describes an binding for Nu's optimized Elmish implementation.
    and [<NoEquality; NoComparison>] internal ElmishBinding =
        | PropertyBinding of PropertyBinding
        | ContentBinding of ContentBinding

    /// Describes bindings for Nu's optimized Elmish implementation.
    and internal ElmishBindings = OMap<Guid, ElmishBinding>

    /// The world's dispatchers (including facets).
    /// 
    /// I would prefer this type to be inlined in World, but it has been extracted to its own white-box
    /// type for efficiency reasons.
    and [<ReferenceEquality; NoComparison>] internal Dispatchers =
        { GameDispatchers : Map<string, GameDispatcher>
          ScreenDispatchers : Map<string, ScreenDispatcher>
          GroupDispatchers : Map<string, GroupDispatcher>
          EntityDispatchers : Map<string, EntityDispatcher>
          Facets : Map<string, Facet>
          TryGetExtrinsic : string -> World ScriptingTrinsic option
          UpdateEntityInEntityTree : bool -> bool -> Vector4 -> Entity -> World -> World -> World
          RebuildEntityTree : World -> Entity SpatialTree }

    /// The subsystems encapsulated by the engine.
    and [<ReferenceEquality; NoComparison>] internal Subsystems =
        { PhysicsEngine : PhysicsEngine
          Renderer : Renderer
          AudioPlayer : AudioPlayer }

    /// Keeps the World from occupying more than two cache lines.
    and [<ReferenceEquality; NoComparison>] WorldExtension =
        { DestructionListRev : Simulant list
          Plugin : NuPlugin
          ScriptingEnv : Scripting.Env
          ScriptingContext : Simulant }

    /// The world, in a functional programming sense. Hosts the game object, the dependencies needed
    /// to implement a game, messages to by consumed by the various engine sub-systems, and general
    /// configuration data.
    ///
    /// NOTE: this would be better as private, but there is just too much code to fit in this file
    /// for that.
    and [<ReferenceEquality; NoComparison>] World =
        internal
            { // cache line 1 (assuming 16 byte header)
              EventSystemDelegate : World EventSystemDelegate
              EntityCachedOpt : KeyedCache<KeyValuePair<Entity, UMap<Entity, EntityState>>, EntityState>
              EntityStates : UMap<Entity, EntityState>
              GroupStates : UMap<Group, GroupState>
              ScreenStates : UMap<Screen, ScreenState>
              GameState : GameState
              // cache line 2
              mutable EntityTree : Entity SpatialTree MutantCache // mutated when Imperative
              mutable SelectedEcsOpt : World Ecs option // mutated when Imperative
              ElmishBindingsMap : UMap<PropertyAddress, ElmishBindings> // TODO: consider making this mutable when Imperative to avoid rebuilding the world value when adding an Elmish binding.
              AmbientState : World AmbientState
              Subsystems : Subsystems
              ScreenDirectory : UMap<string, KeyValuePair<Screen, UMap<string, KeyValuePair<Group, UMap<string, Entity>>>>>
              Dispatchers : Dispatchers
              WorldExtension : WorldExtension }

        interface World EventSystem with

            member this.GetConfig () =
                AmbientState.getConfig this.AmbientState

            member this.GetLiveness () =
                AmbientState.getLiveness this.AmbientState

            member this.GetSimulantExists simulant =
                // OPTIMIZATION: constant-time look-up here improve non-entity existence queries.
                match simulant.SimulantAddress.Names.Length with
                | 3 ->
                    let entity = simulant :?> Entity
                    notNull (entity.EntityStateOpt :> obj) && not entity.EntityStateOpt.Invalidated ||
                    UMap.containsKey (simulant :?> Entity) this.EntityStates
                | 2 -> UMap.containsKey (simulant :?> Group) this.GroupStates
                | 1 -> UMap.containsKey (simulant :?> Screen) this.ScreenStates
                | 0 -> true
                | _  -> false

            member this.GetGlobalSimulantSpecialized () =
                EventSystemDelegate.getGlobalSimulantSpecialized this.EventSystemDelegate

            member this.GetGlobalSimulantGeneralized () =
                EventSystemDelegate.getGlobalSimulantGeneralized this.EventSystemDelegate

            member this.GetEventSystemDelegate () =
                this.EventSystemDelegate

            member this.UpdateEventSystemDelegate updater =
                let this = { this with EventSystemDelegate = updater this.EventSystemDelegate }
#if DEBUG
                Debug.World.Chosen <- this
#endif
                this

            member this.HandleUserDefinedCallback userDefined data world =
                let (handling, worldObj) = handleUserDefinedCallback userDefined data (world :> obj)
                (handling, worldObj :?> World)

            member this.PublishEventHook (subscriber : Simulant) publisher eventData eventAddress eventTrace subscription world =
                let (handling, world) =
                    match subscriber with
                    | :? Entity -> EventSystem.publishEvent<'a, 'p, Entity, World> subscriber publisher eventData eventAddress eventTrace subscription world
                    | :? Group -> EventSystem.publishEvent<'a, 'p, Group, World> subscriber publisher eventData eventAddress eventTrace subscription world
                    | :? Screen -> EventSystem.publishEvent<'a, 'p, Screen, World> subscriber publisher eventData eventAddress eventTrace subscription world
                    | :? Game -> EventSystem.publishEvent<'a, 'p, Game, World> subscriber publisher eventData eventAddress eventTrace subscription world
                    | :? GlobalSimulantGeneralized -> EventSystem.publishEvent<'a, 'p, Simulant, World> subscriber publisher eventData eventAddress eventTrace subscription world
                    | _ -> failwithumf ()
#if DEBUG
                Debug.World.Chosen <- world
#endif
                (handling, world)

            member this.SubscribeEventHook eventAddress subscriber world =
                handleSubscribeAndUnsubscribeEventHook true eventAddress subscriber world :?> World

            member this.UnsubscribeEventHook eventAddress subscriber world =
                handleSubscribeAndUnsubscribeEventHook false eventAddress subscriber world :?> World

        interface World ScriptingSystem with

            member this.GetEnv () =
                this.WorldExtension.ScriptingEnv

            member this.TryGetExtrinsic fnName =
                this.Dispatchers.TryGetExtrinsic fnName

            member this.TryImport ty value =
                match (ty.Name, value) with
                | ("Vector2", (:? Vector2 as v2)) -> let v2p = { Vector2 = v2 } in v2p :> Scripting.Pluggable |> Scripting.Pluggable |> Some
                | ("Vector4", (:? Vector4 as v4)) -> let v4p = { Vector4 = v4 } in v4p :> Scripting.Pluggable |> Scripting.Pluggable |> Some
                | ("Vector2i", (:? Vector2i as v2i)) -> let v2ip = { Vector2i = v2i } in v2ip :> Scripting.Pluggable |> Scripting.Pluggable |> Some
                | ("Vector4i", (:? Vector4i as v4i)) -> let v4ip = { Vector4i = v4i } in v4ip :> Scripting.Pluggable |> Scripting.Pluggable |> Some
                | ("Color", (:? Color as color)) -> let colorp = { Color = color } in colorp :> Scripting.Pluggable |> Scripting.Pluggable |> Some
                | ("Game", (:? Game as game)) -> game.GameAddress |> atos |> Scripting.Keyword |> Some
                | ("Screen", (:? Screen as screen)) -> screen.ScreenAddress |> atos |> Scripting.Keyword |> Some
                | ("Group", (:? Group as group)) -> group.GroupAddress |> atos |> Scripting.Keyword |> Some
                | ("Entity", (:? Entity as entity)) -> entity.EntityAddress |> atos |> Scripting.Keyword |> Some
                | ("Simulant", (:? Simulant as simulant)) -> simulant.SimulantAddress |> atos |> Scripting.Keyword |> Some
                | (_, _) -> None

            member this.TryExport ty value =
                match (ty.Name, value) with
                | ("Vector2", Scripting.Pluggable pluggable) -> let v2 = pluggable :?> Vector2Pluggable in v2.Vector2 :> obj |> Some
                | ("Vector4", Scripting.Pluggable pluggable) -> let v4 = pluggable :?> Vector4Pluggable in v4.Vector4 :> obj |> Some
                | ("Vector2i", Scripting.Pluggable pluggable) -> let v2i = pluggable :?> Vector2iPluggable in v2i.Vector2i :> obj |> Some
                | ("Vector4i", Scripting.Pluggable pluggable) -> let v4i = pluggable :?> Vector4iPluggable in v4i.Vector4i :> obj |> Some
                | ("Color", Scripting.Pluggable pluggable) -> let color = pluggable :?> ColorPluggable in color.Color :> obj |> Some
                | ("Game", Scripting.String str) | ("Game", Scripting.Keyword str) -> str |> stoa |> Game :> obj |> Some
                | ("Screen", Scripting.String str) | ("Screen", Scripting.Keyword str) -> str |> stoa |> Screen :> obj |> Some
                | ("Group", Scripting.String str) | ("Group", Scripting.Keyword str) -> str |> stoa |> Group :> obj |> Some
                | ("Entity", Scripting.String str) | ("Entity", Scripting.Keyword str) -> str |> stoa |> Entity :> obj |> Some
                | ("Simulant", Scripting.String _) | ("Simulant", Scripting.Keyword _) -> None // TODO: see if this should be failwithumf or a violation instead.
                | (_, _) -> None

        override this.ToString () =
            ""

    /// Provides a way to make user-defined dispatchers, facets, and various other sorts of game-
    /// specific values and configurations.
    and NuPlugin () =

        /// The game dispatcher that Nu will utilize when running outside the editor.
        abstract StandAloneConfig : Type
        default this.StandAloneConfig = typeof<GameDispatcher>

        /// The screen / screen dispatcher that Nu will utilize when running inside the editor.
        abstract EditorConfig : Screen * Type
        default this.EditorConfig = (Screen "Screen", typeof<ScreenDispatcher>)

        /// Make a list of keyed values to hook into the engine.
        abstract MakeKeyedValues : World -> ((Guid * obj) list) * World
        default this.MakeKeyedValues world = ([], world)

        /// Make the Ecs for each screen.
        abstract MakeEcs : unit -> World Ecs
        default this.MakeEcs () = Ecs<World> ()

        /// Attempt to make an emitter of the given name.
        abstract TryMakeEmitter : int64 -> int64 -> int64 -> single -> int -> string -> Particles.Emitter option
        default this.TryMakeEmitter time lifeTimeOpt particleLifeTimeOpt particleRate particleMax emitterName =
            match emitterName with
            | "BasicEmitter" -> Particles.BasicEmitter.makeDefault time lifeTimeOpt particleLifeTimeOpt particleRate particleMax :> Particles.Emitter |> Some
            | _ -> None

        /// A call-back at the beginning of each frame.
        abstract PreFrame : World -> World
        default this.PreFrame world = world

        /// A call-back during each frame.
        abstract PerFrame : World -> World
        default this.PerFrame world = world

        /// A call-back at the end of each frame.
        abstract PostFrame : World -> World
        default this.PostFrame world = world

        /// Birth facets / dispatchers of type 'a from plugin.
        member internal this.Birth<'a> () =
            let assembly = (this.GetType ()).Assembly;
            let types = (assembly.GetTypes ()) |> Array.filter (fun ty -> ty.IsSubclassOf typeof<'a>) |> Array.filter (fun ty -> not ty.IsAbstract)
            let instances = types |> Array.map (fun ty -> (ty.Name, Activator.CreateInstance ty :?> 'a)) 
            Array.toList instances

/// Represents an unsubscription operation for an event.
type Unsubscription = WorldTypes.Unsubscription

/// The data for a change in the world's ambient state.
type AmbientChangeData = WorldTypes.AmbientChangeData

/// The data required to execution screen splashing.
type Splash = WorldTypes.Splash

/// Generalized interface tag for dispatchers.
type Dispatcher = WorldTypes.Dispatcher

/// Generalized interface tag for simulant dispatchers.
type SimulantDispatcher = WorldTypes.SimulantDispatcher

/// The default dispatcher for games.
type GameDispatcher = WorldTypes.GameDispatcher

/// The default dispatcher for screens.
type ScreenDispatcher = WorldTypes.ScreenDispatcher

/// The default dispatcher for groups.
type GroupDispatcher = WorldTypes.GroupDispatcher

/// The default dispatcher for entities.
type EntityDispatcher = WorldTypes.EntityDispatcher

/// Dynamically augments an entity's behavior in a composable way.
type Facet = WorldTypes.Facet

/// The game type that hosts the various screens used to navigate through a game.
type Game = WorldTypes.Game

/// The screen type that allows transitioning to and from other screens, and also hosts the
/// currently interactive groups of entities.
type Screen = WorldTypes.Screen

/// Forms a logical group of entities.
type Group = WorldTypes.Group

/// The type around which the whole game engine is based! Used in combination with dispatchers
/// to implement things like buttons, characters, blocks, and things of that sort.
/// OPTIMIZATION: Includes pre-constructed entity event addresses to avoid reconstructing
/// new ones for each entity every frame.
type Entity = WorldTypes.Entity

/// The world, in a functional programming sense. Hosts the game object, the dependencies needed
/// to implement a game, messages to by consumed by the various engine sub-systems, and general
/// configuration data.
///
/// For efficiency, this type is kept under 64 bytes on 32-bit machines as to not exceed the size
/// of a typical cache line.
type World = WorldTypes.World

/// Provides a way to make user-defined dispatchers, facets, and various other sorts of game-
/// specific values.
type NuPlugin = WorldTypes.NuPlugin