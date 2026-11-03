module Fake.Core.IntegrationTests.SimpleHelloWorldTests

open System.Text.RegularExpressions
open Expecto
open Expecto.Flip
open System
open System.IO
open System.Text.Json
open Fake.Core

let fail s = Expect.isTrue s false
let ignProc = ignore<ProcessResult>

type Declaration =
    { File: string
      Line: int
      Column: int
      ErrorDetail: string }

    static member Empty =
        { File = ""
          Line = 0
          Column = 1
          ErrorDetail = "" }

/// a target dependency, either a hard or a soft dependency.
type Dependency =
    { Name: string
      Declaration: Declaration }

/// a FAKE target, its description and its relations to other targets (dependencies), including the
/// declaration lines of the target and the dependencies.
type Target =
    { Name: string
      HardDependencies: Dependency list
      SoftDependencies: Dependency list
      Declaration: Declaration
      Description: string }

[<Tests>]
let tests =
    testList
        "Fake.Core.IntegrationTests"
        [ testCase "fake-cli local tool works, #2425"
          <| fun _ ->
              let scenario = "i002425-dotnet-cli-tool-works"
              prepare scenario
              let scenarioPath = resolvePath scenario ""
              // dotnet tool install --version 5.19.0-alpha.local.1 fake-cli --add-source /e/Projects/FAKE/release/dotnetcore/

              // Work around https://github.com/dotnet/sdk/issues/40655 by specifying the tool manifest explicitly
              let manifestPath = Path.Combine(scenarioPath, ".config", "dotnet-tools.json")

              [ yield!
                    [ "tool"
                      "install"
                      "--prerelease"
                      "fake-cli"
                      "--add-source"
                      releaseDotnetCoreDir
                      "--tool-manifest"
                      manifestPath ] ]
              |> runDotNetRaw
              |> CreateProcess.withWorkingDirectory scenarioPath
              |> CreateProcess.ensureExitCode
              |> CreateProcess.map ignore
              |> Proc.run

              let output =
                  [ yield! [ "fake"; "--version" ] ]
                  |> runDotNetRaw
                  |> CreateProcess.withWorkingDirectory scenarioPath
                  |> CreateProcess.redirectOutput
                  |> CreateProcess.ensureExitCode
                  |> Proc.run

              let ss = Regex.Match(output.Result.Error, "FAKE [0-9]+ \- F# Make *")
              ss.Success |> Expect.isTrue "Expected version in stderror string"

              Expect.stringContains
                  "Expected Fake.Runtime.dll in stderror string"
                  "Fake.Runtime.dll"
                  output.Result.Error

          testCase "no dependencies hello world and casing #2314"
          <| fun _ ->
              let result =
                  if Paket.Utils.isWindows then
                      // #2314
                      fakeRunAndCheck Ctx.Default "HELLO_world.fsx" "HELLO_world.fsx" "core-no-dependencies-hello-world"
                      |> ignProc

                      directFake "--silent run hello_world.fsx" "core-no-dependencies-hello-world"
                  else
                      fakeRunAndCheck Ctx.Default "hello_world.fsx" "hello_world.fsx" "core-no-dependencies-hello-world"

              let stdOut = String.Join("\n", result.Messages)
              let stdErr = String.Join("\n", result.Errors)

              stdOut.Trim() |> Expect.stringContains "Hello FAKE expected" "Hello FAKE"
              stdErr.Trim() |> Expect.equal "empty expected" ""

          testCase "simple failed to compile"
          <| fun _ ->
              let result =
                  expectFailure "Expected an compilation error and a nonzero exit code!" (fun ctx ->
                      fakeRunAndCheck ctx "fail-to-compile.fsx" "fail-to-compile.fsx" "core-simple-failed-to-compile"
                      |> ignProc)

              let stdOut = String.Join("\n", result.Messages)
              let stdErr = String.Join("\n", result.Errors)

              stdErr.Contains("klajsdhgfasjkhd")
              |> Expect.isTrue (
                  sprintf
                      "Standard Error Output should contain 'klajsdhgfasjkhd', but was: '%s', Out: '%s'"
                      stdErr
                      stdOut
              )

              checkIntellisense "fail-to-compile.fsx" "core-simple-failed-to-compile"

          testCase "simple runtime error"
          <| fun _ ->
              let result =
                  expectFailure "Expected an runtime error and a nonzero exit code!" (fun ctx ->
                      fakeRunAndCheck ctx "runtime-error.fsx" "runtime-error.fsx" "core-simple-runtime-error"
                      |> ignProc)

              let stdOut = String.Join("\n", result.Messages)
              let stdErr = String.Join("\n", result.Errors)

              stdErr.Contains("runtime error")
              |> Expect.isTrue (
                  sprintf "Standard Error Output should contain 'runtime error', but was: '%s', Out: '%s'" stdErr stdOut
              )

              checkIntellisense "runtime-error.fsx" "core-simple-runtime-error"

          testCase "reference fake runtime"
          <| fun _ ->
              handleAndFormat
              <| fun ctx ->
                  fakeRunAndCheck
                      ctx
                      "reference_fake-runtime.fsx"
                      "reference_fake-runtime.fsx"
                      "core-reference-fake-runtime"
                  |> ignProc

          testCase "context exists"
          <| fun _ ->
              handleAndFormat
              <| fun ctx ->
                  fakeRunAndCheck ctx "context.exists.fsx" "context.exists.fsx" "core-context-exists"
                  |> ignProc

          testCase "use external paket.dependencies"
          <| fun _ ->
              handleAndFormat
              <| fun ctx ->
                  fakeRunAndCheck
                      ctx
                      "use_external_dependencies.fsx"
                      "use_external_dependencies.fsx"
                      "core-use-external-paket-dependencies"
                  |> ignProc

          testCase "reference fake core targets"
          <| fun _ ->
              let result =
                  handleAndFormat
                  <| fun ctx ->
                      fakeRunAndCheck
                          ctx
                          "reference_fake-targets.fsx"
                          "reference_fake-targets.fsx --test"
                          "core-reference-fake-core-targets"

              let stdOut = String.Join("\n", result.Messages).Trim()
              let stdErr = String.Join("\n", result.Errors)
              printfn "*****************************************************************"
              printfn "%s" stdOut
              printfn "*****************************************************************"
              printfn "%s" stdErr
              printfn "*****************************************************************"

              let expected = "Arguments: [\"--test\"]"

              stdOut.Contains expected
              |> Expect.isTrue (sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)

              let expected = "GlobalArgs: [|\"--test\"|]"

              stdOut.Contains expected
              |> Expect.isTrue (sprintf "stdout should contain '%s', but was: '%s'" expected stdOut)
              // Depending on the CI this string should be in the standard-output or the standard-error
              let expected =
                  if CoreTracing.importantMessagesToStdErr then
                      "Some Info from FAKE"
                  else
                      ""

              stdErr.Trim() |> Expect.stringContains "expected correct stderr" expected

              // Check if --write-info <file> works
              let tempFile = Path.GetTempFileName()

              try
                  let tmpPath = scenarioTempPath "core-reference-fake-core-targets"
                  let scriptFile = Path.Combine(tmpPath, "reference_fake-targets.fsx")
                  let otherScriptFile = Path.Combine(tmpPath, "otherscript.fsx")
                  let otherFileFile = Path.Combine(tmpPath, "otherfile.fs")

                  handleAndFormat
                  <| fun _ctx ->
                      directFake
                          (sprintf
                              "run --fsiargs \"--debug:portable --optimize-\" reference_fake-targets.fsx -- --write-info \"%s\""
                              tempFile)
                          "core-reference-fake-core-targets"
                      |> ignProc

                  let json = File.ReadAllText tempFile
                  let obj = JsonDocument.Parse(json)
                  let targets = obj.RootElement.GetProperty("targets")

                  let parseDecl (t: JsonElement) =
                      { File = string (t.GetProperty("file"))
                        Line = t.GetProperty("line").GetInt32()
                        Column = t.GetProperty("column").GetInt32()
                        ErrorDetail = string (t.GetProperty("errorDetail")) }

                  let parseDep (t: JsonElement) =
                      { Name = string (t.GetProperty("name"))
                        Declaration = parseDecl (t.GetProperty("declaration")) }

                  let parseArray parseItem (a: JsonElement) =
                      a.EnumerateArray() |> Seq.map parseItem |> Seq.toList

                  let parseTarget (t: JsonElement) =
                      { Name = string (t.GetProperty("name"))
                        Declaration = parseDecl (t.GetProperty("declaration"))
                        HardDependencies = parseArray parseDep (t.GetProperty("hardDependencies"))
                        SoftDependencies = parseArray parseDep (t.GetProperty("softDependencies"))
                        Description = string (t.GetProperty("description")) }

                  let dict =
                      targets.EnumerateArray()
                      |> Seq.map (fun t -> let t = parseTarget t in t.Name, t)
                      |> dict

                  Expect.equal "Expected correct number of targets" 4 dict.Count

                  let startTarget = dict["Start"]

                  Expect.equal
                      "Expected correct declaration of 'Start'"
                      { Declaration.Empty with
                          File = scriptFile
                          Line = 37 }
                      startTarget.Declaration

                  Expect.equal "Expected correct hard dependencies of 'Start'" [] startTarget.HardDependencies
                  Expect.equal "Expected correct soft dependencies of 'Start'" [] startTarget.SoftDependencies
                  Expect.equal "Expected correct description of 'Start'" "Test description" startTarget.Description
                  let testTarget = dict["TestTarget"]

                  Expect.equal
                      "Expected correct declaration of 'TestTarget'"
                      { Declaration.Empty with
                          File = scriptFile
                          Line = 39 }
                      testTarget.Declaration

                  Expect.equal
                      "Expected correct hard dependencies of 'TestTarget'"
                      [ { Name = "Start"
                          Declaration =
                            { Declaration.Empty with
                                File = scriptFile
                                Line = 46 } } ]
                      testTarget.HardDependencies

                  Expect.equal "Expected correct description of 'TestTarget'" "" testTarget.Description
                  let scriptTarget = dict["OtherScriptTarget"]

                  Expect.equal
                      "Expected correct declaration of 'OtherScriptTarget'"
                      { Declaration.Empty with
                          File = otherScriptFile
                          Line = 4 }
                      scriptTarget.Declaration

                  Expect.equal
                      "Expected correct hard dependencies of 'OtherScriptTarget'"
                      []
                      scriptTarget.HardDependencies

                  Expect.equal "Expected correct description of 'OtherScriptTarget'" "" scriptTarget.Description
                  let fileTarget = dict["OtherFileTarget"]

                  Expect.equal
                      "Expected correct declaration of 'OtherFileTarget'"
                      { Declaration.Empty with
                          File = otherFileFile
                          Line = 7
                          Column = 5 }
                      fileTarget.Declaration

                  Expect.equal "Expected correct hard dependencies of 'OtherFileTarget'" [] fileTarget.HardDependencies
                  Expect.equal "Expected correct description of 'OtherFileTarget'" "" fileTarget.Description
              finally
                  try
                      File.Delete tempFile
                  with e ->
                      ()


          testCase "issue #2025"
          <| fun _ ->
              handleAndFormat
              <| fun ctx -> fakeRunAndCheckInPath ctx "build.fsx" "build.fsx" "i002025" "script" |> ignProc

          testCase "issue #2007 - native libs work"
          <| fun _ ->
              // should "just" work
              handleAndFormat
              <| fun ctx -> fakeRunAndCheck ctx "build.fsx" "build.fsx" "i002007-native-libs" |> ignProc

              // Should tell FAKE error story
              let result =
                  expectFailure "Expected missing entrypoint error"
                  <| fun _ctx ->
                      directFake "run build.fsx -t FailWithMissingEntry" "i002007-native-libs"
                      |> ignProc

              let stdOut = String.Join("\n", result.Messages).Trim()
              let stdErr = String.Join("\n", result.Errors)

              (stdErr.Contains "Fake_ShouldNotExistExtryPoint"
               && stdErr.Contains "EntryPointNotFoundException:")
              |> Expect.isTrue (
                  sprintf
                      "Standard Error Output should contain 'Fake_ShouldNotExistExtryPoint' and 'EntryPointNotFoundException:', but was: '%s', Out: '%s'"
                      stdErr
                      stdOut
              )

              let result =
                  expectFailure "Expected missing entrypoint error"
                  <| fun _ctx -> directFake "run build.fsx -t FailWithUnknown" "i002007-native-libs" |> ignProc

              let stdOut = String.Join("\n", result.Messages).Trim()
              let stdErr = String.Join("\n", result.Errors)

              (stdErr.Contains "unknown_dependency.dll"
               && stdErr.Contains "DllNotFoundException:")
              |> Expect.isTrue (
                  sprintf
                      "Standard Error Output should contain 'unknown_dependency.dll' and 'DllNotFoundException:', but was: '%s', Out: '%s'"
                      stdErr
                      stdOut
              )
          // TODO: enable instead of the above
          //stdErr.Contains("Could not resolve native library 'unknown_dependency.dll'")
          //    |> Expect.isTrue (sprintf "Standard Error Output should contain \"Could not resolve native library 'unknown_dependency.dll'\", but was: '%s', Out: '%s'" stdErr stdOut)
          //stdErr.Contains("This can happen for various reasons")
          //    |> Expect.isTrue (sprintf "Standard Error Output should contain \"This can happen for various reasons\", but was: '%s', Out: '%s'" stdErr stdOut)

          ]
