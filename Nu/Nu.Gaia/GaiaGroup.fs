﻿// Gaia - The Nu Game Engine editor.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu.Gaia
open System
open System.ComponentModel
open System.Windows.Forms
open Prime
open Nu
open Nu.Gaia
open Nu.Gaia.Design

// TODO: consider getting rid of the duplication of code from GaiaEntity.fs.

type [<TypeDescriptionProvider (typeof<GroupTypeDescriptorProvider>)>] GroupTypeDescriptorSource =
    { DescribedGroup : Group
      Form : GaiaForm }

and GroupPropertyDescriptor (propertyDescriptor, attributes) =
    inherit System.ComponentModel.PropertyDescriptor (propertyDescriptor.PropertyName, attributes)

    let propertyName =
        propertyDescriptor.PropertyName

    let propertyType =
        propertyDescriptor.PropertyType

    let propertyCanWrite =
        true

    override this.Category =
        // HACK: in order to put Scripts as the last category, I start all the other categories with an unprinted
        // \r character as here - https://bytes.com/topic/c-sharp/answers/214456-q-ordering-sorting-category-text-propertygrid
        if propertyName.EndsWith "Script" || propertyName.EndsWith "ScriptOpt" then "Scripts"
        elif propertyName = "Name" then "\rAmbient Properties"
        elif propertyName.EndsWith "Model" then "\rScene Properties"
        elif propertyName = "Persistent" || propertyName = "Elevation" || propertyName = "Visible" then "\rScene Properties"
        else "\rXtension Properties"

    override this.Description =
        // HACK: lets user know the property's expected type
        Reflection.getSimplifiedTypeNameHack propertyType

    override this.ComponentType = propertyType.DeclaringType
    override this.PropertyType = propertyType
    override this.CanResetValue _ = false
    override this.ResetValue _ = ()
    override this.ShouldSerializeValue _ = true

    override this.IsReadOnly =
        not propertyCanWrite ||
        Reflection.isPropertyNonPersistentByName propertyName

    override this.SetValue (source, value) =
        Globals.WorldChangers.Add $ fun world ->
        
            // grab the type descriptor and group
            let groupTds = source :?> GroupTypeDescriptorSource
            let group = groupTds.DescribedGroup

            // pull string quotes out of string
            let value =
                match value with
                | :? string as str -> str.Replace ("\"", "") :> obj
                | _ -> value

            // make property change undo-able
            Globals.pushPastWorld world
            match propertyName with
            
            // change the name property
            | "Name" ->
                MessageBox.Show
                    ("Changing the name of a group after it has been created is not yet implemented.",
                     "Cannot change group name in Gaia.",
                     MessageBoxButtons.OK) |>
                    ignore
                world

            // change the property dynamically
            | _ ->
                let world =
                    match propertyName with
                    | "OverlayNameOpt" ->
                        MessageBox.Show
                            ("Changing the overlay of a group after it has been created is not yet implemented.",
                             "Cannot change group overlay in Gaia.",
                             MessageBoxButtons.OK) |>
                            ignore
                        world
                    | _ ->
                        let struct (_, _, world) = PropertyDescriptor.trySetValue propertyDescriptor value group world
                        world
                Globals.World <- world // must be set for property grid
                groupTds.Form.groupPropertyGrid.Refresh ()
                world

    override this.GetValue source =
        match source with
        | null -> null // WHY THE FUCK IS THIS EVER null???
        | source ->
            let groupTds = source :?> GroupTypeDescriptorSource
            PropertyDescriptor.tryGetValue propertyDescriptor groupTds.DescribedGroup Globals.World |> Option.get

and GroupTypeDescriptor (sourceOpt : obj) =
    inherit CustomTypeDescriptor ()

    override this.GetProperties () =
        let contextOpt =
            match sourceOpt with
            | :? GroupTypeDescriptorSource as source -> Some (source.DescribedGroup :> Simulant, Globals.World)
            | _ -> None
        let makePropertyDescriptor = fun (epv, tcas) -> (GroupPropertyDescriptor (epv, Array.map (fun attr -> attr :> Attribute) tcas)) :> System.ComponentModel.PropertyDescriptor
        let propertyDescriptors = PropertyDescriptor.getPropertyDescriptors<GroupState> makePropertyDescriptor contextOpt
        PropertyDescriptorCollection (Array.ofList propertyDescriptors)

    override this.GetProperties _ =
        this.GetProperties ()

and GroupTypeDescriptorProvider () =
    inherit TypeDescriptionProvider ()
    override this.GetTypeDescriptor (_, sourceOpt) = GroupTypeDescriptor sourceOpt :> ICustomTypeDescriptor
