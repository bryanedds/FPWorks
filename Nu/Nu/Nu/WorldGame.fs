﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2015.

namespace Nu
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Xml
open OpenTK
open Prime
open Nu

[<AutoOpen>]
module WorldGameModule =

    type Game with
        
        member this.GetId world = (World.getGameState world).Id
        member this.GetCreationTimeStampNp world = (World.getGameState world).CreationTimeStampNp
        member this.GetDispatcherNp world = (World.getGameState world).DispatcherNp
        member this.GetOptSelectedScreen world = (World.getGameState world).OptSelectedScreen
        member this.SetOptSelectedScreen value world = World.updateGameState (fun (gameState : GameState) -> { gameState with OptSelectedScreen = value }) world
        member this.GetPublishChanges world = (World.getGameState world).PublishChanges
        member this.SetPublishChanges value world = World.updateGameState (fun gameState -> { gameState with PublishChanges = value }) world
        member this.GetXtension world = (World.getGameState world).Xtension
        member this.UpdateXtension updater world = World.updateGameState (fun gameState -> { gameState with Xtension = updater gameState.Xtension}) world

        /// Get an xtension field by name.
        member this.GetXField name world =
            let xtension = this.GetXtension world
            let xField = Map.find name xtension.XFields
            xField.FieldValue

        /// Query that a game dispatches in the same manner as the dispatcher with the target type.
        member this.DispatchesAs (dispatcherTargetType : Type) world =
            Reflection.dispatchesAs dispatcherTargetType (this.GetDispatcherNp world)

    type World with

        static member internal registerGame (world : World) : World =
            let dispatcher = Simulants.Game.GetDispatcherNp world : GameDispatcher
            dispatcher.Register (Simulants.Game, world)

        static member internal makeGameState dispatcher =
            let gameState = GameState.make dispatcher
            Reflection.attachFields dispatcher gameState
            gameState

        // Get all the entities in the world.
        static member proxyEntities1 world =
            World.proxyGroups1 world |>
            Seq.map (fun group -> World.proxyEntities group world) |>
            Seq.concat

        // Get all the groups in the world.
        static member proxyGroups1 world =
            World.proxyScreens world |>
            Seq.map (fun screen -> World.proxyGroups screen world) |>
            Seq.concat

        /// Try to get the currently selected screen.
        static member getOptSelectedScreen world =
            Simulants.Game.GetOptSelectedScreen world

        /// Set the currently selected screen or None. Be careful using this function directly as
        //// you may be wanting to use the higher-level World.transitionScreen function instead.
        static member setOptSelectedScreen optScreen world =
            Simulants.Game.SetOptSelectedScreen optScreen world

        /// Get the currently selected screen (failing with an exception if there isn't one).
        static member getSelectedScreen world =
            Option.get ^ World.getOptSelectedScreen world
        
        /// Set the currently selected screen. Be careful using this function directly as you may
        /// be wanting to use the higher-level World.transitionScreen function instead.
        static member setSelectedScreen screen world =
            World.setOptSelectedScreen (Some screen) world

        /// Write a game to an xml writer.
        static member writeGame (writer : XmlWriter) world =
            let gameState = World.getGameState world
            let screens = World.proxyScreens world
            writer.WriteAttributeString (Constants.Xml.DispatcherNameAttributeName, Reflection.getTypeName gameState.DispatcherNp)
            Reflection.writeMemberValuesFromTarget tautology3 writer gameState
            writer.WriteStartElement Constants.Xml.ScreensNodeName
            World.writeScreens writer screens world
            writer.WriteEndElement ()

        /// Write a game to an xml file.
        static member writeGameToFile (filePath : string) world =
            let filePathTmp = filePath + ".tmp"
            let writerSettings = XmlWriterSettings ()
            writerSettings.Indent <- true
            use writer = XmlWriter.Create (filePathTmp, writerSettings)
            writer.WriteStartElement Constants.Xml.RootNodeName
            writer.WriteStartElement Constants.Xml.GameNodeName
            World.writeGame writer world
            writer.WriteEndElement ()
            writer.WriteEndElement ()
            writer.Dispose ()
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a game from an xml node.
        static member readGame
            gameNode defaultDispatcherName defaultScreenDispatcherName defaultGroupDispatcherName defaultEntityDispatcherName world =
            let dispatcherName = Reflection.readDispatcherName defaultDispatcherName gameNode
            let dispatcher =
                match Map.tryFind dispatcherName world.Components.GameDispatchers with
                | Some dispatcher -> dispatcher
                | None ->
                    note ^ "Could not locate dispatcher '" + dispatcherName + "'."
                    let dispatcherName = typeof<GameDispatcher>.Name
                    Map.find dispatcherName world.Components.GameDispatchers
            let gameState = World.makeGameState dispatcher
            Reflection.readMemberValuesToTarget gameNode gameState
            let world = World.setGameState gameState world
            let world =
                World.readScreens
                    gameNode
                    defaultScreenDispatcherName
                    defaultGroupDispatcherName
                    defaultEntityDispatcherName
                    world |>
                    snd
            world

        /// Read a game from an xml file.
        static member readGameFromFile (filePath : string) world =
            use reader = XmlReader.Create filePath
            let document = let emptyDoc = XmlDocument () in (emptyDoc.Load reader; emptyDoc)
            let rootNode = document.[Constants.Xml.RootNodeName]
            let gameNode = rootNode.[Constants.Xml.GameNodeName]
            World.readGame
                gameNode
                typeof<GameDispatcher>.Name
                typeof<ScreenDispatcher>.Name
                typeof<GroupDispatcher>.Name
                typeof<EntityDispatcher>.Name
                world

namespace Debug
open Prime
open Nu
open System.Reflection
type Game =

    /// Provides a view of all the properties of a game. Useful for debugging such as with
    /// the Watch feature in Visual Studio.
    static member viewProperties world =
        let state = World.getGameState world
        let properties = Array.map (fun (property : PropertyInfo) -> (property.Name, property.GetValue state)) ((state.GetType ()).GetProperties ())
        Map.ofSeq properties
        
    /// Provides a view of all the xtension fields of a game. Useful for debugging such as
    /// with the Watch feature in Visual Studio.
    static member viewXFields world =
        let state = World.getGameState world
        Map.map (fun _ field -> field.FieldValue) state.Xtension.XFields

    /// Provides a full view of all the member values of a game. Useful for debugging such
    /// as with the Watch feature in Visual Studio.
    static member view world = Game.viewProperties world @@ Game.viewXFields world

    /// Provides a partitioned view of all the member values of a game. Useful for debugging
    /// such as with the Watch feature in Visual Studio.
    static member peek world = Watchable (Game.viewProperties world, Game.viewXFields world)