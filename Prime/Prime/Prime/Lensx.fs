﻿// Prime - A PRIMitivEs code library.
// Copyright (C) Bryan Edds, 2012-2014.

namespace Prime
open FSharpx

[<AutoOpen>]
module Lensx =

    // Here, Lens operations are globalized for convenience, except for Lens creation, composition, and
    // conversion operations.

#if FULLY_GLOBAL_LENS
    /// Makes a lens that maps the given lens in an array
    let arrayMap = Lens.arrayMap

    /// Composes two lenses through a sum in the source
    let choice = Lens.choice

    /// Sequentially composes two lenses
    let compose = Lens.compose

    /// Undocumented
    let codiag<'T> = Lens.codiag

    /// <summary>
    /// <paramref name="pred"/> is applied to source. 
    /// If true, <paramref name="lensTrue"/> is selected.
    /// If false, <paramref name="lensFalse"/> is selected.
    /// </summary>
    let cond = Lens.cond
#endif

    /// Lens for a particular position in an array
    let forArray = Lens.forArray

    /// Lens for a particular position in a list
    let forList = Lens.forList

    /// Lens for a particular key in a map
    let forMap = Lens.forMap

    /// Lens for a particular value in a set
    let forSet = Lens.forSet

    /// Gets/sets the fst element in a pair through a lens
    let fstL = Lens.fst

    /// Get a value through a lens
    let get = Lens.get

    /// Modifies the state in a state monad and returns the original value
    let getAndModifyState = Lens.getAndModifyState

    /// Applies a lens in the 'get' direction within a state monad
    let getState = Lens.getState

    /// Identity lens
    let idL = Lens.id

#if FULLY_GLOBAL_LENS
    /// Undocumented
    let ignore = Lens.ignore

    /// Makes a lens that maps the given lens in a list
    let listMap = Lens.listMap

    /// Converts a lens that views a list into a lens that views an array
    let listToArray = Lens.listToArray*)
#endif

    /// Modifies the state in a state monad and returns the modified value.
    let modifyAndGetState = Lens.modifyAndGetState

#if FULLY_GLOBAL_LENS
    /// Pair two lenses
    let pair = Lens.pair

    /// Makes a lens that maps the given lens in a sequence
    let seqMap = Lens.seqMap
#endif

    /// Set a value through a lens
    let set = Lens.set

    /// Set a value through a lens with piping state
    let inline setP a lens b = Lens.set a b lens

    /// Applies a lens in the 'set' direction within a state monad
    let setState = Lens.setState

    /// Gets/sets the snd element in a pair through a lens
    let sndL = Lens.snd

    /// Update a value through a lens
    let update = Lens.update

    /// Update through a lens within a state monad
    let updateState = Lens.updateState

    /// Applies an isomorphism to the value viewed through a lens
    let xmap = Lens.xmap