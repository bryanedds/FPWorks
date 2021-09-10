﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Generic
open System.IO
open System.Numerics
open System.Text
open System.Xml.Linq
open Prime
open TiledSharp

[<RequireQualifiedAccess>]
module TmxMap =

    /// Make a TmxMap from the content of a stream.
    let makeFromStream (stream : Stream) =
        TmxMap stream

    /// Make a TmxMap from the content of a text fragment.
    let makeFromText (text : string) =
        use stream = new MemoryStream (UTF8Encoding.UTF8.GetBytes text)
        makeFromStream stream

    /// Make a TmxMap from the content of a .tmx file.
    let makeFromFilePath (filePath : string) =
        TmxMap filePath

    /// Make a default TmxMap.
    let makeDefault () =
        makeFromText
            """<?xml version="1.0" encoding="UTF-8"?>
            <map version="1.2" tiledversion="1.3.4" orientation="orthogonal" renderorder="right-down" width="8" height="8" tilewidth="48" tileheight="48" infinite="0" nextlayerid="3" nextobjectid="1">
             <tileset firstgid="1" name="TileSet" tilewidth="48" tileheight="48" tilecount="72" columns="8">
              <properties>
               <property name="Image" value="[Default TileSet]"/>
              </properties>
              <image source="TileSet.png" trans="ff00ff" width="384" height="434"/>
              <tile id="0"><properties><property name="C" value=""/></properties></tile>
              <tile id="1"><properties><property name="C" value=""/></properties></tile>
              <tile id="2"><properties><property name="C" value=""/></properties></tile>
              <tile id="8"><properties><property name="C" value=""/></properties></tile>
              <tile id="9"><properties><property name="C" value=""/></properties></tile>
              <tile id="10"><properties><property name="C" value=""/></properties></tile>
              <tile id="11"><properties><property name="C" value=""/></properties></tile>
              <tile id="12"><properties><property name="C" value=""/></properties></tile>
              <tile id="13"><properties><property name="C" value=""/></properties></tile>
              <tile id="16"><properties><property name="C" value=""/></properties></tile>
              <tile id="17"><properties><property name="C" value=""/></properties></tile>
              <tile id="18"><properties><property name="C" value=""/></properties></tile>
              <tile id="19"><properties><property name="C" value=""/></properties></tile>
              <tile id="20"><properties><property name="C" value=""/></properties></tile>
              <tile id="21"><properties><property name="C" value=""/></properties></tile>
             </tileset>
             <layer id="1" name="Layer" width="8" height="8">
              <data encoding="base64" compression="zlib">
               eJyTYWBgkCGApaAYF18LinHxKcW0tB8At+0HYQ==
              </data>
             </layer>
            </map>"""

    /// Make an empty TmxMap.
    let makeEmpty () =
        makeFromText
            """<?xml version="1.0" encoding="UTF-8"?>
            <map version="1.2" tiledversion="1.3.4" orientation="orthogonal" renderorder="right-down" width="1" height="1" tilewidth="48" tileheight="48" infinite="0" nextlayerid="2" nextobjectid="1">
             <layer id="1" name="Layer" width="1" height="1">
              <data encoding="base64" compression="zlib">
               eJxjYGBgAAAABAAB
              </data>
             </layer>
            </map>"""

    /// Make a TmxLayerTile.
    let makeLayerTile gid x y hflip vflip dflip =
        let tid =
            gid |||
            (if hflip then 0x80000000 else 0x0) |||
            (if vflip then 0x40000000 else 0x0) |||
            (if dflip then 0x20000000 else 0x0) |>
            uint
        TmxLayerTile (tid, x, y)

    /// Make a TmxObject.
    let makeObject (id : int) (gid : int) (x : float) (y : float) (width : float) (height : float) =
        let xml = XElement (XName.op_Implicit "object")
        xml.Add (XAttribute (XName.op_Implicit "id", id))
        xml.Add (XAttribute (XName.op_Implicit "gid", gid))
        xml.Add (XAttribute (XName.op_Implicit "x", x))
        xml.Add (XAttribute (XName.op_Implicit "y", y))
        xml.Add (XAttribute (XName.op_Implicit "width", width))
        xml.Add (XAttribute (XName.op_Implicit "height", height))
        TmxObject xml

    let rec importShape shape (tileSize : Vector2) (tileOffset : Vector2) =
        let tileExtent = tileSize * 0.5f
        match shape with
        | BodyEmpty as be -> be
        | BodyBox box -> BodyBox { box with Extent = box.Extent * tileExtent; Center = box.Center * tileSize + tileOffset }
        | BodyCircle circle -> BodyCircle { circle with Radius = circle.Radius * tileExtent.Y; Center = circle.Center * tileSize + tileOffset }
        | BodyCapsule capsule -> BodyCapsule { capsule with Height = tileSize.Y; Radius = capsule.Radius * tileExtent.Y; Center = capsule.Center * tileSize + tileOffset }
        | BodyBoxRounded boxRounded -> BodyBoxRounded { boxRounded with Extent = boxRounded.Extent * tileExtent; Radius = boxRounded.Radius; Center = boxRounded.Center * tileSize + tileOffset }
        | BodyPolygon polygon -> BodyPolygon { polygon with Vertices = Array.map (fun point -> point * tileSize) polygon.Vertices; Center = polygon.Center * tileSize + tileOffset }
        | BodyShapes shapes -> BodyShapes (List.map (fun shape -> importShape shape tileSize tileOffset) shapes)

    let getDescriptor tileMapPosition (tileMap : TmxMap) =
        let tileSizeI = v2i tileMap.TileWidth tileMap.TileHeight
        let tileSizeF = v2 (single tileSizeI.X) (single tileSizeI.Y)
        let tileMapSizeM = v2i tileMap.Width tileMap.Height
        let tileMapSizeI = v2i (tileMapSizeM.X * tileSizeI.X) (tileMapSizeM.Y * tileSizeI.Y)
        let tileMapSizeF = v2 (single tileMapSizeI.X) (single tileMapSizeI.Y)
        { TileMap = tileMap
          TileSizeI = tileSizeI; TileSizeF = tileSizeF
          TileMapSizeM = tileMapSizeM; TileMapSizeI = tileMapSizeI; TileMapSizeF = tileMapSizeF
          TileMapPosition = tileMapPosition }

    let tryGetTileMap (tileMapAsset : TileMap AssetTag) world =
        match World.tryGetTileMapMetadata tileMapAsset world with
        | Some (_, _, tileMap) -> Some tileMap
        | None -> None

    let tryGetTileDescriptor tileIndex (tl : TmxLayer) tmd (tileDescriptor : TileDescriptor outref) =
        let tileMapRun = tmd.TileMapSizeM.X
        let (i, j) = (tileIndex % tileMapRun, tileIndex / tileMapRun)
        let tile = tl.Tiles.[tileIndex]
        if tile.Gid <> 0 then // not the empty tile
            let mutable tileOffset = 1 // gid 0 is the empty tile
            let mutable tileSetIndex = 0
            let mutable tileSetFound = false
            let mutable enr = tmd.TileMap.Tilesets.GetEnumerator ()
            while enr.MoveNext () && not tileSetFound do
                let set = enr.Current
                let tileCountOpt = set.TileCount
                let tileCount = if tileCountOpt.HasValue then tileCountOpt.Value else 0
                if  tile.Gid >= set.FirstGid && tile.Gid < set.FirstGid + tileCount ||
                    not tileCountOpt.HasValue then // HACK: when tile count is missing, assume we've found the tile...?
                    tileSetFound <- true
                else
                    tileSetIndex <- inc tileSetIndex
                    tileOffset <- tileOffset + tileCount
            let tileId = tile.Gid - tileOffset
            let tileSet = tmd.TileMap.Tilesets.[tileSetIndex]
            let tilePositionI =
                v2i
                    (int tmd.TileMapPosition.X + tmd.TileSizeI.X * i)
                    (int tmd.TileMapPosition.Y - tmd.TileSizeI.Y * inc j + tmd.TileMapSizeI.Y) // invert y coords
            let tilePositionF = v2 (single tilePositionI.X) (single tilePositionI.Y)
            let tileSetTileOpt =
                match tileSet.Tiles.TryGetValue tileId with
                | (true, tileSetTile) -> Some tileSetTile
                | (false, _) -> None
            tileDescriptor.TileXXX <- tile
            tileDescriptor.TileI <- i
            tileDescriptor.TileJ <- j
            tileDescriptor.TilePositionI <- tilePositionI
            tileDescriptor.TilePositionF <- tilePositionF
            tileDescriptor.TileSetTileOpt <- tileSetTileOpt
            true
        else false

    let tryGetTileAnimationDescriptor tileIndex tileLayer tileMapDescriptor =
        let mutable tileDescriptor = Unchecked.defaultof<_>
        if tryGetTileDescriptor tileIndex tileLayer tileMapDescriptor &tileDescriptor then
            match tileDescriptor.TileSetTileOpt with
            | Some tileSetTile ->
                match tileSetTile.Properties.TryGetValue Constants.TileMap.AnimationPropertyName with
                | (true, tileAnimationStr) ->
                    try ValueSome (scvaluem<TileAnimationDescriptor> tileAnimationStr)
                    with _ -> ValueNone
                | (false, _) -> ValueNone
            | None -> ValueNone
        else ValueNone

    let getTileLayerBodyShapes (tileLayer : TmxLayer) tileMapDescriptor =
        
        // construct a list of body shapes
        let bodyShapes = List<BodyShape> ()
        let tileBoxes = dictPlus<single, Vector4 List> HashIdentity.Structural []
        for i in 0 .. dec tileLayer.Tiles.Count do

            // construct a dictionary of tile boxes, adding non boxes to the result list
            let mutable tileDescriptor = Unchecked.defaultof<_>
            if tryGetTileDescriptor i tileLayer tileMapDescriptor &tileDescriptor then
                match tileDescriptor.TileSetTileOpt with
                | Some tileSetTile ->
                    match tileSetTile.Properties.TryGetValue Constants.TileMap.CollisionPropertyName with
                    | (true, cexpr) ->
                        let tileCenter =
                            v2
                                (tileDescriptor.TilePositionF.X + tileMapDescriptor.TileSizeF.X * 0.5f)
                                (tileDescriptor.TilePositionF.Y + tileMapDescriptor.TileSizeF.Y * 0.5f)
                        match cexpr with
                        | "" ->
                            match tileBoxes.TryGetValue tileCenter.Y with
                            | (true, l) ->
                                l.Add (v4Bounds (tileCenter - tileMapDescriptor.TileSizeF * 0.5f) tileMapDescriptor.TileSizeF)
                            | (false, _) ->
                                tileBoxes.Add (tileCenter.Y, List [v4Bounds (tileCenter - tileMapDescriptor.TileSizeF * 0.5f) tileMapDescriptor.TileSizeF])
                        | _ ->
                            let tileShape = scvalue<BodyShape> cexpr
                            let tileShapeImported = importShape tileShape tileMapDescriptor.TileSizeF tileCenter
                            bodyShapes.Add tileShapeImported
                    | (false, _) -> ()
                | None -> ()
            else ()

        // combine adjacent tiles on the same row into strips
        let strips = List ()
        for boxes in tileBoxes.Values do
            let mutable box = boxes.[0]
            let epsilon = box.Size.X * 0.001f
            for i in 1 .. dec boxes.Count do
                let box2 = boxes.[i]
                let distance = abs (box2.Left.X - box.Right.X)
                if distance < epsilon then
                    box <- box.WithSize (v2 (box.Size.X + box2.Size.X) box.Size.Y)
                else
                    strips.Add box
                    box <- box2
                if i = dec boxes.Count then
                    strips.Add box

        // convert strips into BodyShapes and add to the resulting list
        for strip in strips do
            strip |> BodyBox.fromBounds |> BodyBox |> bodyShapes.Add

        // fin
        bodyShapes

    let getBodyShapes tileMapDescriptor =
        tileMapDescriptor.TileMap.Layers |>
        Seq.fold (fun shapess tileLayer ->
            let shapes = getTileLayerBodyShapes tileLayer tileMapDescriptor
            shapes :: shapess)
            [] |>
        Seq.concat |>
        Seq.toList

    let getBodyProperties enabled friction restitution collisionCategories collisionMask bodyId tileMapDescriptor =
        let bodyProperties =
            { BodyId = bodyId
              Position = v2Zero
              Rotation = 0.0f
              BodyShape = BodyShapes (getBodyShapes tileMapDescriptor)
              BodyType = BodyType.Static
              Awake = false
              Enabled = enabled
              Density = Constants.Physics.DensityDefault
              Friction = friction
              Restitution = restitution
              LinearVelocity = v2Zero
              LinearDamping = 0.0f
              AngularVelocity = 0.0f
              AngularDamping = 0.0f
              FixedRotation = true
              Inertia = 0.0f
              GravityScale = 0.0f
              CollisionCategories = Physics.categorizeCollisionMask collisionCategories
              CollisionMask = Physics.categorizeCollisionMask collisionMask
              IgnoreCCD = false
              IsBullet = false
              IsSensor = false }
        bodyProperties

    /// TODO: remove as much allocation from this as possible! See related issue, https://github.com/bryanedds/Nu/issues/324 .
    let getLayeredMessages time absolute (viewBounds : Vector4) (tileMapPosition : Vector2) tileMapElevation tileMapColor tileMapGlow tileMapParallax tileLayerClearance tileIndexOffset (tileMap : TmxMap) =
        let layers = List.ofSeq tileMap.Layers
        let tileSourceSize = v2i tileMap.TileWidth tileMap.TileHeight
        let tileSize = v2 (single tileMap.TileWidth) (single tileMap.TileHeight)
        let tileAssets = tileMap.ImageAssets
        let tileGidCount = Array.fold (fun count (tileSet : TmxTileset, _) -> let count2 = tileSet.TileCount in count + count2.GetValueOrDefault 0) 0 tileAssets // TODO: make this a public function!
        let tileMapDescriptor = getDescriptor tileMapPosition tileMap
        let descriptorLists =
            List.foldi
                (fun i descriptorLists (layer : TmxLayer) ->

                    // compute elevation value
                    let elevationOffset =
                        match layer.Properties.TryGetValue Constants.TileMap.ElevationPropertyName with
                        | (true, elevation) -> scvalue elevation
                        | (false, _) -> single i * tileLayerClearance
                    let elevation = tileMapElevation + elevationOffset

                    // compute parallax position
                    let parallaxPosition =
                        if absolute
                        then tileMapPosition
                        else tileMapPosition + tileMapParallax * elevation * -viewBounds.Center

                    // compute positions relative to tile map
                    let (r, r2) =
                        if absolute then
                            let r = v2Zero
                            let r2 = viewBounds.Size
                            (r, r2)
                        else
                            let r = viewBounds.Position - parallaxPosition
                            let r2 = r + viewBounds.Size
                            (r, r2)

                    // accumulate decriptors
                    let descriptors = List ()
                    let mutable yC = 0
                    let mutable yO = r.Y + single yC * tileSize.Y
                    while r.Y + single yC * tileSize.Y < r2.Y + tileSize.Y do

                        // compute y index and ensure it's in bounds
                        let yI = tileMap.Height - 1 - int (yO / tileSize.Y)
                        if yO >= 0.0f && yI >= 0 && yI < tileMap.Height then

                            // accumulate strip tiles
                            let tiles = List ()
                            let mutable xS = 0.0f
                            let mutable xO = r.X
                            while xO < r2.X + tileSize.X do
                                let xI = int (xO / tileSize.X)
                                if xO >= 0.0f && xI >= 0 then
                                    if xI < tileMap.Width then
                                        let xTileIndex = xI + yI * tileMap.Width
                                        let xTile = layer.Tiles.[xTileIndex]
                                        let xTileGid =
                                            if xTile.Gid <> 0 then // never offset the zero tile!
                                                let xTileGidOffset = xTile.Gid + tileIndexOffset
                                                if xTileGidOffset > 0 && xTileGidOffset < tileGidCount then xTileGidOffset
                                                else xTile.Gid
                                            else xTile.Gid
                                        let xTile =
                                            match tryGetTileAnimationDescriptor xTileIndex layer tileMapDescriptor with
                                            | ValueSome xTileAnimationDescriptor ->
                                                let compressedTime = time / xTileAnimationDescriptor.TileAnimationDelay
                                                let xTileOffset = int compressedTime % xTileAnimationDescriptor.TileAnimationRun
                                                makeLayerTile (xTileGid + xTileOffset) xTile.X xTile.Y xTile.HorizontalFlip xTile.VerticalFlip xTile.DiagonalFlip
                                            | ValueNone ->
                                                makeLayerTile xTileGid xTile.X xTile.Y xTile.HorizontalFlip xTile.VerticalFlip xTile.DiagonalFlip
                                        tiles.Add xTile
                                else xS <- xS + tileSize.X
                                xO <- xO + tileSize.X

                            // compute strip transform
                            let transform =
                                { Position = v2 (xS - modulus r.X tileSize.X) (single yC * tileSize.Y - modulus r.Y tileSize.Y) + viewBounds.Position
                                  Size = v2 (single tiles.Count * tileSize.X) tileSize.Y
                                  Rotation = 0.0f
                                  Elevation = elevation
                                  Flags = 0 }

                            // check if in view bounds
                            if Math.isBoundsIntersectingBounds (v4Bounds transform.Position transform.Size) viewBounds then

                                // accumulate descriptor
                                descriptors.Add
                                    { Elevation = transform.Elevation
                                      PositionY = transform.Position.Y
                                      AssetTag = AssetTag.make "" "" // just disregard asset for render ordering
                                      RenderDescriptor =
                                        TileLayerDescriptor
                                            { Transform = transform
                                              Absolute = absolute
                                              Color = tileMapColor
                                              Glow = tileMapGlow
                                              MapSize = Vector2i (tileMap.Width, tileMap.Height)
                                              Tiles = Seq.toArray tiles
                                              TileSourceSize = tileSourceSize
                                              TileSize = tileSize
                                              TileAssets = tileAssets }}

                        // loop
                        yC <- inc yC
                        yO <- r.Y + single yC * tileSize.Y
                                    
                    Seq.toList descriptors :: descriptorLists)
                [] layers
        List.concat descriptorLists

    let getQuickSize (tileMap : TmxMap) =
        v2
            (single (tileMap.Width * tileMap.TileWidth))
            (single (tileMap.Height * tileMap.TileHeight))