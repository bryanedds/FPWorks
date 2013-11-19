﻿namespace Nu
open System
open SDL2
open OpenTK
open TiledSharp
open Nu.Core
open Nu.Constants
open Nu.Sdl
open Nu.Audio
open Nu.Rendering
open Nu.Physics
open Nu.Metadata
open Nu.Entities
open Nu.Groups
open Nu.Screens
open Nu.Games
open Nu.Sim
module OmniBlade =

    let SplashAddress = [Lun.make "splash"]
    let TitleAddress = [Lun.make "title"]
    let TitleGroupAddress = TitleAddress @ [Lun.make "group"]
    let ClickTitleExitAddress = Lun.make "click" :: TitleGroupAddress @ [Lun.make "exit"]
    let LoadAddress = [Lun.make "load"]
    let OmniAddress = [Lun.make "omni"]
    let CreditsAddress = [Lun.make "credits"]

    let createTitleScreen world =
        let titleScreenModel = Screen <| makeDissolveScreen 90 45
        let (titleGroupModel, titleEntityModels) = loadGroupModelFile "Title.nugroup" world
        let world' = addScreenModel TitleAddress titleScreenModel [(Lun.make "group", titleGroupModel, titleEntityModels)] world
        subscribe ClickTitleExitAddress [] (fun _ _ message world_ -> (handle message, false, world_)) world'

    let tryCreateOmniBladeWorld sdlDeps extData =
        let optWorld = tryCreateEmptyWorld sdlDeps extData
        match optWorld with
        | Left _ as left -> left
        | Right world_ ->
            let splashScreenSprite = { SpriteAssetName = Lun.make "Image5"; PackageName = Lun.make "Default"; PackageFileName = "AssetGraph.xml" }
            let world_ = addSplashScreen (changeSelectedScreen TitleAddress) SplashAddress 90 45 90 splashScreenSprite world_
            let world_ = createTitleScreen world_
            let world_ = set (Some SplashAddress) world_ worldOptSelectedScreenModelAddressLens
            Right world_