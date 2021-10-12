﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace OmniBlade
open System
open System.Numerics
open Prime
open Nu
open Nu.Declarative
open OmniBlade

[<AutoOpen>]
module AvatarDispatcher =

    type [<StructuralEquality; NoComparison>] AvatarMessage =
        | Update
        | PostUpdate
        | Collision of CollisionData
        | Separation of SeparationData
        | BodyRemoving of PhysicsId
        | TryFace of Direction
        | Nil

    type [<StructuralEquality; NoComparison>] AvatarCommand =
        | TryTravel of Vector2

    type Entity with
        member this.GetAvatar world = this.GetModelGeneric<Avatar> world
        member this.SetAvatar value world = this.SetModelGeneric<Avatar> value world
        member this.Avatar = this.ModelGeneric<Avatar> ()

    type AvatarDispatcher () =
        inherit EntityDispatcher<Avatar, AvatarMessage, AvatarCommand>
            (Avatar.make (v4Bounds v2Zero Constants.Gameplay.CharacterSize) Assets.Field.JinnAnimationSheet Downward)

        static let coreShapeId = Gen.id
        static let sensorShapeId = Gen.id

        static let getSpriteInset (entity : Entity) world =
            let avatar = entity.GetAvatar world
            let inset = Avatar.getAnimationInset (World.getUpdateTime world) avatar
            inset

        static let isIntersectedBodyShape collider collidee world =
            if (collider.BodyShapeId = coreShapeId &&
                collidee.Entity.Exists world &&
                collidee.Entity.Is<PropDispatcher> world &&
                match (collidee.Entity.GetProp world).PropData with
                | Portal _ -> true
                | Sensor _ -> true
                | _ -> false) then
                true
            elif (collider.BodyShapeId = sensorShapeId &&
                  collidee.Entity.Exists world &&
                  collidee.Entity.Is<PropDispatcher> world &&
                  match (collidee.Entity.GetProp world).PropData with
                  | Portal _ -> false
                  | Sensor _ -> false
                  | _ -> true) then
                true
            else false

        static member Facets =
            [typeof<RigidBodyFacet>]

        override this.Initializers (avatar, entity) =
            let bodyCenter = v2 -0.015f -0.36f
            let bodyShapes =
                BodyShapes
                    [BodyCircle { Radius = 0.16f; Center = v2 -0.01f -0.36f; PropertiesOpt = Some { BodyShapeProperties.empty with BodyShapeId = coreShapeId }}
                     BodyCircle { Radius = 0.325f; Center = bodyCenter; PropertiesOpt = Some { BodyShapeProperties.empty with BodyShapeId = sensorShapeId; IsSensorOpt = Some true }}]
            [entity.Bounds <== avatar --> fun avatar -> avatar.Bounds
             Entity.Omnipresent == true
             entity.FixedRotation == true
             entity.GravityScale == 0.0f
             entity.BodyShape == bodyShapes]

        override this.Channel (_, entity) =
            [entity.UpdateEvent => msg Update
             entity.Parent.PostUpdateEvent => msg PostUpdate
             entity.CollisionEvent =|> fun evt -> msg (Collision evt.Data)
             entity.SeparationEvent =|> fun evt -> msg (Separation evt.Data)
             Simulants.Game.BodyRemovingEvent =|> fun evt -> msg (BodyRemoving evt.Data)]

        override this.Message (avatar, message, entity, world) =
            let time = World.getUpdateTime world
            match message with
            | Update ->

                // update animation generally
                let velocity = entity.GetLinearVelocity world
                let speed = velocity.Length ()
                let direction = Direction.ofVector2Biased velocity
                let avatar =
                    if speed > Constants.Field.AvatarIdleSpeedMax then
                        if direction <> avatar.Direction || avatar.CharacterAnimationType = IdleAnimation then
                            let avatar = Avatar.updateDirection (constant direction) avatar
                            Avatar.animate time WalkAnimation avatar
                        else avatar
                    else Avatar.animate time IdleAnimation avatar
                just avatar

            | PostUpdate ->

                // clear all temporary body shapes
                let avatar = Avatar.updateCollidedBodyShapes (constant []) avatar
                let avatar = Avatar.updateSeparatedBodyShapes (constant []) avatar
                just avatar

            | TryFace direction ->

                // update facing if enabled, speed is low, and direction pressed
                let avatar =
                    if  not (World.isSelectedScreenTransitioning world) &&
                        entity.GetEnabled world then
                        let velocity = entity.GetLinearVelocity world
                        let speed = velocity.Length ()
                        if speed <= Constants.Field.AvatarIdleSpeedMax
                        then Avatar.updateDirection (constant direction) avatar
                        else avatar
                    else avatar
                just avatar

            | Separation separation ->

                // add separated body shape
                let avatar =
                    if isIntersectedBodyShape separation.Separator separation.Separatee world then
                        let avatar = Avatar.updateSeparatedBodyShapes (fun shapes -> separation.Separatee :: shapes) avatar
                        let avatar = Avatar.updateIntersectedBodyShapes (List.remove ((=) separation.Separatee)) avatar
                        avatar
                    else avatar
                just avatar

            | Collision collision ->

                // add collided body shape
                let avatar =
                    if isIntersectedBodyShape collision.Collider collision.Collidee world then
                        let avatar = Avatar.updateCollidedBodyShapes (fun shapes -> collision.Collidee :: shapes) avatar
                        let avatar = Avatar.updateIntersectedBodyShapes (fun shapes -> collision.Collidee :: shapes) avatar
                        avatar
                    else avatar
                just avatar

            | BodyRemoving physicsId ->
                
                // unfortunately, due to the fact that physics events like separation don't fire until the next frame,
                // we need to handle this Nu-generated event in order to remove the associated shape manually.
                let (separatedBodyShapes, intersectedBodyShapes) = List.split (fun shape -> shape.Entity.GetPhysicsId world = physicsId) avatar.IntersectedBodyShapes
                let avatar = Avatar.updateIntersectedBodyShapes (constant intersectedBodyShapes) avatar
                let avatar = Avatar.updateSeparatedBodyShapes ((@) separatedBodyShapes) avatar
                just avatar

            | Nil ->

                // nothing to do
                just avatar

        override this.Command (_, command, entity, world) =
            match command with
            | TryTravel force ->
                if  not (World.isSelectedScreenTransitioning world) &&
                    force <> v2Zero &&
                    entity.GetEnabled world then
                    let physicsId = Simulants.Field.Scene.Avatar.GetPhysicsId world
                    let world = World.applyBodyForce force physicsId world
                    just world
                else just world

        override this.Physics (position, _, _, _, avatar, _, _) =
            let avatar = Avatar.updatePosition (constant position) avatar
            just avatar

        override this.View (avatar, entity, world) =
            if entity.GetVisible world && entity.GetInView world then
                let transform = entity.GetTransform world
                Render (transform.Elevation, transform.Position.Y, AssetTag.generalize avatar.AnimationSheet,
                    SpriteDescriptor
                        { Transform = transform
                          Absolute = entity.GetAbsolute world
                          Offset = Vector2.Zero
                          InsetOpt = Some (getSpriteInset entity world)
                          Image = avatar.AnimationSheet
                          Color = Color.White
                          Blend = Transparent
                          Glow = Color.Zero
                          Flip = FlipNone })
            else View.empty