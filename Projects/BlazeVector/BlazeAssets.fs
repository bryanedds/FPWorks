﻿namespace BlazeVector
open Nu

// this module contains asset constants that are used by the game.
// having an Assets module is optional, but can prevent you from duplicating string literals across the code base.
[<RequireQualifiedAccess>]
module Assets =

    // these are assets from the Gui package.
    [<RequireQualifiedAccess>]
    module Gui =

        let PackageName = "Gui"
        let MachinerySong = { FadeInMs = 0; FadeOutMs = Constants.Audio.FadeOutMsDefault; Volume = Constants.Audio.SongVolumeDefault; Start = 0.0; Song = asset<Song> PackageName "Machinery" }
        let TitleGroupFilePath = "Assets/Gui/Title.nugroup"
        let CreditsGroupFilePath = "Assets/Gui/Credits.nugroup"
        let GameplayGroupFilePath = "Assets/Gui/Gameplay.nugroup"

    // these are assets from the Gameplay package.
    [<RequireQualifiedAccess>]
    module Gameplay =
        
        let PackageName = "Gameplay"
        let EnemyBulletImage = asset<Image> PackageName "EnemyBullet"
        let PlayerBulletImage = asset<Image> PackageName "PlayerBullet"
        let EnemyImage = asset<Image> PackageName "Enemy"
        let PlayerImage = asset<Image> PackageName "Player"
        let HitSound = asset<Sound> PackageName "Hit"
        let ExplosionSound = asset<Sound> PackageName "Explosion"
        let ShotSound = asset<Sound> PackageName "Shot"
        let JumpSound = asset<Sound> PackageName "Jump"
        let DeathSound = asset<Sound> PackageName "Death"
        let DeadBlazeSong = { FadeInMs = 0; FadeOutMs = Constants.Audio.FadeOutMsDefault; Start = 0.0; Volume = Constants.Audio.SongVolumeDefault * 2.0f; Song = asset<Song> PackageName "DeadBlaze" }
        let SceneGroupFilePath = "Assets/Gameplay/Scene.nugroup"
        let Section0FilePath = "Assets/Gameplay/Section0.nugroup"
        let Section1FilePath = "Assets/Gameplay/Section1.nugroup"
        let Section2FilePath = "Assets/Gameplay/Section2.nugroup"
        let Section3FilePath = "Assets/Gameplay/Section3.nugroup"
        let SectionFilePaths = [Section0FilePath; Section1FilePath; Section2FilePath; Section3FilePath]