//#if (dependencies == "inline")
#r "paket:
nuget Fake.DotNet.Cli prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.Core.Target prerelease
nuget BlackFox.Fake.BuildTask //"
//#endif
#load ".fake/(build.fsx)/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let clean =
    BuildTask.create "Clean" [] { !! "src/**/bin" ++ "src/**/obj" |> Shell.cleanDirs }

let build =
    BuildTask.create "Build" [ clean.IfNeeded ] { !! "src/**/*.*proj" |> Seq.iter (DotNet.build id) }

let _all = BuildTask.createEmpty "All" [ clean; build ]

BuildTask.runOrDefault _all
