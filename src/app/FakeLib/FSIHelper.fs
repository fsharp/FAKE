﻿
[<AutoOpen>]
/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.FSIHelper

open System
open System.IO
open System.Linq
open System.Diagnostics
open System.Threading

let private FSIPath = @".\tools\FSharp\;.\lib\FSharp\;[ProgramFilesX86]\Microsoft SDKs\F#\3.1\Framework\v4.0;[ProgramFilesX86]\Microsoft SDKs\F#\3.0\Framework\v4.0;[ProgramFiles]\Microsoft F#\v4.0\;[ProgramFilesX86]\Microsoft F#\v4.0\;[ProgramFiles]\FSharp-2.0.0.0\bin\;[ProgramFilesX86]\FSharp-2.0.0.0\bin\;[ProgramFiles]\FSharp-1.9.9.9\bin\;[ProgramFilesX86]\FSharp-1.9.9.9\bin\"

/// The path to the F# Interactive tool.
let fsiPath =
    let ev = environVar "FSI"
    if not (isNullOrEmpty ev) then ev else
    if isUnix then
        let paths = appSettings "FSIPath" FSIPath
        // The standard name on *nix is "fsharpi"
        match tryFindFile paths "fsharpi" with
        | Some file -> file
        | None -> 
        // The early F# 2.0 name on *nix was "fsi"
        match tryFindFile paths "fsi" with
        | Some file -> file
        | None -> "fsharpi"
    else
        let dir = Path.GetDirectoryName fullAssemblyPath
        let fi = fileInfo (Path.Combine(dir, "fsi.exe"))
        if fi.Exists then fi.FullName else
        findPath "FSIPath" FSIPath "fsi.exe"

type FsiArgs =
    FsiArgs of string list * string * string list with
    static member parse (args:string array) =
        //Find first arg that does not start with - (as these are fsi options that precede the fsx).
        match args |> Array.tryFindIndex (fun arg -> arg.StartsWith("-") = false) with
        | Some(i) ->
            let fsxPath = args.[i]
            if fsxPath.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase) then
                let fsiOpts = if i > 0 then args.[0..i-1] else [||]
                let scriptArgs = if args.Length > (i+1) then args.[i+1..] else [||]
                Choice1Of2(FsiArgs(fsiOpts |> List.ofArray, fsxPath, scriptArgs |> List.ofArray))
            else Choice2Of2(sprintf "Expected argument %s to be the build script path, but it does not have the .fsx extension." fsxPath) 
        | None -> Choice2Of2("Unable to locate the build script path.") 
    
let private FsiStartInfo workingDirectory (FsiArgs(fsiOptions, scriptPath, scriptArgs)) environmentVars =
    (fun (info: ProcessStartInfo) ->
        info.FileName <- fsiPath
        info.Arguments <- String.concat " " (fsiOptions @ [scriptPath] @ scriptArgs)
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild"  msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Creates a ProcessStartInfo which is configured to the F# Interactive.
let fsiStartInfo script workingDirectory args info =
    FsiStartInfo workingDirectory (FsiArgs([], script, [])) args info

/// Run the given buildscript with fsi.exe
let executeFSI workingDirectory script args =
    let (result, messages) =
        ExecProcessRedirected
            (fsiStartInfo script workingDirectory args)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

/// Run the given build script with fsi.exe and allows for extra arguments to FSI.
let executeFSIWithArgs workingDirectory script extraFsiArgs args =
    let result = ExecProcess (FsiStartInfo workingDirectory (FsiArgs(extraFsiArgs, script, [])) args) TimeSpan.MaxValue
    Thread.Sleep 1000
    result = 0

/// Run the given build script with fsi.exe and allows for extra arguments to the script. Returns output.
let executeFSIWithScriptArgsAndReturnMessages workingDirectory script (scriptArgs: string[]) =
    let (result, messages) =
        ExecProcessRedirected (fun si ->
            FsiStartInfo "" (FsiArgs([], script, scriptArgs |> List.ofArray)) [] si)
            TimeSpan.MaxValue
    Thread.Sleep 1000
    (result, messages)

open Microsoft.FSharp.Compiler.Interactive.Shell

/// Run the given FAKE script with fsi.exe at the given working directory. Provides full access to Fsi options and args. Redirect output and error messages.
let internal runFAKEScriptWithFsiArgsAndRedirectMessages workingDirectory printDetails (FsiArgs(fsiOptions, script, scriptArgs)) args onErrMsg onOutMsg =
    if printDetails then traceFAKE "Running Buildscript: %s" script

    // Add arguments to the Environment
    for (k,v) in args do
      Environment.SetEnvironmentVariable(k, v, EnvironmentVariableTarget.Process)

    // Create an env var that only contains the build script args part from the --fsiargs (or "").
    Environment.SetEnvironmentVariable("fsiargs-buildscriptargs", String.Join(" ", scriptArgs))

    let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()

    let commonOptions =
        [ "fsi.exe"; "--noninteractive" ] @ fsiOptions
        |> List.toArray

    let sbOut = Text.StringBuilder()
    let sbErr = Text.StringBuilder()
    let handleMessages() =
        let handleMessagesFrom (sb:Text.StringBuilder) onMsg =
            let s = sb.ToString()
            if not <| String.IsNullOrEmpty s
                then onMsg s
        handleMessagesFrom sbOut onOutMsg
        handleMessagesFrom sbErr onErrMsg

    use outStream = new StringWriter(sbOut)
    use errStream = new StringWriter(sbErr)
    use stdin = new StreamReader(Stream.Null)

    try
        let session = FsiEvaluationSession.Create(fsiConfig, commonOptions, stdin, outStream, errStream)

        try
            session.EvalScript script
            // TODO: Reactivate when FCS don't show output any more 
            // handleMessages()
            true
        with
        | _ ->
            handleMessages()
            false
    with
    | exn ->
        traceError "FsiEvaluationSession could not be created."
        traceError <| sbErr.ToString()
        raise exn

/// Run the given buildscript with fsi.exe and allows for extra arguments to the script. Returns output.
let executeBuildScriptWithArgsAndReturnMessages workingDirectory script (scriptArgs: string[]) =
    let messages = ref []
    let appendMessage isError msg =
        messages := { IsError = isError
                      Message = msg
                      Timestamp = DateTimeOffset.UtcNow } :: !messages
    let result =
        runFAKEScriptWithFsiArgsAndRedirectMessages
            workingDirectory true (FsiArgs([], script, scriptArgs |> List.ofArray)) []
            (appendMessage true) (appendMessage false)
    (result, !messages)

/// Run the given buildscript with fsi.exe at the given working directory.  Provides full access to Fsi options and args.
let runBuildScriptWithFsiArgsAt workingDirectory printDetails (FsiArgs(fsiOptions, script, scriptArgs)) args =
    runFAKEScriptWithFsiArgsAndRedirectMessages
        workingDirectory printDetails (FsiArgs(fsiOptions, script, scriptArgs)) args
        traceError (fun s-> traceFAKE "%s" s)

/// Run the given buildscript with fsi.exe at the given working directory.
let runBuildScriptAt workingDirectory printDetails script extraFsiArgs args =
    runBuildScriptWithFsiArgsAt workingDirectory printDetails (FsiArgs(extraFsiArgs, script, [])) args

/// Run the given buildscript with fsi.exe
let runBuildScript printDetails script extraFsiArgs args =
    runBuildScriptAt "" printDetails script extraFsiArgs args
