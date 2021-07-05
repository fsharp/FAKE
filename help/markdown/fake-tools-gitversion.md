# Generate semantic version number from Git with GitVersion

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="apidocs/v4/fake-gitversionhelper.html">here</a></p>
</div>

[GitVersion] is a tool that generates a Semantic Version number based on your Git history.

[API-Reference](apidocs/v5/fake-tools-gitversion.html)

## Minimal working example

```fsharp
#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.Tools.GitVersion
//"

open Fake.Core
open Fake.DotNet
open Fake.Tools

let install = lazy DotNet.install DotNet.Versions.FromGlobalJson

Target.create "Version" (fun _ ->
    GitVersion.generateProperties gitVersionGenerateProperties (fun p ->
        { p with ToolType = ToolType.CreateCLIToolReference(install.Value) })
)

```

[GitVersion]: https://gitversion.net/
