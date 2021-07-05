module Fake.Tools.GitVersionTests

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.Testing
open Fake.Tools
open Expecto

let rawCreateProcess setParams =
   Fake.Tools.GitVersion.createProcess (fun param ->
       { param with
           ToolPath = Path.Combine("gitversion", "GitVersion.exe")}
       |> setParams)

let runCreateProcess setParams =
  let cp = rawCreateProcess setParams

  let file, args =
    match cp.Command with
    | RawCommand(file, args) -> file, args
    | _ -> failwithf "expected RawCommand"
    |> ArgumentHelper.checkIfMono

  let expectedPath = Path.Combine("gitversion", "GitVersion.exe")
  Expect.equal file expectedPath "Expected GitVersion.exe"

  expectedPath, (RawCommand(file, args)).CommandLine.Trim()

[<Tests>]
let tests =
  testList "Fake.Tools.GitVersion.Tests" [
    testCase "Test that new argument generation  with default parameters" <| fun _ ->
      let expectedPath, commandLine =
        runCreateProcess id

      Expect.equal commandLine expectedPath "expected proper command line"
    
    testCase "Test that full Framework works" <| fun _ ->
      let path = "another/path/for/tool.exe"
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolPath = path
              ToolType = ToolType.CreateFullFramework() })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfMono

      Expect.equal file path "Expected tool.exe"
      Expect.equal ((RawCommand(file, args)).CommandLine.Trim()) path "expected proper command line"

    testCase "Test that global tool works" <| fun _ ->
      let path = "another/path/for/globaltool.exe"
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolPath = path
              ToolType = ToolType.CreateGlobalTool() })
      let file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"

      Expect.equal file path "Expected globaltool.exe"
      Expect.equal ((RawCommand(file, args)).CommandLine.Trim()) file "expected proper command line"

    testCase "Test that local tool override works" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType =
                ToolType.CreateLocalTool()
                |> ToolType.withDefaultToolCommandName "alternative" })
      let dotnet, file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfDotNet

      Expect.equal file "alternative" "Expected alternative"
      Expect.equal ((RawCommand(file, args)).CommandLine.Trim()) file "expected proper command line"

    testCase "Test that local tool works" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType = ToolType.CreateLocalTool () })
      let dotnet, file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfDotNet

      Expect.equal file "gitversion" "Expected gitversion argument"
      Expect.equal (RawCommand(file, args)).CommandLine "gitversion " "expected proper command line"

    testCase "Test that DotNet can override dotnet" <| fun _ ->
      let cp =
        rawCreateProcess (fun p ->
          { p with
              ToolType =
                ToolType.CreateLocalTool()
                |> ToolType.withDotNetOptions (fun o -> {o with DotNetCliPath = "some/dotnet/path/dotnet.exe"}) })
      let dotnet, file, args =
        match cp.Command with
        | RawCommand(file, args) -> file, args
        | _ -> failwithf "expected RawCommand"
        |> ArgumentHelper.checkIfDotNet

      Expect.equal dotnet "some/dotnet/path/dotnet.exe" "Expected dotnet path"
      Expect.equal (RawCommand(file, args)).CommandLine "gitversion " "expected proper command line"

  ]

