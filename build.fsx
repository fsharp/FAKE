// First do this and ignore the result: dotnet build --configuration Release
// Then to build do: dotnet fake run build.fsx -e configuration=Release
// And to release do: dotnet fake run build.fsx -e configuration=Release -t Release

#r "paket:
source release/dotnetcore
source https://api.nuget.org/v3/index.json
nuget FSharp.Core
nuget Microsoft.Build 17.3.2
nuget Microsoft.Build.Framework 17.3.2
nuget Microsoft.Build.Tasks.Core 17.3.2
nuget Microsoft.Build.Utilities.Core 17.3.2
nuget System.AppContext prerelease
nuget Fake.Api.GitHub prerelease
nuget Fake.BuildServer.AppVeyor prerelease
nuget Fake.BuildServer.TeamCity prerelease
nuget Fake.BuildServer.Travis prerelease
nuget Fake.BuildServer.TeamFoundation prerelease
nuget Fake.BuildServer.GitLab prerelease
nuget Fake.BuildServer.GitHubActions prerelease
nuget Fake.Core.Target prerelease
nuget Fake.Core.SemVer prerelease
nuget Fake.Core.Vault prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.IO.Zip prerelease
nuget Fake.Core.ReleaseNotes prerelease
nuget Fake.DotNet.AssemblyInfoFile prerelease
nuget Fake.DotNet.MSBuild prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.NuGet prerelease
nuget Fake.DotNet.Paket prerelease
nuget Fake.DotNet.Testing.MSpec prerelease
nuget Fake.DotNet.Testing.XUnit2 prerelease
nuget Fake.DotNet.Testing.NUnit prerelease
nuget Fake.Windows.Chocolatey prerelease
nuget Fake.JavaScript.Npm prerelease
nuget Fake.Tools.Git prerelease
nuget Mono.Cecil prerelease
nuget System.Reactive
nuget Suave
nuget Newtonsoft.Json
nuget System.Net.Http
nuget Octokit 6.0.0
nuget Microsoft.Deployment.DotNet.Releases //"

open System.Reflection
open System
open System.IO
open System.IO.Compression
open System.Text
open Fake.Api
open Fake.Core
open Fake.BuildServer
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Windows
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Core.TargetOperators
open System.Net.Http
open Microsoft.Deployment.DotNet.Releases
open Fake.JavaScript
open Octokit
open Octokit.Internal

// ****************************************************************************************************
// ------------------------------------------- Definitions -------------------------------------------
// ****************************************************************************************************

// Set this to true if you have lots of breaking changes, for small breaking changes use #if BOOTSTRAP,
// setting this flag will not be accepted
let disableBootstrap = false

// properties
let release = ReleaseNotes.load "RELEASE_NOTES.md"
let docsDir = "./docs"
let releaseDir = "./release"
let nugetDncDir = releaseDir </> "dotnetcore"
let collectedArtifactsDir = releaseDir </> "artifacts"
let chocoReleaseDir = nugetDncDir </> "chocolatey"
let root = __SOURCE_DIRECTORY__
let srcDir = root </> "src"
let appDir = srcDir </> "app"
let templateDir = srcDir </> "template"

// NuGet
let nugetExe =
    Directory.GetCurrentDirectory()
    </> "packages"
    </> "build"
    </> "NuGet.CommandLine"
    </> "tools"
    </> "NuGet.exe"

// Secrets and Vault
let mutable secrets = []

let vault =
    match Vault.fromFakeEnvironmentOrNone () with
    | Some v -> v
    | None -> TeamFoundation.variables

let getVarOrDefaultFromVault name def =
    match vault.TryGet name with
    | Some v -> v
    | None -> Environment.environVarOrDefault name def

let releaseSecret replacement name =
    let secret =
        lazy
            let env =
                match getVarOrDefaultFromVault name "default_unset" with
                | "default_unset" -> failwithf "variable '%s' is not set" name
                | s -> s
            if BuildServer.buildServer <> BuildServer.TeamFoundation then
                // on TFS/VSTS the build will take care of this.
                TraceSecrets.register replacement env
            env

    secrets <- secret :: secrets
    secret

// More properties
let githubReleaseUser = getVarOrDefaultFromVault "RELEASE_USER_GITHUB" "fsprojects"
let gitName = getVarOrDefaultFromVault "REPOSITORY_NAME_GITHUB" "FAKE"

let nugetSource =
    getVarOrDefaultFromVault "NUGET_SOURCE" "https://www.nuget.org/api/v2/package"

let chocoSource =
    getVarOrDefaultFromVault "CHOCO_SOURCE" "https://push.chocolatey.org/"

let artifactsDir = getVarOrDefaultFromVault "ARTIFACTS_DIRECTORY" ""
let docsDomain =
    match BuildServer.isLocalBuild with
    | true -> "http://127.0.0.1:8083/"
    | false -> getVarOrDefaultFromVault "DOCS_DOMAIN" "fake.build"
let fromArtifacts = not <| String.isNullOrEmpty artifactsDir
let apiKey = releaseSecret "<nugetkey>" "NUGET_KEY"
let chocoKey = releaseSecret "<chocokey>" "CHOCOLATEY_API_KEY"
let githubToken = releaseSecret "<githubtoken>" "GITHUB_TOKEN"

do Environment.setEnvironVar "COREHOST_TRACE" "0"

BuildServer.install [ GitHubActions.Installer ]

// Parsing version. Base version come from RELEASE_NOTES;
// When building on a CI - GitHub actions, nothing will be done, version obtained from RELEASE_NOTES will be used
// However, on local, we will search for local builds on NuGet cache and get the latest local build version and
// increment it. This will be the version metadata in which will be concatenated with version from RELEASE_NOTES.
// For example, if on machine, latest local build has version of fake.core.context.5.21.0-alpha005.local-2,
// then local version used will be fake.core.context.5.21.0-alpha005.local-3
let version =
    let segToString =
        function
        | PreReleaseSegment.AlphaNumeric n -> n
        | PreReleaseSegment.Numeric n -> string n

    let source, buildMeta =
        match BuildServer.buildServer with
        | BuildServer.GitHubActions -> [ yield PreReleaseSegment.AlphaNumeric "" ], ""
        | _ ->
            // from paket, increase versions even locally, this forces integration tests to always use latest packages.
            let GlobalPackagesFolderEnvironmentKey = "NUGET_PACKAGES"

            let getEnVar variable =
                let enVar = System.Environment.GetEnvironmentVariable variable

                if System.String.IsNullOrEmpty enVar then
                    None
                else
                    Some enVar

            let getEnvDir specialPath =
                let dir = System.Environment.GetFolderPath specialPath
                if System.String.IsNullOrEmpty dir then None else Some dir

            let LocalRootForTempData =
                getEnvDir System.Environment.SpecialFolder.UserProfile
                |> Option.orElse (getEnvDir System.Environment.SpecialFolder.LocalApplicationData)
                |> Option.defaultWith (fun _ ->
                    let fallback = Path.GetFullPath ".paket"

                    if not (Directory.Exists fallback) then
                        Directory.CreateDirectory fallback |> ignore

                    fallback)

            let UserNuGetPackagesFolder =
                getEnVar GlobalPackagesFolderEnvironmentKey
                |> Option.map (fun path -> path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
                |> Option.defaultWith (fun _ -> Path.Combine(LocalRootForTempData, ".nuget", "packages"))

            let currentVer =
                Directory.EnumerateDirectories(
                    Path.Combine(UserNuGetPackagesFolder, "fake.core.context"),
                    release.NugetVersion + "*local.*"
                )
                |> Seq.choose (fun dir ->
                    let n = Path.GetFileName dir
                    let v = n.Split("local.").[1]

                    match System.Numerics.BigInteger.TryParse(v) with
                    | true, v -> Some v
                    | _ ->
                        eprintfn "Could not parse '%s' to a bigint to retrieve the latest version (from '%s')" v dir
                        None)
                |> Seq.append [ 0I ]
                |> Seq.max

            let d = System.DateTime.Now
            let newLocalVersionNumber = currentVer + 1I
            [ PreReleaseSegment.AlphaNumeric("local." + newLocalVersionNumber.ToString()) ], d.ToString("yyyy-MM-dd-HH-mm")

    let semVer = SemVer.parse release.NugetVersion

    let prerelease =
        match semVer.PreRelease with
        | None ->
            let toAdd = String.Join(".", source |> Seq.map segToString)

            match String.IsNullOrWhiteSpace toAdd with
            | true -> None
            | false ->
                Some
                    { Name = ""
                      Values = source
                      Origin = toAdd }
        | Some p ->
            let toAdd = String.Join(".", source |> Seq.map segToString)
            let toAdd = if String.IsNullOrEmpty toAdd then toAdd else "." + toAdd

            Some
                { p with
                    Values = p.Values @ source
                    Origin = p.Origin + toAdd }

    let fromRepository =
        match prerelease with
        | Some _ ->
            { semVer with
                PreRelease = prerelease
                Original = None
                BuildMetaData = buildMeta }
        | None -> semVer

    match Environment.environVarOrNone "FAKE_VERSION" with
    | Some ver -> SemVer.parse ver
    | None -> fromRepository

let simpleVersion = version.AsString

let nugetVersion =
    match String.IsNullOrEmpty version.BuildMetaData with
    | true -> version.AsString
    | false -> sprintf "%s+%s" version.AsString version.BuildMetaData

let chocoVersion =
    // Replace "." with "-" in the prerelease-string
    let build =
        if version.Build > 0I then
            ("." + (let bi = version.Build in bi.ToString("D")))
        else
            ""

    let pre =
        match version.PreRelease with
        | Some preRelease -> ("-" + preRelease.Origin.Replace(".", "-"))
        | None -> ""

    let result =
        sprintf "%d.%d.%d%s%s" version.Major version.Minor version.Patch build pre

    if pre.Length > 20 then
        let msg =
            sprintf "Version '%s' is too long for chocolatey (Prerelease string is max 20 chars)" result

        Trace.traceError msg
        failwithf "%s" msg

    result

Trace.tracefn "FAKE build version: %s" simpleVersion
Trace.tracefn "FAKE NuGet build version: %s" nugetVersion
Trace.tracefn "FAKE Chocolatey build version: %s" chocoVersion

// DotNet host properties
let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson

/// <summary>
/// DotNet host with working directory set
/// </summary>
///
/// <param name="wd">The working directory to set for DotNet host</param>
let inline dotnetWorkingDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
    >> DotNet.Options.withTimeout (Some(System.TimeSpan.FromMinutes 20.))

/// <summary>
/// DotNet host with default values and given arguments
/// </summary>
///
/// <param name="arg">DotNet override arguments</param>
let inline dotnetSimple arg = DotNet.Options.lift dotnetSdk.Value arg

/// <summary>
/// Copy given artifact file or directory to artifact directory for build CI consumption
/// </summary>
///
/// <param name="artifact">The artifact file or directory to publish</param>
let collectArtifact artifact =
    Directory.ensure collectedArtifactsDir
    Shell.copyFile collectedArtifactsDir artifact

/// <summary>
/// Publish the given artifact file or directory on the build server
/// </summary>
///
/// <param name="artifact">The artifact file or directory to publish</param>
let publish artifact =
    if not (File.Exists artifact) && not (Directory.Exists artifact) then
        failwithf "The path '%s' is not a file and not a directory so the publish call failed!" artifact

    collectArtifact artifact
    Trace.publish ImportData.BuildArtifact artifact

/// <summary>
/// Clean integration test directories from compiled files/assemblies
/// </summary>
let cleanForTests () =
    let run workingDir fileName args =
        printfn "CWD: %s" workingDir

        let fileName, args =
            if Environment.isUnix then
                fileName, args
            else
                "cmd", ("/C " + fileName + " " + args)

        let processResult =
            CreateProcess.fromRawCommandLine fileName args
            |> CreateProcess.withWorkingDirectory workingDir
            |> CreateProcess.withTimeout System.TimeSpan.MaxValue
            |> Proc.run

        if processResult.ExitCode <> 0 then
            failwith (sprintf "'%s> %s %s' task failed" workingDir fileName args)

    let rmdir dir =
        if Environment.isUnix then
            Shell.rm_rf dir
        // Use this in Windows to prevent conflicts with paths too long
        else
            run "." "cmd" ("/C rmdir /s /q " + Path.GetFullPath dir)
    // Clean test directories
    !! "integrationtests/*/temp" |> Seq.iter rmdir

/// <summary>
/// Restores DotNet tools in FAKE repository
/// </summary>
let restoreTools =
    let alreadyRestored = ref false

    (fun () ->
        if not alreadyRestored.Value then
            DotNet.exec dotnetSimple "tool" "restore" |> ignore<ProcessResult>

            alreadyRestored.Value <- true)

/// <summary>
/// Call Paket with the given working directory and arguments
/// </summary>
///
/// <param name="wd">Working directory to execute Paket in</param>
/// <param name="args">Paket arguments</param>
let callPaket wd args =
    restoreTools ()

    let res = DotNet.exec (dotnetWorkingDir wd) "paket" args

    if not res.OK then
        failwithf "paket failed to start: %A" res

/// <summary>
/// Calls Expecto tool to run tests in the given working directory and DLL.
/// </summary>
///
/// <param name="workDir">Working directory to execute Expecto in</param>
/// <param name="dllPath">Test assembly to run tests from</param>
/// <param name="resultsXml">Expecto test results XML file</param>
let runExpecto workDir dllPath resultsXml =
    let resultsFile = "testresults" </> resultsXml

    let processResult =
        DotNet.exec (dotnetWorkingDir workDir) (sprintf "%s" dllPath) (sprintf "--nunit-summary %s" resultsFile)

    if processResult.ExitCode <> 0 then
        failwithf "Tests in %s failed." (Path.GetFileName dllPath)

    Trace.publish (ImportData.Nunit NunitDataVersion.Nunit) (workDir </> resultsXml)

/// <summary>
/// Get Chocolaty executable
/// </summary>
let getChocoWrapper () =
    let altToolPath = Path.GetFullPath "temp/choco.sh"

    if not Environment.isWindows then
        Directory.ensure "temp"

        File.WriteAllText(
            altToolPath,
            """#!/bin/bash
docker run --rm -v $PWD:$PWD -w $PWD linuturk/mono-choco $@
"""
        )

        let result = Shell.Exec("chmod", sprintf "+x %s" altToolPath)

        if result <> 0 then
            failwithf "'chmod +x %s' failed on unix" altToolPath

    altToolPath

//TODO:: see if we need to update the runtimes to newer versions of OSs
let runtimes = [ "win-x86"; "win-x64"; "osx-x64"; "linux-x64" ]

/// <summary>
/// Publishes the build artifacts for the given runtime
/// </summary>
///
/// <param name="runtimeName">The runtime to publish for</param>
let publishRuntime runtimeName =
    let runtimeDir = sprintf "%s/Fake.netcore/%s" nugetDncDir runtimeName

    let zipFile =
        sprintf "%s/Fake.netcore/fake-dotnetcore-%s.zip" nugetDncDir runtimeName

    !!(sprintf "%s/**" runtimeDir) |> Zip.zip runtimeDir zipFile

    publish zipFile

/// <summary>
/// Pushes the given NuGet package to NuGet registry
/// </summary>
///
/// <param name="tries">The number of re-tries</param>
/// <param name="nugetPackage">The NuGet package to push</param>
let rec nugetPush tries nugetPackage =
    let ignore_conflict = Environment.environVar "IGNORE_CONFLICT" = "true"

    try
        if not <| String.IsNullOrEmpty apiKey.Value then
            let quoteString str = sprintf "\"%s\"" str

            let args =
                sprintf
                    "push %s %s -Source %s %s"
                    (quoteString nugetPackage)
                    (quoteString apiKey.Value)
                    (quoteString nugetSource)
                    (if ignore_conflict then "-SkipDuplicate" else "")

            let errors = System.Collections.Generic.List<string>()
            let results = System.Collections.Generic.List<string>()

            let errorF msg =
                Trace.traceFAKE "%s" msg
                errors.Add msg

            let messageF msg =
                Trace.tracefn "%s" msg
                results.Add msg

            let processResult =
                CreateProcess.fromRawCommandLine nugetExe args
                |> CreateProcess.withTimeout (System.TimeSpan.FromMinutes 10.)
                |> CreateProcess.redirectOutput
                |> CreateProcess.withOutputEventsNotNull errorF messageF
                |> Proc.run
            
            if processResult.ExitCode <> 0 then
                if
                    not ignore_conflict
                    || not (errors |> Seq.exists (fun err -> err.Contains "409"))
                then
                    let msgs =
                        errors |> Seq.map (fun c -> "(Err) " + c)
                        |> Seq.append results |> Seq.map (fun c -> c)

                    let msg = String.Join("\n", msgs)

                    failwithf "failed to push package %s (code %d): \n%s" nugetPackage processResult.ExitCode msg
                else
                    Trace.traceFAKE "ignore conflict error because IGNORE_CONFLICT=true!"
            
        else
            Trace.traceFAKE "could not push '%s', because api key was not set" nugetPackage
    with exn when tries > 1 ->
        Trace.traceFAKE "Error while pushing NuGet package: %s" exn.Message
        nugetPush (tries - 1) nugetPackage

// ****************************************************************************************************
// --------------------------------------------- Targets ---------------------------------------------
// ****************************************************************************************************

Target.initEnvironment ()

Target.create "WorkaroundPaketNuspecBug" (fun _ ->
    // Workaround https://github.com/fsprojects/Paket/issues/2830
    // https://github.com/fsprojects/Paket/issues/2689
    // Basically paket fails if there is already an existing nuspec in obj/ dir because then MSBuild will call paket with multiple nuspec file arguments separated by ';'
    !! "src/*/*/obj/**/*.nuspec"
    -- (sprintf "src/*/*/obj/**/*%s.nuspec" nugetVersion)
    |> File.deleteAll)

Target.create "Clean" (fun _ ->
    !! "src/*/*/bin" |> Shell.cleanDirs

    let fakeRuntimeVersion =
        typeof<Fake.Core.Context.FakeExecutionContext>.Assembly.GetName().Version

    printfn "fake runtime %O" fakeRuntimeVersion

    if fakeRuntimeVersion < new System.Version(5, 10, 0) then
        printfn "deleting obj directories because of https://github.com/fsprojects/Paket/issues/3404"
        !! "src/*/*/obj" |> Shell.cleanDirs
        // Allow paket to do a full-restore (to improve performance)
        Shell.rm ("paket-files" </> "paket.restore.cached")
        callPaket "." "restore"

    Shell.cleanDirs
        [ nugetDncDir
          collectedArtifactsDir ]

    // Clean Data for tests
    cleanForTests ())

Target.create "CheckReleaseSecrets" (fun _ ->
    for secret in secrets do
        secret.Force() |> ignore)

Target.create "CheckFormatting" (fun _ ->
    let dotnetOptions = (fun (buildOptions:DotNet.Options) -> { buildOptions with RedirectOutput = false})
    let result =
     DotNet.exec id "fantomas" "src/app/ src/template/ src/test/ --recurse --check"

    if result.ExitCode = 0 then
        Trace.log "No files need formatting"
    elif result.ExitCode = 99 then
        failwith "Some files need formatting, please run \"dotnet fantomas  src/app/ src/template/ src/test/ --recurse\" to resolve this."
    else
        failwith "Errors while formatting"
)

// ----------------------------------------------------------------------------------------------------
// Documentation targets.

Target.create "GenerateDocs" (fun _ ->
    let source = "./docs"
    
    Shell.cleanDir ".fsdocs"
    Directory.ensure "output"

    let projInfo =
        seq {
          ("root", docsDomain)
          ("fsdocs-logo-src", docsDomain @@ "content/img/logo.svg")
          ("fsdocs-fake-version", simpleVersion)
        }

    File.writeString false "./output/.nojekyll" ""
    File.writeString false "./output/CNAME" docsDomain
    Shell.copy (source @@ "guide") [ "RELEASE_NOTES.md" ]

    try
        Npm.install (fun o -> { o with WorkingDirectory = "./docs" })
        
        Npm.run "build" (fun o -> { o with WorkingDirectory = "./docs" })

        Shell.copy "./output" [source </> "robots.txt"]

        // renaming node_modules directory so that fsdocs skip it when generating site.
        Directory.Move("./docs/node_modules", "./docs/.node_modules")

        let command = sprintf "build --clean --input ./docs --saveimages --properties Configuration=release --parameters fsdocs-logo-src %s fsdocs-fake-version %s" (docsDomain @@ "content/img/logo.svg") simpleVersion
        DotNet.exec id "fsdocs" command |> ignore

        // Fsdocs.build (fun p -> { p with
        //                             Input = Some(source)
        //                             SaveImages = Some(true)
        //                             Clean = Some(true)
        //                             Parameters = Some projInfo
        //                             Properties = Some "Configuration=debug"
        //                             //Strict = Some(true)
        // })
        
    finally
        // clean up
        Shell.rm (source </> "guide/RELEASE_NOTES.md")

        // renaming node_modules directory back after fsdocs generated site.
        Directory.Move("./docs/.node_modules", "./docs/node_modules")


    // validate site generation and ensure all components are generated successfully.
    if DirectoryInfo.ofPath("./output/guide").GetFiles().Length = 0 then failwith "site generation failed due to missing guide directory"
    if DirectoryInfo.ofPath("./output/reference").GetFiles().Length = 0 then failwith "site generation failed due to missing reference directory"
    if DirectoryInfo.ofPath("./output/articles").GetFiles().Length = 0 then failwith "site generation failed due to missing articles directory"
    if not (File.exists("./output/data.json")) then failwith "site generation failed due to missing data.json file"
    if not (File.exists("./output/guide/RELEASE_NOTES.html")) then failwith "site generation failed due to missing RELEASE_NOTES.html file"
    if not (File.exists("./output/guide.html")) then failwith "site generation failed due to missing guide.html file"
    if not (File.exists("./output/index.html")) then failwith "site generation failed due to missing index.html file"

    // prepare artifact
    Directory.ensure "temp"
    
    !!("output" </> "**/*")
    |> Zip.zip docsDir "temp/docs.zip"
    publish "temp/docs.zip")

Target.create "HostDocs" (fun _ ->
    let source = "./docs"

    try
        Npm.install (fun o -> { o with WorkingDirectory = "./docs" })

        Npm.run "build" (fun o -> { o with WorkingDirectory = "./docs" })

        Shell.copy (source @@ "guide") [ "RELEASE_NOTES.md" ]

        Shell.copy "./output" [source </> "robots.txt"]
        
        // renaming node_modules directory so that fsdocs skip it when generating site.
        Directory.Move("./docs/node_modules", "./docs/.node_modules")

        let command = sprintf "watch --input ./docs --saveimages --properties Configuration=release --parameters fsdocs-logo-src %s fsdocs-fake-version %s" (docsDomain @@ "content/img/logo.svg") simpleVersion
        DotNet.exec id "fsdocs" command |> ignore

        // Fsdocs.watch id

    finally
        // clean up
        Shell.rm (source </> "guide/RELEASE_NOTES.md")

        // renaming node_modules directory back after fsdocs generated site.
        Directory.Move("./docs/.node_modules", "./docs/node_modules")
    
)

// ----------------------------------------------------------------------------------------------------
// Test targets.

Target.create "DotNetCoreIntegrationTests" (fun _ ->
    cleanForTests ()

    runExpecto
        root
        ("src" </> "test" </> "Fake.Core.IntegrationTests" </> "bin" </> "Release" </> "net6.0" </> "Fake.Core.IntegrationTests.dll")
        "Fake_Core_IntegrationTests.TestResults.xml")

Target.create "TemplateIntegrationTests" (fun _ ->

    runExpecto
        root
        ("src" </> "test" </> "Fake.DotNet.Cli.IntegrationTests" </> "bin" </> "Release" </> "net6.0" </> "Fake.DotNet.Cli.IntegrationTests.dll")
        "Fake_DotNet_Cli_IntegrationTests.TestResults.xml"
    
    Shell.rm_rf (root </> "test"))

Target.create "DotNetCoreUnitTests" (fun _ ->
    // dotnet run -p src/test/Fake.Core.UnitTests/Fake.Core.UnitTests.fsproj
    runExpecto
        root
        ("src" </> "test" </> "Fake.Core.UnitTests" </> "bin" </> "Release" </> "net6.0" </> "Fake.Core.UnitTests.dll")
        "Fake_Core_UnitTests.TestResults.xml"

    // dotnet run --project src/test/Fake.Core.CommandLine.UnitTests/Fake.Core.CommandLine.UnitTests.fsproj
    runExpecto
        root
        ("src" </> "test" </> "Fake.Core.CommandLine.UnitTests" </> "bin" </> "Release" </> "net6.0" </> "Fake.Core.CommandLine.UnitTests.dll")
        "Fake_Core_CommandLine_UnitTests.TestResults.xml")

// ----------------------------------------------------------------------------------------------------
// Bootstrap Fake targets; These targets will be used as a sort of a dog-food for FAKE. We will use the
// built FAKE runner and call it will targets to validate it.

Target.create "BootstrapFake_PrintColors" (fun _ ->
    let color (color: ConsoleColor) (code: unit -> _) =
        let before = Console.ForegroundColor

        try
            Console.ForegroundColor <- color
            code ()
        finally
            Console.ForegroundColor <- before

    color ConsoleColor.Magenta (fun _ -> printfn "TestMagenta"))

Target.create "BootstrapFake_FailFast" (fun _ -> failwith "Bootstrap FAKE Fail Fast")

Target.create "BootstrapFake" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
    let testScriptLock = "testbuild.fsx.lock"

    // Check if we can build ourself with the new binaries.
    let test timeout clearCache script =
        let clear () =
            // Will make sure the test call actually compiles the script.
            // Note: We cannot just clean .fake here as it might be locked by the currently executing code :)
            [ ".fake/testbuild.fsx/packages"
              ".fake/testbuild.fsx/paket.depedencies.sha1"
              ".fake/testbuild.fsx/paket.lock"
              "testbuild.fsx.lock" ]
            |> List.iter Shell.rm_rf

            !! ".fake/testbuild.fsx/testbuild_*" |> Seq.iter Shell.rm_rf

        let executeTarget target =
            if clearCache then
                clear ()

            let fileName =
                if Environment.isUnix then
                    nugetDncDir </> "Fake.netcore/current/fake"
                else
                    nugetDncDir </> "Fake.netcore/current/fake.exe"


            let processResult =
                CreateProcess.fromRawCommandLine fileName (sprintf "run --fsiargs \"--define:BOOTSTRAP\" %s --target %s" script target)
                |> CreateProcess.withWorkingDirectory "."
                |> CreateProcess.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
                |> CreateProcess.withTimeout timeout
                |> Proc.run
                
            processResult.ExitCode
            
        let result = executeTarget "BootstrapFake_PrintColors"

        if result <> 0 then
            failwithf "Bootstrapping failed for target 'BootstrapFake_PrintColors' (because of exit code %d)" result

        let result = executeTarget "BootstrapFake_FailFast"

        if result = 0 then
            failwithf "Bootstrapping failed for target 'BootstrapFake_FailFast' (because of exit code %d)" result

    File.ReadAllText buildScript |> fun text -> File.WriteAllText(testScript, text)

    try
        // Will compile the script.
        test (System.TimeSpan.FromMinutes 15.0) true testScript
        // Will use the compiled/cached version.
        test (System.TimeSpan.FromMinutes 3.0) false testScript
    finally
        ignore ""
        File.Delete(testScript)
        File.Delete(testScriptLock))

// ----------------------------------------------------------------------------------------------------
// Publishing targets; For each OS runtime we will publish FAKE as well as a portable format.

Target.create "_DotNetPublish_portable" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir

    // Publish portable as well (see https://docs.microsoft.com/en-us/dotnet/articles/core/app-types)
    let netcoreFsproj = appDir </> "Fake.netcore/Fake.netcore.fsproj"
    let outDir = nugetDir @@ "Fake.netcore" @@ "portable"

    DotNet.publish
        (fun c ->
            { c with
                Framework = Some "net6.0"
                OutputPath = Some outDir }
            |> dotnetSimple)
        netcoreFsproj

    publishRuntime "portable")

// Create publishing target for each runtime
let info = lazy DotNet.info dotnetSimple

runtimes
|> List.map Some
|> (fun rs -> None :: rs)
|> Seq.iter (fun runtime ->
    let runtimeName, runtime =
        match runtime with
        | Some r -> r, lazy r
        | None -> "current", lazy info.Value.RID

    let targetName = sprintf "_DotNetPublish_%s" runtimeName

    Target.create targetName (fun _ ->
        !!(appDir </> "Fake.netcore/Fake.netcore.fsproj")
        |> Seq.iter (fun proj ->
            let nugetDir = System.IO.Path.GetFullPath nugetDncDir
            let projName = Path.GetFileName(Path.GetDirectoryName proj)

            let outDir = nugetDir @@ projName @@ runtimeName

            DotNet.publish
                (fun c ->
                    { c with
                        Runtime = Some runtime.Value
                        Configuration = DotNet.Release
                        OutputPath = Some outDir 
                        Framework = Some "net6.0"
                        // DisableInternalBinLog: https://github.com/fsprojects/FAKE/issues/2722
                        MSBuildParams = { MSBuild.CliArguments.Create() with DisableInternalBinLog = true }}
                    |> dotnetSimple)
                proj

            let source = outDir </> "dotnet"

            if File.Exists source then
                failwithf "Workaround no longer required?" //TODO: If this is not triggered delete this block
                Trace.traceFAKE "Workaround https://github.com/dotnet/cli/issues/6465"
                let target = outDir </> "fake"

                if File.Exists target then
                    File.Delete target

                File.Move(source, target)

            // Create zip
            if runtimeName <> "current" then
                publishRuntime runtimeName)))

// ----------------------------------------------------------------------------------------------------
// Package creation targets; Create NuGet, Debian, and Chocolatey packages for FAKE

Target.create "CacheDotNetReleases" (fun _ ->
    let sdkVersionReleases =
        ProductCollection.GetAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> List.ofSeq
        |> List.find (fun product -> product.ProductVersion.Equals("6.0"))

    let client = new HttpClient()

    try
        let response =
            sdkVersionReleases.ReleasesJson
            |> client.GetAsync
            |> Async.AwaitTask
            |> Async.RunSynchronously

        response.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Fake.IO.File.writeString false "src/app/Fake.Runtime/cachedDotnetSdkReleases.json"
    with e ->
        failwith "Could not update DotNet releases file.")

Target.create "DotNetCreateNuGetPackage" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir
    // This lines actually ensures we get the correct version checked in
    // instead of the one previously bundled with `fake` or `paket`
    callPaket "." "restore" // first make paket restore its target file if it feels like it.
    Git.CommandHelper.gitCommand "" "checkout .paket/Paket.Restore.targets" // now restore ours

    restoreTools ()

    // dotnet pack
    DotNet.pack
        (fun c ->
            { c with
                Configuration = DotNet.Release
                OutputPath = Some nugetDir
                Common = c.Common
                MSBuildParams =
                    { c.MSBuildParams with
                        Properties =
                            [ ("Version", nugetVersion)
                              ("PackageReleaseNotes", release.Notes |> String.toLines) ] 
                        // DisableInternalBinLog: https://github.com/fsprojects/FAKE/issues/2722
                        DisableInternalBinLog = true } }
            |> dotnetSimple)
        "Fake.sln"

    // build zip package
    Directory.ensure (nugetDncDir </> "Fake.netcore")

    let zipFile = nugetDncDir </> "Fake.netcore/fake-dotnetcore-packages.zip"

    !!(nugetDncDir </> "*.nupkg") -- (nugetDncDir </> "*.symbols.nupkg")
    |> Zip.zip nugetDncDir zipFile

    publish zipFile

    Directory.ensure "temp"
    let testZip = "temp/tests.zip"

    !! "src/test/*/bin/Release/net6.0/**" |> Zip.zip "src/test" testZip

    publish testZip)

Target.create "DotNetCreateChocolateyPackage" (fun _ ->
    let altToolPath = getChocoWrapper ()

    let changeToolPath (p: Choco.ChocoPackParams) =
        if Environment.isWindows then
            p
        else
            { p with ToolPath = altToolPath }

    Directory.ensure chocoReleaseDir

    Choco.packFromTemplate
        (fun p ->
            { p with
                PackageId = "fake"
                ReleaseNotes = release.Notes |> String.toLines
                InstallerType = Choco.ChocolateyInstallerType.SelfContained
                Version = chocoVersion
                Files =
                    [ System.IO.Path.GetFullPath(nugetDncDir </> @"Fake.netcore\win-x86") + @"\**", Some "bin", None
                      (System.IO.Path.GetFullPath @"src\VERIFICATION.txt"), Some "VERIFICATION.txt", None
                      (System.IO.Path.GetFullPath @"License.txt"), Some "LICENSE.txt", None ]
                OutputDir = chocoReleaseDir }
            |> changeToolPath)
        "src/Fake-choco-template.nuspec"

    let name = sprintf "%s.%s" "fake" chocoVersion
    let chocoPackage = sprintf "%s/%s.nupkg" chocoReleaseDir name
    let chocoTargetPackage = sprintf "%s/chocolatey-%s.nupkg" chocoReleaseDir name
    File.Copy(chocoPackage, chocoTargetPackage, true)
    publish chocoTargetPackage)

Target.create "DotNetCreateDebianPackage" (fun _ ->
    let runtime = "linux-x64"
    let targetFramework = "net6.0"

    let args =
        [ sprintf "--runtime %s" runtime
          sprintf "--framework %s" targetFramework
          sprintf "--configuration %s" "Release"
          sprintf "--output %s" (Path.GetFullPath nugetDncDir)
          "--no-restore" ]
        |> String.concat " "

    Environment.setEnvironVar "PackageVersion" simpleVersion
    Environment.setEnvironVar "Version" simpleVersion
    Environment.setEnvironVar "IsDebianPackaging" "true"

    let result =
        DotNet.exec (fun opt -> { opt with WorkingDirectory = "src/app/fake-cli/" } |> dotnetSimple) "deb" args

    if not result.OK then
        failwith "Debian package creation failed"


    let fileName = sprintf "fake-cli.%s.%s.deb" simpleVersion runtime
    let target = sprintf "%s/%s" nugetDncDir fileName
    publish target)

// ----------------------------------------------------------------------------------------------------
// Releasing targets; Publishing NuGet and Chocolatey packages and other things when we do a release

Target.create "DotNetPushChocolateyPackage" (fun _ ->
    let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
    let path = sprintf "%s/%s" chocoReleaseDir name
    let ignore_conflict = Environment.environVar "IGNORE_CONFLICT" = "true"

    if not Environment.isWindows && not (File.exists path) && fromArtifacts then
        Directory.ensure chocoReleaseDir
        Shell.copyFile path (artifactsDir </> sprintf "chocolatey-%s" name)

    let altToolPath = getChocoWrapper ()

    let changeToolPath (p: Choco.ChocoPushParams) =
        if Environment.isWindows then
            p
        else
            { p with ToolPath = altToolPath }

    try
        path
        |> Choco.push (fun p ->
            { p with
                Source = chocoSource
                ApiKey = chocoKey.Value }
            |> changeToolPath)
    with exn when ignore_conflict ->
        Trace.traceFAKE "ignore conflict error because IGNORE_CONFLICT=true!")

Target.create "DotNetPushToNuGet" (fun _ ->
    !!(appDir </> "*/*.fsproj") -- (appDir </> "Fake.netcore/*.fsproj")
    ++ (templateDir </> "*/*.fsproj")
    |> Seq.iter (fun proj ->
        let projName = Path.GetFileName(Path.GetDirectoryName proj)

        !!(sprintf "%s/%s.*.nupkg" nugetDncDir projName)
        -- (sprintf "%s/%s.*.symbols.nupkg" nugetDncDir projName)
        |> Seq.iter (nugetPush 4)))

Target.create "ReleaseDocs" (fun _ ->
    Shell.cleanDir "gh-pages"
    let auth = sprintf "x-access-token:%s@" githubToken.Value
    let url = sprintf "https://%sgithub.com/%s/%s.git" auth githubReleaseUser gitName
    Git.Repository.cloneSingleBranch "" url "gh-pages" "gh-pages"

    Git.Repository.fullClean "gh-pages"

    Shell.copyRecursive "output" "gh-pages" true |> printfn "%A"

    File.writeString false "./gh-pages/CNAME" docsDomain
    Git.Staging.stageAll "gh-pages"

    if not BuildServer.isLocalBuild then
        Git.CommandHelper.directRunGitCommandAndFail "gh-pages" "config user.email matthi.d@gmail.com"
        Git.CommandHelper.directRunGitCommandAndFail "gh-pages" "config user.name \"Matthias Dittrich\""

    Git.Commit.exec "gh-pages" (sprintf "Update generated documentation %s" simpleVersion)
    Git.Branches.pushBranch "gh-pages" url "gh-pages")

Target.create "GitHubRelease" (fun _ ->
    let token = githubToken.Value
    let auth = sprintf "%s:x-oauth-basic@" token
    let url = sprintf "https://%sgithub.com/%s/%s.git" auth githubReleaseUser gitName

    let gitDirectory = getVarOrDefaultFromVault "GIT_DIRECTORY" ""

    if not BuildServer.isLocalBuild then
        Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.email matthi.d@gmail.com"
        Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.name \"Matthias Dittrich\""

    Git.Branches.tag gitDirectory simpleVersion
    Git.Branches.pushTag gitDirectory url simpleVersion

    let linuxRuntime = "linux-x64"
    let debFileName = sprintf "fake-cli.%s.%s.deb" simpleVersion linuxRuntime
    let debTarget = sprintf "%s/%s" nugetDncDir debFileName

    let files =
        runtimes @ [ "portable"; "packages" ]
        |> List.map (fun n -> sprintf "%s/Fake.netcore/fake-dotnetcore-%s.zip" nugetDncDir n)
        |> fun l -> l @ [ debTarget ]

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease githubReleaseUser gitName simpleVersion (release.SemVer.PreRelease <> None) release.Notes
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
    
    let bumpVersionMessage = (sprintf "Bump version to %s" simpleVersion)
    let branch = "bump-version-to-" + simpleVersion
    Git.Staging.stageAll ".config"
    Git.Commit.exec gitDirectory bumpVersionMessage
    Git.Branches.checkoutNewBranch gitDirectory "master" branch
    Git.Branches.pushBranch gitDirectory "origin" branch

    // when we release the GitHub module, this will be replaced with GitHub.createPullRequest API
    let pullRequest = new NewPullRequest(bumpVersionMessage, branch, "master")
    let pullRequestTask (client: GitHubClient) =
        client.PullRequest.Create(githubReleaseUser, gitName, pullRequest) |> Async.AwaitTask |> Async.RunSynchronously
        
    GitHub.createClientWithToken token
    |> Async.RunSynchronously
    |> pullRequestTask
    |> ignore)

// ----------------------------------------------------------------------------------------------------
// Artifact targets; Preparing artifacts for release from existing artifacts

Target.create "PrepareArtifacts" (fun _ ->
    if not fromArtifacts then
        Trace.trace "empty artifactsDir."
    else
        Trace.trace "ensure artifacts."

        let files = !!(artifactsDir </> "fake-dotnetcore-*.zip") |> Seq.toList

        Trace.tracefn "files: %A" files

        files |> Shell.copy (nugetDncDir </> "Fake.netcore")

        Zip.unzip nugetDncDir (artifactsDir </> "fake-dotnetcore-packages.zip")

        if Environment.isWindows then
            Directory.ensure chocoReleaseDir
            let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
            Shell.copyFile (sprintf "%s/%s" chocoReleaseDir name) (artifactsDir </> sprintf "chocolatey-%s" name)
        else
            Zip.unzip "." (artifactsDir </> "chocolatey-requirements.zip")

        let linuxRuntime = "linux-x64"
        let debFileName = sprintf "fake-cli.%s.%s.deb" simpleVersion linuxRuntime
        Directory.ensure nugetDncDir
        let debTarget = sprintf "%s/%s" nugetDncDir debFileName
        Shell.copyFile debTarget (artifactsDir </> debFileName)

        let unzipIfExists dir file =
            Directory.ensure dir

            if File.Exists file then
                Zip.unzip dir file

        // File is not available in case we already have build the full docs
        unzipIfExists "help" (artifactsDir </> "help-markdown.zip")
        unzipIfExists "docs" (artifactsDir </> "docs.zip")
        unzipIfExists "src/test" (artifactsDir </> "tests.zip"))

Target.create "BuildArtifacts" (fun args ->
    Directory.ensure "temp"

    if not Environment.isWindows then
        // Chocolatey package is done in a separate step...
        let chocoReq = "temp/chocolatey-requirements.zip"

        !! @"src\VERIFICATION.txt" ++ @"License.txt" ++ "src/Fake-choco-template.nuspec"
        |> Zip.zip "." chocoReq

        publish chocoReq

    let buildCache = "temp/build-cache.zip"

    !!(".fake" </> "build.fsx" </> "*.dll")
    ++ (".fake" </> "build.fsx" </> "*.pdb")
    ++ "build.fsx"
    ++ "paket.dependencies"
    ++ "paket.lock"
    ++ "RELEASE_NOTES.md"
    |> Zip.zip "." buildCache

    publish buildCache

    if args.Context.TryFindPrevious "Release_GenerateDocs" |> Option.isNone then
        // When Release_GenerateDocs is missing upload markdown (for later processing)
        let helpZip = "temp/help-markdown.zip"
        !!("help" </> "**") |> Zip.zip "help" helpZip
        publish helpZip)

Target.description "Generate the docs (potentially from artifacts) and publish as artifact."

Target.create "Release_GenerateDocs" (fun _ ->
    let testZip = "temp/docs.zip"
    !! "docs/**" |> Zip.zip "docs" testZip
    publish testZip)

Target.create "EnsureTestsRun" ignore

Target.description "Default Build all artifacts and documentation"
Target.create "Default" ignore

Target.description "Simple local command line release"
Target.create "Release" ignore

Target.description "Release after build, skip build since it is assumed to be done"
Target.create "FastRelease" ignore

Target.description "Grouping for targets that need to run before starting the build"
Target.create "BeforeBuild" ignore

Target.description "Grouping for targets that need to run during DotNet build"
Target.create "DotNetPackage" ignore

Target.description "publish fake runner for various platforms"
Target.create "DotNetPublish" ignore

Target.description "Grouping for targets that need to run after finishing the build"
Target.create "AfterBuild" ignore

Target.description "Build and test the dotnet sdk part"
Target.create "FullDotNetCore" ignore

Target.description "Run the tests - if artifacts are available via 'ARTIFACTS_DIRECTORY' those are used."
Target.create "RunTests" ignore

Target.description "Full Build & Test and publish results as artifacts."
Target.create "Release_BuildAndTest" ignore

// ****************************************************************************************************
// --------------------------------------- Targets Dependencies ---------------------------------------
// ****************************************************************************************************

let mutable prev = None

for runtime in "current" :: "portable" :: runtimes do
    let rawTargetName = sprintf "_DotNetPublish_%s" runtime
    let targetName = sprintf "DotNetPublish_%s" runtime
    Target.description (sprintf "publish FAKE runner for %s" runtime)
    Target.create targetName ignore

    rawTargetName ==> targetName |> ignore
    "BeforeBuild" ==> targetName |> ignore
    targetName ==> "DotNetPublish" |> ignore

    // Make sure we order then (when building parallel!)
    match prev with
    | Some prev -> prev ?=> rawTargetName |> ignore
    | None -> "DotNetCreateNuGetPackage" ?=> rawTargetName |> ignore

    prev <- Some rawTargetName

"CheckFormatting" ==> "Clean"

"CheckReleaseSecrets" ?=> "Clean"
"WorkaroundPaketNuspecBug" ==> "Clean"
"WorkaroundPaketNuspecBug" ==> "DotNetCreateNuGetPackage"

"Clean" ?=> "BeforeBuild" ?=> "DotNetCreateNuGetPackage" ==> "DotNetPackage"

"BeforeBuild" ==> "CacheDotNetReleases" ==> "DotNetCreateNuGetPackage"

"DotNetCreateNuGetPackage" ==> "DotNetPackage"

"DotNetPackage" ==> "AfterBuild"
"DotNetPublish" ==> "AfterBuild"

// Create artifacts when build is finished
"AfterBuild" =?> ("DotNetCreateChocolateyPackage", Environment.isWindows)
==> "DotNetCreateDebianPackage"
=?> ("GenerateDocs", BuildServer.isLocalBuild && Environment.isWindows)
==> "Default"

(if fromArtifacts then "PrepareArtifacts" else "AfterBuild")
=?> ("GenerateDocs", not <| Environment.hasEnvironVar "SkipDocs")
==> "Default"

"AfterBuild" ?=> "GenerateDocs"

"GenerateDocs" ==> "Release_GenerateDocs"

// Build artifacts only (no testing)
"DotNetCreateChocolateyPackage" =?> ("BuildArtifacts", Environment.isWindows)


// Test the dotnetcore build
(if fromArtifacts then
     "PrepareArtifacts"
 else
     "DotNetCreateNuGetPackage")
=?> ("DotNetCoreUnitTests", not <| Environment.hasEnvironVar "SkipTests")
==> "FullDotNetCore"

"DotNetCreateNuGetPackage" ?=> "DotNetCoreUnitTests"

"DotNetCoreUnitTests" ==> "RunTests"

(if fromArtifacts then
     "PrepareArtifacts"
 else
     "_DotNetPublish_current")
=?> ("DotNetCoreIntegrationTests",
     not <| Environment.hasEnvironVar "SkipIntegrationTests"
     && not <| Environment.hasEnvironVar "SkipTests")
==> "FullDotNetCore"

"_DotNetPublish_current" ?=> "DotNetCoreIntegrationTests"

"DotNetCoreIntegrationTests" ==> "RunTests"

(if fromArtifacts then
     "PrepareArtifacts"
 else
     "DotNetCreateNuGetPackage")
=?> ("DotNetCoreIntegrationTests",
     not <| Environment.hasEnvironVar "SkipIntegrationTests"
     && not <| Environment.hasEnvironVar "SkipTests")

"DotNetCreateNuGetPackage" ?=> "DotNetCoreIntegrationTests"

(if fromArtifacts then
     "PrepareArtifacts"
 else
     "_DotNetPublish_current")
=?> ("BootstrapFake", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
==> "FullDotNetCore"

"_DotNetPublish_current" ?=> "BootstrapFake"

"BootstrapFake" ==> "RunTests"

"DotNetPackage"
==> "TemplateIntegrationTests"
==> "FullDotNetCore"
==> "Default"

// Artifacts & Tests
"Default" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ?=> "BuildArtifacts"
"BuildArtifacts" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ==> "Release_BuildAndTest"


// Release stuff ('GitHubRelease' is to release after running 'Default')
(if fromArtifacts then
     "PrepareArtifacts"
 else
     "EnsureTestsRun")
=?> ("DotNetPushChocolateyPackage", Environment.isWindows && chocoSource <> "disabled")
==> "GitHubRelease"

"EnsureTestsRun" ?=> "DotNetPushChocolateyPackage"

(if fromArtifacts then
     "PrepareArtifacts"
 else
     "EnsureTestsRun")
=?> ("ReleaseDocs", not <| Environment.hasEnvironVar "SkipDocs")
==> "GitHubRelease"

"EnsureTestsRun" ?=> "ReleaseDocs"

(if fromArtifacts then
     "PrepareArtifacts"
 else
     "EnsureTestsRun")
=?> ("DotNetPushToNuGet", nugetSource <> "disabled")
==> "GitHubRelease"

if nugetSource <> "disabled" then
    ignore ("EnsureTestsRun" ?=> "DotNetPushToNuGet")

// If 'Default' happens it needs to happen before 'EnsureTestsRun'
"Default" ?=> "EnsureTestsRun"

// A 'Default' includes a 'Clean'
"Clean" ==> "Default"

"GitHubRelease" ==> "FastRelease"
// A 'Release' includes a 'Default'
"Default" ==> "Release"
// A 'Release' includes a 'GitHubRelease'
"GitHubRelease" ==> "Release"
// A 'Release' includes a 'CheckReleaseSecrets'
"CheckReleaseSecrets" ==> "Release"

//start build
Target.runOrDefault "Default"
