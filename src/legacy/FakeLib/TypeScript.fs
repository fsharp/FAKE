/// Contains code to call the typescript compiler. There is also a [tutorial](../typescript.html) for this task available.
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
module Fake.TypeScript

open System
open System.Text
open System.IO

/// Generated ECMAScript version
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
type ECMAScript =
    | ES3
    | ES5

/// Generated JavaScript module type
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
type ModuleGeneration = 
    | CommonJs
    | AMD

/// TypeScript task parameter type
[<CLIMutable>]
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
type TypeScriptParams =
    { 
      /// Specifies which ECMAScript version the TypeScript compiler should generate. Default is ES3.
      ECMAScript : ECMAScript
      /// Specifies if the TypeScript compiler should generate comments. Default is false.
      EmitComments : bool
      /// Specifies if the TypeScript compiler should generate a single output file and its filename.
      OutputSingleFile : string option
      /// Specifies if the TypeScript compiler should generate declarations. Default is false.
      EmitDeclarations : bool
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

let private TypeScriptCompilerPrefix = "Microsoft SDKs" </> "TypeScript"

[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
let extractVersionNumber (di : DirectoryInfo) = 
    match Double.TryParse di.Name with
    | true, d -> d
    | false, _ -> 0.0

/// Default parameters for the TypeScript task
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
let TypeScriptDefaultParams = 
    { ECMAScript = ES3
      EmitComments = false
      OutputSingleFile = None
      EmitDeclarations = false
      ModuleGeneration = CommonJs
      EmitSourceMaps = false
      NoLib = false
      RemoveComments = false
      OutputPath = null
      ToolPath = 
            if isUnix then "tsc"
            else 
                let paths = 
                    [ System.Environment.GetFolderPath System.Environment.SpecialFolder.ProgramFiles; System.Environment.GetFolderPath System.Environment.SpecialFolder.ProgramFilesX86]
                    |> List.map (fun p -> p </> TypeScriptCompilerPrefix)
                    |> List.collect (fun p -> try System.IO.DirectoryInfo(p).GetDirectories() |> List.ofArray with | _ -> [])
                    |> List.sortByDescending extractVersionNumber
                    |> List.map (fun di -> di.FullName)
                findPath "TypeScriptPath" (String.Join(";", paths)) "tsc.exe"
      TimeOut = TimeSpan.FromMinutes 5. }

let private buildArguments parameters file = 
    let version = 
        match parameters.ECMAScript with
        | ES3 -> "ES3"
        | ES5 -> "ES5"
    
    let moduleGeneration = 
        match parameters.ModuleGeneration with
        | CommonJs -> "commonjs"
        | AMD -> "amd"
    
    let args = 
        new StringBuilder()
        |> appendWithoutQuotes (" --target " + version)
        |> appendIfTrueWithoutQuotes parameters.EmitComments " --c"
        |> appendIfSome parameters.OutputSingleFile (fun s -> sprintf " --out %s" s)
        |> appendQuotedIfNotNull parameters.OutputPath " --outDir "
        |> appendIfTrueWithoutQuotes parameters.EmitDeclarations " --declarations"
        |> appendWithoutQuotes (" --module " + moduleGeneration)
        |> appendIfTrueWithoutQuotes parameters.EmitSourceMaps " -sourcemap"
        |> appendIfTrueWithoutQuotes parameters.NoLib " --nolib"
        |> appendIfTrueWithoutQuotes parameters.RemoveComments " --removeComments"
        |> appendWithoutQuotes " "
        |> append file
    
    args.ToString()

/// This task to can be used to call the [TypeScript](http://www.typescriptlang.org/) compiler.
/// There is also a [tutorial](../typescript.html) for this task available.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the TypeScript compiler flags.
///  - `files` - The type script files to compile.
///
/// ## Sample
///
///         !! "src/**/*.ts"
///             |> TypeScriptCompiler (fun p -> { p with TimeOut = TimeSpan.MaxValue }) 
[<System.Obsolete("FAKE0001 Use the Fake.JavaScript.TypeScript module instead.")>]
let TypeScriptCompiler setParams files = 
    use __ = traceStartTaskUsing "TypeScript" ""
    let parameters = setParams TypeScriptDefaultParams
    
    let callResults = 
        files
        |> Seq.map (buildArguments parameters)
        |> Seq.map (fun arguments -> 
               ExecProcessAndReturnMessages (fun (info : Diagnostics.ProcessStartInfo) -> 
                   info.FileName <- parameters.ToolPath
                   info.Arguments <- arguments) parameters.TimeOut)
        |> Seq.toList
    
    let errors = Seq.collect (fun x -> x.Errors) callResults
    if errors
       |> Seq.isEmpty
       |> not
    then Seq.iter traceError errors
    Seq.collect (fun x -> x.Messages) callResults |> Seq.iter trace
