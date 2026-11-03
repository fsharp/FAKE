/// Contains helper functions which allow to interact with the F# Interactive.
module Fake.Runtime.CoreCache

open Fake.Runtime.Environment
open Fake.Runtime.Trace
open Fake.Runtime.Runners
open Fake.Runtime.CompileRunner
open Fake.Runtime.HashGeneration
#if NETSTANDARD1_6
open System.Runtime.Loader
#endif

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Text.RegularExpressions
open System.Xml.Linq
open Yaaf.FSharp.Scripting
open System.Reflection
open Paket.ProjectFile
open Mono.Cecil

exception CacheOutdated

let getCached getUncached readFromCache writeToCache checkCacheUpToDate =
    let inline getUncached () =
        let data = getUncached ()
        writeToCache data
        data

    if checkCacheUpToDate () then
        try
            readFromCache ()
        with
        | CacheOutdated -> getUncached ()
        | e ->
            Trace.traceError
            <| sprintf "Fake cache failed, please consider reporting a bug: %O" e

            getUncached ()
    else
        getUncached ()

type ICachingProvider =
    abstract TryLoadCache: context: FakeContext -> FakeContext * CoreCacheInfo option
    abstract SaveCache: context: FakeContext * cache: CoreCacheInfo -> unit
    abstract CleanCache: context: FakeContext -> unit

module internal Cache =
    let xname name = XName.Get(name)

    let create (loadedAssemblies: Reflection.Assembly seq) =
        let xelement name = XElement(xname name)
        let xattribute name value = XAttribute(xname name, value)

        let doc = XDocument()
        let root = xelement "FAKECache"
        doc.Add(root)
        let assemblies = xelement "Assemblies"
        root.Add(assemblies)

        let assemNodes =
            loadedAssemblies
            |> Seq.map (fun assem ->
                let ele = xelement "Assembly"
                ele.Add(xattribute "Location" assem.Location)
                ele.Add(xattribute "FullName" assem.FullName)
                ele.Add(xattribute "Version" (assem.GetName().Version.ToString()))
                ele)
            |> Seq.iter (assemblies.Add)

        doc

    let read (path: string) =
        let doc = XDocument.Load(path)
        //let root = doc.Descendants() |> Seq.exactlyOne
        let assembliesEle = doc.Descendants(xname "Assemblies") |> Seq.exactlyOne

        assembliesEle.Descendants()
        |> Seq.map (fun assemblyEle ->
            let get name = assemblyEle.Attribute(xname name).Value

            { Location = get "Location"
              FullName = get "FullName"
              Version = get "Version" })
        |> Seq.toList

    let warningsFileName (f: FakeContext) = f.HashPath + "_warnings.txt"

    let cleanFiles filesGen f =
        filesGen
        |> List.map (fun gen -> gen f)
        |> List.filter File.Exists
        |> List.iter File.Delete

    let tryLoadDefault (context: FakeContext) =
        let cachedDll = context.CachedAssemblyFilePath

        if File.Exists cachedDll then
            let warnings = warningsFileName context
            let warningText = File.ReadAllText warnings

            Some
                { CompiledAssembly = cachedDll
                  Warnings = warningText }
        else
            None

    let defaultProvider =
#if !NETSTANDARD1_6
        let xmlFileName (f: FakeContext) = f.HashPath + "_config.xml"
        let cleanFiles = cleanFiles [ warningsFileName; xmlFileName ]

        { new ICachingProvider with
            member __.CleanCache context = cleanFiles context

            member __.TryLoadCache(context) =
                let xmlFile = xmlFileName context

                match tryLoadDefault context with
                | Some config when File.Exists xmlFile ->
                    let readXml = read xmlFile
                    let fsiOpts = context.Config.CompileOptions.FsiOptions // |> FsiOptions.ofArgs

                    { context with
                        Config =
                            { context.Config with
                                CompileOptions =
                                    { context.Config.CompileOptions with
                                        FsiOptions =
                                            { fsiOpts with
                                                References =
                                                    fsiOpts.References @ (readXml |> List.map (fun x -> x.Location)) } }
                                RuntimeOptions =
                                    { context.Config.RuntimeOptions with
                                        _RuntimeDependencies =
                                            context.Config.RuntimeOptions._RuntimeDependencies @ readXml } } },
                    Some config
                | _ -> context, None
            //member x.GetAssembliesFromCache c = c.Assemblies
            member x.SaveCache(context, cache) =
                let xmlFile = xmlFileName context
                let warnings = warningsFileName context

                let dynamicAssemblies =
                    System.AppDomain.CurrentDomain.GetAssemblies()
                    |> Seq.filter (fun assem -> assem.IsDynamic)
                    |> Seq.map (fun assem -> assem.GetName().Name)
                    |> Seq.filter (fun assem -> assem <> fsiAssemblyName)
                    |> Seq.filter (fun assem -> not <| assem.StartsWith cachedAssemblyPrefix)
                    // General Reflection.Emit helper (most likely harmless to ignore)
                    |> Seq.filter (fun assem -> assem <> "Anonymously Hosted DynamicMethods Assembly")
                    // RazorEngine generated
                    |> Seq.filter (fun assem -> assem <> "RazorEngine.Compilation.ImpromptuInterfaceDynamicAssembly")
                    |> Seq.cache

                if dynamicAssemblies |> Seq.length > 0 then
                    let msg =
                        sprintf
                            "Dynamic assemblies were generated during evaluation of script (%s).\nCan not save cache."
                            (System.String.Join(", ", dynamicAssemblies))

                    trace msg
                else
                    let assemblies =
                        System.AppDomain.CurrentDomain.GetAssemblies()
                        |> Seq.filter (fun assem -> not assem.IsDynamic)
                        |> Seq.filter (fun assem -> not <| assem.GetName().Name.StartsWith cachedAssemblyPrefix)
                        // They are not dynamic, but can't be re-used either.
                        |> Seq.filter (fun assem ->
                            not
                            <| assem.GetName().Name.StartsWith("CompiledRazorTemplates.Dynamic.RazorEngine_"))

                    let cacheConfig: XDocument = create assemblies
                    cacheConfig.Save(xmlFile)
                    File.WriteAllText(warnings, cache.Warnings) }
#else
        let cleanFiles = cleanFiles [ warningsFileName ]

        { new ICachingProvider with
            member __.CleanCache context = cleanFiles context

            member __.TryLoadCache(context) =
                traceFAKE
                    "Default caching is disabled on dotnetcore, see https://github.com/dotnet/coreclr/issues/919#issuecomment-219212910"

                traceFAKE "Use a Fake-Header to get rid of this warning and let FAKE handle the script dependencies!"

                let fsiOpts = context.Config.CompileOptions.FsiOptions // |> FsiOptions.ofArgs

                if not fsiOpts.NoFramework then // Caller should take care!
                    let basePath = System.AppContext.BaseDirectory

                    let references =
                        System.IO.Directory.GetFiles(basePath, "*.dll")
                        |> Seq.filter (fun r ->
                            not (System.IO.Path.GetFileName(r).ToLowerInvariant().StartsWith("api-ms")))
                        |> Seq.choose (fun r ->
                            try
                                Some(AssemblyInfo.ofLocation r)
                            with e ->
                                if context.Config.VerboseLevel.PrintVerbose then
                                    Trace.tracefn "Error while trying to load assembly metainformation: %O" e

                                None)
                        |> Seq.toList

                    let newAdditionalArgs =
                        { fsiOpts with
                            NoFramework = true
                            Debug = Some DebugMode.Portable }

                    { context with
                        Config =
                            { context.Config with
                                CompileOptions = { context.Config.CompileOptions with FsiOptions = newAdditionalArgs }
                                RuntimeOptions =
                                    { context.Config.RuntimeOptions with
                                        _RuntimeDependencies =
                                            references @ context.Config.RuntimeOptions._RuntimeDependencies } } },
                    None
                else
                    context, None

            member x.SaveCache(context, cache) = () }
#endif

let loadAssembly (loadContext: AssemblyLoadContext) (logLevel: Trace.VerboseLevel) (assemInfo: AssemblyInfo) =
    let realLoadAssembly (assemInfo: AssemblyInfo) =
        let assem =
            if assemInfo.Location <> "" then
                try
                    Some assemInfo.Location, loadContext.LoadFromAssemblyPath(assemInfo.Location)
                with :? FileLoadException as e ->
                    if logLevel.PrintVerbose then
                        Trace.tracefn "Error while loading assembly: %O" e

                    let assemblyName = System.Reflection.AssemblyName(assemInfo.FullName)

                    let asem =
                        System.Reflection.Assembly.Load(System.Reflection.AssemblyName(assemblyName.Name))

                    if logLevel.PrintVerbose then
                        Trace.traceFAKE
                            "recovered and used already loaded assembly '%s' instead of '%s' ('%s')"
                            asem.FullName
                            assemInfo.FullName
                            assemInfo.Location

                    None, asem
            else
                None, loadContext.LoadFromAssemblyName(AssemblyName(assemInfo.FullName))

        Some(assem)

    try
        //let location = assemInfo.Location.Replace("\\", "/")
        //let newLocation = location.Replace("/ref/", "/lib/")
        //try
        realLoadAssembly assemInfo
    //with
    //| :? System.BadImageFormatException when location.Contains ("/ref/") && File.Exists newLocation->
    //    // TODO: This is a real bad hack for now...
    //    realLoadAssembly { assemInfo with Location = newLocation }
    with ex ->
        if logLevel.PrintVerbose then
            tracefn "Unable to find assembly %A. (Error: %O)" assemInfo ex

        None

let findInAssemblyList (name: AssemblyName) (runtimeDependencies: AssemblyInfo list) =
    let strName = name.FullName

    match runtimeDependencies |> List.tryFind (fun r -> r.FullName = strName) with
    | Some a -> Some(true, a)
    | _ ->
        let token = name.GetPublicKeyToken()
        // When null or empty accept what we have
        // See https://github.com/fsharp/FAKE/issues/2381
        let emptyToken = isNull token || token.Length = 0
        let emptyVersion = isNull name.Version

        match
            runtimeDependencies
            |> Seq.map (fun r -> AssemblyName(r.FullName), r)
            |> Seq.tryFind (fun (n, _) -> n.Name = name.Name && (emptyToken || n.GetPublicKeyToken() = token))
        with
        | Some (otherName, info) ->
            // Then the version matches or is null and the public token is null we still accept this as perfect match
            Some((emptyToken && (emptyVersion || otherName.Version = name.Version)), info)
        | _ -> None

let findAndLoadInRuntimeDeps
    (loadContext: AssemblyLoadContext)
    (name: AssemblyName)
    (logLevel: Trace.VerboseLevel)
    (runtimeDependencies: AssemblyInfo list)
    =
    let strName = name.FullName

    if logLevel.PrintVerbose then
        tracefn "Trying to resolve: %s" strName


    // These guys need to be handled carefully, they must only exist a single time in memory
    let wellKnownAssemblies =
        [ Environment.fakeContextAssembly (); Environment.fsCoreAssembly () ]

    let isPerfectMatch, result =
        match wellKnownAssemblies |> List.tryFind (fun a -> a.GetName().Name = name.Name) with
        | Some a ->
            let knownName = a.GetName()

            if
                knownName.Version < name.Version
                && knownName.Name.ToLower().Contains("fsharp.core")
            then
                // See https://github.com/fsharp/FAKE/issues/2001
                traceFAKE
                    "Downgrade (%O -> %O) of FSharp.Core detected. Try to pin FSharp.Core or upgrade fake."
                    name.Version
                    knownName.Version

            a.FullName = strName, (Some(None, a))
        | None ->
#if NETSTANDARD1_6
            // Check if we can resolve to a framework assembly.
            let result =
                try
                    let assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(name)

                    let isFramework =
                        assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                        |> Seq.exists (fun m ->
                            m.Key = ".NETFrameworkAssembly" // net8.0 framework assemblies have NETFrameworkAssembly attribute removed. So identify them via RepositoryUrl:
                            || (m.Key = "RepositoryUrl" && m.Value.StartsWith "https://github.com/dotnet/"))

                    if not isFramework then
                        None
                    else
                        let location =
                            try
                                Some assembly.Location
                            with
                            | :? NotSupportedException -> None // When this is a dynamic assembly
                            | e ->
                                if logLevel.PrintVerbose then
                                    tracefn
                                        "(DEBUG: Specify Exception Type) Could not get Location from '%s': %O"
                                        strName
                                        e

                                None

                        Some(location, assembly)
                with
                | :? FileNotFoundException -> None // because the parameter is a an assembly name...
                | e ->
                    if logLevel.PrintVerbose then
                        tracefn "(DEBUG: Specify Exception Type) Exception in LoadFromAssemblyName: %O" e

                    None

            match result with
            | Some r -> true, Some r
            | None ->
#endif
            if logLevel.PrintVerbose then
                tracefn "Could not find assembly in the default load-context: %s" strName

            match runtimeDependencies |> findInAssemblyList name with
            | Some (perfectMatch, a) -> perfectMatch, loadAssembly loadContext logLevel a
            | None -> false, None

    match result with
    | Some (location, a) ->
        if logLevel.PrintVerbose then
            if isPerfectMatch then
                tracefn "Redirect assembly load to known assembly: %s (%A)" strName location
            else
                traceFAKE "Redirect assembly from '%s' to '%s' (%A)" strName a.FullName location

        a
    | _ ->
        if
            not (strName.StartsWith("FSharp.Compiler.Service.resources"))
            && not (strName.StartsWith("FSharp.Compiler.Service.MSBuild"))
        then
            if logLevel.PrintVerbose then
                tracefn "Could not resolve assembly: %s" strName

        null

let findAndLoadInRuntimeDepsCached =
    let assemblyCache =
        System.Collections.Concurrent.ConcurrentDictionary<_, Assembly>()

    fun (loadContext: AssemblyLoadContext) (name: AssemblyName) (logLevel: Trace.VerboseLevel) (runtimeDependencies: AssemblyInfo list) ->
        let mutable wasCalled = false

        let result =
            assemblyCache.GetOrAdd(
                name.Name,
                (fun _ ->
                    wasCalled <- true
                    findAndLoadInRuntimeDeps loadContext name logLevel runtimeDependencies)
            )

        if wasCalled && isNull result then
            failwithf
                """Could not load '%A'.
This can happen for various reasons:
- You are trying to load full-framework assemblies which is not supported
  -> You might try to load a legacy-script with the new netcore runner.
    Please take a look at the migration guide: https://fake.build/guide/fake-migrate-to-fake-5.html
- The nuget cache (or packages folder) might be broken.
  -> Please save your state, open an issue and then 
  - delete '%s' from the '~/.nuget' cache (and the 'packages' folder)
  - delete 'paket-files/paket.restore.cached' if it exists
  - delete '<script.fsx>.lock' if it exists
  - try running fake again
  - the package should be downloaded again
- Some package introduced a breaking change in their dependencies and .dll files are missing in the resolution
  -> Try to compare the lockfile with a previous working version
  -> Try to lower transitive dependency versions (for example by adding 'strategy: min' to the paket group)
  see https://github.com/fsharp/FAKE/issues/1966 where this happend for 'System.Reactive' version 4

-> If the above doesn't apply or you need help please open an issue!"""
                name
                name.Name

        if not wasCalled && not (isNull result) then
            let loadedName = result.GetName()

            let isPerfectMatch =
                loadedName.Name = name.Name && loadedName.Version = name.Version

            if logLevel.PrintVerbose then
                if not isPerfectMatch then
                    traceFAKE "Redirect assembly from '%A' to previous loaded assembly '%A'" name loadedName
                else
                    tracefn "Redirect assembly load to previously loaded assembly: %A" loadedName

        result

let findUnmanagedInRuntimeDeps
    (unmanagedDllName: string)
    (logLevel: Trace.VerboseLevel)
    (nativeLibraries: NativeLibrary list)
    =
    if logLevel.PrintVerbose then
        tracefn "Trying to resolve native library: %s" unmanagedDllName

    match
        nativeLibraries
        |> Seq.tryFind (fun l ->
            let fnExt = Path.GetFileName l.File
            let fn = Path.GetFileNameWithoutExtension l.File
            unmanagedDllName = fn || unmanagedDllName = fnExt)
    with
    | Some lib ->
        let path = lib.File

        if logLevel.PrintVerbose then
            tracefn "Redirect native library '%s' to known path: %s" unmanagedDllName path

        path
    | None ->
        // will failwithf later anyway.
        if logLevel.PrintVerbose then
            tracefn "Could not resolve native library '%s', fallback to CLR-host!" unmanagedDllName

        null

let resolveUnmanagedDependencyCached =
    let libCache = System.Collections.Concurrent.ConcurrentDictionary<_, string>()

    fun (loadFromPath: string -> nativeint) (unmanagedDllName: string) (logLevel: Trace.VerboseLevel) (nativeLibraries: NativeLibrary list) ->
        let mutable wasCalled = false

        let path =
            libCache.GetOrAdd(
                unmanagedDllName,
                (fun _ ->
                    wasCalled <- true
                    findUnmanagedInRuntimeDeps unmanagedDllName logLevel nativeLibraries)
            )

        //if wasCalled && not wasFound then
        //let available =
        //    if nativeLibraries.Length > 0 then
        //        "Available native libraries:\n - " + String.Join("\n - ", nativeLibraries |> Seq.map (fun n -> n.File))
        //    else "No native libraries found!"
        // TODO: In the future use the unmanaged assembly resolve event to throw this.
        (*failwithf """Could not resolve native library '%s'.
%s

This can happen for various reasons:
- The file '%s' could not be found in the PATH environment variable.
- The nuget cache (or packages folder) might be broken.
  -> Please save your state, open an issue and then
  - delete the source package of '%s' from the '~/.nuget' cache (and the 'packages' folder)
  - delete 'paket-files/paket.restore.cached' if it exists
  - delete '<script.fsx>.lock' if it exists
  - try running fake again
  - the package should be downloaded again

-> If the above doesn't apply or you need help please open an issue!""" unmanagedDllName available unmanagedDllName unmanagedDllName*)
        if not wasCalled && not (isNull path) then
            if logLevel.PrintVerbose then
                tracefn "Redirect native library '%s' to previous loaded library '%s'" unmanagedDllName path
        // Zero means use CLR-Host strategy, see https://github.com/fsharp/FAKE/issues/2342
        if isNull path then IntPtr.Zero else loadFromPath path

#if NETSTANDARD1_6
// See https://github.com/dotnet/coreclr/issues/6411
type FakeLoadContext
    (
        printDetails: Trace.VerboseLevel,
        dependencies: AssemblyInfo list,
        nativeLibraries: NativeLibrary list
    ) =
    // Mark as Collectible once supported: https://docs.microsoft.com/en-us/dotnet/standard/assembly/unloadability-howto?view=netcore-3.0
    inherit AssemblyLoadContext()
    let allReferences = dependencies

    override x.Load(assem: AssemblyName) =
        findAndLoadInRuntimeDepsCached x assem printDetails allReferences
    // Helper for FS0408, FS0419, FS0405
    member private x.LoadUnmanagedDllFromPathHelper s = base.LoadUnmanagedDllFromPath s
    // Support for unmanaged dlls, see https://github.com/fsharp/FAKE/issues/2007
    override x.LoadUnmanagedDll(unmanagedDllName) =
        resolveUnmanagedDependencyCached x.LoadUnmanagedDllFromPathHelper unmanagedDllName printDetails nativeLibraries
#endif

let fakeDirectoryName = ".fake"

let prepareContext (config: FakeConfig) (cache: ICachingProvider) =

    let fakeDir =
        Path.Combine(Path.GetDirectoryName config.ScriptFilePath, fakeDirectoryName)

    let cacheDir = Path.Combine(fakeDir, Path.GetFileName config.ScriptFilePath)
    // the actual file containing the last 'hash' (relevant for the filename)
    let fakeCacheFile = Path.Combine(cacheDir, "fake-hash.txt")
    // contains all files 'relevant' for checking if the cache is up to date
    let fakeCacheDepsFile = Path.Combine(cacheDir, "fake-hash-files.txt")
    // contains all file contents 'relevant' for checking if the cache is up to date, joined via \n
    let fakeCacheContentsFile = Path.Combine(cacheDir, "fake-hash-contents.txt")

    let getOpts (c: CompileOptions) = c.FsiOptions.AsArgs // @ c.CompileReferences

    let getHashUncached () =
        //TODO this is only calculating the hash for the input file, not anything #load-ed
        let allScriptContents =
            getAllScripts true config.CompileOptions.FsiOptions.Defines config.ScriptTokens.Value config.ScriptFilePath

        let combined = getCombinedString allScriptContents (getOpts config.CompileOptions)
        allScriptContents, combined, getStringHash combined

    let writeToCache ((scripts: Script list), (combined: string), (hash: string)) =
        File.WriteAllText(fakeCacheFile, hash)
        let locations = scripts |> List.map (fun s -> s.Location)
        // write fakeCacheContentsFile
        File.WriteAllText(fakeCacheContentsFile, combined)
        // write fakeCacheDepsFile
        File.WriteAllLines(fakeCacheDepsFile, locations |> Seq.map (Path.fixPathForCache config.ScriptFilePath))

    let readFromCache () = File.ReadAllText fakeCacheFile

    let scriptHash =
        let inline getUncached () =
            let scripts, combined, hashValue = getHashUncached ()
            writeToCache (scripts, combined, hashValue)
            hashValue

        let cacheFilesExist =
            File.Exists fakeCacheDepsFile
            && File.Exists fakeCacheContentsFile
            && File.Exists fakeCacheFile

        let inline dependencyCacheUpToDate () =
            let contents =
                File.ReadLines fakeCacheDepsFile
                |> Seq.map (Path.readPathFromCache config.ScriptFilePath)
                |> Seq.map (fun line ->
                    if File.Exists line then
                        Some
                            { HashContent = File.ReadAllText line
                              Location = line }
                    else
                        None)
                |> Seq.toList

            if contents |> Seq.exists Option.isNone then
                false
            else
                let actual =
                    contents
                    |> Seq.choose id
                    |> fun texts -> getCombinedString texts (getOpts config.CompileOptions)

                let cached = File.ReadAllText fakeCacheContentsFile
                actual = cached
        // TODO: This could be improved in such a way that we only
        // TODO: need to tokenize "changed" files and not everything
        if cacheFilesExist && dependencyCacheUpToDate () then
            // get assembly list from cache
            try
                readFromCache ()
            with e ->
                eprintfn "Caching fake section failed: %O" e
                getUncached ()
        else
            getUncached ()

    let context =
        { FakeContext.Config = config
          CreateAssemblyContext = fun () -> failwithf "No context creation function set yet."
          FakeDirectory = fakeDir
          Hash = scriptHash }

    let context, cache = cache.TryLoadCache context
#if NETSTANDARD1_6
    // See https://github.com/dotnet/coreclr/issues/6411 and https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md
    let fakeLoadContext () : AssemblyLoadContext =
        FakeLoadContext(
            context.Config.VerboseLevel,
            context.Config.RuntimeOptions.RuntimeDependencies,
            context.Config.RuntimeOptions.NativeLibraries
        )
        :> _
#else
    let fakeLoadContext () : AssemblyLoadContext = new AssemblyLoadContext()
#endif
    { context with CreateAssemblyContext = fakeLoadContext }, cache

let setupAssemblyResolverLogger (context: FakeContext) =
#if NETSTANDARD1_6
    let globalLoadContext = AssemblyLoadContext.Default

    globalLoadContext.add_Resolving (
        new Func<AssemblyLoadContext, AssemblyName, Assembly>(fun _ name ->
            let strName = name.FullName
#else
    AppDomain.CurrentDomain.add_AssemblyResolve (
        new ResolveEventHandler(fun _ ev ->
            let strName = ev.Name
            let name = AssemblyName(strName)
#endif
            if context.Config.VerboseLevel.PrintVerbose then
                printfn "Global resolve event: %s" name.FullName

            null)
    )

let runScriptWithCacheProviderExt
    (config: FakeConfig)
    (cache: ICachingProvider)
    : RunResult * ResultCoreCacheInfo * FakeContext =
    let newContext, cacheInfo = prepareContext config cache

    setupAssemblyResolverLogger newContext
    // Create an env var that only contains the build script args part from the --fsiargs (or "").
    setEnvironVar "fsiargs-buildscriptargs" (String.Join(" ", config.CompileOptions.FsiOptions.AsArgs))

    let resultCache, result = runFakeScript cacheInfo newContext

    match resultCache.AsCacheInfo with
    | Some newCache -> cache.SaveCache(newContext, newCache)
    | _ -> ()

    // Return if the script suceeded
    result, resultCache, newContext

[<Obsolete("Use runScriptWithCacheProviderExt instead")>]
let runScriptWithCacheProvider (config: FakeConfig) (cache: ICachingProvider) : RunResult =
    let res, _, _ = runScriptWithCacheProviderExt config cache
    res
