/// Contains tasks to run [Fixie](https://fixie.github.io/) unit tests.
[<RequireQualifiedAccess>]
module Fake.Testing.Fixie

    open System.Text
    open Fake.Core
    open Fake.DotNet

    /// Args type to configure the Fixie runner
    type FixieArgs =
        {
          /// The configuration under which to build
          Configuration: string
          /// Skip building the test project prior to running it.
          NoBuild: bool
          /// Only run test assemblies targeting a specific framework.
          Framework: string
          /// Write test results to the specified path, using the xUnit XML format.
          Report: string
          /// Arbitrary arguments made available to custom discovery/execution classes.
          CustomArguments: (string * string) list }

    /// Fixie default arguments
    let internal FixieDefaults =
        { Configuration = ""
          NoBuild = false
          Framework = ""
          Report = ""
          CustomArguments = [] }

    [<Literal>]
    let internal Configuration = "Configuration"

    [<Literal>]
    let internal NoBuild = "NoBuild"

    [<Literal>]
    let internal Framework = "Framework"

    [<Literal>]
    let internal Report = "Report"

    [<Literal>]
    let internal CustomArguments = "CustomArguments"

    let private formatCustomArguments customArguments =
        let option = sprintf "--%s"
        let appendCustomOptions options builder =
            options
                |> Seq.fold (fun builder (key, value) ->
                    builder
                    |> StringBuilder.appendWithoutQuotes (option key)
                    |> StringBuilder.appendWithoutQuotes value
                ) builder

        new StringBuilder()
        |> appendCustomOptions customArguments
        |> StringBuilder.toText

    let private formatArgument (fixieArgs:FixieArgs)  paramName =
        match paramName with
        | Configuration when not(String.isNullOrEmpty(fixieArgs.Configuration)) -> sprintf "--configuration %s" fixieArgs.Configuration
        | NoBuild when fixieArgs.NoBuild -> "--no-build"
        | Framework when not(String.isNullOrEmpty(fixieArgs.Framework)) -> sprintf "--framework %s" fixieArgs.Framework
        | Report when not(String.isNullOrEmpty(fixieArgs.Report)) -> sprintf "--report \"%s\"" fixieArgs.Report
        | CustomArguments when fixieArgs.CustomArguments.Length > 0 -> sprintf "-- %s" (formatCustomArguments fixieArgs.CustomArguments)
        | _ -> ""

    /// [omit]
    let formatFixieArguments (fixieArgs:FixieArgs) =
        let format = formatArgument fixieArgs

        let configuration = format Configuration 
        let noBuild = format NoBuild
        let framework = format Framework
        let report = format Report
        let customArguments = format CustomArguments

        [ configuration; noBuild; framework; report; customArguments; ]
        |> List.filter (fun item -> item <> "")
        |> String.concat " "
        |> String.trim


    /// This task to can be used to run [Fixie](http://patrick.lioi.net/fixie/) on test libraries.
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the Fixie default parameters.
    ///
    /// ## Sample
    ///
    ///   Fixie (fun p -> { p with Configuration = "Release"; CustomArguments = ["custom","1"; "test","2"] })
    let Fixie setParams =
        let parameters = setParams FixieDefaults

        let arguments = formatFixieArguments parameters

        let dotnetOptions = (fun (buildOptions:DotNet.Options) -> buildOptions);
        let result = DotNet.exec dotnetOptions "fixie" arguments
            
        if 0 <> result.ExitCode
        then failwithf "Fixie test failed with exit code '%d' <> 0." result.ExitCode 
