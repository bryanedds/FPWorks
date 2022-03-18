﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Diagnostics
open System.IO
open Prime
module Program =

    (* DISCUSSION - On Nu's authoring story...

    Instead of using a single, general purpose scripting language for authoring tasks in Nu, we use a small set of
    domain-specific languages. For example, the simulant system uses s-expression-based DSLs, as does the overlay,
    asset graph, and effect system. The simulation interactions are defined with an Elm-style Model-View-Update form
    factor directly in F#.

    What follows is a matrix of engine systems and the authoring language they provide to the user -

    system          | language                  | editor
    -----------------------------------------------------------
    simulant defns  | s-expr DSL                | Gaia
    event filtering | s-expr DSL                | Gaia
    collision bodies| s-expr DSL                | Gaia
    overlay         | s-expr DSL                | Visual Studio & Gaia
    asset graph     | s-expr DSL                | Visual Studio & Gaia
    console script  | s-expr DSL                | Visual Studio & Gaia
    effect system   | s-expr DSL                | Gaia & Aether (TBA)
    mind (TBA)      | s-expr DSL                | Gaia & Pheobe (TBA) - http://www.cs.uu.nl/research/techreps/repo/CS-2013/2013-003.pdf
    particles       | F#                        | Visual Studio
    subsystems      | F#                        | Visual Studio
    ECS             | F#                        | Visual Studio
    OO components   | F# (facets / dispatchers) | Visual Studio
    elmish / MVU    | F# (facets / dispatchers) | Visual Studio
    interactions    | F# (chains)               | Visual Studio (deprecated, but still usable)

    The advantages and limitations that fall out of this are as such -

    The systems that provide an s-expr DSL have their DSLs interpreted at run-time and, unlike code in F#, allow for
    hot-reloading for optimal authoring experiences. For these systems, however, no static checking is in place,
    allowing for trivial errors.

    For the systems that aren't interpreted, a strong type system is in place to make sure complex data-flow
    dependencies are made explicit and checked with good error messages. For this system, however, no hot-reloading
    is possible, negatively affecting the authoring experience.

    The trade-offs for each given domain does seem to be approxiately appropriate. *)

    (* WISDOM - Dealing with different device resolutions - Instead of rendering each component scaled to a back-buffer
    of a varying size, render each component unscaled to an off-screen buffer of a static size and then blit that with
    scaling to the back-buffer. NOTE: this only applies to 2D ~ will not apply to 3D once implemented in Nu (for
    obvious reasons). *)

    (* WISDOM: Keep all animation frame numbers even. That way, you can simply halve them if you need to move the app
    from 60fps to 30fps. *)

    (* WISDOM: You get only slightly lower performance by using reference types in the ECS when all of the pre-
    allocated references are allocated consecutively. However, it's still much better to use value types instead
    because you are able to end up with unmanaged types that will not need to be scanned by the GC. *)

    /// Program entry point.
    let [<EntryPoint; STAThread>] main _ =

        // ensure template directory exists
        let programDir = Reflection.Assembly.GetEntryAssembly().Location |> Path.GetDirectoryName 
        let slnDir = Path.Combine (programDir, "../../../..") |> Path.Simplify
        let templateDir = Path.Combine (programDir, "../../../Nu.Template") |> Path.Simplify
        if Directory.Exists templateDir then

            // query user to create new project
            Console.Write "Create a new game with the Nu Game Engine? [y/n]: "
            let result = Console.ReadLine ()
            match result.ToUpper () with
            | "Y" ->

                // execute name entry
                Console.Write "Please enter your project's name (no spaces, tabs or dots, PascalCase is preferred): "
                let name = Console.ReadLine ()
                let name = name.Replace(" ", "").Replace("\t", "").Replace(".", "")
                if Array.notExists (fun char -> name.Contains (string char)) (Path.GetInvalidPathChars ()) then
                    
                    // compute directories
                    let templateIdentifier = templateDir.Replace("/", "\\") // this is what dotnet knows the template as for uninstall...
                    let templateFileName = "Nu.Template.fsproj"
                    let projectsDir = Path.Combine (programDir, "../../../../Projects") |> Path.Simplify
                    let newProjDir = Path.Combine (projectsDir, name) |> Path.Simplify
                    let newFileName = name + ".fsproj"
                    let newProj = Path.Combine (newProjDir, newFileName) |> Path.Simplify
                    Console.WriteLine ("Creating project '" + name + "' in '" + projectsDir + "'...")

                    // install nu template
                    Directory.SetCurrentDirectory templateDir
                    Process.Start("dotnet", "new -u \"" + templateIdentifier + "\"").WaitForExit()
                    Process.Start("dotnet", "new -i ./").WaitForExit()

                    // instantiate nu template
                    Directory.SetCurrentDirectory projectsDir
                    Directory.CreateDirectory name |> ignore<DirectoryInfo>
                    Directory.SetCurrentDirectory newProjDir
                    Process.Start("dotnet", "new nu-game --force").WaitForExit()

                    // rename project file
                    File.Copy (templateFileName, newFileName, true)
                    File.Delete templateFileName

                    // substitute $safeprojectname$ in project file
                    let newProjStr = File.ReadAllText newProj
                    let newProjStr = newProjStr.Replace("$safeprojectname$", name)
                    File.WriteAllText (newProj, newProjStr)

                    // add project to sln file
                    // NOTE: not currently working due to project in old file format - user is instructed to do this
                    // manually.
                    //Directory.SetCurrentDirectory slnDir
                    //Process.Start("dotnet", "sln add Nu.sln \"" + newProj + "\"").WaitForExit()
                    ignore (slnDir, newProj)
                    
                    // success
                    Constants.Engine.SuccessExitCode

                // invalid name; failure
                else
                    Console.WriteLine ("Project name '" + name + "' contains invalid path characters.")
                    Constants.Engine.FailureExitCode

            // rejected
            | _ -> Constants.Engine.SuccessExitCode

        // nothing to do
        else Constants.Engine.SuccessExitCode