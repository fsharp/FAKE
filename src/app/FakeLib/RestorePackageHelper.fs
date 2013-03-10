﻿[<AutoOpen>]
module Fake.RestorePackageHelper

open System
open System.IO

type RestorePackageParams =
    { ToolPath: string
      TimeOut: TimeSpan
      OutputPath: string}

/// RestorePackage defaults params  
let RestorePackageDefaults =
    { ToolPath = "./tools/NuGet/NuGet.exe"
      TimeOut = TimeSpan.FromMinutes 5.
      OutputPath = "./packages" }

type RestoreSinglePackageParams = 
    { ToolPath: string
      TimeOut: TimeSpan
      OutputPath: string
      Version: Version option
      ExcludeVersion: bool
      IncludePreRelease: bool }

/// RestoreSinglePackageParams defaults params  
let RestoreSinglePackageDefaults =
    { ToolPath = RestorePackageDefaults.ToolPath
      TimeOut = TimeSpan.FromMinutes 2.
      OutputPath = RestorePackageDefaults.OutputPath
      Version = None
      ExcludeVersion = false
      IncludePreRelease = false }

let runNuGet toolPath timeOut args failWith =
    if not (execProcess3 (fun info ->  
        info.FileName <- toolPath |> FullName
        info.Arguments <- args) timeOut)
    then
        failWith()

let buildNuGetArgs setParams packageId = 
    let parameters = RestoreSinglePackageDefaults |> setParams
    let args = " \"install\" \"" + packageId + "\" \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\""
    match (parameters.ExcludeVersion, parameters.IncludePreRelease, parameters.Version) with
        | (true, false, Some(v))  -> args + " \"-ExcludeVersion\" \"-Version\" \"" + v.ToString() + "\""
        | (true, false, None)     -> args + " \"-ExcludeVersion\""
        | (false, _, Some(v))     -> args + " \"-Version\" \"" + v.ToString() + "\""
        | (false, false, None)    -> args
        | (false, true, _)        -> args + " \"-IncludePreRelease\""
        | (true, true, _)         -> args + " \"-ExcludeVersion\" \"-IncludePreRelease\""

let RestorePackageId setParams packageId = 
    traceStartTask "RestorePackageId" packageId
    let parameters = RestoreSinglePackageDefaults |> setParams
    let args = buildNuGetArgs setParams packageId
    runNuGet parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package installation of package %s failed." packageId)
  
    traceEndTask "RestorePackageId" packageId

           
let RestorePackage setParams package = 
    traceStartTask "RestorePackage" package
    let (parameters:RestorePackageParams) = RestorePackageDefaults |> setParams
    let args =
        " \"install\" \"" + (package |> FullName) + "\"" +
        " \"-OutputDirectory\" \"" + (parameters.OutputPath |> FullName) + "\""

    runNuGet parameters.ToolPath parameters.TimeOut args (fun () -> failwithf "Package installation of %s generation failed." package)
                    
    traceEndTask "RestorePackage" package

let RestorePackages() = 
  !! "./**/packages.config"
  |> Seq.iter (RestorePackage id)
