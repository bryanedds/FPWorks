﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2015.

namespace Nu
open System
open OpenTK
open Prime
open Nu

/// The subsystem for the world's physics system.
type [<ReferenceEquality>] PhysicsEngineSubsystem =
    private
        { SubsystemOrder : single
          PhysicsEngine : IPhysicsEngine }
        
    static member private handleBodyTransformMessage (message : BodyTransformMessage) (entity : Entity) world =
        // OPTIMIZATION: entity is not changed (avoiding a change entity event) if position and rotation haven't changed.
        if entity.GetPosition world <> message.Position || entity.GetRotation world <> message.Rotation then
            world |>
                // TODO: ASAP: could shave off a lot of perf penalty by implementing an Entity.SetTransform that only updates
                // the EntityTree once.
                // TODO: see if the following center-offsetting can be encapsulated within the Physics module!
                entity.SetPosition (message.Position - entity.GetSize world * 0.5f) |>
                entity.SetRotation message.Rotation
        else world

    static member private handleIntegrationMessage world integrationMessage =
        match world.State.Liveness with
        | Running ->
            match integrationMessage with
            | BodyTransformMessage bodyTransformMessage ->
                let entity = Entity.proxy ^ atoa bodyTransformMessage.SourceAddress
                if World.containsEntity entity world then
                    PhysicsEngineSubsystem.handleBodyTransformMessage bodyTransformMessage entity world
                else world
            | BodyCollisionMessage bodyCollisionMessage ->
                let source = Entity.proxy ^ atoa bodyCollisionMessage.SourceAddress
                match World.getOptEntityState source world with
                | Some _ ->
                    let collisionAddress = Events.Collision ->- bodyCollisionMessage.SourceAddress
                    let collisionData =
                        { Normal = bodyCollisionMessage.Normal
                          Speed = bodyCollisionMessage.Speed
                          Collidee = Entity.proxy ^ atoa bodyCollisionMessage.CollideeAddress }
                    World.publish collisionData collisionAddress Simulants.Game world
                | None -> world
        | Exiting -> world

    member this.BodyExists physicsId = this.PhysicsEngine.BodyExists physicsId
    member this.GetBodyContactNormals physicsId = this.PhysicsEngine.GetBodyContactNormals physicsId
    member this.GetBodyLinearVelocity physicsId = this.PhysicsEngine.GetBodyLinearVelocity physicsId
    member this.GetBodyGroundContactNormals physicsId = this.PhysicsEngine.GetBodyGroundContactNormals physicsId
    member this.GetBodyOptGroundContactNormal physicsId = this.PhysicsEngine.GetBodyOptGroundContactNormal physicsId
    member this.GetBodyOptGroundContactTangent physicsId = this.PhysicsEngine.GetBodyOptGroundContactTangent physicsId
    member this.BodyOnGround physicsId = this.PhysicsEngine.BodyOnGround physicsId

    interface Subsystem with
        member this.SubsystemType = UpdateType
        member this.SubsystemOrder = this.SubsystemOrder
        member this.ClearMessages () = { this with PhysicsEngine = this.PhysicsEngine.ClearMessages () } :> Subsystem
        member this.EnqueueMessage message = { this with PhysicsEngine = this.PhysicsEngine.EnqueueMessage (message :?> PhysicsMessage) } :> Subsystem
            
        member this.ProcessMessages world =
            let tickRate = World.getTickRate world
            let (integrationMessages, physicsEngine) = this.PhysicsEngine.Integrate tickRate
            (integrationMessages :> obj, { this with PhysicsEngine = physicsEngine } :> Subsystem, world)

        member this.ApplyResult (integrationMessages, world) =
            let integrationMessages = integrationMessages :?> IntegrationMessage list
            List.fold PhysicsEngineSubsystem.handleIntegrationMessage world integrationMessages

        member this.CleanUp world = (this :> Subsystem, world)

    static member make subsystemOrder physicsEngine =
        { SubsystemOrder = subsystemOrder
          PhysicsEngine = physicsEngine }

[<AutoOpen>]
module WorldPhysicsModule =

    type World with

        /// Add a physics message to the world.
        static member addPhysicsMessage (message : PhysicsMessage) world =
            World.updateSubsystem (fun is _ -> is.EnqueueMessage message) Constants.Engine.PhysicsEngineSubsystemName world

        /// Query that the world contains a body with the given physics id?
        static member bodyExists physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.BodyExists physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Get the contact normals of the body with the given physics id.
        static member getBodyContactNormals physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.GetBodyContactNormals physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Get the linear velocity of the body with the given physics id.
        static member getBodyLinearVelocity physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.GetBodyLinearVelocity physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Get the contact normals where the body with the given physics id is touching the ground.
        static member getBodyGroundContactNormals physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.GetBodyGroundContactNormals physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Try to get a contact normal where the body with the given physics id is touching the ground.
        static member getBodyOptGroundContactNormal physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.GetBodyOptGroundContactNormal physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Try to get a contact tangent where the body with the given physics id is touching the ground.
        static member getBodyOptGroundContactTangent physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.GetBodyOptGroundContactTangent physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Query that the body with the given physics id is on the ground.
        static member bodyOnGround physicsId world =
            World.getSubsystemBy (fun (physicsEngine : PhysicsEngineSubsystem) -> physicsEngine.BodyOnGround physicsId) Constants.Engine.PhysicsEngineSubsystemName world

        /// Send a message to the physics system to create a physics body.
        static member createBody (entityAddress : Entity Address) entityId bodyProperties world =
            let createBodyMessage = CreateBodyMessage { SourceAddress = atooa entityAddress; SourceId = entityId; BodyProperties = bodyProperties }
            World.addPhysicsMessage createBodyMessage world

        /// Send a message to the physics system to create several physics bodies.
        static member createBodies (entityAddress : Entity Address) entityId bodyPropertyList world =
            let createBodiesMessage = CreateBodiesMessage { SourceAddress = atooa entityAddress; SourceId = entityId; BodyPropertyList = bodyPropertyList }
            World.addPhysicsMessage createBodiesMessage world

        /// Send a message to the physics system to destroy a physics body.
        static member destroyBody physicsId world =
            let destroyBodyMessage = DestroyBodyMessage { PhysicsId = physicsId }
            World.addPhysicsMessage destroyBodyMessage world

        /// Send a message to the physics system to destroy several physics bodies.
        static member destroyBodies physicsIds world =
            let destroyBodiesMessage = DestroyBodiesMessage { PhysicsIds = physicsIds }
            World.addPhysicsMessage destroyBodiesMessage world

        /// Send a message to the physics system to set the position of a body with the given physics id.
        static member setBodyPosition position physicsId world =
            let setBodyPositionMessage = SetBodyPositionMessage { PhysicsId = physicsId; Position = position }
            World.addPhysicsMessage setBodyPositionMessage world

        /// Send a message to the physics system to set the rotation of a body with the given physics id.
        static member setBodyRotation rotation physicsId world =
            let setBodyRotationMessage = SetBodyRotationMessage { PhysicsId = physicsId; Rotation = rotation }
            World.addPhysicsMessage setBodyRotationMessage world

        /// Send a message to the physics system to set the angular velocity of a body with the given physics id.
        static member setBodyAngularVelocity angularVelocity physicsId world =
            let setBodyAngularVelocityMessage = SetBodyAngularVelocityMessage { PhysicsId = physicsId; AngularVelocity = angularVelocity }
            World.addPhysicsMessage setBodyAngularVelocityMessage world

        /// Send a message to the physics system to apply angular impulse to a body with the given physics id.
        static member applyBodyAngularImpulse angularImpulse physicsId world =
            let applyBodyAngularImpulseMessage = ApplyBodyAngularImpulseMessage { PhysicsId = physicsId; AngularImpulse = angularImpulse }
            World.addPhysicsMessage applyBodyAngularImpulseMessage world

        /// Send a message to the physics system to set the linear velocity of a body with the given physics id.
        static member setBodyLinearVelocity linearVelocity physicsId world =
            let setBodyLinearVelocityMessage = SetBodyLinearVelocityMessage { PhysicsId = physicsId; LinearVelocity = linearVelocity }
            World.addPhysicsMessage setBodyLinearVelocityMessage world

        /// Send a message to the physics system to apply linear impulse to a body with the given physics id.
        static member applyBodyLinearImpulse linearImpulse physicsId world =
            let applyBodyLinearImpulseMessage = ApplyBodyLinearImpulseMessage { PhysicsId = physicsId; LinearImpulse = linearImpulse }
            World.addPhysicsMessage applyBodyLinearImpulseMessage world

        /// Send a message to the physics system to apply force to a body with the given physics id.
        static member applyBodyForce force physicsId world =
            let applyBodyForceMessage = ApplyBodyForceMessage { PhysicsId = physicsId; Force = force }
            World.addPhysicsMessage applyBodyForceMessage world