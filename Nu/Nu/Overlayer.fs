﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open Prime
open Nu

/// Describes the overlay state of a property.
type [<StructuralEquality; StructuralComparison>] internal OverlayState =
    | Bare
    | Altered
    | Overlaid
    | NonPersistent

/// An overlay.
[<Syntax
    ("", "", "", "", "",
     Constants.PrettyPrinter.StructuredThresholdMin,
     Constants.PrettyPrinter.DetailedThresholdMax);
     StructuralEquality; StructuralComparison>]
type Overlay =
    { OverlayName : string
      OverlaysInherited : string list
      OverlaidTypeNames : string list
      OverlayProperties : Map<string, Symbol> }

    static member typeNameToOverlayName (typeName : string) =
        typeName
            .Replace("Dispatcher", "Overlay")
            .Replace("Facet", "FacetOverlay")

    /// Make intrinsic overlays.
    static member makeIntrinsicOverlays requiresFacetNames sourceTypes =

        // get the unique, decomposed source types
        let decomposedTypes =
            seq {
                for sourceType in sourceTypes do
                    yield sourceType
                    for sourceTypeDecomposed in Reflection.getBaseTypesExceptObject sourceType do
                        yield sourceTypeDecomposed } |>
            HashSet |>
            Seq.toList

        // get the descriptors needed to construct the overlays
        let overlayDescriptors =
            List.map
                (fun (sourceType : Type) ->
                    let includeNames =
                        if sourceType.BaseType <> typeof<obj>
                        then [Overlay.typeNameToOverlayName sourceType.BaseType.Name]
                        else []
                    let definitions = Reflection.getPropertyDefinitionsNoInherit sourceType
                    let requiresFacetNames = requiresFacetNames sourceType
                    (Overlay.typeNameToOverlayName sourceType.Name, sourceType.Name, includeNames, definitions, requiresFacetNames))
                decomposedTypes

        // create the intrinsic overlays with the above descriptors
        let overlays =
            List.map
                (fun (overlayName, overlaidTypeName, includeNames, definitions, requiresFacetNames) ->
                    let overlayProperties =
                        List.foldBack
                            (fun definition overlayProperties ->
                                match definition.PropertyExpr with
                                | DefineExpr value ->
                                    let converter = SymbolicConverter (false, None, definition.PropertyType)
                                    let overlayProperty = converter.ConvertTo (value, typeof<Symbol>) :?> Symbol
                                    Map.add definition.PropertyName overlayProperty overlayProperties
                                | VariableExpr _ -> overlayProperties
                                | ComputedExpr _ -> overlayProperties)
                            definitions
                            Map.empty
                    let overlayProperties =
                        if requiresFacetNames
                        then Map.add Property? FacetNames (Symbols ([], None)) overlayProperties
                        else overlayProperties
                    { OverlayName = overlayName
                      OverlaysInherited = includeNames
                      OverlaidTypeNames = [overlaidTypeName]
                      OverlayProperties = overlayProperties })
                overlayDescriptors

        // fin
        overlays

[<RequireQualifiedAccess>]
module Overlayer =

    /// Defines the manner in which overlays are applied to targets.
    type [<ReferenceEquality>] Overlayer =
        private
            { IntrinsicOverlays : Overlay list
              ExtrinsicOverlays : Overlay list
              Overlays : Map<string, Overlay> }

    let rec private getOverlaySymbols2 overlayName overlayer =
        match Map.tryFind overlayName overlayer.Overlays with
        | Some overlay ->
            List.fold (fun overlaySymbols overlayName ->
                let overlaySymbols' = getOverlaySymbols2 overlayName overlayer
                Map.concat overlaySymbols' overlaySymbols)
                overlay.OverlayProperties
                overlay.OverlaysInherited
        | None -> Map.empty

    let internal getOverlaySymbols overlayName facetNames overlayer =
        let overlaySymbols = getOverlaySymbols2 overlayName overlayer
        Seq.fold (fun overlaySymbols facetName ->
            let facetOverlayName = Overlay.typeNameToOverlayName facetName
            let overlaySymbols' = getOverlaySymbols2 facetOverlayName overlayer
            Map.concat overlaySymbols' overlaySymbols)
            overlaySymbols
            facetNames

    let internal getPropertyState propertyName propertyType target overlaySymbols =
        if not (Reflection.isPropertyNonPersistentByName propertyName) then
            match Map.tryFind propertyName overlaySymbols with
            | Some propertySymbol -> 
                let targetType = target.GetType ()
                let propertyOpt = targetType.GetProperty propertyName
                let xtensionOpt =
                    match targetType.GetProperty "Xtension" with
                    | null -> None
                    | xtensionProperty ->
                        match xtensionProperty.GetValue target with
                        | :? Xtension as xtension -> Some xtension
                        | _ -> None
                let propertyValueOpt =
                    match (propertyOpt, xtensionOpt) with
                    | (null, Some xtension) ->
                        let mutable property = Unchecked.defaultof<_>
                        match Xtension.tryGetProperty (propertyName, xtension, &property) with
                        | true -> Some property.PropertyValue
                        | false -> None
                    | (null, None) -> None
                    | (targetProperty, _) -> Some (targetProperty.GetValue target)
                match propertyValueOpt with
                | Some propertyValue ->
                    match propertySymbol with
                    | Symbols ([Text (str, _); _], _) when isNull (Type.GetType str) -> Bare
                    | _ ->
                        let converter = SymbolicConverter (false, None, propertyType)
                        if converter.CanConvertFrom typeof<Symbol> then
                            let overlayValue = converter.ConvertFrom propertySymbol
                            if overlayValue =/= propertyValue then Altered else Overlaid
                        else Bare
                | None -> Bare
            | None -> Bare
        else NonPersistent
        
    let internal shouldPropertySerialize propertyName propertyType target overlaySymbols =
        match getPropertyState propertyName propertyType target overlaySymbols with
        | Altered | Bare -> true
        | Overlaid | NonPersistent -> false

    let internal shouldApplyOverlay (property : PropertyInfo) target oldOverlaySymbols =
        if property.PropertyType <> typeof<Xtension> then
            match getPropertyState property.Name property.PropertyType target oldOverlaySymbols with
            | Bare | Overlaid -> true
            | Altered | NonPersistent -> false
        else false

    let internal tryApplyOverlayToRecordProperty property (propertySymbol : Symbol) target oldOverlaySymbols =
        if shouldApplyOverlay property target oldOverlaySymbols then
            let converter = SymbolicConverter (false, None, property.PropertyType)
            if converter.CanConvertFrom typeof<Symbol> then
                let propertyValue = converter.ConvertFrom propertySymbol
                property.SetValue (target, propertyValue)

    let internal applyOverlayToProperties target oldOverlaySymbols newOverlaySymbols =
        let targetType = target.GetType ()
        let recordProperties = targetType.GetProperties ()
        for property in recordProperties do
            if property.Name <> "FacetNames" && property.PropertyType <> typeof<string Set> then
                match Map.tryFind property.Name newOverlaySymbols with
                | Some propertySymbol -> tryApplyOverlayToRecordProperty property propertySymbol target oldOverlaySymbols
                | None -> ()

    let internal applyOverlayToXtension target oldOverlaySymbols newOverlaySymbols =
        let targetType = target.GetType ()
        match targetType.GetProperty "Xtension" with
        | null -> ()
        | xtensionProperty ->
            match xtensionProperty.GetValue target with
            | :? Xtension as xtension ->
                let xtension =
                    Map.foldBack (fun propertyName propertySymbol xtension ->
                        let mutable property = Unchecked.defaultof<_>
                        match Xtension.tryGetProperty (propertyName, xtension, &property) with
                        | true ->
                            let propertyType = property.PropertyType
                            match getPropertyState propertyName propertyType target oldOverlaySymbols with
                            | Bare | Overlaid ->
                                let converter = SymbolicConverter (true, None, propertyType)
                                let propertyValue = converter.ConvertFrom propertySymbol
                                let property = { PropertyType = propertyType; PropertyValue = propertyValue;  }
                                Xtension.attachProperty propertyName property xtension
                            | Altered | NonPersistent -> xtension
                        | false ->
                            let recordProperties = targetType.GetProperties ()
                            if Array.notExists (fun (property : PropertyInfo) -> property.Name = propertyName) recordProperties then
                                match propertySymbol with
                                | Symbols ([Text (str, _); _], _) when notNull (Type.GetType str) ->
                                    let propertyType = typeof<DesignerProperty>
                                    match getPropertyState propertyName propertyType target oldOverlaySymbols with
                                    | Bare | Overlaid ->
                                        let converter = SymbolicConverter (true, None, propertyType)
                                        let propertyValue = converter.ConvertFrom propertySymbol
                                        let property = { PropertyType = propertyType; PropertyValue = propertyValue;  }
                                        Xtension.attachProperty propertyName property xtension
                                    | Altered | NonPersistent -> xtension
                                | _ -> xtension
                            else xtension)
                        newOverlaySymbols
                        xtension
                xtensionProperty.SetValue (target, xtension)
            | _ -> ()

    let internal applyOverlayToFacetNames4 (copyTarget : 'a -> 'a) target oldOverlaySymbols newOverlaySymbols =
        let target = copyTarget target
        let targetType = target.GetType ()
        match targetType.GetProperty "FacetNames" with
        | null -> target
        | facetNamesProperty ->
            match Map.tryFind facetNamesProperty.Name newOverlaySymbols with
            | Some propertySymbol ->
                tryApplyOverlayToRecordProperty facetNamesProperty propertySymbol target oldOverlaySymbols
                target
            | None -> target

    /// Apply an overlay to the FacetNames property of the given target.
    let applyOverlayToFacetNames (copyTarget : 'a -> 'a) oldOverlayName newOverlayName target oldOverlayer newOverlayer =
        let oldOverlaySymbols = getOverlaySymbols oldOverlayName Seq.empty oldOverlayer
        let newOverlaySymbols = getOverlaySymbols newOverlayName Seq.empty newOverlayer
        applyOverlayToFacetNames4 copyTarget target oldOverlaySymbols newOverlaySymbols

    /// Apply an overlay to the given target (except for any FacetNames property).
    /// Only the properties that are overlaid by the old overlay as specified by the old
    /// overlayer will be changed.
    let applyOverlay6 (copyTarget : 'a -> 'a) oldOverlayName newOverlayName facetNames target oldOverlayer newOverlayer =
        let target = copyTarget target
        let oldOverlaySymbols = getOverlaySymbols oldOverlayName facetNames oldOverlayer
        let newOverlaySymbols = getOverlaySymbols newOverlayName Seq.empty newOverlayer
        applyOverlayToProperties target oldOverlaySymbols newOverlaySymbols
        applyOverlayToXtension target oldOverlaySymbols newOverlaySymbols
        target

    /// Apply an overlay to the given target.
    /// Only the properties that are overlaid by the old overlay will be changed.
    let applyOverlay copyTarget oldOverlayName newOverlayName facetNames target overlayer =
        applyOverlay6 copyTarget oldOverlayName newOverlayName facetNames target overlayer overlayer

    /// Get intrinsic overlays.
    let getIntrinsicOverlays overlayer =
        overlayer.IntrinsicOverlays

    /// Get extrinsic overlays.
    let getExtrinsicOverlays overlayer =
        overlayer.ExtrinsicOverlays

    /// Get overlays.
    let getOverlays overlayer =
        overlayer.Overlays

    /// The empty overlayer.
    let empty =
        { IntrinsicOverlays = List.empty
          ExtrinsicOverlays = List.empty
          Overlays = Map.empty }

    /// Make an overlayer.
    let make intrinsicOverlays extrinsicOverlays =
        let intrinsicOverlaysMap = Map.ofListBy (fun overlay -> (overlay.OverlayName, overlay)) intrinsicOverlays
        let extrinsicOverlaysMap = Map.ofListBy (fun overlay -> (overlay.OverlayName, overlay)) extrinsicOverlays
        { IntrinsicOverlays = intrinsicOverlays
          ExtrinsicOverlays = extrinsicOverlays
          Overlays = Map.concat intrinsicOverlaysMap extrinsicOverlaysMap }

    /// Attempt to make an overlayer by loading overlays from a file and then combining it with
    /// the given intrinsic overlays.
    let tryMakeFromFile intrinsicOverlays (filePath : string) =
        try let extrinsicOverlays =
                File.ReadAllText filePath |>
                String.unescape |>
                scvalue<Overlay list>
            make intrinsicOverlays extrinsicOverlays |> Right
        with exn -> Left ("Could not make overlayer from file '" + filePath + "' due to: " + scstring exn)

/// Defines the manner in which overlays are applied to targets.
type Overlayer = Overlayer.Overlayer