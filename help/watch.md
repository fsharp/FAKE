# Watching for file changes with "FAKE - F# Make"

FAKE makes it easy to setup monitoring for filesystem changes. Using the standard glob patterns you can
watch for changes, and automatically run a function or another target.

## Using WatchChanges

Add a new target named "Watch" to your build:

    Target "GenerateDocs" (fun _ ->
        tracefn "Generating docs."
    )

    Target "Watch" (fun _ ->
        use watcher = !! "docs/**/*.*" |> WatchChanges (fun changes -> 
            tracefn "%A" changes
            Run "GenerateDocs"
        )
    
        System.Console.ReadLine() |> ignore //Needed to keep FAKE from exiting
    
        watcher.Dispose() // Use to stop the watch from elsewhere, ie another task.
    )

Now run build.fsx and make some changes to the docs directory. They should be printed out to the console as they happen,
and the GenerateDocs target should be rerun.

If you need to watch only a subset of the files, say you want to rerun tests as soon as the compiled DLLs change:

    Target "RunTests" (fun _ ->
        tracefn "Running tests."
    )
    
    Target "Watch" (fun _ ->
        use watcher = !! "tests/**/bin/debug/*.dll" |> WatchChanges (fun changes -> 
            tracefn "%A" changes
            Run "RunTests"
        )
    
        System.Console.ReadLine() |> ignore //Needed to keep FAKE from exiting
    
        watcher.Dispose() // Use to stop the watch from elsewhere, ie another task.
    )

## Running on Linux or Mac OSX

`WatchChanges` requires additional care when running on Linux or Mac OSX. The following sections describe potential issues you may encounter.

### Maximum Number of Files to Watch Exception

When running on Linux or Mac OSX, you should add the following export to your `.bashrc` or `.bash_profile`:

```
export MONO_MANAGED_WATCHER=false
```

If you don't add this, you may see the following exception when attempting to run the `WatchChanges` task:

```
Running build failed.
Error:
System.IO.IOException: kqueue() FileSystemWatcher has reached the maximum nunmber of files to watch.
  at System.IO.KqueueMonitor.Add (System.String path, Boolean postEvents, System.Collections.Generic.List`1& fds) [0x00000] in <filename unknown>:0
  at System.IO.KqueueMonitor.Scan (System.String path, Boolean postEvents, System.Collections.Generic.List`1& fds) [0x00000] in <filename unknown>:0
  at System.IO.KqueueMonitor.Setup () [0x00000] in <filename unknown>:0
  at System.IO.KqueueMonitor.DoMonitor () [0x00000] in <filename unknown>:0
```

### Watching Changes from Windows over Parallels

The Windows file watcher does not appear to be able to correctly identify changes that occur within a folder shared by Parallels between Mac OSX and Windows. If you want to run `WatchChanges`, you will need to run your FAKE script from Mac OSX.

At this time, only Parallels is known to have this problem, but you should assume that any other virtualization solutions will have the same problem. If you confirm a similar problem with other Linux distros or VM platforms, please update this document accordingly.
