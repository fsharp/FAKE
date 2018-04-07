# FAKE 5 - Custom Modules

**Note:  This documentation is for FAKE.exe version 5.0 or later. The old documentation can be found [here](legacy-customtasks.html)**

"FAKE - F# Make" is intended to be an extensible build framework and therefore it should be as easy as possible to create custom tasks. 
This tutorial shows how to create a (very simple) custom task in C#. The same works for other .NET language like Visual Basic or F#.

## Creating a custom task

Open Visual Studio and create a new C# class library called my MyCustomTask and create a class called RandomNumberTask:

```csharp
using System;
namespace MyCustomTask
{
    public class RandomNumberTask
    {
        public static int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }
    }
}
```

Now you can build, package and upload your build task to a NuGet feed.
There are no special requirements, you can add dependencies to your NuGet package and define the API however you like.

> Note: As FAKE 5 currently is a `netcore10` application you need to provide a binary in your NuGet package compatible with
> netcore. We suggest targetting `netstandard16` or lower.
> We will update FAKE 5 to target netcore20 as soon as netstandard20 is released, such that you can benefit from more APIs as soon as possible.

If you want to use FAKE's standard functionality (like [globbing](http://en.wikipedia.org/wiki/Glob_(programming))) within your CustomTask project, just reference the corresponding NuGet package and [explore the FAKE namespace](apidocs/index.html).

## Using the custom task

Assume you pushed your custom task as `MyTaskNuGetPackage` to NuGet.
Now you can use your CustomTask in the build script, by adding the

    nuget MyTaskNuGetPackage

to your [paket.dependencies](fake-fake5-modules.md), see the relevant documentation for adding modules.
This documentation now applies to your package too!
One example would be:

```fsharp
#r "paket:
nuget Fake.Core.Target prerelease
nuget MyTaskNuGetPackage"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open MyCustomTask

Target.create "GenerateNumber" (fun _ ->
    // use custom functionality
    RandomNumberTask.RandomNumber(2,13)
        |> Trace.tracefn "RandomNumber: %d"
)

Target.RunOrDefault "GenerateNumber"
```
