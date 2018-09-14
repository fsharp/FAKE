﻿/// Contains tasks which allow to use MSBuild (or xBuild on Linux/Unix) to build .NET project files or solution files.

namespace Fake.DotNet

open System
open System.IO
open System.Xml.Linq
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core

/// A type to represent MSBuild project files.
type MSBuildProject = XDocument

/// An exception type to signal build errors.
exception MSBuildException of string*list<string>
  with
    override x.ToString() = x.Data0.ToString() + Environment.NewLine + (String.separated Environment.NewLine x.Data1)

[<System.Obsolete("Using this is a BUG as this exception is no longer thrown! Use MSBuildException instead!")>]
exception BuildException of string*list<string>
  with
    override x.ToString() = x.Data0.ToString() + Environment.NewLine + (String.separated Environment.NewLine x.Data1)

type MSBuildEntry =
    { Version: string
      Paths: string list }

/// MSBuild verbosity option
type MSBuildVerbosity =
    | Quiet
    | Minimal
    | Normal
    | Detailed
    | Diagnostic

/// MSBuild log option
type MSBuildLogParameter =
    | Append
    | PerformanceSummary
    | Summary
    | NoSummary
    | ErrorsOnly
    | WarningsOnly
    | NoItemAndPropertyList
    | ShowCommandLine
    | ShowTimestamp
    | ShowEventId
    | ForceNoAlign
    | DisableConsoleColor
    | DisableMPLogging
    | EnableMPLogging

/// A type for MSBuild configuration
type MSBuildFileLoggerConfig =
    { Number : int
      Filename : string option
      Verbosity : MSBuildVerbosity option
      Parameters : MSBuildLogParameter list option }

type MSBuildDistributedLoggerConfig =
    { ClassName : string option
      AssemblyPath : string
      Parameters : (string * string) list option }

module private MSBuildExe =
  let knownMSBuildEntries =
    [
        { Version = "15.0"; Paths = [@"\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
                                     @"\MSBuild\15.0\Bin"
                                     @"\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"] }
        { Version = "14.0"; Paths = [@"\MSBuild\14.0\Bin"] }
        { Version = "12.0"; Paths = [@"\MSBuild\12.0\Bin"; @"\MSBuild\12.0\Bin\amd64"] }
    ]

  let oldMSBuildLocations =
    [ @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\";
      @"c:\Windows\Microsoft.NET\Framework\v4.0.30128\";
      @"c:\Windows\Microsoft.NET\Framework\v3.5\"
    ]

  let private toDict items =
    items |> Seq.map (fun f -> f.Version, f.Paths) |> Map.ofSeq

  let private getAllKnownPaths =
    let getVSPathFromVSWhere ver =
        try
            Process.execWithResult (fun proc ->
                { proc with
                    FileName = Fake.Core.Environment.VSWhere
                    Arguments = sprintf "-version %s -property installationPath" ver })
                TimeSpan.MaxValue
            |> fun processResult ->
                if processResult.OK then
                    processResult.Messages |> List.tryHead 
                    |> Option.map(fun vsRoot -> sprintf @"%s\MSBuild\%s\Bin" vsRoot ver) 
                else Option.None
        with _ -> Option.None

    let paths =
      (knownMsBuildEntries |> List.collect (fun m -> m.Paths)) @ oldMsBuildLocations
    
    let version = Environment.environVarOrNone "VisualStudioVersion"
    Option.bind getVSPathFromVSWhere version
    |> Option.fold(fun paths path -> path :: paths) paths


  /// Versions of Mono prior to this one have faulty implementations of MSBuild
  /// NOTE: in System.Version 5.0 >= 5.0.0.0 is false while 5.0.0.0 >= 5.0 is true...
  let monoVersionToUseMSBuildOn = System.Version("5.0")

  /// Tries to detect the right version of MSBuild.
  ///
  ///   - On all OS's, we check a `MSBuild` environment variable which is either
  ///     * a direct path to a file to use, or
  ///     * a directory that contains a file called
  ///         * `msbuild` on non-Windows systems with mono >= 5.0.0.0, or
  ///         * `xbuild` on non-Windows systems with mono < 5.0.0.0,
  ///         * `MSBuild.exe` on Windows systems, or
  ///     * a tool that exists on the current PATH
  ///   - In addition, on non-Windows systems we check the current PATH for the following binaries, in this order:
  ///     * Mono >= 5.0.0.0: `msbuild`, `xbuild`
  ///     * Mono < 5.0.0.0: `xbuild`, `msbuild`
  ///     * This is due to several known issues in the Mono < 5.0 implementation of MSBuild.
  ///   - In addition, on Windows systems we
  ///     * try to read the MSBuild tool location from the AppSettings file using a parameter named `MSBuild`, and finally
  ///     * if a `VisualStudioVersion` environment variable is specified, we try to use the specific MSBuild version, matching that Visual Studio version.
  let msBuildExe =
    /// the value we're given can be a:
    ///     * full path to a file or
    ///     * just a directory
    /// if just a directory we can make it the path to a file by Path-Combining the tool name to the directory.
    let exactPathOrBinaryOnPath tool input =
        if Path.isDirectory input && Directory.Exists input
        then input </> tool
        else input

    let which tool = Process.tryFindFileOnPath tool
    let msbuildEnvironVar = Environment.environVarOrNone "MSBuild"

    let preferMSBuildOnNetCore =
        if not Environment.isUnix || Environment.isMono then false
        else
            match Mono.monoVersion with
            | Some(_, Some(version)) when version >= monoVersionToUseMSBuildOn -> true
            | _ -> false

    let preferMSBuildOnMono =
        match Environment.monoVersion with
        | Some(_, Some(version)) when version >= monoVersionToUseMSBuildOn -> true
        | _ -> false

    let foundExe =
        match Environment.isUnix, preferMSBuildOnNetCore || preferMSBuildOnMono with
        | true, true ->
            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "msbuild")
                msbuildEnvironVar |> Option.bind which
                which "msbuild"
                which "xbuild"
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "msbuild"
        | true, _ ->
            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "xbuild")
                msbuildEnvironVar |> Option.bind which
                which "xbuild"
                which "msbuild"
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "xbuild"
        | false, _ ->

            let configIgnoreMSBuild =
#if !FX_NO_SYSTEM_CONFIGURATION
                if "true".Equals(System.Configuration.ConfigurationManager.AppSettings.["IgnoreMSBuild"], StringComparison.OrdinalIgnoreCase)
                then Some ""
                else
#endif
                    None
            let findOnVSPathsThenSystemPath =
                let dict = toDict knownMSBuildEntries
                let vsVersionPaths =
                    defaultArg (Environment.environVarOrNone "VisualStudioVersion" |> Option.bind dict.TryFind) getAllKnownPaths
                    |> List.map ((@@) Environment.ProgramFilesX86)

                Process.tryFindFile vsVersionPaths "MSBuild.exe"

            let sources = [
                msbuildEnvironVar |> Option.map (exactPathOrBinaryOnPath "MSBuild.exe")
                msbuildEnvironVar |> Option.bind which
                configIgnoreMSBuild
                findOnVSPathsThenSystemPath
            ]
            defaultArg (sources |> List.choose id |> List.tryHead) "MSBuild.exe"

    if foundExe.Contains @"\BuildTools\" then
        Trace.traceFAKE "If you encounter msbuild errors make sure you have copied the required SDKs, see https://github.com/Microsoft/msbuild/issues/1697"
    elif foundExe.Contains @"\2017\" then
        Trace.logVerbosefn "Using msbuild of VS2017 (%s), if you encounter build errors make sure you have installed the necessary workflows!" foundExe
    foundExe

/// A type for MSBuild task parameters
type MSBuildParams =
    {
      /// Set the MSBuild executable to use. Defaults to the latest installed MSBuild.
      ToolPath : string
      Targets : string list
      Properties : (string * string) list
      /// corresponds to the msbuild option '/m':
      ///  - 'None' will omit the option.
      ///  - 'Some None' will emit '/m'.
      ///  - 'Some 2' will emit '/m:2'.
      MaxCpuCount : int option option
      NoLogo : bool
      NodeReuse : bool
      RestorePackagesFlag : bool
      ToolsVersion : string option
      Verbosity : MSBuildVerbosity option
      NoConsoleLogger : bool
      WarnAsError: string list option
      NoWarn: string list option
      /// corresponds to the msbuild option '/fl'
      FileLoggers : MSBuildFileLoggerConfig list option
      /// corresponds to the msbuild option '/bl'
      BinaryLoggers : string list option
      /// corresponds to the msbuild option '/dl'
      DistributedLoggers : (MSBuildDistributedLoggerConfig * MSBuildDistributedLoggerConfig option) list option
      Environment : Map<string, string> }
    /// Defines a default for MSBuild task parameters
    static member Create() =
        { ToolPath = MSBuildExe.msBuildExe
          Targets = []
          Properties = []
          MaxCpuCount = Some None
          NoLogo = false
          NodeReuse = false
          ToolsVersion = None
          Verbosity = None
          NoConsoleLogger = false
          WarnAsError = None
          NoWarn = None
          RestorePackagesFlag = false
          FileLoggers = None
          BinaryLoggers = None
          DistributedLoggers = None
          Environment =
            Process.createEnvironmentMap()
            |> Map.remove "MSBUILD_EXE_PATH"
            |> Map.remove "MSBuildExtensionsPath"
            |> Map.remove "MSBuildLoadMicrosoftTargetsReadOnly"
            |> Map.remove "MSBuildSDKsPath" }
    [<Obsolete("Please use 'Create()' instead and make sure to properly set Environment via Process-module funtions!")>]
    static member Empty = MSBuildParams.Create()

    /// Sets the current environment variables.
    member x.WithEnvironment map =
        { x with Environment = map }

[<RequireQualifiedAccess>]
module MSBuild =

  let msBuildExe = MSBuildExe.msBuildExe

  /// [omit]
  let msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"

  /// [omit]
  let xname name = XName.Get(name, msbuildNamespace)

  /// [omit]
  let loadProject (projectFileName : string) : MSBuildProject =
    MSBuildProject.Load(projectFileName, LoadOptions.PreserveWhitespace)

  // See: http://msdn.microsoft.com/en-us/library/ms228186.aspx
  let internal unescapeMSBuildSpecialChars s =
    let replExpr = new Text.RegularExpressions.Regex("%..")
    replExpr.Replace(s, new Text.RegularExpressions.MatchEvaluator(
                            fun _match -> match _match.Value with
                                          | "%24" -> "$" | "%25" -> "%" | "%27" -> "'" | "%40" -> "@"
                                          | "%3B" -> ";" | "%3F" -> "?" | "%2A" -> "*"
                                          | _ -> _match.Value))

  /// [omit]
  let internal getReferenceElements elementName projectFileName (doc : XDocument) =
    let fi = FileInfo.ofPath projectFileName
    doc.Descendants(xname "Project").Descendants(xname "ItemGroup").Descendants(xname elementName)
    |> Seq.map (fun e ->
        let a = e.Attribute(XName.Get "Include")
        let value = a.Value |> unescapeMSBuildSpecialChars |> Path.convertWindowsToCurrentPath

        let fileName =
            if value.StartsWith(".." + Path.directorySeparator) || (not <| value.Contains Path.directorySeparator) then
                fi.Directory.FullName @@ value
            else value
        a, fileName |> Path.getFullName)

  /// [omit]
  let processReferences elementName f projectFileName (doc : XDocument) =
    doc
        |> getReferenceElements elementName projectFileName
        |> Seq.iter (fun (a, fileName) -> a.Value <- f fileName)
    doc

  /// [omit]
  let rec getProjectReferences (projectFileName : string) =
    if projectFileName.EndsWith ".sln" then Set.empty
    else // exclude .sln-files since the are not XML

    let doc = loadProject projectFileName
    let references = getReferenceElements "ProjectReference" projectFileName doc |> Seq.map snd |> Seq.filter File.Exists
    references
      |> Seq.collect getProjectReferences
      |> Seq.append references
      |> Set.ofSeq

  /// [omit]
  let internal getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger warnAsError nowarn fileLoggers binaryLoggers distributedFileLoggers properties =
    if Environment.isUnix then [ targets; tools; verbosity; noconsolelogger; warnAsError; nowarn ] @ fileLoggers @ binaryLoggers @ distributedFileLoggers @ properties
    else [ targets; maxcpu; noLogo; nodeReuse; tools; verbosity; noconsolelogger; warnAsError; nowarn ] @ fileLoggers @ binaryLoggers @ distributedFileLoggers @ properties

  let private serializeArgs args =
    args
    |> Seq.choose id
    |> Seq.map (fun (k, v) ->
               "/" + k + (if String.isNullOrEmpty v then ""
                          else ":" + v))
    |> Args.toWindowsCommandLine

  /// [omit]
  let serializeMSBuildParams (p : MSBuildParams) =
    let verbosityName v =
        match v with
        | Quiet -> "q"
        | Minimal -> "m"
        | Normal -> "n"
        | Detailed -> "d"
        | Diagnostic -> "diag"

    let targets =
        match p.Targets with
        | [] -> None
        | t -> Some("t", t |> Seq.map (String.replace "." "_") |> String.separated ";")

    let properties = ("RestorePackages",p.RestorePackagesFlag.ToString()) :: p.Properties |> List.map (fun (k, v) -> Some("p", sprintf "%s=%s" k v))

    let maxcpu =
        match p.MaxCpuCount with
        | None -> None
        | Some x ->
            Some("m",
                 match x with
                 | Some v -> v.ToString()
                 | _ -> "")

    let noLogo =
        if p.NoLogo then Some("nologo", "")
        else None

    let nodeReuse =
        if p.NodeReuse then None
        else Some("nodeReuse", "False")

    let tools =
        match p.ToolsVersion with
        | None -> None
        | Some t -> Some("tv", t)

    let verbosity =
        match p.Verbosity with
        | None -> None
        | Some v -> Some("v", verbosityName v)

    let noconsolelogger =
        if p.NoConsoleLogger then Some("noconlog", "")
        else None

    let warnAsError =
        match p.WarnAsError with
        | None -> None
        | Some w -> Some("warnaserror", w |> String.concat ";")

    let nowarn =
        match p.NoWarn with
        | None -> None
        | Some w -> Some("nowarn", w |> String.concat ";")

    let fileLoggers =
        let serializeLogger fl =
            let logParams param =
                match param with
                | Append -> "Append"
                | PerformanceSummary -> "PerformanceSummary"
                | Summary -> "Summary"
                | NoSummary -> "NoSummary"
                | ErrorsOnly -> "ErrorsOnly"
                | WarningsOnly -> "WarningsOnly"
                | NoItemAndPropertyList -> "NoItemAndPropertyList"
                | ShowCommandLine -> "ShowCommandLine"
                | ShowTimestamp -> "ShowTimestamp"
                | ShowEventId -> "ShowEventId"
                | ForceNoAlign -> "ForceNoAlign"
                | DisableConsoleColor -> "DisableConsoleColor"
                | DisableMPLogging -> "DisableMPLogging"
                | EnableMPLogging -> "EnableMPLogging"

            sprintf "%s%s%s"
                (match fl.Filename with
                | None -> ""
                | Some f -> sprintf "LogFile=%s;" f)
                (match fl.Verbosity with
                | None -> ""
                | Some v -> sprintf "Verbosity=%s;" (verbosityName v))
                (match fl.Parameters with
                | None -> ""
                | Some ps ->
                    ps
                    |> List.map (logParams >> (sprintf "%s"))
                    |> String.concat "")

        match p.FileLoggers with
        | None -> []
        | Some fls ->
            fls
            |> List.map (fun fl -> Some ("flp" + (string fl.Number), serializeLogger fl) )

    let binaryLoggers =
        match p.BinaryLoggers with
        | None -> []
        | Some bls ->
            bls
            |> List.map (fun bl -> Some ("bl", bl) )

    let distributedFileLoggers =
        let serializeDLogger (dlogger : MSBuildDistributedLoggerConfig) =
            sprintf "%s%s%s"
                (match dlogger.ClassName with | None -> "" | Some name -> sprintf "%s," name)
                (sprintf "%s" dlogger.AssemblyPath)
                (match dlogger.Parameters with
                    | None -> ""
                    | Some vars -> vars
                                    |> List.fold (fun acc (k,v) -> sprintf "%s%s=%s;" acc k v) ""
                                    |> sprintf ";%s"
                )

        let createLoggerString cl fl =
            match fl with
            | None -> serializeDLogger cl
            | Some l -> sprintf "%s*%s" (serializeDLogger cl) (serializeDLogger l)

        match p.DistributedLoggers with
        | None -> []
        | Some dfls ->
            dfls
            |> List.map(fun (cl, fl) -> Some("dl", createLoggerString cl fl))

    getAllParameters targets maxcpu noLogo nodeReuse tools verbosity noconsolelogger warnAsError nowarn fileLoggers binaryLoggers distributedFileLoggers properties
    |> serializeArgs

#if !NO_MSBUILD_AVAILABLE
  /// [omit]
  let ErrorLoggerName = typedefof<MSBuildLogger.ErrorLogger>.FullName

  let private pathToLogger = typedefof<MSBuildParams>.Assembly.Location
#endif

  /// Runs a MSBuild project
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `project` - A string with the path to the project file to build.
  ///
  /// ## Sample
  ///
  ///     open Fake.DotNet
  ///     let buildMode = Environment.environVarOrDefault "buildMode" "Release"
  ///     let setParams (defaults:MSBuildParams) =
  ///             { defaults with
  ///                 Verbosity = Some(Quiet)
  ///                 Targets = ["Build"]
  ///                 Properties =
  ///                     [
  ///                         "Optimize", "True"
  ///                         "DebugSymbols", "True"
  ///                         "Configuration", buildMode
  ///                     ]
  ///              }
  ///     MSBuild.build setParams "./MySolution.sln"
  let build setParams project =
    use __ = Trace.traceTask "MSBuild" project
    let msBuildParams =
        MSBuildParams.Create()
        |> setParams
    let argsString = msBuildParams |> serializeMSBuildParams

    let args = Process.toParam project + " " + argsString
    Trace.tracefn "Building project: %s\n  %s %s" project msBuildParams.ToolPath args
    let exitCode =
        Process.execSimple (fun info ->
        { info with
            FileName = msBuildParams.ToolPath
            Arguments = args }
        |> Process.setEnvironment msBuildParams.Environment) TimeSpan.MaxValue
    if exitCode <> 0 then
        let errors =
            System.Threading.Thread.Sleep(200) // wait for the file to write
#if !NO_MSBUILD_AVAILABLE
            if File.Exists MSBuildLogger.ErrorLoggerFile then
                File.ReadAllLines(MSBuildLogger.ErrorLoggerFile) |> List.ofArray
            else []
#else
            []
#endif

        let errorMessage = sprintf "Building %s failed with exitcode %d." project exitCode
        raise (MSBuildException(errorMessage, errors))
    __.MarkSuccess()

  /// Builds the given project files and collects the output files.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - If it is null or empty then the project settings are used.
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `properties` - A list with tuples of property name and property values.
  ///  - `projects` - A list of project or solution files.
  let runWithProperties setParams outputPath (targets : string) (properties : (string) -> (string * string) list) projects =
    let projects = projects |> Seq.toList

    let output =
        match String.liftString outputPath with
        | Some path -> Some (Path.getFullName path)
        | None -> None

    let properties =
        match output with
        | Some path ->
            (fun project ->
                let outputPath = (path |> String.trimSeparator) + Path.directorySeparator
                ("OutputPath", outputPath) :: (properties project)
            )
        | None -> properties

    let dependencies =
        projects
        |> List.map getProjectReferences
        |> Set.unionMany

    let setBuildParam project defaultParams =
        let projectParams = setParams defaultParams
        let targets = targets |> String.split ';' |> List.filter String.isNotNullOrEmpty
        { projectParams with
            Targets = projectParams.Targets @ targets
            Properties = projectParams.Properties @ properties project }

    projects
    |> List.filter (fun project -> not <| Set.contains project dependencies)
    |> List.iter (fun project -> build (setBuildParam project) project)

    // it makes no sense to output the root dir content here since it does not contain the build output
    match output with
    | Some path -> !! (path @@ "/**/*.*") |> Seq.toList
    | None -> []

  /// Builds the given project files or solution files and collects the output files.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - If it is null or empty then the project settings are used.
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `properties` - A list with tuples of property name and property values.
  ///  - `projects` - A list of project or solution files.
  let run setParams outputPath targets properties projects = runWithProperties setParams outputPath targets (fun _ -> properties) projects

  /// Builds the given project files or solution files and collects the output files.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - If it is null or empty then the project settings are used.
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `projects` - A list of project or solution files.
  let runDebug setParams outputPath targets projects = run setParams outputPath targets [ "Configuration", "Debug" ] projects

  /// Builds the given project files or solution files and collects the output files.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - If it is null or empty then the project settings are used.
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `projects` - A list of project or solution files.
  let runRelease setParams outputPath targets projects = run setParams outputPath targets [ "Configuration", "Release" ] projects

  /// Builds the given project files or solution files in release mode to the default outputs.
  /// ## Parameters
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `projects` - A list of project or solution files.
  let runWithDefaults targets projects = run id null targets [ "Configuration", "Release" ] projects

  /// Builds the given project files or solution files in release mode and collects the output files.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - If it is null or empty then the project settings are used.
  ///  - `properties` - A list with tuples of property name and property values.
  ///  - `targets` - A string with the target names which should be run by MSBuild.
  ///  - `projects` - A list of project or solution files.
  let runReleaseExt setParams outputPath properties targets projects =
    let properties = ("Configuration", "Release") :: properties
    run setParams outputPath targets properties projects

  /// Builds the given web project file in the specified configuration and copies it to the given outputPath.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - The output path.
  ///  - `configuration` - MSBuild configuration.
  ///  - `projectFile` - The project file path.
  let buildWebsiteConfig setParams outputPath configuration projectFile  =
    use __ = Trace.traceTask "BuildWebsite" projectFile
    let projectName = Path.GetFileNameWithoutExtension projectFile

    let slashes (dir : string) =
        dir.Replace("\\", "/").TrimEnd('/')
        |> Seq.filter ((=) '/')
        |> Seq.length

    let currentDir = (DirectoryInfo.ofPath ".").FullName
    let projectDir = (FileInfo.ofPath projectFile).Directory.FullName

    let diff = slashes projectDir - slashes currentDir
    let prefix = if Path.IsPathRooted outputPath
                 then ""
                 else (String.replicate diff "../")

    run setParams null "Build" [ "Configuration", configuration ] [ projectFile ] |> ignore
    run setParams null "_CopyWebApplication;_BuiltWebOutputGroupOutput"
        [ "Configuration", configuration
          "OutDir", prefix + outputPath
          "WebProjectOutputDir", prefix + outputPath + "/" + projectName ] [ projectFile ]
        |> ignore
    !! (projectDir + "/bin/*.*") |> Shell.copy(outputPath + "/" + projectName + "/bin/")
    __.MarkSuccess()

  /// Builds the given web project file with debug configuration and copies it to the given outputPath.
  /// ## Parameters
  ///  - `outputPath` - The output path.
  ///  - `projectFile` - The project file path.
  let buildWebsite outputPath projectFile = buildWebsiteConfig id outputPath "Debug" projectFile

  /// Builds the given web project files in specified configuration and copies them to the given outputPath.
  /// ## Parameters
  ///  - `setParams` - A function that overwrites the default MSBuildParams
  ///  - `outputPath` - The output path.
  ///  - `configuration` - MSBuild configuration.
  ///  - `projectFiles` - The project file paths.
  let buildWebsitesConfig setParams outputPath configuration projectFiles = Seq.iter (buildWebsiteConfig setParams outputPath configuration) projectFiles

  /// Builds the given web project files with debug configuration and copies them to the given websiteDir.
  /// ## Parameters
  ///  - `outputPath` - The output path.
  ///  - `projectFiles` - The project file paths.
  let buildWebsites outputPath projectFiles = buildWebsitesConfig outputPath "Debug" projectFiles
