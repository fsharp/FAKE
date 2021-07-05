namespace Fake.JavaScript

open Fake.Core
open Fake.IO.FileSystemOperators
open System
open System.IO
open System.Text

/// Helpers to run the typeScript compiler.
/// 
/// ## Sample
///
///     !! "src/**/*.ts"
///         |> TypeScriptCompiler (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
[<RequireQualifiedAccess>]
module TypeScript =
    /// Generated ECMAScript version
    type ECMAScript =
        | ES3
        | ES5
        | ES6
        | ES7
        | ES2017
        | ES2018
        | ES2019
        | ES2020
        | ESNext
    
    /// Generated JavaScript module type
    type ModuleGeneration = 
        | CommonJs
        | ES6
        | ES2020
        | None
        | UMD
        | AMD
        | System
        | ESNext

    /// TypeScript task parameter type
    type TypeScriptParams =
        { 
          /// Specifies which ECMAScript version the TypeScript compiler should generate. Default is ES3.
          ECMAScript : ECMAScript
          /// Specifies if the TypeScript compiler should generate a single output file and its filename.
          OutputSingleFile : string option
          /// Specifies if the TypeScript compiler should generate declaration. Default is false.
          EmitDeclaration : bool
          /// Specifies which JavaScript module type the TypeScript compiler should generate. Default is CommonJs.
          ModuleGeneration : ModuleGeneration
          /// Specifies if the TypeScript compiler should emit source maps. Default is false.
          EmitSourceMaps : bool
          /// Specifies if the TypeScript compiler should not use libs. Default is false.
          NoLib : bool      
          /// Specifies if the TypeScript compiler should remove comments. Default is false.
          RemoveComments : bool
          /// Specifies the TypeScript compiler path.
          ToolPath : string
          /// Specifies the TypeScript compiler output path.
          OutputPath : string
          /// Specifies the timeout for the TypeScript compiler.
          TimeOut : TimeSpan }

    let internal TypeScriptCompilerPrefix = "Microsoft SDKs" </> "TypeScript"

    let extractVersionNumber (di : DirectoryInfo) = 
        match Double.TryParse di.Name with
        | true, d -> d
        | false, _ -> 0.0

    /// Default parameters for the TypeScript task
    let TypeScriptDefaultParams = 
        { ECMAScript = ES3
          OutputSingleFile = Option.None
          EmitDeclaration = false
          ModuleGeneration = CommonJs
          EmitSourceMaps = false
          NoLib = false
          RemoveComments = false
          OutputPath = null
          ToolPath = 
                if Environment.isUnix then "tsc"
                else 
                    let paths = 
                        [ System.Environment.GetFolderPath Environment.SpecialFolder.ProgramFiles; System.Environment.GetFolderPath Environment.SpecialFolder.ProgramFilesX86]
                        |> List.map (fun p -> p </> TypeScriptCompilerPrefix)
                        |> List.collect (fun p -> try DirectoryInfo(p).GetDirectories() |> List.ofArray with | _ -> [])
                        |> List.sortByDescending extractVersionNumber
                        |> List.map (fun di -> di.FullName)
                    ProcessUtils.tryFindPath paths "tsc.exe"
                    |> Option.defaultWith (fun _ -> "tsc.exe")
          TimeOut = TimeSpan.FromMinutes 5. }

    /// [omit]
    let buildArguments parameters file = 
        let version = 
            match parameters.ECMAScript with
            | ECMAScript.ES3 -> "ES3"
            | ECMAScript.ES5 -> "ES5"
            | ECMAScript.ES6 -> "ES6"
            | ECMAScript.ES7 -> "ES7"
            | ECMAScript.ES2017 -> "ES2017"
            | ECMAScript.ES2018 -> "ES2018"
            | ECMAScript.ES2019 -> "ES2019"
            | ECMAScript.ES2020 -> "ES2020"
            | ECMAScript.ESNext -> "ESNext"
        
        let moduleGeneration = 
            match parameters.ModuleGeneration with
            | ModuleGeneration.CommonJs -> "CommonJS"
            | ModuleGeneration.ES6 -> "ES6"
            | ModuleGeneration.ES2020 -> "ES2020"
            | ModuleGeneration.None -> "None"
            | ModuleGeneration.UMD -> "UMD"
            | ModuleGeneration.AMD -> "AMD"
            | ModuleGeneration.System -> "System"
            | ModuleGeneration.ESNext -> "ESNext"
        
        let args = 
            new StringBuilder()
            |> StringBuilder.appendWithoutQuotes (" --target " + version)
            |> StringBuilder.appendIfSome parameters.OutputSingleFile (fun s -> sprintf " --outFile %s" s)
            |> StringBuilder.appendQuotedIfNotNull parameters.OutputPath " --outDir "
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.EmitDeclaration " --declaration"
            |> StringBuilder.appendWithoutQuotes (" --module " + moduleGeneration)
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.EmitSourceMaps " --sourceMap"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.NoLib " --noLib"
            |> StringBuilder.appendIfTrueWithoutQuotes parameters.RemoveComments " --removeComments"
            |> StringBuilder.appendWithoutQuotes " "
            |> StringBuilder.append file
        
        args.ToString()

    /// Run `tsc --declaration src/app/index.ts`
    /// ## Parameters
    ///
    ///  - `setParams` - Function used to overwrite the TypeScript compiler flags.
    ///  - `files` - The type script files to compile.
    ///
    /// ## Sample
    ///
    ///         !! "src/**/*.ts"
    ///             |> TypeScript.compile (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
    let compile setParams files = 
        use __ = Trace.traceTask "TypeScript" ""
        let parameters = setParams TypeScriptDefaultParams

        let callResults = 
            files
            |> Seq.map (buildArguments parameters)
            |> Seq.map (fun arguments ->
                Diagnostics.ProcessStartInfo(FileName = parameters.ToolPath, Arguments = arguments)
                |> CreateProcess.ofStartInfo
                |> CreateProcess.redirectOutput
                |> CreateProcess.withTimeout parameters.TimeOut
                |> Proc.run)

        let hasErrors =
            callResults
            |> Seq.fold (fun acc result -> 
                match result.ExitCode = 0 with
                | true -> Trace.trace result.Result.Output
                | false -> Trace.traceError result.Result.Output
                if result.ExitCode = 0 then acc else acc + 1) 0
        
        if hasErrors > 0 then 
            failwith "TypeScript compiler encountered errors!"
