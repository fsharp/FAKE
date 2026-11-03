module internal Fake.Runtime.FakeHeader

open System
open System.IO
open Fake.Runtime
open Fake.Runtime.Runners
open Fake.Runtime.Trace
open Paket
open System

type HeaderType =
    | PaketInline
    | PaketDependenciesRef

type FakeSection =
    | PaketDependencies of HeaderType * Paket.Dependencies * Lazy<Paket.DependenciesFile> * group: String option

let readAllLines (r: TextReader) =
    seq {
        let mutable line = r.ReadLine()

        while not (isNull line) do
            yield line
            line <- r.ReadLine()
    }

let private dependenciesFileName = "paket.dependencies"

type InlinePaketDependenciesSection = { Header: HeaderType; Section: string }

let writeFixedPaketDependencies (scriptCacheDir: Lazy<string>) (f: InlinePaketDependenciesSection) =
    match f.Header with
    | HeaderType.PaketInline ->
        let dependenciesFile = Path.Combine(scriptCacheDir.Value, dependenciesFileName)

        let fixedSection =
            f.Section.Split([| "\r\n"; "\r"; "\n" |], System.StringSplitOptions.None)
            |> Seq.map (fun line ->
                let replacePaketCommand (command: string) (line: string) =
                    let trimmed = line.Trim()

                    if trimmed.StartsWith command then
                        let restString = trimmed.Substring(command.Length).Trim()

                        let isValidPath =
                            try
                                Path.GetFullPath restString |> ignore
                                true
                            with _ ->
                                false

                        let isAbsoluteUrl =
                            match Uri.TryCreate(restString, UriKind.Absolute) with
                            | true, _ -> true
                            | _ -> false

                        if isAbsoluteUrl || not isValidPath || Path.IsPathRooted restString then
                            line
                        else
                            line.Replace(restString, Path.Combine("..", "..", restString))
                    else
                        line

                line |> replacePaketCommand "source" |> replacePaketCommand "cache")

        File.WriteAllLines(dependenciesFile, fixedSection)

        PaketDependencies(
            HeaderType.PaketInline,
            Dependencies dependenciesFile,
            (lazy DependenciesFile.ReadFromFile dependenciesFile),
            None
        )
    | PaketDependenciesRef ->
        let groupStart = "group "
        let fileStart = "file "

        let readLine (l: string) : (string * string) option =
            if l.StartsWith groupStart then
                ("group", (l.Substring groupStart.Length).Trim()) |> Some
            elif l.StartsWith fileStart then
                ("file", (l.Substring fileStart.Length).Trim()) |> Some
            elif String.IsNullOrWhiteSpace l then
                None
            else
                failwithf "Cannot recognise line in dependency section: '%s'" l

        let options =
            (use r = new StringReader(f.Section)
             readAllLines r |> Seq.toList)
            |> Seq.choose readLine
            |> dict

        let group =
            match options.TryGetValue "group" with
            | true, gr -> Some gr
            | _ -> None

        let file =
            match options.TryGetValue "file" with
            | true, depFile -> depFile
            | _ -> dependenciesFileName

        let fullpath = Path.GetFullPath file

        PaketDependencies(
            PaketDependenciesRef,
            Dependencies fullpath,
            (lazy DependenciesFile.ReadFromFile fullpath),
            group
        )
//| _ -> failwithf "unknown dependencies header '%s'" f.Header

let tryReadPaketDependenciesFromScript
    (tokenized: Fake.Runtime.FSharpParser.TokenizedScript)
    cacheDir
    (scriptPath: string)
    =
    let pRefStr = "paket:"
    let grRefStr = "groupref"

    let groupReferences, paketLines =
        FSharpParser.findInterestingItems tokenized
        |> Seq.choose (fun item ->
            match item with
            | FSharpParser.InterestingItem.Reference ref when ref.StartsWith pRefStr ->
                let sub = ref.Substring(pRefStr.Length)
                Some(sub.TrimStart [| ' ' |])
            | _ -> None)
        |> Seq.toList
        |> List.partition (fun ref -> ref.StartsWith(grRefStr, System.StringComparison.OrdinalIgnoreCase))

    let paketCode = paketLines |> String.concat "\n"

    let paketGroupReferences =
        groupReferences
        |> List.map (fun groupRefString ->
            let raw = groupRefString.Substring(grRefStr.Length).Trim()
            let commentStart = raw.IndexOf "//"

            if commentStart >= 0 then
                raw.Substring(0, commentStart).Trim()
            else
                raw)

    if paketCode <> "" && paketGroupReferences.Length > 0 then
        failwith "paket code in combination with a groupref is currently not supported!"

    if paketGroupReferences.Length > 1 then
        failwith "multiple paket groupref are currently not supported!"

    if paketCode <> "" then
        let fixDefaults (paketCode: string) =
            let lines =
                paketCode.Split([| '\r'; '\n' |])
                |> Array.map (fun line -> line.ToLower().TrimStart())

            let storageRef = "storage"
            let sourceRef = "source"
            let frameworkRef = "framework"
            let restrictionRef = "restriction"
            let containsStorage = lines |> Seq.exists (fun line -> line.StartsWith(storageRef))
            let containsSource = lines |> Seq.exists (fun line -> line.StartsWith(sourceRef))

            let containsFramework =
                lines |> Seq.exists (fun line -> line.StartsWith(frameworkRef))

            let containsRestriction =
                lines |> Seq.exists (fun line -> line.StartsWith(restrictionRef))

            paketCode
            |> fun p -> if containsStorage then p else "storage: none" + "\n" + p
            |> fun p ->
                if containsSource then
                    p
                else
                    "source https://api.nuget.org/v3/index.json" + "\n" + p
            |> fun p ->
                if containsFramework || containsRestriction then
                    p
                else
                    "framework: netstandard2.1,netstandard2.0,net6.0,net8.0,net10.0" + "\n" + p

        { Header = PaketInline
          Section = fixDefaults paketCode }
        |> writeFixedPaketDependencies cacheDir
        |> Some
    else
        match paketGroupReferences with
        | [] -> None
        | group :: _ ->
            let fullScriptDir = Path.GetFullPath scriptPath
            let scriptDir = Path.GetDirectoryName fullScriptDir

            let dependencies =
                match Paket.Dependencies.TryLocate(scriptDir) with
                | Some deps -> deps
                | None ->
                    failwithf
                        "Could not find '%s'. To use Fake with an external file, please run 'paket init' first.%sAlternatively you can use inline-dependencies. See https://fake.build/guide/fake-modules.html"
                        Constants.DependenciesFileName
                        Environment.NewLine

            let fullpath = Path.GetFullPath dependencies.DependenciesFile

            PaketDependencies(
                PaketDependenciesRef,
                dependencies,
                (lazy DependenciesFile.ReadFromFile fullpath),
                Some group
            )
            |> Some
