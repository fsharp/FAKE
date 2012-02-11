﻿// include Fake libs
#r @"FakeLib.dll"

open Fake 
open System.IO

// *** Define Targets ***

Description "Cleans the last build"
Target "Clean" (fun () -> 
    trace " --- Cleaning stuff --- "
)

Target "Build" (fun () -> 
    trace " --- Building the app --- "
    trace (File.ReadAllText(@"Readme.txt"))
)

Target "Deploy" (fun () -> 
    trace " --- Deploying app --- "
)

// *** Define Dependencies ***
"Clean"
  ==> "Build"
  ==> "Deploy"

// *** Start Build ***
RunParameterTargetOrDefault "target" "Deploy"