# Analyze your code coverage with Coverlet

From the [project](https://github.com/tonerdo/coverlet):
> Coverlet is a cross platform code coverage framework for .NET, with support for line, branch and method coverage. It works with .NET Framework on Windows and .NET Core on all supported platforms.

[API-Reference](apidocs/v5/fake-dotnet-testing-coverlet.html)

## Minimal working example

To work with Coverlet, you must first:

* Add the NuGet reference `coverlet.msbuild` to your test projects (and only your test projects!).
* Ensure that these test projects are marked as such with a property `<IsTestProject>true</IsTestProject>`.

Then, Coverlet will run as part of `dotnet test` using `Coverlet.withDotNetTestOptions`:

```fsharp
#r "paket:
nuget Fake.DotNet.Testing.Coverlet //"
open Fake.DotNet
open Fake.DotNet.Testing

DotNet.test (fun p ->
    { p with 
        // Your dotnet test configuration here...
        Configuration = DotNet.BuildConfiguration.Release
    }
    |> Coverlet.withDotNetTestOptions (fun p ->
        { p with
            Output = "coverage.json"
        }))
    "tests/MyProject.fsproj"
```

## Full example

```fsharp
#r "paket:
nuget Fake.DotNet.Testing.Coverlet //"
open Fake.DotNet
open Fake.DotNet.Testing

DotNet.test (fun p ->
    { p with 
        // Your dotnet test configuration here...
        Configuration = DotNet.BuildConfiguration.Release
    }
    |> Coverlet.withDotNetTestOptions (fun p ->
        { p with
            OutputFormat = [Coverlet.OutputFormat.OpenCover]
            Output = "coverage.xml"
            Include = [
                // Include all namespaces from assemblies whose name starts with MyProject
                "MyProject.*", "*"
            ]
            Exclude = [
                // Exclude all namespaces from assemblies whose name ends with .Test or .Tests
                "*.Tests?", "*"
                // Exclude all namespaces starting with System., even in included assemblies
                "*", "System.*"
            ]
            // Exclude assemblies, types and methods marked with these attributes
            ExcludeByAttribute = ["MyCustomIgnoreCoverageAttribute"]
            // Exclude all code from these files
            ExcludeByFile = ["AssemblyInfo.fs"; "Program.fs"]
            // Merge results with results from another coverage session
            // (which must have OutputFormat = Json)
            MergeWith = Some "other-coverage.json"
            // Fail if total line coverage is below 80%
            Threshold = Some 80
            TresholdType = Coverlet.ThresholdType.Line
            ThresholdStat = Coverlet.ThresholdState.Total
            // Generate links to SourceLink URLs rather than local paths
            UseSourceLink = true
        }))
    "tests/MyProject.fsproj"
```
