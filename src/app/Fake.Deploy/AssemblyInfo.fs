﻿namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy tool")>]
[<assembly: GuidAttribute("413E2050-BECC-4FA6-87AA-5A74ACE9B8E1")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("4.40.0")>]
[<assembly: AssemblyInformationalVersionAttribute("4.40.0")>]
[<assembly: AssemblyFileVersionAttribute("4.40.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "4.40.0"
    let [<Literal>] InformationalVersion = "4.40.0"
