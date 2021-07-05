#if BOOTSTRAP

#r "paket:
source release/dotnetcore
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
nuget Fake.Core.Vault prerelease
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
nuget Suave 2.5.6
nuget Newtonsoft.Json
nuget Octokit //"
#endif

// We need to use this for now as "regular" Fake breaks when its caching logic cannot find "intellisense.fsx".
// This is the reason why we need to checkin the "intellisense.fsx" file for now...
#load ".fake/build.fsx/intellisense.fsx"
#load "legacy-build.fsx"

open System.Reflection

//#if !FAKE
//let execContext = Fake.Core.Context.FakeExecutionContext.Create false "build.fsx" []
//Fake.Core.Context.setExecutionContext (Fake.Core.Context.RuntimeContext.Fake execContext)
//#endif
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
let projectDescription = "FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#."
let authors = ["Steffen Forkmann"; "Mauricio Scheffer"; "Colin Bull"; "Matthias Dittrich"]

let release = ReleaseNotes.load "RELEASE_NOTES.md"

let buildDir = "./build"
let testDir = "./test"
let docsDir = "./docs"
let apidocsDir = "./docs/apidocs/"

let releaseDir = "./release"
let nugetDncDir = releaseDir </> "dotnetcore"
let chocoReleaseDir = nugetDncDir </> "chocolatey"
let nugetLegacyDir = releaseDir </> "legacy"

let reportDir = "./report"

let root = __SOURCE_DIRECTORY__
let srcDir = root</>"src"
let appDir = srcDir</>"app"
let templateDir = srcDir</>"template"

let nuget_exe = Directory.GetCurrentDirectory() </> "packages" </> "build" </> "NuGet.CommandLine" </> "tools" </> "NuGet.exe"

let getVarOrDefault name def = ``Legacy-build``.getVarOrDefault name def
let releaseSecret replacement name = ``Legacy-build``.releaseSecret replacement name

let github_release_user = getVarOrDefault "github_release_user" "fsharp"

// The name of the project on GitHub
let gitName = getVarOrDefault "github_repository_name" "FAKE"
let nugetsource = getVarOrDefault "nugetsource" "https://www.nuget.org/api/v2/package"
let chocosource = getVarOrDefault "chocosource" "https://push.chocolatey.org/"
let artifactsDir = getVarOrDefault "artifactsdirectory" ""
let docsDomain = getVarOrDefault "docs_domain" "fake.build"
let buildLegacy = System.Boolean.Parse(getVarOrDefault "BuildLegacy" "false")
let fromArtifacts = not <| String.isNullOrEmpty artifactsDir


let apikey = releaseSecret "<nugetkey>" "nugetkey"
let chocoKey = releaseSecret "<chocokey>" "CHOCOLATEY_API_KEY"
let githubtoken = releaseSecret "<githubtoken>" "github_token"

do Environment.setEnvironVar "COREHOST_TRACE" "0"

BuildServer.install [
    AppVeyor.Installer
    TeamCity.Installer
    Travis.Installer
    TeamFoundation.Installer
    GitLab.Installer
]

let version = ``Legacy-build``.version
let simpleVersion = ``Legacy-build``.simpleVersion
let nugetVersion = ``Legacy-build``.nugetVersion
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

match TeamFoundation.Environment.SystemPullRequestIsFork with
| None | Some false ->
    Trace.setBuildNumber nugetVersion
| _ ->
    Trace.traceFAKE "Not setting buildNumber to '%s', because of https://developercommunity.visualstudio.com/content/problem/350007/build-from-github-pr-fork-error-tf400813-the-user-1.html" nugetVersion

let dotnetSdk = lazy DotNet.install DotNet.Versions.FromGlobalJson
let inline dtntWorkDir wd =
    DotNet.Options.lift dotnetSdk.Value
    >> DotNet.Options.withWorkingDirectory wd
    >> DotNet.Options.withTimeout (Some (System.TimeSpan.FromMinutes 20.))
let inline dtntSmpl arg = DotNet.Options.lift dotnetSdk.Value arg

let publish f =``Legacy-build``.publish f

let cleanForTests () =
    // Clean NuGet cache (because it might contain appveyor stuff)
    //let cacheFolders = [ Paket.Constants.UserNuGetPackagesFolder; Paket.Constants.NuGetCacheFolder ]
    //for f in cacheFolders do
    //    printfn "Clearing FAKE-NuGet packages in %s" f
    //    !! (f </> "Fake.*")
    //    |> Seq.iter (Shell.rm_rf)

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

Target.initEnvironment()

Target.create "WorkaroundPaketNuspecBug" (fun _ ->
    // Workaround https://github.com/fsprojects/Paket/issues/2830
    // https://github.com/fsprojects/Paket/issues/2689
    // Basically paket fails if there is already an existing nuspec in obj/ dir because then MSBuild will call paket with multiple nuspec file arguments separated by ';'
    !! "src/*/*/obj/**/*.nuspec"
    -- (sprintf "src/*/*/obj/**/*%s.nuspec" nugetVersion)
    |> File.deleteAll
)

let restoreTools =
    let mutable alreadyRestored = false
    (fun () ->
        if not alreadyRestored then
            DotNet.exec dtntSmpl "tool" "restore" |> ignore<ProcessResult>
            alreadyRestored <- true
    )

let callpaket wd args =
    restoreTools()

    let res = DotNet.exec (dtntWorkDir wd) "paket" args
    if not res.OK then
        failwithf "paket failed to start: %A" res

// Targets
Target.create "Clean" (fun _ ->
    !! "src/*/*/bin"
    //++ "src/*/*/obj"
    |> Shell.cleanDirs

    let fakeRuntimeVersion = typeof<Fake.Core.Context.FakeExecutionContext>.Assembly.GetName().Version
    printfn "fake runtime %O" fakeRuntimeVersion
    if fakeRuntimeVersion < new System.Version(5, 10, 0) then
        printfn "deleting obj directories because of https://github.com/fsprojects/Paket/issues/3404"
        !! "src/*/*/obj"
        |> Shell.cleanDirs
        // Allow paket to do a full-restore (to improve performance)
        Shell.rm ("paket-files" </> "paket.restore.cached")
        callpaket "." "restore"

    Shell.cleanDirs [buildDir; testDir; docsDir; apidocsDir; nugetDncDir; nugetLegacyDir; reportDir]

    // Clean Data for tests
    cleanForTests()
)

let common = [
    AssemblyInfo.Product "FAKE - F# Make"
    AssemblyInfo.Version release.AssemblyVersion
    AssemblyInfo.InformationalVersion nugetVersion
    AssemblyInfo.FileVersion nugetVersion
    AssemblyInfo.Metadata("BuildDate", System.DateTime.UtcNow.ToString("yyyy-MM-dd")) ]

// New FAKE libraries
let dotnetAssemblyInfos =
    [ "fake-cli", "Fake global dotnet-cli command line tool"
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
      "Fake.BuildServer.Bitbucket", "Integration into Bitbucket buildserver"
      "Fake.Core.CommandLineParsing", "Core commandline parsing support via docopt like syntax"
      "Fake.Core.Context", "Core Context Infrastructure"
      "Fake.Core.DependencyManager.Paket", "Paket Dependency Manager"
      "Fake.Core.Environment", "Environment Detection"
      "Fake.Core.Process", "Starting and managing Processes"
      "Fake.Core.ReleaseNotes", "Parsing ReleaseNotes"
      "Fake.Core.SemVer", "Parsing and working with SemVer"
      "Fake.Core.String", "Core String manipulations"
      "Fake.Core.Target", "Defining and running Targets"
      "Fake.Core.Tasks", "Repeating and managing Tasks"
      "Fake.Core.Trace", "Core Logging functionality"
      "Fake.Core.UserInput", "User input helpers"
      "Fake.Core.Vault", "Encrypt secrets and prevent accidental disclosure"
      "Fake.Core.Xml", "Core Xml functionality"
      "Fake.Documentation.DocFx", "Documentation with DocFx"
      "Fake.DotNet.AssemblyInfoFile", "Writing AssemblyInfo files"
      "Fake.DotNet.Cli", "Running the dotnet cli"
      "Fake.DotNet.Fsc", "Running the f# compiler - fsc"
      "Fake.DotNet.FSFormatting", "Running fsformatting.exe and generating documentation"
      "Fake.DotNet.Fsi", "FSharp Interactive - fsi"
      "Fake.DotNet.FxCop", "Running FxCop for static analysis"
      "Fake.DotNet.ILMerge", "Running the ILMerge static linker tool"
      "Fake.DotNet.Mage", "Manifest Generation and Editing Tool"
      "Fake.DotNet.MSBuild", "Running msbuild"
      "Fake.DotNet.NuGet", "Running NuGet Client and interacting with NuGet Feeds"
      "Fake.DotNet.Paket", "Running Paket and publishing packages"
      "Fake.DotNet.Testing.Coverlet", "Code coverage with Coverlet"
      "Fake.DotNet.Testing.DotCover", "Code coverage with DotCover"
      "Fake.DotNet.Testing.Expecto", "Running expecto test runner"
      "Fake.DotNet.Testing.MSpec", "Running mspec test runner"
      "Fake.DotNet.Testing.MSTest", "Running mstest test runner"
      "Fake.DotNet.Testing.NUnit", "Running nunit test runner"
      "Fake.DotNet.Testing.OpenCover", "Code coverage with OpenCover"
      "Fake.DotNet.Testing.SpecFlow", "BDD with Gherkin and SpecFlow"
      "Fake.DotNet.Testing.VSTest", "Running vstest test runner"
      "Fake.DotNet.Testing.XUnit2", "Running xunit test runner"
      "Fake.DotNet.Xamarin", "Running Xamarin builds"
      "Fake.DotNet.Xdt", "Running XML Transforms"
      "Fake.Installer.InnoSetup", "Creating installers with InnoSetup"
      "Fake.Installer.Squirrel", "Squirrel for windows Squirrel.exe tool helper"
      "Fake.Installer.Wix", "WiX helper to create msi installers"
      "Fake.IO.FileSystem", "Core Filesystem utilities and globbing support"
      "Fake.IO.Zip", "Core Zip functionality"
      "Fake.JavaScript.Npm", "Running npm commands"
      "Fake.JavaScript.Yarn", "Running Yarn commands"
      "Fake.JavaScript.TypeScript", "Running TypeScript compiler"
      "Fake.Net.Http", "HTTP Client"
      "Fake.netcore", "Command line tool"
      "Fake.Runtime", "Core runtime features"
      "Fake.Sql.DacPac", "Sql Server Data Tools DacPac operations (Obsolete: Use Fake.Sql.SqlPackage instead)"
      "Fake.Sql.SqlPackage", "Sql Server Data Tools DacPac operations"
      "Fake.Sql.SqlServer", "Helpers around interacting with SQL Server databases"
      "Fake.Testing.Common", "Common testing data types"
      "Fake.Testing.ReportGenerator", "Convert XML coverage output to various formats"
      "Fake.Testing.SonarQube", "Analyzing your project with SonarQube"
      "Fake.Testing.Fixie", "Running fixie unit tests"
      "Fake.Tools.Git", "Running git commands"
      "Fake.Tools.GitVersion", "GitVersion helper"
      "Fake.Tools.Octo", "Octopus Deploy octo.exe tool helper"
      "Fake.Tools.Pickles", "Convert Gherkin to HTML"
      "Fake.Tools.Rsync", "Running Rsync commands"
      "Fake.Tools.SignTool", "Running signtool commands"
      "Fake.Tracing.NAntXml", "NAntXml"
      "Fake.Windows.Chocolatey", "Running and packaging with Chocolatey"
      "Fake.Windows.Registry", "CRUD functionality for Windows registry" ]

let assemblyInfos =
   (``Legacy-build``.legacyAssemblyInfos |> List.map (fun (proj, desc) -> proj, desc @ common)) @
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

Target.create "StartBootstrapBuild" (fun _ ->
    // Prepare stuff
    let token = githubtoken.Value
    let auth = sprintf "%s:x-oauth-basic@" token
    let url = sprintf "https://%sgithub.com/%s/%s.git" auth github_release_user gitName
    let gitDirectory = getVarOrDefault "git_directory" "."
    let remoteUrl =
        if not BuildServer.isLocalBuild then
            Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.email matthi.d@gmail.com"
            Git.CommandHelper.directRunGitCommandAndFail gitDirectory "config user.name \"Matthias Dittrich\""
            url
        else "origin"
    if BuildServer.buildServer = BuildServer.TeamFoundation then
        Trace.trace "Prepare git directory"
        Git.Branches.checkout gitDirectory false TeamFoundation.Environment.BuildSourceVersion

    let oldBranch = Git.Information.getBranchName gitDirectory
    let branchName = sprintf "release-stage/%s" nugetVersion
    Git.Branches.checkout gitDirectory true branchName

    // update paket.dependencies
    let depsFile = File.ReadAllText (gitDirectory </> "paket.dependencies")
    let replacedFile =
        depsFile
            .Replace("// FAKE_MYGET_FEED (don't edit this line)", "source https://www.myget.org/F/fake-vsts/api/v3/index.json")
            .Replace("prerelease // FAKE_VERSION (don't edit this line)", nugetVersion)
    File.WriteAllText(gitDirectory </> "paket.dependencies", replacedFile)

    // paket update
    callpaket gitDirectory "update --group NetcoreBuild"

    // push to branch
    Git.Staging.stageAll gitDirectory
    Git.Commit.exec gitDirectory (sprintf "Bootstrap check for %s" simpleVersion)
    Git.Branches.pushBranch gitDirectory remoteUrl branchName
    let sha = Git.Information.getCurrentSHA1 gitDirectory

    // check status API
    let startTime = System.Diagnostics.Stopwatch.StartNew()
    let maxTime = System.TimeSpan.FromMinutes 60.0
    let formatState (state:Octokit.CommitStatus) =
        sprintf "{ State: %O, Description: %O, TargetUrl: %O }"
            state.State state.Description state.TargetUrl
    let result =
        async {
            let! client = GitHub.createClientWithToken token
            let mutable whileResult = None
            while startTime.Elapsed < maxTime && whileResult.IsNone do
                let! combStatus = client.Repository.Status.GetCombined(github_release_user, gitName, sha) |> Async.AwaitTask
                let doWait () =
                    async {
                        Trace.trace "GitHub state is still pending:"
                        for status in combStatus.Statuses do
                            Trace.trace (sprintf " - %s" (formatState status))
                        do! Async.Sleep (1000 * 60 * 2) // wait 2 minutes
                    }

                match combStatus.State.Value with
                | _ when combStatus.TotalCount < 2 -> // not yet notified
                    do! doWait()
                | Octokit.CommitState.Success ->
                    whileResult <- Some <| Result.Ok ()
                    ()
                | Octokit.CommitState.Error | Octokit.CommitState.Failure ->
                    whileResult <- Some <| Result.Error combStatus
                    ()
                | _ -> // pending
                    do! doWait()
            match whileResult with
            | Some r -> return r
            | None ->
                // time is up
                let! combStatus = client.Repository.Status.GetCombined(github_release_user, gitName, sha) |> Async.AwaitTask
                return
                    match combStatus.State.Value with
                    | Octokit.CommitState.Error | Octokit.CommitState.Failure ->
                        Result.Error combStatus
                    | _ -> // if pending then some ci is just crasy slow
                        Result.Ok ()
        } |> Async.RunSynchronously
    match result with
    | Result.Ok () ->
        Trace.trace "All CI systems returned OK or pending... -> OK"
    | Result.Error combStatus ->
        System.String.Join("\n - ", combStatus.Statuses |> Seq.map formatState)
        |> failwithf "At least one CI failed:\n - %s"
)

Target.create "UnskipAssemblyInfo" (fun _ ->
    for assemblyFile, _ in assemblyInfos do
        // Unskip assemblyinfos, needed if you want to checkin changes...
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --no-skip-worktree %s" assemblyFile)
        ()
)

Target.create "UnskipAndRevertAssemblyInfo" (fun _ ->
    for assemblyFile, _ in assemblyInfos do
        // While the files are skipped in can be hard to switch between branches
        // Therefore we unskip and revert here.
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "update-index --no-skip-worktree %s" assemblyFile)
        Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "checkout HEAD %s" assemblyFile)
        ()
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

    // Legacy v4 and v5 documentation
    let buildLegacyFromDocsDir layoutRoots fakeLegacyApidocsDir prefix githubBranch toolsDir =
        Directory.ensure fakeLegacyApidocsDir
        let fakeLegacyDlls, fakeLegacyLibDirs =
            !! (toolsDir + "/Fake.*.dll")
              ++ (toolsDir + "/FakeLib.dll")
              -- (toolsDir + "/Fake.Experimental.dll")
              -- (toolsDir + "/FSharp.Compiler.Service.dll")
              -- (toolsDir + "/FAKE.FSharp.Compiler.Service.dll")
              -- (toolsDir + "/Fake.IIS.dll")
            |> dllsAndLibDirs

        fakeLegacyDlls
        |> FSFormatting.createDocsForDlls (fun s ->
            { s with
                OutputDirectory = fakeLegacyApidocsDir
                LayoutRoots = layoutRoots
                LibDirs = fakeLegacyLibDirs
                // TODO: CurrentPage shouldn't be required as it's written in the template, but it is -> investigate
                ProjectParameters = ("api-docs-prefix", prefix) ::("CurrentPage", "APIReference") :: projInfo
                SourceRepository = githubLink + githubBranch })

    // FAKE 5 legacy documentation
    if buildLegacy then
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
    else
        buildLegacyFromDocsDir legacyLayoutRoots (apidocsDir @@ "v5/legacy") "/apidocs/v5/legacy/" "/blob/master" ("packages/docslegacyv5/FAKE/tools")

    // FAKE 4 legacy documentation
    buildLegacyFromDocsDir fake4LayoutRoots (apidocsDir @@ "v4") "/apidocs/v4/" "/blob/hotfix_fake4" ("packages/docslegacyv4/FAKE/tools")
)

let startWebServer () =
    let rec findPort port =
        let portIsTaken = false
            //if Environment.isMono then false else
            //System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            //|> Seq.exists (fun x -> x.Port = port)

        if portIsTaken then findPort (port + 1) else port

    let port = findPort 8083

    let inline (@@) a b = Suave.WebPart.concatenate a b
    let mimeTypes =
        Suave.Writers.defaultMimeTypesMap
        @@ (function
            | ".avi" -> Suave.Writers.createMimeType "video/avi" false
            | ".mp4" -> Suave.Writers.createMimeType "video/mp4" false
            | _ -> None)
    let serverConfig =
        { Suave.Web.defaultConfig with
           homeFolder = Some (Path.GetFullPath docsDir)
           mimeTypesMap = mimeTypes
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

let runExpecto workDir dllPath resultsXml =
    let processResult =
        DotNet.exec (dtntWorkDir workDir) (sprintf "%s" dllPath) "--summary"

    if processResult.ExitCode <> 0 then failwithf "Tests in %s failed." (Path.GetFileName dllPath)
    Trace.publish (ImportData.Nunit NunitDataVersion.Nunit) (workDir </> resultsXml)

Target.create "DotNetCoreIntegrationTests" (fun _ ->
    cleanForTests()

    runExpecto root "src/test/Fake.Core.IntegrationTests/bin/Release/netcoreapp2.1/Fake.Core.IntegrationTests.dll" "Fake_Core_IntegrationTests.TestResults.xml"
)

Target.create "TemplateIntegrationTests" (fun _ ->
    let targetDir = srcDir </> "test" </> "Fake.DotNet.Cli.IntegrationTests"
    runExpecto targetDir "bin/Release/netcoreapp2.1/Fake.DotNet.Cli.IntegrationTests.dll" "Fake_DotNet_Cli_IntegrationTests.TestResults.xml"
)

Target.create "DotNetCoreUnitTests" (fun _ ->
    // dotnet run -p src/test/Fake.Core.UnitTests/Fake.Core.UnitTests.fsproj
    runExpecto root "src/test/Fake.Core.UnitTests/bin/Release/netcoreapp2.1/Fake.Core.UnitTests.dll" ("Fake_Core_UnitTests.TestResults.xml")

    // dotnet run --project src/test/Fake.Core.CommandLine.UnitTests/Fake.Core.CommandLine.UnitTests.fsproj
    runExpecto root "src/test/Fake.Core.CommandLine.UnitTests/bin/Release/netcoreapp2.1/Fake.Core.CommandLine.UnitTests.dll" ("Fake_Core_CommandLine_UnitTests.TestResults.xml")
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
                if Environment.isUnix then nugetDncDir </> "Fake.netcore/current/fake"
                else nugetDncDir </> "Fake.netcore/current/fake.exe"
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


let runtimes =
  [ "win7-x86"; "win7-x64"; "osx.10.11-x64"; "linux-x64" ]

module CircleCi =
    let isCircleCi = Environment.environVarAsBool "CIRCLECI"


let publishRuntime runtimeName =
    let runtimeDir = sprintf "%s/Fake.netcore/%s" nugetDncDir runtimeName
    let zipFile = sprintf "%s/Fake.netcore/fake-dotnetcore-%s.zip" nugetDncDir runtimeName
    !! (sprintf "%s/**" runtimeDir)
    |> Zip.zip runtimeDir zipFile

    publish zipFile

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

            // Create zip
            if runtimeName <> "current" then
                publishRuntime runtimeName
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

    publishRuntime "portable"
)

let setBuildEnvVars versionVar isDebianPackaging =
    Environment.setEnvironVar "GenerateDocumentationFile" "true"
    Environment.setEnvironVar "PackageVersion" versionVar
    Environment.setEnvironVar "Version" versionVar
    Environment.setEnvironVar "Authors" (String.separated ";" authors)
    Environment.setEnvironVar "Description" projectDescription
    Environment.setEnvironVar "PackageReleaseNotes" (release.Notes |> String.toLines)
    Environment.setEnvironVar "SourceLinkCreate" "false"
    Environment.setEnvironVar "PackageTags" "build;fake;f#"
    Environment.setEnvironVar "PackageIconUrl" "https://raw.githubusercontent.com/fsharp/FAKE/7305422ea912e23c1c5300b23b3d0d7d8ec7d27f/help/content/pics/logo.png"
    Environment.setEnvironVar "PackageProjectUrl" "https://fake.build"
    Environment.setEnvironVar "PackageRepositoryUrl" "https://github.com/fsharp/Fake"
    Environment.setEnvironVar "PackageRepositoryType" "git"
    Environment.setEnvironVar "PackageLicenseExpression" "Apache-2.0 OR MS-PL"
    Environment.setEnvironVar "IsDebianPackaging" (if isDebianPackaging then "true" else "false")
    //Environment.setEnvironVar "IncludeSource" "true"
    //Environment.setEnvironVar "IncludeSymbols" "false"
    // for github package management to allow uploading the package... -> We need to re-package...
    //Environment.setEnvironVar "PackageProjectUrl" (sprintf "https://github.com/%s/%s" github_release_user gitName)

Target.create "_DotNetPackage" (fun _ ->
    let nugetDir = System.IO.Path.GetFullPath nugetDncDir
    // This lines actually ensures we get the correct version checked in
    // instead of the one previously bundled with `fake` or `paket`
    callpaket "." "restore" // first make paket restore its target file if it feels like it.
    Git.CommandHelper.gitCommand "" "checkout .paket/Paket.Restore.targets" // now restore ours

    restoreTools()
    setBuildEnvVars nugetVersion false
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

    // build zip package
    Directory.ensure (nugetDncDir </> "Fake.netcore")
    let zipFile = nugetDncDir </> "Fake.netcore/fake-dotnetcore-packages.zip"
    !! (nugetDncDir </> "*.nupkg")
    -- (nugetDncDir </> "*.symbols.nupkg")
    |> Zip.zip nugetDncDir zipFile
    publish zipFile

    // TODO: Check if we run the test in the current build!
    Directory.ensure "temp"
    let testZip = "temp/tests.zip"
    !! "src/test/*/bin/Release/netcoreapp2.1/**"
    |> Zip.zip "src/test" testZip
    publish testZip
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
    Directory.ensure chocoReleaseDir
    Choco.packFromTemplate (fun p ->
        { p with
            PackageId = "fake"
            ReleaseNotes = release.Notes |> String.toLines
            InstallerType = Choco.ChocolateyInstallerType.SelfContained
            Version = chocoVersion
            Files =
                [ (System.IO.Path.GetFullPath (nugetDncDir </> @"Fake.netcore\win7-x86")) + @"\**", Some "bin", None
                  (System.IO.Path.GetFullPath @"src\VERIFICATION.txt"), Some "VERIFICATION.txt", None
                  (System.IO.Path.GetFullPath @"License.txt"), Some "LICENSE.txt", None ]
            OutputDir = chocoReleaseDir }
        |> changeToolPath) "src/Fake-choco-template.nuspec"

    let name = sprintf "%s.%s" "fake" chocoVersion
    let chocoPackage = sprintf "%s/%s.nupkg" chocoReleaseDir name
    let chocoTargetPackage = sprintf "%s/chocolatey-%s.nupkg" chocoReleaseDir name
    File.Copy(chocoPackage, chocoTargetPackage, true)
    publish chocoTargetPackage
)
Target.create "DotNetCorePushChocolateyPackage" (fun _ ->
    let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
    let path = sprintf "%s/%s" chocoReleaseDir name
    if not Environment.isWindows && not (File.exists path) && fromArtifacts then
        Directory.ensure chocoReleaseDir
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
    for secret in ``Legacy-build``.secrets do
        secret.Force() |> ignore
)


Target.create "DotNetCoreCreateDebianPackage" (fun _ ->
    let runtime = "linux-x64"
    let targetFramework = "netcoreapp2.1"
    let args =
        [
            sprintf "--runtime %s" runtime
            sprintf "--framework %s" targetFramework
            sprintf "--configuration %s" "Release"
            sprintf "--output %s" (Path.GetFullPath nugetDncDir)
        ] |> String.concat " "
    setBuildEnvVars simpleVersion true
    let result =
        DotNet.exec (fun opt ->
            { opt with
                WorkingDirectory = "src/app/fake-cli/" } |> dtntSmpl
        ) "deb" args
    if not result.OK then
        failwith "Debian package creation failed"


    let fileName = sprintf "fake-cli.%s.%s.deb" simpleVersion runtime
    let target = sprintf "%s/%s" nugetDncDir fileName
    publish target
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
    !! (appDir </> "*/*.fsproj")
    -- (appDir </> "Fake.netcore/*.fsproj")
    ++ (templateDir </> "*/*.fsproj")
    |> Seq.iter(fun proj ->
        let projName = Path.GetFileName(Path.GetDirectoryName proj)
        !! (sprintf "%s/%s.*.nupkg" nugetDncDir projName)
        -- (sprintf "%s/%s.*.symbols.nupkg" nugetDncDir projName)
        |> Seq.iter (nugetPush 4))
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

    let gitDirectory = getVarOrDefault "git_directory" ""
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

    let linuxRuntime = "linux-x64"
    let debFileName = sprintf "fake-cli.%s.%s.deb" simpleVersion linuxRuntime
    let debTarget = sprintf "%s/%s" nugetDncDir debFileName

    let files =
        runtimes @ [ "portable"; "packages" ]
        |> List.map (fun n -> sprintf "%s/Fake.netcore/fake-dotnetcore-%s.zip" nugetDncDir n)
        |> fun l -> l @ [ debTarget ]

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease github_release_user gitName simpleVersion (release.SemVer.PreRelease <> None) release.Notes
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
)

Target.create "Release_Staging" (fun _ -> ())

open System.IO.Compression

Target.create "PrepareArtifacts" (fun _ ->
    if not fromArtifacts then
        Trace.trace "empty artifactsDir."
    else
        Trace.trace "ensure artifacts."
        let files =
            !! (artifactsDir </> "fake-dotnetcore-*.zip")
            |> Seq.toList
        Trace.tracefn "files: %A" files
        files
        |> Shell.copy (nugetDncDir </> "Fake.netcore")

        Zip.unzip nugetDncDir (artifactsDir </> "fake-dotnetcore-packages.zip")

        if Environment.isWindows then
            Directory.ensure chocoReleaseDir
            let name = sprintf "%s.%s.nupkg" "fake" chocoVersion
            Shell.copyFile (sprintf "%s/%s" chocoReleaseDir name) (artifactsDir </> sprintf "chocolatey-%s" name)
        else
            Zip.unzip "." (artifactsDir </> "chocolatey-requirements.zip")

        if buildLegacy then
            Directory.ensure nugetLegacyDir
            Zip.unzip nugetLegacyDir (artifactsDir </> "fake-legacy-packages.zip")

            Directory.ensure "temp/build"
            !! (nugetLegacyDir </> "*.nupkg")
            |> Seq.iter (fun pack ->
                Zip.unzip "temp/build" pack
            )
            Shell.copyDir "build" "temp/build" (fun _ -> true)

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
Target.description "Default Build all artifacts and documentation"
Target.create "Default" ignore
Target.create "_StartDnc" ignore
Target.description "Simple local command line release"
Target.create "Release" ignore
Target.description "dotnet pack pack to build all nuget packages"
Target.create "DotNetPackage" ignore
Target.create "_AfterBuild" ignore
Target.description "Build and test the dotnet sdk part (fake 5 - no legacy)"
Target.create "FullDotNetCore" ignore
Target.description "publish fake 5 runner for various platforms"
Target.create "DotNetPublish" ignore
Target.description "Run the tests - if artifacts are available via 'artifactsdirectory' those are used."
Target.create "RunTests" ignore
Target.description "Generate the docs (potentially from artifacts) and publish as artifact."
Target.create "Release_GenerateDocs" (fun _ ->
    let testZip = "temp/docs.zip"
    !! "docs/**"
    |> Zip.zip "docs" testZip
    publish testZip
)

Target.description "Full Build & Test and publish results as artifacts."
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
    ?=> "SetAssemblyInfo"
    ==> "_DotNetPackage"
    ?=> "UnskipAndRevertAssemblyInfo"
    ==> "DotNetPackage"
"_StartDnc"
    ==> "_DotNetPackage"
"_DotNetPackage"
    ==> "DotNetPackage"

let mutable prev = None
for runtime in "current" :: "portable" :: runtimes do
    let rawTargetName = sprintf "_DotNetPublish_%s" runtime
    let targetName = sprintf "DotNetPublish_%s" runtime
    Target.description (sprintf "publish fake 5 runner for %s" runtime)
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
    targetName
        ==> "DotNetPublish"
        |> ignore

    // Make sure we order then (when building parallel!)
    match prev with
    | Some prev -> prev ?=> rawTargetName |> ignore
    | None -> "_DotNetPackage" ?=> rawTargetName |> ignore
    prev <- Some rawTargetName

if buildLegacy then
    ``Legacy-build``.setTargetDependencies fromArtifacts

"DotNetPackage"
    ==> "_AfterBuild"
"DotNetPublish"
    ==> "_AfterBuild"


// Create artifacts when build is finished
"_AfterBuild"
    =?> ("DotNetCoreCreateChocolateyPackage", Environment.isWindows)
    ==> "DotNetCoreCreateDebianPackage"
    =?> ("GenerateDocs", BuildServer.isLocalBuild && Environment.isWindows)
    ==> "Default"

(if fromArtifacts then "PrepareArtifacts" else "_AfterBuild")
    =?> ("GenerateDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "Default"
"_AfterBuild" ?=> "GenerateDocs"

"GenerateDocs"
    ==> "Release_GenerateDocs"

// Build artifacts only (no testing)
"DotNetCoreCreateChocolateyPackage"
    =?> ("BuildArtifacts", Environment.isWindows)


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
    ==> "TemplateIntegrationTests"
    ==> "FullDotNetCore"
    ==> "Default"

// Artifacts & Tests
"Default" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ?=> "BuildArtifacts"
"BuildArtifacts" ==> "Release_BuildAndTest"
"Release_GenerateDocs" ==> "Release_BuildAndTest"


// Release stuff ('FastRelease' is to release after running 'Default')
(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("DotNetCorePushChocolateyPackage", Environment.isWindows && chocosource <> "disabled")
    ==> "FastRelease"
"EnsureTestsRun" ?=> "DotNetCorePushChocolateyPackage"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("ReleaseDocs", not <| Environment.hasEnvironVar "SkipDocs")
    ==> "FastRelease"
"EnsureTestsRun" ?=> "ReleaseDocs"

(if fromArtifacts then "PrepareArtifacts" else "EnsureTestsRun")
    =?> ("DotNetCorePushNuGet", nugetsource <> "disabled")
    ==> "FastRelease"

if nugetsource <> "disabled" then
    ignore ("EnsureTestsRun" ?=> "DotNetCorePushNuGet")


// Gitlab staging (myget release)
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
