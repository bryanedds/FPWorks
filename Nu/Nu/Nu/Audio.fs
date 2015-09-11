﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2015.

namespace Nu
open System
open System.IO
open System.ComponentModel
open FSharpx.Collections
open SDL2
open Prime
open Nu

/// A message to the audio system to play a song.
type [<StructuralEquality; NoComparison>] PlaySongMessage =
    { TimeToFadeOutSongMs : int
      Volume : single
      Song : AssetTag }

/// A message to the audio system to play a sound.
type [<StructuralEquality; NoComparison>] PlaySoundMessage =
    { Volume : single
      Sound : AssetTag }
      
/// Hint that an audio asset package with the given name should be loaded. Should be used to
/// avoid loading assets at inconvenient times (such as in the middle of game play!)
type [<StructuralEquality; NoComparison>] HintAudioPackageUseMessage =
    { PackageName : string }

/// Hint that an audio package should be unloaded since its assets will not be used again (or
/// until specified via a HintAudioPackageUseMessage).
type [<StructuralEquality; NoComparison>] HintAudioPackageDisuseMessage =
    { PackageName : string }

/// A message to the audio system.
type [<StructuralEquality; NoComparison>] AudioMessage =
    | HintAudioPackageUseMessage of HintAudioPackageUseMessage
    | HintAudioPackageDisuseMessage of HintAudioPackageDisuseMessage
    | PlaySoundMessage of PlaySoundMessage
    | PlaySongMessage of PlaySongMessage
    | FadeOutSongMessage of int
    | StopSongMessage
    | ReloadAudioAssetsMessage

/// An audio asset used by the audio system.
type [<ReferenceEquality>] AudioAsset =
    | WavAsset of nativeint
    | OggAsset of nativeint

/// The audio player. Represents the audio system of Nu generally.
type IAudioPlayer =
    /// Clear all of the audio messages that have been enqueued.
    abstract ClearMessages : unit -> IAudioPlayer
    /// Enqueue a message from an external source.
    abstract EnqueueMessage : AudioMessage -> IAudioPlayer
    /// 'Play' the audio system. Must be called once per frame.
    abstract Play : unit -> IAudioPlayer

/// The primary implementation of IAudioPlayer.
type [<ReferenceEquality>] AudioPlayer =
    private
        { AudioContext : unit // audio context, interestingly, is global. Good luck encapsulating that!
          AudioAssetMap : AudioAsset AssetMap
          AudioMessages : AudioMessage Queue
          OptCurrentSong : PlaySongMessage option
          OptNextPlaySong : PlaySongMessage option
          AssetGraphFilePath : string }

    static member private haltSound () =
        SDL_mixer.Mix_HaltMusic () |> ignore
        let (_, _, _, channelCount) =  SDL_mixer.Mix_QuerySpec ()
        for i in [0 .. channelCount - 1] do
            SDL_mixer.Mix_HaltChannel i |> ignore

    static member private tryLoadAudioAsset2 (asset : Asset) =
        match Path.GetExtension asset.FilePath with
        | ".wav" ->
            let optWav = SDL_mixer.Mix_LoadWAV asset.FilePath
            if optWav <> IntPtr.Zero then Some (asset.AssetTag.AssetName, WavAsset optWav)
            else
                let errorMsg = SDL.SDL_GetError ()
                trace ^ "Could not load wav '" + asset.FilePath + "' due to '" + errorMsg + "'."
                None
        | ".ogg" ->
            let optOgg = SDL_mixer.Mix_LoadMUS asset.FilePath
            if optOgg <> IntPtr.Zero then Some (asset.AssetTag.AssetName, OggAsset optOgg)
            else
                let errorMsg = SDL.SDL_GetError ()
                trace ^ "Could not load ogg '" + asset.FilePath + "' due to '" + errorMsg + "'."
                None
        | extension -> trace ^ "Could not load audio asset '" + acstring asset + "' due to unknown extension '" + extension + "'."; None

    static member private tryLoadAudioPackage packageName audioPlayer =
        match Assets.tryLoadAssetsFromPackage true (Some Constants.Xml.AudioAssociation) packageName audioPlayer.AssetGraphFilePath with
        | Right assets ->
            let optAudioAssets = List.map AudioPlayer.tryLoadAudioAsset2 assets
            let audioAssets = List.definitize optAudioAssets
            let optAudioAssetMap = Map.tryFind packageName audioPlayer.AudioAssetMap
            match optAudioAssetMap with
            | Some audioAssetMap ->
                let audioAssetMap = Map.addMany audioAssets audioAssetMap
                { audioPlayer with AudioAssetMap = Map.add packageName audioAssetMap audioPlayer.AudioAssetMap }
            | None ->
                let audioAssetMap = Map.ofSeq audioAssets
                { audioPlayer with AudioAssetMap = Map.add packageName audioAssetMap audioPlayer.AudioAssetMap }
        | Left error ->
            trace ^ "HintAudioPackageUseMessage failed due unloadable assets '" + error + "' for '" + acstring (packageName, audioPlayer.AssetGraphFilePath) + "'."
            audioPlayer
        
    static member private tryLoadAudioAsset (assetTag : AssetTag) audioPlayer =
        let (audioPlayer, optAssetMap) =
            match Map.tryFind assetTag.PackageName audioPlayer.AudioAssetMap with
            | Some _ -> (audioPlayer, Map.tryFind assetTag.PackageName audioPlayer.AudioAssetMap)
            | None ->
                note ^ "Loading audio package '" + assetTag.PackageName + "' for asset '" + assetTag.AssetName + "' on the fly."
                let audioPlayer = AudioPlayer.tryLoadAudioPackage assetTag.PackageName audioPlayer
                (audioPlayer, Map.tryFind assetTag.PackageName audioPlayer.AudioAssetMap)
        (audioPlayer, Option.bind (fun assetMap -> Map.tryFind assetTag.AssetName assetMap) optAssetMap)

    static member private playSong playSongMessage audioPlayer =
        let song = playSongMessage.Song
        let (audioPlayer, optAudioAsset) = AudioPlayer.tryLoadAudioAsset song audioPlayer
        match optAudioAsset with
        | Some (WavAsset _) -> note ^ "Cannot play wav file as song '" + acstring song + "'."
        | Some (OggAsset oggAsset) ->
            SDL_mixer.Mix_VolumeMusic (int ^ playSongMessage.Volume * single SDL_mixer.MIX_MAX_VOLUME) |> ignore
            SDL_mixer.Mix_FadeInMusic (oggAsset, -1, 256) |> ignore // Mix_PlayMusic seems to somtimes cause audio 'popping' when starting a song, so a fade is used instead... |> ignore
        | None -> note ^ "PlaySongMessage failed due to unloadable assets for '" + acstring song + "'."
        { audioPlayer with OptCurrentSong = Some playSongMessage }

    static member private handleHintAudioPackageUse (hintPackageUse : HintAudioPackageUseMessage) audioPlayer =
        AudioPlayer.tryLoadAudioPackage hintPackageUse.PackageName audioPlayer

    static member private handleHintAudioPackageDisuse (hintPackageDisuse : HintAudioPackageDisuseMessage) audioPlayer =
        let packageName = hintPackageDisuse.PackageName
        match Map.tryFind packageName audioPlayer.AudioAssetMap with
        | Some assets ->
            // all sounds / music must be halted because one of them might be playing during unload
            // (which is very bad according to the API docs).
            AudioPlayer.haltSound ()
            for asset in assets do
                match asset.Value with
                | WavAsset wavAsset -> SDL_mixer.Mix_FreeChunk wavAsset
                | OggAsset oggAsset -> SDL_mixer.Mix_FreeMusic oggAsset
            { audioPlayer with AudioAssetMap = Map.remove packageName audioPlayer.AudioAssetMap }
        | None -> audioPlayer

    static member private handlePlaySound playSoundMessage audioPlayer =
        let sound = playSoundMessage.Sound
        let (audioPlayer, optAudioAsset) = AudioPlayer.tryLoadAudioAsset sound audioPlayer
        match optAudioAsset with
        | Some (WavAsset wavAsset) ->
            SDL_mixer.Mix_VolumeChunk (wavAsset, int ^ playSoundMessage.Volume * single SDL_mixer.MIX_MAX_VOLUME) |> ignore
            SDL_mixer.Mix_PlayChannel (-1, wavAsset, 0) |> ignore
        | Some (OggAsset _) -> note ^ "Cannot play ogg file as sound '" + acstring sound + "'."
        | None -> note ^ "PlaySoundMessage failed due to unloadable assets for '" + acstring sound + "'."
        audioPlayer

    static member private handlePlaySong playSongMessage audioPlayer =
        if SDL_mixer.Mix_PlayingMusic () = 1 then
            if audioPlayer.OptCurrentSong <> Some playSongMessage then
                if  playSongMessage.TimeToFadeOutSongMs <> 0 &&
                    not (SDL_mixer.Mix_FadingMusic () = SDL_mixer.Mix_Fading.MIX_FADING_OUT) then
                    SDL_mixer.Mix_FadeOutMusic playSongMessage.TimeToFadeOutSongMs |> ignore
                else
                    SDL_mixer.Mix_HaltMusic () |> ignore
                { audioPlayer with OptNextPlaySong = Some playSongMessage }
            else audioPlayer
        else AudioPlayer.playSong playSongMessage audioPlayer

    static member private handleFadeOutSong timeToFadeOutSongMs audioPlayer =
        if SDL_mixer.Mix_PlayingMusic () = 1 then
            if  timeToFadeOutSongMs <> 0 &&
                SDL_mixer.Mix_FadingMusic () <> SDL_mixer.Mix_Fading.MIX_FADING_OUT then
                SDL_mixer.Mix_FadeOutMusic timeToFadeOutSongMs |> ignore
            else
                SDL_mixer.Mix_HaltMusic () |> ignore
        audioPlayer

    static member private handleStopSong audioPlayer =
        if SDL_mixer.Mix_PlayingMusic () = 1 then SDL_mixer.Mix_HaltMusic () |> ignore
        audioPlayer

    static member private handleReloadAudioAssets audioPlayer =
        let oldAssetMap = audioPlayer.AudioAssetMap
        let audioPlayer = { audioPlayer with AudioAssetMap = Map.empty }
        List.fold
            (fun audioPlayer packageName -> AudioPlayer.tryLoadAudioPackage packageName audioPlayer)
            audioPlayer
            (Map.toKeyList oldAssetMap)

    static member private handleAudioMessage audioPlayer audioMessage =
        match audioMessage with
        | HintAudioPackageUseMessage hintPackageUse -> AudioPlayer.handleHintAudioPackageUse hintPackageUse audioPlayer
        | HintAudioPackageDisuseMessage hintPackageDisuse -> AudioPlayer.handleHintAudioPackageDisuse hintPackageDisuse audioPlayer
        | PlaySoundMessage playSoundMessage -> AudioPlayer.handlePlaySound playSoundMessage audioPlayer
        | PlaySongMessage playSongMessage -> AudioPlayer.handlePlaySong playSongMessage audioPlayer
        | FadeOutSongMessage timeToFadeSongMs -> AudioPlayer.handleFadeOutSong timeToFadeSongMs audioPlayer
        | StopSongMessage -> AudioPlayer.handleStopSong audioPlayer
        | ReloadAudioAssetsMessage -> AudioPlayer.handleReloadAudioAssets audioPlayer

    static member private handleAudioMessages audioMessages audioPlayer =
        Queue.fold AudioPlayer.handleAudioMessage audioPlayer audioMessages

    static member private tryUpdateCurrentSong audioPlayer =
        if SDL_mixer.Mix_PlayingMusic () = 1 then audioPlayer
        else { audioPlayer with OptCurrentSong = None }

    static member private tryUpdateNextSong audioPlayer =
        match audioPlayer.OptNextPlaySong with
        | Some nextPlaySong ->
            if SDL_mixer.Mix_PlayingMusic () = 0 then
                let audioPlayer = AudioPlayer.handlePlaySong nextPlaySong audioPlayer
                { audioPlayer with OptNextPlaySong = None }
            else audioPlayer
        | None -> audioPlayer

    static member private updateAudioPlayer audioPlayer =
        audioPlayer |>
            AudioPlayer.tryUpdateCurrentSong |>
            AudioPlayer.tryUpdateNextSong

    /// Make an AudioPlayer.
    static member make assetGraphFilePath =
        if SDL.SDL_WasInit SDL.SDL_INIT_AUDIO = 0u then
            failwith "Cannot create an AudioPlayer without SDL audio initialized."
        let audioPlayer =
            { AudioContext = ()
              AudioAssetMap = Map.empty
              AudioMessages = Queue.empty
              OptCurrentSong = None
              OptNextPlaySong = None
              AssetGraphFilePath = assetGraphFilePath }
        audioPlayer

    interface IAudioPlayer with

        member audioPlayer.ClearMessages () =
            let audioPlayer = { audioPlayer with AudioMessages = Queue.empty }
            audioPlayer :> IAudioPlayer

        member audioPlayer.EnqueueMessage audioMessage =
            let audioMessages = Queue.conj audioMessage audioPlayer.AudioMessages
            let audioPlayer = { audioPlayer with AudioMessages = audioMessages }
            audioPlayer :> IAudioPlayer

        member audioPlayer.Play () =
            let audioMessages = audioPlayer.AudioMessages
            let audioPlayer = { audioPlayer with AudioMessages = Queue.empty }
            let audioPlayer = AudioPlayer.handleAudioMessages audioMessages audioPlayer
            let audioPlayer = AudioPlayer.updateAudioPlayer audioPlayer
            audioPlayer :> IAudioPlayer

/// The mock implementation of IAudioPlayer.
type [<ReferenceEquality>] MockAudioPlayer =
    private
        { MockAudioPlayer : unit }

    interface IAudioPlayer with
        member audioPlayer.ClearMessages () = audioPlayer :> IAudioPlayer
        member audioPlayer.EnqueueMessage _ = audioPlayer :> IAudioPlayer
        member audioPlayer.Play () = audioPlayer :> IAudioPlayer

    static member make () =
        { MockAudioPlayer = () }