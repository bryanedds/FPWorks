﻿namespace Nu
open System
open System.ComponentModel
open System.Collections.Generic
open FarseerPhysics
open FarseerPhysics.Common
open FarseerPhysics.Dynamics
open FarseerPhysics.Dynamics.Contacts
open OpenTK
open Microsoft.Xna
open Prime
open Nu
open Nu.NuConstants

[<AutoOpen>]
module PhysicsModule =

    type PhysicsId =
        struct
            val Major : Guid
            val Minor : Guid
            new (major, minor) = { Major = major; PhysicsId.Minor = minor }
            override this.ToString () = "{ Major = " + string this.Major + "; Minor = " + string this.Minor + " }"
            end

    type Vertices = Vector2 list

    type [<StructuralEquality; NoComparison>] BoxShape =
        { Extent : Vector2
          Center : Vector2 } // NOTE: I guess this is like a center offset for the shape?

    type [<StructuralEquality; NoComparison>] CircleShape =
        { Radius : single
          Center : Vector2 } // NOTE: I guess this is like a center offset for the shape?

    type [<StructuralEquality; NoComparison>] CapsuleShape =
        { Height : single
          Radius : single
          Center : Vector2 } // NOTE: I guess this is like a center offset for the shape?

    type [<StructuralEquality; NoComparison>] PolygonShape =
        { Vertices : Vertices
          Center : Vector2 } // NOTE: I guess this is like a center offset for the shape?

    type [<StructuralEquality; NoComparison>] BodyShape =
        | BoxShape of BoxShape
        | CircleShape of CircleShape
        | CapsuleShape of CapsuleShape
        | PolygonShape of PolygonShape

    type [<StructuralEquality; NoComparison; TypeConverter (typeof<BodyTypeTypeConverter>)>] BodyType =
        | Static
        | Kinematic
        | Dynamic

    and BodyTypeTypeConverter () =
        inherit TypeConverter ()
        override this.CanConvertTo (_, destType) =
            destType = typeof<string>
        override this.ConvertTo (_, _, obj, _) =
            let bodyType = obj :?> BodyType
            match bodyType with
            | Static -> "Static" :> obj
            | Kinematic -> "Kinematic" :> obj
            | Dynamic -> "Dynamic" :> obj
        override this.CanConvertFrom (_, sourceType) =
            sourceType = typeof<Vector2> || sourceType = typeof<string>
        override this.ConvertFrom (_, _, obj) =
            let sourceType = obj.GetType ()
            if sourceType = typeof<BodyType> then obj
            else
                match obj :?> string with
                | "Static" -> Static :> obj
                | "Kinematic" -> Kinematic :> obj
                | "Dynamic" -> Dynamic :> obj
                | other -> failwith <| "Unknown BodyType '" + other + "'."

    type [<StructuralEquality; NoComparison>] BodyProperties =
        { Shape : BodyShape
          BodyType : BodyType
          Density : single
          Friction : single
          Restitution : single
          FixedRotation : bool
          LinearDamping : single
          AngularDamping : single
          GravityScale : single
          CollisionCategories : int
          CollisionMask : int
          IsBullet : bool
          IsSensor : bool }

    type [<StructuralEquality; NoComparison>] CreateBodyMessage =
        { EntityAddress : Address
          PhysicsId : PhysicsId
          Position : Vector2
          Rotation : single
          BodyProperties : BodyProperties }

    type [<StructuralEquality; NoComparison>] DestroyBodyMessage =
        { PhysicsId : PhysicsId }

    type [<StructuralEquality; NoComparison>] SetLinearVelocityMessage =
        { PhysicsId : PhysicsId
          LinearVelocity : Vector2 }

    type [<StructuralEquality; NoComparison>] ApplyLinearImpulseMessage =
        { PhysicsId : PhysicsId
          LinearImpulse : Vector2 }

    type [<StructuralEquality; NoComparison>] ApplyForceMessage =
        { PhysicsId : PhysicsId
          Force : Vector2 }

    type [<StructuralEquality; NoComparison>] BodyCollisionMessage =
        { EntityAddress : Address
          EntityAddress2 : Address
          Normal : Vector2
          Speed : single }

    type [<StructuralEquality; NoComparison>] BodyTransformMessage =
        { EntityAddress : Address
          Position : Vector2
          Rotation : single }

    type BodyDictionary =
        Dictionary<PhysicsId, Dynamics.Body>

    type [<StructuralEquality; NoComparison>] PhysicsMessage =
        | CreateBodyMessage of CreateBodyMessage
        | DestroyBodyMessage of DestroyBodyMessage
        | SetLinearVelocityMessage of SetLinearVelocityMessage
        | ApplyLinearImpulseMessage of ApplyLinearImpulseMessage
        | ApplyForceMessage of ApplyForceMessage
        | SetGravityMessage of Vector2
        | RebuildPhysicsHackMessage

    type [<StructuralEquality; NoComparison>] IntegrationMessage =
        | BodyCollisionMessage of BodyCollisionMessage
        | BodyTransformMessage of BodyTransformMessage

    type [<ReferenceEquality>] Integrator =
        { PhysicsContext : Dynamics.World
          Bodies : BodyDictionary
          IntegrationMessages : IntegrationMessage List
          mutable RebuildingHack : bool }
          
[<RequireQualifiedAccess>]
module Physics =

    let InvalidId =
        PhysicsId (NuCore.InvalidId, NuCore.InvalidId)

    let getId (entityId : Guid) =
        PhysicsId (entityId, NuCore.getId ())

    let private toPixel value =
        value * PhysicsToPixelRatio

    let private toPhysics value =
        value * PixelToPhysicsRatio

    let private toPixelV2 (v2 : Framework.Vector2) =
        Vector2 (toPixel v2.X, toPixel v2.Y)

    let private toPhysicsV2 (v2 : Vector2) =
        Framework.Vector2 (toPhysics v2.X, toPhysics v2.Y)

    let private toPhysicsBodyType bodyType =
        match bodyType with
        | Static -> Dynamics.BodyType.Static
        | Kinematic -> Dynamics.BodyType.Kinematic
        | Dynamic -> Dynamics.BodyType.Dynamic

    let private getNormalAndManifold (contact : Contact) =
        let (normal, manifold) = (ref <| Framework.Vector2 (), ref <| FixedArray2<Framework.Vector2> ())
        contact.GetWorldManifold (normal, manifold)
        (!normal, !manifold)

    let private handleCollision
        integrator
        (fixture : Dynamics.Fixture)
        (fixture2 : Dynamics.Fixture)
        (contact : Dynamics.Contacts.Contact) =
        let (normal, _) = getNormalAndManifold contact
        let bodyCollisionMessage =
            { EntityAddress = fixture.Body.UserData :?> Address
              EntityAddress2 = fixture2.Body.UserData :?> Address
              Normal = Vector2 (normal.X, normal.Y)
              Speed = contact.TangentSpeed * PhysicsToPixelRatio }
        let integrationMessage = BodyCollisionMessage bodyCollisionMessage
        integrator.IntegrationMessages.Add integrationMessage
        true

    let private getBodyContacts physicsId integrator =
        let body = integrator.Bodies.[physicsId]
        let contacts = List<Contact> ()
        let mutable current = body.ContactList
        while current <> null do
            contacts.Add current.Contact
            current <- current.Next
        List.ofSeq contacts

    let hasBody physicsId integrator =
        integrator.Bodies.ContainsKey physicsId

    let getBodyContactNormals physicsId integrator =
        let contacts = getBodyContacts physicsId integrator
        List.map
            (fun (contact : Contact) ->
                let (normal, _) = getNormalAndManifold contact
                Vector2 (normal.X, normal.Y))
            contacts

    let getLinearVelocity physicsId integrator =
        let body = integrator.Bodies.[physicsId]
        toPixelV2 body.LinearVelocity

    let getGroundContactNormals physicsId integrator =
        let normals = getBodyContactNormals physicsId integrator
        List.filter
            (fun normal ->
                let theta = Vector2.Dot (normal, Vector2.UnitY) |> double |> Math.Acos |> Math.Abs
                theta < Math.PI * 0.25)
            normals

    let getOptGroundContactNormal physicsId integrator =
        let groundNormals = getGroundContactNormals physicsId integrator
        if List.isEmpty groundNormals then None
        else
            let averageNormal = List.reduce (fun normal normal2 -> (normal + normal2) * 0.5f) groundNormals
            Some averageNormal

    let getOptGroundContactTangent physicsId integrator =
        match getOptGroundContactNormal physicsId integrator with
        | None -> None
        | Some normal -> Some <| Vector2 (normal.Y, -normal.X) 

    let isBodyOnGround physicsId integrator =
        let groundNormals = getGroundContactNormals physicsId integrator
        not <| List.isEmpty groundNormals

    let toCollisionCategories categoryExpr =
        match categoryExpr with
        | "*" -> -1
        | _ -> Convert.ToInt32 (categoryExpr, 2)

    let private configureBodyProperties bodyPosition bodyRotation bodyProperties (body : Body) =
        body.Position <- toPhysicsV2 bodyPosition
        body.Rotation <- bodyRotation
        body.Friction <- bodyProperties.Friction
        body.Restitution <- bodyProperties.Restitution
        body.FixedRotation <- bodyProperties.FixedRotation
        body.LinearDamping <- bodyProperties.LinearDamping
        body.AngularDamping <- bodyProperties.AngularDamping
        body.GravityScale <- bodyProperties.GravityScale
        body.CollisionCategories <- enum<Category> bodyProperties.CollisionCategories
        body.CollidesWith <- enum<Category> bodyProperties.CollisionMask
        body.IsBullet <- bodyProperties.IsBullet
        body.IsSensor <- bodyProperties.IsSensor
        body.SleepingAllowed <- true

    let private makeBoxBody (createBodyMessage : CreateBodyMessage) boxShape integrator =
        let body =
            Factories.BodyFactory.CreateRectangle (
                integrator.PhysicsContext,
                toPhysics <| boxShape.Extent.X * 2.0f,
                toPhysics <| boxShape.Extent.Y * 2.0f,
                createBodyMessage.BodyProperties.Density,
                toPhysicsV2 boxShape.Center,
                0.0f,
                toPhysicsBodyType createBodyMessage.BodyProperties.BodyType,
                createBodyMessage.EntityAddress)
        configureBodyProperties createBodyMessage.Position createBodyMessage.Rotation createBodyMessage.BodyProperties body
        body

    let private makeCircleBody (createBodyMessage : CreateBodyMessage) (circleShape : CircleShape) integrator =
        let body =
            Factories.BodyFactory.CreateCircle (
                integrator.PhysicsContext,
                toPhysics circleShape.Radius,
                createBodyMessage.BodyProperties.Density,
                toPhysicsV2 circleShape.Center,
                toPhysicsBodyType createBodyMessage.BodyProperties.BodyType,
                createBodyMessage.EntityAddress) // BUG: Farseer doesn't seem to set the UserData with the parameter I give it here...
        body.UserData <- createBodyMessage.EntityAddress // BUG: ...so I set it again here :/
        configureBodyProperties createBodyMessage.Position createBodyMessage.Rotation createBodyMessage.BodyProperties body
        body

    let private makeCapsuleBody (createBodyMessage : CreateBodyMessage) capsuleShape integrator =
        let body =
            Factories.BodyFactory.CreateCapsule (
                integrator.PhysicsContext,
                toPhysics capsuleShape.Height,
                toPhysics capsuleShape.Radius,
                createBodyMessage.BodyProperties.Density,
                toPhysicsV2 capsuleShape.Center,
                0.0f,
                toPhysicsBodyType createBodyMessage.BodyProperties.BodyType,
                createBodyMessage.EntityAddress) // BUG: Farseer doesn't seem to set the UserData with the parameter I give it here...
        body.UserData <- createBodyMessage.EntityAddress // BUG: ...so I set it again here :/
        
        // scale in the capsule's box to stop sticking
        let capsuleBox = body.FixtureList.[0].Shape :?> FarseerPhysics.Collision.Shapes.PolygonShape
        ignore <| capsuleBox.Vertices.Scale (Framework.Vector2 (0.75f, 1.0f))

        configureBodyProperties createBodyMessage.Position createBodyMessage.Rotation createBodyMessage.BodyProperties body
        body

    let private makePolygonBody (createBodyMessage : CreateBodyMessage) polygonShape integrator =
        let body =
            Factories.BodyFactory.CreatePolygon (
                integrator.PhysicsContext,
                FarseerPhysics.Common.Vertices (List.map toPhysicsV2 polygonShape.Vertices),
                createBodyMessage.BodyProperties.Density,
                toPhysicsV2 polygonShape.Center,
                0.0f,
                toPhysicsBodyType createBodyMessage.BodyProperties.BodyType,
                createBodyMessage.EntityAddress) // BUG: Farseer doesn't seem to set the UserData with the parameter I give it here...
        body.UserData <- createBodyMessage.EntityAddress // BUG: ...so I set it again here :/
        configureBodyProperties createBodyMessage.Position createBodyMessage.Rotation createBodyMessage.BodyProperties body
        body

    // TODO: remove code duplication here
    let private createBody createBodyMessage integrator =
        let body =
            match createBodyMessage.BodyProperties.Shape with
            | BoxShape boxShape -> makeBoxBody createBodyMessage boxShape integrator
            | CircleShape circleShape -> makeCircleBody createBodyMessage circleShape integrator
            | CapsuleShape capsuleShape -> makeCapsuleBody createBodyMessage capsuleShape integrator
            | PolygonShape polygonShape -> makePolygonBody createBodyMessage polygonShape integrator
        body.add_OnCollision (fun fn fn2 collision -> handleCollision integrator fn fn2 collision) // NOTE: F# requires us to use an lambda inline here (not sure why)
        integrator.Bodies.Add (createBodyMessage.PhysicsId, body)

    let private destroyBody (destroyBodyMessage : DestroyBodyMessage) integrator =
        let body = ref Unchecked.defaultof<Dynamics.Body>
        if  integrator.Bodies.TryGetValue (destroyBodyMessage.PhysicsId, body) then
            ignore <| integrator.Bodies.Remove destroyBodyMessage.PhysicsId
            integrator.PhysicsContext.RemoveBody !body
        elif not integrator.RebuildingHack then
             debug <| "Could not destroy non-existent body with PhysicsId = " + string destroyBodyMessage.PhysicsId + "'."

    let private setLinearVelocity (setLinearVelocityMessage : SetLinearVelocityMessage) integrator =
        let body = ref Unchecked.defaultof<Dynamics.Body>
        if  integrator.Bodies.TryGetValue (setLinearVelocityMessage.PhysicsId, body) then
            (!body).LinearVelocity <- toPhysicsV2 setLinearVelocityMessage.LinearVelocity
        else debug <| "Could not set linear velocity of non-existent body with PhysicsId = " + string setLinearVelocityMessage.PhysicsId + "'."

    let private applyLinearImpulse (applyLinearImpulseMessage : ApplyLinearImpulseMessage) integrator =
        let body = ref Unchecked.defaultof<Dynamics.Body>
        if  integrator.Bodies.TryGetValue (applyLinearImpulseMessage.PhysicsId, body) then
            (!body).ApplyLinearImpulse (toPhysicsV2 applyLinearImpulseMessage.LinearImpulse)
        else debug <| "Could not apply linear impulse to non-existent body with PhysicsId = " + string applyLinearImpulseMessage.PhysicsId + "'."

    let private applyForce applyForceMessage integrator =
        let body = ref Unchecked.defaultof<Dynamics.Body>
        if  integrator.Bodies.TryGetValue (applyForceMessage.PhysicsId, body) then
            (!body).ApplyForce (toPhysicsV2 applyForceMessage.Force)
        else debug <| "Could not apply force to non-existent body with PhysicsId = " + string applyForceMessage.PhysicsId + "'."

    let private handlePhysicsMessage integrator physicsMessage =
        match physicsMessage with
        | CreateBodyMessage createBodyMessage -> createBody createBodyMessage integrator
        | DestroyBodyMessage destroyBodyMessage -> destroyBody destroyBodyMessage integrator
        | SetLinearVelocityMessage setLinearVelocityMessage -> setLinearVelocity setLinearVelocityMessage integrator
        | ApplyLinearImpulseMessage applyLinearImpulseMessage -> applyLinearImpulse applyLinearImpulseMessage integrator
        | ApplyForceMessage applyForceMessage -> applyForce applyForceMessage integrator
        | SetGravityMessage gravity -> integrator.PhysicsContext.Gravity <- toPhysicsV2 gravity
        | RebuildPhysicsHackMessage ->
            integrator.RebuildingHack <- true
            integrator.PhysicsContext.Clear ()
            integrator.Bodies.Clear ()
            integrator.IntegrationMessages.Clear ()

    let private handlePhysicsMessages (physicsMessages : PhysicsMessage rQueue) integrator =
        let physicsMessagesRev = List.rev physicsMessages
        for physicsMessage in physicsMessagesRev do
            handlePhysicsMessage integrator physicsMessage
        integrator.RebuildingHack <- false

    let private createTransformMessages integrator =
        for body in integrator.Bodies.Values do
            if body.Awake && not body.IsStatic then
                let bodyTransformMessage =
                    BodyTransformMessage
                        { EntityAddress = body.UserData :?> Address
                          Position = toPixelV2 body.Position
                          Rotation = body.Rotation }
                integrator.IntegrationMessages.Add bodyTransformMessage

    let integrate (physicsMessages : PhysicsMessage rQueue) integrator =
        handlePhysicsMessages physicsMessages integrator
        integrator.PhysicsContext.Step PhysicsStepRate
        createTransformMessages integrator
        let messages = List.ofSeq integrator.IntegrationMessages
        integrator.IntegrationMessages.Clear ()
        messages

    let makeIntegrator gravity =
         { PhysicsContext = FarseerPhysics.Dynamics.World (toPhysicsV2 gravity)
           Bodies = BodyDictionary ()
           IntegrationMessages = List<IntegrationMessage> ()
           RebuildingHack = false }