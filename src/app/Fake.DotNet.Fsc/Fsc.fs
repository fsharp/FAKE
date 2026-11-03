namespace Fake.DotNet

/// <summary>
/// Contains tasks to compiles F# source file with the
/// <a href="https://github.com/fsharp/FSharp.Compiler.Service">FSharp.Compiler.Service</a>.
/// </summary>
[<RequireQualifiedAccess>]
module Fsc =

    open System
    open FSharp.Compiler.CodeAnalysis
    open FSharp.Compiler.Diagnostics
    open Fake.IO
    open Fake.Core

    /// <summary>
    /// An exception type to signal build errors.
    /// </summary>
    exception BuildException of string * list<string> with
        override x.ToString() =
            x.Data0.ToString()
            + Environment.NewLine
            + (String.separated Environment.NewLine x.Data1)

    /// The target compilation type
    type TargetType =
        /// Build a console executable
        | Exe
        ///  Build a Windows executable
        | Winexe
        /// Build a library
        | Library
        /// Build a module that can be added to another assembly (.netmodule)
        | Module

        override self.ToString() =
            match self with
            | Exe -> "exe"
            | Winexe -> "winexe"
            | Library -> "library"
            | Module -> "module"


    /// Limit which platforms the compiled code can run on:
    ///     x86, Itanium, x64, anycpu32bitpreferred, or anycpu.
    /// The default is anycpu.
    type PlatformType =
        | X86
        | Itanium
        | X64
        | AnyCpu32BitPreferred
        | AnyCpu

        override self.ToString() =
            match self with
            | X86 -> "x86"
            | Itanium -> "Itanium"
            | X64 -> "x64"
            | AnyCpu32BitPreferred -> "anycpu32bitpreferred"
            | AnyCpu -> "anycpu"

    /// Specify debugging type: <c>full</c>, <c>pdbonly</c>.
    /// (<c>full<c> is the default and enables attaching a debugger to a running program).
    type DebugType =
        | Full
        | PdbOnly

        override self.ToString() =
            (function
            | Full -> "full"
            | PdbOnly -> "pdbonly")
                self

    /// Specify target framework profile of this assembly.
    /// Valid values are mscorlib or netcore. Default - <c>mscorlib</c>
    type Profile =
        | MsCorlib
        | Netcore

        override self.ToString() =
            (function
            | MsCorlib -> "mscorlib"
            | Netcore -> "netcore")
                self

    /// Used to set the Accessibility of an embedded or linked resource
    type Access =
        | Public
        | Private

        override self.ToString() =
            (function
            | Public -> "public"
            | Private -> "private")
                self

    /// Optimization options that can be disabled or enabled selectively by listing them
    /// with the optimize compiler flag
    type Optimization =
        | NoJitOptimize
        | NoJitTracking
        | NoLocalOptimize
        | NoCrossoptimize
        | NoTailcalls

        override self.ToString() =
            match self with
            | NoJitOptimize -> "nojitoptimize"
            | NoJitTracking -> "nojittracking"
            | NoLocalOptimize -> "nolocaloptimize"
            | NoCrossoptimize -> "nocrossoptimize"
            | NoTailcalls -> "notailcalls"


    /// Specified path of a managed resource with an optional name alias and accessibility flag
    /// resource info format is <c>&lt;file&gt;[,&lt;stringname&gt;[,public|private]]
    /// e.g. <c>resource.dat,rezName,public</c>
    type ResourceInfo = string * string option * Access option

    let resourceStr ((file, name, access): ResourceInfo) =
        match file, name, access with
        | f, None, None -> f
        | f, Some n, None -> sprintf "%s,%s" f n
        | f, None, Some a -> sprintf "%s,%s" f (string a)
        | f, Some n, Some a -> sprintf "%s,%s,%s" f n (string a)

    /// <summary>
    /// The F# compiler parameters
    /// </summary>
    type FscParam =
        (* - OUTPUT FILES - *)
        /// Name of the output file
        | Out of file: string

        /// The <c>fsc.exe</c> output target types : exe, winexe, library, module
        | Target of TargetType

        /// Delay-sign the assembly using only the public portion of the strong name key
        | DelaySign of on: bool

        /// Write the xmldoc of the assembly to the given file
        | Doc of file: string

        /// Specify a strong name key file
        | KeyFile of file: string

        /// Specify a strong name key container
        | KeyContainer of name: string

        /// Limit which platforms the compiled code can run on:
        | Platform of platform: PlatformType

        /// Only include optimization information essential for implementing inlined constructs.
        /// Inhibits cross-module inlining but improves binary compatibility.
        | NoOptimizationData

        /// Don't add a resource to the generated assembly containing F#-specific metadata
        | NoInterfacedata

        /// Print the inferred interface of the assembly to a file
        | Sig of file: string

        (* - INPUT FILES - *)
        /// Reference an assembly
        | Reference of dllPath: string

        /// Reference assemblies in the order listed
        | References of dllPaths: string list

        (* - RESOURCES - *)
        /// Specify a Win32 resource file (.res)
        | Win32res of file: string

        /// Specify a Win32 manifest file
        | Win32Manifest of file: string

        /// Do not include the default Win32 manifest
        | NoWin32Manifest

        /// Embed the specified managed resource
        | Resource of resInfo: ResourceInfo

        /// Link the specified resource to this assembly
        | LinkResource of resInfo: ResourceInfo

        (* - CODE GENERATION - *)
        /// Emit debug information
        | Debug of on: bool

        /// Specify debugging type: full, pdbonly.
        /// (<c>full</c> is the default and enables attaching a debugger to a running program).
        | DebugType of debugType: DebugType

        /// Enable optimizations
        | Optimize of on: bool * optimizations: Optimization list

        /// Enable or disable tailcalls
        | Tailcalls of on: bool

        /// Enable or disable cross-module optimizations
        | CrossOptimize of on: bool

        (* - ERRORS AND WARNINGS - *)
        /// Report all warnings as errors
        | WarnAsError of on: bool

        /// Report specific warnings as errors
        | WarnAsErrors of on: bool * warningCodes: int list

        /// Set a warning level (0-5)
        | Warn of level: int

        /// Disable specific warning messages
        | NoWarn of warningCodes: int list

        /// Enable specific warnings that may be off by default
        | WarnOn of warningCodes: int list

        /// Output warning and error messages in color
        | ConsoleColors of on: bool

        (* - LANGUAGE - *)
        /// Generate overflow checks
        | Checked of on: bool

        /// Define a conditional compilation symbols
        | Define of symbol: string

        /// Define a list of conditional compilation symbols
        | Definitions of symbols: string list

        /// Ignore ML compatibility warnings
        | MLCompatibility

        (* - MISCELLANEOUS - *)
        /// Suppress compiler copyright message
        | NoLogo

        ///  Display the commandline flags and their usage
        | Help

        (* - ADVANCED - *)
        /// Specify the codepage used to read source files
        | Codepage of n: int

        /// Displays timing information for compilation.
        | Times

        /// Output messages in UTF-8 encoding
        | Utf8Output

        /// Output messages with fully qualified paths
        | FullPaths

        ///  Specify a directory for the include path which is used to resolve source files and assemblies
        | Lib of directories: string list

        /// Base address for the library to be built
        | BaseAddress of address: string

        /// Do not reference the default CLI assemblies by default
        | NoFramework

        /// Statically link the F# library and all referenced DLLs
        /// that depend on it into the assembly being generated
        | Standalone

        /// Statically link the given assembly and all referenced DLLs that depend on this assembly.
        /// Use an assembly name e.g. mylib, not a DLL name.
        | StaticLink of assemblyName: string

        /// Name the output debug file
        | Pdb of debugFile: string

        /// Resolve assembly references using directory-based rules rather than MSBuild resolution
        | SimpleResolution

        /// Enable high-entropy ASLR
        | HighEntropyVA of on: bool

        /// Specifies the version of the OS subsystem to be used by the generated executable.
        /// Use 6.02 for Windows 8, 6.01 for Windows 7, 6.00 for Windows Vista.
        /// This option only applies to executables, not DLL  and need only be used if your application
        /// depends on specific security features available only on certain versions of the OS
        | SubsystemVersion of version: string

        /// Specify target framework profile of this assembly.
        | TargetProfile of profile: Profile

        /// Emit debug information in quotations
        | QuotationsDebug of on: bool

        override self.ToString() =
            // commandline formatting helper functions
            /// format a standalone compiler arg: "--%s"
            let arg s = sprintf "--%s" s
            /// format a compiler arg with a parameter: "--%s:%s"
            let argp s p = sprintf "--%s:%s" s p
            /// format a short form compiler arg with a parameter: "-%s:%s"
            let sargp s p = sprintf "-%s:%s" s p // for short forms
            /// helper function to convert a bool to a "+" or "-"
            let inline chk b = if b then "+" else "-"
            /// format a compiler arg that ends with "+" or "-": "--%s%s"
            let togl s b = sprintf "--%s%s" s (chk b)
            /// format a short form compiler arg that ends with "+" or "-": "-%s%s"
            let stogl s b = if b then sprintf "-%s" s else "" // for short forms

            /// format a list of compiler args with string parameters "--%s:\"%s\""
            let argls s (ls: string list) =
                sprintf "--%s:\"%s\"" s (String.concat ";" ls)

            /// format a compiler arg that ends with "+" or "-" with string parameters  "--%s%s:\"%s\""
            let inline toglls s b (ls: 'a list) =
                sprintf "--%s%s:\"%s\"" s (chk b) (String.concat ";" (List.map string ls))

            /// format a list of short form complier args using the same symbol
            let sargmap sym ls =
                ls |> List.map (sargp sym) |> String.concat ";"

            match self with
            | Out file -> argp "out" file
            | Target t -> argp "target" <| string t
            | DelaySign on -> togl "delaysign" on
            | Doc file -> argp "doc" file
            | KeyFile file -> argp "keyfile" file
            | KeyContainer name -> argp "keycontainer" name
            | Platform p -> argp "platform" <| string p
            | NoOptimizationData -> arg "nooptimizationdata"
            | NoInterfacedata -> arg "nointerfacedata"
            | Sig file -> argp "sig" file
            | Reference dllPath -> sargp "r" dllPath
            | References dllPaths ->
                dllPaths
                |> List.map (sargp "r" >> sprintf "\"%s\"")
                |> String.concat (sprintf "; %s" Environment.NewLine)
                |> (fun x -> x.Substring(1, x.Length - 2))
            | Win32res file -> argp "win32res" file
            | Win32Manifest file -> argp "win32manifest" file
            | NoWin32Manifest -> arg "nowin32manifest"
            | Resource rinfo -> argp "resource" <| resourceStr rinfo
            | LinkResource rinfo -> argp "linkresource" <| resourceStr rinfo
            | Debug on -> stogl "g" on
            | DebugType dt -> argp "debug" <| string dt
            | Optimize (on, opts) ->
                match opts with
                | [] -> stogl "O" on
                | _ -> toglls "O" on opts
            | Tailcalls on -> togl "tailcalls" on
            | CrossOptimize on -> togl "crossoptimize" on
            | WarnAsError on -> togl "warnaserror" on
            | WarnAsErrors (on, warningCodes) -> toglls "warnaserror" on warningCodes
            | Warn lvl ->
                argp "warn"
                <| string (
                    if lvl < 0 then 0
                    elif lvl > 5 then 5
                    else lvl
                )
            | NoWarn warningCodes -> argls "nowarn" (List.map string warningCodes)
            | WarnOn warningCodes -> argls "warnon" (List.map string warningCodes)
            | ConsoleColors on -> togl "consolecolors" on
            | Checked on -> togl "checked" on
            | Define symbol -> sargp "d" symbol
            | Definitions symbols -> sargmap "d" symbols
            | MLCompatibility -> arg "mlcompatibility"
            | NoLogo -> arg "nologo"
            | Help -> arg "help"
            | Codepage n -> argp "codepage" <| string n
            | Utf8Output -> arg "utf8output"
            | FullPaths -> arg "fullpaths"
            | Lib directories -> argls "lib" directories
            | BaseAddress address -> argp "baseaddress" address
            | NoFramework -> arg "noframework"
            | Standalone -> arg "standalone"
            | StaticLink file -> argp "staticlink" file
            | Pdb debugFile -> argp "pdb" debugFile
            | SimpleResolution -> arg "simpleresolution"
            | HighEntropyVA on -> togl "highentropyva" on
            | SubsystemVersion version -> argp "subsystemversion" version
            | TargetProfile profile -> argp "targetprofile" <| string profile
            | QuotationsDebug on -> togl "quotations-debug" on
            | Times -> arg "times"

        static member Defaults = [ Out ""; Target Exe; Platform AnyCpu; References []; Debug false ]

    /// <summary>
    /// Common Error Result type for tracing errors
    /// </summary>
    type FscResultMessage =
        | Warning of string
        | Error of string

    /// <summary>
    /// Type signature for a Compiler Function
    /// </summary>
    type CompilerFunc<'TReturn> = string[] -> FscResultMessage[] * 'TReturn

    /// <summary>
    /// Computes output type and appends source files to argument list
    /// </summary>
    let private makeArgsList (opts: string list) (srcFiles: string list) =
        let outputArg arg = arg = "-o" || arg.StartsWith("--out:")
        let libTarget arg = arg = "-a" || arg = "--target:library"
        let hasOutputArg = Seq.exists outputArg
        let hasLibTarget = Seq.exists libTarget

        // If output file name is specified, pass it on to fsc.
        if opts |> hasOutputArg then
            opts @ srcFiles
        // But if it's not, then figure out what it should be.
        else
            let outExt = if opts |> hasLibTarget then ".dll" else ".exe"
            "-o" :: Path.changeExtension outExt (List.head srcFiles) :: opts @ srcFiles
        |> Array.ofList

    /// <summary>
    /// Reports Fsc compile errors to the console using Fake.Core.Trace
    /// </summary>
    let private reportErrors (errors: FscResultMessage[]) =
        for e in errors do
            match e with
            | FscResultMessage.Warning errMsg -> Trace.traceImportant errMsg
            | FscResultMessage.Error errMsg -> Trace.traceError errMsg

    /// <summary>
    /// Compiles the given source files with the given options using either
    /// the internal FCS or an external fsc.exe. If no options
    /// given (i.e. the second argument is an empty list), by default tries
    /// to behave the same way as would the command-line <c>fsc.exe</c> tool.
    /// </summary>
    let private compileFiles (compiler: CompilerFunc<'TReturn>) (srcFiles: string list) (opts: string list) : 'TReturn =
        let optsArr = makeArgsList opts srcFiles

        Trace.trace <| sprintf "FSC with args:%A" optsArr
        let errors, exitCode = compiler optsArr

        reportErrors errors
        exitCode

    /// <summary>
    /// Common compiler arg prep code
    /// </summary>
    let private doCompile
        (compiler: CompilerFunc<'TReturn>)
        (fscParams: FscParam list)
        (inputFiles: string list)
        : 'TReturn =
        let inputFiles = inputFiles |> Seq.toList
        let taskDesc = inputFiles |> String.separated ", "

        let fscParams =
            if List.isEmpty fscParams then
                FscParam.Defaults
            else
                fscParams

        let argList = fscParams |> List.map string

        use __ = Trace.traceTask "Fsc " taskDesc
        let res = compileFiles compiler inputFiles argList
        __.MarkSuccess()
        res


    (*
    Compile using the internals of FCS
    *)
    /// <summary>
    /// The internal FCS Compiler
    /// </summary>
    let private scsCompile optsArr =
        let scs = FSharpChecker.Create()
        // Always prepend "fsc.exe" since fsc compiler skips the first argument
        let optsArr = Array.append [| "fsc.exe" |] optsArr
        let errors, exn = scs.Compile optsArr |> Async.RunSynchronously

        /// Better compile reporting thanks to:
        /// https://github.com/jbtule/ComposableExtensions/blob/5b961b30668bb7f4d17238770869b5a884bc591f/tools/CompilerHelper.fsx#L233
        let errors =
            errors
            |> Array.map (fun (e: FSharpDiagnostic) ->
                match e.Severity with
                | FSharpDiagnosticSeverity.Error -> FscResultMessage.Error e.Message
                | FSharpDiagnosticSeverity.Warning -> FscResultMessage.Warning e.Message
                | FSharpDiagnosticSeverity.Hidden -> FscResultMessage.Warning e.Message
                | FSharpDiagnosticSeverity.Info -> FscResultMessage.Warning e.Message)

        errors, exn

    /// <summary>
    /// Compiles the given F# source files with the specified parameters.
    /// </summary>
    ///
    /// <param name="setParams">Function used to overwrite the default Fsc parameters.</param>
    /// <param name="inputFiles">The F# input files.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// ["file1.fs"; "file2.fs"]
    ///     |> compileWithResult [Out ""
    ///                 Target Exe
    ///                 Platform AnyCpu
    ///                 References []
    ///                 Debug false
    ///             ]
    /// </code>
    /// </example>
    let compileWithResult (fscParams: FscParam list) (inputFiles: string list) : exn option =
        doCompile scsCompile fscParams inputFiles

    /// <summary>
    /// Compiles one or more F# source files with the specified parameters.
    /// </summary>
    ///
    /// <param name="setParams">Function used to overwrite the default Fsc parameters.</param>
    /// <param name="inputFiles">The F# input files.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// ["file1.fs"; "file2.fs"]
    ///     |> compile [Out ""
    ///                 Target Exe
    ///                 Platform AnyCpu
    ///                 References []
    ///                 Debug false
    ///             ]
    /// </code>
    /// </example>
    let compile (fscParams: FscParam list) (inputFiles: string list) : unit =
        doCompile scsCompile fscParams inputFiles
        |> Option.iter (fun err -> raise <| BuildException("Fsc: compile failed with error", [ err.Message ]))


    (*
    Compile using a path to Fsc.exe
    *)
    /// <summary>
    /// An external fsc.exe compiler
    /// </summary>
    let private extFscCompile (fscTool: string) (optsArr: string[]) =
        let args = Arguments.OfArgs optsArr

        let splitLines (text: string) =
            let variants = [| "\n"; "\r\n"; "\r" |]
            text.Split(variants, StringSplitOptions.RemoveEmptyEntries)

        let r =
            Command.RawCommand(fscTool, args)
            |> CreateProcess.fromCommand
            |> CreateProcess.redirectOutput
            |> CreateProcess.withFramework // start with mono if needed.
            |> Proc.run

        let errors = r.Result.Error |> splitLines |> Array.map FscResultMessage.Warning
        errors, r.ExitCode

    /// <summary>
    /// Compiles the given F# source files with the specified parameters.
    /// </summary>
    ///
    /// <param name="fscTool">Path to an existing fsc.exe executable</param>
    /// <param name="setParams">Function used to overwrite the default Fsc parameters.</param>
    /// <param name="inputFiles">The F# input files.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// ["file1.fs"; "file2.fs"]
    ///     |> compileExternalWithResult "path/to/fsc.exe"
    ///                 [Out ""
    ///                 Target Exe
    ///                 Platform AnyCpu
    ///                 References []
    ///                 Debug false
    ///             ]
    /// </code>
    /// </example>
    let compileExternalWithResult (fscTool: string) (fscParams: FscParam list) (inputFiles: string list) : int =
        let compile = extFscCompile fscTool
        doCompile compile fscParams inputFiles

    /// <summary>
    /// Compiles one or more F# source files with the specified parameters
    /// using an existing fsc.exe installed on the system
    /// </summary>
    ///
    /// <param name="fscTool">Path to an existing fsc.exe executable</param>
    /// <param name="setParams">Function used to overwrite the default Fsc parameters.</param>
    /// <param name="inputFiles">The F# input files.</param>
    ///
    /// <example>
    /// <code lang="fsharp">
    /// ["file1.fs"; "file2.fs"]
    ///     |> compileExternal "path/to/fsc.exe"
    ///                 [Out ""
    ///                 Target Exe
    ///                 Platform AnyCpu
    ///                 References []
    ///                 Debug false
    ///             ]
    /// </code>
    /// </example>
    let compileExternal (fscTool: string) (fscParams: FscParam list) (inputFiles: string list) : unit =
        let compile = extFscCompile fscTool
        let res = doCompile compile fscParams inputFiles

        if res <> 0 then
            raise <| BuildException("Fsc: compile failed with exit code", [ string res ])
