﻿#nowarn "9"
#r "System.Configuration"
#r "../../../Prime/FSharpx.Core/FSharpx.Core.dll"
#r "../../../Prime/FSharpx.Collections/FSharpx.Collections.dll"
#r "../../../Prime/FParsec/FParsecCS.dll" // MUST be referenced BEFORE FParsec.dll!
#r "../../../Prime/FParsec/FParsec.dll"
#r "../../../Prime/xUnit/xunit.dll"
#r "../../../Prime/Prime/Prime/bin/Debug/Prime.exe"
#r "../../../Nu/Farseer/FarseerPhysics.dll"
#r "../../../Nu/Magick.NET/Magick.NET-AnyCPU.dll"
#r "../../../Nu/SDL2#/Debug/SDL2#.dll"
#r "../../../Nu/TiledSharp/Debug/TiledSharp.dll"
#r "../../../SDL2Addendum/SDL2Addendum/SDL2Addendum/bin/Debug/SDL2Addendum.dll"
#r "../../../Nu/Nu/Nu/bin/Debug/Nu.exe"

open System
open FSharpx
open FParsec
open SDL2
open OpenTK
open TiledSharp
open Prime
open Nu
open Nu.Constants
open Nu.WorldConstants
open Nu.Observation
open Nu.Chain

System.IO.Directory.SetCurrentDirectory <| __SOURCE_DIRECTORY__ + "../bin/Debug"

