module Fake.DotNet.Cli.IntegrationTests.TemplateTests

open Expecto
open System
open System.IO

open Fake.Core
open Fake.DotNet
open Fake.IO

let templateProj = "fake-template.fsproj"
let templatePackageName = "fake-template"
let templateName = "fake"

type BootstrapKind =
    | Tool
    | Local
    | None

    override x.ToString() =
        match x with
        | Tool -> "tool"
        | Local -> "local"
        | None -> "none"

type DslKind =
    | Fake
    | BuildTask

    override x.ToString() =
        match x with
        | Fake -> "fake"
        | BuildTask -> "buildtask"

type DependenciesKind =
    | File
    | Inline
    | None

    override x.ToString() =
        match x with
        | File -> "file"
        | Inline -> "inline"
        | None -> "none"


let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

let inline opts () = DotNet.Options.lift dotnetSdk.Value

let inline dotnetWorkingDir wd =
    DotNet.Options.lift dotnetSdk.Value >> DotNet.Options.withWorkingDirectory wd

let inline redirect () =
    DotNet.Options.lift (fun opts -> { opts with RedirectOutput = true })

let getDebuggingInfo () =
    sprintf
        "\nDOTNET_ROOT: %s\nPATH: %s\n"
        (Environment.GetEnvironmentVariable("DOTNET_ROOT"))
        (Environment.GetEnvironmentVariable "PATH")

let isProcessSucceeded message (r: ProcessResult<ProcessOutput>) =
    $"Message: {message}\n
    Exit Code: {r.ExitCode}\n
    Debugging Info: {getDebuggingInfo ()}\n
    Result:\n    stderr: {r.Result.Error}\n    stdout: {r.Result.Output}"
    |> Expect.isTrue (r.ExitCode = 0)

let timeout = (TimeSpan.FromMinutes 10.)

let writeTestNuGetConfig rootDir =

    let nugetConfigFilePath = System.IO.Path.Combine(rootDir, "NuGet.config")

    let nugetPackageFolder =
        System.IO.Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "..", "release", "dotnetcore")

    let nugetConfig =
        $"""<?xml version="1.0" encoding="utf-8"?>
        <configuration>
        <packageSources>
        <clear />
        <add key="api.nuget.org" value="https://api.nuget.org/v3/index.json" />
        <add key="localBuild" value="{nugetPackageFolder}" />
        </packageSources>
        </configuration>
        """

    System.IO.File.WriteAllText(nugetConfigFilePath, nugetConfig)

let runTemplate rootDir kind dependencies dsl =
    Directory.ensure rootDir

    try
        writeTestNuGetConfig rootDir

        let result =
            DotNet.exec
                (dotnetWorkingDir rootDir >> redirect ())
                "new"
                $"{templateName} --allow-scripts yes --bootstrap {string kind} --dependencies {string dependencies} --dsl {string dsl}"

        let errors =
            result.Results
            |> List.filter (fun res -> res.IsError)
            |> List.map (fun res -> res.Message)

        let messages =
            result.Results
            |> List.filter (fun res -> not res.IsError)
            |> List.map (fun res -> res.Message)

        let processResult: ProcessResult<ProcessOutput> =
            { ExitCode = result.ExitCode
              Result =
                { Output = String.Join("\n", messages)
                  Error = String.Join("\n", errors) } }

        isProcessSucceeded "should have run the template successfully" processResult
    with e ->
        if
            e.Message.Contains "Command succeeded"
            && e.Message.Contains "was created successfully"
        then
            printfn $"Ignoring exit-code while template creation: {e}"
        else
            reraise ()

let invokeScript dir scriptName (args: string) =
    let fullScriptPath = Path.Combine(dir, scriptName)

    CreateProcess.fromRawCommandLine fullScriptPath args
    |> CreateProcess.withTimeout timeout
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.redirectOutput
    |> Proc.run

let fileContainsText dir fileName text =
    let filePath = Path.Combine(dir, fileName)
    let content = File.ReadAllText(filePath)
    content.Contains(text: string)

let expectMissingTarget targetName (r: ProcessResult<ProcessOutput>) =
    let contains = r.Result.Error.Contains $"Target \"{targetName}\" is not defined"
    Expect.isTrue contains $"Expected the message 'Target {targetName} is not defined' but got: {r.Result.Error}"

let tempDir () =
    Path.Combine("../../../test/fake-template", Path.GetRandomFileName())

let fileExists dir fileName =
    File.Exists(Path.Combine(dir, fileName))

let setupTemplate () =
    Process.setEnableProcessTracing true

    let templateNupkg =
        GlobbingPattern.create "../../../release/dotnetcore/fake-template.*.nupkg"
        |> GlobbingPattern.setBaseDir __SOURCE_DIRECTORY__
        |> Seq.toList
        |> List.rev
        |> List.tryHead

    let fakeTemplateName =
        match templateNupkg with
        | Some t -> t
        | Option.None -> templatePackageName

    try // Specs have changed: needs full package name: https://github.com/dotnet/docs/pull/3054
        DotNet.uninstallTemplate templatePackageName
    with exn ->
        $"should clear out preexisting templates\nDebugging Info: {getDebuggingInfo ()}"
        |> Expect.isTrue false

    printfn $"{Environment.CurrentDirectory}"

    DotNet.setupEnv dotnetSdk.Value

    try
        DotNet.installTemplate fakeTemplateName id
    with exn ->
        $"should install new FAKE template\nDebugging Info: {getDebuggingInfo ()}"
        |> Expect.isTrue false

[<Tests>]
let tests =
    // we need to (uninstall) the template, install the packed version, and then execute that template
    testList
        "Fake.DotNet.Cli.IntegrationTests.TemplateTests"
        [ testList
              "can install and run the template"
              [ setupTemplate ()

                let scriptFile = if Environment.isUnix then "fake.sh" else "fake.cmd"

                let buildFile = "build.fsx"
                let dependenciesFile = "paket.dependencies"

                yield
                    test "fails to build a target that doesn't exist" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool File Fake

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        let result = invokeScript tempDir scriptFile "build -t Nonexistent"
                        Expect.isFalse (result.ExitCode = 0) "the script should have failed"
                        expectMissingTarget "Nonexistent" result
                    }

                yield
                    test "can install a inline-dependencies template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool Inline Fake

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        Expect.isTrue
                            (fileContainsText tempDir buildFile "#r \"paket:")
                            "the build file should contain inline dependencies"

                        Expect.isFalse (fileExists tempDir dependenciesFile) "the dependencies file should not exist"
                    }

                yield
                    test "can install a buildtask-dsl file-dependencies template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool File BuildTask

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        Expect.isTrue
                            (fileContainsText tempDir buildFile "open BlackFox.Fake")
                            "the build file should contain blackfox"

                        Expect.isTrue
                            (fileContainsText tempDir dependenciesFile "nuget BlackFox.Fake.BuildTask")
                            "the dependencies file should contain blackfox"
                    }

                yield
                    test "can build a buildtask-dsl file-dependencies template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool File BuildTask

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        invokeScript tempDir scriptFile "build -t All"
                        |> isProcessSucceeded "should build successfully"
                    }

                yield
                    test "can install a buildtask-dsl inline-dependencies template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool Inline BuildTask

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        Expect.isTrue
                            (fileContainsText tempDir buildFile "nuget BlackFox.Fake.BuildTask")
                            "the build file should contain blackfox dependency"
                    }

                yield
                    test "can build a buildtask-dsl inline-dependencies template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool Inline BuildTask

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        invokeScript tempDir scriptFile "build -t All"
                        |> isProcessSucceeded "should build successfully"
                    }

                yield
                    test "can build with the local-style template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Local Inline BuildTask

                        invokeScript tempDir scriptFile "build -t All"
                        |> isProcessSucceeded "should build successfully"
                    }

                // ignored because the .net tool install to a subdirectory is broken: https://github.com/fsharp/FAKE/pull/1989#issuecomment-396057330
                yield
                    ptest "can install a tool-style template" {
                        let tempDir = tempDir ()
                        runTemplate tempDir Tool File Fake

                        Expect.isFalse
                            (Directory.Exists(Path.Combine(tempDir, ".fake")))
                            "After creating the template the '.fake' directory should not exist!"

                        invokeScript tempDir scriptFile "--help"
                        |> isProcessSucceeded "should invoke help"
                    } ] ]
