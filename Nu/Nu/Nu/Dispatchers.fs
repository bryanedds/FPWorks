﻿namespace Nu
open System
open System.ComponentModel
open OpenTK
open Prime
open TiledSharp
open Nu
open Nu.Constants
open Nu.WorldConstants

[<AutoOpen>]
module RigidBodyFacetModule =

    type EntityRep with

        // TODO: consider implementing Awake, AngularVelocity, and LinearVelocity fields to make
        // physics resets less destructive.

        member this.GetMinorId world : Guid = (this.GetXtension world)?MinorId
        member this.SetMinorId (value : Guid) world = this.UpdateXtension (fun xtension -> xtension?MinorId <- value) world
        member this.GetBodyType world : BodyType = (this.GetXtension world)?BodyType
        member this.SetBodyType (value : BodyType) world = this.UpdateXtension (fun xtension -> xtension?BodyType <- value) world
        member this.GetDensity world : single = (this.GetXtension world)?Density
        member this.SetDensity (value : single) world = this.UpdateXtension (fun xtension -> xtension?Density <- value) world
        member this.GetFriction world : single = (this.GetXtension world)?Friction
        member this.SetFriction (value : single) world = this.UpdateXtension (fun xtension -> xtension?Friction <- value) world
        member this.GetRestitution world : single = (this.GetXtension world)?Restitution
        member this.SetRestitution (value : single) world = this.UpdateXtension (fun xtension -> xtension?Restitution <- value) world
        member this.GetFixedRotation world : bool = (this.GetXtension world)?FixedRotation
        member this.SetFixedRotation (value : bool) world = this.UpdateXtension (fun xtension -> xtension?FixedRotation <- value) world
        member this.GetLinearDamping world : single = (this.GetXtension world)?LinearDamping
        member this.SetLinearDamping (value : single) world = this.UpdateXtension (fun xtension -> xtension?LinearDamping <- value) world
        member this.GetAngularDamping world : single = (this.GetXtension world)?AngularDamping
        member this.SetAngularDamping (value : single) world = this.UpdateXtension (fun xtension -> xtension?AngularDamping <- value) world
        member this.GetGravityScale world : single = (this.GetXtension world)?GravityScale
        member this.SetGravityScale (value : single) world = this.UpdateXtension (fun xtension -> xtension?GravityScale <- value) world
        member this.GetCollisionCategories world : string = (this.GetXtension world)?CollisionCategories
        member this.SetCollisionCategories (value : string) world = this.UpdateXtension (fun xtension -> xtension?CollisionCategories <- value) world
        member this.GetCollisionMask world : string = (this.GetXtension world)?CollisionMask
        member this.SetCollisionMask (value : string) world = this.UpdateXtension (fun xtension -> xtension?CollisionMask <- value) world
        member this.GetCollisionExpr world : string = (this.GetXtension world)?CollisionExpr
        member this.SetCollisionExpr (value : string) world = this.UpdateXtension (fun xtension -> xtension?CollisionExpr <- value) world
        member this.GetIsBullet world : bool = (this.GetXtension world)?IsBullet
        member this.SetIsBullet (value : bool) world = this.UpdateXtension (fun xtension -> xtension?IsBullet <- value) world
        member this.GetIsSensor world : bool = (this.GetXtension world)?IsSensor
        member this.SetIsSensor (value : bool) world = this.UpdateXtension (fun xtension -> xtension?IsSensor <- value) world
        member this.GetPhysicsId world = { SourceId = this.GetId world; BodyId = this.GetMinorId world }

    type RigidBodyFacet () =
        inherit Facet ()

        static let getBodyShape (entityRep : EntityRep) world =
            Physics.evalCollisionExpr (entityRep.GetSize world) (entityRep.GetCollisionExpr world)

        static member FieldDefinitions =
            [variable? MinorId <| fun () -> Core.makeId ()
             define? BodyType Dynamic
             define? Density NormalDensity
             define? Friction 0.0f
             define? Restitution 0.0f
             define? FixedRotation false
             define? LinearDamping 1.0f
             define? AngularDamping 1.0f
             define? GravityScale 1.0f
             define? CollisionCategories "1"
             define? CollisionMask "*"
             define? CollisionExpr "Box"
             define? IsBullet false
             define? IsSensor false]

        override facet.RegisterPhysics (entityRep, world) =
            let bodyProperties = 
                { BodyId = (entityRep.GetPhysicsId world).BodyId
                  Position = entityRep.GetPosition world + entityRep.GetSize world * 0.5f
                  Rotation = entityRep.GetRotation world
                  Shape = getBodyShape entityRep world
                  BodyType = entityRep.GetBodyType world
                  Density = entityRep.GetDensity world
                  Friction = entityRep.GetFriction world
                  Restitution = entityRep.GetRestitution world
                  FixedRotation = entityRep.GetFixedRotation world
                  LinearDamping = entityRep.GetLinearDamping world
                  AngularDamping = entityRep.GetAngularDamping world
                  GravityScale = entityRep.GetGravityScale world
                  CollisionCategories = Physics.toCollisionCategories <| entityRep.GetCollisionCategories world
                  CollisionMask = Physics.toCollisionCategories <| entityRep.GetCollisionMask world
                  IsBullet = entityRep.GetIsBullet world
                  IsSensor = entityRep.GetIsSensor world }
            World.createBody entityRep.EntityAddress (entityRep.GetId world) bodyProperties world

        override facet.UnregisterPhysics (entityRep, world) =
            World.destroyBody (entityRep.GetPhysicsId world) world

        override facet.PropagatePhysics (entityRep, world) =
            let world = facet.UnregisterPhysics (entityRep, world)
            facet.RegisterPhysics (entityRep, world)

[<AutoOpen>]
module SpriteFacetModule =

    type EntityRep with

        member this.GetSpriteImage world : AssetTag = (this.GetXtension world)?SpriteImage
        member this.SetSpriteImage (value : AssetTag) world = this.UpdateXtension (fun xtension -> xtension?SpriteImage <- value) world

    type SpriteFacet () =
        inherit Facet ()

        static member FieldDefinitions =
            [define? SpriteImage { PackageName = DefaultPackageName; AssetName = "Image3" }]

        override facet.GetRenderDescriptors (entityRep : EntityRep, world) =
            if  entityRep.GetVisible world &&
                Camera.inView3 (entityRep.GetViewType world) (entityRep.GetPosition world) (entityRep.GetSize world) world.State.Camera then
                [LayerableDescriptor
                    { Depth = entityRep.GetDepth world
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = entityRep.GetPosition world
                              Size = entityRep.GetSize world
                              Rotation = entityRep.GetRotation world
                              ViewType = entityRep.GetViewType world
                              OptInset = None
                              Image = entityRep.GetSpriteImage world
                              Color = Vector4.One }}]
            else []

        override facet.GetQuickSize (entityRep, world) =
            match Metadata.tryGetTextureSizeAsVector2 (entityRep.GetSpriteImage world) world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module AnimatedSpriteFacetModule =

    type EntityRep with
    
        member this.GetTileCount world : int = (this.GetXtension world)?TileCount
        member this.SetTileCount (value : int) world = this.UpdateXtension (fun xtension -> xtension?TileCount <- value) world
        member this.GetTileRun world : int = (this.GetXtension world)?TileRun
        member this.SetTileRun (value : int) world = this.UpdateXtension (fun xtension -> xtension?TileRun <- value) world
        member this.GetTileSize world : Vector2 = (this.GetXtension world)?TileSize
        member this.SetTileSize (value : Vector2) world = this.UpdateXtension (fun xtension -> xtension?TileSize <- value) world
        member this.GetAnimationStutter world : int64 = (this.GetXtension world)?AnimationStutter
        member this.SetAnimationStutter (value : int64) world = this.UpdateXtension (fun xtension -> xtension?AnimationStutter <- value) world
        member this.GetAnimationSheet world : AssetTag = (this.GetXtension world)?AnimationSheet
        member this.SetAnimationSheet (value : AssetTag) world = this.UpdateXtension (fun xtension -> xtension?AnimationSheet <- value) world

    type AnimatedSpriteFacet () =
        inherit Facet ()

        static let getOptSpriteInset (entityRep : EntityRep) world =
            let tile = int (world.State.TickTime / entityRep.GetAnimationStutter world) % entityRep.GetTileCount world
            let tileRun = entityRep.GetTileRun world
            let tileSize = entityRep.GetTileSize world
            let tileI = tile % tileRun
            let tileJ = tile / tileRun
            let tileX = single tileI * tileSize.X
            let tileY = single tileJ * tileSize.Y
            let inset = Vector4 (tileX, tileY, tileX + tileSize.X, tileY + tileSize.Y)
            Some inset

        static member FieldDefinitions =
            [define? TileCount 16 
             define? TileRun 4
             define? TileSize <| Vector2 (16.0f, 16.0f)
             define? AnimationStutter 4L
             define? AnimationSheet { PackageName = DefaultPackageName; AssetName = "Image7" }]

        override facet.GetRenderDescriptors (entityRep, world) =
            if  entityRep.GetVisible world &&
                Camera.inView3 (entityRep.GetViewType world) (entityRep.GetPosition world) (entityRep.GetSize world) world.State.Camera then
                [LayerableDescriptor
                    { Depth = entityRep.GetDepth world
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = entityRep.GetPosition world
                              Size = entityRep.GetSize world
                              Rotation = entityRep.GetRotation world
                              ViewType = entityRep.GetViewType world
                              OptInset = getOptSpriteInset entityRep world
                              Image = entityRep.GetAnimationSheet world
                              Color = Vector4.One }}]
            else []

        override facet.GetQuickSize (entityRep, world) =
            entityRep.GetTileSize world

[<AutoOpen>]
module EntityDispatcherModule =

    type Entity with
    
        // OPTIMIZATION: priority annotated as single to decrease GC pressure.
        static member private sortFstDesc (priority : single, _) (priority2 : single, _) =
            if priority > priority2 then -1
            elif priority < priority2 then 1
            else 0

        static member setPositionSnapped snap position (entity : Entity) =
            let snapped = Math.snap2F snap position
            Entity.setPosition snapped entity

        static member getTransform (entity : Entity) =
            { Transform.Position = entity.Position
              Depth = entity.Depth
              Size = entity.Size
              Rotation = entity.Rotation }

        static member setTransform positionSnap rotationSnap transform (entity : Entity) =
            let transform = Math.snapTransform positionSnap rotationSnap transform
            entity |>
                Entity.setPosition transform.Position |>
                Entity.setDepth transform.Depth |>
                Entity.setSize transform.Size |>
                Entity.setRotation transform.Rotation

        static member pickingSort entities world =
            let prioritiesAndEntities = List.map (fun (entity : Entity) -> (Entity.getPickingPriority entity world, entity)) entities
            let prioritiesAndEntities = List.sortWith Entity.sortFstDesc prioritiesAndEntities
            List.map snd prioritiesAndEntities

        static member tryPick position entities world =
            let entitiesSorted = Entity.pickingSort entities world
            List.tryFind
                (fun entity ->
                    let positionWorld = Camera.mouseToWorld entity.ViewType position world.State.Camera
                    let transform = Entity.getTransform entity
                    let picked = Math.isPointInBounds3 positionWorld transform.Position transform.Size
                    picked)
                entitiesSorted

[<AutoOpen>]
module GuiDispatcherModule =

    type Entity with
        
        member gui.Enabled = gui?Enabled : bool
        static member getEnabled (gui : Entity) = gui.Enabled
        static member setEnabled (value : bool) (gui : Entity) = gui?Enabled <- value

        member gui.DisabledColor = gui?DisabledColor : Vector4
        static member getDisabledColor (gui : Entity) = gui.DisabledColor
        static member setDisabledColor (value : Vector4) (gui : Entity) = gui?DisabledColor <- value

        member gui.SwallowMouseLeft = gui?SwallowMouseLeft : bool
        static member getSwallowMouseLeft (gui : Entity) = gui.SwallowMouseLeft
        static member setSwallowMouseLeft (value : bool) (gui : Entity) = gui?SwallowMouseLeft <- value

    type GuiDispatcher () =
        inherit EntityDispatcher ()

        static let handleMouseLeft event world =
            let gui = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            let eventHandling =
                if World.isAddressSelected address world && gui.Visible then
                    let mousePositionWorld = Camera.mouseToWorld gui.ViewType data.Position world.State.Camera
                    if gui.SwallowMouseLeft && Math.isPointInBounds3 mousePositionWorld gui.Position gui.Size then Resolve else Cascade
                else Cascade
            (eventHandling, world)
        
        static member FieldDefinitions =
            [define? ViewType Absolute
             define? Enabled true
             define? DisabledColor <| Vector4 0.75f
             define? SwallowMouseLeft true]

        override dispatcher.Register (gui, address, world) =
            let world =
                world |>
                    World.monitor handleMouseLeft MouseLeftDownEventAddress address |>
                    World.monitor handleMouseLeft MouseLeftUpEventAddress address
            (gui, world)

[<AutoOpen>]
module ButtonDispatcherModule =

    type Entity with
    
        member button.Down = button?Down : bool
        static member getDown (entity : Entity) = entity.Down
        static member setDown (value : bool) (button : Entity) = button?Down <- value
        
        member button.UpImage = button?UpImage : AssetTag
        static member getUpImage (entity : Entity) = entity.UpImage
        static member setUpImage (value : AssetTag) (button : Entity) = button?UpImage <- value
        
        member button.DownImage = button?DownImage : AssetTag
        static member getDownImage (entity : Entity) = entity.DownImage
        static member setDownImage (value : AssetTag) (button : Entity) = button?DownImage <- value
        
        member button.OptClickSound = button?OptClickSound : AssetTag option
        static member getOptClickSound (entity : Entity) = entity.OptClickSound
        static member setOptClickSound (value : AssetTag option) (button : Entity) = button?OptClickSound <- value

    type ButtonDispatcher () =
        inherit GuiDispatcher ()

        let handleMouseLeftDown event world =
            let button = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world then
                let mousePositionWorld = Camera.mouseToWorld button.ViewType data.Position world.State.Camera
                if Math.isPointInBounds3 mousePositionWorld button.Position button.Size && button.Visible then
                    if button.Enabled then
                        let button = Entity.setDown true button
                        let world = World.setEntity button address world
                        let world = World.publish4 () (DownEventAddress ->>- address) address world
                        (Resolve, world)
                    else (Resolve, world)
                else (Cascade, world)
            else (Cascade, world)

        let handleMouseLeftUp event world =
            let button = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world then
                let wasDown = button.Down
                let button = Entity.setDown false button
                let world = World.setEntity button address world
                let mousePositionWorld = Camera.mouseToWorld button.ViewType data.Position world.State.Camera
                if Math.isPointInBounds3 mousePositionWorld button.Position button.Size && button.Visible then
                    if button.Enabled && wasDown then
                        let world = World.publish4 () (UpEventAddress ->>- address) address world
                        let world = World.publish4 () (ClickEventAddress ->>- address) address world
                        let world =
                            match button.OptClickSound with
                            | Some clickSound -> World.playSound 1.0f clickSound world
                            | None -> world
                        (Resolve, world)
                    else (Resolve, world)
                else (Cascade, world)
            else (Cascade, world)

        static member FieldDefinitions =
            [define? SwallowMouseLeft false
             define? Down false
             define? UpImage { PackageName = DefaultPackageName; AssetName = "Image" }
             define? DownImage { PackageName = DefaultPackageName; AssetName = "Image2" }
             define? OptClickSound <| Some { PackageName = DefaultPackageName; AssetName = "Sound" }]

        override dispatcher.Register (button, address, world) =
            let world =
                world |>
                    World.monitor handleMouseLeftDown MouseLeftDownEventAddress address |>
                    World.monitor handleMouseLeftUp MouseLeftUpEventAddress address
            (button, world)

        override dispatcher.GetRenderDescriptors (button, _) =
            if button.Visible then
                [LayerableDescriptor
                    { Depth = button.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = button.Position
                              Size = button.Size
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = if button.Down then button.DownImage else button.UpImage
                              Color = if button.Enabled then Vector4.One else button.DisabledColor }}]
            else []

        override dispatcher.GetQuickSize (button, world) =
            match Metadata.tryGetTextureSizeAsVector2 button.UpImage world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module LabelDispatcherModule =

    type Entity with

        member label.LabelImage = label?LabelImage : AssetTag
        static member getLabelImage (entity : Entity) = entity.LabelImage
        static member setLabelImage (value : AssetTag) (label : Entity) = label?LabelImage <- value

    type LabelDispatcher () =
        inherit GuiDispatcher ()

        static member FieldDefinitions =
            [define? SwallowMouseLeft true
             define? LabelImage { PackageName = DefaultPackageName; AssetName = "Image4" }]

        override dispatcher.GetRenderDescriptors (label, _) =
            if label.Visible then
                [LayerableDescriptor
                    { Depth = label.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = label.Position
                              Size = label.Size
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = label.LabelImage
                              Color = if label.Enabled then Vector4.One else label.DisabledColor }}]
            else []

        override dispatcher.GetQuickSize (label, world) =
            match Metadata.tryGetTextureSizeAsVector2 label.LabelImage world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module TextDispatcherModule =

    type Entity with

        member text.Text : string = text?Text
        static member getText (entity : Entity) = entity.Text
        static member setText (value : string) (text : Entity) = text?Text <- value
        
        member text.TextFont = text?TextFont : AssetTag
        static member getTextFont (entity : Entity) = entity.TextFont
        static member setTextFont (value : AssetTag) (text : Entity) = text?TextFont <- value
        
        member text.TextOffset = text?TextOffset : Vector2
        static member getTextOffset (entity : Entity) = entity.TextOffset
        static member setTextOffset (value : Vector2) (text : Entity) = text?TextOffset <- value
        
        member text.TextColor = text?TextColor : Vector4
        static member getTextColor (entity : Entity) = entity.TextColor
        static member setTextColor (value : Vector4) (text : Entity) = text?TextColor <- value
        
        member text.BackgroundImage = text?BackgroundImage : AssetTag
        static member getBackgroundImage (entity : Entity) = entity.BackgroundImage
        static member setBackgroundImage (value : AssetTag) (text : Entity) = text?BackgroundImage <- value

    type TextDispatcher () =
        inherit GuiDispatcher ()

        static member FieldDefinitions =
            [define? SwallowMouseLeft true
             define? Text String.Empty
             define? TextFont { PackageName = DefaultPackageName; AssetName = "Font" }
             define? TextOffset Vector2.Zero
             define? TextColor Vector4.One
             define? BackgroundImage { PackageName = DefaultPackageName; AssetName = "Image4" }]

        override dispatcher.GetRenderDescriptors (text, _) =
            if text.Visible then
                [LayerableDescriptor
                    { Depth = text.Depth
                      LayeredDescriptor =
                        TextDescriptor
                            { Text = text.Text
                              Position = (text.Position + text.TextOffset)
                              Size = text.Size - text.TextOffset
                              ViewType = Absolute
                              Font = text.TextFont
                              Color = text.TextColor }}
                 LayerableDescriptor
                    { Depth = text.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = text.Position
                              Size = text.Size
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = text.BackgroundImage
                              Color = if text.Enabled then Vector4.One else text.DisabledColor }}]
            else []

        override dispatcher.GetQuickSize (text, world) =
            match Metadata.tryGetTextureSizeAsVector2 text.BackgroundImage world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module ToggleDispatcherModule =

    type Entity with

        member toggle.On = toggle?On : bool
        static member getOn (entity : Entity) = entity.On
        static member setOn (value : bool) (toggle : Entity) = toggle?On <- value
        
        member toggle.Pressed = toggle?Pressed : bool
        static member getPressed (entity : Entity) = entity.Pressed
        static member setPressed (value : bool) (toggle : Entity) = toggle?Pressed <- value
        
        member toggle.OffImage = toggle?OffImage : AssetTag
        static member getOffImage (entity : Entity) = entity.OffImage
        static member setOffImage (value : AssetTag) (toggle : Entity) = toggle?OffImage <- value
        
        member toggle.OnImage = toggle?OnImage : AssetTag
        static member getOnImage (entity : Entity) = entity.OnImage
        static member setOnImage (value : AssetTag) (toggle : Entity) = toggle?OnImage <- value
        
        member toggle.OptToggleSound = toggle?OptToggleSound : AssetTag option
        static member getOptToggleSound (entity : Entity) = entity.OptToggleSound
        static member setOptToggleSound (value : AssetTag option) (toggle : Entity) = toggle?OptToggleSound <- value

    type ToggleDispatcher () =
        inherit GuiDispatcher ()
        
        let handleMouseLeftDown event world =
            let toggle = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world then
                let mousePositionWorld = Camera.mouseToWorld toggle.ViewType data.Position world.State.Camera
                if Math.isPointInBounds3 mousePositionWorld toggle.Position toggle.Size && toggle.Visible then
                    if toggle.Enabled then
                        let toggle = Entity.setPressed true toggle
                        let world = World.setEntity toggle address world
                        (Resolve, world)
                    else (Resolve, world)
                else (Cascade, world)
            else (Cascade, world)

        let handleMouseLeftUp event world =
            let toggle = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world then
                let wasPressed = toggle.Pressed
                let toggle = Entity.setPressed false toggle
                let world = World.setEntity toggle address world
                let mousePositionWorld = Camera.mouseToWorld toggle.ViewType data.Position world.State.Camera
                if Math.isPointInBounds3 mousePositionWorld toggle.Position toggle.Size && toggle.Visible then
                    if toggle.Enabled && wasPressed then
                        let toggle = Entity.setOn (not toggle.On) toggle
                        let world = World.setEntity toggle address world
                        let eventAddress = if toggle.On then OnEventAddress else OffEventAddress
                        let world = World.publish4 () (eventAddress ->>- address) address world
                        let world =
                            match toggle.OptToggleSound with
                            | Some toggleSound -> World.playSound 1.0f toggleSound world
                            | None -> world
                        (Resolve, world)
                    else (Resolve, world)
                else (Cascade, world)
            else (Cascade, world)

        static member FieldDefinitions =
            [define? SwallowMouseLeft false
             define? On false
             define? Pressed false
             define? OffImage { PackageName = DefaultPackageName; AssetName = "Image" }
             define? OnImage { PackageName = DefaultPackageName; AssetName = "Image2" }
             define? OptToggleSound <| Some { PackageName = DefaultPackageName; AssetName = "Sound" }]

        override dispatcher.Register (toggle, address, world) =
            let world =
                world |>
                    World.monitor handleMouseLeftDown MouseLeftDownEventAddress address |>
                    World.monitor handleMouseLeftUp MouseLeftUpEventAddress address
            (toggle, world)

        override dispatcher.GetRenderDescriptors (toggle, _) =
            if toggle.Visible then
                [LayerableDescriptor
                    { Depth = toggle.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = toggle.Position
                              Size = toggle.Size
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = if toggle.On || toggle.Pressed then toggle.OnImage else toggle.OffImage
                              Color = if toggle.Enabled then Vector4.One else toggle.DisabledColor }}]
            else []

        override dispatcher.GetQuickSize (toggle, world) =
            match Metadata.tryGetTextureSizeAsVector2 toggle.OffImage world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module FeelerDispatcherModule =

    type Entity with

        member feeler.Touched = feeler?Touched : bool
        static member getTouched (entity : Entity) = entity.Touched
        static member setTouched (value : bool) (feeler : Entity) = feeler?Touched <- value

    type FeelerDispatcher () =
        inherit GuiDispatcher ()

        let handleMouseLeftDown event world =
            let feeler = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world then
                let mousePositionWorld = Camera.mouseToWorld feeler.ViewType data.Position world.State.Camera
                if Math.isPointInBounds3 mousePositionWorld feeler.Position feeler.Size && feeler.Visible then
                    if feeler.Enabled then
                        let feeler = Entity.setTouched true feeler
                        let world = World.setEntity feeler address world
                        let world = World.publish4 data.Position (TouchEventAddress ->>- address) address world
                        (Resolve, world)
                    else (Resolve, world)
                else (Cascade, world)
            else (Cascade, world)
    
        let handleMouseLeftUp event world =
            let feeler = World.getEntity event.SubscriberAddress world
            let address = event.SubscriberAddress
            let data = event.Data : MouseButtonData
            if World.isAddressSelected address world && feeler.Visible then
                if feeler.Enabled then
                    let feeler = Entity.setTouched false feeler
                    let world = World.setEntity feeler address world
                    let world = World.publish4 data.Position (UntouchEventAddress ->>- address) address world
                    (Resolve, world)
                else (Resolve, world)
            else (Cascade, world)

        static member FieldDefinitions =
            [define? SwallowMouseLeft false
             define? Touched false]

        override dispatcher.Register (feeler, address, world) =
            let world =
                world |>
                    World.monitor handleMouseLeftDown MouseLeftDownEventAddress address |>
                    World.monitor handleMouseLeftUp MouseLeftUpEventAddress address
            (feeler, world)

        override dispatcher.GetQuickSize (_, _) =
            Vector2 64.0f

[<AutoOpen>]
module FillBarDispatcherModule =

    type Entity with
    
        member fillBar.Fill = fillBar?Fill : single
        static member getFill (entity : Entity) = entity.Fill
        static member setFill (value : single) (fillBar : Entity) = fillBar?Fill <- value
        
        member fillBar.FillInset = fillBar?FillInset : single
        static member getFillInset (entity : Entity) = entity.FillInset
        static member setFillInset (value : single) (fillBar : Entity) = fillBar?FillInset <- value
        
        member fillBar.FillImage = fillBar?FillImage : AssetTag
        static member getFillImage (entity : Entity) = entity.FillImage
        static member setFillImage (value : AssetTag) (fillBar : Entity) = fillBar?FillImage <- value
        
        member fillBar.BorderImage = fillBar?BorderImage : AssetTag
        static member getBorderImage (entity : Entity) = entity.BorderImage
        static member setBorderImage (value : AssetTag) (fillBar : Entity) = fillBar?BorderImage <- value

    type FillBarDispatcher () =
        inherit GuiDispatcher ()
        
        let getFillBarSpriteDims (fillBar : Entity) =
            let spriteInset = fillBar.Size * fillBar.FillInset * 0.5f
            let spritePosition = fillBar.Position + spriteInset
            let spriteWidth = (fillBar.Size.X - spriteInset.X * 2.0f) * fillBar.Fill
            let spriteHeight = fillBar.Size.Y - spriteInset.Y * 2.0f
            (spritePosition, Vector2 (spriteWidth, spriteHeight))

        static member FieldDefinitions =
            [define? SwallowMouseLeft true
             define? Fill 0.0f
             define? FillInset 0.0f
             define? FillImage { PackageName = DefaultPackageName; AssetName = "Image9" }
             define? BorderImage { PackageName = DefaultPackageName; AssetName = "Image10" }]

        override dispatcher.GetRenderDescriptors (fillBar, _) =
            if fillBar.Visible then
                let (fillBarSpritePosition, fillBarSpriteSize) = getFillBarSpriteDims fillBar
                [LayerableDescriptor
                    { Depth = fillBar.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = fillBar.Position
                              Size = fillBar.Size
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = fillBar.BorderImage
                              Color = if fillBar.Enabled then Vector4.One else fillBar.DisabledColor }}
                 LayerableDescriptor
                    { Depth = fillBar.Depth
                      LayeredDescriptor =
                        SpriteDescriptor
                            { Position = fillBarSpritePosition
                              Size = fillBarSpriteSize
                              Rotation = 0.0f
                              ViewType = Absolute
                              OptInset = None
                              Image = fillBar.FillImage
                              Color = if fillBar.Enabled then Vector4.One else fillBar.DisabledColor }}]
            else []

        override dispatcher.GetQuickSize (fillBar, world) =
            match Metadata.tryGetTextureSizeAsVector2 fillBar.BorderImage world.State.AssetMetadataMap with
            | Some size -> size
            | None -> DefaultEntitySize

[<AutoOpen>]
module BlockDispatcherModule =

    type BlockDispatcher () =
        inherit EntityDispatcher ()

        static member FieldDefinitions =
            [define? BodyType Static
             define? SpriteImage { PackageName = DefaultPackageName; AssetName = "Image3" }]

        static member IntrinsicFacetNames =
            [typeof<RigidBodyFacet>.Name
             typeof<SpriteFacet>.Name]

[<AutoOpen>]
module BoxDispatcherModule =

    type BoxDispatcher () =
        inherit EntityDispatcher ()

        static member FieldDefinitions =
            [define? SpriteImage { PackageName = DefaultPackageName; AssetName = "Image3" }]

        static member IntrinsicFacetNames =
            [typeof<RigidBodyFacet>.Name
             typeof<SpriteFacet>.Name]

[<AutoOpen>]
module TopViewCharacterDispatcherModule =

    type TopViewCharacterDispatcher () =
        inherit EntityDispatcher ()

        static member FieldDefinitions =
            [define? FixedRotation true
             define? LinearDamping 10.0f
             define? GravityScale 0.0f
             define? CollisionExpr "Circle"
             define? SpriteImage { PackageName = DefaultPackageName; AssetName = "Image7" }]
        
        static member IntrinsicFacetNames =
            [typeof<RigidBodyFacet>.Name
             typeof<SpriteFacet>.Name]

[<AutoOpen>]
module SideViewCharacterDispatcherModule =

    type SideViewCharacterDispatcher () =
        inherit EntityDispatcher ()

        static member FieldDefinitions =
            [define? FixedRotation true
             define? LinearDamping 3.0f
             define? CollisionExpr "Capsule"
             define? SpriteImage { PackageName = DefaultPackageName; AssetName = "Image6" }]

        static member IntrinsicFacetNames =
            [typeof<RigidBodyFacet>.Name
             typeof<SpriteFacet>.Name]

[<AutoOpen>]
module TileMapDispatcherModule =

    type Entity with

        member entity.TileMapAsset = entity?TileMapAsset : AssetTag
        static member getTileMapAsset (entity : Entity) = entity.TileMapAsset
        static member setTileMapAsset (value : AssetTag) (entity : Entity) = entity?TileMapAsset <- value
        
        member entity.Parallax = entity?Parallax : single
        static member getParallax (entity : Entity) = entity.Parallax
        static member setParallax (value : single) (entity : Entity) = entity?Parallax <- value

        static member makeTileMapData (tileMapAsset : AssetTag) world =
            let map = __c <| Metadata.getTileMapMetadata tileMapAsset world.State.AssetMetadataMap
            let mapSize = Vector2i (map.Width, map.Height)
            let tileSize = Vector2i (map.TileWidth, map.TileHeight)
            let tileSizeF = Vector2 (single tileSize.X, single tileSize.Y)
            let tileMapSize = Vector2i (mapSize.X * tileSize.X, mapSize.Y * tileSize.Y)
            let tileMapSizeF = Vector2 (single tileMapSize.X, single tileMapSize.Y)
            let tileSet = map.Tilesets.[0] // MAGIC_VALUE: I'm not sure how to properly specify this
            let optTileSetWidth = tileSet.Image.Width
            let optTileSetHeight = tileSet.Image.Height
            let tileSetSize = Vector2i (optTileSetWidth.Value / tileSize.X, optTileSetHeight.Value / tileSize.Y)
            { Map = map; MapSize = mapSize; TileSize = tileSize; TileSizeF = tileSizeF; TileMapSize = tileMapSize; TileMapSizeF = tileMapSizeF; TileSet = tileSet; TileSetSize = tileSetSize }

        static member makeTileData (tileMap : Entity) tmd (tl : TmxLayer) tileIndex =
            let mapRun = tmd.MapSize.X
            let tileSetRun = tmd.TileSetSize.X
            let (i, j) = (tileIndex % mapRun, tileIndex / mapRun)
            let tile = tl.Tiles.[tileIndex]
            let gid = tile.Gid - tmd.TileSet.FirstGid
            let gidPosition = gid * tmd.TileSize.X
            let gid2 = Vector2i (gid % tileSetRun, gid / tileSetRun)
            let tileMapPosition = tileMap.Position
            let tilePosition =
                Vector2i (
                    int tileMapPosition.X + tmd.TileSize.X * i,
                    int tileMapPosition.Y - tmd.TileSize.Y * (j + 1)) // subtraction for right-handedness
            let optTileSetTile = Seq.tryFind (fun (item : TmxTilesetTile) -> tile.Gid - 1 = item.Id) tmd.TileSet.Tiles
            { Tile = tile; I = i; J = j; Gid = gid; GidPosition = gidPosition; Gid2 = gid2; TilePosition = tilePosition; OptTileSetTile = optTileSetTile }

    type TileMapDispatcher () =
        inherit EntityDispatcher ()

        let getTileBodyProperties6 (tm : Entity) tmd tli td ti cexpr =
            let tileShape = Physics.evalCollisionExpr (Vector2 (single tmd.TileSize.X, single tmd.TileSize.Y)) cexpr
            { BodyId = intsToGuid tli ti
              Position =
                Vector2 (
                    single <| td.TilePosition.X + tmd.TileSize.X / 2,
                    single <| td.TilePosition.Y + tmd.TileSize.Y / 2 + tmd.TileMapSize.Y)
              Rotation = tm.Rotation
              Shape = tileShape
              BodyType = BodyType.Static
              Density = NormalDensity
              Friction = tm.Friction
              Restitution = tm.Restitution
              FixedRotation = true
              LinearDamping = 0.0f
              AngularDamping = 0.0f
              GravityScale = 0.0f
              CollisionCategories = Physics.toCollisionCategories tm.CollisionCategories
              CollisionMask = Physics.toCollisionCategories tm.CollisionMask
              IsBullet = false
              IsSensor = false }

        let getTileBodyProperties tm tmd (tl : TmxLayer) tli ti =
            let td = Entity.makeTileData tm tmd tl ti
            match td.OptTileSetTile with
            | Some tileSetTile ->
                match tileSetTile.Properties.TryGetValue CollisionProperty with
                | (true, collisionProperty) ->
                    let collisionExpr = acstring collisionProperty
                    let tileBodyProperties = getTileBodyProperties6 tm tmd tli td ti collisionExpr
                    Some tileBodyProperties
                | (false, _) -> None
            | None -> None

        let getTileLayerBodyPropertyList tileMap tileMapData tileLayerIndex (tileLayer : TmxLayer) =
            if tileLayer.Properties.ContainsKey CollisionProperty then
                Seq.foldi
                    (fun i bodyPropertyList _ ->
                        match getTileBodyProperties tileMap tileMapData tileLayer tileLayerIndex i with
                        | Some bodyProperties -> bodyProperties :: bodyPropertyList
                        | None -> bodyPropertyList)
                    []
                    tileLayer.Tiles
            else []
        
        let registerTileLayerPhysics tileMap tileMapData address tileLayerIndex world tileLayer =
            let bodyPropertyList = getTileLayerBodyPropertyList tileMap tileMapData tileLayerIndex tileLayer
            World.createBodies address tileMap.Id bodyPropertyList world

        let registerTileMapPhysics (tileMap : Entity) address world =
            let tileMapData = Entity.makeTileMapData tileMap.TileMapAsset world
            Seq.foldi
                (registerTileLayerPhysics tileMap tileMapData address)
                world
                tileMapData.Map.Layers

        let getTileLayerPhysicsIds (tileMap : Entity) tileMapData tileLayer tileLayerIndex =
            Seq.foldi
                (fun tileIndex physicsIds _ ->
                    let tileData = Entity.makeTileData tileMap tileMapData tileLayer tileIndex
                    match tileData.OptTileSetTile with
                    | Some tileSetTile ->
                        if tileSetTile.Properties.ContainsKey CollisionProperty then
                            let physicsId = { SourceId = tileMap.Id; BodyId = intsToGuid tileLayerIndex tileIndex }
                            physicsId :: physicsIds
                        else physicsIds
                    | None -> physicsIds)
                []
                tileLayer.Tiles

        let unregisterTileMapPhysics (tileMap : Entity) world =
            let tileMapData = Entity.makeTileMapData tileMap.TileMapAsset world
            Seq.foldi
                (fun tileLayerIndex world (tileLayer : TmxLayer) ->
                    if tileLayer.Properties.ContainsKey CollisionProperty then
                        let physicsIds = getTileLayerPhysicsIds tileMap tileMapData tileLayer tileLayerIndex
                        World.destroyBodies physicsIds world
                    else world)
                world
                tileMapData.Map.Layers

        static member FieldDefinitions =
            [define? Friction 0.0f
             define? Restitution 0.0f
             define? CollisionCategories "1"
             define? CollisionMask "*"
             define? TileMapAsset { PackageName = DefaultPackageName; AssetName = "TileMap" }
             define? Parallax 0.0f]

        override dispatcher.Register (tileMap, address, world) =
            let world = registerTileMapPhysics tileMap address world
            (tileMap, world)

        override dispatcher.Unregister (tileMap, _, world) =
            let world = unregisterTileMapPhysics tileMap world
            (tileMap, world)
            
        override dispatcher.PropagatePhysics (tileMap, address, world) =
            world |>
                unregisterTileMapPhysics tileMap |>
                registerTileMapPhysics tileMap address

        override dispatcher.GetRenderDescriptors (tileMap, world) =
            if tileMap.Visible then
                match Metadata.tryGetTileMapMetadata tileMap.TileMapAsset world.State.AssetMetadataMap with
                | Some (_, images, map) ->
                    let layers = List.ofSeq map.Layers
                    let tileSourceSize = Vector2i (map.TileWidth, map.TileHeight)
                    let tileSize = Vector2 (single map.TileWidth, single map.TileHeight)
                    List.foldi
                        (fun i descriptors (layer : TmxLayer) ->
                            let camera = world.State.Camera
                            let depth = tileMap.Depth + single i * 2.0f // MAGIC_VALUE: assumption
                            let parallaxTranslation =
                                match tileMap.ViewType with
                                | Absolute -> Vector2.Zero
                                | Relative -> tileMap.Parallax * depth * -camera.EyeCenter
                            let parallaxPosition = tileMap.Position + parallaxTranslation
                            let size = Vector2 (tileSize.X * single map.Width, tileSize.Y * single map.Height)
                            if Camera.inView3 tileMap.ViewType parallaxPosition size camera then
                                let descriptor =
                                    LayerableDescriptor 
                                        { Depth = depth
                                          LayeredDescriptor =
                                            TileLayerDescriptor
                                                { Position = parallaxPosition
                                                  Size = size
                                                  Rotation = tileMap.Rotation
                                                  ViewType = tileMap.ViewType
                                                  MapSize = Vector2i (map.Width, map.Height)
                                                  Tiles = layer.Tiles
                                                  TileSourceSize = tileSourceSize
                                                  TileSize = tileSize
                                                  TileSet = map.Tilesets.[0] // MAGIC_VALUE: I have no idea how to tell which tile set each tile is from...
                                                  TileSetImage = List.head images }} // MAGIC_VALUE: for same reason as above
                                descriptor :: descriptors
                            else descriptors)
                        []
                        layers
                | None -> []
            else []

        override dispatcher.GetQuickSize (tileMap, world) =
            match Metadata.tryGetTileMapMetadata tileMap.TileMapAsset world.State.AssetMetadataMap with
            | Some (_, _, map) -> Vector2 (single <| map.Width * map.TileWidth, single <| map.Height * map.TileHeight)
            | None -> DefaultEntitySize