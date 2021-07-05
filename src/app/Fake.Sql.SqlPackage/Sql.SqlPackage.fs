namespace Fake.Sql

/// Contains helpers around deploying databases.
[<RequireQualifiedAccess>]
module SqlPackage =

    open Fake.Core
    open Fake.IO.FileSystemOperators
    open Fake.IO.Globbing.Operators
    open System.IO
    open System

    /// The type of action to execute.
    type DeployAction =
        /// Generate and apply a synchronisation script between two databases.
        | Deploy
        /// Generate a SQL script to sync two databases.
        | Script of OutputPath:string
        /// Generate an XML report for the differences between two databases.
        | Report of OutputPath:string

    /// Configuration arguments for DacPac deploy
    type DeployDbArgs = {
        /// The path to SqlPackage.exe.
        SqlPackageToolPath : string
        /// Type of action to execute. Defaults to Deploy.
        Action : DeployAction
        /// Azure AccessToken
        AccessToken: string
        /// Path to source (path to DACPAC or Connection String).
        Source : string
        /// Path to destination (path to DACPAC or Connection String).
        Destination : string
        /// Timeout for deploy (defaults to 120 seconds).
        Timeout : int option
        /// Block deployment if data loss can occur. Defaults to true.
        BlockOnPossibleDataLoss : bool option
        /// Drops objects in the destination that do not exist in the source. Defaults to false.
        DropObjectsNotInSource : bool option
        /// Recreates the database from scratch on publish (rather than an in-place update). Defaults to false.
        RecreateDb : bool option
        /// Additional configuration parameters required by sqlpackage.exe
        AdditionalSqlPackageProperties : (string * string) list
        /// SQLCMD variables
        Variables : (string * string) list
        ///Specifies the file path to a DAC Publish Profile. The profile defines a collection of properties and variables to use when generating outputs.
        Profile : string }

    let internal validPaths =
        let getSqlVersion (path:string) = path.Split '\\' |> Array.item 3 |> int
        let getVsVersion (path: string) = (Path.GetDirectoryName path |> DirectoryInfo).Name |> int
        let sql = !!(Environment.ProgramFilesX86 </> @"Microsoft SQL Server\**\DAC\bin\SqlPackage.exe") |> Seq.map(fun path -> path, getSqlVersion path)
        let vs = !!(Environment.ProgramFilesX86 </> @"Microsoft Visual Studio*\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\*\SqlPackage.exe") |> Seq.map(fun path -> path, getVsVersion path)
        let vs2017 = !!(Environment.ProgramFilesX86 </> @"Microsoft Visual Studio\**\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\*\SqlPackage.exe") |> Seq.map(fun path -> path, getVsVersion path)

        [ sql; vs; vs2017 ]
        |> List.collect Seq.toList
        |> List.sortByDescending snd
        |> List.map fst

    /// The default DacPac deployment arguments.
    let internal DefaultDeploymentArgs = 
        { SqlPackageToolPath = 
            validPaths
            |> List.tryHead
            |> defaultArg <| ""
          Action = Deploy
          AccessToken = ""
          Source = ""
          Destination = ""
          Timeout = None
          BlockOnPossibleDataLoss = None
          DropObjectsNotInSource = None
          RecreateDb = None
          AdditionalSqlPackageProperties = []
          Variables = []
          Profile = "" }

    [<Literal>]
    let internal Action = "Action"

    [<Literal>]
    let internal AccessToken = "AccessToken"

    [<Literal>]
    let internal Source = "Source"

    [<Literal>]
    let internal Destination = "Destination"

    [<Literal>]
    let internal OutputPath = "OutputPath"

    [<Literal>]
    let internal BlockOnPossibleDataLoss = "BlockOnPossibleDataLoss"

    [<Literal>]
    let internal DropObjectsNotInSource = "DropObjectsNotInSource"

    [<Literal>]
    let internal Timeout = "Timeout"

    [<Literal>]
    let internal RecreateDb = "RecreateDb"

    [<Literal>]
    let internal AdditionalSqlPackageProperties = "AdditionalSqlPackageProperties"

    [<Literal>]
    let internal Variables = "Variables"

    [<Literal>]
    let internal Profile = "Profile"

    /// [omit]
    let formatArgument (args:DeployDbArgs) action outputPath additionalParameters variables argumentName =

        match argumentName with
        | Action -> sprintf "/Action:%s" action
        | AccessToken when not(String.isNullOrEmpty args.AccessToken) -> sprintf """/AccessToken:"%s" """ args.AccessToken
        | Source -> sprintf """/SourceFile:"%s" """ args.Source
        | Destination when not(String.isNullOrEmpty(args.Destination)) -> sprintf """/TargetConnectionString:"%s" """ args.Destination
        | OutputPath -> sprintf "%s" outputPath
        | BlockOnPossibleDataLoss when args.BlockOnPossibleDataLoss.IsSome -> sprintf "/p:BlockOnPossibleDataLoss=%b" args.BlockOnPossibleDataLoss.Value
        | BlockOnPossibleDataLoss when String.isNullOrEmpty(args.Profile) && args.BlockOnPossibleDataLoss.IsNone -> sprintf "/p:BlockOnPossibleDataLoss=%b" false
        | DropObjectsNotInSource when args.DropObjectsNotInSource.IsSome -> sprintf "/p:DropObjectsNotInSource=%b" args.DropObjectsNotInSource.Value
        | DropObjectsNotInSource when String.isNullOrEmpty(args.Profile) && args.DropObjectsNotInSource.IsNone -> sprintf "/p:DropObjectsNotInSource=%b" false
        | Timeout when args.Timeout.IsSome -> sprintf "/p:CommandTimeout=%d" args.Timeout.Value
        | Timeout when String.isNullOrEmpty(args.Profile) && args.Timeout.IsSome -> sprintf "/p:CommandTimeout=%d" args.Timeout.Value
        | RecreateDb when args.RecreateDb.IsSome -> sprintf "/p:CreateNewDatabase=%b" args.RecreateDb.Value
        | RecreateDb when String.isNullOrEmpty(args.Profile) && args.RecreateDb.IsNone -> sprintf "/p:CreateNewDatabase=%b" false
        | AdditionalSqlPackageProperties when not(String.isNullOrEmpty(additionalParameters)) -> sprintf "%s" additionalParameters
        | Variables when not(String.isNullOrEmpty(variables)) -> sprintf "%s" variables
        | Profile when not(System.String.IsNullOrEmpty(args.Profile)) -> sprintf "/pr:%s" args.Profile
        | _ -> ""

    module PropertyKeys =
        /// When creating a new SQL Azure database, specifies the database service tier to use e.g. S2, P1
        let sqlAzureDbSize = "DatabaseServiceObjective"

    let private generateCommandLine args =
        let action, outputPath =
            match args with
            | Deploy -> "Publish", None
            | Script outputPath -> "Script", Some outputPath
            | Report outputPath -> "DeployReport", Some outputPath
        let outputPath = defaultArg(outputPath |> Option.map(sprintf """/OutputPath:"%s" """)) ""
        action, outputPath

    let private formatDacPacArguments args action outputPath additionalParameters variables =

        let format = formatArgument args action outputPath additionalParameters variables

        let actionParameter = format Action
        let accessTokenParameter = format AccessToken
        let sourceParameter = format Source
        let destinationParameter = format Destination
        let outputPathParameter = format OutputPath
        let blockOnPossibleDataLossParameter = format BlockOnPossibleDataLoss
        let dropObjectsNotInSourceParameter = format DropObjectsNotInSource
        let timeoutParameter = format Timeout
        let recreateDbParameter = format RecreateDb
        let additionalSqlPackagePropertiesParameter = format AdditionalSqlPackageProperties
        let variablesParameter = format Variables
        let profileParameter = format Profile
        
        [ actionParameter; accessTokenParameter; sourceParameter; destinationParameter; outputPathParameter; blockOnPossibleDataLossParameter; dropObjectsNotInSourceParameter; timeoutParameter; recreateDbParameter; additionalSqlPackagePropertiesParameter; variablesParameter; profileParameter ]
            |> List.filter (fun item -> item <> "")
            |> String.concat " "

    /// Deploys a SQL DacPac or database to another database or DacPac.
    let deployDb setParams =
        let args = setParams DefaultDeploymentArgs
        let action, outputPath = generateCommandLine args.Action

        let concat parameter =
            List.map (fun (key, value) -> sprintf "/%s:%s=%s" parameter key value)
            >> String.concat " "

        let additionalParameters = args.AdditionalSqlPackageProperties |> concat "p"

        let variables = args.Variables |> concat "v"

        let arguments = formatDacPacArguments args action outputPath additionalParameters variables

        if System.String.IsNullOrWhiteSpace args.SqlPackageToolPath then
            failwith "No SqlPackage.exe filename was given."

        if not (File.Exists args.SqlPackageToolPath) then
            let paths =
                if validPaths |> List.contains args.SqlPackageToolPath then validPaths
                else [ args.SqlPackageToolPath ]
            failwithf "Unable to find a valid instance of SqlPackage.exe. Paths checked were: %A." paths
        
        CreateProcess.fromRawCommandLine args.SqlPackageToolPath arguments
        |> CreateProcess.withTimeout TimeSpan.MaxValue
        |> CreateProcess.addOnExited (fun data exitCode ->
            if exitCode <> 0 then
                failwithf "Process exit code '%d' <> 0." exitCode
            else
                data)
        |> Proc.run
        |> ignore

