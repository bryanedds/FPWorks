﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2012-2013.

[<AutoOpen>]
module Dictionary
open System.Collections.Generic

/// Create a dictionary with a single item.
let singleton elem = List.toDictionary [elem] // TODO: change implementation to accommodate 2 separate params (1 for key, 1 for value)

/// Map over a dictionary. A new dictionary is produced.
let map (mapper : KeyValuePair<'k, 'v> -> 'v) (dictionary : Dictionary<'k, 'v>) =
    let dictionary2 = Dictionary<'k, 'v> ()
    for kvp in dictionary do dictionary2.Add (kvp.Key, mapper kvp)
    dictionary2

let inline tryFind key (dictionary : Dictionary<'k, 'v>) =
    let valueRef = ref Unchecked.defaultof<'v>
    if dictionary.TryGetValue (key, valueRef)
    then Some valueRef.Value
    else None

let dictC kvps =
    let dictionary = Dictionary ()
    for (key, value) in kvps do dictionary.Add (key, value)
    dictionary

let addMany kvps (dictionary : Dictionary<'k, 'v>) =
    for (key, value) in kvps do dictionary.Add (key, value)
    dictionary

/// Dictionary extension methods.
type Dictionary<'k, 'v> with

    /// Force the addition of an element, removing the existing one if necessary.
    member this.ForceAdd (key, value) =
        let forced = this.Remove key
        this.Add (key, value)
        forced

    /// Check value equality of dictionary.
    /// NOTE: be wary the highly imperative nature of this code.
    member this.ValueEquals (other : Dictionary<'k, 'v>) =
        let mutable enr = this.GetEnumerator ()
        let mutable enr2 = other.GetEnumerator ()
        let mutable moving = true
        let mutable equal = true
        while moving && equal do
            if enr.MoveNext () then
                if enr2.MoveNext () then equal <- enr.Current = enr2.Current
                else equal <- false
            else
                if enr2.MoveNext () then equal <- false
                else moving <- false
        equal