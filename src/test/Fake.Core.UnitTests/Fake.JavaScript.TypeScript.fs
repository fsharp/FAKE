module Fake.JavaScript.TypeScriptTests

open Fake.Core
open Fake.JavaScript
open Expecto
open System

[<Tests>]
let tests =
  testList "Fake.JavaScript.TypeScript.Tests" [
    testCase "Test all arguments are mapped correctly" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = Some("index.js")
          EmitDeclaration = true
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = true
          NoLib = true
          RemoveComments = true
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outFile index.js  --outDir \"./TestFiles/\"  --declaration  --module ES2020  --sourceMap  --noLib  --removeComments   \"index.ts\" " "expected proper arguments formatting"

    testCase "Test setting output signe file to None will drop --outFile option from command" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = None
          EmitDeclaration = true
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = true
          NoLib = true
          RemoveComments = true
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outDir \"./TestFiles/\"  --declaration  --module ES2020  --sourceMap  --noLib  --removeComments   \"index.ts\" " "expected proper arguments formatting"

    testCase "Test setting emit declaration to false will drop --declaration option from command" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = Some("index.js")
          EmitDeclaration = false
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = true
          NoLib = true
          RemoveComments = true
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outFile index.js  --outDir \"./TestFiles/\"  --module ES2020  --sourceMap  --noLib  --removeComments   \"index.ts\" " "expected proper arguments formatting"

    testCase "Test setting emit source maps to false will drop --sourceMap option from command" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = Some("index.js")
          EmitDeclaration = true
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = false
          NoLib = true
          RemoveComments = true
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outFile index.js  --outDir \"./TestFiles/\"  --declaration  --module ES2020  --noLib  --removeComments   \"index.ts\" " "expected proper arguments formatting"

    testCase "Test setting no libs to false will drop --noLib option from command" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = Some("index.js")
          EmitDeclaration = true
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = true
          NoLib = false
          RemoveComments = true
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outFile index.js  --outDir \"./TestFiles/\"  --declaration  --module ES2020  --sourceMap  --removeComments   \"index.ts\" " "expected proper arguments formatting"

    testCase "Test setting remove comments to false will drop --removeComments option from command" <| fun _ ->
      let args: TypeScript.TypeScriptParams = 
        { ECMAScript = TypeScript.ECMAScript.ES2020
          OutputSingleFile = Some("index.js")
          EmitDeclaration = true
          ModuleGeneration = TypeScript.ModuleGeneration.ES2020
          EmitSourceMaps = true
          NoLib = true
          RemoveComments = false
          ToolPath = "tsc.exe"
          OutputPath = "./TestFiles/"
          TimeOut = TimeSpan.FromMinutes 5. }

      let cmd = TypeScript.buildArguments args "index.ts"

      Expect.equal cmd " --target ES2020  --outFile index.js  --outDir \"./TestFiles/\"  --declaration  --module ES2020  --sourceMap  --noLib   \"index.ts\" " "expected proper arguments formatting"

  ]
