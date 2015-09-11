﻿namespace Nu
open System
open System.Collections.Generic
open OpenTK
open Prime

type [<NoEquality; NoComparison>] private QuadNode<'e when 'e : equality> =
    private
        { Depth : int
          Bounds : Vector4
          Children : 'e QuadNode array
          OptElements : 'e HashSet option }

[<RequireQualifiedAccess>]
module private QuadNode =

    let private makeChildPosition i position (childSize : Vector2) =
        match i with
        | 0 -> position
        | 1 -> position + Vector2 (childSize.X, 0.0f)
        | 2 -> position + Vector2 (0.0f, childSize.Y)
        | 3 -> position + childSize
        | _ -> failwithumf ()

    let rec internal tryAddElement bounds element node =
        if Math.isBoundsInBounds bounds node.Bounds then
            match node.OptElements with
            | Some elements -> elements.Add element
            | None ->
                // OPTIMIZATION: imperative to avoid creating a lambda each call
                let mutable result = false
                for child in node.Children do
                    result <- result || tryAddElement bounds element child
                result
        else false

    let rec internal tryRemoveElement bounds element node =
        if Math.isBoundsInBounds bounds node.Bounds then
            match node.OptElements with
            | Some elements -> elements.Remove element
            | None ->
                // OPTIMIZATION: imperative to avoid creating a lambda each call
                let mutable result = false
                for child in node.Children do
                    result <- result || tryRemoveElement bounds element child
                result
        else false

    let rec internal getElementsNearPoint position node =
        if Math.isPointInBounds position node.Bounds then
            match node.OptElements with
            | Some elements -> List.ofSeq elements
            | None -> List.ofSeq ^ Seq.concat ^ Seq.map (fun child -> getElementsNearPoint position child) node.Children
        else []

    let rec internal getElementsNearBounds bounds node =
        if Math.isBoundsInBounds bounds node.Bounds then
            match node.OptElements with
            | Some elements -> List.ofSeq elements
            | None -> List.ofSeq ^ Seq.concat ^ Seq.map (fun child -> getElementsNearBounds bounds child) node.Children
        else []

    let internal getDepth node =
        node.Depth

    let rec internal clone node =
        { Depth = node.Depth
          Bounds = node.Bounds
          Children = Array.map clone node.Children
          // NOTE: it is inefficient to shallow-clone a HashSet like this, but sadly, .NET does not provide a proper
          // Clone method! #ARGH!
          OptElements = Option.map (fun elements -> HashSet (elements, HashIdentity.Structural)) node.OptElements }

    let rec internal make<'e when 'e : equality> depth (bounds : Vector4) =
        if depth < 1 then failwith "Invalid depth for QuadNode. Expected depth >= 1."
        let children =
            if depth > 1 then 
                [|for i in 0 .. 3 do
                    let childDepth = depth - 1
                    let childSize = Vector2 (bounds.Z - bounds.X, bounds.W - bounds.Y) * 0.5f
                    let childPosition = makeChildPosition i bounds.Xy childSize
                    let childBounds = Vector4 (childPosition.X, childPosition.Y, childPosition.X + childSize.X, childPosition.Y + childSize.Y)
                    yield make<'e> childDepth childBounds|]
            else [||]
        { Depth = depth
          Bounds = bounds
          Children = (children : 'e QuadNode array)
          OptElements = match depth with 1 -> Some ^ HashSet HashIdentity.Structural | _ -> None }

type [<NoEquality; NoComparison>] QuadTree<'e when 'e : equality> =
    private
        { Node : 'e QuadNode
          OmnipresentElements : 'e HashSet }

[<RequireQualifiedAccess>]
module QuadTree =

    let addElement omnipresence bounds element tree =
        if omnipresence then
            tree.OmnipresentElements.Add element |> ignore
        elif not ^ QuadNode.tryAddElement bounds element tree.Node then
            note "Element is outside of quad tree's containment area or is being added redundantly."
            tree.OmnipresentElements.Add element |> ignore

    let removeElement omnipresence bounds element tree =
        if omnipresence then
            tree.OmnipresentElements.Remove element |> ignore
        elif not ^ QuadNode.tryRemoveElement bounds element tree.Node then
            note "Element is outside of quad tree's containment area or is not present for removal."
            tree.OmnipresentElements.Remove element |> ignore

    let updateElement
        oldOmnipresence oldBounds
        newOmnipresence newBounds
        element tree =
        removeElement oldOmnipresence oldBounds element tree
        addElement newOmnipresence newBounds element tree

    let getElementsNearPoint position tree =
        let otherElements = QuadNode.getElementsNearPoint position tree.Node
        List.ofSeq ^ Seq.append tree.OmnipresentElements ^ Seq.distinct otherElements

    let getElementsNearBounds bounds tree =
        let otherElements = QuadNode.getElementsNearBounds bounds tree.Node
        List.ofSeq ^ Seq.append tree.OmnipresentElements ^ Seq.distinct otherElements

    let getDepth tree =
        QuadNode.getDepth tree.Node

    let clone tree =
        { Node = QuadNode.clone tree.Node
          OmnipresentElements = HashSet (tree.OmnipresentElements, HashIdentity.Structural) }

    let make<'e when 'e : equality> depth bounds =
        { Node = QuadNode.make<'e> depth bounds
          OmnipresentElements = HashSet HashIdentity.Structural }