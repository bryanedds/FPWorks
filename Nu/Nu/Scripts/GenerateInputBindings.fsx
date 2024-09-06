﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2023.

#I __SOURCE_DIRECTORY__
#r "nuget: Aether.Physics2D, 2.1.0"
#r "nuget: Csv, 2.0.93"
#r "nuget: DotRecast.Recast.Toolset, 2024.3.1"
#r "nuget: FParsec, 1.1.1"
#r "nuget: Twizzle.ImGuizmo.NET, 1.89.4.1"
#r "nuget: Magick.NET-Q8-AnyCpu, 13.5.0"
#r "nuget: Pfim, 0.11.3"
#r "nuget: Prime, 9.27.0"
#r "nuget: System.Configuration.ConfigurationManager, 8.0.0"
#r "nuget: System.Drawing.Common, 8.0.0"
#r "../../../Nu/Nu.Dependencies/AssimpNet/netstandard2.1/AssimpNet.dll"
#r "../../../Nu/Nu.Dependencies/BulletSharpPInvoke/netstandard2.1/BulletSharp.dll"
#r "../../../Nu/Nu.Dependencies/OpenGL.NET/lib/netcoreapp2.2/OpenGL.Net.dll"
#r "../../../Nu/Nu.Dependencies/SDL2-CS/netstandard2.0/SDL2-CS.dll"
#r "../../../Nu/Nu.Dependencies/TiledSharp/lib/netstandard2.0/TiledSharp.dll"
#r "../../../Nu/Nu.Math/bin/Debug/netstandard2.1/Nu.Math.dll"
#r "../../../Nu/Nu/bin/Debug/net8.0/Nu.dll"

open System
open System.Text.RegularExpressions
open System.Linq
open System.IO
open Prime
open SDL2

// this function was copied and converted from - https://stackoverflow.com/a/46095771
let upperCaseToPascalCase (original : string) =
    let invalidCharsRgx = Regex "[^_a-zA-Z0-9]"
    let whiteSpace = Regex "(?<=\\s)"
    let startsWithLowerCaseChar = Regex "^[a-z]"
    let firstCharFollowedByUpperCasesOnly = Regex "(?<=[A-Z])[A-Z0-9]+$"
    let lowerCaseNextToNumber = Regex "(?<=[0-9])[a-z]"
    let upperCaseInside = Regex "(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))"
    let pascalCase =
        // replace white spaces with undescore, then replace all invalid chars with empty string
        invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), "") |>
        // split by underscores
        (fun (str : string) -> str.Split ([|'_'|], StringSplitOptions.RemoveEmptyEntries)) |>
        // set first letter to uppercase
        (fun (strs : string array) -> strs.Select(fun w -> startsWithLowerCaseChar.Replace (w, fun m -> m.Value.ToUpperInvariant ()))) |>
        // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
        (fun (strs : string seq) -> strs.Select (fun w -> firstCharFollowedByUpperCasesOnly.Replace (w, fun m -> m.Value.ToLowerInvariant ()))) |>
        // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
        (fun (strs : string seq) -> strs.Select(fun w -> lowerCaseNextToNumber.Replace (w, fun m -> m.Value.ToUpperInvariant ()))) |>
        // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
        (fun (strs : string seq) -> strs.Select(fun w -> upperCaseInside.Replace (w, fun m -> m.Value.ToLowerInvariant ())))
    String.Concat pascalCase

let enumEntries (ty : Type) =
    ty.GetEnumNames () |>
    enumerable<string> |>
    Seq.map (fun (name : string) ->
        let name = name.Replace ("SDL_SCANCODE_", "")
        let firstChar = name.[0] // NOTE: elided bounds check because I presume no case where this is possible
        if firstChar >= '0' && firstChar <= '9'
        then "Num" + name
        else name) |>
    flip Seq.zip (ty.GetEnumValues () |> enumerable<int>) |>
    Seq.filter (fun (name, _) -> name <> "SDL_NUM_SCANCODES") |>
    Seq.map (mapFst (fun (name : string) -> name.Replace ("RETURN", "ENTER"))) |> // NOTE: ImGui calls this the 'enter' key, so I choose that.
    Seq.map (mapFst upperCaseToPascalCase) |>
    List.ofSeq

let enumEntryToCode (entryName : string, entryValue : int) =
    "    | " + entryName + " = " + scstring entryValue

let enumEntriesToCode entries =
    let codes = List.map enumEntryToCode entries
    String.Join ("\n", codes)

let generateBindingsCode codesStr =
    "// Nu Game Engine.\n" +
    "// Copyright (C) Bryan Edds, 2013-2023.\n" +
    "\n" +
    "//*********************************************************************************************//\n" +
    "//                                                                                             //\n" +
    "// NOTE: This code is GENERATED by 'GenerateInputBindings.fsx'! Do NOT edit this code by hand! //\n" +
    "//                                                                                             //\n" +
    "//*********************************************************************************************//\n" +
    "\n" +
    "namespace Nu\n" +
    "open System\n" +
    "\n" +
    "type KeyboardKey =\n" +
    codesStr +
    "\n"

do
    Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__ + "/../bin/Debug")
    let code =
        typeof<SDL.SDL_Scancode> |>
        enumEntries |>
        enumEntriesToCode |>
        generateBindingsCode
    File.WriteAllText ("../../Sdl/SdlInputBindings.fs", code)