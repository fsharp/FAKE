﻿/// This module contains helper function for the GAC
module Fake.GACHelper

open System

/// GAC parameters
type GACParams = 
    { /// (Required) Path to the gacutil
      ToolPath : string
      /// The timeout for the process.
      TimeOut : TimeSpan
      /// The directory where the process will be started.
      WorkingDir : string }

let mutable GACUtil = ProgramFilesX86 @@ "Microsoft SDKs/Windows/v8.0A/bin/NETFX 4.0 Tools/gacutil.exe"

/// GACutil default parameters
let GACDefaults = 
    { ToolPath = GACUtil
      TimeOut = TimeSpan.FromMinutes 5.
      WorkingDir = currentDirectory }

/// Runs gacutil with the given command.
let GAC setParams command = 
    let taskName = "GAC"
    traceStartTask taskName command
    let param = setParams GACDefaults
    
    let ok = 
        execProcess (fun info -> 
            info.FileName <- param.ToolPath
            if param.WorkingDir <> String.Empty then info.WorkingDirectory <- param.WorkingDir
            info.Arguments <- command) param.TimeOut
    if not ok then failwithf "gacutil reported errors."

    traceEndTask taskName command