﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Numerics
open BulletSharp
open Prime
open Nu

/// Tracks Bullet physics bodies by their PhysicsIds.
type internal BulletBodyDictionary = OrderedDictionary<PhysicsId, Vector3 option * RigidBody>

/// Tracks Bullet physics ghosts by their PhysicsIds.
type internal BulletGhostDictionary = OrderedDictionary<PhysicsId, GhostObject>

/// Tracks Bullet physics constraints by their PhysicsIds.
type internal BulletConstraintDictionary = OrderedDictionary<PhysicsId, TypedConstraint>

/// The BulletPhysics 3d implementation of PhysicsEngine.
type [<ReferenceEquality>] BulletPhysicsEngine =
    private
        { PhysicsContext : DynamicsWorld
          Bodies : BulletBodyDictionary
          Ghosts : BulletGhostDictionary
          Constraints : BulletConstraintDictionary
          CollisionConfiguration : CollisionConfiguration
          PhysicsDispatcher : Dispatcher
          BroadPhaseInterface : BroadphaseInterface
          ConstraintSolver : ConstraintSolver
          PhysicsMessages : PhysicsMessage UList
          IntegrationMessages : IntegrationMessage ConcurrentQueue
          mutable RebuildingHack : bool }

    static member make () =
        let physicsMessages = UList.makeEmpty Imperative
        let collisionConfiguration = new DefaultCollisionConfiguration ()
        let physicsDispatcher = new CollisionDispatcher (collisionConfiguration)
        let broadPhaseInterface = new DbvtBroadphase ()
        let constraintSolver = new SequentialImpulseConstraintSolver ()
        let world = new DiscreteDynamicsWorld (physicsDispatcher, broadPhaseInterface, constraintSolver, collisionConfiguration)
        let integrationMessages = ConcurrentQueue ()
        { PhysicsContext = world
          Bodies = OrderedDictionary HashIdentity.Structural
          Ghosts = OrderedDictionary HashIdentity.Structural
          Constraints = OrderedDictionary HashIdentity.Structural
          CollisionConfiguration = collisionConfiguration
          PhysicsDispatcher = physicsDispatcher
          BroadPhaseInterface = broadPhaseInterface
          ConstraintSolver = constraintSolver
          PhysicsMessages = physicsMessages
          IntegrationMessages = integrationMessages
          RebuildingHack = false }

    static member cleanUp physicsEngine =
        physicsEngine.PhysicsContext.Dispose ()
        physicsEngine.ConstraintSolver.Dispose ()
        physicsEngine.BroadPhaseInterface.Dispose ()
        physicsEngine.PhysicsDispatcher.Dispose ()
        physicsEngine.CollisionConfiguration.Dispose ()

    static member private configureBodyShapeProperties (_ : BodyProperties) (_ : BodyShapeProperties option) (_ : PolyhedralConvexShape) =
        () // NOTE: cannot configure bullet shapes on a per-shape basis.

    static member private configureObjectProperties (bodyProperties : BodyProperties) (object : CollisionObject) =
        if bodyProperties.Awake
        then object.ActivationState <- object.ActivationState &&& ~~~ActivationState.IslandSleeping
        else object.ActivationState <- object.ActivationState ||| ActivationState.IslandSleeping
        if bodyProperties.AwakeAlways
        then object.ActivationState <- object.ActivationState ||| ActivationState.DisableDeactivation
        else object.ActivationState <- object.ActivationState &&& ~~~ActivationState.DisableDeactivation
        if bodyProperties.Enabled
        then object.ActivationState <- object.ActivationState ||| ActivationState.DisableSimulation
        else object.ActivationState <- object.ActivationState &&& ~~~ActivationState.DisableSimulation
        object.Friction <- bodyProperties.Friction
        object.Restitution <- bodyProperties.Restitution
        match bodyProperties.CollisionDetection with
        | Discontinuous ->
            object.CcdMotionThreshold <- 0.0f
            object.CcdSweptSphereRadius <- 0.0f
        | Continuous continuous ->
            object.CcdMotionThreshold <- continuous.ContinuousMotionThreshold
            object.CcdSweptSphereRadius <- continuous.SweptSphereRadius
        match bodyProperties.BodyType with
        | Static ->
            object.CollisionFlags <- object.CollisionFlags ||| CollisionFlags.StaticObject
            object.CollisionFlags <- object.CollisionFlags &&& ~~~CollisionFlags.KinematicObject
        | Dynamic ->
            object.CollisionFlags <- object.CollisionFlags &&& ~~~CollisionFlags.StaticObject
            object.CollisionFlags <- object.CollisionFlags &&& ~~~CollisionFlags.KinematicObject
        | Kinematic ->
            object.CollisionFlags <- object.CollisionFlags ||| CollisionFlags.KinematicObject
            object.CollisionFlags <- object.CollisionFlags &&& ~~~CollisionFlags.StaticObject

    static member private configureBodyProperties (bodyProperties : BodyProperties) (body : RigidBody) gravity =
        BulletPhysicsEngine.configureObjectProperties bodyProperties body
        body.MotionState.WorldTransform <- Matrix4x4.CreateFromTrs (bodyProperties.Center, bodyProperties.Rotation, v3One)
        body.LinearVelocity <- bodyProperties.LinearVelocity
        body.AngularVelocity <- bodyProperties.AngularVelocity
        body.AngularFactor <- bodyProperties.AngularFactor
        body.SetDamping (bodyProperties.LinearDamping, bodyProperties.AngularDamping)
        body.Gravity <- match bodyProperties.GravityOverrideOpt with Some gravityOverride -> gravityOverride | None -> gravity

    static member private attachBodyBox sourceSimulant (bodyProperties : BodyProperties) (bodyBox : BodyBox) (compoundShape : CompoundShape) (massAccumulator : single ref) =
        let box = new BoxShape (bodyBox.Size * 0.5f)
        BulletPhysicsEngine.configureBodyShapeProperties bodyProperties bodyBox.PropertiesOpt box
        box.UserObject <-
            { Simulant = sourceSimulant
              BodyId = bodyProperties.BodyId
              ShapeId = match bodyBox.PropertiesOpt with Some p -> p.BodyShapeId | None -> 0UL }
        let mass =
            match bodyProperties.Substance with
            | Density density ->
                let volume = bodyBox.Size.X * bodyBox.Size.Y * bodyBox.Size.Z
                volume * density
            | Mass mass -> mass
        massAccumulator := massAccumulator.Value + mass
        compoundShape.AddChildShape (Matrix4x4.CreateFromTrs (bodyProperties.Center, bodyProperties.Rotation, v3Zero), box)

    //static member private attachBodySphere (bodySphere : BodySphere) (bodyProperties : BodyProperties) (compoundShapeIds : uint64 List) (compoundBuilder : CompoundBuilder array) =
    //    let bodyShapeId = match bodySphere.PropertiesOpt with Some bodyProperties2 -> bodyProperties2.BodyShapeId | None -> 0UL
    //    compoundShapeIds.Add bodyShapeId
    //    let sphere = Collidables.Sphere bodySphere.Radius
    //    let volume = 4.0f / 3.0f * MathF.PI * pown bodySphere.Radius 3
    //    let mass = volume * bodyProperties.Density
    //    let pose = RigidPose (bodyProperties.Center, bodyProperties.Rotation)
    //    compoundBuilder.[0].Add (&sphere, &pose, mass) // NOTE: passing mass as weight.
    //
    //static member private attachBodyCapsule (bodyCapsule : BodyCapsule) (bodyProperties : BodyProperties) (compoundShapeIds : uint64 List) (compoundBuilder : CompoundBuilder array) =
    //    let bodyShapeId = match bodyCapsule.PropertiesOpt with Some bodyProperties2 -> bodyProperties2.BodyShapeId | None -> 0UL
    //    compoundShapeIds.Add bodyShapeId
    //    let capsule = Capsule (bodyCapsule.Radius, bodyCapsule.Length)
    //    let volume = MathF.PI * bodyCapsule.Radius |> flip pown 2
    //    let mass = volume * bodyProperties.Density
    //    let pose = RigidPose (bodyProperties.Center, bodyProperties.Rotation)
    //    compoundBuilder.[0].Add (&capsule, &pose, mass) // NOTE: passing mass as weight.
    //
    //static member private attachBodyTriangle a b c bodyPropertiesOpt (bodyProperties : BodyProperties) (compoundShapeIds : uint64 List) (compoundBuilder : CompoundBuilder array) =
    //    let bodyShapeId = match bodyPropertiesOpt with Some bodyProperties2 -> bodyProperties2.BodyShapeId | None -> 0UL
    //    compoundShapeIds.Add bodyShapeId
    //    let capsule = Triangle (a, b, c)
    //    let ab = (b - a).Magnitude // NOTE: using Heron's formula.
    //    let bc = (c - b).Magnitude
    //    let ca = (a - c).Magnitude
    //    let s = (ab + bc + ca) * 0.5f
    //    let volume = sqrt (s * (s - ab) * (s - bc) * (s - ca))
    //    let mass = volume * bodyProperties.Density
    //    let pose = RigidPose (bodyProperties.Center, bodyProperties.Rotation)
    //    compoundBuilder.[0].Add (&capsule, &pose, mass) // NOTE: passing mass as weight.
    //
    //static member private attachBodyPolygon bodyPolygon bodyProperties compoundShapeIds compoundBuilder =
    //    if bodyPolygon.Vertices.Length >= 3 then
    //        let triangles = Array.windowed 3 bodyPolygon.Vertices
    //        for triangle in triangles do
    //            let (a, b, c) = (triangle.[0], triangle.[1], triangle.[2])
    //            BulletPhysicsEngine.attachBodyTriangle a b c bodyPolygon.PropertiesOpt bodyProperties compoundShapeIds compoundBuilder
    //    else Log.debug "Degenerate polygon sent to BulletPhysicsEngine; 3 or more vertices required."
    //
    //static member private attachBodyBoxRounded (bodyBoxRounded : BodyBoxRounded) (bodyProperties : BodyProperties) (compoundShapeIds : uint64 List) (compoundBuilder : CompoundBuilder array) =
    //    Log.debug "Rounded box not yet implemented via BulletPhysicsEngine; creating a normal box instead."
    //    let bodyBox = { Center = bodyBoxRounded.Center; Size = bodyBoxRounded.Size; PropertiesOpt = bodyBoxRounded.PropertiesOpt }
    //    BulletPhysicsEngine.attachBodyBox bodyBox bodyProperties compoundShapeIds compoundBuilder

    static member private attachBodyShapes sourceSimulant bodyProperties bodyShapes compoundShape massAccumulator =
        for bodyShape in bodyShapes do
            BulletPhysicsEngine.attachBodyShape sourceSimulant bodyProperties bodyShape compoundShape massAccumulator

    static member private attachBodyShape sourceSimulant bodyProperties bodyShape compoundShape massAccumulator =
        match bodyShape with
        | BodyEmpty -> ()
        | BodyBox bodyBox -> BulletPhysicsEngine.attachBodyBox sourceSimulant bodyProperties bodyBox compoundShape massAccumulator
        | BodySphere bodySphere -> () //BulletPhysicsEngine.attachBodySphere bodySphere bodyProperties compoundShapeIds compoundBuilder
        | BodyCapsule bodyCapsule -> () //BulletPhysicsEngine.attachBodyCapsule bodyCapsule bodyProperties compoundShapeIds compoundBuilder
        | BodyBoxRounded bodyBoxRounded -> () //BulletPhysicsEngine.attachBodyBoxRounded bodyBoxRounded bodyProperties compoundShapeIds compoundBuilder
        | BodyPolygon bodyPolygon -> () //BulletPhysicsEngine.attachBodyPolygon bodyPolygon bodyProperties compoundShapeIds compoundBuilder
        | BodyShapes bodyShapes -> BulletPhysicsEngine.attachBodyShapes sourceSimulant bodyProperties bodyShapes compoundShape massAccumulator

    static member private createBody3 attachBodyShape sourceId (bodyProperties : BodyProperties) physicsEngine =
        let massAccumulator = ref 0.0f
        let compoundShape = new CompoundShape ()
        attachBodyShape bodyProperties compoundShape massAccumulator
        if bodyProperties.Sensor then
            let motionState = new DefaultMotionState (Matrix4x4.CreateFromTrs (bodyProperties.Center, bodyProperties.Rotation, v3One))
            use constructionInfo = new RigidBodyConstructionInfo (massAccumulator.Value, motionState, compoundShape)
            let body = new RigidBody (constructionInfo)
            BulletPhysicsEngine.configureBodyProperties bodyProperties body physicsEngine.PhysicsContext.Gravity
            physicsEngine.PhysicsContext.AddRigidBody (body, bodyProperties.CollisionCategories, bodyProperties.CollisionMask)
            if not (physicsEngine.Bodies.TryAdd ({ SourceId = sourceId; CorrelationId = bodyProperties.BodyId }, (bodyProperties.GravityOverrideOpt, body))) then
                Log.debug ("Could not add body via '" + scstring bodyProperties + "'.")
        else
            let ghost = new GhostObject ()
            ghost.CollisionFlags <- ghost.CollisionFlags &&& ~~~CollisionFlags.NoContactResponse
            BulletPhysicsEngine.configureObjectProperties bodyProperties ghost
            physicsEngine.PhysicsContext.AddCollisionObject (ghost, bodyProperties.CollisionCategories, bodyProperties.CollisionMask)
            if not (physicsEngine.Ghosts.TryAdd ({ SourceId = sourceId; CorrelationId = bodyProperties.BodyId }, ghost)) then
                Log.debug ("Could not add body via '" + scstring bodyProperties + "'.")

    static member private createBody4 bodyShape bodyProperties (bodySource : BodySourceInternal) physicsEngine =
        BulletPhysicsEngine.createBody3
            (fun ps cs ma -> BulletPhysicsEngine.attachBodyShape bodySource.Simulant ps bodyShape cs ma)
            bodySource.BodyId bodyProperties physicsEngine

    static member private createBody (createBodyMessage : CreateBodyMessage) physicsEngine =
        let sourceSimulant = createBodyMessage.SourceSimulant
        let bodyProperties = createBodyMessage.BodyProperties
        let bodySource = { Simulant = sourceSimulant; BodyId = bodyProperties.BodyId }
        BulletPhysicsEngine.createBody4 bodyProperties.BodyShape bodyProperties bodySource physicsEngine

    static member private createBodies (createBodiesMessage : CreateBodiesMessage) physicsEngine =
        List.iter
            (fun bodyProperties ->
                let createBodyMessage =
                    { SourceSimulant = createBodiesMessage.SourceSimulant
                      SourceId = createBodiesMessage.SourceId
                      BodyProperties = bodyProperties }
                BulletPhysicsEngine.createBody createBodyMessage physicsEngine)
            createBodiesMessage.BodiesProperties

    static member private createJoint (createJointMessage : CreateJointMessage) physicsEngine =
        let jointProperties = createJointMessage.JointProperties
        match jointProperties.JointDevice with
        | JointEmpty ->
            ()
        | JointAngle jointAngle ->
            match (physicsEngine.Bodies.TryGetValue jointAngle.TargetId, physicsEngine.Bodies.TryGetValue jointAngle.TargetId2) with
            | ((true, (_, body)), (true, (_, body2))) ->
                let hinge = new HingeConstraint (body, body2, jointAngle.Anchor, jointAngle.Anchor2, jointAngle.Axis, jointAngle.Axis2)
                hinge.SetLimit (-jointAngle.AngleLimit * 0.5f, jointAngle.AngleLimit * 0.5f, jointAngle.Softness, jointAngle.BiasFactor)
                hinge.BreakingImpulseThreshold <- jointAngle.BreakImpulseThreshold
                physicsEngine.PhysicsContext.AddConstraint hinge
                if not (physicsEngine.Constraints.TryAdd ({ SourceId = createJointMessage.SourceId; CorrelationId = jointProperties.JointId }, hinge)) then
                    Log.debug ("Could not add joint via '" + scstring jointProperties + "'.")
            | (_, _) -> Log.debug "Could not set create a joint for one or more non-existent bodies."
        | _ -> failwithnie ()

    static member private handlePhysicsMessage physicsEngine physicsMessage =
        match physicsMessage with
        | CreateBodyMessage createBodyMessage -> BulletPhysicsEngine.createBody createBodyMessage physicsEngine
        | CreateBodiesMessage createBodiesMessage -> BulletPhysicsEngine.createBodies createBodiesMessage physicsEngine
        //| DestroyBodyMessage destroyBodyMessage -> BulletPhysicsEngine.destroyBody destroyBodyMessage physicsEngine
        //| DestroyBodiesMessage destroyBodiesMessage -> BulletPhysicsEngine.destroyBodies destroyBodiesMessage physicsEngine
        | CreateJointMessage createJointMessage -> BulletPhysicsEngine.createJoint createJointMessage physicsEngine
        //| CreateJointsMessage createJointsMessage -> BulletPhysicsEngine.createJoints createJointsMessage physicsEngine
        //| DestroyJointMessage destroyJointMessage -> BulletPhysicsEngine.destroyJoint destroyJointMessage physicsEngine
        //| DestroyJointsMessage destroyJointsMessage -> BulletPhysicsEngine.destroyJoints destroyJointsMessage physicsEngine
        //| SetBodyEnabledMessage setBodyEnabledMessage -> BulletPhysicsEngine.setBodyEnabled setBodyEnabledMessage physicsEngine
        //| SetBodyPositionMessage setBodyPositionMessage -> BulletPhysicsEngine.setBodyPosition setBodyPositionMessage physicsEngine
        //| SetBodyRotationMessage setBodyRotationMessage -> BulletPhysicsEngine.setBodyRotation setBodyRotationMessage physicsEngine
        //| SetBodyAngularVelocityMessage setBodyAngularVelocityMessage -> BulletPhysicsEngine.setBodyAngularVelocity setBodyAngularVelocityMessage physicsEngine
        //| ApplyBodyAngularImpulseMessage applyBodyAngularImpulseMessage -> BulletPhysicsEngine.applyBodyAngularImpulse applyBodyAngularImpulseMessage physicsEngine
        //| SetBodyLinearVelocityMessage setBodyLinearVelocityMessage -> BulletPhysicsEngine.setBodyLinearVelocity setBodyLinearVelocityMessage physicsEngine
        //| ApplyBodyLinearImpulseMessage applyBodyLinearImpulseMessage -> BulletPhysicsEngine.applyBodyLinearImpulse applyBodyLinearImpulseMessage physicsEngine
        //| ApplyBodyForceMessage applyBodyForceMessage -> BulletPhysicsEngine.applyBodyForce applyBodyForceMessage physicsEngine
        //| ApplyBodyTorqueMessage applyBodyTorqueMessage -> BulletPhysicsEngine.applyBodyTorque applyBodyTorqueMessage physicsEngine
        | SetGravityMessage gravity ->
            physicsEngine.PhysicsContext.Gravity <- gravity
            for (gravityOverrideOpt, body) in physicsEngine.Bodies.Values do
                match gravityOverrideOpt with
                | Some gravityOverride -> body.Gravity <- gravityOverride
                | None -> body.Gravity <- gravity
        | RebuildPhysicsHackMessage ->
            physicsEngine.RebuildingHack <- true
            for (_, body) in physicsEngine.Bodies.Values do physicsEngine.PhysicsContext.RemoveRigidBody body
            physicsEngine.Bodies.Clear ()
            for ghost in physicsEngine.Ghosts.Values do physicsEngine.PhysicsContext.RemoveCollisionObject ghost
            physicsEngine.Ghosts.Clear ()
            for constrain in physicsEngine.Constraints.Values do physicsEngine.PhysicsContext.RemoveConstraint constrain
            physicsEngine.Constraints.Clear ()
            physicsEngine.IntegrationMessages.Clear ()

    static member private integrate stepTime physicsEngine =
        let physicsStepAmount =
            match (Constants.GameTime.DesiredFrameRate, stepTime) with
            | (StaticFrameRate frameRate, UpdateTime frames) -> 1.0f / single frameRate * single frames
            | (DynamicFrameRate _, ClockTime secs) -> secs
            | (_, _) -> failwithumf ()
        if physicsStepAmount > 0.0f then
            let result = physicsEngine.PhysicsContext.StepSimulation physicsStepAmount
            ignore result

    static member private createIntegrationMessages physicsEngine =
        for (_, body) in physicsEngine.Bodies.Values do
            let asleep = int body.ActivationState &&& int ActivationState.IslandSleeping <> 0
            if not asleep then
                let bodyTransformMessage =
                    BodyTransformMessage
                        { BodySource = body.UserObject :?> BodySourceInternal
                          Center = body.MotionState.WorldTransform.Translation
                          Rotation = body.MotionState.WorldTransform.Rotation
                          LinearVelocity = body.LinearVelocity
                          AngularVelocity = body.AngularVelocity }
                physicsEngine.IntegrationMessages.Enqueue bodyTransformMessage

        static member private handlePhysicsMessages physicsMessages physicsEngine = () // TODO.

    interface PhysicsEngine with

        member physicsEngine.BodyExists physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.GetBodyContactNormals physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.GetBodyLinearVelocity physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.GetBodyToGroundContactNormals physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.GetBodyToGroundContactNormalOpt physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.GetBodyToGroundContactTangentOpt physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.IsBodyOnGround physicsId = Unchecked.defaultof<_> // TODO.
        member physicsEngine.PopMessages () = Unchecked.defaultof<_> // TODO.
        member physicsEngine.ClearMessages () = Unchecked.defaultof<_> // TODO.
        member physicsEngine.EnqueueMessage physicsMessage = Unchecked.defaultof<_> // TODO.

        member physicsEngine.Integrate stepTime physicsMessages =
            BulletPhysicsEngine.handlePhysicsMessages physicsMessages physicsEngine
            BulletPhysicsEngine.integrate stepTime physicsEngine
            BulletPhysicsEngine.createIntegrationMessages physicsEngine
            let integrationMessages = SegmentedArray.ofSeq physicsEngine.IntegrationMessages
            physicsEngine.IntegrationMessages.Clear ()
            integrationMessages