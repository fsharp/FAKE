namespace Fake.Core

open System
open System.Diagnostics

/// <summary>
/// A record type which captures console messages
/// </summary>
type ConsoleMessage =
    { IsError: bool
      Message: string
      Timestamp: DateTimeOffset }

    static member Create isError msg =
        { IsError = isError
          Message = msg
          Timestamp = DateTimeOffset.UtcNow }

    static member CreateError msg = ConsoleMessage.Create true msg
    static member CreateOut msg = ConsoleMessage.Create false msg

/// <summary>
/// A process result including error code, message log and errors.
/// </summary>
type ProcessResult =
    { ExitCode: int
      Results: ConsoleMessage list }

    member x.OK = x.ExitCode = 0

    member internal x.ReportString =
        String.Join(
            "\n",
            x.Results
            |> Seq.map (fun m -> sprintf "%s: %s" (if m.IsError then "stderr" else "stdout") m.Message)
        )

    member x.Messages =
        x.Results
        |> List.choose (function
            | { IsError = false } as m -> Some m.Message
            | _ -> None)

    member x.Errors =
        x.Results
        |> List.choose (function
            | { IsError = true } as m -> Some m.Message
            | _ -> None)

    static member New exitCode results =
        { ExitCode = exitCode
          Results = results }

module private ProcStartInfoData =
    let defaultEnvVar = "__FAKE_CHECK_USER_ERROR"

    let createEnvironmentMap () =
        Environment.environVars () |> Map.ofSeq |> Map.add defaultEnvVar defaultEnvVar

    let checkMap (map: Map<string, string>) =
        if Environment.isWindows then
            let hs =
                System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)

            for kv in map do
                if not (hs.Add kv.Key) then
                    // Environment variables are case sensitive and this is invalid!
                    let existing =
                        hs |> Seq.find (fun s -> s.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))

                    failwithf
                        "Detected invalid environment map the key '%s' was used as '%s' as well, however in windows environment variables are case-insensitive. This error shouldn't happen if you use the process helpers like 'Process.setEnvironmentVariable' instead of setting the map manually."
                        kv.Key
                        existing

open ProcStartInfoData

/// <summary>
/// The process start info, a type used to define a process configurations, options and arguments
/// </summary>
type ProcStartInfo =
    {
        /// Gets or sets the set of command-line arguments to use when starting the application.
        Arguments: string

        /// Gets or sets a value indicating whether to start the process in a new window.
        CreateNoWindow: bool

        /// Gets or sets a value that identifies the domain to use when starting the process. If this value is null,
        /// the UserName property must be specified in UPN format.
        Domain: string

        /// Gets the environment variables that apply to this process and its child processes.
        /// NOTE: Recommendation is to not use this Field, but instead use the helper function in the Proc module
        /// (for example Process.setEnvironmentVariable)
        /// NOTE: This field is ignored when UseShellExecute is true.
        Environment: Map<string, string>

#if FX_ERROR_DIALOG
        /// Gets or sets a value indicating whether an error dialog box is displayed to the user if the
        /// process cannot be started.
        ErrorDialog: bool

        /// Gets or sets the window handle to use when an error dialog box is shown for a process that cannot be started.
        ErrorDialogParentHandle: IntPtr

#endif
        /// Gets or sets the application or document to start.
        FileName: string

        /// true if the Windows user profile should be loaded; otherwise, false. The default is false.
        LoadUserProfile: bool

        // Note: No SecureString as that one is obsolete anyway and to provide a uniform API across netstandard16.
        /// Gets or sets the user password in clear text to use when starting the process.
        Password: string

#if FX_WINDOWSTLE
        /// One of the enumeration values that indicates whether the process is started in a window that is maximized,
        /// minimized, normal (neither maximized nor minimized), or not visible. The default is Normal.
        WindowStyle: ProcessWindowStyle

#endif
        /// true if error output should be written to Process.StandardError; otherwise, false. The default is false.
        RedirectStandardError: bool

        /// true if input should be read from Process.StandardInput; otherwise, false. The default is false.
        RedirectStandardInput: bool

        /// true if output should be written to Process.StandardOutput; otherwise, false. The default is false.
        RedirectStandardOutput: bool

        /// An object that represents the preferred encoding for error output. The default is null.
        StandardErrorEncoding: System.Text.Encoding

        /// An object that represents the preferred encoding for standard output. The default is null.
        StandardOutputEncoding: System.Text.Encoding

        /// The user name to use when starting the process. If you use the UPN format, user@DNS_domain_name, the Domain
        /// property must be null.
        UserName: string

        /// true if the shell should be used when starting the process; false if the process should be created directly
        /// from the executable file. The default is true.
        UseShellExecute: bool

#if FX_VERB
        /// The action to take with the file that the process opens. The default is an empty string (""), which signifies no action.
        Verb: string

#endif
        /// When UseShellExecute is true, the fully qualified name of the directory that contains the process to be
        /// started. When the UseShellExecute property is false, the working directory for the process to be started.
        /// The default is an empty string ("").
        WorkingDirectory: string
    }

    static member Create() =
        { Arguments = null
          CreateNoWindow = false
          Domain = null
          Environment = createEnvironmentMap ()
#if FX_ERROR_DIALOG
          ErrorDialog = false
          ErrorDialogParentHandle = IntPtr.Zero
#endif
          FileName = ""
          LoadUserProfile = false
          Password = null
#if FX_WINDOWSTLE
          WindowStyle = ProcessWindowStyle.Normal
#endif
          RedirectStandardError = false
          RedirectStandardInput = false
          RedirectStandardOutput = false
          StandardErrorEncoding = null
          StandardOutputEncoding = null
          UserName = null
          UseShellExecute = true
#if FX_VERB
          Verb = ""
#endif
          WorkingDirectory = "" }

    /// Sets the current environment variables.
    member x.WithEnvironment map = { x with Environment = map }

    member x.AsStartInfo =
        let p = ProcessStartInfo(x.FileName, x.Arguments)
        p.CreateNoWindow <- x.CreateNoWindow

        if not (isNull x.Domain) then
            p.Domain <- x.Domain

        checkMap x.Environment

        match x.Environment |> Map.tryFind defaultEnvVar with
        | None ->
            failwithf
                "Your environment variables look like they are set manually, but you are missing the default variables. Use the `Process.` helpers to change the 'Environment' field to inherit default values! See https://github.com/fsharp/FAKE/issues/1776#issuecomment-365431982"
        | Some _ ->
            if not x.UseShellExecute then
                p.Environment.Clear()

                x.Environment
                |> Map.remove defaultEnvVar
                |> Map.iter (fun var key -> p.Environment[ var ] <- key)

#if FX_ERROR_DIALOG
        if p.ErrorDialog then
            p.ErrorDialog <- x.ErrorDialog
            p.ErrorDialogParentHandle <- x.ErrorDialogParentHandle
#endif
        if x.LoadUserProfile then
            p.LoadUserProfile <- x.LoadUserProfile

        if not (isNull x.Password) then
#if FX_PASSWORD_CLEAR_TEXT
            p.PasswordInClearText <- x.Password
#else
#if FX_PASSWORD
            p.Password <-
                (let sec = new System.Security.SecureString()
                 x.Password |> Seq.iter (sec.AppendChar)
                 sec.MakeReadOnly()
                 sec)
#else
            failwithf
                "Password for starting a process was set but with this compiled binary neither ProcessStartInfo.Password nor ProcessStartInfo.PasswordInClearText was available."
#endif
#endif
#if FX_WINDOWSTLE
        if ProcessWindowStyle.Normal <> x.WindowStyle then
            p.WindowStyle <- x.WindowStyle
#endif
        p.RedirectStandardError <- x.RedirectStandardError
        p.RedirectStandardInput <- x.RedirectStandardInput
        p.RedirectStandardOutput <- x.RedirectStandardOutput

        if not (isNull x.StandardErrorEncoding) then
            p.StandardErrorEncoding <- x.StandardErrorEncoding

        if not (isNull x.StandardOutputEncoding) then
            p.StandardOutputEncoding <- x.StandardOutputEncoding

        if not (isNull x.UserName) then
            p.UserName <- x.UserName

        p.UseShellExecute <- x.UseShellExecute
#if FX_VERB
        if "" <> x.Verb then
            p.Verb <- x.Verb
#endif
        p.WorkingDirectory <- x.WorkingDirectory
        p

/// <summary>
/// Parameter type for process execution.
/// </summary>
type ExecParams =
    {
        /// The path to the executable, without arguments.
        Program: string

        /// The working directory for the program. Defaults to "".
        WorkingDir: string

        /// Command-line parameters in a string.
        CommandLine: string

        /// Command-line argument pairs. The value will be quoted if it contains
        /// a string, and the result will be appended to the CommandLine property.
        /// If the key ends in a letter or number, a space will be inserted between
        /// the key and the value.
        Args: (string * string) list
    }

    /// Default parameters for process execution.
    static member Empty =
        { Program = ""
          WorkingDir = ""
          CommandLine = ""
          Args = [] }


namespace Fake.Core

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Text
open System.Collections.Generic
open Fake.Core
open Fake.IO


module internal Kernel32 =
#if !FX_NO_HANDLE
    open System.Runtime.InteropServices

    [<DllImport("Kernel32.dll", SetLastError = true)>]
    extern UInt32 QueryFullProcessImageName(IntPtr hProcess, UInt32 flags, StringBuilder text, [<Out>] UInt32& size)

    let getPathToApp (proc: Process) =
        let mutable nChars = 256u
        let buff = StringBuilder(int nChars)

        let success = QueryFullProcessImageName(proc.Handle, 0u, buff, &nChars)

        if (0u <> success) then
            buff.ToString()
        else
            let hresult = Marshal.GetHRForLastWin32Error()
            Marshal.ThrowExceptionForHR hresult
            "Error = " + string hresult + " when calling GetProcessImageFileName"
#endif
    // TODO: complete, see https://github.com/dotnet/corefx/issues/1086
    [<DllImport("Kernel32.dll", SetLastError = true)>]
    extern UInt32 GetFinalPathNameByHandleA(IntPtr hFile, StringBuilder lpszFilePath, uint32 cchFilePath, uint32 dwFlags)

type AsyncProcessResult<'a> =
    { Result: System.Threading.Tasks.Task<'a>
      Raw: System.Threading.Tasks.Task<RawProcessResult> }

/// <summary>
/// Contains functions which can be used to start other tools.
/// </summary>
[<RequireQualifiedAccess>]
module Process =

    /// <summary>
    /// Kills the given process
    /// </summary>
    ///
    /// <param name="proc">The process to kill</param>
    let kill (proc: Process) =
        Trace.tracefn "Trying to kill process '%s' (Id = %d)" proc.ProcessName proc.Id

        try
            proc.Kill()
        with ex ->
            if Trace.isVerbose (true) then
                Trace.logfn "Killing '%s' failed with: %O" proc.ProcessName ex
            else
                Trace.logfn "Killing '%s' failed with: %s" proc.ProcessName ex.Message

    type ProcessList() =
        let mutable shouldKillProcesses = true
        let lockObj = obj ()
        let startedProcesses = HashSet()

        let killProcesses () =
            let traced = ref false
            let processList = Process.GetProcesses()

            lock lockObj (fun _ ->
                for pid, startTime in startedProcesses do
                    try
                        match processList |> Seq.tryFind (fun p -> p.Id = pid) with
                        // process IDs may be reused by the operating system so we need
                        // to make sure the process is indeed the one we started
                        | Some proc when proc.StartTime = startTime && not proc.HasExited ->
                            if not traced.Value then
                                Trace.tracefn "Killing all processes that are created by FAKE and are still running."
                                traced.Value <- true

                            kill proc
                        | _ -> ()
                    with exn ->
                        if Trace.isVerbose (true) then
                            Trace.logfn "Killing '%d' failed with: %s" pid exn.Message
                        else
                            Trace.logfn "Killing '%d' failed with: %O" pid exn

                startedProcesses.Clear())

        member _.KillAll() = killProcesses ()

        member _.Add(pid, startTime) =
            lock lockObj (fun _ -> startedProcesses.Add(pid, startTime))

        member _.SetShouldKill enable = shouldKillProcesses <- enable
        member _.GetShouldKill = shouldKillProcesses

        interface IDisposable with
            member _.Dispose() =
                if shouldKillProcesses then
                    killProcesses ()

    //let startedProcesses = HashSet()
    let private startedProcessesVar = "Fake.Core.Process.startedProcesses"

    let private getStartedProcesses, _, private setStartedProcesses =
        FakeVar.defineAllowNoContext<ProcessList> startedProcessesVar

    let private processListLock = obj ()

    let private doWithProcessList f =
        if Context.isFakeContext () then
            // The get-or-create must be atomic: under the parallel target runner two workers can both
            // observe None on the first process start, each allocate a ProcessList and register their
            // own PID, and the second setStartedProcesses then overwrites the first - so the losing
            // list's PID is never tracked and killAllCreatedProcesses (Ctrl+C cleanup) leaks it.
            lock processListLock (fun () ->
                match getStartedProcesses () with
                | Some h -> Some(f h)
                | None ->
                    let h = new ProcessList()
                    setStartedProcesses h
                    Some(f h))
        else
            None

    let private addStartedProcess (id: int, startTime: DateTime) =
        doWithProcessList (fun h -> h.Add(id, startTime)) |> ignore

    let setKillCreatedProcesses enable =
        doWithProcessList (fun h -> h.SetShouldKill enable) |> ignore

    let shouldKillCreatedProcesses () =
        match doWithProcessList (fun h -> h.GetShouldKill) with
        | Some v -> v
        | None -> false

    let private redirectOutputToTraceVar = "Fake.Core.Process.redirectOutputToTrace"

    let private tryGetRedirectOutputToTrace, _, public setRedirectOutputToTrace =
        FakeVar.defineAllowNoContext redirectOutputToTraceVar

    let getRedirectOutputToTrace () =
        match tryGetRedirectOutputToTrace () with
        | Some v -> v
        | None ->
            let shouldEnable = false
            setRedirectOutputToTrace shouldEnable
            shouldEnable

    //let mutable enableProcessTracing = true
    let private enableProcessTracingVar = "Fake.Core.Process.enableProcessTracing"

    let private getEnableProcessTracing, private removeEnableProcessTracing, public setEnableProcessTracing =
        FakeVar.defineAllowNoContext enableProcessTracingVar

    let shouldEnableProcessTracing () =
        match getEnableProcessTracing () with
        | Some v -> v
        | None -> Context.isFakeContext ()

    /// <summary>
    /// If set to true the ProcessHelper will start all processes with a custom ProcessEncoding.
    /// If set to false (default) only mono processes will be changed.
    /// </summary>
    let mutable AlwaysSetProcessEncoding = false

    /// <summary>
    /// The ProcessHelper will start all processes with this encoding if AlwaysSetProcessEncoding is set to true.
    /// If AlwaysSetProcessEncoding is set to false (default) only mono processes will be changed.
    /// </summary>
    let mutable ProcessEncoding = Encoding.UTF8

    let inline private recordProcess (proc: Process) =
        let startTime =
            try
                proc.StartTime
            with
            | :? PlatformNotSupportedException
            | :? NotSupportedException
            | :? InvalidOperationException
            | :? System.ComponentModel.Win32Exception as e ->
                let hasExited =
                    try
                        proc.HasExited
                    with
                    | :? InvalidOperationException
                    | :? NotSupportedException
                    | :? System.ComponentModel.Win32Exception as e2 -> false

                if (not hasExited) && (Environment.isWindows || Trace.isVerbose false) then
                    Trace.traceFAKE "Error while retrieving StartTime of started process: %O" e

                DateTime.Now

        addStartedProcess (proc.Id, startTime)

    let inline internal rawStartProcessNoRecord (proc: Process) =
        if String.isNullOrEmpty proc.StartInfo.WorkingDirectory |> not then
            if Directory.Exists proc.StartInfo.WorkingDirectory |> not then
                sprintf
                    "Start of process '%s' failed. WorkingDir '%s' does not exist."
                    proc.StartInfo.FileName
                    proc.StartInfo.WorkingDirectory
                |> DirectoryNotFoundException
                |> raise

        try
            let result = proc.Start()

            if not result then
                failwithf "Could not start process (Start() returned false)."
        with ex ->
            raise
            <| exn (sprintf "Start of process '%s' failed." proc.StartInfo.FileName, ex)

    let internal rawStartProcess (proc: Process) =
        rawStartProcessNoRecord proc
        recordProcess proc

    let internal processStarter =
        RawProc.createProcessStarter (fun (c: RawCreateProcess) (p: Process) ->
            let si = p.StartInfo

            if Environment.isMono || AlwaysSetProcessEncoding then
                if si.RedirectStandardOutput then
                    si.StandardOutputEncoding <- ProcessEncoding

                if si.RedirectStandardError then
                    si.StandardErrorEncoding <- ProcessEncoding

            if c.TraceCommand && shouldEnableProcessTracing () then
                let commandLine =
                    sprintf "%s> \"%s\" %s" si.WorkingDirectory si.FileName si.Arguments
                //Trace.tracefn "%s %s" proc.StartInfo.FileName proc.StartInfo.Arguments
                Trace.tracefn
                    "%s (In: %b, Out: %b, Err: %b)"
                    commandLine
                    si.RedirectStandardInput
                    si.RedirectStandardOutput
                    si.RedirectStandardError

            rawStartProcessNoRecord p
            recordProcess p)

    [<RequireQualifiedAccess>]
    module internal Proc =
        open Fake.Core.ProcessHelpers

        let startRaw (processStarter: IProcessStarter) (c: CreateProcess<_>) =
            async {
                let hook = c.Hook

                let state = hook.PrepareState()
                let mutable stateNeedsDispose = true

                try
                    let! exitCode =
                        async {
                            let procRaw =
                                { Command = c.InternalCommand
                                  TraceCommand = c.TraceCommand
                                  WorkingDirectory = c.InternalWorkingDirectory
                                  Environment = c.InternalEnvironment
                                  Streams = c.Streams
                                  OutputHook =
                                    { new IRawProcessHook with
                                        member x.Prepare streams = hook.PrepareStreams(state, streams)
                                        member x.OnStart p = hook.ProcessStarted(state, p) } }

                            let! e = processStarter.Start(procRaw)
                            return e
                        }

                    let output = hook.RetrieveResult(state, exitCode) |> Async.StartImmediateAsTask

                    async {
                        let mutable needDispose = true

                        try
                            try
                                let all =
                                    System.Threading.Tasks.Task.WhenAll(
                                        [ exitCode :> System.Threading.Tasks.Task
                                          output :> System.Threading.Tasks.Task ]
                                    )

                                let! _streams =
                                    all.ContinueWith(System.Func<System.Threading.Tasks.Task, unit>(fun t -> ()))
                                    |> Async.AwaitTaskWithoutAggregate

                                needDispose <- false

                                if not (isNull state) then
                                    state.Dispose()
                            with e ->
                                Trace.traceFAKE "Error in state dispose: %O" e
                        finally
                            if needDispose && not (isNull state) then
                                state.Dispose()
                    }
                    |> Async.Start

                    stateNeedsDispose <- false
                    return { Result = output; Raw = exitCode }
                finally
                    if stateNeedsDispose && not (isNull state) then
                        state.Dispose()
            }
            // Immediate makes sure we set the ref cell before we return the task...
            |> Async.StartImmediateAsTask

        let start (processStarter: IProcessStarter) c =
            async {
                let! result = startRaw processStarter c
                return! result.Result |> Async.AwaitTaskWithoutAggregate
            }
            |> Async.StartImmediateAsTask

        let startRawSync (processStarter: IProcessStarter) c = (startRaw processStarter c).Result

        let startAndAwait (processStarter: IProcessStarter) c =
            start processStarter c |> Async.AwaitTaskWithoutAggregate

        let run (processStarter: IProcessStarter) c =
            startAndAwait processStarter c |> Async.RunSynchronously

    let defaultEnvVar = ProcStartInfoData.defaultEnvVar

    let createEnvironmentMap () =
        ProcStartInfoData.createEnvironmentMap ()

    let inline setRedirectOutput (shouldRedirect: bool) (startInfo: ^a) =
        //let inline getEnv s = ((^a) : (member Environment : unit -> Map<string, string>) (s))
        let inline setRedirect s e =
            (^a: (member WithRedirectOutput: bool -> ^a) (s, e))

        setRedirect startInfo shouldRedirect

    let inline redirectOutput (startInfo: ^a) = setRedirectOutput true startInfo
    let inline disableRedirectOutput (startInfo: ^a) = setRedirectOutput false startInfo

    let inline setEnvironment (map: Map<string, string>) (startInfo: ^a) =
        //let inline getEnv s = ((^a) : (member Environment : unit -> Map<string, string>) (s))
        let inline setEnv s e =
            (^a: (member WithEnvironment: Map<string, string> -> ^a) (s, e))

        setEnv startInfo map
    //{ startInfo with Environment = map }

    let disableShellExecute (startInfo: ProcStartInfo) =
        { startInfo with UseShellExecute = false }

    /// <summary>
    /// Sets the given environment variable for the given startInfo.
    /// Existing values will be overriden.
    /// </summary>
    ///
    /// <param name="envKey">The environment variable name</param>
    /// <param name="envVar">The environment variable value</param>
    /// <param name="startInfo">The start process info</param>
    let inline setEnvironmentVariable envKey (envVar: string) (startInfo: ^a) =
        let inline getEnv s =
            (^a: (member Environment: Map<string, string>) s)

        let inline setEnv s e =
            (^a: (member WithEnvironment: Map<string, string> -> ^a) (s, e))

        let env = getEnv startInfo

        env
        |> (if Environment.isWindows then
                match
                    env
                    |> Seq.tryFind (fun kv -> kv.Key.Equals(envKey, StringComparison.OrdinalIgnoreCase))
                with
                | Some oldKey -> Map.remove oldKey.Key
                | None -> id
            else
                Map.remove envKey)
        |> Map.add envKey envVar
        |> setEnv startInfo

    let inline getEnvironmentVariable envKey (startInfo: ^a) =
        let inline getEnv s =
            (^a: (member Environment: Map<string, string>) s)

        let env = getEnv startInfo

        if Environment.isWindows then
            env
            |> Seq.tryFind (fun kv -> kv.Key.Equals(envKey, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun kv -> kv.Value)
        else
            env |> Map.tryFind envKey

    /// <summary>
    /// Unsets the given environment variable for the started process
    /// </summary>
    ///
    /// <param name="envKey">The environment variable name</param>
    /// <param name="startInfo">The start process info</param>
    let inline removeEnvironmentVariable envKey (startInfo: ^a) =
        let inline getEnv s =
            (^a: (member Environment: Map<string, string>) s)

        let inline setEnv s e =
            (^a: (member WithEnvironment: Map<string, string> -> ^a) (s, e))

        let env = getEnv startInfo

        env
        |> (if Environment.isWindows then
                match
                    env
                    |> Seq.tryFind (fun kv -> kv.Key.Equals(envKey, StringComparison.OrdinalIgnoreCase))
                with
                | Some oldKey -> Map.remove oldKey.Key
                | None -> id
            else
                Map.remove envKey)
        //|> Map.remove envKey
        |> setEnv startInfo

    /// <summary>
    /// Sets the given environment variables.
    /// </summary>
    ///
    /// <param name="vars">The environment variables to set</param>
    /// <param name="startInfo">The start process info</param>
    let inline setEnvironmentVariables vars (startInfo: ^a) =
        vars
        |> Seq.fold (fun state (newKey, newVar) -> setEnvironmentVariable newKey newVar state) startInfo

    /// <summary>
    /// Sets all current environment variables to their current values
    /// </summary>
    ///
    /// <param name="startInfo">The start process info</param>
    let inline setCurrentEnvironmentVariables (startInfo: ^a) =
        setEnvironmentVariables (Environment.environVars ()) startInfo
        |> setEnvironmentVariable defaultEnvVar defaultEnvVar

    let internal getProcI config =
        let startInfo: ProcStartInfo =
            config { ProcStartInfo.Create() with UseShellExecute = false }

        CreateProcess.ofStartInfo startInfo.AsStartInfo

    let internal formatArgs args =
        let delimit (str: string) =
            if String.isLetterOrDigit (str.Chars(str.Length - 1)) then
                str + " "
            else
                str

        args
        |> Seq.collect (fun (k, v) -> [ delimit k; v ])
        |> CmdLineParsing.windowsArgvToCommandLine true

    /// <summary>
    /// Kills all processes with the given id
    /// </summary>
    ///
    /// <param name="id">The process id to kill</param>
    let killById id = Process.GetProcessById id |> kill

    /// <summary>
    /// Retrieve the file-path of the running executable of the given process.
    /// </summary>
    ///
    /// <param name="p">The process instance to use</param>
    let getFileName (p: Process) =
#if !FX_NO_HANDLE
        if Environment.isWindows then
            Kernel32.getPathToApp p
        else
#endif
        p.MainModule.FileName

    /// <summary>
    /// Returns all processes with the given name
    /// </summary>
    ///
    /// <param name="name">The process name</param>
    let getAllByName (name: string) =
        Process.GetProcesses()
        |> Seq.filter (fun p ->
            try
                not p.HasExited
            with _ ->
                false)
        |> Seq.filter (fun p ->
            try
                p.ProcessName.ToLower().StartsWith(name.ToLower())
            with _ ->
                false)

    /// <summary>
    /// Kills all processes with the given name
    /// </summary>
    ///
    /// <param name="name">The process name</param>
    let killAllByName name =
        Trace.tracefn "Searching for process with name = %s" name
        getAllByName name |> Seq.iter kill

    /// <summary>
    /// Kills the F# Interactive (FSI) process.
    /// </summary>
    let killFSI () = killAllByName "fsi.exe"

    /// <summary>
    /// Kills the MSBuild process.
    /// </summary>
    let killMSBuild () = killAllByName "msbuild"

    /// <summary>
    /// Kills all processes that are created by the FAKE build script unless "donotkill" flag was set.
    /// </summary>
    let killAllCreatedProcesses () =
        match getStartedProcesses () with
        | Some startedProcesses when shouldKillCreatedProcesses () -> startedProcesses.KillAll()
        | _ -> ()

    /// <summary>
    /// Waits until the processes with the given name have stopped or fails after given timeout.
    /// </summary>
    ///
    /// <param name="name">The name of the processes in question.</param>
    /// <param name="timeout">The timespan to time out after.</param>
    let ensureProcessesHaveStopped name timeout =
        let endTime = DateTime.Now.Add timeout

        while DateTime.Now <= endTime && not (getAllByName name |> Seq.isEmpty) do
            Trace.tracefn "Waiting for %s to stop (Timeout: %A)" name endTime
            Thread.Sleep 100

        if not (getAllByName name |> Seq.isEmpty) then
            failwithf "The process %s has not stopped (check the logs for errors)" name

    /// <summary>
    /// Execute an external program and return the exit code.
    /// </summary>
    ///
    /// <param name="args">The execution arguments</param>
    /// [omit]
    let shellExec (args: ExecParams) =
        if String.isNullOrEmpty args.Program then
            invalidArg "args" "You must specify a program to run!"

        let errorF msg = Trace.traceError msg

        let messageF msg = Trace.log msg

        let commandLine = args.CommandLine + " " + formatArgs args.Args

        let processResult =
            CreateProcess.fromRawCommandLine args.Program commandLine
            |> CreateProcess.withWorkingDirectory args.WorkingDir
            |> CreateProcess.redirectOutputIfNotRedirected
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Proc.run processStarter

        processResult.ExitCode

    let internal monoPath, monoVersion =
        match ProcessUtils.tryFindTool "MONO" "mono" with
        | Some path ->
            let result =
                try
                    let results = List<string>()
                    let errors = List<string>()

                    let errorF msg = errors.Add msg
                    let messageF msg = results.Add msg

                    let processResult =
                        CreateProcess.fromRawCommandLine path "--version"
                        |> CreateProcess.withTimeout (TimeSpan.FromMinutes 1.)
                        |> CreateProcess.redirectOutput
                        |> CreateProcess.withOutputEventsNotNull messageF errorF
                        |> Proc.run processStarter

                    match processResult.ExitCode <> 0 with
                    | true ->
                        ProcessResult.New
                            0
                            [ { ConsoleMessage.IsError = true
                                ConsoleMessage.Message = String.toLines errors
                                ConsoleMessage.Timestamp = DateTimeOffset.Now } ]
                    | false ->
                        ProcessResult.New
                            0
                            [ { ConsoleMessage.IsError = false
                                ConsoleMessage.Message = String.toLines results
                                ConsoleMessage.Timestamp = DateTimeOffset.Now } ]

                with e ->
                    ProcessResult.New
                        1
                        [ { ConsoleMessage.IsError = true
                            ConsoleMessage.Message = e.ToString()
                            ConsoleMessage.Timestamp = DateTimeOffset.Now } ]

            let out =
                let outStr = String.Join("\n", result.Results |> Seq.map (fun m -> m.Message))
                sprintf "Success: %b (%d), Out: %s" result.OK result.ExitCode outStr

            let ver =
                match result.OK, result.Results |> Seq.tryHead with
                | true, Some firstLine -> Some(out, Environment.Internal.parseMonoDisplayName firstLine.Message)
                | _ -> Some(out, None)

            Some path, ver
        | None -> None, None

    /// <summary>
    /// Ensures the executable is run with the full framework. On non-windows platforms that
    /// means running the tool by invoking 'mono'.
    /// </summary>
    ///
    /// <param name="proc">The process start info</param>
    let withFramework (proc: ProcStartInfo) =
        match Environment.isWindows, proc.FileName.ToLowerInvariant().EndsWith(".exe"), monoPath with
        | false, true, Some monoPath ->
            { proc with
                Arguments = "--debug \"" + proc.FileName + "\" " + proc.Arguments
                FileName = monoPath }
        | false, true, _ ->
            failwithf
                "trying to start a .NET process on a non-windows platform, but mono could not be found. Try to set the MONO environment variable or add mono to the PATH."
        | _ -> proc

/// <summary>
/// Allows to exec shell operations synchronously and asynchronously.
/// </summary>
type Shell private () =
    static member private GetParams(cmd, ?args, ?dir) =
        let args = defaultArg args ""
        let dir = defaultArg dir (Directory.GetCurrentDirectory())

        { WorkingDir = dir
          Program = cmd
          CommandLine = args
          Args = [] }

    /// <summary>
    /// Runs the given process, waits for it's completion and returns the exit code.
    /// </summary>
    ///
    /// <param name="cmd">The command which should be run in elevated context.</param>
    /// <param name="args">The process arguments (optional).</param>
    /// <param name="directory">The working directory (optional).</param>
    static member Exec(cmd, ?args, ?dir) =
        Process.shellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))

    /// <summary>
    /// Runs the given process asynchronously.
    /// </summary>
    ///
    /// <param name="cmd">The command which should be run in elevated context.</param>
    /// <param name="args">The process arguments (optional).</param>
    /// <param name="directory">The working directory (optional).</param>
    static member AsyncExec(cmd, ?args, ?dir) =
        let internalArgs = Shell.GetParams(cmd, ?args = args, ?dir = dir)

        if String.isNullOrEmpty internalArgs.Program then
            invalidArg "args" "You must specify a program to run!"

        let commandLine =
            internalArgs.CommandLine + " " + Process.formatArgs internalArgs.Args

        let errorF msg = Trace.traceError msg

        let messageF msg = Trace.log msg

        let processResult =
            CreateProcess.fromRawCommandLine internalArgs.Program commandLine
            |> CreateProcess.withWorkingDirectory internalArgs.WorkingDir
            |> CreateProcess.redirectOutputIfNotRedirected
            |> CreateProcess.withOutputEventsNotNull messageF errorF
            |> Process.Proc.run Process.processStarter

        processResult.ExitCode

/// <summary>
/// An extension to process start info type
/// </summary>
[<AutoOpen>]
module ProcStartInfoExtensions =
    type ProcStartInfo with

        /// Gets or sets the set of command-line arguments to use when starting the application.
        member x.WithArguments args = { x with Arguments = args }

        /// Gets or sets a value indicating whether to start the process in a new window.
        member x.WithCreateNoWindow noWindow = { x with CreateNoWindow = noWindow }

        /// Gets or sets a value that identifies the domain to use when starting the process.
        member x.WithDomain domain = { x with Domain = domain }

        /// Remove the current Environment Variables and use the default
        member x.WithoutEnvironment() =
            { x with Environment = Map.empty } |> Process.setCurrentEnvironmentVariables

        /// Sets the given environment variable for the given startInfo.
        member x.WithEnvironmentVariable(envKey, envVar) =
            Process.setEnvironmentVariable envKey envVar x

        /// Unsets the given environment variable for the given startInfo.
        member x.WithRemovedEnvironmentVariable(envKey) =
            Process.removeEnvironmentVariable envKey x

        /// Gets or sets a value that identifies the domain to use when starting the process.
        member x.WithEnvironmentVariables vars = Process.setEnvironmentVariables vars x

        /// Sets the current environment variables.
        member x.WithCurrentEnvironmentVariables() =
            Process.setCurrentEnvironmentVariables x

#if FX_ERROR_DIALOG
        /// Gets or sets a value indicating whether an error dialog box is displayed to the user if the process cannot be started.
        member x.WithErrorDialog errorDialog = { x with ErrorDialog = errorDialog }

        /// Gets or sets the window handle to use when an error dialog box is shown for a process that cannot be started.
        member x.WithErrorDialogParentHandle handle =
            { x with ErrorDialogParentHandle = handle }

#endif
        /// Gets or sets the application or document to start.
        member x.WithFileName name = { x with FileName = name }

        /// true if the Windows user profile should be loaded; otherwise, false. The default is false.
        member x.WithLoadUserProfile userProfile =
            { x with LoadUserProfile = userProfile }

        // Note: No SecureString as that one is obsolete anyway and to provide a uniform API across netstandard16.
        /// Gets or sets the user password in clear text to use when starting the process.
        member x.WithPassword password = { x with Password = password }

#if FX_WINDOWSTLE
        /// One of the enumeration values that indicates whether the process is started in a window that is maximized,
        /// minimized, normal (neither maximized nor minimized), or not visible. The default is Normal.
        member x.WithWindowStyle style = { x with WindowStyle = style }

#endif
        /// true if error output should be written to Process.StandardError; otherwise, false. The default is false.
        member x.WithRedirectStandardError redirectStdErr =
            { x with RedirectStandardError = redirectStdErr }

        /// true if input should be read from Process.StandardInput; otherwise, false. The default is false.
        member x.WithRedirectStandardInput redirectStdInput =
            { x with RedirectStandardInput = redirectStdInput }

        /// true if output should be written to Process.StandardOutput; otherwise, false. The default is false.
        member x.WithRedirectStandardOutput redirectStdOutput =
            { x with RedirectStandardOutput = redirectStdOutput }

        /// An object that represents the preferred encoding for error output. The default is null.
        member x.WithStandardErrorEncoding encoding =
            { x with StandardErrorEncoding = encoding }

        /// An object that represents the preferred encoding for standard output. The default is null.
        member x.WithStandardOutputEncoding encoding =
            { x with StandardOutputEncoding = encoding }

        /// The user name to use when starting the process. If you use the UPN format, user@DNS_domain_name,
        /// the Domain property must be null.
        member x.WithUserName name = { x with UserName = name }

        /// true if the shell should be used when starting the process; false if the process should be
        /// created directly from the executable file. The default is true.
        member x.WithUseShellExecute shellExec = { x with UseShellExecute = shellExec }

#if FX_VERB
        /// The action to take with the file that the process opens. The default is an empty string (""),
        /// which signifies no action.
        member x.WithVerb name = { x with Verb = name }

#endif
        /// When UseShellExecute is true, the fully qualified name of the directory that contains the process
        /// to be started. When the UseShellExecute property is false, the working directory for the process to be
        /// started. The default is an empty string ("").
        member x.WithWorkingDirectory dir = { x with WorkingDirectory = dir }


/// <summary>
/// Module to start or run processes, used in combination with the <c>CreateProcess</c> API.
/// </summary>
///
/// <example>
/// <code lang="fsharp">
/// #r "paket:
///     nuget Fake.Core.Process //"
///     open Fake.Core
///     CreateProcess.fromRawCommand "./folder/mytool.exe" ["arg1"; "arg2"]
///     |&gt; Proc.run
///     |&gt; ignore
/// </code>
/// </example>
///
[<RequireQualifiedAccess>]
module Proc =

    /// <summary>
    /// Starts a process. The process has been started successfully after the returned task has been completed.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    ///
    /// <remarks>
    /// After the task has been completed you retrieve two other tasks: <br/>
    /// <list type="number">
    /// <item>
    /// One <c>Raw</c> - Task to indicate when the process exited (and return the exit-code for example)
    /// </item>
    /// <item>
    /// One <c>Result</c> - Task for the final result object.
    /// </item>
    /// </list>
    /// <br/>
    /// Note: The <c>Result</c> task might finish while the <c>Raw</c> task is still running,
    /// this enables you to work with the result object before the process has exited.
    /// For example consider a long running process where you are only interested in the first couple of output lines
    /// </remarks>
    let startRaw (c: CreateProcess<_>) =
        Process.Proc.startRaw Process.processStarter c

    /// <summary>
    /// Similar to <c>startRaw</c> but waits until the process has been started.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let startRawSync c =
        Process.Proc.startRawSync Process.processStarter c

    /// <summary>
    /// Starts the given process and waits for the <c>Result</c> task. (see <c>startRaw</c> documentation).
    /// In most common scenarios the <c>Result</c> includes the <c>Raw</c> task or the exit-code one way or another.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let start c =
        Process.Proc.start Process.processStarter c

    /// <summary>
    /// Convenience method when you immediately want to await the result of <c>start</c>, just note that
    /// when used incorrectly this might lead to race conditions
    /// (ie if you use StartAsTask and access reference cells in <c>CreateProcess</c> after that returns)
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let startAndAwait c =
        Process.Proc.startAndAwait Process.processStarter c

    /// <summary>
    /// Like <c>start</c> but waits for the result synchronously.
    /// </summary>
    ///
    /// <param name="c">The create process instance</param>
    let run c =
        Process.Proc.run Process.processStarter c
