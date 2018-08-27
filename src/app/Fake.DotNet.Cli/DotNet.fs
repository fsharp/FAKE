/// .NET Core + CLI tools helpers
namespace Fake.DotNet

[<RequireQualifiedAccess>]
module DotNet =

    // NOTE: The #if can be removed once we have a working release with the "new" API
    // Currently we #load this file in build.fsx

    open Fake.Core
    open Fake.IO
    open Fake.IO.FileSystemOperators
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text
    open Newtonsoft.Json.Linq

    /// .NET Core SDK default install directory (set to default SDK installer paths (%HOME/.dotnet or %LOCALAPPDATA%/Microsoft/dotnet).
    let internal defaultUserInstallDir =
        if Environment.isUnix
        then Environment.environVar "HOME" @@ ".dotnet"
        else Environment.environVar "LocalAppData" @@ "Microsoft" @@ "dotnet"

    /// .NET Core SDK default install directory (set to default SDK installer paths (/usr/local/share/dotnet or C:\Program Files\dotnet))
    let internal defaultSystemInstallDir =
        if Environment.isUnix
        then "/usr/local/share/dotnet"
        else @"C:\Program Files\dotnet"

    /// Gets the DotNet SDK from the global.json, starts searching in the given directory.
    let internal getSDKVersionFromGlobalJsonDir startDir : string =
        let globalJsonPaths rootDir =
            let rec loop (dir: DirectoryInfo) = seq {
                match dir.GetFiles "global.json" with
                | [| json |] -> yield json
                | _ -> ()
                if not (isNull dir.Parent) then
                    yield! loop dir.Parent
            }
            loop (DirectoryInfo rootDir)

        match Seq.tryHead (globalJsonPaths startDir) with
        | None ->
            failwithf "global.json not found"
        | Some globalJson ->
            try
                let content = File.ReadAllText globalJson.FullName
                let json = JObject.Parse content
                let sdk = json.Item("sdk") :?> JObject
                let version = sdk.Property("version").Value.ToString()
                version
            with
            | exn -> failwithf "Could not parse `sdk.version` from global.json at '%s': %s" globalJson.FullName exn.Message

    /// Gets the DotNet SDK from the global.json
    /// This file can exist in the working directory or any of the parent directories
    let getSDKVersionFromGlobalJson() : string = getSDKVersionFromGlobalJsonDir "."


    /// Get dotnet cli executable path. Probes the provided path first, then as a fallback tries the system PATH
    /// ## Parameters
    ///
    /// - 'dotnetCliDir' - the path to check else will probe system PATH
    let private findPossibleDotnetCliPaths dotnetCliDir = seq {
        let fileName = if Environment.isUnix then "dotnet" else "dotnet.exe"
        yield!
            Process.findFilesOnPath "dotnet"
            |> Seq.filter File.Exists
        let userInstalldir = defaultUserInstallDir </> fileName
        if File.exists userInstalldir then yield userInstalldir
        let systemInstalldir = defaultSystemInstallDir </> fileName
        if File.exists systemInstalldir then yield systemInstalldir
        match dotnetCliDir with
        | Some userSetPath ->
            let defaultCliPath = userSetPath @@ fileName
            match File.Exists defaultCliPath with
            | true -> yield defaultCliPath
            | _ -> ()
        | None -> () }

    /// Get .NET Core SDK download uri
    let private getGenericDotNetCliInstallerUrl branch installerName =
        sprintf "https://raw.githubusercontent.com/dotnet/cli/%s/scripts/obtain/%s" branch installerName

    let private getPowershellDotNetCliInstallerUrl branch = getGenericDotNetCliInstallerUrl branch "dotnet-install.ps1"
    let private getBashDotNetCliInstallerUrl branch = getGenericDotNetCliInstallerUrl branch "dotnet-install.sh"


    /// Download .NET Core SDK installer
    let private downloadDotNetInstallerFromUrl (url:string) fileName =
        //let url = getDotNetCliInstallerUrl branch
    #if USE_HTTPCLIENT
        let h = new System.Net.Http.HttpClient();
        use f = File.Open(fileName, FileMode.Create);
        h.GetStreamAsync(url).Result.CopyTo(f);
    #else
        use w = new System.Net.WebClient()
        w.DownloadFile(url, fileName) // Http.RequestStream url
    #endif
        //use outFile = File.Open(fileName, FileMode.Create)
        //installScript.ResponseStream.CopyTo(outFile)
        Trace.trace (sprintf "downloaded dotnet installer (%s) to %s" url fileName)

    /// [omit]
    let private md5 (data : byte array) : string =
        use md5 = MD5.Create()
        (StringBuilder(), md5.ComputeHash(data))
        ||> Array.fold (fun sb b -> sb.Append(b.ToString("x2")))
        |> string


    /// .NET Core SDK installer download options
    type InstallerOptions =
        {
            /// Always download install script (otherwise install script is cached in temporary folder)
            AlwaysDownload: bool;
            /// Download installer from this github branch
            Branch: string;
        }

        /// Parameter default values.
        static member Default = {
            AlwaysDownload = false
            Branch = "master"
        }

    /// Download .NET Core SDK installer
    /// ## Parameters
    ///
    /// - 'setParams' - set download installer options
    let downloadInstaller setParams =
        let param = InstallerOptions.Default |> setParams

        let ext = if Environment.isUnix then "sh" else "ps1"
        let getInstallerUrl = if Environment.isUnix then getBashDotNetCliInstallerUrl else getPowershellDotNetCliInstallerUrl
        let scriptName =
            sprintf "dotnet_install_%s.%s" (md5 (Encoding.ASCII.GetBytes(param.Branch))) ext
        let tempInstallerScript = Path.GetTempPath() @@ scriptName

        // maybe download installer script
        match param.AlwaysDownload || not(File.Exists(tempInstallerScript)) with
            | true ->
                let url = getInstallerUrl param.Branch
                downloadDotNetInstallerFromUrl url tempInstallerScript
            | _ -> ()

        tempInstallerScript


    /// .NET Core SDK architecture
    type CliArchitecture =
        /// this value represents currently running OS architecture
        | Auto
        | X86
        | X64

    /// .NET Core SDK version (used to specify version when installing .NET Core SDK)
    type CliVersion =
        /// most latest build on specific channel
        | Latest
        ///  last known good version on specific channel (Note: LKG work is in progress. Once the work is finished, this will become new default)
        | Lkg
        /// 4-part version in a format A.B.C.D - represents specific version of build
        | Version of string
        /// Take version from global.json and fail if it is not found.
        | GlobalJson

    /// .NET Core SDK install options
    [<NoComparison>]
    [<NoEquality>]
    type CliInstallOptions =
        {
            /// Custom installer obtain (download) options
            InstallerOptions: InstallerOptions -> InstallerOptions
            /// .NET Core SDK channel (defaults to normalized installer branch)
            Channel: string option
            /// .NET Core SDK version
            Version: CliVersion
            /// Custom installation directory (for local build installation)
            CustomInstallDir: string option
            /// Architecture
            Architecture: CliArchitecture
            /// Include symbols in the installation (Switch does not work yet. Symbols zip is not being uploaded yet)
            DebugSymbols: bool
            /// If set it will not perform installation but instead display what command line to use
            DryRun: bool
            /// Do not update path variable
            NoPath: bool
        }

        /// Parameter default values.
        static member Default = {
            InstallerOptions = id
            Channel = None
            Version = Latest
            CustomInstallDir = None
            Architecture = Auto
            DebugSymbols = false
            DryRun = false
            NoPath = true
        }

    /// The a list of well-known versions to install
    module Versions =
        /// .NET Core SDK install options preconfigured for preview2 tooling
        let internal Preview2ToolingOptions options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "v1.0.0-preview2"
                    })
                Channel = Some "preview"
                Version = Version "1.0.0-preview2-003121"
            }

        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal LatestPreview4ToolingOptions options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "rel/1.0.0-preview4"
                    })
                Channel = None
                Version = Latest
            }
        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal Preview4_004233ToolingOptions options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "rel/1.0.0-preview4"
                    })
                Channel = None
                Version = Version "1.0.0-preview4-004233"
            }
        /// .NET Core SDK install options preconfigured for preview4 tooling
        let internal RC4_004771ToolingOptions options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "rel/1.0.0-rc3"
                    })
                Channel = None
                Version = Version "1.0.0-rc4-004771"
            }

        /// .NET Core SDK install options preconfigured for preview4 tooling, this is marketized as v1.0.1 release of the .NET Core tools
        let internal RC4_004973ToolingOptions options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "rel/1.0.1"
                    })
                Channel = None
                Version = Version "1.0.3-rc4-004973"
            }

        let Release_1_0_4 options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.0.0"
                    })
                Channel = None
                Version = Version "1.0.4"
            }

        let Release_2_0_0 options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.0.0"
                    })
                Channel = None
                Version = Version "2.0.0"
            }

        let Release_2_0_3 options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.0.0"
                    })
                Channel = None
                Version = Version "2.0.3"
            }

        let Release_2_1_4 options =
            { options with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.1"
                    })
                Channel = None
                Version = Version "2.1.4"
            }

        let Release_2_1_300_RC1 option =
            { option with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.1"
                    })
                Channel = None
                Version = Version "2.1.300-rc1-008673"
            }

        let Release_2_1_300 option =
            { option with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.1"
                    })
                Channel = None
                Version = Version "2.1.300"
            }

        let Release_2_1_301 option =
            { option with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.1"
                    })
                Channel = None
                Version = Version "2.1.301"
            }

        let Release_2_1_302 option =
            { option with
                InstallerOptions = (fun io ->
                    { io with
                        Branch = "release/2.1"
                    })
                Channel = None
                Version = Version "2.1.302"
            }

        let FromGlobalJson option =
            { option with
                InstallerOptions = id
                Channel = None
                Version = CliVersion.GlobalJson
            }


    /// .NET Core SDK install options preconfigured for preview2 tooling
    [<Obsolete "Please use a stable release at this point">]
    let Preview2ToolingOptions options = Versions.Preview4_004233ToolingOptions options

    /// .NET Core SDK install options preconfigured for preview4 tooling
    [<Obsolete "Please use a stable release at this point">]
    let LatestPreview4ToolingOptions options = Versions.LatestPreview4ToolingOptions options

    /// .NET Core SDK install options preconfigured for preview4 tooling
    [<Obsolete "Please use a stable release at this point">]
    let RC4_004771ToolingOptions options = Versions.RC4_004771ToolingOptions options

    /// .NET Core SDK install options preconfigured for preview4 tooling, this is marketized as v1.0.1 release of the .NET Core tools
    [<Obsolete "Please use a stable release at this point">]
    let RC4_004973ToolingOptions options = Versions.RC4_004973ToolingOptions options

    [<Obsolete "Please use DotNet.Versions.Release_1_0_4 instead">]
    let Release_1_0_4 options = Versions.Release_1_0_4 options

    [<Obsolete "Please use DotNet.Versions.Release_2_0_0 instead">]
    let Release_2_0_0 options = Versions.Release_2_0_0 options

    [<Obsolete "Please use DotNet.Versions.Release_2_0_3 instead">]
    let Release_2_0_3 options = Versions.Release_2_0_3 options

    [<Obsolete "Please use DotNet.Versions.Release_2_1_4 instead">]
    let Release_2_1_4 options =Versions.Release_2_1_4 options

    [<Obsolete "Please use DotNet.Versions.Release_2_1_300_RC1 instead">]
    let Release_2_1_300_RC1 option = Versions.Release_2_1_300_RC1 option

    [<Obsolete "Please use DotNet.Versions.Release_2_1_300 instead">]
    let Release_2_1_300 option = Versions.Release_2_1_300 option

    /// [omit]
    let private optionToParam option paramFormat =
        match option with
        | Some value -> sprintf paramFormat value
        | None -> ""

    /// [omit]
    let private boolToFlag value flagParam =
        match value with
        | true -> flagParam
        | false -> ""

    /// [omit]
    let private buildDotNetCliInstallArgs (param: CliInstallOptions) =
        let versionParamValue =
            match param.Version with
            | Latest -> "latest"
            | Lkg -> "lkg"
            | Version ver -> ver
            | GlobalJson -> getSDKVersionFromGlobalJson()

        // get channel value from installer branch info
        let channelParamValue =
            match param.Channel with
                | Some ch -> ch
                | None ->
                    let installerOptions = InstallerOptions.Default |> param.InstallerOptions
                    installerOptions.Branch |> String.replace "/" "-"

        let architectureParamValue =
            match param.Architecture with
            | Auto -> None
            | X86 -> Some "x86"
            | X64 -> Some "x64"
        [
            Process.boolParam ("Verbose", true)
            Process.stringParam ("Channel", channelParamValue)
            Process.stringParam ("Version", versionParamValue)
            Process.optionParam ("Architecture", architectureParamValue)
            Process.stringParam ("InstallDir", defaultArg param.CustomInstallDir defaultUserInstallDir)
            Process.boolParam ("DebugSymbols", param.DebugSymbols)
            Process.boolParam ("DryRun", param.DryRun)
            Process.boolParam ("NoPath", param.NoPath)
        ] |> Process.parametersToString "-" " "


    /// dotnet restore verbosity
    type Verbosity =
        | Quiet
        | Minimal
        | Normal
        | Detailed
        | Diagnostic

    /// dotnet cli command execution options
    type Options =
        {
            /// DotNet cli executable path
            DotNetCliPath: string
            /// Write a global.json with the given version (required to make SDK choose the correct version)
            Version : string option
            /// Command working directory
            WorkingDirectory: string
            /// Custom parameters
            CustomParams: string option
            /// Logging verbosity (--verbosity)
            Verbosity: Verbosity option
            /// Restore logging verbosity (--diagnostics)
            Diagnostics: bool
            /// If true the function will redirect the output of the called process (but will disable colors, false by default)
            RedirectOutput : bool
            /// Gets the environment variables that apply to this process and its child processes.
            /// NOTE: Recommendation is to not use this Field, but instead use the helper function in the Proc module (for example Process.setEnvironmentVariable)
            /// NOTE: This field is ignored when UseShellExecute is true.
            Environment : Map<string, string>
        }
        static member Create() = {
            DotNetCliPath =
                findPossibleDotnetCliPaths None
                |> Seq.tryHead
                // shouldn't hit this one because the previous two probe PATH...
                |> Option.defaultWith (fun () -> if Environment.isUnix then "dotnet" else "dotnet.exe")
            WorkingDirectory = Directory.GetCurrentDirectory()
            CustomParams = None
            Version = None
            Verbosity = None
            Diagnostics = false
            RedirectOutput = false
            Environment =
                Process.createEnvironmentMap()
                |> Map.remove "MSBUILD_EXE_PATH"
                |> Map.remove "MSBuildExtensionsPath"
                |> Map.remove "MSBuildLoadMicrosoftTargetsReadOnly"
                |> Map.remove "MSBuildSDKsPath"
                |> Map.remove "DOTNET_HOST_PATH"
        }
        [<Obsolete("Use Options.Create instead")>]
        static member Default = Options.Create()

        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Environment = map }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with RedirectOutput = shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f = f x

    module Options =
        let inline lift (f:Options -> Options) (x : ^a) =
            let inline withCommon s e = ((^a) : (member WithCommon : (Options -> Options) -> ^a) (s, e))
            withCommon x f

        let inline withEnvironment map x =
            lift (fun o -> o.WithEnvironment map) x

        let inline withRedirectOutput shouldRedirect x =
            lift (fun o -> o.WithRedirectOutput shouldRedirect) x

        let inline withWorkingDirectory wd x =
            lift (fun o -> { o with WorkingDirectory = wd}) x
        let inline withDiagnostics diag x =
            lift (fun o -> { o with Diagnostics = diag}) x
        let inline withVerbosity verb x =
            lift (fun o -> { o with Verbosity = verb}) x
        let inline withCustomParams args x =
            lift (fun o -> { o with CustomParams = args}) x
        /// Sets custom command-line arguments expressed as a sequence of strings.
        /// This function overwrites and gets overwritten by `withCustomParams`.
        let inline withAdditionalArgs args x =
            withCustomParams (args |> Args.toWindowsCommandLine |> (function | "" -> None | x -> Some x)) x
        let inline withDotNetCliPath path x =
            lift (fun o -> { o with DotNetCliPath = path}) x

    /// [omit]
    let private argList2 name values =
        values
        |> Seq.collect (fun v -> ["--" + name; sprintf @"""%s""" v])
        |> String.concat " "

    /// [omit]
    let private argOption name value =
        match value with
            | true -> sprintf "--%s" name
            | false -> ""

    /// [omit]
    let private buildCommonArgs (param: Options) =
        [   defaultArg param.CustomParams ""
            param.Verbosity |> Option.toList |> Seq.map (fun v -> v.ToString().ToLowerInvariant()) |> argList2 "verbosity"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "

    /// [omit]
    let private buildSdkOptionsArgs (param: Options) =
        [   param.Diagnostics |> argOption "--diagostics"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "

    let internal withGlobalJson workDir version f =
        let globalJsonPath =
            if (Path.GetFullPath workDir).StartsWith(Path.GetFullPath ".") then "global.json"
            else workDir </> "global.json"
        let writtenJson =
            match version with
            | Some version when Directory.Exists workDir ->
                // make sure to write global.json if we did not read the version from it
                // We need to do this as the SDK will use this file to select the actual version
                // See https://github.com/fsharp/FAKE/pull/1963 and related discussions
                if File.Exists globalJsonPath then
                    let readVersion = getSDKVersionFromGlobalJsonDir workDir
                    if readVersion <> version then failwithf "Existing global.json with a different version found!"
                    false
                else
                    let template = sprintf """{ "sdk": { "version": "%s" } }""" version
                    File.WriteAllText(globalJsonPath, template)
                    true
            | _ -> false
        try f ()
        finally if writtenJson then File.delete globalJsonPath

    /// Execute raw dotnet cli command
    /// ## Parameters
    ///
    /// - 'options' - common execution options
    /// - 'command' - the sdk command to execute 'test', 'new', 'build', ...
    /// - 'args' - command arguments
    let exec (buildOptions: Options -> Options) command args =
        let results = new System.Collections.Generic.List<Fake.Core.ConsoleMessage>()
        let timeout = TimeSpan.MaxValue

        let errorF msg =
            Trace.traceError msg
            results.Add (ConsoleMessage.CreateError msg)

        let messageF msg =
            Trace.trace msg
            results.Add (ConsoleMessage.CreateOut msg)

        let options = buildOptions (Options.Create())
        let sdkOptions = buildSdkOptionsArgs options
        let commonOptions = buildCommonArgs options
        let cmdArgs = sprintf "%s %s %s %s" sdkOptions command commonOptions args

        let result =
            let f (info:ProcStartInfo) =
                let dir = System.IO.Path.GetDirectoryName options.DotNetCliPath
                let oldPath =
                    match options.Environment |> Map.tryFind "PATH" with
                    | None -> ""
                    | Some s -> s
                { info with
                    FileName = options.DotNetCliPath
                    WorkingDirectory = options.WorkingDirectory
                    Arguments = cmdArgs }
                |> Process.setEnvironment options.Environment
                |> Process.setEnvironmentVariable "PATH" (sprintf "%s%c%s" dir System.IO.Path.PathSeparator oldPath)


            withGlobalJson options.WorkingDirectory options.Version (fun () ->
                if options.RedirectOutput then
                  Process.execRaw f timeout true errorF messageF
                else Process.execSimple f timeout
            )
        ProcessResult.New result (results |> List.ofSeq)


    /// dotnet --info command options
    type InfoOptions =
        {
            /// Common tool options
            Common: Options;
        }
        /// Parameter default values.
        static member Create() = {
            Common = Options.Create().WithRedirectOutput true
        }
        [<Obsolete("Use InfoOptions.Create instead")>]
        static member Default = InfoOptions.Create()
        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }

    /// dotnet info result
    type InfoResult =
        {
            /// Common tool options
            RID: string;
        }

    /// Execute dotnet --info command
    /// ## Parameters
    ///
    /// - 'setParams' - set info command parameters
    let info setParams =
        use __ = Trace.traceTask "DotNet:info" "running dotnet --info"
        let param = InfoOptions.Create() |> setParams
        let args = "--info" // project (buildPackArgs param)
        let result = exec (fun _ -> param.Common) "" args
        if not result.OK then failwithf "dotnet --info failed with code %i" result.ExitCode

        let rid =
            result.Messages
            |> Seq.tryFind (fun m -> m.Contains "RID:")
            |> Option.map (fun line -> line.Split([|':'|]).[1].Trim())

        if rid.IsNone then failwithf "could not read rid from output: \n%s" (System.String.Join("\n", result.Messages))

        __.MarkSuccess()
        { RID = rid.Value }


    /// dotnet --version command options
    type VersionOptions =
        {
            /// Common tool options
            Common: Options;
        }
        /// Parameter default values.
        static member Create() = {
            Common = Options.Create().WithRedirectOutput true
        }
        /// Gets the current environment
        member x.Environment = x.Common.Environment

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            x.WithCommon (fun c -> { c with Environment = map })
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }


    /// dotnet info result
    type VersionResult = string

    /// Execute dotnet --version command
    /// ## Parameters
    ///
    /// - 'setParams' - set version command parameters
    let getVersion setParams =
        use __ = Trace.traceTask "DotNet:version" "running dotnet --version"
        let param = VersionOptions.Create() |> setParams
        let args = "--version"
        let result = exec (fun _ -> param.Common) "" args
        if not result.OK then failwithf "dotnet --version failed with code %i" result.ExitCode

        let version =
            result.Messages
            |> String.separated "\n"
            |> String.trim

        if String.isNullOrWhiteSpace version then failwithf "could not read version from output: \n%s" (System.String.Join("\n", result.Messages))

        __.MarkSuccess()
        version

    /// Install .NET Core SDK if required
    /// ## Parameters
    ///
    /// - 'setParams' - set installation options
    let install setParams : Options -> Options =
        let param = CliInstallOptions.Default |> setParams

        let dir = defaultArg param.CustomInstallDir defaultUserInstallDir
        let checkVersion, fromGlobalJson =
            match param.Version with
            | Version version -> Some version, false
            | CliVersion.Lkg -> None, false
            | CliVersion.Latest -> None, false
            | CliVersion.GlobalJson -> Some (getSDKVersionFromGlobalJson()), true

        let dotnetInstallations = findPossibleDotnetCliPaths (Some dir)

        let existingDotNet =
            match checkVersion with
            | Some version ->
                let passVersion = if fromGlobalJson then None else Some version
                withGlobalJson "." passVersion (fun () ->
                    dotnetInstallations
                    |> Seq.tryFind (fun dotnet ->
                        try
                            let result = getVersion (fun opt -> opt.WithCommon (fun c -> { c with DotNetCliPath = dotnet; Version = None}))
                            result = version
                        with e ->
                            Trace.traceFAKE "Retrieving version failed, assuming because it doesn't match global.json, error was: %s" e.Message
                            false
                    )
                ), passVersion
            | None ->
                // Just take first if we found a result
                dotnetInstallations |> Seq.tryHead, None

        match existingDotNet with
        | Some dotnet, passVersion ->
            Trace.traceVerbose "Suitable dotnet installation found, skipping .NET SDK installer."
            (fun opt -> { opt with DotNetCliPath = dotnet; Version = passVersion})
        | _ ->

        let passVersion = if fromGlobalJson then None else checkVersion
        let installScript = downloadInstaller param.InstallerOptions

        let exitCode =
            let args, fileName =
                if Environment.isUnix then
                    let args = sprintf "%s %s" installScript (buildDotNetCliInstallArgs param)
                    args, "bash" // Otherwise we need to set the executable flag!
                else
                    let args =
                        sprintf
                            "-ExecutionPolicy Bypass -NoProfile -NoLogo -NonInteractive -Command \"%s %s; if (-not $?) { exit -1 };\""
                            installScript
                            (buildDotNetCliInstallArgs param)
                    args, "powershell"
            Process.execSimple (fun info ->
            { info with
                FileName = fileName
                WorkingDirectory = Path.GetTempPath()
                Arguments = args }
            ) TimeSpan.MaxValue

        if exitCode <> 0 then
            // force download new installer script
            Trace.traceError ".NET Core SDK install failed, trying to redownload installer..."
            downloadInstaller (param.InstallerOptions >> (fun o ->
                { o with
                    AlwaysDownload = true
                })) |> ignore
            failwithf ".NET Core SDK install failed with code %i" exitCode

        let exe = dir @@ (if Environment.isUnix then "dotnet" else "dotnet.exe")
        Trace.tracefn ".NET Core SDK installed to %s" exe
        (fun opt -> { opt with DotNetCliPath = exe; Version = passVersion})

    /// dotnet restore command options
    type RestoreOptions =
        {
            /// Common tool options
            Common: Options
            /// The runtime to restore for (seems added in RC4). Maybe a bug, but works.
            Runtime: string option
            /// Nuget feeds to search updates in. Use default if empty.
            Sources: string list
            /// Directory to install packages in (--packages).
            Packages: string list
            /// Path to the nuget configuration file (nuget.config).
            ConfigFile: string option
            /// No cache flag (--no-cache)
            NoCache: bool
            /// Only warning failed sources if there are packages meeting version requirement (--ignore-failed-sources)
            IgnoreFailedSources: bool
            /// Disables restoring multiple projects in parallel (--disable-parallel)
            DisableParallel: bool
        }

        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Sources = []
            Runtime = None
            Packages = []
            ConfigFile = None
            NoCache = false
            IgnoreFailedSources = false
            DisableParallel = false
        }
        [<Obsolete("Use Options.Create instead")>]
        static member Default = Options.Create()

        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }

        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }

    /// [omit]
    let private buildRestoreArgs (param: RestoreOptions) =
        [   param.Sources |> argList2 "source"
            param.Packages |> argList2 "packages"
            param.ConfigFile |> Option.toList |> argList2 "configFile"
            param.NoCache |> argOption "no-cache"
            param.Runtime |> Option.toList |> argList2 "runtime"
            param.IgnoreFailedSources |> argOption "ignore-failed-sources"
            param.DisableParallel |> argOption "disable-parallel"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


    /// Execute dotnet restore command
    /// ## Parameters
    ///
    /// - 'setParams' - set restore command parameters
    /// - 'project' - project to restore packages
    let restore setParams project =
        use __ = Trace.traceTask "DotNet:restore" project
        let param = RestoreOptions.Create() |> setParams
        let args = sprintf "%s %s" (Process.quoteIfNeeded project) (buildRestoreArgs param)
        let result = exec (fun _ -> param.Common) "restore" args
        if not result.OK then failwithf "dotnet restore failed with code %i" result.ExitCode
        __.MarkSuccess()

    /// build configuration
    type BuildConfiguration =
        | Debug
        | Release
        | Custom of string
    with
        /// Convert the build configuration to a string that can be passed to the .NET CLI
        override this.ToString() =
            match this with
            | Debug -> "Debug"
            | Release -> "Release"
            | Custom config -> config

    [<RequireQualifiedAccess>]
    module BuildConfiguration =
        /// Parse a build configuration string
        let fromString (s: string) =
            match s.ToLowerInvariant() with
            | "debug" -> Debug
            | "release" -> Release
            | _ -> Custom s

        /// Get the build configuration from an environment variable with the given name or returns
        /// the default if not value was set
        let fromEnvironVarOrDefault (name: string) (defaultValue: BuildConfiguration) =
            match Environment.environVarOrNone name with
            | Some config -> fromString config
            | None -> defaultValue      

    /// [omit]
    let private buildConfigurationArg (param: BuildConfiguration) =
        sprintf "--configuration %O" param

    /// dotnet pack command options
    type PackOptions =
        {
            /// Common tool options
            Common: Options
            /// Pack configuration (--configuration)
            Configuration: BuildConfiguration
            /// Version suffix to use
            VersionSuffix: string option
            /// Build base path (--build-base-path)
            BuildBasePath: string option
            /// Output path (--output)
            OutputPath: string option
            /// No build flag (--no-build)
            NoBuild: bool
        }

        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Configuration = Release
            VersionSuffix = None
            BuildBasePath = None
            OutputPath = None
            NoBuild = false
        }
        [<Obsolete("Use PackOptions.Create instead")>]
        static member Default = PackOptions.Create()
        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }

    /// [omit]
    let private buildPackArgs (param: PackOptions) =
        [
            buildConfigurationArg param.Configuration
            param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
            param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
            param.OutputPath |> Option.toList |> argList2 "output"
            param.NoBuild |> argOption "no-build"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


    /// Execute dotnet pack command
    /// ## Parameters
    ///
    /// - 'setParams' - set pack command parameters
    /// - 'project' - project to pack
    let pack setParams project =
        use __ = Trace.traceTask "DotNet:pack" project
        let param = PackOptions.Create() |> setParams
        let args = sprintf "%s %s" (Process.quoteIfNeeded project) (buildPackArgs param)
        let result = exec (fun _ -> param.Common) "pack" args
        if not result.OK then failwithf "dotnet pack failed with code %i" result.ExitCode
        __.MarkSuccess()

    /// dotnet publish command options
    type PublishOptions =
        {
            /// Common tool options
            Common: Options
            /// Pack configuration (--configuration)
            Configuration: BuildConfiguration
            /// Target framework to compile for (--framework)
            Framework: string option
            /// Target runtime to publish for (--runtime)
            Runtime: string option
            /// Build base path (--build-base-path)
            BuildBasePath: string option
            /// Output path (--output)
            OutputPath: string option
            /// Defines what `*` should be replaced with in version field in project.json (--version-suffix)
            VersionSuffix: string option
            /// No build flag (--no-build)
            NoBuild: bool
        }

        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Configuration = Release
            Framework = None
            Runtime = None
            BuildBasePath = None
            OutputPath = None
            VersionSuffix = None
            NoBuild = false
        }
        [<Obsolete("Use PublishOptions.Create instead")>]
        static member Default = PublishOptions.Create()
        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }

    /// [omit]
    let private buildPublishArgs (param: PublishOptions) =
        [
            buildConfigurationArg param.Configuration
            param.Framework |> Option.toList |> argList2 "framework"
            param.Runtime |> Option.toList |> argList2 "runtime"
            param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
            param.OutputPath |> Option.toList |> argList2 "output"
            param.VersionSuffix |> Option.toList |> argList2 "version-suffix"
            param.NoBuild |> argOption "no-build"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


    /// Execute dotnet publish command
    /// ## Parameters
    ///
    /// - 'setParams' - set publish command parameters
    /// - 'project' - project to publish
    let publish setParams project =
        use __ = Trace.traceTask "DotNet:publish" project
        let param = PublishOptions.Create() |> setParams
        let args = sprintf "%s %s" (Process.quoteIfNeeded project) (buildPublishArgs param)
        let result = exec (fun _ -> param.Common) "publish" args
        if not result.OK then failwithf "dotnet publish failed with code %i" result.ExitCode
        __.MarkSuccess()

    /// dotnet build command options
    type BuildOptions =
        {
            /// Common tool options
            Common: Options
            /// Pack configuration (--configuration)
            Configuration: BuildConfiguration
            /// Target framework to compile for (--framework)
            Framework: string option
            /// Target runtime to publish for (--runtime)
            Runtime: string option
            /// Build base path (--build-base-path)
            BuildBasePath: string option
            /// Output path (--output)
            OutputPath: string option
            /// Native flag (--native)
            Native: bool
        }

        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Configuration = Release
            Framework = None
            Runtime = None
            BuildBasePath = None
            OutputPath = None
            Native = false
        }
        [<Obsolete("Use BuildOptions.Create instead")>]
        static member Default = BuildOptions.Create()
        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }


    /// [omit]
    let private buildBuildArgs (param: BuildOptions) =
        [
            buildConfigurationArg param.Configuration
            param.Framework |> Option.toList |> argList2 "framework"
            param.Runtime |> Option.toList |> argList2 "runtime"
            param.BuildBasePath |> Option.toList |> argList2 "build-base-path"
            param.OutputPath |> Option.toList |> argList2 "output"
            (if param.Native then "--native" else "")
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


    /// Execute dotnet build command
    /// ## Parameters
    ///
    /// - 'setParams' - set compile command parameters
    /// - 'project' - project to compile
    let build setParams project =
        use __ = Trace.traceTask "DotNet:build" project
        let param = BuildOptions.Create() |> setParams
        let args = sprintf "%s %s" (Process.quoteIfNeeded project) (buildBuildArgs param)
        let result = exec (fun _ -> param.Common) "build" args
        if not result.OK then failwithf "dotnet build failed with code %i" result.ExitCode
        __.MarkSuccess()

    /// dotnet build command options
    type TestOptions =
        {
            /// Common tool options
            Common: Options
            /// Settings to use when running tests (--settings)
            Settings: string option
            /// Lists discovered tests (--list-tests)
            ListTests: bool
            /// Run tests that match the given expression. (--filter)
            ///  Examples:
            ///   Run tests with priority set to 1: --filter "Priority = 1"
            ///   Run a test with the specified full name: --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
            ///   Run tests that contain the specified name: --filter "FullyQualifiedName~Namespace.Class"
            ///   More info on filtering support: https://aka.ms/vstest-filtering
            Filter: string option
            /// Use custom adapters from the given path in the test run. (--test-adapter-path)
            TestAdapterPath: string option
            /// Specify a logger for test results. (--logger)
            Logger: string option
            ///Configuration to use for building the project.  Default for most projects is  "Debug". (--configuration)
            Configuration: BuildConfiguration
            /// Target framework to publish for. The target framework has to be specified in the project file. (--framework)
            Framework: string option
            ///  Directory in which to find the binaries to be run (--output)
            Output: string option
            /// Enable verbose logs for test platform. Logs are written to the provided file. (--diag)
            Diag: string option
            ///  Do not build project before testing. (--no-build)
            NoBuild: bool
            /// The directory where the test results are going to be placed. The specified directory will be created if it does not exist. (--results-directory)
            ResultsDirectory: string option
            /// Enables data collector for the test run. More info here : https://aka.ms/vstest-collect (--collect)
            Collect: string option
            ///  Does not do an implicit restore when executing the command. (--no-restore)
            NoRestore: bool
            /// Arguments to pass runsettings configurations through commandline. Arguments may be specified as name-value pair of the form [name]=[value] after "-- ". Note the space after --.
            RunSettingsArguments : string option
        }

        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Settings = None
            ListTests = false
            Filter = None
            TestAdapterPath = None
            Logger = None
            Configuration = BuildConfiguration.Debug
            Framework = None
            Output = None
            Diag = None
            NoBuild = false
            ResultsDirectory = None
            Collect = None
            NoRestore = false
            RunSettingsArguments = None
        }
        [<Obsolete("Use TestOptions.Create instead")>]
        static member Default = TestOptions.Create()
        /// Gets the current environment
        member x.Environment = x.Common.Environment
        /// Sets the current environment variables.
        member x.WithEnvironment map =
            { x with Common = { x.Common with Environment = map } }
        /// Sets a value indicating whether the output for the given process is redirected.
        member x.WithRedirectOutput shouldRedirect =
            { x with Common = x.Common.WithRedirectOutput shouldRedirect }

        /// Changes the "Common" properties according to the given function
        member inline x.WithCommon f =
            { x with Common = f x.Common }

    /// [omit]
    let private buildTestArgs (param: TestOptions) =
        [
            param.Settings |> Option.toList |> argList2 "settings"
            param.ListTests |> argOption "list-tests"
            param.Filter |> Option.toList |> argList2 "filter"
            param.TestAdapterPath |> Option.toList |> argList2 "test-adapter-path"
            param.Logger |> Option.toList |> argList2 "logger"
            buildConfigurationArg param.Configuration
            param.Framework |> Option.toList |> argList2 "framework"
            param.Output |> Option.toList |> argList2 "output"
            param.Diag |> Option.toList |> argList2 "diag"
            param.NoBuild |> argOption "no-build"
            param.ResultsDirectory |> Option.toList |> argList2 "results-directory"
            param.Collect |> Option.toList |> argList2 "collect"
            param.NoRestore |> argOption "no-restore"
        ] |> Seq.filter (not << String.IsNullOrEmpty) |> String.concat " "


    /// Execute dotnet build command
    /// ## Parameters
    ///
    /// - 'setParams' - set compile command parameters
    /// - 'project' - project to compile
    let test setParams project =
        use __ = Trace.traceTask "DotNet:test" project
        let param = TestOptions.Create() |> setParams
        let args = sprintf "%s %s" (Process.quoteIfNeeded project) (buildTestArgs param)
        let result = exec (fun _ -> param.Common) "test" args
        if not result.OK then failwithf "dotnet test failed with code %i" result.ExitCode
        __.MarkSuccess()

    /// dotnet msbuild command options
    type MSBuildOptions =
        {
            /// Common tool options
            Common: Options
            /// Targets to run (/target)
            Targets : string option
            /// Set or override project-level properties (/property)
            Properties : (string * string) list
            /// Maximum number of concurrent processes (/maxcpucount)
            MaxCpuCount : int
            /// version of the MSBuild Toolset (/toolsversion)
            ToolsVersion : string option
            /// Logging verbosity (/verbosity)
            Verbosity : Verbosity option
            /// Disable console logger (/noconsolelogger)
            NoConsoleLogger : bool
            /// Warning codes to treats as errors (/warnaserror)
            WarnAsError : string list
            /// Warning codes to treats as messages (/warnasmesssage)
            WarnAsMessage : string list
            /// Extensions to ignore when determining which project file to build. (/ignoreprojectextensions)
            IgnoreProjectExtensions : string list
            ///  Enables or Disables the reuse of MSBuild nodes. (/nodeReuse)
            NodeReuse : bool
            /// Creates a single, aggregated project file (/preprocess)
            Preprocess : string option
            /// Shows detailed information at the end of the build (/detailedsummary)
            DetailedSummary : bool
            /// Runs a target named Restore prior to building other targets (/restore)
            Restore : bool
            /// Set or override project-level properties during restore (/restoreProperty)
            RestoreProperties : (string * string) list
            /// Profiles MSBuild evaluation and writes result to file (/profileevaluation)
            ProfileEvaluation : bool
            /// Do not auto-include any MSBuild.rsp files (/noautoresponse)
            NoAutoResponse : bool
            /// Do not display the startup banner and copyright message (/nologo)
            NoLogo : bool
        }
        /// Parameter default values.
        static member Create() = {
            Common = Options.Create()
            Targets = None
            Properties = List.empty
            MaxCpuCount = 1
            ToolsVersion = None
            Verbosity = None
            NoConsoleLogger = false
            WarnAsError = List.empty
            WarnAsMessage = List.empty
            IgnoreProjectExtensions = List.empty
            NodeReuse = true
            Preprocess = None
            DetailedSummary = false
            Restore = true
            RestoreProperties = List.empty
            ProfileEvaluation = false
            NoAutoResponse = false
            NoLogo = false
        }

    let private msbuildArgList name values =
        let values =
            values
            |> Seq.map (fun x -> sprintf @"""%s""" x)
            |> String.concat ";"

        (sprintf "/%s:%s" name values)

    let private msbuildProps name values =
        let values =
            values
            |> Seq.map (fun (x, v) -> sprintf @"""%s=%s""" x v)
            |> String.concat ";"

        (sprintf "/%s:%s" name values)

    let private msbuildArg name value =
        sprintf "/%s:%s" name (value.ToString().ToLowerInvariant())

    let private buildMSBuildArgs (param : MSBuildOptions) =
        [
            param.Targets |> Option.toList |> msbuildArgList "t"
            param.Properties |> msbuildProps "p"
            param.MaxCpuCount |> msbuildArg "m"
            param.ToolsVersion |> Option.toList |> msbuildArgList "tv"
            param.Verbosity |> msbuildArg "v"
            boolToFlag param.NoConsoleLogger "/noconlog"
            param.WarnAsError |> msbuildArgList "err"
            param.WarnAsMessage |> msbuildArgList "nowarn"
            param.IgnoreProjectExtensions |> msbuildArgList "ignore"
            boolToFlag param.NodeReuse "/nr"
            param.Preprocess |> msbuildArg "pp"
            boolToFlag param.DetailedSummary "/ds"
            boolToFlag param.Restore "/r"
            param.ProfileEvaluation |> msbuildArg "profileevaluation"
            boolToFlag param.NoAutoResponse "/noautorsp"
            boolToFlag param.NoLogo "/nologo"
        ] |> Seq.filter String.isNotNullOrEmpty |> String.concat " "

    let msbuild setParams project =
        use __ = Trace.traceTask "DotNet:msbuild" project
        let param = MSBuildOptions.Create() |> setParams
        let args = sprintf "%s %s" project (buildMSBuildArgs param)
        let result = exec (fun _ -> param.Common) "msbuild" args
        if not result.OK then failwithf "dotnet msbuild failed with code %i" result.ExitCode
        __.MarkSuccess()
