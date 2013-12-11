﻿[<AutoOpen>]
/// Contains functions which can be used to start other tools.
module Fake.ProcessHelper

open System
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Threading
open System.Collections.Generic

let startedProcesses = HashSet()
let start (proc:Process) =
    proc.Start() |> ignore
    startedProcesses.Add proc.Id |> ignore

/// [omit]
let mutable redirectOutputToTrace = false 

/// [omit]
let mutable enableProcessTracing = true

/// A record type which captures console messages
type ConsoleMessage = {
    IsError : bool
    Message : string
    Timestamp : DateTimeOffset }

/// A process result including error code, message log and errors.
type ProcessResult = {
    ExitCode : int
    Messages : List<string>
    Errors : List<string> }
    with
      member x.OK = x.ExitCode = 0
      static member New exitCode messages errors = { ExitCode = exitCode; Messages = messages; Errors = errors }

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
///  - `silent` - If this flag is set then the process output is redirected to the given output functions `errorF` and `messageF`.
///  - `errorF` - A function which will be called with the error log.
///  - `messageF` - A function which will be called with the message log.
let ExecProcessWithLambdas configProcessStartInfoF (timeOut:TimeSpan) silent errorF messageF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
    platformInfoAction proc.StartInfo
    if isNullOrEmpty proc.StartInfo.WorkingDirectory |> not then
        if Directory.Exists proc.StartInfo.WorkingDirectory |> not then
            failwithf "Start of process %s failed. WorkingDir %s does not exist." proc.StartInfo.FileName proc.StartInfo.WorkingDirectory

    if silent then
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true

        proc.ErrorDataReceived.Add (fun d -> if d.Data <> null then errorF d.Data)
        proc.OutputDataReceived.Add (fun d -> if d.Data <> null then messageF d.Data)

    try
        if enableProcessTracing && (not <| proc.StartInfo.FileName.EndsWith "fsi.exe" ) then 
          tracefn "%s %s" proc.StartInfo.FileName proc.StartInfo.Arguments

        start proc
    with
    | exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

    if silent then
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()     
  
    if timeOut = TimeSpan.MaxValue then
        proc.WaitForExit()
    else
        if not <| proc.WaitForExit(int timeOut.TotalMilliseconds) then
            try
                proc.Kill()
            with exn -> traceError <| sprintf "Could not kill process %s  %s after timeout." proc.StartInfo.FileName proc.StartInfo.Arguments
            failwithf "Process %s %s timed out." proc.StartInfo.FileName proc.StartInfo.Arguments
    
    proc.ExitCode

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
let ExecProcessAndReturnMessages configProcessStartInfoF timeOut =
    let errors = new List<_>()
    let messages = new List<_>()
    let exitCode = ExecProcessWithLambdas configProcessStartInfoF timeOut true (errors.Add) (messages.Add)    
    ProcessResult.New exitCode messages errors

/// Runs the given process and returns the process result.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
let ExecProcessRedirected configProcessStartInfoF timeOut = 
    let messages = ref []
    let appendMessage isError msg = 
        messages := { IsError = isError; Message = msg; Timestamp = DateTimeOffset.UtcNow } :: !messages
    let exitCode = ExecProcessWithLambdas configProcessStartInfoF timeOut true (appendMessage true) (appendMessage false)    
    exitCode = 0, (!messages |> List.rev |> Seq.ofList)
 
/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
///  - `silent` - If this flag is set then the process output is redicted to the trace.
/// [omit]
[<Obsolete("Please use the new ExecProcess.")>]
let execProcess2 configProcessStartInfoF timeOut silent = ExecProcessWithLambdas configProcessStartInfoF timeOut silent traceError trace  

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
/// [omit]
[<Obsolete("Please use the new ExecProcess.")>]
let execProcessAndReturnExitCode configProcessStartInfoF timeOut = ExecProcessWithLambdas configProcessStartInfoF timeOut true traceError trace

/// Runs the given process and returns if the exit code was 0.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
/// [omit]
[<Obsolete("Please use the new ExecProcess.")>]
let execProcess3 configProcessStartInfoF timeOut = ExecProcessWithLambdas configProcessStartInfoF timeOut true traceError trace = 0   

/// Runs the given process and returns the exit code.
/// ## Parameters
///
///  - `configProcessStartInfoF` - A function which overwrites the default ProcessStartInfo.
///  - `timeOut` - The timeout for the process.
let ExecProcess configProcessStartInfoF timeOut = ExecProcessWithLambdas configProcessStartInfoF timeOut redirectOutputToTrace traceError trace  

/// Runs the given process in an elevated context and returns the exit code.
/// ## Parameters
///
///  - `cmd` - The command which should be run in elavated context.
///  - `args` - The process arguments.
///  - `timeOut` - The timeout for the process.
let ExecProcessElevated cmd args timeOut = 
    ExecProcess 
        (fun si -> 
            si.Verb <- "runas"
            si.Arguments <- args
            si.FileName <- cmd
            si.UseShellExecute <- true) 
        timeOut

/// Sets the environment Settings for the given startInfo.
/// Existing values will be overriden.
/// [omit]
let setEnvironmentVariables (startInfo:ProcessStartInfo) environmentSettings = 
    for key,value in environmentSettings do
        if startInfo.EnvironmentVariables.ContainsKey key then
            startInfo.EnvironmentVariables.[key] <- value
        else
            startInfo.EnvironmentVariables.Add(key, value)
          
/// Runs the given process and returns true if the exit code was 0.
/// [omit]
let execProcess configProcessStartInfoF timeOut = ExecProcess configProcessStartInfoF timeOut = 0

/// Starts the given process and returns immediatly.
let fireAndForget configProcessStartInfoF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
  
    try
        start proc
    with
    | exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message

/// Runs the given process, waits for its completion and returns if it succeeded.
let directExec configProcessStartInfoF =
    use proc = new Process()
    proc.StartInfo.UseShellExecute <- false
    configProcessStartInfoF proc.StartInfo
  
    try
        start proc
    with
    | exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message
  
    proc.WaitForExit()
    
    proc.ExitCode = 0

/// Starts the given process and forgets about it.
let StartProcess configProcessStartInfoF =
   use proc = new Process()
   proc.StartInfo.UseShellExecute <- false
   configProcessStartInfoF proc.StartInfo
   start proc

/// Sends a command to a windows service.
let RunService command serviceName =
    tracefn "%s %s" command serviceName
    let proc = new Process()
    proc.StartInfo.FileName <- "sc";
    proc.StartInfo.Arguments <- sprintf "%s %s" command serviceName
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.UseShellExecute <- false
    start proc

/// Stops a windows service
let StopService serviceName = 
    stopService serviceName
    ensureServiceHasStopped serviceName (TimeSpan.FromMinutes 2.)

/// Starts a windows service
let StartService serviceName = 
    startService serviceName
    ensureServiceHasStarted serviceName (TimeSpan.FromMinutes 2.)

/// Adds quotes around the string
/// [omit]
let quote str = "\"" + str + "\""

/// Adds quotes around the string if needed
/// [omit]
let quoteIfNeeded str =
    if isNullOrEmpty str then
        ""
    elif str.Contains " " then
        quote str
    else
        str

/// Adds quotes and a blank around the string´.
/// [omit]
let toParam x = " " + quoteIfNeeded x
 
/// Use default Parameters
/// [omit]
let UseDefaults = id

/// [omit]
let stringParam(paramName,paramValue) = 
    if isNullOrEmpty paramValue then None else Some(paramName, quote paramValue)

/// [omit]
let multipleStringParams paramName =
    Seq.map (fun x -> stringParam(paramName,x)) >> Seq.toList

/// [omit]
let optionParam(paramName,paramValue) = 
    match paramValue with
    | Some x -> Some(paramName, x.ToString())
    | None -> None

/// [omit]
let boolParam(paramName,paramValue) = if paramValue then Some(paramName, null) else None

/// [omit]
let parametersToString flagPrefix delimiter parameters =
    parameters
      |> Seq.choose id
      |> Seq.map (fun (paramName,paramValue) -> 
            flagPrefix + paramName + 
                if isNullOrEmpty paramValue then "" else delimiter + paramValue)
      |> separated " "

/// Searches the given directories for all occurrences of the given file name
/// [omit]
let tryFindFile dirs file =
    let files = 
        dirs
          |> Seq.map 
               (fun (path:string) ->
                   let dir =
                     path
                       |> replace "[ProgramFiles]" ProgramFiles
                       |> replace "[ProgramFilesX86]" ProgramFilesX86
                       |> replace "[SystemRoot]" SystemRoot
                       |> directoryInfo
                   if not dir.Exists then "" else
                   let fi = dir.FullName @@ file |> fileInfo
                   if fi.Exists then fi.FullName else "")
          |> Seq.filter ((<>) "")
          |> Seq.cache

    if not (Seq.isEmpty files) then
        Some (Seq.head files)
    else
        None

/// Searches the given directories for the given file, failing if not found.
/// [omit]
let findFile dirs file =
    match tryFindFile dirs file with
    | Some found -> found
    | None -> failwithf "%s not found in %A." file dirs

/// Returns the AppSettings for the key - Splitted on ;
/// [omit]
let appSettings (key:string) (fallbackValue:string) =
    let value =
        let setting =
            try
                System.Configuration.ConfigurationManager.AppSettings.[key]
            with
            | exn -> ""
        
        if not (isNullOrWhiteSpace setting) then setting else fallbackValue

    value.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
let tryFindPath settingsName fallbackValue tool = 
    let paths = appSettings settingsName fallbackValue
    tryFindFile paths tool

/// Tries to find the tool via AppSettings. If no path has the right tool we are trying the PATH system variable.
/// [omit]
let findPath settingsName fallbackValue tool =
    match tryFindPath settingsName fallbackValue tool with
    | Some file -> file
    | None -> tool

/// Parameter type for process execution.
type ExecParams = {
    /// The path to the executable, without arguments. 
    Program          : string
    /// The working directory for the program. Defaults to "".
    WorkingDirectory : string
    /// Command-line parameters in a string.
    CommandLine      : string
    /// Command-line argument pairs. The value will be quoted if it contains
    /// a string, and the result will be appended to the CommandLine property.
    /// If the key ends in a letter or number, a space will be inserted between
    /// the key and the value.
    Args             : (string * string) list
}

/// Default parameters for process execution.
let defaultParams = {
    Program          = ""
    WorkingDirectory = ""
    CommandLine      = ""
    Args             = []
}

let private formatArgs args =
    let delimit (str:string) =
        if isLetterOrDigit (str.Chars(str.Length - 1))
        then str + " " else str

    args
    |> Seq.map (fun (k, v) -> delimit k + quoteIfNeeded v)
    |> separated " "

/// See: http://stackoverflow.com/questions/2649161/need-help-regarding-async-and-fsi/
/// [omit]
let guard f (e:IEvent<'Del, 'Args>) = 
    let e = Event.map id e
    { new IEvent<'Args> with 
        member this.AddHandler d = 
            e.AddHandler d 
            f() //must call f here!
        member this.RemoveHandler d = e.RemoveHandler d
        member this.Subscribe observer = let rm = e.Subscribe observer in f(); rm }

/// Execute an external program asynchronously and return the exit code,
/// logging output and error messages to FAKE output. You can compose the result
/// with Async.Parallel to run multiple external programs at once, but be
/// sure that none of them depend on the output of another.
let asyncShellExec (args:ExecParams) = async {
    if isNullOrEmpty args.Program then
        invalidArg "args" "You must specify a program to run!"
    let commandLine = args.CommandLine + " " + formatArgs args.Args
    let info = ProcessStartInfo( args.Program, 
                                 UseShellExecute = false,
                                 RedirectStandardError = true,
                                 RedirectStandardOutput = true,
                                 WindowStyle = ProcessWindowStyle.Hidden,
                                 WorkingDirectory = args.WorkingDirectory,
                                 Arguments = commandLine )

    use proc = new Process(StartInfo = info, EnableRaisingEvents = true)
    proc.ErrorDataReceived.Add(fun e -> if e.Data <> null then traceError e.Data)
    proc.OutputDataReceived.Add(fun e -> if e.Data <> null then trace e.Data)
    
    let! exit = 
        proc.Exited 
        |> guard (fun () -> 
                    start proc
                    proc.BeginErrorReadLine()
                    proc.BeginOutputReadLine())
        |> Async.AwaitEvent
    
    return proc.ExitCode
}

/// Kills the given process
let kill (proc:Process) =
    tracefn "Trying to kill process %s (Id = %d)" proc.ProcessName proc.Id
    try proc.Kill() with 
    | exn -> ()

/// Kills all processes with the given id
let killProcessById id =
    Process.GetProcessById id
    |> kill

/// Returns all processes with the given name
let getProcessesByName (name:string) =
      Process.GetProcesses()
      |> Seq.filter (fun p -> try not p.HasExited with | exn -> false)
      |> Seq.filter (fun p -> try p.ProcessName.ToLower().StartsWith(name.ToLower()) with | exn -> false)

/// Kills all processes with the given name
let killProcess name =
    tracefn "Searching for process with name = %s" name
    getProcessesByName name
      |> Seq.iter kill

/// Kills the F# Interactive (FSI) process.
let killFSI() = killProcess "fsi.exe"

/// Kills the MSBuild process.
let killMSBuild() = killProcess "msbuild"

/// Kills all processes that are created by the FAKE build script.
let killAllCreatedProcesses() =
    let traced = ref false
    for id in startedProcesses do
        try
            let p = Process.GetProcessById id
            if !traced |> not then
                tracefn "Killing all processes that are created by FAKE and are still running."
                traced := true
            kill p
        with
        | exn -> ()
    startedProcesses.Clear()
    
/// Waits until the processes with the given name have stopped or fails after given timeout.
/// ## Parameters
///  - `name` - The name of the processes in question.
///  - `timeout` - The timespan to time out after.
let ensureProcessesHaveStopped name timeout =
    let endTime = DateTime.Now.Add timeout
    
    while DateTime.Now <= endTime && (getProcessesByName name <> Seq.empty) do
        tracefn "Waiting for %s to stop (Timeout: %A)" name endTime
        Thread.Sleep 1000

    if getProcessesByName name <> Seq.empty then 
        failwithf "The process %s has not stopped (check the logs for errors)" name

/// Execute an external program and return the exit code.
/// [omit]
let shellExec = asyncShellExec >> Async.RunSynchronously

/// Allows to exec shell operations synchronously and asynchronously.
type Shell() =
    static member private GetParams (cmd, ?args, ?dir) =
        let args = defaultArg args ""
        let dir = defaultArg dir (Directory.GetCurrentDirectory())
        { WorkingDirectory = dir
          Program = cmd
          CommandLine = args 
          Args = [] }
       
    /// Runs the given process, waits for it's completion and returns the exit code.
    /// ## Parameters
    ///
    ///  - `cmd` - The command which should be run in elavated context.
    ///  - `args` - The process arguments (optional).
    ///  - `directory` - The working directory (optional).
    static member Exec (cmd, ?args, ?dir) = 
        shellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))

    /// Runs the given process asynchronously.
    /// ## Parameters
    ///
    ///  - `cmd` - The command which should be run in elavated context.
    ///  - `args` - The process arguments (optional).
    ///  - `directory` - The working directory (optional).
    static member AsyncExec (cmd, ?args, ?dir) =
        asyncShellExec (Shell.GetParams(cmd, ?args = args, ?dir = dir))
