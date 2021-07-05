module Fake.DotNet.NuGetTests

open Fake.Core
open Fake.DotNet.NuGet
open Fake.DotNet.NuGet.Version
open Expecto

[<Tests>]
let tests =
    testList "Fake.DotNet.NuGet.Tests" [
        testCase "Test that the default nuget push arguments returns empty string" <| fun _ ->
            let cli =
                NuGet.NuGetPushParams.Create() 
                |> NuGet.toPushCliArgs
                |> Args.toWindowsCommandLine
                  
            Expect.isEmpty cli "Empty push args."
          
        testCase "Test that the nuget push arguments with all params set returns correct string" <| fun _ ->
            let param =
                { NuGet.NuGetPushParams.Create() with
                    DisableBuffering = true
                    ApiKey = Some "abc123"
                    NoSymbols = true
                    NoServiceEndpoint = true
                    Source = Some "MyNuGetSource"
                    SymbolApiKey = Some "MySymbolApiKey"
                    SymbolSource = Some "MySymbolSource"
                    Timeout = Some <| System.TimeSpan.FromMinutes 6.00001
                    PushTrials = 5 }
          
            let cli =
                param
                |> NuGet.toPushCliArgs
                |> Args.toWindowsCommandLine
    
            let expected = "-ApiKey abc123 -DisableBuffering -NoSymbols -NoServiceEndpoint -Source MyNuGetSource -SymbolApiKey MySymbolApiKey -SymbolSource MySymbolSource -Timeout 360"
                
            Expect.equal cli expected "Push args generated correctly."
        
        test "Incrementing Patch for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.0") |> IncPatch |> string) "1.1.1" "Incremented Patch from 1.1.0 should be 1.1.1"
        }

        test "Incrementing Minor for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.1") |> IncMinor |> string) "1.2.0" "Incremented Minor from 1.1.1 should be 1.2.0"
        }

        test "Incrementing Major for a SemVerInfo" {
            Expect.equal (SemVer.parse("1.1.1") |> IncMajor |> string) "2.0.0" "Incremented Patch from 1.1.1 should be 2.0.0"
        }

        test "Getting the NuGet feed URL return V3" {
            Expect.equal NuGet.galleryV3 "https://api.nuget.org/v3/index.json" "NuGet feed V3 API is used"
        }

        test "Getting NuGet resource service URL" {
            Expect.isNotEmpty (NuGet.getRepoUrl()) "Get Repo URL will return NuGet service URL"
        }

        testCase "Getting latest version of FAKE from NuGet feed" <| fun _ ->
            let package: NuGet.NugetPackageInfo = NuGet.getLatestPackage (NuGet.getRepoUrl()) "FAKE"
            Expect.isTrue package.IsLatestVersion "getting latest version of a package"

        testCase "Getting latest version of FAKE and package info are populated" <| fun _ ->
            let package: NuGet.NugetPackageInfo = NuGet.getLatestPackage (NuGet.getRepoUrl()) "FAKE"
            Expect.equal package.Id "FAKE" "Id filled"
            Expect.isNotEmpty package.Version "Version filled"
            Expect.isNotEmpty package.Description "Description filled"
            Expect.isNotEmpty package.Summary "Id filled"
            Expect.isTrue package.IsLatestVersion "IsLatestVersion filled"
            Expect.isNotEmpty package.Authors "Authors filled"
            Expect.isNotEmpty package.Owners "Owners filled"
            Expect.isNotEmpty package.Tags "Tags filled"
            Expect.isNotEmpty package.ProjectUrl "ProjectUrl filled"
            Expect.isNotEmpty package.LicenseUrl "LicenseUrl filled"
            Expect.equal package.Title "FAKE" "Title filled"
        
        testCase "Requesting specific version of FAKE" <| fun _ ->
            let package: NuGet.NugetPackageInfo = NuGet.getPackage (NuGet.getRepoUrl()) "FAKE" "1.46.2"
            Expect.isFalse package.IsLatestVersion "getting version 1.46.2 of FAKE"

        testCase "Requesting un-registered version of FAKE throws exception" <| fun _ ->
            Expect.throws (fun _ -> NuGet.getPackage (NuGet.getRepoUrl()) "FAKE" "-1.55.0" |> ignore) "Version -1.55.0 is not registered for FAKE!"

        testCase "Requesting specific version of FAKE and package info are populated" <| fun _ ->
            let package: NuGet.NugetPackageInfo = NuGet.getPackage (NuGet.getRepoUrl()) "FAKE" "1.46.2"
            Expect.equal package.Id "FAKE" "Id filled"
            Expect.equal package.Version "1.46.2" "Version filled"
            Expect.isNotEmpty package.Description "Description filled"
            Expect.isNotEmpty package.Summary "Id filled"
            Expect.isFalse package.IsLatestVersion "IsLatestVersion filled"
            Expect.isNotEmpty package.Authors "Authors filled"
            Expect.isNotEmpty package.Owners "Owners filled"
            Expect.isNotEmpty package.Tags "Tags filled"
            Expect.isNotEmpty package.ProjectUrl "ProjectUrl filled"
            Expect.isNotEmpty package.LicenseUrl "LicenseUrl filled"
            Expect.equal package.Title "FAKE" "Title filled"
        
        testCase "Search by title returns results for matching packages by provided title" <| fun _ ->
            let packages: NuGet.NugetPackageInfo list = NuGet.searchByTitle (NuGet.getRepoUrl()) "FAKE"
            Expect.isGreaterThanOrEqual packages.Length 1 "Expected result has at least one element"
            Expect.equal packages.[0].Id "FAKE" "Id filled"
            Expect.isNotEmpty packages.[0].Version "Version filled"
            Expect.isNotEmpty packages.[0].Description "Description filled"
            Expect.isNotEmpty packages.[0].Summary "Id filled"
            Expect.isFalse packages.[0].IsLatestVersion "IsLatestVersion filled"
            Expect.isNotEmpty packages.[0].Authors "Authors filled"
            Expect.isNotEmpty packages.[0].Owners "Owners filled"
            Expect.isNotEmpty packages.[0].Tags "Tags filled"
            Expect.isNotEmpty packages.[0].ProjectUrl "ProjectUrl filled"
            Expect.isNotEmpty packages.[0].LicenseUrl "LicenseUrl filled"
            Expect.equal packages.[0].Title "FAKE" "Title filled"
    ]

