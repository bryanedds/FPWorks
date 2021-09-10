﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections
open System.Collections.Generic
open System.Numerics
open Prime

[<RequireQualifiedAccess>]
module internal SpatialNode =

    type [<NoEquality; NoComparison>] SpatialNode<'e when 'e : equality> =
        private
            { Depth : int
              Bounds : Vector4
              Children : ValueEither<'e SpatialNode array, 'e HashSet> }

    let internal atPoint point node =
        Math.isPointInBounds point node.Bounds

    let internal isIntersectingBounds bounds node =
        Math.isBoundsIntersectingBounds bounds node.Bounds

    let rec internal addElement bounds element node =
        if isIntersectingBounds bounds node then
            match node.Children with
            | ValueLeft nodes -> for node in nodes do addElement bounds element node
            | ValueRight elements -> elements.Add element |> ignore

    let rec internal removeElement bounds element node =
        if isIntersectingBounds bounds node then
            match node.Children with
            | ValueLeft nodes -> for node in nodes do removeElement bounds element node
            | ValueRight elements -> elements.Remove element |> ignore

    let rec internal updateElement oldBounds newBounds element node =
        match node.Children with
        | ValueLeft nodes ->
            for node in nodes do
                if isIntersectingBounds oldBounds node || isIntersectingBounds newBounds node then
                    updateElement oldBounds newBounds element node
        | ValueRight elements ->
            if isIntersectingBounds oldBounds node then
                if not (isIntersectingBounds newBounds node) then elements.Remove element |> ignore
            elif isIntersectingBounds newBounds node then
                elements.Add element |> ignore

    let rec internal getElementsAtPoint point node (set : 'e HashSet) =
        match node.Children with
        | ValueLeft nodes -> for node in nodes do if atPoint point node then getElementsAtPoint point node set
        | ValueRight elements -> for element in elements do set.Add element |> ignore

    let rec internal getElementsInBounds bounds node (set : 'e HashSet) =
        match node.Children with
        | ValueLeft nodes -> for node in nodes do if isIntersectingBounds bounds node then getElementsInBounds bounds node set
        | ValueRight elements -> for element in elements do set.Add element |> ignore

    let rec internal clone node =
        { Depth = node.Depth
          Bounds = node.Bounds
          Children =
            match node.Children with
            | ValueRight elements -> ValueRight (HashSet (elements, HashIdentity.Structural))
            | ValueLeft nodes -> ValueLeft (Array.map clone nodes) }

    let rec internal make<'e when 'e : equality> granularity depth (bounds : Vector4) =
        if granularity < 2 then failwith "Invalid granularity for SpatialNode. Expected value of at least 2."
        if depth < 1 then failwith "Invalid depth for SpatialNode. Expected value of at least 1."
        let children =
            if depth > 1 then
                let (nodes : 'e SpatialNode array) =
                    [|for i in 0 .. granularity * granularity - 1 do
                        let childDepth = depth - 1
                        let childSize = v2 bounds.Z bounds.W / single granularity
                        let childPosition = v2 bounds.X bounds.Y + v2 (childSize.X * single (i % granularity)) (childSize.Y * single (i / granularity))
                        let childBounds = v4Bounds childPosition childSize
                        yield make granularity childDepth childBounds|]
                ValueLeft nodes
            else ValueRight (HashSet<'e> HashIdentity.Structural)
        { Depth = depth
          Bounds = bounds
          Children = children }

type internal SpatialNode<'e when 'e : equality> = SpatialNode.SpatialNode<'e>

[<RequireQualifiedAccess>]
module SpatialTree =

    /// Provides an enumerator interface to spatial tree queries.
    type internal SpatialTreeEnumerator<'e when 'e : equality> (localElements : 'e HashSet, omnipresentElements : 'e HashSet) =

        let localList = List localElements // eagerly convert to list to keep iteration valid
        let omnipresentList = List omnipresentElements // eagerly convert to list to keep iteration valid
        let mutable localEnrValid = false
        let mutable omnipresentEnrValid = false
        let mutable localEnr = Unchecked.defaultof<_>
        let mutable omnipresentEnr = Unchecked.defaultof<_>

        interface 'e IEnumerator with
            member this.MoveNext () =
                if not localEnrValid then
                    localEnr <- localList.GetEnumerator ()
                    localEnrValid <- true
                    if not (localEnr.MoveNext ()) then
                        omnipresentEnr <- omnipresentList.GetEnumerator ()
                        omnipresentEnrValid <- true
                        omnipresentEnr.MoveNext ()
                    else true
                else
                    if not (localEnr.MoveNext ()) then
                        if not omnipresentEnrValid then
                            omnipresentEnr <- omnipresentList.GetEnumerator ()
                            omnipresentEnrValid <- true
                            omnipresentEnr.MoveNext ()
                        else omnipresentEnr.MoveNext ()
                    else true

            member this.Current =
                if omnipresentEnrValid then omnipresentEnr.Current
                elif localEnrValid then localEnr.Current
                else failwithumf ()

            member this.Current =
                (this :> 'e IEnumerator).Current :> obj

            member this.Reset () =
                localEnrValid <- false
                omnipresentEnrValid <- false
                localEnr <- Unchecked.defaultof<_>
                omnipresentEnr <- Unchecked.defaultof<_>

            member this.Dispose () =
                localEnr <- Unchecked.defaultof<_>
                omnipresentEnr <- Unchecked.defaultof<_>
            
    /// Provides an enumerable interface to spatial tree queries.
    type internal SpatialTreeEnumerable<'e when 'e : equality> (enr : 'e SpatialTreeEnumerator) =
        interface IEnumerable<'e> with
            member this.GetEnumerator () = enr :> 'e IEnumerator
            member this.GetEnumerator () = enr :> IEnumerator

    /// A spatial structure that organizes elements on a 2D plane. TODO: document this.
    type [<NoEquality; NoComparison>] SpatialTree<'e when 'e : equality> =
        private
            { Node : 'e SpatialNode
              OmnipresentElements : 'e HashSet
              Depth : int
              Granularity : int
              Bounds : Vector4 }

    let addElement omnipresent bounds element tree =
        if omnipresent then
            tree.OmnipresentElements.Add element |> ignore
        else
            if not (SpatialNode.isIntersectingBounds bounds tree.Node) then
                Log.info "Element is outside spatial tree's containment area or is being added redundantly."
                tree.OmnipresentElements.Add element |> ignore
            else SpatialNode.addElement bounds element tree.Node

    let removeElement omnipresent bounds element tree =
        if omnipresent then 
            tree.OmnipresentElements.Remove element |> ignore
        else
            if not (SpatialNode.isIntersectingBounds bounds tree.Node) then
                Log.info "Element is outside spatial tree's containment area or is not present for removal."
                tree.OmnipresentElements.Remove element |> ignore
            else SpatialNode.removeElement bounds element tree.Node

    let updateElement oldBounds newBounds element tree =
        let oldInBounds = SpatialNode.isIntersectingBounds oldBounds tree.Node
        let newInBounds = SpatialNode.isIntersectingBounds newBounds tree.Node
        if oldInBounds && not newInBounds then
            // going out of bounds
            Log.info "Element is outside spatial tree's containment area."
            if not newInBounds then tree.OmnipresentElements.Add element |> ignore
            SpatialNode.updateElement oldBounds newBounds element tree.Node
        elif not oldInBounds && newInBounds then
            // going back in bounds
            if not oldInBounds then tree.OmnipresentElements.Remove element |> ignore
            SpatialNode.updateElement oldBounds newBounds element tree.Node
        elif oldInBounds && newInBounds then
            // staying in bounds
            let rootBounds = tree.Bounds
            let rootDepth = pown tree.Granularity tree.Depth
            let leafSize = rootBounds.Size / single rootDepth
            let leafPosition =
                v2
                    (oldBounds.Position.X - (rootBounds.X + oldBounds.Position.X) % leafSize.X)
                    (oldBounds.Position.Y - (rootBounds.Y + oldBounds.Position.Y) % leafSize.Y)
            let leafBounds = v4Bounds leafPosition leafSize
            if  not (Math.isBoundsInBounds oldBounds leafBounds) ||
                not (Math.isBoundsInBounds newBounds leafBounds) then
                SpatialNode.updateElement oldBounds newBounds element tree.Node
        else
            // staying out of bounds
            ()

    let getElementsOmnipresent tree =
        let set = HashSet HashIdentity.Structural
        new SpatialTreeEnumerable<'e> (new SpatialTreeEnumerator<'e> (tree.OmnipresentElements, set)) :> 'e IEnumerable

    let getElementsAtPoint point tree =
        let set = HashSet HashIdentity.Structural
        SpatialNode.getElementsAtPoint point tree.Node set
        new SpatialTreeEnumerable<'e> (new SpatialTreeEnumerator<'e> (tree.OmnipresentElements, set)) :> 'e IEnumerable

    let getElementsInBounds bounds tree =
        let set = HashSet HashIdentity.Structural
        SpatialNode.getElementsInBounds bounds tree.Node set
        new SpatialTreeEnumerable<'e> (new SpatialTreeEnumerator<'e> (tree.OmnipresentElements, set)) :> 'e IEnumerable

    let getDepth tree =
        tree.Depth

    let clone tree =
        { Node = SpatialNode.clone tree.Node
          OmnipresentElements = HashSet (tree.OmnipresentElements, HashIdentity.Structural)
          Depth = tree.Depth
          Granularity = tree.Granularity
          Bounds = tree.Bounds }

    let make<'e when 'e : equality> granularity depth bounds =
        { Node = SpatialNode.make<'e> granularity depth bounds
          OmnipresentElements = HashSet HashIdentity.Structural
          Depth = depth
          Granularity = granularity
          Bounds = bounds }
          
/// A spatial structure that organizes elements on a 2D plane. TODO: document this.
type SpatialTree<'e when 'e : equality> = SpatialTree.SpatialTree<'e>