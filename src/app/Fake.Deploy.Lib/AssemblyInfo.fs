﻿namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("FAKE - F# Make Deploy Lib")>]
[<assembly: GuidAttribute("AA284C42-1396-42CB-BCAC-D27F18D14AC7")>]
[<assembly: AssemblyProductAttribute("FAKE - F# Make")>]
[<assembly: AssemblyVersionAttribute("4.46.1")>]
[<assembly: AssemblyInformationalVersionAttribute("4.46.1")>]
[<assembly: AssemblyFileVersionAttribute("4.46.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "4.46.1"
    let [<Literal>] InformationalVersion = "4.46.1"
