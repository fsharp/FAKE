module Fake.Testing.FixieTests

open Fake.Testing
open Expecto

[<Tests>]
let tests =
  testList "Fake.Testing.Fixie.Tests" [
    testCase "Test it prepares Fixie call parameters" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = "Debug"
        NoBuild = true
        Framework = "netcoreapp3.1"
        Report = "path/to/report"
        CustomArguments = ["custom1", "1"; "custom2", "2"] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "--configuration Debug --no-build --framework netcoreapp3.1 --report \"path/to/report\" -- --custom1 1 --custom2 2" "Expected proper options for fixie"

    testCase "Test it appends no arguments when arguments are used with default values" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = ""
        NoBuild = false
        Framework = ""
        Report = ""
        CustomArguments = [] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "" "Expected proper options for fixie"

    testCase "Test it appends custom arguments when provided only" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = ""
        NoBuild = false
        Framework = ""
        Report = ""
        CustomArguments = ["custom1", "1"; "custom2", "2"] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "-- --custom1 1 --custom2 2" "Expected proper options for fixie"

    testCase "Test it appends configuration only when specified" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = "Release"
        NoBuild = false
        Framework = ""
        Report = ""
        CustomArguments = [] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "--configuration Release" "Expected proper options for fixie"

    testCase "Test it appends no build only when specified" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = ""
        NoBuild = true
        Framework = ""
        Report = ""
        CustomArguments = [] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "--no-build" "Expected proper options for fixie"

    testCase "Test it appends framework only when specified" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = ""
        NoBuild = false
        Framework = "netcoreapp3.1"
        Report = ""
        CustomArguments = [] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "--framework netcoreapp3.1" "Expected proper options for fixie"
    
    testCase "Test it appends report only when specified" <| fun _ ->
      let fixieArgs: Fixie.FixieArgs = {
        Configuration = ""
        NoBuild = false
        Framework = ""
        Report = "to/to/report"
        CustomArguments = [] }

      let cmd = Fixie.formatFixieArguments fixieArgs
      
      Expect.equal cmd "--report \"to/to/report\"" "Expected proper options for fixie"
  ]
