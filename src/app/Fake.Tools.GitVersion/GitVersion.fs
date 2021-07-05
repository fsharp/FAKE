[<RequireQualifiedAccess>]
module Fake.Tools.GitVersion

open Newtonsoft.Json
open System
open System.IO
open Fake.IO.Globbing
open Fake.Core
open Fake.DotNet

type GitversionParams = {
    /// Tool type
    ToolType : ToolType
    /// Path to the GitVersion exe file.
    ToolPath : string
    /// The timeout for the GitVersion process.
    TimeOut : TimeSpan
}

let private GitVersionDefaults = {
    ToolType = ToolType.Create()
    ToolPath = Tools.findToolInSubPath "GitVersion.exe" (Environment.environVarOrDefault "ChocolateyInstall" (Directory.GetCurrentDirectory()))
    TimeOut = TimeSpan.FromMinutes 1.
}

type GitVersionProperties = {
    Major : int
    Minor : int
    Patch : int
    PreReleaseTag : string
    PreReleaseTagWithDash : string
    PreReleaseLabel : string
    PreReleaseNumber : Nullable<int>
    BuildMetaData : string
    BuildMetaDataPadded : string
    FullBuildMetaData : string
    MajorMinorPatch : string
    SemVer : string
    LegacySemVer : string
    LegacySemVerPadded : string
    AssemblySemVer : string
    FullSemVer : string
    InformationalVersion : string
    BranchName : string
    Sha : string
    NuGetVersionV2 : string
    NuGetVersion : string
    CommitsSinceVersionSource : int
    CommitsSinceVersionSourcePadded : string
    CommitDate : string
}

let internal createProcess setParams =
    let parameters = GitVersionDefaults |> setParams
    
    CreateProcess.fromCommand (RawCommand(parameters.ToolPath, Arguments.Empty))
    |> CreateProcess.withToolType (parameters.ToolType.WithDefaultToolCommandName "gitversion")
    |> CreateProcess.redirectOutput
    |> CreateProcess.withTimeout parameters.TimeOut
    |> CreateProcess.ensureExitCode
    |> fun command ->
        Trace.trace command.CommandLine
        command

/// Runs [GitVersion](https://gitversion.readthedocs.io/en/latest/) on a .NET project file.
/// ## Parameters
///
///  - `setParams` - Function used to manipulate the GitVersionDefaults value.
///
/// ## Sample
///
///      generateProperties id // Use Defaults
///      generateProperties (fun p -> { p with ToolPath = "/path/to/directory" }
let generateProperties setParams =
    let result =
        createProcess setParams
        |> Proc.run

    result.Result.Output
    |> JsonConvert.DeserializeObject<GitVersionProperties>
