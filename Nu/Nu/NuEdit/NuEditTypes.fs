﻿// NuEdit - The Nu Game Engine editor.
// Copyright (C) Bryan Edds, 2013-2015.

namespace NuEdit
open NuEditDesign
open OpenTK
open Prime
open System
open System.ComponentModel
open System.Collections.Generic
open Prime
open Nu

type WorldChanger = World -> World

type WorldChangers = WorldChanger Collections.Generic.List

type DragEntityState =
    | DragEntityPosition of Vector2 * Vector2 * Entity
    | DragEntityRotation of Vector2 * Vector2 * Entity
    | DragEntityNone

type DragCameraState =
    | DragCameraPosition of Vector2 * Vector2
    | DragCameraNone

type EditorState =
    { TargetDir : string
      RightClickPosition : Vector2
      DragEntityState : DragEntityState
      DragCameraState : DragCameraState
      PastWorlds : World list
      FutureWorlds : World list
      SelectedGroup : Group }

type [<TypeDescriptionProvider (typeof<EntityTypeDescriptorProvider>)>] EntityTypeDescriptorSource =
    { DescribedEntity : Entity
      Form : NuEditForm
      WorldChangers : WorldChangers
      RefWorld : World ref }

and EntityPropertyDescriptor (property, attributes) =
    inherit PropertyDescriptor (
        (match property with EntityXFieldDescriptor xfd -> xfd.FieldName | EntityPropertyInfo pi -> pi.Name),
        attributes)

    let propertyName = match property with EntityXFieldDescriptor xfd -> xfd.FieldName | EntityPropertyInfo pi -> pi.Name
    let propertyType = match property with EntityXFieldDescriptor xfd -> xfd.FieldType | EntityPropertyInfo pi -> pi.PropertyType
    let propertyCanWrite = match property with EntityXFieldDescriptor _ -> true | EntityPropertyInfo xfd -> xfd.CanWrite

    let pushPastWorld pastWorld world =
        World.updateUserState
            (fun editorState -> { editorState with PastWorlds = pastWorld :: editorState.PastWorlds; FutureWorlds = [] })
            world

    override this.ComponentType = propertyType.DeclaringType
    override this.PropertyType = propertyType
    override this.CanResetValue _ = false
    override this.ResetValue _ = ()
    override this.ShouldSerializeValue _ = true

    override this.IsReadOnly =
        not propertyCanWrite ||
        not ^ Reflection.isPropertyPersistentByName propertyName

    override this.GetValue source =
        match source with
        | null -> null // WHY THE FUCK IS THIS EVER null???
        | source ->
            let entityTds = source :?> EntityTypeDescriptorSource
            EntityMemberValue.getValue property entityTds.DescribedEntity !entityTds.RefWorld

    override this.SetValue (source, value) =
        
        // grab the type descriptor and assign the value
        let entityTds = source :?> EntityTypeDescriptorSource
        let changer = (fun world ->

            // TODO: comment
            let world = pushPastWorld world world
            match propertyName with
            | "Name" ->
                let valueStr = acstring value
                if fst ^ Int64.TryParse valueStr then
                    trace ^ "Invalid entity name '" + valueStr + "' (must not be a number)."
                    world
                else
                    let entity = entityTds.DescribedEntity
                    let (entity, world) = World.reassignEntity entity (Some valueStr) (etog entity) world
                    entityTds.RefWorld := world // must be set for property grid
                    entityTds.Form.propertyGrid.SelectedObject <- { entityTds with DescribedEntity = entity }
                    world

            // TODO: comment
            | "FacetNames" ->
                let facetNames = value :?> string list
                let entity = entityTds.DescribedEntity
                let world =
                    match World.trySetEntityFacetNames facetNames entity world with
                    | Right world -> world
                    | Left error -> trace error; world
                entityTds.RefWorld := world // must be set for property grid
                entityTds.Form.propertyGrid.Refresh ()
                world

            // TODO: comment
            | _ ->
                let entity = entityTds.DescribedEntity
                let world =
                    match propertyName with
                    | "OptOverlayName" ->
                        match World.trySetEntityOptOverlayName (value :?> string option) entity world with
                        | Right world -> world
                        | Left error -> trace error; world
                    | _ -> EntityMemberValue.setValue property value entity world
                let world = World.propagateEntityPhysics entityTds.DescribedEntity world
                entityTds.RefWorld := world // must be set for property grid
                entityTds.Form.propertyGrid.Refresh ()
                world)

        // NOTE: in order to update the view immediately, we have to apply the changer twice,
        // once immediately and once in the update function
        entityTds.RefWorld := changer !entityTds.RefWorld
        entityTds.WorldChangers.Add changer |> ignore

and EntityTypeDescriptor (optSource : obj) =
    inherit CustomTypeDescriptor ()
    override this.GetProperties _ =
        let optXtension =
            match optSource with
            | :? EntityTypeDescriptorSource as source -> Some (source.DescribedEntity.GetXtension !source.RefWorld)
            | _ -> None
        ignore optXtension
        let makePropertyDescriptor = fun (emv, tcas) -> (EntityPropertyDescriptor (emv, Array.map (fun attr -> attr :> Attribute) tcas)) :> PropertyDescriptor
        let propertyDescriptors = EntityMemberValue.getPropertyDescriptors makePropertyDescriptor optXtension
        PropertyDescriptorCollection (Array.ofList propertyDescriptors)

and EntityTypeDescriptorProvider () =
    inherit TypeDescriptionProvider ()
    override this.GetTypeDescriptor (_, optSource) = EntityTypeDescriptor optSource :> ICustomTypeDescriptor