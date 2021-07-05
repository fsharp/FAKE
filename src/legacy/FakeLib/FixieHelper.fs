/// Contains tasks to run [Fixie](http://fixie.github.io/) unit tests.
[<System.Obsolete("use Fake.Testing.Fixie instead (FAKE0001 - package: Fake.Testing.Fixie)")>]
module Fake.FixieHelper

open System
open System.IO
open System.Text

/// Parameter type to configure the Fixie runner
[<CLIMutable>]
[<System.Obsolete("use Fake.Testing.Fixie instead (FAKE0001 - package: Fake.Testing.Fixie)")>]
type FixieParams = {
    /// FileName of the Fixie runner
    ToolPath: string
    /// Working directory (optional)
    WorkingDir: string
    /// Custom options to pass to Fixie runner
    CustomOptions: (string * Object) list
    /// A timeout for the test runner
    TimeOut: TimeSpan}

/// Fixie default parameters - tries to locate Fixie.Console.exe in any subfolder.
[<System.Obsolete("use Fake.Testing.Fixie instead (FAKE0001 - package: Fake.Testing.Fixie)")>]
let FixieDefaults = { 
    ToolPath = findToolInSubPath "Fixie.Console.exe" (currentDirectory @@ "tools" @@ "Fixie")
    WorkingDir = null
    CustomOptions = []
    TimeOut = TimeSpan.FromMinutes 5.}

/// This task to can be used to run [Fixie](http://patrick.lioi.net/fixie/) on test libraries.
/// ## Parameters
///
///  - `setParams` - Function used to overwrite the Fixie default parameters.
///  - `assemblies` - The file names of the test assemblies.
///
/// ## Sample
///
///     !! (testDir @@ "Test.*.dll") 
///       |> Fixie (fun p -> { p with CustomOptions = ["custom","1"; "test",2] })
[<System.Obsolete("use Fake.Testing.Fixie instead (FAKE0001 - package: Fake.Testing.Fixie)")>]
let Fixie setParams assemblies =
    let details = separated ", " assemblies
    use __ = traceStartTaskUsing "Fixie" details
    let parameters = setParams FixieDefaults
    
    let args =
        let option = sprintf "\"--%s\" "
        let appendCustomOptions options builder =
            options
                |> Seq.fold (fun builder (key, value) -> appendQuotedIfNotNull value (option key) builder) builder

        new StringBuilder()
        |> appendCustomOptions parameters.CustomOptions
        |> appendFileNamesIfNotNull assemblies
        |> toText

    if 0 <> ExecProcess (fun info ->  
        info.FileName <- parameters.ToolPath
        info.WorkingDirectory <- parameters.WorkingDir
        info.Arguments <- args) parameters.TimeOut
    then
        failwithf "Fixie test failed on %s." details
