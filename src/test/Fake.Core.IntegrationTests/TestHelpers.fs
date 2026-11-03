[<AutoOpen>]
module Fake.Core.IntegrationTests.TestHelpers

open System.Collections.Generic
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open System
open System.Linq
open Expecto
open Expecto.Flip
open System.IO
open Microsoft.FSharp.Collections

type TestDir =
    { Dir: string }

    interface IDisposable with
        member x.Dispose() =
            try
                Directory.Delete(x.Dir, true)
            with e ->
                eprintf "Failed to delete '%s': %O" x.Dir e
                ()

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let runDotNetRaw args =
    let options = dotnetSdk.Value(DotNet.Options.Create())

    let dir = Path.GetDirectoryName options.DotNetCliPath
    let oldPath = options |> Process.getEnvironmentVariable "PATH"

    args
    |> CreateProcess.fromRawCommand options.DotNetCliPath
    |> CreateProcess.withEnvironment (options.Environment |> Map.toList)
    |> CreateProcess.setEnvironmentVariable
        "PATH"
        (match oldPath with
         | Some oldPath -> sprintf "%s%c%s" dir Path.PathSeparator oldPath
         | None -> dir)
    |> CreateProcess.withWorkingDirectory options.WorkingDirectory

let createTestDir () =
    let testFile = Path.combine (Path.GetTempPath()) (Path.GetRandomFileName())
    Directory.CreateDirectory(testFile) |> ignore<DirectoryInfo>
    { Dir = testFile }

let testDirLocation = Path.GetDirectoryName(typeof<TestDir>.Assembly.Location)

let createTestDirInCurrent () =
    let folder = testDirLocation </> (Guid.NewGuid()).ToString()
    Directory.CreateDirectory folder |> ignore<DirectoryInfo>
    { Dir = folder }

let getTestFile testFile =
    Path.Combine(testDirLocation, "testdata", testFile)

exception FakeExecutionFailed of ProcessResult with
    override x.ToString() =
        let result = x.Data0
        let stdErr = String.Join(Environment.NewLine, result.Errors)
        let stdOut = String.Join(Environment.NewLine, result.Messages)
        sprintf "FAKE Process exited with %d:\n%s\nStdout: \n%s" result.ExitCode stdErr stdOut

let fakeRootPath = Path.getFullName (__SOURCE_DIRECTORY__ + "../../../../")
let releaseDir = Path.getFullName (fakeRootPath + "/release")
let releaseDotnetCoreDir = Path.getFullName (releaseDir + "/dotnetcore")

let fakeToolPath =
    let rawWithoutExtension =
        let releasePath = releaseDir </> "dotnetcore" </> "Fake.netcore" </> "current"

        if System.IO.Directory.Exists releasePath then
            Path.getFullName (releasePath </> "fake")
        else // Local debug:
            Path.getFullName (
                releaseDir
                </> ".."
                </> "src"
                </> "app"
                </> "Fake.netcore"
                </> "bin"
                </> "Debug"
                </> "net10.0"
                </> "fake"
            )

    if Environment.isUnix then
        rawWithoutExtension
    else
        rawWithoutExtension + ".exe"

let integrationTestPath =
    Path.getFullName (__SOURCE_DIRECTORY__ + "../../../../integrationtests")

let scenarioTempPath scenario =
    integrationTestPath @@ scenario @@ "temp"

let originalScenarioPath scenario =
    integrationTestPath @@ scenario @@ "before"

let resolvePath scenario (path: string) =
    if Path.IsPathRooted path then
        path
    else
        scenarioTempPath scenario @@ path

let prepare scenario =
    let originalScenarioPath = originalScenarioPath scenario
    let scenarioPath = scenarioTempPath scenario

    if Directory.Exists scenarioPath then
        try
            Directory.Delete(scenarioPath, true)
        with _ -> // Maybe locked. Let's sleep and retry.
            System.Threading.Thread.Sleep 5000
            Directory.Delete(scenarioPath, true)
            ()

    Directory.ensure scenarioPath

    Shell.copyDir scenarioPath originalScenarioPath (fun file ->
        // this should be in sync with integrationtests/.gitignore, but CI should ensure that
        let fp = Path.GetFullPath file
        let scfp = Path.GetFullPath scenarioPath
        let isFakeTmp = fp.Substring(scfp.Length).Contains ".fake"
        let isLockFile = fp.EndsWith ".lock"
        not isFakeTmp && not isLockFile)

let directFakeInPath command workingDir target =
    let results = List<ConsoleMessage>()

    let errorF msg =
        results.Add(ConsoleMessage.CreateError msg)

    let messageF msg =
        results.Add(ConsoleMessage.CreateOut msg)

    let processOutput =
        CreateProcess.fromRawCommandLine fakeToolPath command
        |> CreateProcess.withTimeout (TimeSpan.FromMinutes 15.)
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.setEnvironmentVariable "target" target
        |> CreateProcess.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
        |> CreateProcess.redirectOutput
        |> CreateProcess.withOutputEventsNotNull messageF errorF
        |> Proc.run

    let processResult = ProcessResult.New processOutput.ExitCode (results |> List.ofSeq)

    if processOutput.ExitCode <> 0 then
        raise <| FakeExecutionFailed(processResult)

    processResult

type Ctx =
    { FakeFlags: string }

    static member Default = { FakeFlags = "--silent" }
    static member Verbose = { FakeFlags = "--verbose" }

let handleAndFormat f =
    try
        f Ctx.Default
    with FakeExecutionFailed (result) ->
        // Try to improve error output
        let stdOut = String.Join("\n", result.Messages).Trim()
        let stdErr = String.Join("\n", result.Errors)

        try
            ignore (f Ctx.Verbose)
            // Well somehow it worked this time, lets fail with the old message...
            Expect.isTrue
                (sprintf "fake.exe (silent mode) failed with code %d\nOut: %s\nError: %s" result.ExitCode stdOut stdErr)
                false
        with FakeExecutionFailed (verboseResult) ->
            let verboseStdOut = String.Join("\n", verboseResult.Messages).Trim()
            let verboseStdErr = String.Join("\n", verboseResult.Errors)

            Expect.isTrue
                (sprintf
                    "fake.exe (verbose mode) failed with (silentCode %d\nSilentOut: %s\nSilentError: %s)\ncode %d\nOut: %s\nError: %s"
                    verboseResult.ExitCode
                    verboseStdOut
                    verboseStdErr
                    result.ExitCode
                    stdOut
                    stdErr)
                false

            reraise ()

        reraise () // for return value

let expectFailure msg f =
    try
        f Ctx.Default
        Expect.isTrue msg false
        failwithf "%s" msg
    with FakeExecutionFailed (result) ->
        result


let directFake command scenario =
    directFakeInPath command (scenarioTempPath scenario) null

let fakeInPath command scenario path =
    prepare scenario

    directFakeInPath command (resolvePath scenario path) null

let fake command scenario =
    fakeInPath command scenario (scenarioTempPath scenario)

//let fakeFlags = "--verbose"
let fakeFlags = "--silent"

let fakeRunInPath (ctx: Ctx) runArgs scenario path =
    fakeInPath (sprintf "%s run %s" ctx.FakeFlags runArgs) scenario path

let fakeRun ctx runArgs scenario =
    fakeRunInPath ctx runArgs scenario (scenarioTempPath scenario)

let checkIntellisenseInPath scriptName path =
    let cachePath = path </> ".fake" </> scriptName

    File.Exists(cachePath </> "intellisense.fsx")
    |> Expect.isTrue "Expect intellisense.fsx to exist"

    File.Exists(cachePath </> "intellisense_lazy.fsx")
    |> Expect.isTrue "Expect intellisense_lazy.fsx to exist"

    let lines = File.ReadAllLines(cachePath </> "intellisense.fsx") |> Seq.toList

    let expected =
        [ "// This file is automatically generated by FAKE"
          "// This file is needed for IDE support only"
          "#if !FAKE"
          sprintf "#load \"%s\"" Fake.Runtime.Runners.loadScriptLazyName
          "#endif" ]

    Expect.equal "intellisense.fsx should be forwarding" expected lines

let checkScriptAssemblyInPath (scriptName: string) path =
    let scriptNameWithoutExtension = scriptName.Replace(".fsx", "")
    let cachePath = path </> ".fake" </> scriptName

    Directory.EnumerateFiles(cachePath, $"{scriptNameWithoutExtension}_*.dll").Any()
    |> Expect.isTrue "Expect script compiled assembly to exist"

let checkIntellisense scriptName scenario =
    let scenarioPath = scenarioTempPath scenario
    checkIntellisenseInPath scriptName scenarioPath

let fakeRunAndCheckInPath ctx scriptName runArgs scenario path =
    let result = fakeRunInPath ctx runArgs scenario path
    checkIntellisenseInPath scriptName (resolvePath scenario path)
    checkScriptAssemblyInPath scriptName (resolvePath scenario path)
    result

let fakeRunAndCheck ctx scriptName runArgs scenario =
    fakeRunAndCheckInPath ctx scriptName runArgs scenario (scenarioTempPath scenario)
