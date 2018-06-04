#if BOOTSTRAP && DOTNETCORE

#r "paket:
source nuget/dotnetcore
source https://api.nuget.org/v3/index.json
nuget FSharp.Core ~> 4.1
nuget System.AppContext prerelease
nuget Paket.Core prerelease
nuget Fake.Api.GitHub prerelease
nuget Fake.BuildServer.AppVeyor prerelease
nuget Fake.BuildServer.TeamCity prerelease
nuget Fake.BuildServer.Travis prerelease
nuget Fake.BuildServer.TeamFoundation prerelease
nuget Fake.BuildServer.GitLab prerelease
nuget Fake.Core.Target prerelease
nuget Fake.Core.SemVer prerelease
nuget Fake.IO.FileSystem prerelease
nuget Fake.IO.Zip prerelease
nuget Fake.Core.ReleaseNotes prerelease
nuget Fake.DotNet.AssemblyInfoFile prerelease
nuget Fake.DotNet.MSBuild prerelease
nuget Fake.DotNet.Cli prerelease
nuget Fake.DotNet.NuGet prerelease
nuget Fake.DotNet.Paket prerelease
nuget Fake.DotNet.FSFormatting prerelease
nuget Fake.DotNet.Testing.MSpec prerelease
nuget Fake.DotNet.Testing.XUnit2 prerelease
nuget Fake.DotNet.Testing.NUnit prerelease
nuget Fake.Windows.Chocolatey prerelease
nuget Fake.Tools.Git prerelease
nuget Mono.Cecil prerelease
nuget System.Reactive.Compatibility
nuget Suave
nuget Newtonsoft.Json
nuget Octokit //"
#endif


#if DOTNETCORE
// We need to use this for now as "regular" Fake breaks when its caching logic cannot find "intellisense.fsx".
// This is the reason why we need to checkin the "intellisense.fsx" file for now...
#load ".fake/build.fsx/intellisense.fsx"

open System.Reflection

#else
// Load this before FakeLib, see https://github.com/fsharp/FSharp.Compiler.Service/issues/763
#r "packages/Mono.Cecil/lib/net40/Mono.Cecil.dll"
#I "packages/build/FAKE/tools/"
#r "FakeLib.dll"
#r "Paket.Core.dll"
#r "packages/build/System.Net.Http/lib/net46/System.Net.Http.dll"
#r "packages/build/Octokit/lib/net45/Octokit.dll"
#r "packages/build/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#I "packages/build/SourceLink.Fake/tools/"

#r "System.IO.Compression"
//#load "packages/build/SourceLink.Fake/tools/SourceLink.fsx"

#endif

//#if !FAKE
//let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
//Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
//#endif
#load "src/app/Fake.DotNet.Cli/DotNet.fs"
open System.IO
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

// Set this to true if you have lots of breaking changes, for small breaking changes use #if BOOTSTRAP, setting this flag will not be accepted
let disableBootstrap = false

// properties
let projectName = "FAKE"
let projectSummary = "FAKE - F# Make - Get rid of the noise in your build scripts."
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"; "Matthias Dittrich"]
let github_release_user = Environment.environVarOrDefault "github_release_user" "fsharp"

// The name of the project on GitHub
let gitName = "FAKE"

let release = ReleaseNotes.load "RELEASE_NOTES.md"

(*
let version =
    let semVer = SemVer.parse release.NugetVersion
    match semVer.PreRelease with
    | None -> ()
    | _ -> ()*)

let packages =
    ["FAKE.Core",projectDescription
     "FAKE.Gallio",projectDescription + " Extensions for Gallio"
     "FAKE.IIS",projectDescription + " Extensions for IIS"
     "FAKE.FluentMigrator",projectDescription + " Extensions for FluentMigrator"
     "FAKE.SQL",projectDescription + " Extensions for SQL Server"
     "FAKE.Experimental",projectDescription + " Experimental Extensions"
     "Fake.Deploy.Lib",projectDescription + " Extensions for FAKE Deploy"
     projectName,projectDescription + " This package bundles all extensions."
     "FAKE.Lib",projectDescription + " FAKE helper functions as library"]

let buildDir = "./build"
let testDir = "./test"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"
let nugetDncDir = "./nuget/dotnetcore"
let nugetLegacyDir = "./nuget/legacy"
let reportDir = "./report"
let packagesDir = "./packages"
let buildMergedDir = buildDir </> "merged"

let root = __SOURCE_DIRECTORY__
let srcDir = root</>"src"
let appDir = srcDir</>"app"
let legacyDir = srcDir</>"legacy"

let additionalFiles = [
    "License.txt"
    "README.markdown"
    "RELEASE_NOTES.md"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.sigdata"
    "./packages/FSharp.Core/lib/net45/FSharp.Core.optdata"]
let nuget_exe = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "NuGet.CommandLine" </> "tools" </> "NuGet.exe"

let nugetsource = Environment.environVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
let chocosource = Environment.environVarOrDefault "chocosource" "https://push.chocolatey.org/"
let artifactsDir = Environment.environVarOrDefault "artifactsdirectory" ""
let docsDomain = Environment.environVarOrDefault "docs_domain" "fake.build"
let fromArtifacts = not <| String.isNullOrEmpty artifactsDir

let mutable secrets = []
let releaseSecret replacement name =
    let secret =
        lazy
            let env = Environment.environVarOrFail name
            TraceSecrets.register replacement env
            env
    secrets <- secret :: secrets
    secret

let apikey = releaseSecret "<nugetkey>" "nugetkey"
let chocoKey = releaseSecret "<chocokey>" "CHOCOLATEY_API_KEY"
let githubtoken = releaseSecret "<githubtoken>" "github_token"

BuildServer.install [
    AppVeyor.Installer
    TeamCity.Installer
    Travis.Installer
    TeamFoundation.Installer
#if DOTNETCORE
    GitLab.Installer
#endif
]

let version =
    let segToString = function
        | PreReleaseSegment.AlphaNumeric n -> n
        | PreReleaseSegment.Numeric n -> string n
    //let createAlphaNum (s:string) =
    //    PreReleaseSegment.AlphaNumeric (s.Replace("_", "-").Replace("+", "-"))
    let source, buildMeta =
        match BuildServer.buildServer with
#if DOTNETCORE
        | BuildServer.GitLabCI ->
            // Workaround for now
            // We get CI_COMMIT_REF_NAME=master and CI_COMMIT_SHA
            // Too long for chocolatey (limit = 20) and we don't strictly need it.
            //let branchPath =
            //    MyGitLab.Environment.CommitRefName.Split('/')
            //    |> Seq.map createAlphaNum
            [ //yield! branchPath
              //yield PreReleaseSegment.AlphaNumeric "gitlab"
              yield PreReleaseSegment.AlphaNumeric GitLab.Environment.PipelineId
            ], sprintf "gitlab.%s" GitLab.Environment.CommitSha
#endif
        | BuildServer.TeamFoundation ->
            let sourceBranch = TeamFoundation.Environment.BuildSourceBranch
            let isPr = sourceBranch.StartsWith "refs/pull/"
            let firstSegment =
                if isPr then
                    let splits = sourceBranch.Split '/'
                    let prNum = bigint (int splits.[2])
                    [ PreReleaseSegment.AlphaNumeric "pr"; PreReleaseSegment.Numeric prNum ]
                else
                    // Too long for chocolatey (limit = 20) and we don't strictly need it.
                    //let branchPath = sourceBranch.Split('/') |> Seq.skip 2 |> Seq.map createAlphaNum
                    //[ yield! branchPath ]
                    []
            let buildId = bigint (int TeamFoundation.Environment.BuildId)
            [ yield! firstSegment
              //yield PreReleaseSegment.AlphaNumeric "vsts"
              yield PreReleaseSegment.Numeric buildId
            ], sprintf "vsts.%s" TeamFoundation.Environment.BuildSourceVersion
        | _ -> [], ""

    let semVer = SemVer.parse release.NugetVersion
    let prerelease =
        match semVer.PreRelease with
        | None -> None
        | Some p ->
            let toAdd = System.String.Join(".", source |> Seq.map segToString)
            let toAdd = if System.String.IsNullOrEmpty toAdd then toAdd else "." + toAdd
            Some ({p with
                        Values = p.Values @ source
                        Origin = p.Origin + toAdd })
    let fromRepository = { semVer with PreRelease = prerelease; Original = None; BuildMetaData = buildMeta }

    match Environment.environVarOrNone "FAKE_VERSION" with
    | Some ver -> SemVer.parse ver
    | None -> fromRepository

let simpleVersion = version.AsString

let nugetVersion =
    if System.String.IsNullOrEmpty version.BuildMetaData
    then version.AsString
    else sprintf "%s+%s" version.AsString version.BuildMetaData
let chocoVersion =
    // Replace "." with "-" in the prerelease-string
    let build =
        if version.Build > 0I then ("." + (let bi = version.Build in bi.ToString("D"))) else ""
    let pre =
        match version.PreRelease with
        | Some preRelease -> ("-" + preRelease.Origin.Replace(".", "-"))
        | None -> ""
    let result = sprintf "%d.%d.%d%s%s" version.Major version.Minor version.Patch build pre
    if pre.Length > 20 then
        let msg = sprintf "Version '%s' is too long for chocolatey (Prerelease string is max 20 chars)" result
        Trace.traceError msg
        failwithf "%s" msg
    result

Trace.setBuildNumber nugetVersion

//let current = CoreTracing.getListeners()
//if current |> Seq.contains CoreTracing.defaultConsoleTraceListener |> not then
//    CoreTracing.setTraceListeners (CoreTracing.defaultConsoleTraceListener :: current)


let dotnetSdk = lazy DotNet.install DotNet.Release_2_1_300
let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
let inline dtntSmpl arg = DotNet.Options.lift dotnetSdk.Value arg

let publish f =
    // Workaround
    let path = Path.GetFullPath f
    let name = Path.GetFileName path
    let target = Path.Combine("artifacts", name)
    let targetDir = Path.GetDirectoryName target
    Directory.ensure targetDir
    Trace.publish ImportData.BuildArtifact (Path.GetFullPath f)

let cleanForTests () =
    // Clean NuGet cache (because it might contain appveyor stuff)
    let cacheFolders = [ Paket.Constants.UserNuGetPackagesFolder; Paket.Constants.NuGetCacheFolder ]
    for f in cacheFolders do
        printfn "Clearing FAKE-NuGet packages in %s" f
        !! (f </> "Fake.*")
        |> Seq.iter (Shell.rm_rf)

    let run workingDir fileName args =
        printfn "CWD: %s" workingDir
        let fileName, args =
            if Environment.isUnix
            then fileName, args else "cmd", ("/C " + fileName + " " + args)
        let ok =
            Process.execSimple (fun info ->
            { info with
                FileName = fileName
                WorkingDirectory = workingDir
                Arguments = args }
            ) System.TimeSpan.MaxValue
        if ok <> 0 then failwith (sprintf "'%s> %s %s' task failed" workingDir fileName args)

    let rmdir dir =
        if Environment.isUnix
        then Shell.rm_rf dir
        // Use this in Windows to prevent conflicts with paths too long
        else run "." "cmd" ("/C rmdir /s /q " + Path.GetFullPath dir)
    // Clean test directories
    !! "integrationtests/*/temp"
    |> Seq.iter rmdir

Target.create "WorkaroundPaketNuspecBug" (fun _ ->
    // Workaround https://github.com/fsprojects/Paket/issues/2830
    // https://github.com/fsprojects/Paket/issues/2689
    // Basically paket fails if there is already an existing nuspec in obj/ dir because then MSBuild will call paket with multiple nuspec file arguments separated by ';'
    !! "src/*/*/obj/**/*.nuspec"
    -- (sprintf "src/*/*/obj/**/*%s.nuspec" nugetVersion)
    |> File.deleteAll
)

// Targets
Target.create "Clean" (fun _ ->
    !! "src/*/*/bin"
    //++ "src/*/*/obj"
    |> Shell.cleanDirs

    Shell.cleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDncDir; nugetLegacyDir; reportDir]

    // Clean Data for tests
    cleanForTests()
)

Target.create "RenameFSharpCompilerService" (fun _ ->
  for packDir in ["FSharp.Compiler.Service";"netcore"</>"FSharp.Compiler.Service"] do
    // for framework in ["net40"; "net45"] do
    for framework in ["netstandard2.0"; "net45"] do
      let dir = __SOURCE_DIRECTORY__ </> "packages"</>packDir</>"lib"</>framework
      let targetFile = dir </>  "FAKE.FSharp.Compiler.Service.dll"
      File.delete targetFile

#if DOTNETCORE
      let reader =
          let searchpaths =
              [ dir; __SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net45" ]
          let resolve name =
              let n = AssemblyName(name)
              match searchpaths
                      |> Seq.collect (fun p -> Directory.GetFiles(p, "*.dll"))
                      |> Seq.tryFind (fun f -> f.ToLowerInvariant().Contains(n.Name.ToLowerInvariant())) with
              | Some f -> f
              | None ->
                  failwithf "Could not resolve '%s'" name
          let readAssemblyE (name:string) (parms: Mono.Cecil.ReaderParameters) =
              Mono.Cecil.AssemblyDefinition.ReadAssembly(
                  resolve name,
                  parms)
          let readAssembly (name:string) (x:Mono.Cecil.IAssemblyResolver) =
              readAssemblyE name (new Mono.Cecil.ReaderParameters(AssemblyResolver = x))
          { new Mono.Cecil.IAssemblyResolver with
              member x.Dispose () = ()
              //member x.Resolve (name : string) = readAssembly name x
              //member x.Resolve (name : string, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name parms
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference) = readAssembly name.FullName x
              member x.Resolve (name : Mono.Cecil.AssemblyNameReference, parms : Mono.Cecil.ReaderParameters) = readAssemblyE name.FullName parms
               }
#else
      let reader = new Mono.Cecil.DefaultAssemblyResolver()
      reader.AddSearchDirectory(dir)
      reader.AddSearchDirectory(__SOURCE_DIRECTORY__ </> "packages/FSharp.Core/lib/net45")
#endif
      let readerParams = Mono.Cecil.ReaderParameters(AssemblyResolver = reader)
      let asem = Mono.Cecil.AssemblyDefinition.ReadAssembly(dir </>"FSharp.Compiler.Service.dll", readerParams)
      asem.Name <- Mono.Cecil.AssemblyNameDefinition("FAKE.FSharp.Compiler.Service", System.Version(1,0,0,0))
      asem.Write(dir</>"FAKE.FSharp.Compiler.Service.dll")
)


let common = [
    AssemblyInfo.Product "FAKE - F# Make"
    AssemblyInfo.Version release.AssemblyVersion
    AssemblyInfo.InformationalVersion nugetVersion
    AssemblyInfo.FileVersion nugetVersion]

// New FAKE libraries
let dotnetAssemblyInfos =
    [ "dotnet-fake", "Fake dotnet-cli command line tool"
      "fake-cli", "Fake global dotnet-cli command line tool"
      "Fake.Api.GitHub", "GitHub Client API Support via Octokit"
      "Fake.Api.HockeyApp", "HockeyApp Integration Support"
      "Fake.Api.Slack", "Slack Integration Support"
      "Fake.Azure.CloudServices", "Azure Cloud Services Support"
      "Fake.Azure.Emulators", "Azure Emulators Support"
      "Fake.Azure.Kudu", "Azure Kudu Support"
      "Fake.Azure.WebJobs", "Azure Web Jobs Support"
      "Fake.BuildServer.AppVeyor", "Integration into AppVeyor buildserver"
      "Fake.BuildServer.GitLab", "Integration into GitLab-CI buildserver"
      "Fake.BuildServer.TeamCity", "Integration into TeamCity buildserver"
      "Fake.BuildServer.TeamFoundation", "Integration into TeamFoundation buildserver"
      "Fake.BuildServer.Travis", "Integration into Travis buildserver"
      "Fake.Core.CommandLineParsing", "Core commandline parsing support via docopt like syntax"
      "Fake.Core.Context", "Core Context Infrastructure"
      "Fake.Core.Environment", "Environment Detection"
      "Fake.Core.Process", "Starting and managing Processes"
      "Fake.Core.ReleaseNotes", "Parsing ReleaseNotes"
      "Fake.Core.SemVer", "Parsing and working with SemVer"
      "Fake.Core.String", "Core String manipulations"
      "Fake.Core.Target", "Defining and running Targets"
      "Fake.Core.Tasks", "Repeating and managing Tasks"
      "Fake.Core.Trace", "Core Logging functionality"
      "Fake.Core.Xml", "Core Xml functionality"
      "Fake.Documentation.DocFx", "Documentation with DocFx"
      "Fake.DotNet.AssemblyInfoFile", "Writing AssemblyInfo files"
      "Fake.DotNet.Cli", "Running the dotnet cli"
      "Fake.DotNet.Fsc", "Running the f# compiler - fsc"
      "Fake.DotNet.FSFormatting", "Running fsformatting.exe and generating documentation"
      "Fake.DotNet.Mage", "Manifest Generation and Editing Tool"
      "Fake.DotNet.MSBuild", "Running msbuild"
      "Fake.DotNet.NuGet", "Running NuGet Client and interacting with NuGet Feeds"
      "Fake.DotNet.Paket", "Running Paket and publishing packages"
      "Fake.DotNet.Testing.Expecto", "Running expecto test runner"
      "Fake.DotNet.Testing.MSpec", "Running mspec test runner"
      "Fake.DotNet.Testing.MSTest", "Running mstest test runner"
      "Fake.DotNet.Testing.NUnit", "Running nunit test runner"
      "Fake.DotNet.Testing.OpenCover", "Code coverage with OpenCover"
      "Fake.DotNet.Testing.SpecFlow", "BDD with Gherkin and SpecFlow"
      "Fake.DotNet.Testing.XUnit2", "Running xunit test runner"
      "Fake.DotNet.Xamarin", "Running Xamarin builds"
      "Fake.Installer.InnoSetup", "Creating installers with InnoSetup"
      "Fake.IO.FileSystem", "Core Filesystem utilities and globbing support"
      "Fake.IO.Zip", "Core Zip functionality"
      "Fake.JavaScript.Npm", "Running npm commands"
      "Fake.JavaScript.Yarn", "Running Yarn commands"
      "Fake.Net.Http", "HTTP Client"
      "Fake.netcore", "Command line tool"
      "Fake.Runtime", "Core runtime features"
      "Fake.Sql.DacPac", "Sql Server Data Tools DacPac operations"
      "Fake.Testing.Common", "Common testing data types"
      "Fake.Testing.ReportGenerator", "Convert XML coverage output to various formats"
      "Fake.Testing.SonarQube", "Analyzing your project with SonarQube"
      "Fake.Tools.Git", "Running git commands"
      "Fake.Tools.Pickles", "Convert Gherkin to HTML"
      "Fake.Tracing.NAntXml", "NAntXml"
      "Fake.Windows.Chocolatey", "Running and packaging with Chocolatey"
      "Fake.Windows.Registry", "CRUD functionality for Windows registry" ]

let assemblyInfos =
  [ legacyDir </> "FAKE/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Command line tool"
        AssemblyInfo.Guid "fb2b540f-d97a-4660-972f-5eeff8120fba"] @ common
    legacyDir </> "Fake.Deploy/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy tool"
        AssemblyInfo.Guid "413E2050-BECC-4FA6-87AA-5A74ACE9B8E1"] @ common
    legacyDir </> "deploy.web/Fake.Deploy.Web/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy Web"
        AssemblyInfo.Guid "27BA7705-3F57-47BE-B607-8A46B27AE876"] @ common
    legacyDir </> "Fake.Deploy.Lib/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Deploy Lib"
        AssemblyInfo.Guid "AA284C42-1396-42CB-BCAC-D27F18D14AC7"] @ common
    legacyDir </> "FakeLib/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Lib"
        AssemblyInfo.InternalsVisibleTo "Test.FAKECore"
        AssemblyInfo.Guid "d6dd5aec-636d-4354-88d6-d66e094dadb5"] @ common
    legacyDir </> "Fake.SQL/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make SQL Lib"
        AssemblyInfo.Guid "A161EAAF-EFDA-4EF2-BD5A-4AD97439F1BE"] @ common
    legacyDir </> "Fake.Experimental/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make Experimental Lib"
        AssemblyInfo.Guid "5AA28AED-B9D8-4158-A594-32FE5ABC5713"] @ common
    legacyDir </> "Fake.FluentMigrator/AssemblyInfo.fs",
      [ AssemblyInfo.Title "FAKE - F# Make FluentMigrator Lib"
        AssemblyInfo.Guid "E18BDD6F-1AF8-42BB-AEB6-31CD1AC7E56D"] @ common ] @
   (dotnetAssemblyInfos
    |> List.map (fun (project, description) ->
        appDir </> sprintf "%s/AssemblyInfo.fs" project, [AssemblyInfo.Title (sprintf "FAKE - F# Make %s" description) ] @ common))

Target.create "SetAssemblyInfo" (fun _ ->
    for assemblyFile, attributes in assemblyInfos do
        // Fixes merge conflicts in AssemblyInfo.fs files, while at the same time leaving the repository in a compilable state.
        // http://stackoverflow.com/questions/32251037/ignore-changes-to-a-tracked-file
        // Quick-fix: git ls-files -v . | grep ^S | cut -c3- | xargs git update-index --no-skip-worktree
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --skip-worktree %s" assemblyFile)
        attributes |> AssemblyInfoFile.createFSharp assemblyFile
        ()
)

Target.create "DownloadPaket" (fun _ ->
    if 0 <> Process.execSimple (fun info ->
            { info with
                FileName = ".paket/paket.exe"
                Arguments = "--version" }
            |> Process.withFramework
            ) (System.TimeSpan.FromMinutes 5.0) then
        failwith "paket failed to start"
)

Target.create "UnskipAndRevertAssemblyInfo" (fun _ ->
    for assemblyFile, _ in assemblyInfos do
        // While the files are skipped in can be hard to switch between branches
        // Therefore we unskip and revert here.
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --no-skip-worktree %s" assemblyFile)
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "checkout HEAD %s" assemblyFile)
        ()
)

Target.create "_BuildSolution" (fun _ ->
    MSBuild.runWithDefaults "Build" ["./src/Legacy-FAKE.sln"; "./src/Legacy-FAKE.Deploy.Web.sln"]
    |> Trace.logItems "AppBuild-Output: "

    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests-legacy.zip"
    !! "test/**"
    |> Zip.zip "." testZip
    publish testZip
)

Target.create "GenerateDocs" (fun _ ->
    Shell.cleanDir docsDir
    let source = "./help"
    let docsTemplate = "docpage.cshtml"
    let indexTemplate = "indexpage.cshtml"
    let githubLink = sprintf "https://github.com/%s/%s" github_release_user gitName
    let projInfo =
      [ "page-description", "FAKE - F# Make"
        "page-author", String.separated ", " authors
        "project-author", String.separated ", " authors
        "github-link", githubLink
        "version", simpleVersion
        "project-github", sprintf "http://github.com/%s/%s" github_release_user gitName
        "project-nuget", "https://www.nuget.org/packages/FAKE"
        "root", sprintf "https://%s" docsDomain
        "project-name", "FAKE - F# Make" ]

    let layoutRoots = [ "./help/templates"; "./help/templates/reference"]
    let fake5LayoutRoots = "./help/templates/fake5" :: layoutRoots
    let legacyLayoutRoots = "./help/templates/legacy" :: layoutRoots
    let fake4LayoutRoots = "./help/templates/fake4" :: layoutRoots

    Shell.copyDir (docsDir) "help/content" FileFilter.allFiles
    // to skip circleci builds
    let docsCircleCi = docsDir + "/.circleci"
    Directory.ensure docsCircleCi
    Shell.copyDir docsCircleCi ".circleci" FileFilter.allFiles
    File.writeString false "./docs/.nojekyll" ""
    File.writeString false "./docs/CNAME" docsDomain
    //CopyDir (docsDir @@ "pics") "help/pics" FileFilter.allFiles

    Shell.copy (source @@ "markdown") ["RELEASE_NOTES.md"]
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "markdown"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "Modules") :: projInfo
            LayoutRoots = layoutRoots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "redirects"
            OutputDirectory = docsDir
            Template = docsTemplate
            ProjectParameters = ("CurrentPage", "FAKE-4") :: projInfo
            LayoutRoots = layoutRoots })
    FSFormatting.createDocs (fun s ->
        { s with
            Source = source @@ "startpage"
            OutputDirectory = docsDir
            Template = indexTemplate
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("CurrentPage", "Home") :: projInfo
            LayoutRoots = layoutRoots })

    Directory.ensure apidocsDir

    let baseDir = Path.GetFullPath "."
    let dllsAndLibDirs (dllPattern:IGlobbingPattern) =
        let dlls =
            dllPattern
            |> GlobbingPattern.setBaseDir baseDir
            |> Seq.distinctBy Path.GetFileName
            |> List.ofSeq
        let libDirs =
            dlls
            |> Seq.map Path.GetDirectoryName
            |> Seq.distinct
            |> List.ofSeq
        (dlls,libDirs)
    // FAKE 5 module documentation
    let fake5ApidocsDir = apidocsDir @@ "v5"
    Directory.ensure fake5ApidocsDir

    let fake5Dlls, fake5LibDirs =
        !! "src/app/Fake.*/bin/Release/**/Fake.*.dll"
        |> dllsAndLibDirs

    fake5Dlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake5ApidocsDir
            LayoutRoots =  fake5LayoutRoots
            LibDirs = fake5LibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v5/") :: ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" })

    // Compat urls
    let redirectPage newPage =
        sprintf """
<html>
	<head>
		<title>Redirecting</title>
		<meta charset="utf-8" />
		<meta name="viewport" content="width=device-width, initial-scale=1" />
	</head>
    <body>
        <p><a href="%s">This page has moved here...</a></p>
        <script type="text/javascript">
            var url = "%s";
            window.location.replace(url);
        </script>
    </body>
</html>"""  newPage newPage

    !! (fake5ApidocsDir + "/*.html")
    |> Seq.iter (fun v5File ->
        // ./docs/apidocs/v5/blub.html
        let name = Path.GetFileName v5File
        let v4Name = Path.GetDirectoryName (Path.GetDirectoryName v5File) @@ name
        // ./docs/apidocs/blub.html
        let link = sprintf "/apidocs/v5/%s" name
        File.WriteAllText(v4Name, redirectPage link)
    )

    // FAKE 5 legacy documentation
    let fake5LegacyApidocsDir = apidocsDir @@ "v5/legacy"
    Directory.ensure fake5LegacyApidocsDir
    let fake5LegacyDlls, fake5LegacyLibDirs =
        !! "build/**/Fake.*.dll"
          ++ "build/FakeLib.dll"
          -- "build/**/Fake.Experimental.dll"
          -- "build/**/FSharp.Compiler.Service.dll"
          -- "build/**/netcore/FAKE.FSharp.Compiler.Service.dll"
          -- "build/**/FAKE.FSharp.Compiler.Service.dll"
          -- "build/**/Fake.IIS.dll"
          -- "build/**/Fake.Deploy.Lib.dll"
        |> dllsAndLibDirs

    fake5LegacyDlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake5LegacyApidocsDir
            LayoutRoots = legacyLayoutRoots
            LibDirs = fake5LegacyLibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v5/legacy/") :: ("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/master" })

    // FAKE 4 legacy documentation
    let fake4LegacyApidocsDir = apidocsDir @@ "v4"
    Directory.ensure fake4LegacyApidocsDir
    let fake4LegacyDlls, fake4LegacyLibDirs =
        !! "packages/docs/FAKE/tools/Fake.*.dll"
          ++ "packages/docs/FAKE/tools/FakeLib.dll"
          -- "packages/docs/FAKE/tools/Fake.Experimental.dll"
          -- "packages/docs/FAKE/tools/FSharp.Compiler.Service.dll"
          -- "packages/docs/FAKE/tools/FAKE.FSharp.Compiler.Service.dll"
          -- "packages/docs/FAKE/tools/Fake.IIS.dll"
          -- "packages/docs/FAKE/tools/Fake.Deploy.Lib.dll"
        |> dllsAndLibDirs

    fake4LegacyDlls
    |> FSFormatting.createDocsForDlls (fun s ->
        { s with
            OutputDirectory = fake4LegacyApidocsDir
            LayoutRoots = fake4LayoutRoots
            LibDirs = fake4LegacyLibDirs
            // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
            ProjectParameters = ("api-docs-prefix", "/apidocs/v4/") ::("CurrentPage", "APIReference") :: projInfo
            SourceRepository = githubLink + "/blob/hotfix_fake4" })
)

#if DOTNETCORE
let startWebServer () =
    let rec findPort port =
        let portIsTaken = false
            //if Environment.isMono then false else
            //System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            //|> Seq.exists (fun x -> x.Port = port)

        if portIsTaken then findPort (port + 1) else port

    let port = findPort 8083
    let serverConfig =
        { Suave.Web.defaultConfig with
           homeFolder = Some (Path.GetFullPath docsDir)
           bindings = [ Suave.Http.HttpBinding.createSimple Suave.Http.Protocol.HTTP "127.0.0.1" port ]
        }
    let (>=>) = Suave.Operators.(>=>)
    let app =
      Suave.WebPart.choose [
        //Filters.path "/websocket" >=> handShake socketHandler
        Suave.Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
        >=> Suave.Writers.setHeader "Pragma" "no-cache"
        >=> Suave.Writers.setHeader "Expires" "0"
        >=> Suave.Files.browseHome ]
    Suave.Web.startWebServerAsync serverConfig app |> snd |> Async.Start
    let psi = System.Diagnostics.ProcessStartInfo(sprintf "http://localhost:%d/index.html" port)
    psi.UseShellExecute <- true
    System.Diagnostics.Process.Start (psi) |> ignore

Target.create "HostDocs" (fun _ ->
    startWebServer()
    Trace.traceImportant "Press any key to stop."
    System.Console.ReadKey() |> ignore
)
#endif

Target.create "CopyLicense" (fun _ ->
    Shell.copyTo buildDir additionalFiles
)

Target.create "Test" (fun _ ->
    !! (testDir @@ "Test.*.dll")
    |> Seq.filter (fun fileName -> if Environment.isMono then fileName.ToLower().Contains "deploy" |> not else true)
    |> MSpec.exec (fun p ->
            {p with
                ToolPath = Globbing.Tools.findToolInSubPath "mspec-x86-clr4.exe" (Shell.pwd() @@ "tools" @@ "MSpec")
                ExcludeTags = if Environment.isWindows then ["HTTP"] else ["HTTP"; "WindowsOnly"]
                TimeOut = System.TimeSpan.FromMinutes 5.
                HtmlOutputDir = reportDir})
    try
        !! (testDir @@ "Test.*.dll")
          ++ (testDir @@ "FsCheck.Fake.dll")
        |> XUnit2.run id
    with e when e.Message.Contains "timed out" && Environment.isUnix ->
        Trace.traceFAKE "Ignoring xUnit timeout for now, there seems to be something funny going on ..."
)

Target.create "DotNetCoreIntegrationTests" (fun _ ->
    cleanForTests()

    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.IntegrationTests/bin/Release/netcoreapp2.1/Fake.Core.IntegrationTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "DotNet Core Integration tests failed."
)


Target.create "DotNetCoreUnitTests" (fun _ ->
    // dotnet run -p src/test/Fake.Core.UnitTests/Fake.Core.UnitTests.fsproj
    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.UnitTests/bin/Release/netcoreapp2.1/Fake.Core.UnitTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "Unit-Tests failed."

    // dotnet run --project src/test/Fake.Core.CommandLine.UnitTests/Fake.Core.CommandLine.UnitTests.fsproj
    let processResult =
        DotNet.exec (dtntWorkDir root) "src/test/Fake.Core.CommandLine.UnitTests/bin/Release/netcoreapp2.1/Fake.Core.CommandLine.UnitTests.dll" "--summary"

    if processResult.ExitCode <> 0 then failwithf "Unit-Tests for Fake.Core.CommandLine failed."
)

Target.create "BootstrapTest" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
    // Check if we can build ourself with the new binaries.
    let test clearCache (script:string) =
        let clear () =
            // Will make sure the test call actually compiles the script.
            // Note: We cannot just clean .fake here as it might be locked by the currently executing code :)
            if Directory.Exists ".fake" then
                Directory.EnumerateFiles(".fake")
                  |> Seq.filter (fun s -> (Path.GetFileName s).StartsWith script)
                  |> Seq.iter File.Delete
        let executeTarget span target =
            if clearCache then clear ()
            if Environment.isUnix then
                let result =
                    Process.execSimple (fun info ->
                    { info with
                        FileName = "chmod"
                        WorkingDirectory = "."
                        Arguments = "+x build/FAKE.exe" }
                    |> Process.withFramework
                    ) span
                if result <> 0 then failwith "'chmod +x build/FAKE.exe' failed on unix"
            Process.execSimple (fun info ->
            { info with
                FileName = "build/FAKE.exe"
                WorkingDirectory = "."
                Arguments = sprintf "%s %s --fsiargs \"--define:BOOTSTRAP\"" script target }
            |> Process.withFramework
            |> Process.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
                ) span

        let result = executeTarget (System.TimeSpan.FromMinutes 10.0) "PrintColors"
        if result <> 0 then failwith "Bootstrapping failed"

        let result = executeTarget (System.TimeSpan.FromMinutes 1.0) "FailFast"
        if result = 0 then failwith "Bootstrapping failed"

    // Replace the include line to use the newly build FakeLib, otherwise things will be weird.
    File.ReadAllText buildScript
    |> fun s -> s.Replace("#I \"packages/build/FAKE/tools/\"", "#I \"build/\"")
    |> fun text -> File.WriteAllText(testScript, text)

    try
      // Will compile the script.
      test true testScript
      // Will use the compiled/cached version.
      test false testScript
    finally File.Delete(testScript)
)


Target.create "BootstrapTestDotNetCore" (fun _ ->
    let buildScript = "build.fsx"
    let testScript = "testbuild.fsx"
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
            // TODO: Clean a potentially cached dll as well.

        let executeTarget target =
            if clearCache then clear ()
            let fileName =
                if Environment.isUnix then "nuget/dotnetcore/Fake.netcore/current/fake"
                else "nuget/dotnetcore/Fake.netcore/current/fake.exe"
            Process.execSimple (fun info ->
                { info with
                    FileName = fileName
                    WorkingDirectory = "."
                    Arguments = sprintf "run --fsiargs \"--define:BOOTSTRAP\" %s --target %s" script target }
                |> Process.setEnvironmentVariable "FAKE_DETAILED_ERRORS" "true"
                )
                timeout
                //true (Trace.traceFAKE "%s") Trace.trace


        let result = executeTarget "PrintColors"
        if result <> 0 then failwithf "Bootstrapping failed (because of exitcode %d)" result

        let result = executeTarget "FailFast"
        if result = 0 then failwithf "Bootstrapping failed (because of exitcode %d)" result

    // Replace the include line to use the newly build FakeLib, otherwise things will be weird.
    // TODO: We might need another way, because currently we reference the same paket group?
    File.ReadAllText buildScript
    |> fun text -> File.WriteAllText(testScript, text)

    try
      // Will compile the script.
      test (System.TimeSpan.FromMinutes 15.0) true testScript
      // Will use the compiled/cached version.
      test (System.TimeSpan.FromMinutes 3.0) false testScript
    finally File.Delete(testScript)
)

Target.create "SourceLink" (fun _ ->
//#if !DOTNETCORE
//    !! "src/app/**/*.fsproj"
//    |> Seq.iter (fun f ->
//        let proj = VsProj.LoadRelease f
//        let url = sprintf "%s/%s/{0}/%%var2%%" gitRaw projectName
//        SourceLink.Index proj.CompilesNotLinked proj.OutputFilePdb __SOURCE_DIRECTORY__ url )
//    let pdbFakeLib = "./build/FakeLib.pdb"
//    Shell.CopyFile "./build/FAKE.Deploy" pdbFakeLib
//    Shell.CopyFile "./build/FAKE.Deploy.Lib" pdbFakeLib
//#else
    printfn "We don't currently have VsProj.LoadRelease on dotnetcore."
//#endif
)

Target.create "ILRepack" (fun _ ->
    Directory.ensure buildMergedDir

    let internalizeIn filename =
        let toPack =
            [filename; "FSharp.Compiler.Service.dll"]
            |> List.map (fun l -> buildDir </> l)
            |> String.separated " "
        let targetFile = buildMergedDir </> filename

        let result =
            Process.execSimple (fun info ->
            { info with
                FileName = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "ILRepack" </> "tools" </> "ILRepack.exe"
                Arguments = sprintf "/verbose /lib:%s /ver:%s /out:%s %s" buildDir release.AssemblyVersion targetFile toPack }
            ) (System.TimeSpan.FromMinutes 5.)

        if result <> 0 then failwithf "Error during ILRepack execution."

        Shell.copyFile (buildDir </> filename) targetFile

    internalizeIn "FAKE.exe"

    !! (buildDir </> "FSharp.Compiler.Service.**")
    |> Seq.iter File.delete

    Shell.deleteDir buildMergedDir
)

Target.create "CreateNuGet" (fun _ ->
    let path =
        if Environment.isWindows
        then "lib" @@ "corflags.exe"
        else "lib" @@ "xCorFlags.exe"
    let set64BitCorFlags files =
        files
        |> Seq.iter (fun file ->
            let exitCode =
                Process.execSimple (fun proc ->
                { proc with
                    FileName = Path.GetFullPath path
                    WorkingDirectory = Path.GetDirectoryName file
                    Arguments = "/32BIT- /32BITPREF- " + Process.quoteIfNeeded file
                    }
                |> Process.withFramework) (System.TimeSpan.FromMinutes 1.)
            if exitCode <> 0 then failwithf "corflags.exe failed with %d" exitCode)

    let x64ify (package:NuGet.NuGet.NuGetParams) =
        { package with
            Dependencies = package.Dependencies |> List.map (fun (pkg, ver) -> pkg + ".x64", ver)
            Project = package.Project + ".x64" }

    let nugetExe =
        let prefs =
           [ "packages/build/Nuget.CommandLine/tools/NuGet.exe"
             "packages/build/NuGet.CommandLine/tools/NuGet.exe" ]
           |> List.map Path.GetFullPath
        match Seq.tryFind (File.Exists) prefs with
        | Some pref -> pref
        | None ->
            let rec printDir space d =
                for f in Directory.EnumerateFiles d do
                    Trace.tracefn "%sFile: %s" space f
                for sd in Directory.EnumerateDirectories d do
                    Trace.tracefn "%sDirectory: %s" space sd
                    printDir (space + "  ") sd
            printDir "  " (Path.GetFullPath "packages")
            match !! "packages/**/NuGet.exe" |> Seq.tryHead with
            | Some e ->
                Trace.tracefn "Found %s" e
                e
            | None ->
                prefs |> List.head

    for package,description in packages do
        let nugetDocsDir = nugetLegacyDir @@ "docs"
        let nugetToolsDir = nugetLegacyDir @@ "tools"
        let nugetLibDir = nugetLegacyDir @@ "lib"
        let nugetLib451Dir = nugetLibDir @@ "net451"

        Shell.cleanDir nugetDocsDir
        Shell.cleanDir nugetToolsDir
        Shell.cleanDir nugetLibDir
        Shell.deleteDir nugetLibDir

        File.delete "./build/FAKE.Gallio/Gallio.dll"

        let deleteFCS _ =
          //!! (dir </> "FSharp.Compiler.Service.**")
          //|> Seq.iter DeleteFile
          ()

        Directory.ensure docsDir
        match package with
        | p when p = projectName ->
            !! (buildDir @@ "**/*.*") |> Shell.copy nugetToolsDir
            Shell.copyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Core" ->
            !! (buildDir @@ "*.*") |> Shell.copy nugetToolsDir
            Shell.copyDir nugetDocsDir docsDir FileFilter.allFiles
            deleteFCS nugetToolsDir
        | p when p = "FAKE.Lib" ->
            Shell.cleanDir nugetLib451Dir
            {
                Globbing.BaseDirectory = buildDir
                Globbing.Includes = [ "FakeLib.dll"; "FakeLib.XML" ]
                Globbing.Excludes = []
            }
            |> Shell.copy nugetLib451Dir
            deleteFCS nugetLib451Dir
        | _ ->
            Shell.copyDir nugetToolsDir (buildDir @@ package) FileFilter.allFiles
            Shell.copyTo nugetToolsDir additionalFiles
        !! (nugetToolsDir @@ "*.srcsv") |> File.deleteAll


        let setParams (p:NuGet.NuGet.NuGetParams) =
            {p with
                NuGet.NuGet.NuGetParams.ToolPath = nugetExe
                NuGet.NuGet.NuGetParams.Authors = authors
                NuGet.NuGet.NuGetParams.Project = package
                NuGet.NuGet.NuGetParams.Description = description
                NuGet.NuGet.NuGetParams.Version = nugetVersion
                NuGet.NuGet.NuGetParams.OutputPath = nugetLegacyDir
                NuGet.NuGet.NuGetParams.WorkingDir = nugetLegacyDir
                NuGet.NuGet.NuGetParams.Summary = projectSummary
                NuGet.NuGet.NuGetParams.ReleaseNotes = release.Notes |> String.toLines
                NuGet.NuGet.NuGetParams.Dependencies =
                    (if package <> "FAKE.Core" && package <> projectName && package <> "FAKE.Lib" then
                       ["FAKE.Core", NuGet.NuGet.RequireExactly (String.NormalizeVersion release.AssemblyVersion)]
                     else p.Dependencies )
                NuGet.NuGet.NuGetParams.Publish = false }

        NuGet.NuGet.NuGet setParams "fake.nuspec"
        !! (nugetToolsDir @@ "FAKE.exe") |> set64BitCorFlags
        NuGet.NuGet.NuGet (setParams >> x64ify) "fake.nuspec"

    let legacyZip = "nuget/fake-legacy-packages.zip"
    !! (nugetLegacyDir </> "**/*.nupkg")
    |> Zip.zip nugetLegacyDir legacyZip
    publish legacyZip
)

let netCoreProjs =
    !! (appDir </> "*/*.fsproj")

let runtimes =
  [ "win7-x86"; "win7-x64"; "osx.10.11-x64"; "ubuntu.14.04-x64"; "ubuntu.16.04-x64" ]

module CircleCi =
    let isCircleCi = Environment.environVarAsBool "CIRCLECI"


// Create target for each runtime
let info = lazy DotNet.info dtntSmpl
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
        !! (appDir </> "Fake.netcore/Fake.netcore.fsproj")
        |> Seq.iter(fun proj ->
            let nugetDir = System.IO.Path.GetFullPath nugetDncDir
            let projName = Path.GetFileName(Path.GetDirectoryName proj)

            //DotNetRestore (fun c -> {c with Runtime = Some runtime}) proj
            let outDir = nugetDir @@ projName @@ runtimeName
            DotNet.publish (fun c ->
                { c with
                    Runtime = Some runtime.Value
                    Configuration = DotNet.Release
                    OutputPath = Some outDir
                } |> dtntSmpl) proj
            let source = outDir </> "dotnet"
            if File.Exists source then
                failwithf "Workaround no longer required?" //TODO: If this is not triggered delete this block
                Trace.traceFAKE "Workaround https://github.com/dotnet/cli/issues/6465"
                let target = outDir </> "fake"
                if File.Exists target then File.Delete target
                File.Move(source, target)
        )
    )
)

Target.create "_DotNetPublish_portable" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir

    // Publish portable as well (see https://docs.microsoft.com/en-us/dotnet/articles/core/app-types)
    let netcoreFsproj = appDir </> "Fake.netcore/Fake.netcore.fsproj"
    let outDir = nugetDir @@ "Fake.netcore" @@ "portable"
    DotNet.publish (fun c ->
        { c with
            Framework = Some "netcoreapp2.1"
            OutputPath = Some outDir
        } |> dtntSmpl) netcoreFsproj
)

Target.create "_DotNetPackage" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir
    // This line actually ensures we get the correct version checked in
    // instead of the one previously bundled with 'fake`
    Git.CommandHelper.gitCommand "" "checkout .paket/Paket.Restore.targets"


    //Environment.setEnvironVar "IncludeSource" "true"
    //Environment.setEnvironVar "IncludeSymbols" "false"
    Environment.setEnvironVar "GenerateDocumentationFile" "true"
    Environment.setEnvironVar "PackageVersion" nugetVersion
    Environment.setEnvironVar "Version" nugetVersion
    Environment.setEnvironVar "Authors" (String.separated ";" authors)
    Environment.setEnvironVar "Description" projectDescription
    Environment.setEnvironVar "PackageReleaseNotes" (release.Notes |> String.toLines)
    Environment.setEnvironVar "PackageTags" "build;fake;f#"
    Environment.setEnvironVar "PackageIconUrl" "https://raw.githubusercontent.com/fsharp/FAKE/fee4f05a2ee3c646979bf753f3b1f02d927bfde9/help/content/pics/logo.png"
    Environment.setEnvironVar "PackageProjectUrl" "https://github.com/fsharp/Fake"
    Environment.setEnvironVar "PackageLicenseUrl" "https://github.com/fsharp/FAKE/blob/d86e9b5b8e7ebbb5a3d81c08d2e59518cf9d6da9/License.txt"


    // dotnet pack
    DotNet.pack (fun c ->
        { c with
            Configuration = DotNet.Release
            OutputPath = Some nugetDir
            Common =
                if CircleCi.isCircleCi then
                    { c.Common with CustomParams = Some "/m:1" }
                else c.Common
        } |> dtntSmpl) "Fake.sln"

    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests.zip"
    !! "src/test/*/bin/Release/netcoreapp2.1/**"
    |> Zip.zip "src/test" testZip
    publish testZip
)

Target.create "DotNetCoreCreateZipPackages" (fun _ ->
    Environment.setEnvironVar "Version" nugetVersion

    // build zip packages
    !! "nuget/dotnetcore/*.nupkg"
    -- "nuget/dotnetcore/*.symbols.nupkg"
    |> Zip.zip "nuget/dotnetcore" "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-packages.zip"

    ("portable" :: runtimes)
    |> Seq.iter (fun runtime ->
        let runtimeDir = sprintf "nuget/dotnetcore/Fake.netcore/%s" runtime
        !! (sprintf "%s/**" runtimeDir)
        |> Zip.zip runtimeDir (sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" runtime)
    )

    runtimes @ [ "portable"; "packages" ]
    |> List.map (fun n -> sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)
    |> List.iter publish
)

let getChocoWrapper () =
    let altToolPath = Path.GetFullPath "temp/choco.sh"
    if not Environment.isWindows then
        Directory.ensure "temp"
        File.WriteAllText(altToolPath, """#!/bin/bash
docker run --rm -v $PWD:$PWD -w $PWD linuturk/mono-choco $@
"""          )
        let result = Shell.Exec("chmod", sprintf "+x %s" altToolPath)
        if result <> 0 then failwithf "'chmod +x %s' failed on unix" altToolPath
    altToolPath

Target.create "DotNetCoreCreateChocolateyPackage" (fun _ ->
    // !! ""
    let altToolPath = getChocoWrapper()
    let changeToolPath (p: Choco.ChocoPackParams) =
        if Environment.isWindows
        then p
        else { p with ToolPath = altToolPath }
    Directory.ensure "nuget/dotnetcore/chocolatey"
    Choco.packFromTemplate (fun p ->
        { p with
            PackageId = "fake"
            ReleaseNotes = release.Notes |> String.toLines
            InstallerType = Choco.ChocolateyInstallerType.SelfContained
            Version = chocoVersion
            Files =
                [ (System.IO.Path.GetFullPath @"nuget\dotnetcore\Fake.netcore\win7-x86") + @"\**", Some "bin", None
                  (System.IO.Path.GetFullPath @"src\VERIFICATION.txt"), Some "VERIFICATION.txt", None
                  (System.IO.Path.GetFullPath @"License.txt"), Some "LICENSE.txt", None ]
            OutputDir = "nuget/dotnetcore/chocolatey" }
        |> changeToolPath) "src/Fake-choco-template.nuspec"

    let name = sprintf "%s.%s" "fake" chocoVersion
    let chocoPackage = sprintf "nuget/dotnetcore/chocolatey/%s.nupkg" name
    let chocoTargetPackage = sprintf "nuget/dotnetcore/chocolatey/chocolatey-%s.nupkg" name
    File.Copy(chocoPackage, chocoTargetPackage, true)
    publish chocoTargetPackage
)
Target.create "DotNetCorePushChocolateyPackage" (fun _ ->
    let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
    let path = sprintf "nuget/dotnetcore/chocolatey/%s" name
    if not Environment.isWindows && not (File.exists path) && fromArtifacts then
        Directory.ensure "nuget/dotnetcore/chocolatey"
        Shell.copyFile path (artifactsDir </> sprintf "chocolatey-%s" name)

    let altToolPath = getChocoWrapper()
    let changeToolPath (p: Choco.ChocoPushParams) =
        if Environment.isWindows then p else { p with ToolPath = altToolPath }
    path |> Choco.push (fun p ->
        { p with
            Source = chocosource
            ApiKey = chocoKey.Value }
        |> changeToolPath)
)

Target.create "CheckReleaseSecrets" (fun _ ->
    for secret in secrets do
        secret.Force() |> ignore
)

let executeFPM args =
    printfn "%s %s" "fpm" args
    Shell.Exec("fpm", args=args, dir="bin")


type SourceType =
    | Dir of source:string * target:string
type DebPackageManifest =
    {
        SourceType : SourceType
        Name : string
        Version : string
        Dependencies : (string * string option) list
        BeforeInstall : string option
        AfterInstall : string option
        ConfigFile : string option
        AdditionalOptions: string list
        AdditionalArgs : string list
    }
(*
See https://www.debian.org/doc/debian-policy/ch-maintainerscripts.html
Ask @theangrybyrd (slack)

{
    SourceType = Dir("./MyCoolApp", "/opt/")
    Name = "mycoolapp"
    Version = originalVersion
    Dependencies = [("mono-devel", None)]
    BeforeInstall = "../deploy/preinst" |> Some
    AfterInstall = "../deploy/postinst" |> Some
    ConfigFile = "/etc/mycoolapp/default.conf" |> Some
    AdditionalOptions = []
    AdditionalArgs =
        [ "../deplo/mycoolapp.service=/lib/systemd/system/" ]
}
23:08
so thats stuff i you want to setup like users or what not
23:09
adding to your path would be in the after script postinst
23:10
setting permissions also, its just a shell script
23:10
might also want a prerm and postrm if you want to play nice on cleanup
*)

Target.create "DotNetCoreCreateDebianPackage" (fun _ ->
    let createDebianPackage (manifest : DebPackageManifest) =
        let argsList = ResizeArray<string>()
        argsList.Add <| match manifest.SourceType with
                        | Dir (_) -> "-s dir"
        argsList.Add <| "-t deb"
        argsList.Add <| "-f"
        argsList.Add <| (sprintf "-n %s" manifest.Name)
        argsList.Add <| (sprintf "-v %s" (manifest.Version.Replace("-","~")))
        let dependency name version =
            match version with
            | Some v -> sprintf "-d '%s %s'" name v
            | None  -> sprintf "-d '%s'" name
        argsList.AddRange <| (Seq.map(fun (a,b) -> dependency a b) manifest.Dependencies)
        manifest.BeforeInstall |> Option.iter(sprintf "--before-install %s" >> argsList.Add)
        manifest.AfterInstall |> Option.iter(sprintf "--after-install %s" >> argsList.Add)
        manifest.ConfigFile |> Option.iter(sprintf "--config-files %s" >> argsList.Add)
        argsList.AddRange <| manifest.AdditionalOptions
        argsList.Add <| match manifest.SourceType with
                        | Dir (source,target) -> sprintf "%s=%s" source target
        argsList.AddRange <| manifest.AdditionalArgs
        if argsList |> String.concat " " |> executeFPM <> 0 then
            failwith "Failed creating deb package"
    ignore createDebianPackage
    ()

)


let rec nugetPush tries nugetpackage =
    let ignore_conflict = Environment.environVar "IGNORE_CONFLICT" = "true"
    try
        if not <| System.String.IsNullOrEmpty apikey.Value then
            Process.execWithResult (fun info ->
            { info with
                FileName = nuget_exe
                Arguments = sprintf "push %s %s -Source %s" (Process.toParam nugetpackage) (Process.toParam apikey.Value) (Process.toParam nugetsource) }
            ) (System.TimeSpan.FromMinutes 10.)
            |> (fun r ->
                 for res in r.Results do
                    if res.IsError then
                        Trace.traceFAKE "%s" res.Message
                    else
                        Trace.tracefn "%s" res.Message
                 if r.ExitCode <> 0 then
                    if not ignore_conflict ||
                       not (r.Errors |> Seq.exists (fun err -> err.Contains "409"))
                    then
                        let msgs = r.Results |> Seq.map (fun c -> (if c.IsError then "(Err) " else "") + c.Message)                    
                        let msg = System.String.Join ("\n", msgs)
                 
                        failwithf "failed to push package %s (code %d): \n%s" nugetpackage r.ExitCode msg
                    else Trace.traceFAKE "ignore conflict error because IGNORE_CONFLICT=true!")
        else Trace.traceFAKE "could not push '%s', because api key was not set" nugetpackage
    with exn when tries > 1 ->
        Trace.traceFAKE "Error while pushing NuGet package: %s" exn.Message
        nugetPush (tries - 1) nugetpackage

Target.create "DotNetCorePushNuGet" (fun _ ->
    // dotnet pack
    netCoreProjs
    -- (appDir </> "Fake.netcore/*.fsproj")
    |> Seq.iter(fun proj ->
        let projName = Path.GetFileName(Path.GetDirectoryName proj)
        !! (sprintf "nuget/dotnetcore/%s.*.nupkg" projName)
        -- (sprintf "nuget/dotnetcore/%s.*.symbols.nupkg" projName)
        |> Seq.iter (nugetPush 4))
)

Target.create "PublishNuget" (fun _ ->
    // uses NugetKey environment variable.
    // Timeout atm
    Paket.push(fun p ->
        { p with
            PublishUrl = nugetsource
            DegreeOfParallelism = 2
            WorkingDir = nugetLegacyDir })
    //!! (nugetLegacyDir </> "**/*.nupkg")
    //|> Seq.iter nugetPush
)

Target.create "ReleaseDocs" (fun _ ->
    Shell.cleanDir "gh-pages"
    let auth = sprintf "%s:x-oauth-basic@" githubtoken.Value
    let url = sprintf "https://%sgithub.com/%s/%s.git" auth github_release_user gitName
    Git.Repository.cloneSingleBranch "" url "gh-pages" "gh-pages"

    Git.Repository.fullclean "gh-pages"
    Shell.copyRecursive "docs" "gh-pages" true |> printfn "%A"
    Shell.copyFile "gh-pages" "./Samples/FAKE-Calculator.zip"
    File.writeString false "./gh-pages/CNAME" docsDomain
    Git.Staging.stageAll "gh-pages"
    if not BuildServer.isLocalBuild then
        Git.CommandHelper.directRunGitCommandAndFail "gh-pages" "config user.email matthi.d@gmail.com"
        Git.CommandHelper.directRunGitCommandAndFail "gh-pages" "config user.name \"Matthias Dittrich\""
    Git.Commit.exec "gh-pages" (sprintf "Update generated documentation %s" simpleVersion)
    Git.Branches.pushBranch "gh-pages" url "gh-pages"
)

Target.create "FastRelease" (fun _ ->
    let token = githubtoken.Value
    let auth = sprintf "%s:x-oauth-basic@" token
    let url = sprintf "https://%sgithub.com/%s/%s.git" auth github_release_user gitName

    let gitDirectory = Environment.environVarOrDefault "git_directory" ""
    if not BuildServer.isLocalBuild then
        Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.email matthi.d@gmail.com"
        Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.name \"Matthias Dittrich\""
    if gitDirectory <> "" && BuildServer.buildServer = BuildServer.TeamFoundation then
        Trace.trace "Prepare git directory"
        Git.Branches.checkout gitDirectory false TeamFoundation.Environment.BuildSourceVersion
    else
        Git.Staging.stageAll gitDirectory
        Git.Commit.exec gitDirectory (sprintf "Bump version to %s" simpleVersion)
        let branch = Git.Information.getBranchName gitDirectory
        Git.Branches.pushBranch gitDirectory "origin" branch

    Git.Branches.tag gitDirectory simpleVersion
    Git.Branches.pushTag gitDirectory url simpleVersion

    let files =
        runtimes @ [ "portable"; "packages" ]
        |> List.map (fun n -> sprintf "nuget/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease github_release_user gitName simpleVersion (release.SemVer.PreRelease <> None) release.Notes
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
)

Target.create "Release_Staging" (fun _ -> ())

open System.IO.Compression
let unzip target (fileName : string) =
    use stream = new FileStream(fileName, FileMode.Open)
    use zipFile = new ZipArchive(stream)
    for zipEntry in zipFile.Entries do
        let unzipPath = Path.Combine(target, zipEntry.FullName)
        let directoryPath = Path.GetDirectoryName(unzipPath)
        if unzipPath.EndsWith "/" then
            Directory.CreateDirectory(unzipPath) |> ignore
        else
            // unzip the file
            Directory.ensure directoryPath
            let zipStream = zipEntry.Open()
            if unzipPath.EndsWith "/" |> not then
                use unzippedFileStream = File.Create(unzipPath)
                zipStream.CopyTo(unzippedFileStream)

Target.create "PrepareArtifacts" (fun _ ->
    if not fromArtifacts then
        Trace.trace "empty artifactsDir."
    else
        Trace.trace "ensure artifacts."
        let files =
            !! (artifactsDir </> "fake-dotnetcore-*.zip")
            |> GlobbingPattern.setBaseDir "C:\\" // workaround a globbing bug, remove me with 5.0.0-rc014
            |> Seq.toList
        Trace.tracefn "files: %A" files
        files
        |> Shell.copy "nuget/dotnetcore/Fake.netcore"

        unzip "nuget/dotnetcore" (artifactsDir </> "fake-dotnetcore-packages.zip")

        if Environment.isWindows then
            Directory.ensure "nuget/dotnetcore/chocolatey"
            let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
            Shell.copyFile (sprintf "nuget/dotnetcore/chocolatey/%s" name) (artifactsDir </> sprintf "chocolatey-%s" name)
        else
            unzip "." (artifactsDir </> "chocolatey-requirements.zip")

        Directory.ensure "nuget/legacy"
        unzip "nuget/legacy" (artifactsDir </> "fake-legacy-packages.zip")

        Directory.ensure "temp/build"
        !! ("nuget" </> "legacy" </> "*.nupkg")
        |> Seq.iter (fun pack ->
            unzip "temp/build" pack
        )
        Shell.copyDir "build" "temp/build" (fun _ -> true)

        let unzipIfExists dir file =
            Directory.ensure dir
            if File.Exists file then
                unzip dir file

        // File is not available in case we already have build the full docs
        unzipIfExists "help" (artifactsDir </> "help-markdown.zip")
        unzipIfExists "docs" (artifactsDir </> "docs.zip")
        unzipIfExists "src/test" (artifactsDir </> "tests.zip")
)

Target.create "BuildArtifacts" (fun args ->
    Directory.ensure "temp"

    if not Environment.isWindows then
        // Chocolatey package is done in a separate step...
        let chocoReq = "temp/chocolatey-requirements.zip"
        //!! @"nuget\dotnetcore\Fake.netcore\win7-x86\**" already part of fake-dotnetcore-win7-x86
        !! @"src\VERIFICATION.txt"
        ++ @"License.txt"
        ++ "src/Fake-choco-template.nuspec"
        |> Zip.zip "." chocoReq
        publish chocoReq

    let buildCache = "temp/build-cache.zip"
    !! (".fake" </> "build.fsx" </> "*.dll")
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
        !! ("help" </> "**")
        |> Zip.zip "help" helpZip
        publish helpZip
)

open System
Target.create "PrintColors" (fun _ ->
  let color (color: ConsoleColor) (code : unit -> _) =
      let before = Console.ForegroundColor
      try
        Console.ForegroundColor <- color
        code ()
      finally
        Console.ForegroundColor <- before
  color ConsoleColor.Magenta (fun _ -> printfn "TestMagenta")
)
Target.create "FailFast" (fun _ -> failwith "fail fast")
Target.create "EnsureTestsRun" (fun _ ->
//#if !DOTNETCORE
//  if Environment.hasEnvironVar "SkipIntegrationTests" || Environment.hasEnvironVar "SkipTests" then
//      let res = getUserInput "Are you really sure to continue without running tests (yes/no)?"
//      if res <> "yes" then
//          failwith "cannot continue without tests"
//#endif
  ()
)
#if BOOTSTRAP
Target.description "Default Build all artifacts and documentation"
#else
Target.Description "Default Build all artifacts and documentation"
#endif
Target.create "Default" ignore
Target.create "_StartDnc" ignore
#if BOOTSTRAP
Target.description "Simple local command line release"
#else
Target.Description "Simple local command line release"
#endif
Target.create "Release" ignore
#if BOOTSTRAP
Target.description "Build the full-framework (legacy) solution"
#else
Target.Description "Build the full-framework (legacy) solution"
#endif
Target.create "BuildSolution" ignore
#if BOOTSTRAP
Target.description "dotnet pack pack to build all nuget packages"
#else
Target.Description "dotnet pack pack to build all nuget packages"
#endif
Target.create "DotNetPackage" ignore
Target.create "_AfterBuild" ignore
#if BOOTSTRAP
Target.description "Build and test the dotnet sdk part (fake 5 - no legacy)"
#else
Target.Description "Build and test the dotnet sdk part (fake 5 - no legacy)"
#endif
Target.create "FullDotNetCore" ignore
#if BOOTSTRAP
Target.description "publish fake 5 runner for various platforms"
#else
Target.Description "publish fake 5 runner for various platforms"
#endif
Target.create "DotNetPublish" ignore
#if BOOTSTRAP
Target.description "Run the tests - if artifacts are available via 'artifactsdirectory' those are used."
#else
Target.Description "Run the tests - if artifacts are available via 'artifactsdirectory' those are used."
#endif
Target.create "RunTests" ignore
#if BOOTSTRAP
Target.description "Generate the docs (potentially from artifacts) and publish as artifact."
#else
Target.Description "Generate the docs (potentially from artifacts) and publish as artifact."
#endif
Target.create "Release_GenerateDocs" (fun _ ->
    let testZip = "temp/docs.zip"
    !! "docs/**"
    |> Zip.zip "docs" testZip
    publish testZip
)

#if BOOTSTRAP
Target.description "Full Build & Test and publish results as artifacts."
#else
Target.Description "Full Build & Test and publish results as artifacts."
#endif
Target.create "Release_BuildAndTest" ignore
open Fake.Core.TargetOperators

"CheckReleaseSecrets"
    ?=> "Clean"
"WorkaroundPaketNuspecBug"
    ==> "Clean"
"WorkaroundPaketNuspecBug"
    ==> "_DotNetPackage"

// DotNet Core Build
"Clean"
    ?=> "_StartDnc"
    ?=> "DownloadPaket"
    ?=> "SetAssemblyInfo"
    ==> "_DotNetPackage"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "DotNetPackage"
"_StartDnc"
    ==> "_DotNetPackage"
"DownloadPaket"
    ==> "_DotNetPackage"
"_DotNetPackage"
    ==> "DotNetPackage"

let mutable prev = None
for runtime in "current" :: "portable" :: runtimes do
    let rawTargetName = sprintf "_DotNetPublish_%s" runtime
    let targetName = sprintf "DotNetPublish_%s" runtime
    Target.Description (sprintf "publish fake 5 runner for %s" runtime)
    Target.create targetName ignore
    "SetAssemblyInfo"
        ==> rawTargetName
        ?=> "UnskipAndRevertAssemblyInfo"
        ==> targetName
        |> ignore
    rawTargetName
        ==> targetName
        |> ignore
    "_StartDnc"
        ==> targetName
        |> ignore
    "DownloadPaket"
        ==> targetName
        |> ignore
    targetName
        ==> "DotNetPublish"
        |> ignore

    // Make sure we order then (when building parallel!)
    match prev with
    | Some prev -> prev ?=> rawTargetName |> ignore
    | None -> "_DotNetPackage" ?=> rawTargetName |> ignore
    prev <- Some rawTargetName


// Full framework build
"Clean"
    ?=> "RenameFSharpCompilerService"
    ?=> "SetAssemblyInfo"
    ==> "_BuildSolution"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "BuildSolution"
"RenameFSharpCompilerService"
    ==> "_BuildSolution"
"_BuildSolution"
    ==> "BuildSolution"
// AfterBuild -> Both Builds completed
"BuildSolution"
    ==> "_AfterBuild"
"DotNetPackage"
    ==> "_AfterBuild"
"DotNetPublish"
    ==> "_AfterBuild"


// Create artifacts when build is finished
let prevDocs =
    "_AfterBuild"
    ==> "CreateNuGet"
    ==> "CopyLicense"
    =?> ("DotNetCoreCreateChocolateyPackage", Environment.isWindows)
    ==> "Default"
(if fromArtifacts then "PrepareArtifacts" else "_AfterBuild")
    =?> ("GenerateDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "Default"
"_AfterBuild" ?=> "GenerateDocs"

"GenerateDocs"
    ==> "Release_GenerateDocs"

// Build artifacts only (no testing)
"CreateNuGet"
    ==> "BuildArtifacts"
"DotNetCoreCreateChocolateyPackage"
    =?> ("BuildArtifacts", Environment.isWindows)
"DotNetCoreCreateZipPackages"
    ==> "BuildArtifacts"

// Test the full framework build
"_BuildSolution"
    =?> ("Test", not <| Environment.hasEnvironVar "SkipTests")
    ==> "Default"

"BuildSolution"
    ==> "Default"

(if fromArtifacts then "PrepareArtifacts" else "_BuildSolution")
    =?> ("BootstrapTest", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "Default"
"_BuildSolution" ?=> "BootstrapTest"

"BootstrapTest"
    ==> "RunTests"

// Test the dotnetcore build
(if fromArtifacts then "PrepareArtifacts" else "_DotNetPackage")
    =?> ("DotNetCoreUnitTests",not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPackage" ?=> "DotNetCoreUnitTests"

"DotNetCoreUnitTests"
    ==> "RunTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPublish_current")
    =?> ("DotNetCoreIntegrationTests", not <| Environment.hasEnvironVar "SkipIntegrationTests" && not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPublish_current" ?=> "DotNetCoreIntegrationTests"

"DotNetCoreIntegrationTests"
    ==> "RunTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPackage")
    =?> ("DotNetCoreIntegrationTests", not <| Environment.hasEnvironVar "SkipIntegrationTests" && not <| Environment.hasEnvironVar "SkipTests")
"_DotNetPackage" ?=> "DotNetCoreIntegrationTests"

(if fromArtifacts then "PrepareArtifacts" else "_DotNetPublish_current")
    =?> ("BootstrapTestDotNetCore", not disableBootstrap && not <| Environment.hasEnvironVar "SkipTests")
    ==> "FullDotNetCore"
"_DotNetPublish_current" ?=> "BootstrapTestDotNetCore"

"BootstrapTestDotNetCore"
    ==> "RunTests"

"DotNetPackage"
    ==> "DotNetCoreCreateZipPackages"
    ==> "FullDotNetCore"
    ==> "Default"

// Artifacts & Tests
"Default" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ?=> "BuildArtifacts"
"BuildArtifacts" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ==> "Release_BuildAndTest"


// Release stuff ('FastRelease' is to release after running 'Default')
(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("DotNetCorePushChocolateyPackage", Environment.isWindows)
    ==> "FastRelease"
"EnsureTestsRun" ?=> "DotNetCorePushChocolateyPackage"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("ReleaseDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "FastRelease"
"EnsureTestsRun" ?=> "ReleaseDocs"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    ==> "DotNetCorePushNuGet"
    ==> "FastRelease"
"EnsureTestsRun" ?=> "DotNetCorePushNuGet"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    ==> "PublishNuget"
    ==> "FastRelease"
"EnsureTestsRun" ?=> "PublishNuget"

// Gitlab staging (myget release)
"PublishNuget"
    ==> "Release_Staging"
"DotNetCorePushNuGet"
    ==> "Release_Staging"
"DotNetCorePushChocolateyPackage"
    ==> "Release_Staging"

// If 'Default' happens it needs to happen before 'EnsureTestsRun'
"Default"
    ?=> "EnsureTestsRun"

// A 'Default' includes a 'Clean'
"Clean"
    ==> "Default"

// A 'Release' includes a 'Default'
"Default"
    ==> "Release"
// A 'Release' includes a 'FastRelease'
"FastRelease"
    ==> "Release"
// A 'Release' includes a 'CheckReleaseSecrets'
"CheckReleaseSecrets"
    ==> "Release"
//start build
Target.runOrDefault "Default"
