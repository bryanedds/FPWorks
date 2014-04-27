﻿namespace Nu
open System
open OpenTK
open Prime
open TiledSharp
open Nu
open Nu.NuCore
open Nu.NuConstants
open Nu.NuMath
open Nu.Physics
open Nu.Metadata
open Nu.Entity
open Nu.WorldPrims

[<AutoOpen>]
module DispatchersModule =

    type Entity with
        
        (* button xfields *)
        member this.IsDown with get () = this?IsDown () : bool
        member this.SetIsDown (value : bool) : Entity = this?IsDown <- value
        member this.UpSprite with get () = this?UpSprite () : Sprite
        member this.SetUpSprite (value : Sprite) : Entity = this?UpSprite <- value
        member this.DownSprite with get () = this?DownSprite () : Sprite
        member this.SetDownSprite (value : Sprite) : Entity = this?DownSprite <- value
        member this.ClickSound with get () = this?ClickSound () : Sound
        member this.SetClickSound (value : Sound) : Entity = this?ClickSound <- value

        (* label xfields *)
        member this.LabelSprite with get () = this?LabelSprite () : Sprite
        member this.SetLabelSprite (value : Sprite) : Entity = this?LabelSprite <- value

        (* text box xfields *)
        member this.BoxSprite with get () = this?BoxSprite () : Sprite
        member this.SetBoxSprite (value : Sprite) : Entity = this?BoxSprite <- value
        member this.Text with get () = this?Text () : string
        member this.SetText (value : string) : Entity = this?Text <- value
        member this.TextFont with get () = this?TextFont () : Font
        member this.SetTextFont (value : Font) : Entity = this?TextFont <- value
        member this.TextOffset with get () = this?TextOffset () : Vector2
        member this.SetTextOffset (value : Vector2) : Entity = this?TextOffset <- value
        member this.TextColor with get () = this?TextColor () : Vector4
        member this.SetTextColor (value : Vector4) : Entity = this?TextColor <- value

        (* toggle xfields *)
        member this.IsOn with get () = this?IsOn () : bool
        member this.SetIsOn (value : bool) : Entity = this?IsOn <- value
        member this.IsPressed with get () = this?IsPressed () : bool
        member this.SetIsPressed (value : bool) : Entity = this?IsPressed <- value
        member this.OffSprite with get () = this?OffSprite () : Sprite
        member this.SetOffSprite (value : Sprite) : Entity = this?OffSprite <- value
        member this.OnSprite with get () = this?OnSprite () : Sprite
        member this.SetOnSprite (value : Sprite) : Entity = this?OnSprite <- value
        member this.ToggleSound with get () = this?ToggleSound () : Sound
        member this.SetToggleSound (value : Sound) : Entity = this?ToggleSound <- value

        (* feeler xfields *)
        member this.IsTouched with get () = this?IsTouched () : bool
        member this.SetIsTouched (value : bool) : Entity = this?IsTouched <- value

        (* fill bar xfields *)
        member this.Fill with get () = this?Fill () : single
        member this.SetFill (value : single) : Entity = this?Fill <- value
        member this.FillInset with get () = this?FillInset () : single
        member this.SetFillInset (value : single) : Entity = this?FillInset <- value
        member this.FillSprite with get () = this?FillSprite () : Sprite
        member this.SetFillSprite (value : Sprite) : Entity = this?FillSprite <- value
        member this.BorderSprite with get () = this?BorderSprite () : Sprite
        member this.SetBorderSprite (value : Sprite) : Entity = this?BorderSprite <- value

        (* block xfields *)
        member this.PhysicsId with get () = this?PhysicsId () : PhysicsId
        member this.SetPhysicsId (value : PhysicsId) : Entity = this?PhysicsId <- value
        member this.Density with get () = this?Density () : single
        member this.SetDensity (value : single) : Entity = this?Density <- value
        member this.BodyType with get () = this?BodyType () : BodyType
        member this.SetBodyType (value : BodyType) : Entity = this?BodyType <- value
        member this.ImageSprite with get () = this?ImageSprite () : Sprite
        member this.SetImageSprite (value : Sprite) : Entity = this?ImageSprite <- value

        (* avatar xfields *)
        // uses same xfields as block

        (* tile map xfields *)
        member this.PhysicsIds with get () = this?PhysicsIds () : PhysicsId list
        member this.SetPhysicsIds (value : PhysicsId list) : Entity = this?PhysicsIds <- value
        member this.TileMapAsset with get () = this?TileMapAsset () : TileMapAsset
        member this.SetTileMapAsset (value : TileMapAsset) : Entity = this?TileMapAsset <- value

    type ButtonDispatcher () =
        inherit Entity2dDispatcher ()

        let handleButtonEventDownMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData (mousePosition, _) ->
                let button = get world <| worldEntityLens subscriber
                if button.Enabled && button.Visible then
                    if isInBox3 mousePosition button.Position button.Size then
                        let button' = button.SetIsDown true
                        let world' = set button' world <| worldEntityLens subscriber
                        let (keepRunning, world'') = publish (straddr "Down" subscriber) subscriber { Handled = false; Data = NoData } world'
                        (handleMessage message, keepRunning, world'')
                    else (message, true, world)
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")

        let handleButtonEventUpMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData (mousePosition, _) ->
                let button = get world <| worldEntityLens subscriber
                if button.Enabled && button.Visible then
                    let (keepRunning, world') =
                        let button' = button.SetIsDown false
                        let world'' = set button' world <| worldEntityLens subscriber
                        publish (straddr "Up" subscriber) subscriber { Handled = false; Data = NoData } world''
                    if keepRunning && isInBox3 mousePosition button.Position button.Size && button.IsDown then
                        let (keepRunning', world'') = publish (straddr "Click" subscriber) subscriber { Handled = false; Data = NoData } world'
                        let sound = PlaySound { Volume = 1.0f; Sound = button.ClickSound }
                        let world'3 = { world'' with AudioMessages = sound :: world''.AudioMessages }
                        (handleMessage message, keepRunning', world'3)
                    else (message, keepRunning, world')
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")

        override this.Init (button, dispatcherContainer) =
            let button' = base.Init (button, dispatcherContainer)
            button'
                .SetIsTransformRelative(false)
                .SetIsDown(false)
                .SetUpSprite({ SpriteAssetName = Lun.make "Image"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetDownSprite({ SpriteAssetName = Lun.make "Image2"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetClickSound({ SoundAssetName = Lun.make "Sound"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.Register (address, button, world) =
            let world' =
                world |>
                    subscribe DownMouseLeftEvent address (CustomSub handleButtonEventDownMouseLeft) |>
                    subscribe UpMouseLeftEvent address (CustomSub handleButtonEventUpMouseLeft)
            (button, world')

        override this.Unregister (address, button, world) =
            world |>
                unsubscribe DownMouseLeftEvent address |>
                unsubscribe UpMouseLeftEvent address

        override this.GetRenderDescriptors (view, button, world) =
            if not button.Visible then []
            else
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = button.Position
                              Size = button.Size
                              Rotation = 0.0f
                              Sprite = if button.IsDown then button.DownSprite else button.UpSprite
                              Color = Vector4.One }
                          Depth = button.Depth }]

        override this.GetQuickSize (button, world) =
            let sprite = button.UpSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type LabelDispatcher () =
        inherit Entity2dDispatcher ()
            
        override this.Init (label, dispatcherContainer) =
            let label' = base.Init (label, dispatcherContainer)
            label'
                .SetIsTransformRelative(false)
                .SetLabelSprite({ SpriteAssetName = Lun.make "Image4"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.GetRenderDescriptors (view, label, world) =
            if not label.Visible then []
            else
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = label.Position
                              Size = label.Size
                              Rotation = 0.0f
                              Sprite = label.LabelSprite
                              Color = Vector4.One }
                          Depth = label.Depth }]

        override this.GetQuickSize (label, world) =
            let sprite = label.LabelSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type TextBoxDispatcher () =
        inherit Entity2dDispatcher ()
            
        override this.Init (textBox, dispatcherContainer) =
            let textBox' = base.Init (textBox, dispatcherContainer)
            textBox'
                .SetIsTransformRelative(false)
                .SetBoxSprite({ SpriteAssetName = Lun.make "Image4"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetText(String.Empty)
                .SetTextFont({ FontAssetName = Lun.make "Font"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetTextOffset(Vector2.Zero)
                .SetTextColor(Vector4.One)

        override this.GetRenderDescriptors (view, textBox, world) =
            if not textBox.Visible then []
            else
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = textBox.Position
                              Size = textBox.Size
                              Rotation = 0.0f
                              Sprite = textBox.BoxSprite
                              Color = Vector4.One }
                          Depth = textBox.Depth }
                    LayerableDescriptor <|
                    LayeredTextDescriptor
                        { Descriptor =
                            { Text = textBox.Text
                              Position = textBox.Position + textBox.TextOffset
                              Size = textBox.Size - textBox.TextOffset
                              Font = textBox.TextFont
                              Color = textBox.TextColor }
                          Depth = textBox.Depth }]

        override this.GetQuickSize (textBox, world) =
            let sprite = textBox.BoxSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type ToggleDispatcher () =
        inherit Entity2dDispatcher ()

        let handleToggleEventDownMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData (mousePosition, _) ->
                let toggle = get world <| worldEntityLens subscriber
                if toggle.Enabled && toggle.Visible then
                    if isInBox3 mousePosition toggle.Position toggle.Size then
                        let toggle' = toggle.SetIsPressed true
                        let world' = set toggle' world <| worldEntityLens subscriber
                        (handleMessage message, true, world')
                    else (message, true, world)
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")
    
        let handleToggleEventUpMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData (mousePosition, _) ->
                let toggle = get world <| worldEntityLens subscriber
                if toggle.Enabled && toggle.Visible && toggle.IsPressed then
                    let toggle' = toggle.SetIsPressed false
                    if isInBox3 mousePosition toggle'.Position toggle'.Size then
                        let toggle'' = toggle'.SetIsOn <| not toggle'.IsOn
                        let world' = set toggle'' world <| worldEntityLens subscriber
                        let messageType = if toggle''.IsOn then "On" else "Off"
                        let (keepRunning, world'') = publish (straddr messageType subscriber) subscriber { Handled = false; Data = NoData } world'
                        let sound = PlaySound { Volume = 1.0f; Sound = toggle''.ToggleSound }
                        let world'3 = { world'' with AudioMessages = sound :: world''.AudioMessages }
                        (handleMessage message, keepRunning, world'3)
                    else
                        let world' = set toggle' world <| worldEntityLens subscriber
                        (message, true, world')
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")
        
        override this.Init (toggle, dispatcherContainer) =
            let toggle' = base.Init (toggle, dispatcherContainer)
            toggle'
                .SetIsTransformRelative(false)
                .SetIsOn(false)
                .SetIsPressed(false)
                .SetOffSprite({ SpriteAssetName = Lun.make "Image"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetOnSprite({ SpriteAssetName = Lun.make "Image2"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetToggleSound({ SoundAssetName = Lun.make "Sound"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.Register (address, label, world) =
            let world' =
                world |>
                    subscribe DownMouseLeftEvent address (CustomSub handleToggleEventDownMouseLeft) |>
                    subscribe UpMouseLeftEvent address (CustomSub handleToggleEventUpMouseLeft)
            (label, world')

        override this.Unregister (address, label, world) =
            world |>
                unsubscribe DownMouseLeftEvent address |>
                unsubscribe UpMouseLeftEvent address

        override this.GetRenderDescriptors (view, toggle, world) =
            if not toggle.Visible then []
            else
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = toggle.Position
                              Size = toggle.Size
                              Rotation = 0.0f
                              Sprite = if toggle.IsOn || toggle.IsPressed then toggle.OnSprite else toggle.OffSprite
                              Color = Vector4.One }
                          Depth = toggle.Depth }]

        override this.GetQuickSize (toggle, world) =
            let sprite = toggle.OffSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type FeelerDispatcher () =
        inherit Entity2dDispatcher ()

        let handleFeelerEventDownMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData (mousePosition, _) as mouseButtonData ->
                let feeler = get world <| worldEntityLens subscriber
                if feeler.Enabled && feeler.Visible then
                    if isInBox3 mousePosition feeler.Position feeler.Size then
                        let feeler' = feeler.SetIsTouched true
                        let world' = set feeler' world <| worldEntityLens subscriber
                        let (keepRunning, world'') = publish (straddr "Touch" subscriber) subscriber { Handled = false; Data = mouseButtonData } world'
                        (handleMessage message, keepRunning, world'')
                    else (message, true, world)
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")
    
        let handleFeelerEventUpMouseLeft event publisher subscriber message world =
            match message.Data with
            | MouseButtonData _ ->
                let feeler = get world <| worldEntityLens subscriber
                if feeler.Enabled && feeler.Visible then
                    let feeler' = feeler.SetIsTouched false
                    let world' = set feeler' world <| worldEntityLens subscriber
                    let (keepRunning, world'') = publish (straddr "Release" subscriber) subscriber { Handled = false; Data = NoData } world'
                    (handleMessage message, keepRunning, world'')
                else (message, true, world)
            | _ -> failwith ("Expected MouseButtonData from event '" + addrToStr event + "'.")
        
        override this.Init (feeler, dispatcherContainer) =
            let feeler' = base.Init (feeler, dispatcherContainer)
            feeler'
                .SetIsTransformRelative(false)
                .SetIsTouched(false)

        override this.Register (address, feeler, world) =
            let world' =
                world |>
                    subscribe DownMouseLeftEvent address (CustomSub handleFeelerEventDownMouseLeft) |>
                    subscribe UpMouseLeftEvent address (CustomSub handleFeelerEventUpMouseLeft)
            (feeler, world')

        override this.Unregister (address, feeler, world) =
            world |>
                unsubscribe UpMouseLeftEvent address |>
                unsubscribe DownMouseLeftEvent address

        override this.GetQuickSize (feeler, world) =
            Vector2 64.0f

    type FillBarDispatcher () =
        inherit Entity2dDispatcher ()

        let getFillBarSpriteDims (fillBar : Entity) =
            let spriteInset = fillBar.Size * fillBar.FillInset * 0.5f
            let spritePosition = fillBar.Position + spriteInset
            let spriteWidth = (fillBar.Size.X - spriteInset.X * 2.0f) * fillBar.Fill
            let spriteHeight = fillBar.Size.Y - spriteInset.Y * 2.0f
            (spritePosition, Vector2 (spriteWidth, spriteHeight))

        override this.Init (fillBar, dispatcherContainer) =
            let fillBar' = base.Init (fillBar, dispatcherContainer)
            fillBar'
                .SetIsTransformRelative(false)
                .SetFill(0.0f)
                .SetFillInset(0.0f)
                .SetFillSprite({ SpriteAssetName = Lun.make "Image9"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })
                .SetBorderSprite({ SpriteAssetName = Lun.make "Image10"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.GetRenderDescriptors (view, fillBar, world) =
            if not fillBar.Visible then []
            else
                let (fillBarSpritePosition, fillBarSpriteSize) = getFillBarSpriteDims fillBar
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = fillBarSpritePosition
                              Size = fillBarSpriteSize
                              Rotation = 0.0f
                              Sprite = fillBar.FillSprite
                              Color = Vector4.One }
                          Depth = fillBar.Depth }
                    LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = fillBar.Position
                              Size = fillBar.Size
                              Rotation = 0.0f
                              Sprite = fillBar.BorderSprite
                              Color = Vector4.One }
                          Depth = fillBar.Depth }]

        override this.GetQuickSize (fillBar, world) =
            let sprite = fillBar.BorderSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type BlockDispatcher () =
        inherit Entity2dDispatcher ()

        let registerBlockPhysics address (block : Entity) world =
            let block' = block.SetPhysicsId <| getPhysicsId block.Id
            let bodyCreateMessage =
                BodyCreateMessage
                    { EntityAddress = address
                      PhysicsId = block'.PhysicsId
                      Shape = BoxShape
                        { Extent = block'.Size * 0.5f
                          Properties =
                            { Center = Vector2.Zero
                              Restitution = 0.0f
                              FixedRotation = false
                              LinearDamping = 5.0f
                              AngularDamping = 5.0f }}
                      Position = block'.Position + block'.Size * 0.5f
                      Rotation = block'.Rotation
                      Density = block'.Density
                      BodyType = block'.BodyType }
            let world' = { world with PhysicsMessages = bodyCreateMessage :: world.PhysicsMessages }
            (block', world')

        let unregisterBlockPhysics (_ : Address) (block : Entity) world =
            let bodyDestroyMessage = BodyDestroyMessage { PhysicsId = block.PhysicsId }
            { world with PhysicsMessages = bodyDestroyMessage :: world.PhysicsMessages }

        override this.Init (block, dispatcherContainer) =
            let block' = base.Init (block, dispatcherContainer)
            block'
                .SetIsTransformRelative(true)
                .SetPhysicsId(InvalidPhysicsId)
                .SetDensity(NormalDensity)
                .SetBodyType(BodyType.Dynamic)
                .SetImageSprite({ SpriteAssetName = Lun.make "Image3"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.Register (address, block, world) =
            registerBlockPhysics address block world

        override this.Unregister (address, block, world) =
            unregisterBlockPhysics address block world
            
        override this.PropagatePhysics (address, block, world) =
            let (block', world') = world |> unregisterBlockPhysics address block |> registerBlockPhysics address block
            set block' world' <| worldEntityLens address

        override this.ReregisterPhysicsHack (groupAddress, block, world) =
            let address = addrstr groupAddress block.Name
            let world' = unregisterBlockPhysics address block world
            let (block', world'') = registerBlockPhysics address block world'
            set block' world'' <| worldEntityLens address

        override this.HandleBodyTransformMessage (message, address, block, world) =
            let block' =
                block
                    .SetPosition(message.Position - block.Size * 0.5f) // TODO: see if this center-offsetting can be encapsulated within the Physics module!
                    .SetRotation(message.Rotation)
            set block' world <| worldEntityLens message.EntityAddress
            
        override this.GetRenderDescriptors (view, block, world) =
            if not block.Visible then []
            else
                let spritePosition = block.Position * view
                let spriteSize = block.Size * Matrix3.getScaleMatrix view
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = spritePosition
                              Size = spriteSize
                              Rotation = block.Rotation
                              Sprite = block.ImageSprite
                              Color = Vector4.One }
                          Depth = block.Depth }]

        override this.GetQuickSize (block, world) =
            let sprite = block.ImageSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size
    
    type AvatarDispatcher () =
        inherit Entity2dDispatcher ()

        let registerAvatarPhysics address (avatar : Entity) world =
            let avatar' = avatar.SetPhysicsId <| getPhysicsId avatar.Id
            let bodyCreateMessage =
                BodyCreateMessage
                    { EntityAddress = address
                      PhysicsId = avatar'.PhysicsId
                      Shape =
                        CircleShape
                            { Radius = avatar'.Size.X * 0.5f
                              Properties =
                                { Center = Vector2.Zero
                                  Restitution = 0.0f
                                  FixedRotation = true
                                  LinearDamping = 10.0f
                                  AngularDamping = 0.0f }}
                      Position = avatar'.Position + avatar'.Size * 0.5f
                      Rotation = avatar'.Rotation
                      Density = avatar'.Density
                      BodyType = BodyType.Dynamic }
            let world' = { world with PhysicsMessages = bodyCreateMessage :: world.PhysicsMessages }
            (avatar', world')

        let unregisterAvatarPhysics (_ : Address) (avatar : Entity) world =
            let bodyDestroyMessage = BodyDestroyMessage { PhysicsId = avatar.PhysicsId }
            { world with PhysicsMessages = bodyDestroyMessage :: world.PhysicsMessages }

        override this.Init (avatar, dispatcherContainer) =
            let avatar' = base.Init (avatar, dispatcherContainer)
            avatar'
                .SetIsTransformRelative(true)
                .SetPhysicsId(InvalidPhysicsId)
                .SetDensity(NormalDensity)
                .SetImageSprite({ SpriteAssetName = Lun.make "Image7"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.Register (address, avatar, world) =
            registerAvatarPhysics address avatar world

        override this.Unregister (address, avatar, world) =
            unregisterAvatarPhysics address avatar world
            
        override this.PropagatePhysics (address, avatar, world) =
            let (avatar', world') = world |> unregisterAvatarPhysics address avatar |> registerAvatarPhysics address avatar
            set avatar' world' <| worldEntityLens address

        override this.ReregisterPhysicsHack (groupAddress, avatar, world) =
            let address = addrstr groupAddress avatar.Name
            let world' = unregisterAvatarPhysics address avatar world
            let (avatar', world'') = registerAvatarPhysics address avatar world'
            set avatar' world'' <| worldEntityLens address

        override this.HandleBodyTransformMessage (message, address, avatar, world) =
            let avatar' =
                (avatar
                    .SetPosition <| message.Position - avatar.Size * 0.5f) // TODO: see if this center-offsetting can be encapsulated within the Physics module!
                    .SetRotation message.Rotation
            set avatar' world <| worldEntityLens message.EntityAddress

        override this.GetRenderDescriptors (view, avatar, world) =
            if not avatar.Visible then []
            else
                let spritePosition = avatar.Position * view
                let spriteSize = avatar.Size * Matrix3.getScaleMatrix view
                [LayerableDescriptor <|
                    LayeredSpriteDescriptor
                        { Descriptor =
                            { Position = spritePosition
                              Size = spriteSize
                              Rotation = avatar.Rotation
                              Sprite = avatar.ImageSprite
                              Color = Vector4.One }
                          Depth = avatar.Depth }]

        override this.GetQuickSize (avatar, world) =
            let sprite = avatar.ImageSprite
            match tryGetTextureSizeAsVector2 sprite.SpriteAssetName sprite.PackageName world.AssetMetadataMap with
            | None -> DefaultEntitySize
            | Some size -> size

    type TileMapDispatcher () =
        inherit Entity2dDispatcher ()

        let registerTilePhysics tileMap tmd tld address n (world, physicsIds) (_ : TmxLayerTile) =
            let td = makeTileData tileMap tmd tld n
            match td.OptTileSetTile with
            | None -> (world, physicsIds)
            | Some tileSetTile when not <| tileSetTile.Properties.ContainsKey "c" -> (world, physicsIds)
            | Some tileSetTile ->
                let physicsId = getPhysicsId tileMap.Id
                let boxShapeProperties =
                    { Center = Vector2.Zero
                      Restitution = 0.0f
                      FixedRotation = true
                      LinearDamping = 0.0f
                      AngularDamping = 0.0f }
                let bodyCreateMessage =
                    BodyCreateMessage
                        { EntityAddress = address
                          PhysicsId = physicsId
                          Shape = BoxShape { Extent = Vector2 (single <| fst tmd.TileSize, single <| snd tmd.TileSize) * 0.5f; Properties = boxShapeProperties }
                          Position = Vector2 (single <| fst td.TilePosition + fst tmd.TileSize / 2, single <| snd td.TilePosition + snd tmd.TileSize / 2)
                          Rotation = tileMap.Rotation
                          Density = tileMap.Density
                          BodyType = BodyType.Static }
                let world' = { world with PhysicsMessages = bodyCreateMessage :: world.PhysicsMessages }
                (world', physicsId :: physicsIds)

        let registerTileMapPhysics address (tileMap : Entity) world =
            let collisionLayer = 0 // MAGIC_VALUE: assumption
            let tmd = makeTileMapData tileMap.TileMapAsset world
            let tld = makeTileLayerData tileMap tmd collisionLayer
            let (world', physicsIds) = Seq.foldi (registerTilePhysics tileMap tmd tld address) (world, []) tld.Tiles
            let tileMap' = tileMap.SetPhysicsIds physicsIds
            (tileMap', world')

        let unregisterTilePhysics world physicsId =
            let bodyDestroyMessage = BodyDestroyMessage { PhysicsId = physicsId }
            { world with PhysicsMessages = bodyDestroyMessage :: world.PhysicsMessages }

        let unregisterTileMapPhysics (_ : Address) (tileMap : Entity) world =
            List.fold unregisterTilePhysics world <| tileMap.PhysicsIds
        
        override this.Init (tileMap, dispatcherContainer) =
            let tileMap' = base.Init (tileMap, dispatcherContainer)
            tileMap'
                .SetIsTransformRelative(true)
                .SetPhysicsIds([])
                .SetDensity(NormalDensity)
                .SetTileMapAsset({ TileMapAssetName = Lun.make "TileMap"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" })

        override this.Register (address, tileMap, world) =
            registerTileMapPhysics address tileMap world

        override this.Unregister (address, tileMap, world) =
            unregisterTileMapPhysics address tileMap world
            
        override this.PropagatePhysics (address, tileMap, world) =
            let (tileMap', world') = world |> unregisterTileMapPhysics address tileMap |> registerTileMapPhysics address tileMap
            set tileMap' world' <| worldEntityLens address

        override this.ReregisterPhysicsHack (groupAddress, tileMap, world) =
            let address = addrstr groupAddress tileMap.Name
            let world' = unregisterTileMapPhysics address tileMap world
            let (tileMap', world'') = registerTileMapPhysics address tileMap world'
            set tileMap' world'' <| worldEntityLens address

        override this.GetRenderDescriptors (view, tileMap, world) =
            if not tileMap.Visible then []
            else
                let tileMapAsset = tileMap.TileMapAsset
                match tryGetTileMapMetadata tileMapAsset.TileMapAssetName tileMapAsset.PackageName world.AssetMetadataMap with
                | None -> []
                | Some (_, sprites, map) ->
                    let layers = List.ofSeq map.Layers
                    let viewScale = Matrix3.getScaleMatrix view
                    let tileSourceSize = Vector2 (single map.TileWidth, single map.TileHeight)
                    let tileSize = tileSourceSize * viewScale
                    List.mapi
                        (fun i (layer : TmxLayer) ->
                            let layeredTileLayerDescriptor =
                                LayeredTileLayerDescriptor
                                    { Descriptor =
                                        { Position = tileMap.Position * view
                                          Size = Vector2.Zero
                                          Rotation = tileMap.Rotation
                                          MapSize = Vector2 (single map.Width, single map.Height)
                                          Tiles = layer.Tiles
                                          TileSourceSize = tileSourceSize
                                          TileSize = tileSize
                                          TileSet = map.Tilesets.[0] // MAGIC_VALUE: I have no idea how to tell which tile set each tile is from...
                                          TileSetSprite = List.head sprites } // MAGIC_VALUE: for same reason as above
                                      Depth = tileMap.Depth + single i * 2.0f } // MAGIC_VALUE: assumption
                            LayerableDescriptor layeredTileLayerDescriptor)
                        layers

        override this.GetQuickSize (tileMap, world) =
            let tileMapAsset = tileMap.TileMapAsset
            match tryGetTileMapMetadata tileMapAsset.TileMapAssetName tileMapAsset.PackageName world.AssetMetadataMap with
            | None -> failwith "Unexpected match failure in Nu.World.TileMapDispatcher.GetQuickSize."
            | Some (_, _, map) -> Vector2 (single <| map.Width * map.TileWidth, single <| map.Height * map.TileHeight)