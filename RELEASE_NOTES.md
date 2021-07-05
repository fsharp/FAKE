# Release Notes

## 5.20.5-alpha - tbd
* tbd

## 5.20.4 - 2021-03-31

* ENHANCEMENT: Allow users of nunit3 set environment variables in the runner, thanks @robertpi - https://github.com/fsharp/FAKE/pull/2543
* BUGFIX: Fix reportgenerator docs and compilation,thanks @matthid - https://github.com/fsharp/FAKE/issues/2535
* BUGFIX: Fix Travis build, thanks @gdziadkiewicz - https://github.com/fsharp/FAKE/pull/2549
* BUGIFX: Update Paket.Core to (better) support net5.0, thanks @pchinery - https://github.com/fsharp/FAKE/pull/2556
* BUGFIX: fix build by using Suave version to 2.5.6 in build script which is compatible with netstandard 2.0, thanks @yazeedobaid https://github.com/fsharp/FAKE/pull/2574
* ENHANCEMENT: Change NuGet feed APIs to use V3 of NuGet feed APIs, thanks @yazeedobaid https://github.com/fsharp/FAKE/pull/2574

## 5.20.3 - 2020-08-05

* BUGFIX: Fix unintentional unit return, thanks @objectx - https://github.com/fsharp/FAKE/pull/2534
* BUGFIX: Add null check for values with null as default value, thanks @aklefdal - https://github.com/fsharp/FAKE/pull/2531
* BUGFIX: Small Copy&Paste issue, thanks @vilinski - https://github.com/fsharp/FAKE/pull/2537
* BUGFIX: Ignore TraceSecret.register call if secret is `null` or empty and throw if replacement is `null`

## 5.20.2 - 2020-06-27

* ENHANCEMENT: Update bundled paket
* ENHANCEMENT: Add seq overload for `Docopt.Parse`, thanks @rmunn - https://github.com/fsharp/FAKE/pull/2525
* BUGFIX: Fixed increments in SemVerInfo, thanks @MecuSorin - https://github.com/fsharp/FAKE/pull/2523
* BUGFIX: Fix secrets leakage in certain scenarios (NuGet key, Paket key), thanks @zvirja - https://github.com/fsharp/FAKE/pull/2527

## 5.20.1 - 2020-06-13

* ENHANCEMENT: add basic support for GitHub Actions, thanks @Bomret - https://github.com/fsharp/FAKE/pull/2512
* BUGFIX: Change internal standard error and standard input redirection and support UTF8, fixes https://github.com/fsharp/FAKE/issues/2503 if you have problems try setting `FAKE_DISABLE_UTF8`
* DOCS: Update documentation following "bug" #2486, thanks @DavidSSL - https://github.com/fsharp/FAKE/pull/2520

## 5.20.0 - 2020-05-05

* (Minor) BREAKING: Drop support for `net462` and update to `net472`.
* ENHANCEMENT: Keep unreleased changelog section when promote to new version, thanks @vilinski - https://github.com/fsharp/FAKE/pull/2480
* ENHANCEMENT: Added SignTool for v5, thanks @jhromadik - https://github.com/fsharp/FAKE/pull/2444
* ENHANCEMENT: Remove old netstandard1.6 dependencies, thanks @teo-tsirpanis - https://github.com/fsharp/FAKE/pull/2493
* ENHANCEMENT: Add support for running Octo as dotnet tool, thanks @jeremyabbott - https://github.com/fsharp/FAKE/pull/2489
* ENHANCEMENT: Add support for `--include-symbols` in `dotnet pack`, thanks @devployment - https://github.com/fsharp/FAKE/pull/2508
* ENHANCEMENT: Add support for default proxy credentials to GitHub (Octokit), thanks @csmager - https://github.com/fsharp/FAKE/pull/2507
* ENHANCEMENT: `Fake.DotNet.FSFormatting` supports the 4.0 RC release
* ENHANCEMENT: Update paket to support `netcoreapp5.0`, thanks @TheAngryByrd, @fc1943s - https://github.com/fsharp/FAKE/issues/2496
* BUGFIX: Update FCS, fixes ionide FAKE support (https://github.com/fsharp/FsAutoComplete/issues/561), thanks @baronfel - https://github.com/fsharp/FAKE/pull/2475, https://github.com/fsharp/FAKE/pull/2479, https://github.com/fsharp/FAKE/pull/2481, https://github.com/fsharp/FAKE/pull/2500
* BUGFIX: Fix Paket.restore references-files, thanks @nilshelmig - https://github.com/fsharp/FAKE/pull/2474
* BUGFIX: Fix/octo args to string bug, thanks @ids-pfinn - https://github.com/fsharp/FAKE/pull/2469
* BUGFIX: Fix an AppVeyor environment variable (`APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED`) returning `null`, thanks @teo-tsirpanis - https://github.com/fsharp/FAKE/pull/2448
* BUGFIX: Fix potential `FileNotFoundException` when cache is outdated.
* DOCS: Fixed typos and improved docs, thanks @milbrandt, @jzabroski, @objectx - https://github.com/fsharp/FAKE/pull/2492, https://github.com/fsharp/FAKE/pull/2497, https://github.com/fsharp/FAKE/pull/2502

## 5.19.1 - 2020-02-10

* ANNOUNCEMENT: `dotnet-fake` cli tool via `DotNetCliToolReference` is now history - https://github.com/fsharp/FAKE/issues/2465
* ENHANCEMENT: Update MSTest.fs for VS 2019, thanks @0x53A - https://github.com/fsharp/FAKE/pull/2450
* ENHANCEMENT: Added `CreateProcess.addOnStartedEx` in order to retrieve the .NET Process object, thanks @maciej-izak - https://github.com/fsharp/FAKE/pull/2451
* BUGFIX: Improved error reporting when paket initialization fails, see https://github.com/isaacabraham/vsts-fsharp/issues/33
* DOCS: Fixed typos and improved docs, thanks @ncave, @rmunn, @jeremyabbott, @mjul, @smoothdeveloper - https://github.com/fsharp/FAKE/pull/2449 https://github.com/fsharp/FAKE/pull/2452 https://github.com/fsharp/FAKE/pull/2454 https://github.com/fsharp/FAKE/pull/2459 https://github.com/fsharp/FAKE/pull/2460
* INFRASTRUCTURE: Use stable version of reference assemblies package, thanks @teo-tsirpanis - https://github.com/fsharp/FAKE/pull/2463

## 5.19.0 - 2019-12-19

* NEW: Added `Fake.Sql.SqlPackage` which is a redesign or `Fake.Sql.DacPac` and added support for publish profiles, thanks @kmadof - https://github.com/fsharp/FAKE/pull/2366
* ENHANCEMENT: `Fake.DotNet.Cli` Add timeout field to kill the process after the given timeout - https://github.com/fsharp/FAKE/pull/2425
* ENHANCEMENT: `Fake.Core.Target` Add notification when a fake worker is idle - https://github.com/fsharp/FAKE/pull/2425
* ENHANCEMENT: Use SPDX license expressions in the nuspec files, thanks @teo-tsirpanis - https://github.com/fsharp/FAKE/pull/2403
* ENHANCEMENT: `fake-cli` Update bundled paket - https://github.com/fsharp/FAKE/pull/2425
* ENHANCEMENT: `Fake.DotNet.Cli` Add support for suppressing copyright messages (`--nologo`) when invoking `dotnet`, thanks @objectx - https://github.com/fsharp/FAKE/pull/2436
* ENHANCEMENT: `Fake.Installer.Squirrel` Add additional Squirrel parameters, thanks @pchinery - https://github.com/fsharp/FAKE/pull/2431
* ENHANCEMENT: `Fake.Testing.SonarQube` Several improvements like workdir and local tool support, thanks @vilinski - https://github.com/fsharp/FAKE/pull/2438
* BUGFIX: `Fake.Tools.Rsync` Fix typo in Rsync.fs options, thanks @rmunn - https://github.com/fsharp/FAKE/pull/2432
* BUGFIX: `Fake.DotNet.Testing.Coverlet` Fix Coverlet output property name, thanks @SteveGilham - https://github.com/fsharp/FAKE/pull/2427
* BUGFIX: `Fake.Tools.Octo` Octopus deploy trace always marked failures, thanks @ids-pfinn - https://github.com/fsharp/FAKE/pull/2428
* BUGFIX: `Fake.Core.Process` Process redirection was not working as advertised, thanks @ZeekoZhu - https://github.com/fsharp/FAKE/issues/2445
* DOCS: Change the README badges and their placement, thanks @teo-tsirpanis - https://github.com/fsharp/FAKE/pull/2437
* DOCS: Several documentation improvements, thanks to @jhromadik - https://github.com/fsharp/FAKE/pull/2441 https://github.com/fsharp/FAKE/pull/2442 https://github.com/fsharp/FAKE/pull/2443

## 5.18.3 - 2019-11-04

* BUGFIX: `Fake.DotNet.Testing.Coverlet` was not working, thanks @Tarmil - https://github.com/fsharp/FAKE/pull/2424

## 5.18.2 - 2019-10-26

* NEW: Add `Fake.DotNet.Testing.Coverlet`, thanks @Tarmil - https://github.com/fsharp/FAKE/pull/2413
* BUGFIX: `paket pack` module was broken, thanks @sergey-tihon - https://github.com/fsharp/FAKE/pull/2418

## 5.18.1 - 2019-10-22

* BUGFIX: Paket module was broken - https://github.com/fsharp/FAKE/pull/2413
* BUGFIX: New `CreateProcess.withToolType` wasn't working for `ToolType.FrameworkDependentDeployment` - https://github.com/fsharp/FAKE/issues/2412
* ENHANCEMENT: Add support for local dotnet tool to fake-template and make it the default.

## 5.18.0 - 2019-10-21

* ENHANCEMENT: Add core support for local tools via `CreateProcess.withToolType`, this helper is part of `Fake.DotNet.Cli` and available after `open Fake.Core` - https://github.com/fsharp/FAKE/pull/2399
* ENHANCEMENT: Add `ToolType` support in `ReportGenerator` and `Paket`, thanks @SteveGilham and @Tarmil - https://github.com/fsharp/FAKE/pull/2399
* ENHANCEMENT: Add `FAKE_COREFX_VERBOSE` in order to increase verbosity for the FAKE libraries
* ENHANCEMENT: Add `DotNet.setupEnv` in order to improve working with installed SDKs (for example when calling fable), fixes https://github.com/fsharp/FAKE/issues/2405
* BUGFIX: Address https://github.com/fsharp/FAKE/issues/2406 by only printing a warning instead of crashing
* BUGFIX: Partially address https://github.com/fsharp/FAKE/issues/2401 by allowing the user to continue and request additional info.
* BUGFIX: Fix "FindLocalTools fails to search Paket.exe in Subdirectory" - https://github.com/fsharp/FAKE/issues/2390

## 5.17.0 - 2019-10-10

* DOCS: Remove Issue Stats, thanks @vbfox - https://github.com/fsharp/FAKE/pull/2393
* ENHANCEMENT: Support VSTest parallel test execution setting, thanks @dungpa - https://github.com/fsharp/FAKE/pull/2385
* ENHANCEMENT: Update dotnet new template, thanks @Krzysztof-Cieslak - https://github.com/fsharp/FAKE/pull/2391
* BUGFIX: Fix API for DotCover.report, thanks @TomasMorton - https://github.com/fsharp/FAKE/pull/2389
* ENHANCEMENT: Add installFrozenLockFile option for Yarn, thanks @rfrerebe - https://github.com/fsharp/FAKE/pull/2388
* ENHANCEMENT: Add the extra OpenCover registration options, thanks @SteveGilham - https://github.com/fsharp/FAKE/pull/2387
* ENHANCEMENT: Allow FSI evaluation option for FSharp.Formatting, thanks @kurtschelfthout - https://github.com/fsharp/FAKE/pull/2158
* BUGFIX: Fixed DocFx bug, thanks @DigitalFlow - https://github.com/fsharp/FAKE/pull/2188
* ENHANCEMENT: Add support for contentFiles to Fake.DotNet.NuGet packaging, thanks @chappoo - https://github.com/fsharp/FAKE/pull/2165
* ENHANCEMENT: Support mono in Fake.DotNet.Fsc, thanks @wallymathieu - https://github.com/fsharp/FAKE/pull/2397

## 5.16.1 - 2019-08-25

* BUGFIX: Fix that `generate_load_scripts` prevents restore after update - https://github.com/fsharp/FAKE/issues/2382
* BUGFIX: Fix FAKE unable to load assemblies in some scenarios - https://github.com/fsharp/FAKE/issues/2381
* BUGFIX: Fix File.getVersion fails if version is not set, thanks @SCullman - https://github.com/fsharp/FAKE/issues/2378
* ENHANCEMENT: make `Fake.DotNet.Paket` work with the dotnet tool based version of Paket, thanks @seanamosw - https://github.com/fsharp/FAKE/pull/2364
* ENHANCEMENT: add `Organization` field to `Fake.Testing.SonarQube`, thanks @Lutando - https://github.com/fsharp/FAKE/pull/2358
* ENHANCEMENT: Added `Arguments.appendRaw*` functions to handle weird microsoft escaping.
* ENHANCEMENT: Added `Environment.getNuGetPackagesCacheFolder()`, returns the NuGet packages path.
* ENHANCEMENT: Added `ProcessUtils.tryFindLocalTool` to resolve tools via a common logic (`Fake.IO.Globbing.Tools` is now obsolete)
* DOCS: Fix some broken links - https://github.com/fsharp/FAKE/issues/2351

## 5.16.0 - 2019-08-17

* LEGACY: Remove `Fake.Deploy` from repository and NuGet package, see https://github.com/fsharp/FAKE/issues/1820
* LEGACY: Update to `net461` to support latest `FSharp.Compiler.Service` to fix https://github.com/fsharp/FAKE/issues/2362
* LEGACY: Release a new version of `FakeLib.dll` (the original FAKE NuGet package) 
* BUGFIX: Fake.Api.Slack uses `Username` not `From`, thanks @mastion - https://github.com/fsharp/FAKE/pull/2360
* ENHANCEMENT: add rollforward policy to next-major to make `fake-cli` work in future dotnet sdk  major version, thanks @baronfel - https://github.com/fsharp/FAKE/pull/2372
* ENHANCEMENT: `ProcessUtils` now considers `PATHEXT` on windows - https://github.com/fsharp/FAKE/pull/2368
* ENHANCEMENT: [`Fake.Api.GitHub`] Added `TargetCommitish` parameter to the `CreateReleaseParams` record. This parameter is passed to Octokit's 'NewRelease', and allows for the creation of releases from arbitrary commits when the release tag does not exist yet, thanks @nikolamilekic - https://github.com/fsharp/FAKE/pull/2367
* (Minor) BREAKING: Drop support for `netstandard1.6` and `net46` (please open an issue if that actually hits you). All fake modules are now compiled for `netstandard2.0`, `net462` and `FSharp.Core 4.7`, you should consider to upgrade your runtime.
* ENHANCEMENT: `Fake.DotNet.Fsc` and `Fake.DotNet.Fsi` now use latest `FSharp.Compiler.Service`
* ENHANCEMENT: The fake runtime now supports `FSharp.Core 4.7`, thanks @drssoccer55 - https://github.com/fsharp/FAKE/pull/2373

## 5.15.4 - 2019-07-16

* BUGFIX: Fix high memory use and slowness with a large number of targets/dependencies - https://github.com/fsharp/FAKE/pull/2354

## 5.15.3 - 2019-07-03

* BUGFIX: Disable fast restore for MSBuild version < 15.8 - https://github.com/fsprojects/Paket/pull/3611

## 5.15.2 - 2019-07-03

* BUGFIX: Fast Restore (bugfix from paket) - https://github.com/fsprojects/Paket/pull/3608

## 5.15.1 - 2019-06-30

* ENHANCEMENT: Some internal improvements in `Fake.Runtime` for Ionide - https://github.com/fsharp/FAKE/pull/2341
* ENHANCEMENT: Add new `Target.getArguments()` function in order to retrieve arguments globally - https://github.com/fsharp/FAKE/pull/2341
* BUGFIX: Make sure to detect proper code locations when using `#load` (Ionide tooling, `Fake.Core.Target`) - https://github.com/fsharp/FAKE/pull/2341
* DOCS: Consolidate getting started, recommend Ionide, add FAQ section to menu and some feature updates - https://github.com/fsharp/FAKE/pull/2341

## 5.15.0 - 2019-06-17

* ENHANCEMENT: Add `--write-info <file>` command line to `Fake.Core.Target` in order to support tooling in Ionide see https://github.com/ionide/ionide-vscode-fsharp/pull/1137 - https://github.com/fsharp/FAKE/pull/2335
* BUGFIX: Fake 5 runner now recompiles the script when `--fsiargs` have changed (invalidates the cache) - https://github.com/fsharp/FAKE/pull/2335

## 5.14.1 - 2019-06-13

* BUGFIX: Fake 5 native libraries support now works on unix - https://github.com/fsharp/FAKE/pull/2334

## 5.14.0 - 2019-06-12

* BREAKING: Renamed `CliVersion.Lkg` to `CliVersion.Coherent` as it was renamed on the installer. If you use an old installer with this flag you can still use `CliVersion.Version "lkg"` - https://github.com/fsharp/FAKE/pull/2318
* BREAKING: Renamed `NuGet.feedUrl` to `NuGet.galleryV1` and marked it as obsolete. Added `galleryV2` and `galleryV3` - https://github.com/fsharp/FAKE/issues/2323
* ENHANCEMENT: Fake 5 now supports native libraries (like SQL and SQLite NuGet packages) - https://github.com/fsharp/FAKE/issues/2007
* BUGFIX: DotNet.exec (dotnet installer) fails when username has a blank - https://github.com/fsharp/FAKE/issues/2319
* BUGFIX: Error when invoking fake run with different casings - https://github.com/fsharp/FAKE/issues/2314
* BUGFIX: Potential fix for incorrect console colors after running fake - https://github.com/fsharp/FAKE/issues/2173
* DOCS: Document difference between `@@` and `</>` as well as `Path.combineTrimEnd` and `Path.combine` - https://github.com/fsharp/FAKE/issues/2329

## 5.13.7 - 2019-05-11

* BUGFIX: Xamarin Android x86_64 build fails - https://github.com/fsharp/FAKE/issues/2313

## 5.13.6 - 2019-05-11

* BUGFIX: update dependencies (Paket.Core update to 5.206)

## 5.13.5 - 2019-05-01

* BUGFIX: `Shell.mv` now has the correct documented behavior (5.13.3 was still broken) - https://github.com/fsharp/FAKE/pull/2309
* BUGFIX: `Shell.rename` now works for files and directories as documented - https://github.com/fsharp/FAKE/pull/2309

## 5.13.3 - 2019-04-29

* ENHANCEMENT: Use NuGet Libraries for `getLastNuGetVersion` - see https://github.com/NuGet/NuGetGallery/issues/7085
* ENHANCEMENT: TeamCitySummary, added in ReportGenerator 4.1.3 Fixes #2300 - https://github.com/fsharp/FAKE/pull/2301
* BUGFIX: `Shell.mv` now has the correct documented behavior, consistent with parameter names - https://github.com/fsharp/FAKE/issues/2293
* BUGFIX: NuGet Push command syntax. Fixes #2299 - https://github.com/fsharp/FAKE/pull/2304

## 5.13.2 - 2019-04-23

* ENHANCEMENT: Update .NET Core runtime
* BUGFIX: Do not print dotnet --version errors - https://github.com/fsharp/FAKE/issues/2295

## 5.13.1 - 2019-04-22

* ENHANCEMENT: Support F# 4.6 syntax in scripts
* ENHANCEMENT: Add hint when runner is older than 6 months and warning after 12 months

## 5.13.0 - 2019-04-14

* NEW: Add dotnet nuget push command and arguments - https://github.com/fsharp/FAKE/pull/2229
* NEW: Add `Target.initEnvironment()` in order to initialize environment - https://github.com/fsharp/FAKE/issues/2283
* ENHANCEMENT: Update dependencies - https://github.com/fsharp/FAKE/pull/2286
* ENHANCEMENT: Return the latest msbuild version by default when using vswhere - https://github.com/fsharp/FAKE/pull/2287
* ENHANCEMENT: Improve help message - https://github.com/fsharp/FAKE/issues/2282
* BUGFIX: Retry GitHub asset upload on failure
* BUGFIX: No such variable `CI_BUILD_ID` - https://github.com/fsharp/FAKE/issues/2290

## 5.12.6 - 2019-03-17

* ENHANCEMENT: Add Support for MSBuild 16 / VS 2019 - https://github.com/fsharp/FAKE/pull/2274
* ENHANCEMENT: Throw more helpful exception when directory not a git repository - https://github.com/fsharp/FAKE/pull/2275
* BUGFIX: Thread safe for `Path.toRelativeFrom` - https://github.com/fsharp/FAKE/pull/2268
* DOCS: Re-Add NuGet link - https://github.com/fsharp/FAKE/issues/2278
* DOCS: Fix broken link in getting-started - https://github.com/fsharp/FAKE/issues/2276

## 5.12.4 - 2019-02-27

* Update dependencies

## 5.12.3 - 2019-02-27

* Re-release of 5.12.2 (which failed)
* ENHANCEMENT: Allow multiple FSI Defines - https://github.com/fsharp/FAKE/pull/2260
* BUGFIX: Fix TeamCity logging. - https://github.com/fsharp/FAKE/pull/2258
* TESTSUITE improvements - https://github.com/fsharp/FAKE/pull/2264

## 5.12.2 - 2019-02-26

* ENHANCEMENT: Allow multiple FSI Defines - https://github.com/fsharp/FAKE/pull/2260
* BUGFIX: Fix TeamCity logging. - https://github.com/fsharp/FAKE/pull/2258

## 5.12.1 - 2019-02-15

* ENHANCEMENT: Add dotnet cli options (--no-restore) and (--blame) - https://github.com/fsharp/FAKE/pull/2250
* ENHANCEMENT: Update FSharp.Core and Paket to latest. - https://github.com/fsharp/FAKE/pull/2253
* BUGFIX: Correct quoting used by sendPublishNamedArtifact - https://github.com/fsharp/FAKE/pull/2240
* BUGFIX: Fixed getBaseDirectoryInclude when one directory name include the oth…  - https://github.com/fsharp/FAKE/pull/2235
* DOCS: Resort TraceSecrets.register indexed parameters - https://github.com/fsharp/FAKE/pull/2254
* DOCS: fix broken links #2241 - https://github.com/fsharp/FAKE/pull/2241

## 5.12.0 - 2019-01-12

* NEW: module `Fake.DotNet.Xdt` - https://github.com/fsharp/FAKE/pull/2218
* IMPROVEMENT: DotNet take version into account when determining dotnet cli path - https://github.com/fsharp/FAKE/pull/2220
  Usages of the `DotNet` might yield different results than what you would get in the command line to ensure the given version is used.
* DOCUMENTATION: `StreamSpecification` doc improvements - https://github.com/fsharp/FAKE/pull/2222
* DOCUMENTATION: Various improvements to the contributors guide - https://github.com/fsharp/FAKE/pull/2232

## 5.11.1 - 2018-12-07

* Update dependencies

## 5.11.0 - 2018-12-03

* DOCUMENTATION: Further fixes on the website - https://github.com/fsharp/FAKE/pull/2196 & https://github.com/fsharp/FAKE/pull/2202
* DOCUMENTATION: Fix DotNet.NuGet samples - https://github.com/fsharp/FAKE/pull/2198/files
* DOCUMENTATION: Smaller fixes - https://github.com/fsharp/FAKE/pull/2211 & https://github.com/fsharp/FAKE/pull/2213 & https://github.com/fsharp/FAKE/pull/2212 &  https://github.com/fsharp/FAKE/pull/2217 & https://github.com/fsharp/FAKE/pull/2208 & https://github.com/fsharp/FAKE/pull/2209
* NEW: module `Fake.DotNet.ILMerge` - https://github.com/fsharp/FAKE/pull/2195
* ENHANCEMENT: Support netcore paket (global tool) - https://github.com/fsharp/FAKE/pull/2191
* ENHANCEMENT: Properly fail when using FxCop on non-windows
* (Minor) BREAKING: Drop support for `netstandard1.6` (please open an issue if that actually hits you).
* BUGFIX: Fail on non-windows - https://github.com/fsharp/FAKE/pull/2200
* BUGFIX: Globbing fails when directory is deleted at the same time - https://github.com/fsharp/FAKE/issues/2203
* BUGFIX: Custom escaping was not working with `fromRawWindowsCommandLine` - https://github.com/fsharp/FAKE/issues/2197

## 5.10.1 - 2018-11-11

* DOCUMENTATION: New homepage design for fake.build, huge thanks to @FlorianBaderDE (with help from @JarnoNijboer) - https://github.com/fsharp/FAKE/pull/2164 https://github.com/fsharp/FAKE/pull/2175 https://github.com/fsharp/FAKE/pull/2178 https://github.com/fsharp/FAKE/pull/2179
* ICON: Fake has a new icon (again huge thanks to @FlorianBaderDE)
* NEW: Add tab-completion for powershell and bash - https://github.com/fsharp/FAKE/pull/2157
* NEW: module `Fake.DotNet.FxCop` - https://github.com/fsharp/FAKE/pull/2174
* ENHANCEMENT: add new ReportType "Cobertura" to `Fake.Testing.ReportGenerator` - https://github.com/fsharp/FAKE/pull/2163
* ENHANCEMENT: the fake template now supports the buildtask-dsl - https://github.com/fsharp/FAKE/pull/2177
* ENHANCEMENT: add new fake hints for https://github.com/fsharp/FAKE/issues/2097, https://github.com/fsharp/FAKE/issues/2099 and https://github.com/fsharp/FAKE/issues/2181
* BUGFIX: Dispose streams created by File.create - https://github.com/fsharp/FAKE/pull/2184/files
* BUGFIX: NuGet.Version.getLastNuGetVersion failed for some servers - https://github.com/fsharp/FAKE/pull/2170
* BUGFIX: Fake.DotNet.Tesing.NUnit failed to work when temporary-directory contains spaces (for example if windows username contains spaces) - https://github.com/fsharp/FAKE/pull/2180
* BUGFIX: `--silent` now disables fake hints as well
* (Minor) BREAKING: Refactor & cleanup `Fake.Runtime` internals - make sure to open an issue if you are hit by this.
* OTHER: Internal code cleanup - https://github.com/fsharp/FAKE/pull/2162

## 5.10.0 - 2018-11-11

* DOCUMENTATION: New homepage design for fake.build, huge thanks to @FlorianBaderDE (with help from @JarnoNijboer) - https://github.com/fsharp/FAKE/pull/2164 https://github.com/fsharp/FAKE/pull/2175 https://github.com/fsharp/FAKE/pull/2178 https://github.com/fsharp/FAKE/pull/2179
* ICON: Fake has a new icon (again huge thanks to @FlorianBaderDE)
* NEW: Add tab-completion for powershell and bash - https://github.com/fsharp/FAKE/pull/2157
* NEW: module `Fake.DotNet.FxCop` - https://github.com/fsharp/FAKE/pull/2174
* ENHANCEMENT: add new ReportType "Cobertura" to `Fake.Testing.ReportGenerator` - https://github.com/fsharp/FAKE/pull/2163
* ENHANCEMENT: the fake template now supports the buildtask-dsl - https://github.com/fsharp/FAKE/pull/2177
* ENHANCEMENT: add new fake hints for https://github.com/fsharp/FAKE/issues/2097, https://github.com/fsharp/FAKE/issues/2099 and https://github.com/fsharp/FAKE/issues/2181
* BUGFIX: Dispose streams created by File.create - https://github.com/fsharp/FAKE/pull/2184/files
* BUGFIX: NuGet.Version.getLastNuGetVersion failed for some servers - https://github.com/fsharp/FAKE/pull/2170
* BUGFIX: Fake.DotNet.Tesing.NUnit failed to work when temporary-directory contains spaces (for example if windows username contains spaces) - https://github.com/fsharp/FAKE/pull/2180
* BUGFIX: `--silent` now disables fake hints as well
* (Minor) BREAKING: Refactor & cleanup `Fake.Runtime` internals - make sure to open an issue if you are hit by this.
* OTHER: Internal code cleanup - https://github.com/fsharp/FAKE/pull/2162

## 5.9.3 - 2018-10-15

* BUGFIX: `/restore` was missing from msbuild command line - https://github.com/fsharp/FAKE/issues/2160

## 5.9.2 - 2018-10-14

* BUGFIX: `Fake.Core.Target` module no longer crashes with stackoverflow on some occations - https://github.com/fsharp/FAKE/pull/2156
* PERFORMANCE: The `Fake.Core.Target` module is now several orders of magnitude faster when using lots of targets - https://github.com/fsharp/FAKE/pull/2156

## 5.9.1 - 2018-10-14

* BUGFIX: Add a null-check to remove fake warning

## 5.9.0 - 2018-10-13

* ENHANCEMENT: Write NUnit arguments to an arguments file, fixes problems with long command lines - https://github.com/fsharp/FAKE/pull/2114
* ENHANCEMENT: Added `SpecFlowNext` module to `Fake.DotNet.Testing.SpecFlow` with improved API and missing arguments - https://github.com/fsharp/FAKE/pull/2143
* ENHANCEMENT (BREAKING): Updated and finalized the new (and undocumented) process API which is more unit-testable - https://github.com/fsharp/FAKE/pull/2131 
* ENHANCEMENT: Updated `Fake.Testing.ReportGenerator` to include `ClassFilter` and `FileFilter` - https://github.com/fsharp/FAKE/pull/2120
* ENHANCEMENT: Improve TeamCity integrations - https://github.com/fsharp/FAKE/pull/2138
* ENHANCEMENT: Update `Fake.Tools.Pickles` to include latest CLI additions - https://github.com/fsharp/FAKE/pull/2133
* ENHANCEMENT: The `Trace` module can now report build-state with a message - https://github.com/fsharp/FAKE/pull/2139
* ENHANCEMENT: The target module not supports a `Trace.WithContext` mode to retrieve the context and handle potenial problems manually - https://github.com/fsharp/FAKE/pull/2140
* ENHANCEMENT: The target module now allows to set the build-state by using the new `WithContext`-mode - https://github.com/fsharp/FAKE/pull/2141
* BUGFIX: Fix TeamCity (named) Artifact upload - https://github.com/fsharp/FAKE/pull/2147
* BUGFIX: AppVeyor module should not trace `appveyor.exe` calls - https://github.com/fsharp/FAKE/pull/2137
* BUGFIX: Always print the stack-trace when in verbose mode - https://github.com/fsharp/FAKE/issues/2136
* DOCUMENTATION: Some smaller fixes

## 5.8.5 - 2018-10-10

* BUGFIX: PATH Variable was overwritten when using the `DotNet.Cli` module - https://github.com/fsharp/FAKE/issues/2134
* BUGFIX: Fix an instance where TraceSecrets did not work - https://github.com/fsharp/FAKE/issues/2122
* WORKAROUND: Do not try to publish artifacts on github PRs when using Azure Pipelines - https://github.com/fsharp/FAKE/pull/2129/commits/bb723c41160f60002169246cb70ecbb9aad45fa3

## 5.8.4 - 2018-10-08

* ENHANCEMENT: Some modules are now usable without FAKE context (ie. in your regular projects)
* ENHANCEMENT: Inform when the `target` environment variable is used - https://github.com/fsharp/FAKE/issues/2107
* ENHANCEMENT: Improve TeamCity support - https://github.com/fsharp/FAKE/pull/2111
* ENHANCEMENT: Update Netstandard.Library package to `2.0.3` - https://github.com/fsharp/FAKE/pull/2106
* ENHANCEMENT: Add `Process.getFileName` api to retrieve the full-path of a given process
* ENHANCEMENT: Kill existing running `dotnet.exe` processes before running the dotnet-cli installer
* BUGFIX: Environment variables are case insensitive on windows, fake will now throw exceptions if it detects invalid environment maps
* BUGFIX: MSBuild properties containing special characters lead to errors - https://github.com/fsharp/FAKE/issues/2112
* BUGFIX: Improve error message when release notes are invalid - https://github.com/fsharp/FAKE/issues/2085
* BUGFIX: Improve error message when globbing pattern is invalid - https://github.com/fsharp/FAKE/issues/2073
* BUGFIX: Do no longer provide the `--parallel` argument by default when running expecto - https://github.com/fsharp/FAKE/issues/2039
* DOCS: Fix syntax errors in dacpac docs - https://github.com/fsharp/FAKE/pull/2115

## 5.7.2 - 2018-09-24

* ENHANCEMENT: TeamFoundation now reports errors as errors instead of warnings - https://github.com/fsharp/FAKE/pull/2103
* BUGFIX: Ignore some arguments when running `dotnet msbuild /version` - https://github.com/fsharp/FAKE/issues/2102

## 5.7.0 - 2018-09-23

* ENHANCEMENT: Use VSWhere to detect msbuild location - https://github.com/fsharp/FAKE/pull/2077
* ENHANCEMENT: Add Dotnet SDK 2.1.400, 2.1.401 and 2.1.402 - https://github.com/fsharp/FAKE/pull/2089
* ENHANCEMENT: Improve error reporting of msbuild errors across all CI servers - https://github.com/fsharp/FAKE/issues/2096
* ENHANCEMENT: Add /logger support for MSBuild - https://github.com/fsharp/FAKE/issues/1712
* ENHANCEMENT: Add /consoleloggerparameters support for MSBuild - https://github.com/fsharp/FAKE/issues/1607
* ENHANCEMENT: Added `DotNet.msbuild` to call `dotnet msbuild` - https://github.com/fsharp/FAKE/pull/2098
* ENHANCEMENT: Added `MSBuildParams` to `DotNet.restore`, `DotNet.build`, `DotNet.pack`, `DotNet.publish` and `DotNet.test` in order to add regular msbuild parameters - https://github.com/fsharp/FAKE/pull/2098
* ENHANCEMENT: AppVeyor now reports errors and warnings to the 'Messages'-tab - https://github.com/fsharp/FAKE/pull/2098

## 5.6.1 - 2018-09-09

* BUGFIX: dotnet restore with configfile did not work - https://github.com/fsharp/FAKE/issues/2082
* BUGFIX: Fake cache was broken - https://github.com/fsharp/FAKE/issues/2083
* DOCS: various improvements in the documentation - https://github.com/fsharp/FAKE/pull/2081

## 5.6.0 - 2018-09-09

* NEW: Fake.DotNet.Testing.DotCover module - https://github.com/fsharp/FAKE/pull/2058
* NEW: Fake.Installer.Squirrel module - https://github.com/fsharp/FAKE/pull/2076
* ENHANCEMENT: Fake.BuildServer.TeamCity now contains all parameters - https://github.com/fsharp/FAKE/pull/2069
* BUGFIX: template now contains `storage: none` - https://github.com/fsharp/FAKE/pull/2070
* BUGFIX: Improve error message when nuget cache is possibly broken - https://github.com/fsharp/FAKE/pull/2070
* BUGFIX: Invalidate cache if files don't exist - https://github.com/fsharp/FAKE/pull/2070
* BUGFIX: Vault.fs was not decrypting/encrypting properly - https://github.com/fsharp/FAKE/pull/2070
* BUGFIX: Fix an issue with empty arguments in command line parsing - https://github.com/fsharp/FAKE/pull/2070
* BUGFIX: Do not allow tripple quotes in command line - https://github.com/fsharp/FAKE/pull/2070

## 5.5.0 - 2018-08-26

* DOCS: Update core-targets.md - https://github.com/fsharp/FAKE/pull/2067
* NEW: module Fake.Tools.Octo to use Octopus Deploy - https://github.com/fsharp/FAKE/pull/2048
* NEW: module Fake.Core.Vault to store secrets - https://github.com/fsharp/FAKE/pull/2068/commits/929ec55bcb05b0d9ece0066c4d2c4f0ad2bce783
* ENHANCEMENT: Fake.BuildServer.TeamFoundation now supports secret variables - https://github.com/isaacabraham/vsts-fsharp/pull/16

## 5.4.1 - 2018-08-16

* BUGFIX: Support naming of artifacts in TFS/VSTS - https://github.com/fsharp/FAKE/pull/2060
* BUGFIX: Detect when expecto should use the .NET CLI - https://github.com/fsharp/FAKE/pull/2064

## 5.4.0 - 2018-08-11

* ENHANCEMENT: Template always uses latest version now - https://github.com/fsharp/FAKE/pull/2055
* ENHANCEMENT: Add all GITLAB environment variables - https://github.com/fsharp/FAKE/pull/2059
* ENHANCEMENT: Support naming of artifacts in TFS/VSTS - https://github.com/fsharp/FAKE/pull/2056
* BUGFIX: Use UTF8 without BOM by default - https://github.com/fsharp/FAKE/issues/2032
* BUGFIX: Improve error message when starting dotnet-cli with incorrect working directory - https://github.com/fsharp/FAKE/issues/2046

## 5.3.1 - 2018-08-06

* BUGFIX: Minor code improvements - https://github.com/fsharp/FAKE/pull/2045
* BUGFIX: Adding newline to fix 'fake --help' formatting - https://github.com/fsharp/FAKE/pull/2051
* BUGFIX: Proper quotations in `Fake.DotNet.Cli` module - https://github.com/fsharp/FAKE/pull/2053
* BUGFIX: Proper quotations in `Process.quote` and `Process.quoteIfNeeded` - https://github.com/fsharp/FAKE/pull/2045
* BUGFIX: The latest template now always installs the latest stable of fake - https://github.com/fsharp/FAKE/pull/2045
* BUGFIX: Support white spaces in paths when using the template on unix shell - https://github.com/fsharp/FAKE/issues/2054
* DOCS: Add SemVer module to the docs
* DOCS: Fix Rsync link in the docs

## 5.3.0 - 2018-07-30

* NEW: module Fake.Tools.Rsync - https://github.com/fsharp/FAKE/pull/1987
* NEW: module Fake.Installer.Wix - https://github.com/fsharp/FAKE/pull/2002
* NEW: module Fake.DotNet.Testing.VSTest - https://github.com/fsharp/FAKE/pull/2008
* ENHANCEMENT: Add .NET Sdk 2.1.302 version to Fake.DotNet.Cli - https://github.com/fsharp/FAKE/pull/2034
* ENHANCEMENT: Add extended commit message to Fake.Tools.Git - https://github.com/fsharp/FAKE/pull/2038
* ENHANCEMENT: GlobbingPattern.createFrom in Fake.IO.FileSystem - https://github.com/fsharp/FAKE/pull/2030
* ENHANCEMENT: Add `BuildConfiguration.fromEnvironmentVarOrDefault` to Fake.DotNet.Cli - https://github.com/fsharp/FAKE/pull/2031
* ENHANCEMENT: Add `withAdditionalArgs` helper to Fake.DotNet.Cli - https://github.com/fsharp/FAKE/pull/2044
* BUGFIX: Unbreak outdir on older mono - https://github.com/fsharp/FAKE/pull/2021
* BUGFIX: Fix Expecto ParallelWorkers - https://github.com/fsharp/FAKE/pull/2028
* BUGFIX: Environment setup error when running fake from a subdirectory - https://github.com/fsharp/FAKE/issues/2025
* BUGFIX: Fix signature of `Zip.createZip` - https://github.com/fsharp/FAKE/issues/2024
* BUGFIX: Call gitversion with mono if required - https://github.com/fsharp/FAKE/issues/2041

## 5.2.0 - 2018-07-10

* ENHANCEMENT: Improve output around empty target descriptions - https://github.com/fsharp/FAKE/pull/1996
* BUGFIX: Proper handling of dotnet SDK versioning - https://github.com/fsharp/FAKE/pull/1963
* NEW: Add debian package (maintainer wanted for hosting!) - https://github.com/fsharp/FAKE/pull/1863
* BUGFIX: Fix ambiguity in new octokit release - https://github.com/fsharp/FAKE/pull/2004
* BUGFIX: Fix XML poke - https://github.com/fsharp/FAKE/pull/2005
* BUGFIX: Fix SQLPackage path with VS2017 - https://github.com/fsharp/FAKE/pull/2006
* BUGFIX: Warn when resolved FSharp.Core doesn't match - https://github.com/fsharp/FAKE/issues/2001
* ENHANCEMENT: Upgrade runner to FSharp.Core 4.5
* ENHANCEMENT: Fix tracing of final and failure targets and add new `Trace.useWith` API - https://github.com/fsharp/FAKE/pull/2009 and https://github.com/fsharp/FAKE/pull/2016

## 5.1.0 - 2018-06-18

* NEW: module Fake.DotNet.Fsi to start fsharpi/fsi.exe on a F# script - https://github.com/fsharp/FAKE/pull/1970
* NEW: `dotnet new -i "fake-template::*"` and `dotnet new fake` to get started via SDK-template - https://github.com/fsharp/FAKE/pull/1989 and https://github.com/fsharp/FAKE/pull/1990
* NEW: module Fake.Tools.GitVersion to interact with gitversion.exe - https://github.com/fsharp/FAKE/pull/1988
* ENHANCEMENT: Add `traceImportantf` and `traceErrorf` - https://github.com/fsharp/FAKE/pull/1986
* ENHANCEMENT: Minimalize dependencies between FAKE packages - https://github.com/fsharp/FAKE/pull/1980
* ENHANCEMENT: Change chocolatey package according to feedback - https://github.com/fsharp/FAKE/pull/1983
* BUGFIX: Fix locating kudusync.cmd in Fake.Azure.Kudu - https://github.com/fsharp/FAKE/pull/1995
* NEW: module Fake.Core.UserInput - https://github.com/fsharp/FAKE/pull/1997

## 5.0.0 - 2018-06-06

* Deep integration into the .NET SDK and .Net Core
* Global dotnet cli-tool `dotnet tool install fake-cli -g --version 5.*`
* `dotnet-fake` cli tool via `DotNetCliToolReference`
* Standalone `.zip` xcopy deployment and various package manager integrations (for example chocolatey).
* The old runner (`FAKE` nuget package) is obsolete
* FakeLib and FAKE.Deploy are obsolete - https://github.com/fsharp/FAKE/issues/1820
* Clean and modularized API - A lot of stuff is obsolete now as it moved to a different location and will be removed with version 6
* Feature to import fake modules - https://fake.build/fake-fake5-modules.html
* Migration guide - https://fake.build/fake-migrate-to-fake-5.html
* New and clean CLI interface - https://fake.build/fake-commandline.html
* `build.sh` and `build.cmd` are no longer required
* Modules can be used in regular projects
* You can extend FAKE more easily - https://fake.build/fake-fake5-custom-modules.html
* API Guideline - https://fake.build/contributing.html#API-Design
* Improved F# scripting support including support for command-line arguments
* Learn more - https://fake.build/fake-fake5-learn-more.html

## 5.0.0-rc018.248 - 2018-06-05

* FAKE5: New module Fake.Core.FakeVar - https://github.com/fsharp/FAKE/pull/1978

## 5.0.0-rc018.244 - 2018-06-02

* ENHANCEMENT: Upgrade to final release of global tools - https://github.com/fsharp/FAKE/pull/1972
* ENHANCEMENT: Upgrade to netcore2.1 - https://github.com/fsharp/FAKE/pull/1972
* BUGFIX: Fix issue to hide secrets in output - https://github.com/fsharp/FAKE/pull/1969

## 5.0.0-rc017 - 2018-05-22

* BREAKING: Add some `[<RequireQualifiedAccess>]` attributes according to API-Guidelines (Target, Globbing, AssemblyInfoFile)
* ENHANCEMENT: Mark `Target.DoNothing` as obsolete
* ENHANCEMENT: Mark `Target.Description` as obsolete and add `Target.description`
* BUGFIX: Includes fixes from 4.64.12
* BUGFIX: Quoting in NuGet Restore task - https://github.com/fsharp/FAKE/pull/1962
* BUGFIX: Fix several bugs in AssemblyInfo reader - https://github.com/fsharp/FAKE/pull/1959
* BUGFIX: Workaround System.Reactive v4 release breaking us - https://github.com/fsharp/FAKE/issues/1966

## 5.0.0-rc016 - 2018-05-21

* ENHANCEMENT: Make caches more portable - https://github.com/fsharp/FAKE/issues/1924
* ENHANCEMENT: Handle Ctrl+C to properly shutdown (for example run final targets) - https://github.com/fsharp/FAKE/pull/1946
* ENHANCEMENT: Add `Target.deactivateBuildFailure` and `Target.deactivateFinal` - https://github.com/fsharp/FAKE/pull/1949
* BUGFIX: Fake had problems with `#load` directives in some scenarios - https://github.com/fsharp/FAKE/issues/1947
* BUGFIX: Parallel Builds on Team-Foundation would sometimes fail because of racy output of `#vso[]` commands - https://github.com/fsharp/FAKE/pull/1949
* BUGFIX: Parallel runner would run more targets than needed in error case - https://github.com/fsharp/FAKE/pull/1949

## 5.0.0-rc015 - 2018-05-21

* FAKE5: Global dotnet cli-tool `fake-cli` - https://github.com/fsharp/FAKE/pull/1932
  Install via `dotnet tool install fake-cli -g --version 5.0.0-*`
* BUGFIX: Some issues when running latest `dotnet cli` via the Fake.DotNet.Cli module.
* BUGFIX: Fake.Core.Xml changed DOCTYPE - https://github.com/fsharp/FAKE/issues/1692
* ENHANCEMENT: Add API to set the build state - https://github.com/fsharp/FAKE/issues/1936
* ENHANCEMENT: Add `Shell.copyFilesWithSubFolder` to copy files while keeping relative directories in place - https://github.com/fsharp/FAKE/issues/1937
* ENHANCEMENT: Fake runner will now hint you into success when dependencies are missing - https://github.com/fsharp/FAKE/issues/1783

## 5.0.0-rc014 - 2018-05-20

* BUGFIX: Globbing is now more robust (especially outside the working directory) - https://github.com/fsharp/FAKE/issues/1925 https://github.com/fsharp/FAKE/issues/1750 and some not tracked issues
* COSMETICS: Fake is printing target description twice - https://github.com/fsharp/FAKE/issues/1931
* ENHANCEMENT: Fake parallel logic is not even smarter in running targets parallel - https://github.com/fsharp/FAKE/pull/1934
* DOCS: We now have a full staging environment in place - https://fake.build/contributing.html#Staging-environment
* DOCS: We now have a place to thank our supporters - https://fake.build/help-supporters.html

## 5.0.0-rc013 - 2018-05-20

* ENHANCEMENT: Add partial restore (to improve the speed when using in a release-pipeline) - https://github.com/fsharp/FAKE/issues/1926
* FAKE5: Xake now supports FAKE 5 and is advertised as module - https://github.com/xakebuild/Xake
* ENHANCEMENT: Parallelize targets even more - https://github.com/fsharp/FAKE/pull/1934
* COSMETICS: Targets are always shown as "failed" - https://github.com/fsharp/FAKE/issues/1929
* COSMETICS: Target description was printed twice - https://github.com/fsharp/FAKE/issues/1931

## 5.0.0-rc012 - 2018-05-12

* FAKE5: New module `Fake.Windows.Registry` - https://github.com/fsharp/FAKE/pull/1909
* BUGFIX: Fix MSBuild parameter parsing - https://github.com/fsharp/FAKE/pull/1918
* BUGFIX: DocoptException was not reported correctly - https://github.com/MangelMaxime/fulma-demo/issues/4
* FAKE5: New module `Fake.DotNet.Fsc` - https://github.com/fsharp/FAKE/pull/1919
* ENHANCEMENT: Improve GitLab and VSTS CI Support - https://github.com/fsharp/FAKE/pull/1920
* FAKE5: New module `Fake.BuildServer.GitLab` - https://github.com/fsharp/FAKE/pull/1919
* BUGFIX: Zip.unzip was flattening the result - https://github.com/fsharp/FAKE/pull/1920
* BUGFIX: Fake complained that Intellisense.fsx didn't exist - https://github.com/fsharp/FAKE/issues/1917
* ENHANCEMENT: Targets now retrieve the current execution list as context parameter - https://github.com/fsharp/FAKE/pull/1920
* DOCS: Huge update to the documentation, adds obsolete warnings thanks @kblohm - https://github.com/fsharp/FAKE/pull/1923

## 5.0.0-rc011 - 2018-05-06

* ENHANCEMENT: Add Verbosity setting for NuGet restore - https://github.com/fsharp/FAKE/pull/1904
* BUGFIX: Fix msbuild helper OutputPath with trailing `\` - https://github.com/fsharp/FAKE/pull/1905
* BUGFIX: Make `Fake.Tools.Pickles` run on unix (with mono) - https://github.com/fsharp/FAKE/pull/1901
* DOCS: Add docs on how to test modules locally - https://github.com/fsharp/FAKE/pull/1906
* DOCS: Added some links to the documentation of SpecFlow, Pickles and ReportGenerator - https://github.com/fsharp/FAKE/pull/1907
* BUGFIX: API-Reference documentation showing invalid tool-tips - https://github.com/fsharp/FAKE/pull/1912
* BUGFIX: Fake being unable to compile when `intellisense.fsx` doesn't exist - https://github.com/fsharp/FAKE/issues/1908
* ENHANCEMENT: Some improvements to the target build order algorithm - https://github.com/fsharp/FAKE/pull/1903

## 5.0.0-rc010 - 2018-05-01

* BUGFIX: Some minor issues after last performance release - https://github.com/fsharp/FAKE/pull/1902

## 5.0.0-rc009 - 2018-05-01

* FAKE5: New module `Fake.Installer.InnoSetup` - https://github.com/fsharp/FAKE/pull/1890
* DOCS: Order module-namespaces alphabetically - https://github.com/fsharp/FAKE/pull/1891
* BUGFIX: Make sure ReportGenerator is run with mono on unix - https://github.com/fsharp/FAKE/pull/1894
* DOCS: Make sure API docs for new modules and FAKE 4 is available - https://github.com/fsharp/FAKE/pull/1893
* PERFORMANCE: A lot of shortcuts have been added to improve the speed of some common use-cases - https://github.com/fsharp/FAKE/pull/1891
* BUGFIX: `tryFindFileOnPath` not working properly on windows - https://github.com/fsharp/FAKE/issues/1899
* BREAKING: Change --environmentvariable to --environment-variable and --singletarget to --single-target - https://github.com/fsharp/FAKE/issues/1896
* BREAKING: Targets with arguments are not opt-in to make the command line parser fail on spurious inputs by default - https://github.com/fsharp/FAKE/issues/1896
  See https://fake.build/core-targets.html#Targets-with-arguments for details, basically you need to use `Target.runOrDefaultWithArguments` instead of `Target.runOrDefault` to opt-in.
* DOCS: Fix missing modules on documentation page - https://github.com/fsharp/FAKE/issues/1895
* DOCS: Documentation can now be hosted locally via `fake build target HostDocs` (after it has been built) - https://github.com/fsharp/FAKE/pull/1891

## 5.0.0-rc008 - 2018-04-26

* FAKE4: Bundle System.ValueTuple to prevent some issues - https://github.com/fsharp/FAKE/pull/1883
* DOCS: Various improvements - https://github.com/fsharp/FAKE/pull/1883
* FAKE5: New module `Fake.Tools.Pickles` - https://github.com/fsharp/FAKE/pull/1884
* FAKE5: New module `Fake.Tools.Mage` - https://github.com/fsharp/FAKE/pull/1885
* FAKE5: New module `Fake.DotNet.Testing.SpecFlow` - https://github.com/fsharp/FAKE/pull/1886
* FAKE5: New module `Fake.Testing.ReportGenerator` - https://github.com/fsharp/FAKE/pull/1887
* BUGFIX: Some fixes in TeamFoundation integration - https://github.com/fsharp/FAKE/pull/1883

## 5.0.0-rc007 - 2018-04-23

* DOCS: Update documentation issues - https://github.com/fsharp/FAKE/pull/1881
* BUGFIX: Improve error message when groups doesn't exist - https://github.com/fsharp/FAKE/issues/1874
* BUGFIX: Improve error message when file doesn't exist (`Process.start`) - https://github.com/fsharp/FAKE/issues/1875
* ENHANCEMENT: Make `Shell` module conform to API-guidelines. Mark existing functions as obsolete - https://github.com/fsharp/FAKE/pull/1882

## 5.0.0-rc006 - 2018-04-22

* FAKE5: New module `Fake.DotNet.Testing.Expecto` - https://github.com/fsharp/FAKE/pull/1871
* FAKE5: New module `Fake.Sql.DacPac` - https://github.com/fsharp/FAKE/pull/1878
* FAKE5: New module `Fake.Documentation.DocFx` - https://github.com/fsharp/FAKE/pull/1872
* FAKE5: Add ChangeLog module to `Fake.Core.ReleaseNotes` - https://github.com/fsharp/FAKE/pull/1876
* DOCS: Fix some documentation - https://github.com/fsharp/FAKE/pull/1877

## 5.0.0-rc005 - 2018-04-15

* [DOCS] Improve NPM documentation - https://github.com/fsharp/FAKE/pull/1867
* [FAKE 5] Add yarn module 'Fake.JavaScript.Yarn' - https://github.com/fsharp/FAKE/pull/1868
* [FAKE 5] Add HockeyApp module 'Fake.Api.HockeyApp' - https://github.com/fsharp/FAKE/pull/1870
* [BUGFIX] Properly handle '.' in file-names - https://github.com/fsharp/FAKE/issues/1865
* Add some missing RequireQualifiedAccess Attributes - https://github.com/fsharp/FAKE/pull/1862

## 5.0.0-rc004 - 2018-04-09

* Same as rc002

## 5.0.0-rc002 - 2018-04-09

* [BUGFIX] Fake could no longer find fsi after sdk upgrade - https://github.com/fsharp/FAKE/pull/1857
* [BUGFIX] Some hotfixes merged from fake 4 - https://github.com/fsharp/FAKE/pull/1849
* [DOCS] Fix various documentation issues - https://github.com/fsharp/FAKE/pull/1849

## 5.0.0-rc001

* [BREAKING] New runner is not compatible with old `Fake.Core.Target` package. Make sure to upgrade your packages before updating the runner!
* [BREAKING] Fix inconsistent naming of MSBuild package (MsBuild -> MSBuild), add setParams to run* methods - https://github.com/fsharp/FAKE/pull/1837
* [BREAKING] Update to the CLI, see https://fake.build/fake-commandline.html
* [FAKE 5] Implement MSBuild /nowarn command line option - https://github.com/fsharp/FAKE/pull/1840
* [FAKE 5] Target Parameters (closes [https://github.com/fsharp/FAKE/issues/1830](https://github.com/fsharp/FAKE/issues/1830))
* [FAKE 5] Scripts can now have/use their own CLI.
* [FAKE 5] Performance numbers (closes [https://github.com/fsharp/FAKE/issues/1831](https://github.com/fsharp/FAKE/issues/1831))
* [FAKE 5] new module `Fake.Core.CommandLineParsing`, see https://fake.build/core-commandlineparsing.html
* [DOCS] fixes https://github.com/fsharp/FAKE/issues/1845
* [BUGFIX] fix various migration messages and fix ChangeWatcher according to API-Guideline
* [BUGFIX] Remove content-disposition from Azure.Webjobs - https://github.com/fsharp/FAKE/pull/1844
* [BUGFIX] Don't alter global shell var $cur - https://github.com/fsharp/FAKE/pull/1844
* [BUGFIX] Fix docs and traces for dotnet version - https://github.com/fsharp/FAKE/pull/1835

## 5.0.0-beta029

* BUGFIX: Fix mono version detection on netcore.

## 5.0.0-beta028

* ENHANCEMENT: Fix API Guidelines on various modules
* BREAKING: API changes, cleanup and redesign Process module slightly
* BUGFIX: Crash on `DotNet.Install` when no dotnet was found.

## 5.0.0-beta027

* ENHANCEMENT: Allow to wire `DotNet.Install` and `DotNet.<Command>` such that they work together, see https://fake.build/dotnet-cli.html
* DOCS: Document how to run fake 5 scripts via `fsi`, see https://fake.build/fake-debugging.html

## 5.0.0-beta026

* BUGFIX: remove `Killing <id> failed with Process...` messages after Build
* BUGFIX: change the upload uri for AzureWebJobs - https://github.com/fsharp/FAKE/pull/1826
* BUGFIX: `fake run build.fsx --fsiargs "--debug:portable --optimize-"` will now actually create a pdb file and enable debugging

## 5.0.0-beta025

* BREAKING: Update ReleaseNotes module to match new API Guidelines.
* FAKE5: Concept for Build-Server Support
* FAKE5: New module `Fake.BuildServer.TeamCity`
* FAKE5: New module `Fake.BuildServer.AppVeyor`
* FAKE5: New module `Fake.BuildServer.Travis`
* FAKE5: New module `Fake.BuildServer.TeamFoundation`
* FAKE5: New module `Fake.JavaScript.Npm` - https://github.com/fsharp/FAKE/pull/1822
* BUGFIX: setKillCreatedProcesses was not working as expected - https://github.com/fsharp/FAKE/issues/1814
* ANNOUNCEMENT: Fake.Deploy is obsolete.

## 5.0.0-beta024

* ENHANCEMENT: Refactor Dotnet API - https://github.com/fsharp/FAKE/pull/1812
* BUGFIX: Find `dotnet` on `PATH` - https://github.com/fsharp/FAKE/pull/1813
* FAKE5: New modules `Fake.Azure.CloudServices`, `Fake.Azure.Emulators`, `Fake.Azure.Kudu` and `Fake.Azure.WebJobs` - https://github.com/fsharp/FAKE/pull/1757

## 5.0.0-beta023

* [CORE-PROCESS] ENHANCEMENT: Experiment with new Process API
* [CORE-TRACE] ENHANCEMENT: Add `TraceSecrets`-API to prevent FAKE from printing secrets

## 5.0.0-beta022

* FAKE5: New module "ChangeWatcher"
* FAKE5: Reduce number of modules by combining some modules:
  * `Fake.Core.BuildServer` moved to `Fake.Core.Environment`
  * `Fake.Core.Globbing` moved to `Fake.IO.FileSystem` (includes the namespace change from `Fake.Core` to `Fake.IO`)
* BREAKING: Refactor Process API according to FAKE5 guidelines
* BREAKING: Add `RequireQualifiedAccess` to some core modules in order to lead users into the new API-Usage
* ENHANCEMENT: Refactor `Fake.Core.Target` module and improve summary output and error reporting.
* BUGFIX: Use "defines" for the `#r "paket:..."`  tokenizer
* BUGFIX: Fake5 did not properly handle the --fsiargs command line argument.

## 5.0.0-beta021

* BUGFIX: Backslashes in paths wheren't escaped in intellisense.fsx - https://github.com/fsprojects/Paket/issues/3093

## 5.0.0-beta020

* BUGFIX: Fake now works without `.paket` folder - https://github.com/fsharp/FAKE/issues/1778 and https://github.com/fsharp/FAKE/issues/1564
* BUGFIX: https://github.com/fsharp/FAKE/issues/1744
* ENHANCEMENT: Fake now supports Credential Providers (like paket, see [https://fsprojects.github.io/Paket/credential-providers.html](https://fsprojects.github.io/Paket/credential-providers.html))

## 5.0.0-beta019

* BUGFIX: Added VS 2017 MSTest location - https://github.com/fsharp/FAKE/pull/1794 and https://github.com/fsharp/FAKE/pull/1604
* BUGFIX: Fix DotNetCompile - https://github.com/fsharp/FAKE/pull/1793

## 5.0.0-beta018 - 2018-02-19

* BUGFIX: Fix https://github.com/fsharp/FAKE/issues/1776 again for dotnet cli helpers.
* ENHANCEMENT: Fix chocolatey warning and add VERIFICATION.txt

## 5.0.0-beta017 - 2018-02-17

* BUGFIX: Fix https://github.com/fsharp/FAKE/issues/1776 again for dotnet cli helpers.
* BREAKING: the `Fake.Core.Tracing` nuget package has been renamed to `Fake.Core.Trace`
* BREAKING: Some functions/types with the name `Dotnet` have been renamed to `DotNet` for overall consistency.
* BUGFIX: ArgumentException in FtpHelper.uploadAFolder - https://github.com/fsharp/FAKE/issues/1785

## 5.0.0-beta016 - 2018-02-17

* BUGFIX: Fix bug in Cli.DotNetPack

## 5.0.0-beta015 - 2018-02-16

* INFRA: Add unit-test-suite for dotnetcore
* ENHANCEMENT: Add zip-helpers to allow more complex zip structures, fixes #1014
* BREAKING: Hide globbing behind an interface to make stuff more testable.
* BREAKING: Change Environment Map behavior in Process to allow removal of environment variables, required to fix #1776

## 5.0.0-beta014 - 2018-02-10

* BREAKING: Change Fake.DotNet.Cli API according to latest changes and add `dotnet test`
* BREAKING: Change Fake.DotNet.MsBuild API according to new API-Guidelines
* BUGFIX: msbuild doesn't work within dotnet-fake - https://github.com/fsharp/FAKE/issues/1776
* ENHANCEMENT: Fix SemVer - https://github.com/fsharp/FAKE/pull/848
* ENHANCEMENT: Add Nuget.RequireRange - https://github.com/fsharp/FAKE/pull/1775/commits/0c5b86b747285c596de0fff68002df422fabf15e

## 5.0.0-beta013 - 2018-02-03

* FAKE5: partial support for FST-1027 instead of FAKE Header (now obsolete and will be removed on release).
  This adds support for writing `#r "paket: nuget Fake.Module prerelease"` in your scripts, see https://github.com/fsharp/FAKE/pull/1770
* FAKE5: Self-Contained scripts (not using/referencing external paket.dependencies) will now write a `<script>.fsx.lock` file for the dependencies, see https://github.com/fsharp/FAKE/issues/1769
* FAKE5: `fake build <target>` is now a shortcut for `fake run build.fsx -t <target>`, see https://github.com/fsharp/FAKE/issues/1569

## 5.0.0-beta012 - 2018-01-28

* FAKE5: New distribution as dotnet-cli-tool `dotnet-fake` - https://github.com/fsharp/FAKE/pull/1766
* ENHANCEMENT: added GetDotNetSDKVersionFromGlobalJson - https://github.com/fsharp/FAKE/pull/1754
* BUGFIX: Include pdb and xml files again - https://github.com/fsharp/FAKE/pull/1766
* BUGFIX: Change default dotnet-cli-installer branch to `master` - https://github.com/fsharp/FAKE/issues/1739

## 5.0.0-beta011 - 2018-01-27

* ENHANCEMENT: Fake now writes load-scripts to provide intellisense - https://github.com/fsharp/FAKE/pull/1763
* BUGFIX: Fix version normalization - https://github.com/fsharp/FAKE/pull/1742
* DOCS: Fix documentation of SonarCube.End - https://github.com/fsharp/FAKE/pull/1743
* BUGFIX: Paket.Pack uses outdated syntax - https://github.com/fsharp/FAKE/pull/1737
* ENHANCEMENT: New Fake.Net.Http API to download files - https://github.com/fsharp/FAKE/pull/1746
* DOCS: Add simple Hello-World example - https://github.com/fsharp/FAKE/pull/1748
* FAKE4: fix custom proxy credentials in DotNetCLIHelper - https://github.com/fsharp/FAKE/pull/1741
* FAKE4: added executeFSIWithArgsAndReturnMessages  https://github.com/fsharp/FAKE/pull/1719
* DOCS: Fix path to migration guide - https://github.com/fsharp/FAKE/pull/1755
* NEW MODULE: Fake.Net.Http to download files - https://github.com/fsharp/FAKE/pull/1759
* ENHANCEMENT: Add Force parameter to Choco Push - https://github.com/fsharp/FAKE/pull/1735

## 5.0.0-beta010 - 2017-10-29

* ENHANCEMENT: Refactor GitHub API - https://github.com/fsharp/FAKE/pull/1726
* BUGFIX: Fix Overwrite in Fake.Core.Xml and arguments in Fake.DotNet.Paket - https://github.com/fsharp/FAKE/pull/1725
* ENHANCEMENT: New property ToolPath in MsBuild - https://github.com/fsharp/FAKE/pull/1703

## 5.0.0-beta008 - 2017-10-23

* BUGFIX: Fix error on mono when starting processes

## 5.0.0-beta007 - 2017-10-23

* BUGFIX: Fix error on mono when starting processes

## 5.0.0-beta006 - 2017-10-22

* BUGFIX: Add `Process.withFramework` to indicate that a process might need to be started with mono and use it in kown wrappers like test-runners - https://github.com/fsharp/FAKE/pull/1697
* DOCS: Typo ([https://github.com/fsharp/FAKE/pull/1701](https://github.com/fsharp/FAKE/pull/1701)), Canopy docs ([https://github.com/fsharp/FAKE/pull/1704](https://github.com/fsharp/FAKE/pull/1704)), some Urls ([https://github.com/fsharp/FAKE/pull/1708](https://github.com/fsharp/FAKE/pull/1708))
* DOCS: Migrate Slack API documentation for FAKE 5 - https://github.com/fsharp/FAKE/pull/1706
* ENHANCEMENT: Provide full fidelity of build options in Xamarin helpers - https://github.com/fsharp/FAKE/pull/1702
* ENHANCEMENT: Added WarnAsError to MSBuild options - https://github.com/fsharp/FAKE/pull/1691

## 5.0.0-beta005 - 2017-10-02

* ENHANCEMENT: Improve error messages of Fake.Core.Process - https://github.com/fsharp/FAKE/pull/1696
* BUGFIX: `fake --version` was printing the wrong version - https://github.com/fsharp/FAKE/pull/1696
* BUGFIX: `Fake.Api.GitHub` was not part of FakeLib - https://github.com/fsharp/FAKE/pull/1696

## 5.0.0-beta004 - 2017-10-02

* BUILD: Remove hardcoded paths to FSharpTargets, replace with FSharp.Compiler.Tools - https://github.com/fsharp/FAKE/pull/1693
* ENHANCEMENT: Fake.IO.FileSystem Rework, functionality moved from `Fake.IO.FileSystem` to `Fake.IO` and APIs have been adapted to the new design guideline - https://github.com/fsharp/FAKE/pull/1670
* PERFORMANCE: Fake should be a lot faster now - https://github.com/fsharp/FAKE/pull/1694
* ENHANCEMENT: Scripts are now compiled agains netstandard20 which allows a lot more APIs to be used - https://github.com/fsharp/FAKE/pull/1694
* DOCS: A lot of 404 pages have been filled in the fake.build website - https://github.com/fsharp/FAKE/pull/1694

## 5.0.0-beta003 - 2017-09-26

* ENHANCEMENT: Fix some migration warnings, Docs and bugs - https://github.com/fsharp/FAKE/pull/1686

## 5.0.0-beta002 - 2017-09-25

* BREAKING-CHANGE: Rework Fake.Core.Target package - https://github.com/fsharp/FAKE/pull/1664
* ENHANCEMENT: Update fake 5 to netcoreapp20 - https://github.com/fsharp/FAKE/pull/1678
* BUGFIX: Fix Zip.Unzip

## 5.0.0-beta001 - 2017-09-24

* BREAKING-CHANGE: Rework Fake.Core.Target package - https://github.com/fsharp/FAKE/pull/1664
* ENHANCEMENT: Update fake 5 to netcoreapp20 - https://github.com/fsharp/FAKE/pull/1678

## 5.0.0-alpha018 - 2017-09-24

* BUGFIX: Cache loaded assemblies and redirect later calls.

## 5.0.0-alpha017 - 2017-09-23

* BUGFIX: try to fallback to load framework assemblies from the default AssemblyLoadContext.

## 5.0.0-alpha016 - 2017-09-23

* BREAKING-CHANGE: Renew AssemblyInfo API - https://github.com/fsharp/FAKE/pull/1619
* ENHANCEMENT: Add XML to FakeLib - https://github.com/fsharp/FAKE/pull/1641
* ENHANCEMENT: Move Paket.Paket() over to use flag arguments - https://github.com/fsharp/FAKE/pull/1627
* ENHANCEMENT: MSTest detail switch - https://github.com/fsharp/FAKE/pull/1625
* API: StringBuilder functionality moved into a StringBuilder module
* ENHANCEMENT: Remove logfn from Compress and Extract - https://github.com/fsharp/FAKE/pull/1624
* BUGFIX: Correctly determine the 64-bit "Program Files" folder - https://github.com/fsharp/FAKE/pull/1623
* ENHANCEMENT: Bitbucket Environment variables - https://github.com/fsharp/FAKE/pull/1563
* ENHANCEMENT: AppVeyor Environment variables - https://github.com/fsharp/FAKE/pull/1560
* ENHANCEMENT: Change how npm.cmd is located in NpmHelper - https://github.com/fsharp/FAKE/pull/1629
* NEW MODULE: Fake.Api.Slack - https://github.com/fsharp/FAKE/pull/1648
* ENHANCEMENT: Add MSBuild binary logger support -  https://github.com/fsharp/FAKE/pull/1649
* ENHANCEMENT: Add BasePath support for NuGet pack - https://github.com/fsharp/FAKE/pull/1651
* DOCS: fix code in FAKE5 modules doc - https://github.com/fsharp/FAKE/pull/1653/files
* ENHANCEMENT: Paket 5.5.0+: GNU-compatible Paket commands - https://github.com/fsharp/FAKE/pull/1655
* BUGFIX: Target: update DoNothing signature - https://github.com/fsharp/FAKE/pull/1658
* DOCS: Review discussion channels in contributing page - https://github.com/fsharp/FAKE/pull/1661
* DOCS: Fixed the tooltip position and improved the styling in the api reference pages - https://github.com/fsharp/FAKE/pull/1663
* NEW MODULE: Fake.Api.GitHub previously known as Octokit.fsx - https://github.com/fsharp/FAKE/pull/1667
* DOCS: Fix menu on mobile - https://github.com/fsharp/FAKE/pull/1668
* ENHANCEMENT: Add Paket helper to push specific files - https://github.com/fsharp/FAKE/pull/1665

## 5.0.0-alpha015 - 2017-08-27

* Update Paket.core

## 5.0.0-alpha014 - 2017-07-28

* Disable MSBuild NodeReuse by default - https://github.com/fsharp/FAKE/pull/1600
* MSTest: add Tests parameter to specify list of tests - https://github.com/fsharp/FAKE/pull/1615

## 5.0.0-alpha013 - 2017-07-26

* ENHANCEMENT: Support for Microsoft Dynamics NAV 2017

## 5.0.0-alpha012 - 2017-07-25

* ENHANCEMENT: (NETCORE) Prefer msbuild over xbuild when we detect a mono installation > 5

## 5.0.0-alpha011 - 2017-07-24

* BUGFIX: Restore console encoding on .NET Framework - https://github.com/fsharp/FAKE/pull/1587
* FAKE5: Copy missing changes from legacy NuGet helpers - https://github.com/fsharp/FAKE/pull/1596
* ENHANCEMENT: Rename intellisense script - https://github.com/fsharp/FAKE/pull/1594
* NEW MODULE: Opencover migration - https://github.com/fsharp/FAKE/pull/1586
* PERFORMANCE: Fake 5 - Update Paket
* PERFORMANCE: Use Paket cache to increase warm start (with existing cache)
* PERFORMANCE: Remove runtime dependency resolution be default.
* DOCS Update comment https://github.com/fsharp/FAKE/pull/1602
* Added SkipNonTestAssemblies to NUnit3Params https://github.com/fsharp/FAKE/pull/1608

## 5.0.0-alpha010 - 2017-06-11

* BREAKING CHANGE: Change Targets API to be compatible with new API-Guidelines
* DOCS: Fix chocolatey docs and some links in footer https://github.com/fsharp/FAKE/pull/1584
* DOCS: Spelling, grammar, and emphasis changes https://github.com/fsharp/FAKE/pull/1575
* NEW MODULE: Sonarqube migration https://github.com/fsharp/FAKE/pull/1583
* BUGFIX: Restore the output encoding https://github.com/fsharp/FAKE/pull/1580
* BUGFIX: Fix Fake.IO.Zip creating invalid zip packages (empty folder at root level)
* Fake.exe -> fake.exe (windows) and Fake -> fake (unix)

## 5.0.0-alpha009 - 2017-05-27

* Change fake group feature because of https://github.com/fsprojects/Paket/issues/2374

## 5.0.0-alpha008 - 2017-05-27

* Add Fake.DotNet.FSFormatting module
* Add Fake.DotNet.Paket module

## 5.0.0-alpha007 - 2017-05-26

* CopyDir https://github.com/matthid/FAKE/pull/4
* Allow to annotate paket groups (no header needed) https://github.com/matthid/FAKE/pull/7

## 5.0.0-alpha006 - 2017-05-25

* DotnetCore Support, version 6 will only have the netcore version the old way of using FAKE is obsolete, but supported for now
  Migration guide is available in the documentation.
* Cleanup API - A lot of stuff is obsolete now as it moved to a different location and will be removed with version 6
* New CLI interface - The netcore version has a new cleaned-up CLI interface
* No more 'build.sh' and 'build.cmd' files required (though you still can use them of you want a 'zero' dependency build)
* Chocolatey Helper now supports Self-Contained packages
* Fix NuGet key leak if push fails - https://github.com/matthid/FAKE/pull/2
* Coreclr nunit3 params - https://github.com/matthid/FAKE/pull/3

## 4.64.18 - 2020-01-30

* Add Dynamics Business Central 365 support

## 4.64.17 - 2019-03-19

* Add support for MSBuild16 

## 4.64.16 - 2019-02-15

* Add Dynamics NAV FullCompile function

## 4.64.14 - 2019-01-07

* Add DynamicsNAV 365 Business Central support - https://github.com/fsharp/FAKE/pull/2224

## 4.64.13 - 2019-05-02

* Download x86 version of dotnet core on win x86 - https://github.com/SAFE-Stack/SAFE-BookStore/issues/328

## 4.64.11 - 2018-03-09

* Added SynchronizeSchemaChanges for DynamicsNAV

## 4.64.10 - 2018-03-06

* Added RunCodeunitWithSettings for DynamicsNAV - https://github.com/fsharp/FAKE/pull/1811

## 4.64.9 - 2018-03-05

* CompileWithFilter for DynamicsNAV

## 4.64.7 - 2018-03-01

* Allow REST calls without credentials

## 4.64.6 - 2018-02-21

* ConvertFileFromWin7ToWin8 reads file line by line

## 4.64.4 - 2018-01-17

* Support for Dynamics NAV 2018 - https://github.com/fsharp/FAKE/pull/1758

## 4.64.3 - 2017-12-19

* New helper: GetDotNetSDKVersionFromGlobalJson
* Do not download DotNet SDK when it's already in temp
* Use "dotnet --info" to determine DotNet SDK version
* Update PaketHelper to use new-style arguments - https://github.com/fsharp/FAKE/pull/1628
* Do not use "file" parameter for paket push
* Download .NET SDK to temp path

## 4.63.0 - 2017-07-27

* Disable MSBuild NodeReuse by default - https://github.com/fsharp/FAKE/pull/1600

## 4.62.6 - 2017-07-26

* Support for Microsoft Dynamics NAV 2017

## 4.62.5 - 2017-07-15

* Allow to download SDK from release page

## 4.62.1 - 2017-06-29

* Allow to set process encoding and fix issues with node tools - https://github.com/fsharp/FAKE/pull/1606

## 4.61.3 - 2017-05-26

* Fix msbuild 15 resolution - fixes https://github.com/fsprojects/Paket/issues/2345
* Upgrade assembly binding redirect for FluentMigrator - https://github.com/fsharp/FAKE/pull/1558

## 4.61.2 - 2017-05-11

* Allow to specify the default NuGet source - https://github.com/fsharp/FAKE/pull/1551
* Add support for custom NuGet symbol feed during push https://github.com/fsharp/FAKE/pull/1549

## 4.61.1 - 2017-05-04

* BUGFIX: Stop AzureHelper throwing exceptions unnecessarily - https://github.com/fsharp/FAKE/pull/1542

## 4.61.0 - 2017-05-02

* BUGFIX: msbuild is no longer being used on mono < 5.0.0.0 instead of xbuild - https://github.com/fsharp/FAKE/pull/1540

## 4.60.0 - 2017-04-27

* Add support for NUnit3 --params parameter - https://github.com/fsharp/FAKE/pull/1532
* New StyleCode helper - https://github.com/fsharp/FAKE/pull/1526
* BUGFIX: Fixed FtpHelper.isFolderPresent - https://github.com/fsharp/FAKE/pull/1529
* BUGFIX: Fixed NuGet key leak if publish fails - https://github.com/fsharp/FAKE/pull/1530
* BUGFIX: Disable caching for now - mono 5 bug - [https://github.com/fsharp/FAKE/pull/1535](https://github.com/fsharp/FAKE/pull/1535), https://github.com/fsharp/FAKE/pull/1536

## 4.59.0 - 2017-04-27

* BUGFIX: Upgraded FSharp.Core and FSharp.Compiler.Service to support mono 5 - https://github.com/fsharp/FAKE/pull/1534
* BUGFIX: Fixed Expecto TeamCity adapter - https://github.com/fsharp/FAKE/pull/1520
* Added installer for dotnet SDK
* Revert single thread build order change - https://github.com/fsharp/FAKE/pull/1516

## 4.58.0 - 2017-04-09

* Added helper for Office 365 / MS Teams Notifications - https://github.com/fsharp/FAKE/pull/1501
* Added options for better TC reporting of npm tasks - https://github.com/fsharp/FAKE/pull/1510
* Added a way to set the --trace parameter on the NUnit3 command line - https://github.com/fsharp/FAKE/pull/1513
* Add version-option to NugetUpdate in order to support explicit versions - https://github.com/fsharp/FAKE/pull/1514
* Make exit code accessible - https://github.com/fsharp/FAKE/pull/1502
* Additional Environment Vars for Appveyor - https://github.com/fsharp/FAKE/pull/1497
* BUGFIX: TeamCityHelper report build status incorrect string - https://github.com/fsharp/FAKE/pull/1503
* BUGFIX: Fixed Azure Storage Emulator path and arguments - https://github.com/fsharp/FAKE/pull/1499
* USABILITY: Improved log integration with AppVeyor - https://github.com/fsharp/FAKE/pull/1490
* USABILITY: Report target name if duplicate target is detected - https://github.com/fsharp/FAKE/pull/1506

## 4.57.3 - 2017-03-29

* Run parallel targets just in time - https://github.com/fsharp/FAKE/pull/1396

## 4.56.0 - 2017-03-17

* Yarn-Helper - https://github.com/fsharp/FAKE/pull/1494
* Add F# 4.1 directory path to FSIHelper paths

## 4.55.0 - 2017-03-13

* HockeyApp - create version and upload build to a specific version - https://github.com/fsharp/FAKE/pull/1487

## 4.54.0 - 2017-03-13

* Unix msbuild probing enhancements - https://github.com/fsharp/FAKE/pull/1488

## 4.53.0 - 2017-03-12

* New change log helper - https://github.com/fsharp/FAKE/pull/1467
* New output parameter to Dotnet BuildParams - https://github.com/fsharp/FAKE/pull/1481
* Added MSBuild location for VS2017 Build Tools - https://github.com/fsharp/FAKE/pull/1484
* BUGFIX: Fixed bug in getLastNuGetVersion when result is in JSON - https://github.com/fsharp/FAKE/pull/1482

## 4.52.0 - 2017-03-01

* Implement --dotGraph command line option - https://github.com/fsharp/FAKE/pull/1469
* USABILITY: Improve error handling on SqlPackage - https://github.com/fsharp/FAKE/pull/1476
* USABILITY: Don't fail on git file status detection

## 4.51.0 - 2017-02-28

* Teamcity build parameters - https://github.com/fsharp/FAKE/pull/1475
* Added updating of build details to AppVeyor - https://github.com/fsharp/FAKE/pull/1473
* New Expecto --parallel-workers and --summary-location parameters - https://github.com/fsharp/FAKE/pull/1450
* BREAKING: Git: recognize renamed (and other status) files - https://github.com/fsharp/FAKE/pull/1472
* BUGFIX: Using correct CLI parameter for silent xUnit output - https://github.com/fsharp/FAKE/pull/1464

## 4.50.1 - 2017-02-20

* BUGFIX: Use sequenced parameter for expecto

## 4.50.0 - 2017-01-17

* Visual Studio aware msbuild selection - https://github.com/fsharp/FAKE/pull/1453

## 4.49.0 - 2017-01-15

* MSBuild 15.0/VS 2017RC support - https://github.com/fsharp/FAKE/pull/1442

## 4.48.0 - 2017-01-05

* Add DisableParallel to dotnet restore params - https://github.com/fsharp/FAKE/pull/1443
* Added Expecto.CustomArgs property for new cli arguments - https://github.com/fsharp/FAKE/pull/1441
* New Expecto --verion parameter
* New Expecto --fail-on-focused-tests parameter
* New Expecto --summary parameter
* USABILITY: More verbose kill of processes

## 4.47.0 - 2016-12-17

* New Expecto helper - https://github.com/fsharp/FAKE/pull/1435
* Displas original server response when failing to parse JSON in Fake.Deploy - https://github.com/fsharp/FAKE/pull/1432
* Added SQLCMD variable support - https://github.com/fsharp/FAKE/pull/1434
* USABILITY: Improve error logging in GitVersionHelper - https://github.com/fsharp/FAKE/pull/1429

## 4.46.0 - 2016-12-03

* Decorated all *Params helper records with [<CLIMutable>] for C# access - https://github.com/fsharp/FAKE/pull/1428
* Added credentials parameter to ApplicationPoolConfig in IISHelper - https://github.com/fsharp/FAKE/pull/1425
* BUGFIX: Added a delay to prevent object disposed exceptions from process on macosx - https://github.com/fsharp/FAKE/pull/1426
* BUGFIX: Added try catch block to ignore error from setting console encoding - https://github.com/fsharp/FAKE/pull/1422
* BUGFIX: Disable NodeReuse for MSBuild on Jenkins too - https://github.com/fsharp/FAKE/pull/1418

## 4.45.1 - 2016-11-05

* BREAKING CHANGE: Remove old DotNet helper and cleanup DotNetCli helper
* BUGFIX: Worked around breaking change in NuGet 3.5 - https://github.com/fsharp/FAKE/issues/1415
* BUGFIX: Added logic to allow parsing of git branch names which track - https://github.com/fsharp/FAKE/pull/1417
* BUGFIX: Set Console.OutputEncoding <- System.Text.Encoding.UTF8 - https://github.com/fsharp/FAKE/pull/1414
* REVERT: Enable ServiceConfig element configuration in WixHelper - https://github.com/fsharp/FAKE/pull/1412

## 4.44.0 - 2016-11-03

* Enable ServiceConfig element configuration in WixHelper - https://github.com/fsharp/FAKE/pull/1412
* BUGFIX: Moved process stdout encoding out of is silent check - https://github.com/fsharp/FAKE/pull/1414

## 4.43.0 - 2016-10-30

* Better tracing of tasks in TeamCity - https://github.com/fsharp/FAKE/pull/1408
* BUGFIX: getBranchName works language independent - https://github.com/fsharp/FAKE/pull/1409
* Add support for the pin-project-references switch to PaketHelper - https://github.com/fsharp/FAKE/pull/1410

## 4.42.0 - 2016-10-25

* Add option to emit SuppressMessage Attributes - https://github.com/fsharp/FAKE/pull/1406
* Add language in NuGetParams - https://github.com/fsharp/FAKE/pull/1407
* Change order of Dynamics NAV process killing
* New SSH helper - https://github.com/fsharp/FAKE/pull/1405
* BUGFIX: FAKE should work with old and new commit messages
* BUGFIX: Fixed bug in assembly info variable name suffixes - https://github.com/fsharp/FAKE/pull/1404
* BUGFIX: Make FAKE fail on failing git push
* BUGFIX: When generating AssemblyMetadata_XYZ for AssemblyInfo, pass just value - https://github.com/fsharp/FAKE/pull/1399
* BUGFIX: Fixed AssemblyVersion bug - https://github.com/fsharp/FAKE/pull/1397
* BUGFIX: Fixing the famous chinese FAKE bug

## 4.41.1 - 2016-10-06

* Add all assembly metadata to AssemblyVersionInformation type - https://github.com/fsharp/FAKE/pull/1392
* Allow additional properties to be specified when running the SQL dacpac tooling - https://github.com/fsharp/FAKE/pull/1386
* Support for dotnet publish
* BUGFIX: wixHelper AllowDowngrades - https://github.com/fsharp/FAKE/pull/1389
* REVERT: Use nuget instead of referenced dlls. Fix SQL Server version issue - https://github.com/fsharp/FAKE/pull/1377

## 4.40.0 - 2016-09-19

* Allow to pass parameter to SonarQube end - https://github.com/fsharp/FAKE/pull/1381
* New helper: Search for files with a given pattern also in subdirectories - https://github.com/fsharp/FAKE/pull/1354
* Adds comment on top of auto-generated AssemblyInfo.fs files - https://github.com/fsharp/FAKE/pull/1373
* Use nuget instead of referenced dlls. Fix SQL Server version issue - https://github.com/fsharp/FAKE/pull/1377
* BUGFIX: NuGetVersion: adds application/xml to request accept header - https://github.com/fsharp/FAKE/pull/1383
* BUGFIX: Replace process cache with safe alternative - https://github.com/fsharp/FAKE/pull/1378
* BUGFIX: Call 'traceEndTask' in the finally block of a try-finally, so that it is always closed, even if the task throws an exception. https://github.com/fsharp/FAKE/pull/1379
* USABILITY: Check for npm on path variable in NpmHelper on Windows - https://github.com/fsharp/FAKE/pull/1371

## 4.39.0 - 2016-08-25

* Added Checksum[64][Type] in ChocoHelper - https://github.com/fsharp/FAKE/pull/1367
* Better support for multiple versions of SqlPackage - https://github.com/fsharp/FAKE/pull/1368
* Hint shown in for ArgumentException #1355 instead of trying to set it directly - https://github.com/fsharp/FAKE/pull/1366
* Added isWindows helper - https://github.com/fsharp/FAKE/pull/1356
* BUGFIX: Made GitVersionHelper PreReleaseNumber Nullable - https://github.com/fsharp/FAKE/pull/1365
* BUGFIX: TERM environment property should be upper case - https://github.com/fsharp/FAKE/pull/1363

## 4.38.0 - 2016-08-22

* BUGFIX: System.ArgumentNullException thrown for colored output on mono - https://github.com/fsharp/FAKE/pull/1362
* BUGFIX: Trim assembly info attribute value - https://github.com/fsharp/FAKE/pull/1361
* BUGFIX: Not printing MSBUILD command line twice - https://github.com/fsharp/FAKE/pull/1359
* BUGFIX: Semver parse fix to handle prereleases and build parts - https://github.com/fsharp/FAKE/pull/1325
* BUGFIX: Fixed FSCHelper - [https://github.com/fsharp/FAKE/pull/1351](https://github.com/fsharp/FAKE/pull/1351), https://github.com/fsharp/FAKE/pull/1352

## 4.37.0 - 2016-08-09

* New Octopus command for push - https://github.com/fsharp/FAKE/pull/1349
* New GitVersionHelper - https://github.com/fsharp/FAKE/pull/1319
* BUGFIX: Fixed multiple references for DLL in Fsc helper - https://github.com/fsharp/FAKE/pull/1350
* BUGFIX: Fixed NugetHelper.fs: GetPackageVersion - https://github.com/fsharp/FAKE/pull/1343
* BUGFIX: Fixed detection of GitLab CI with current multi-runner - https://github.com/fsharp/FAKE/pull/1345

## 4.36.0 - 2016-08-01

* Added methods to cover manipulation of Content Nodes in project files - https://github.com/fsharp/FAKE/pull/1335
* BUGFIX: Fix Fsc short toggle argument format - https://github.com/fsharp/FAKE/pull/1339
* BUGFIX: Update search pattern in NuGet helper - https://github.com/fsharp/FAKE/pull/1334
* BUGFIX: Expanded typescript search paths on windows to cover all new typescript compilers - https://github.com/fsharp/FAKE/pull/1308

## 4.35.0 - 2016-07-24

* New registry support in WiXHelper - https://github.com/fsharp/FAKE/pull/1331
* BREAKING CHANGE: Changed DotNet helper to DotNetCLI - https://github.com/fsharp/FAKE/pull/1332
* BUGFIX: Fixed exception when dotnet cli is not installed - https://github.com/fsharp/FAKE/pull/1332
* BUGFIX: Fixed git reset helper to use checkout when file resets are requested - https://github.com/fsharp/FAKE/pull/1326
* BUGFIX: Masked octo api key when tracing - https://github.com/fsharp/FAKE/pull/1327

## 4.34.5 - 2016-07-21

* DotNet version support - https://github.com/fsharp/FAKE/pull/1310
* DotNet test support - https://github.com/fsharp/FAKE/pull/1311
* DotNet build support - https://github.com/fsharp/FAKE/pull/1318
* DotNet pack support - https://github.com/fsharp/FAKE/pull/1313
* Allows to set version in project.json
* Allow to run arbitrary dotnet CLI commands
* Allow to add arbitrary args to all dotnet CLI commands
* DotNet restore support - https://github.com/fsharp/FAKE/pull/1309
* BUGFIX: Update DACPAC module - https://github.com/fsharp/FAKE/pull/1307

## 4.32.0 - 2016-07-18

* BUGFIX: Convert relative path to absolute path when creating NAntXmlTraceListen - https://github.com/fsharp/FAKE/pull/1305
* BUGFIX: Update DACPAC module - https://github.com/fsharp/FAKE/pull/1306
* BUGFIX: Fixed FscParam.References issue - https://github.com/fsharp/FAKE/pull/1304
* REVERT: Better Old-Style Arg parsing - https://github.com/fsharp/FAKE/pull/1301

## 4.31.0 - 2016-07-14

* BUGFIX: Better Old-Style Arg parsing - https://github.com/fsharp/FAKE/pull/1301
* BUGFIX: Fixed typo in continuous web job path - https://github.com/fsharp/FAKE/pull/1297 https://github.com/fsharp/FAKE/pull/1300
* COSMETICS: Update XUnit2 module to fail gracefully - https://github.com/fsharp/FAKE/pull/1302

## 4.30.0 - 2016-07-12

* Improved default npm path handling - https://github.com/fsharp/FAKE/pull/1278
* BUGFIX: Fixed Fake.Deploy downloadString - https://github.com/fsharp/FAKE/pull/1288
* BUGFIX: update fix for mono encoding in ProcessHelper.fs - https://github.com/fsharp/FAKE/pull/1276
* BUGFIX: XamarinHelper - file name was not quoted by calling zipalign - https://github.com/fsharp/FAKE/pull/1294

## 4.29.0 - 2016-06-19

* New helper to execute Sysinternals PsExec - https://github.com/fsharp/FAKE/pull/1266
* Add initial support for Fuchu tests - https://github.com/fsharp/FAKE/pull/1268
* New Bower helper - https://github.com/fsharp/FAKE/pull/1258
* FAKE cache is now local to the build script - https://github.com/fsharp/FAKE/pull/1250
* BUGFIX: Correct waiting for android packaging and fix for mono processes encoding - https://github.com/fsharp/FAKE/pull/1275
* BUGFIX: Fixed issue in bulk component creation functions where IDs where invalid - https://github.com/fsharp/FAKE/pull/1264
* BUGFIX: Support VB.NET's case-insensitive assembly attributes - https://github.com/fsharp/FAKE/pull/1255
* BUGFIX: Bamboo buildNumber environment variable in case sensitive behavior - https://github.com/fsharp/FAKE/pull/1252
* BUGFIX: Final Targets are no longer lowercased - https://github.com/fsharp/FAKE/pull/1261

## 4.28.0 - 2016-05-30

* New DocFx helper - https://github.com/fsharp/FAKE/pull/1251
* Added basic support for bitbuckets piplines CI - https://github.com/fsharp/FAKE/pull/1248
* BUGFIX: XamarinHelper: surround jarsigner input file path with quotes - https://github.com/fsharp/FAKE/pull/1249
* BUGFIX: NUnit3: don't set a timeout on the nunit3-console process - https://github.com/fsharp/FAKE/pull/1247
* BUGFIX: Changed the cache path to be relative to script location - https://github.com/fsharp/FAKE/pull/1250

## 4.27.0 - 2016-05-19

* New Slack argument for Link_Names - https://github.com/fsharp/FAKE/pull/1245
* Extended WiXHelper types for supporting creation of 64bit setups - https://github.com/fsharp/FAKE/pull/1244
* BUGFIX: Corrected NuGet install verbosity parameter - https://github.com/fsharp/FAKE/pull/1239

## 4.26.0 - 2016-05-11

* Added new function for appending signatures - https://github.com/fsharp/FAKE/pull/1223
* New separate environ variable helpers - https://github.com/fsharp/FAKE/pull/1133
* Reversed the order of actions in traceStartTarget - https://github.com/fsharp/FAKE/pull/1222
* Update Pickles helper to reflect the latest changes to pickles - https://github.com/fsharp/FAKE/pull/1236
* New parameter 'AppId' in HockeyAppHelper - https://github.com/fsharp/FAKE/pull/1234
* MSBuildHelper: Add BuildWebsite(s)Config - https://github.com/fsharp/FAKE/pull/1230
* BUGFIX: OpenCoverHelper does not assume AppData and ProgramFiles exists by default - https://github.com/fsharp/FAKE/pull/1229
* BUGFIX: Disable node reuse on Team foundation builds - https://github.com/fsharp/FAKE/pull/1237
* BUGFIX: Fixed FAKE parameter split - https://github.com/fsharp/FAKE/pull/1228
* USABILITY: Look into packages folder for findToolPath
* COSMETICS: Added open/close block to teamcity target tracing - https://github.com/fsharp/FAKE/pull/1219

## 4.25.0 - 2016-04-12

* Use FSharp.Compiler.Service 3.0
* BUGFIX: Added TeamCity to the list of build servers that fails on error stream output - https://github.com/fsharp/FAKE/pull/1216
* BUGFIX: Fixed failure to handle spaces or other special characters when running mono exes - https://github.com/fsharp/FAKE/pull/1214
* BUGFIX: Use UTF-8 encoding for process output on Mono - https://github.com/fsharp/FAKE/pull/1215

## 4.24.0 - 2016-04-10

* New Kudu feature to copy recursively - https://github.com/fsharp/FAKE/pull/1203
* Support for source code deployments on Azure websites through KuduSync - https://github.com/fsharp/FAKE/pull/1200
* Expose 'GetErrors' on TargetHelper - https://github.com/fsharp/FAKE/pull/1209
* BUGFIX: Call WaitForExit twice to catch all output - https://github.com/fsharp/FAKE/pull/1211
* BUGFIX: Only write to STDERR if an error happened - https://github.com/fsharp/FAKE/pull/1210
* BUGFIX: Prevent false-positive in EnvironmentHelper.isMacOS on Windows - https://github.com/fsharp/FAKE/pull/1204
* BUGFIX: Accept the cached assembly when the public token of the given assembly is null - https://github.com/fsharp/FAKE/pull/1205

## 4.23.0 - 2016-04-01

* Make a new method for sending a coverage file to TeamCity - https://github.com/fsharp/FAKE/pull/1195
* Added more deployment options for OctoTools - https://github.com/fsharp/FAKE/pull/1192
* Added contents of `AssemblyInformationalVersionAttribute` to the `AssemblyVersionInformation` class - https://github.com/fsharp/FAKE/pull/1183
* Added HarvestDirectory helper to the WixHelper Library - https://github.com/fsharp/FAKE/pull/1179
* Added support for SQL DacPac - https://github.com/fsharp/FAKE/pull/1185
* Provide CurrentTargetOrder for build scripts
* Added namespace/class/method args for xunit2 - https://github.com/fsharp/FAKE/pull/1180
* If build failed then kill all created processes at end of build
* Make DynamicsNAV errors a known FAKE exception
* BUGFIX: Fixed hard coded path in artifact publishing to AppVeyor - https://github.com/fsharp/FAKE/pull/1176

## 4.22.0 - 2016-03-13

* Added artifact publishing to AppVeyor - https://github.com/fsharp/FAKE/pull/1173
* Azure Web Jobs now get created during deploy if they do not already exist - https://github.com/fsharp/FAKE/pull/1174
* BUGFIX: New Sonar options - https://github.com/fsharp/FAKE/pull/1172
* BUGFIX: Fixed issue with IDs that did not start with a letter - https://github.com/fsharp/FAKE/pull/1167
* BUGFIX: Fixed IgnoreTestCase helper - https://github.com/fsharp/FAKE/pull/1159
* BUGFIX: use compileFiles in compile - https://github.com/fsharp/FAKE/pull/1165
* BUGFIX: Fixed bug in WiXDir function, that would set plain directory name as id - https://github.com/fsharp/FAKE/pull/1164
* BUGFIX: Fixed bug that prevented using directory names with spaces in WiX - https://github.com/fsharp/FAKE/pull/1160

## 4.21.0 - 2016-02-29

* New helper for chocolatey - http://fsharp.github.io/FAKE/chocolatey.html
* New helper for Slack - http://fsharp.github.io/FAKE/slacknotification.html
* New helper for SonarQube - http://fsharp.github.io/FAKE/sonarcube.html
* New helper for creating paket.template files for Paket - https://github.com/fsharp/FAKE/pull/1148
* New version of WatchChanges that support options - https://github.com/fsharp/FAKE/pull/1144
* Improved AppVeyor test results upload - https://github.com/fsharp/FAKE/pull/1138
* Added support for Paket's minimum-from-lock-file in pack command - https://github.com/fsharp/FAKE/pull/1149
* Added support for NUnit3 --labels parameter - https://github.com/fsharp/FAKE/pull/1153
* BUGFIX: Fixed Issue #1142: Arguments of CombinePaths are switched in WixHelper - https://github.com/fsharp/FAKE/pull/1145
* BUGFIX: NuGet auto version bug fix - https://github.com/fsharp/FAKE/pull/1146
* WORKAROUND: nuget.org changed base url

## 4.20.0 - 2016-02-06

* Allows to create full Wix directory hierarchy - https://github.com/fsharp/FAKE/pull/1116
* New PicklesHelper for generating living documentation with Pickles - https://github.com/fsharp/FAKE/pull/1126
* BUGFIX: Replaced system directory separator with "/" in ArchiveHelper - https://github.com/fsharp/FAKE/pull/1127

## 4.19.0 - 2016-02-02

* New FSC task - https://github.com/fsharp/FAKE/pull/1122
* Disable warning from #1082 for now because it created lots of confusion

## 4.18.0 - 2016-02-02

* New helpers which allow to send .NET coverage settings to TeamCity - https://github.com/fsharp/FAKE/pull/1117
* Disabled NodeReuse on TeamCity, it can lead to consecutive builds failing - https://github.com/fsharp/FAKE/pull/1110
* Added IncludeReferencedProjects property to the Packet.Pack params - https://github.com/fsharp/FAKE/pull/1112
* BUGFIX: Ensure that traceEndTask is called in DotCover - https://github.com/fsharp/FAKE/pull/1118
* BUGFIX: WiXHelper: fixed typos in WiXDir.ToString - https://github.com/fsharp/FAKE/pull/1120

## 4.17.0 - 2016-01-23

* Renamed internal FSharp.Compiler.Service to avoid clashes - https://github.com/fsharp/FAKE/pull/1097
* Added support for "paket restore" - https://github.com/fsharp/FAKE/pull/1108
* WiX service install - https://github.com/fsharp/FAKE/pull/1099
* Passing timeout value also to solution exchanger in DynamicsCRMHelper - https://github.com/fsharp/FAKE/pull/1102
* BUGFIX: Fallback to recompile when caching of build script fails - https://github.com/fsharp/FAKE/pull/1093
* BUGFIX: Commit message will be retrieved for older and newer git versions - https://github.com/fsharp/FAKE/pull/1098
* BUGFIX: Fixed case sensitivity on package name when search references in Paket.lock - https://github.com/fsharp/FAKE/pull/1089
* COSMETICS: Don't show the obsolete usage - https://github.com/fsharp/FAKE/pull/1094

## 4.16.0 - 2016-01-20

* General FAKE improvements - https://github.com/fsharp/FAKE/pull/1088
* Hockey app UploadTimeout - https://github.com/fsharp/FAKE/pull/1087

## 4.15.0 - 2016-01-19

* Add support for appcast generation - https://github.com/fsharp/FAKE/pull/1057
* Function to remove Compile elems missing files - https://github.com/fsharp/FAKE/pull/1078
* AssemblyInfoFile: added functions to read and update attributes -https://github.com/fsharp/FAKE/pull/1073
* Added support for packing symbols via PaketHelper - https://github.com/fsharp/FAKE/pull/1071
* Tell the clr to use the cached assemblies even when it tries to reload them with a different context - https://github.com/fsharp/FAKE/pull/1056
* BUGFIX: Fix failure when space in temp path - https://github.com/fsharp/FAKE/pull/1076
* BUGFIX: Fix app.config files
* BUGFIX: Cache invalidate on changing fsiOptions - https://github.com/fsprojects/ProjectScaffold/issues/231

## 4.14.0 - 2016-01-12

* NuGet automatic version increment - https://github.com/fsharp/FAKE/pull/1063
* Added support for the Paket pack parameter buildPlatform - https://github.com/fsharp/FAKE/pull/1066
* Added possibility to bulk update assembly infos with file includes - https://github.com/fsharp/FAKE/pull/1067

## 4.13.0 - 2016-01-11

* NUnit 3 support - https://github.com/fsharp/FAKE/pull/1064
* Automatic discovery of octo.exe - https://github.com/fsharp/FAKE/pull/1065
* Prefer git from cmd path to get ssh key - https://github.com/fsharp/FAKE/pull/1062

## 4.12.0 - 2015-12-28

* Change SignToolHelper syntax to reflect common call syntax - https://github.com/fsharp/FAKE/pull/1051
* New Open/Close block helpers for TeamCity - https://github.com/fsharp/FAKE/pull/1049
* BUGFIX: Use UTF-8 encoding of AssemblyInfo as written by ReplaceAssemblyInfoVersions - https://github.com/fsharp/FAKE/pull/1055

## 4.11.0 - 2015-12-19

* Add specific version parameter in PaketPackParams - https://github.com/fsharp/FAKE/pull/1046
* Fixed isMacOS function - https://github.com/fsharp/FAKE/pull/1044
* Added more comfortable types to WiXHelper, flagged old ones obsolete - https://github.com/fsharp/FAKE/pull/1036
* Use FSharp.Compiler.Service 1.4.2.3
* Only add relative path prefix if not rooted path in MSBuildHelper - https://github.com/fsharp/FAKE/pull/1033
* Replaced hard reference on gacutil path with automatic search  - https://github.com/fsharp/FAKE/pull/1040
* Wrap OutputPath in quotes in paket helper - https://github.com/fsharp/FAKE/pull/1027
* Allow override of the signature algorithm and message digest algorithm in Xamarin helper - https://github.com/fsharp/FAKE/pull/1025
* Expose excluded templates in Pack helper - https://github.com/fsharp/FAKE/pull/1026
* Added initial implementation of DynamicsCRMHelper - https://github.com/fsharp/FAKE/pull/1009

## 4.10.0 - 2015-11-30

* Added support for Squirrel's --no-msi option - https://github.com/fsharp/FAKE/pull/1013
* Upload has longer timeout - https://github.com/fsharp/FAKE/pull/1004
* Added the History Directory argument of ReportGenerator - https://github.com/fsharp/FAKE/pull/1003
* Support for Bamboo build server - https://github.com/fsharp/FAKE/pull/1015
* Added APPVEYOR_JOB_NAME appveyor environment variable - https://github.com/fsharp/FAKE/pull/1022
* Updated octopus sample to reflect 3.3.0 package - https://github.com/fsharp/FAKE/pull/1021
* Added functionality for deleting files and folders in FTP Helper - https://github.com/fsharp/FAKE/pull/1018
* BASH completion for FAKE targets - https://github.com/fsharp/FAKE/pull/1020
* BUGFIX: Fix case on MsBuild LogFile option - https://github.com/fsharp/FAKE/pull/1008
* BUGFIX: Fix git version on Mac - https://github.com/fsharp/FAKE/pull/1006

## 4.9.1 - 2015-11-11

* Added support for channels to OctoTools - https://github.com/fsharp/FAKE/pull/1001
* BUGFIX: Create AssemblyInfo directory only where required - https://github.com/fsharp/FAKE/pull/997
* COSMETICS: Renamed confusing parameter in FSI helper - https://github.com/fsharp/FAKE/pull/1000

## 4.8.0 - 2015-11-04

* Basic npm support - https://github.com/fsharp/FAKE/pull/993
* New RoboCopy helper - https://github.com/fsharp/FAKE/pull/988
* Option ignore failing tests DotCover https://github.com/fsharp/FAKE/pull/990
* Add code to replace new assemblyinfo attributes - https://github.com/fsharp/FAKE/pull/991
* Cleanup Registry helpers - https://github.com/fsharp/FAKE/pull/980
* FAKE.Deploy scans for default scripts - https://github.com/fsharp/FAKE/pull/981
* BUGFIX: Use WorkingDir in Paket helpers
* BUGFIX: support caching even when running RazorEngine as part of the build script - https://github.com/fsharp/FAKE/pull/979

## 4.6.0 - 2015-10-14

* New Registry functions - https://github.com/fsharp/FAKE/pull/976
* Add attribute filters to DotCover - https://github.com/fsharp/FAKE/pull/974
* Always use FullName of nuspec for NuGet pack
* DotCover support for MSTest - https://github.com/fsharp/FAKE/pull/972
* Added new functions: replace and poke for inner xml - https://github.com/fsharp/FAKE/pull/970
* Adding TestFile helper - https://github.com/fsharp/FAKE/pull/962

## 4.5.0 - 2015-10-07

* Ensure FSI-ASSEMBLY.dll path exists - https://github.com/fsharp/FAKE/pull/969
* New dotCover runner for Xunit2 - https://github.com/fsharp/FAKE/pull/965
* Make FAKE compatible with Microsoft Dynamics 2016
* Don't assume that mono is on the path for El Capitan - https://github.com/fsharp/FAKE/pull/963/files
* Better target handling - https://github.com/fsharp/FAKE/pull/954
* Ignore group lines in paket.references parser
* Revert breaking change in FCS
* Support for Android-MultiPackages - https://github.com/fsharp/FAKE/pull/964
* BUGFIX: Exclude long directories from globbing - https://github.com/fsharp/FAKE/pull/955
* BUGFIX: Encode script path in cache - https://github.com/fsharp/FAKE/pull/956

## 4.4.0 - 2015-09-11

* iOSBuild relies on xbuild instead of mdtool - https://github.com/fsharp/FAKE/pull/945
* New method to return whether or not a value exists for a registry key - https://github.com/fsharp/FAKE/pull/944
* Extended ReportGeneratorHelper to add Badges report type - https://github.com/fsharp/FAKE/pull/943
* HockeyAppHelper download team restriction - https://github.com/fsharp/FAKE/pull/939
* Use TFS variables as fallback, fixes #933 - https://github.com/fsharp/FAKE/pull/937
* Deployment configurable timeouts in FAKE.Deploy - https://github.com/fsharp/FAKE/pull/927
* Fixed bug where only first 1024 bytes were uploaded using FTP - https://github.com/fsharp/FAKE/pull/932
* FAKE 4.2 or newer started with wrong Target - https://github.com/fsharp/FAKE/pull/931
* Better user input helper - https://github.com/fsharp/FAKE/pull/930
* Add support for new Xunit2 runner -noappdomain flag - https://github.com/fsharp/FAKE/pull/928

## 4.3.0 - 2015-08-26

* FluentMigrator helper library - http://fsharp.github.io/FAKE/fluentmigrator.html

## 4.2.0 - 2015-08-24

* Support for soft dependencies for targets - http://fsharp.github.io/FAKE/soft-dependencies.html
* Added support for builds within Team Foundation Server (and VSO) - https://github.com/fsharp/FAKE/pull/915
* New options in the SquirrelHelper - https://github.com/fsharp/FAKE/pull/910
* Logging improvement in Fake.Deploy - https://github.com/fsharp/FAKE/pull/914
* New RunTargetOrListTargets function - https://github.com/fsharp/FAKE/pull/921
* Added date to ReleaseNotes type definition - https://github.com/fsharp/FAKE/pull/917
* Added `createClientWithToken` & `createRelease` to Octokit.fsx - https://github.com/fsharp/FAKE/pull/913
* Fixed WatchChanges not properly removing subdirectories from watch list - https://github.com/fsharp/FAKE/pull/908
* Added ability to optionally pass in SiteId to configure IIS Site - https://github.com/fsharp/FAKE/pull/905
* Pass OutputDataReceived to logfn instead of trace in shellExec - https://github.com/fsharp/FAKE/pull/906
* Add GetDependenciesForReferencesFile

## 4.1.0 - 2015-08-10

* Using FSharp.Compiler.Server for F# 4.0
* Added Squirrel helpers to generate Squirrel installers - https://github.com/fsharp/FAKE/pull/899
* Added Ability to specify Identity for AppPool - https://github.com/fsharp/FAKE/pull/902
* Dynamics NAV: version helpers - https://github.com/fsharp/FAKE/pull/900
* Added ReleaseNotes to NugetHelper - https://github.com/fsharp/FAKE/pull/893
* BUGFIX: running from a network drive - https://github.com/fsharp/FAKE/pull/892
* BUGFIX: Align NUnitDomainModel with NUnit documentation - https://github.com/fsharp/FAKE/pull/897
* BUGFIX: Skip Octokit retry logic on Mono where it causes a crash - https://github.com/fsharp/FAKE/pull/895
* BUGFIX: FAKE removes mono debug file after cache is saved - https://github.com/fsharp/FAKE/pull/891
* BUGFIX: Nunit Domain Fix - https://github.com/fsharp/FAKE/pull/883
* BUGGFIX: Dynamic assembly handling for caching - https://github.com/fsharp/FAKE/pull/884
* BUGFIX: Loaded dlls versions are used to invalidate FAKE's cache - https://github.com/fsharp/FAKE/pull/882

## 4.0.0 - 2015-07-23

* Automatic caching of FAKE build scripts - https://github.com/fsharp/FAKE/pull/859
* Added MSBuild properties to AndroidPackageParams - https://github.com/fsharp/FAKE/pull/863
* Add support for outputting NUnit style test result XML to Fake.Testing.XUnit2  - https://github.com/fsharp/FAKE/pull/870
* Add support for VS2015 VSTest executable - https://github.com/fsharp/FAKE/pull/877
* Add lock-dependencies parameter to Paket.Pack - https://github.com/fsharp/FAKE/pull/876

## 3.36.0 - 2015-07-13

* NoLogo parameter for MSBuildHelper - https://github.com/fsharp/FAKE/pull/850
* Expose Globbing.isMatch for use by external code - https://github.com/fsharp/FAKE/pull/860
* VB6 dependency updater - https://github.com/fsharp/FAKE/pull/857
* Added BuildConfig/TemplateFile options to PaketHelper's Pack command - https://github.com/fsharp/FAKE/pull/854
* Add a UserInputHelper to allow interactive input - https://github.com/fsharp/FAKE/pull/858
* Look for MSTest in VS2015 location - https://github.com/fsharp/FAKE/pull/843
* Add caching to globbing
* BUGFIX: Fix for single * glob not working - https://github.com/fsharp/FAKE/pull/836
* BUGFIX: Get package version from nuspec file - https://github.com/fsharp/FAKE/pull/829
* Report all NuGet errors, even if ExitCode = 0

## 3.35.0 - 2015-06-09

* Added Raygun.io helper - https://github.com/fsharp/FAKE/pull/826
* Re-added internal class generated for AssemblyInfo.vb - https://github.com/fsharp/FAKE/pull/827
* Allow test nUnit test assemblies containing SetupFixture attributes be compatible with NUnitParallel - https://github.com/fsharp/FAKE/pull/824
* Fix FtpHelper
* Trace no. of files in a patch
* CMake support improvements - https://github.com/fsharp/FAKE/pull/821
* Wix Helper Improvements - https://github.com/fsharp/FAKE/pull/818
* Wix Helper Improvements - https://github.com/fsharp/FAKE/pull/817
* Wix Helper Improvements - https://github.com/fsharp/FAKE/pull/815
* Added SemVerHelper.isValidSemVer - https://github.com/fsharp/FAKE/pull/811

## 3.34.0 - 2015-05-25

* Support for CMake configuration and builds - https://github.com/fsharp/FAKE/pull/785
* New task to create C++ AssemblyInfo files - https://github.com/fsharp/FAKE/pull/812
* New environVarOrFail helper - https://github.com/fsharp/FAKE/pull/814
* New WiX helper functions - https://github.com/fsharp/FAKE/pull/804

## 3.33.0 - 2015-05-20

* IMPORTANT: Rewrite of the xUnit tasks. Deprecating existing xUnit and xUnit2 tasks - https://github.com/fsharp/FAKE/pull/800
* Better NUnit docs - https://github.com/fsharp/FAKE/pull/802

## 3.32.4 - 2015-05-18

* Add test adapter path to vs test params - https://github.com/fsharp/FAKE/pull/793
* BUGFIX: Fix WatchChanges on Mac, fix Dispose, improve Timer usage - https://github.com/fsharp/FAKE/pull/799
* REVERT: FCS simplification - https://github.com/fsharp/FAKE/pull/773
* BUGFIX: Don't use MSBuild from invalid path
* BUGFIX: Improved detection of MSBuild.exe on TeamCity - https://github.com/fsharp/FAKE/pull/789

## 3.31.0 - 2015-05-06

* BUGFIX: close stdin in asyncShellExec to avoid hangs - https://github.com/fsharp/FAKE/pull/786
* Fix FAKE not working on machines with only F# 4.0 installed - https://github.com/fsharp/FAKE/pull/784
* Fix for watching files via relative paths - https://github.com/fsharp/FAKE/pull/782
* Fix package id parsing and avoid NPE when feed is missing some properties - https://github.com/fsharp/FAKE/pull/776

## 3.30.1 - 2015-04-29

* FCS simplification - https://github.com/fsharp/FAKE/pull/773
* Paket push task runs in parallel - https://github.com/fsharp/FAKE/pull/768

## 3.29.2 - 2015-04-27

* New file system change watcher - http://fsharp.github.io/FAKE/watch.html
* NuGet pack task treats non csproj files as nuspec files - https://github.com/fsharp/FAKE/pull/767
* New helpers to start and stop DynamicsNAV ServiceTiers
* Automatically replace Win7ToWin8 import files for Dynamics NAV during Import
* OpenSourced DynamicsNAV replacement helpers
* Use Microsoft.AspNet.Razor 2.0.30506 for FAKE.Deploy - https://github.com/fsharp/FAKE/pull/756
* New build parameter functions
* Fix http://stackoverflow.com/questions/29572870/f-fake-unable-to-get-fake-to-merge-placeholder-arguments-in-nuspec-file
* New environment variable helpers

## 3.28.0 - 2015-04-09

* Don't run package restore during MSBuild run from FAKE - https://github.com/fsharp/FAKE/pull/753
* Added support for Mage's CertHash parameter - https://github.com/fsharp/FAKE/pull/750
* Force build server output in xUnit2 if the user wishes to - https://github.com/fsharp/FAKE/pull/749
* Reverting 0df4569b3bdeef99edf2eec6013dab784e338b7e due to backwards compat issues
* Improvements for FAKE.Deploy - https://github.com/fsharp/FAKE/pull/745
* Set debug flag on mono - https://github.com/fsharp/FAKE/pull/744

## 3.27.0 - 2015-04-07

* New Android publisher - http://fsharp.github.io/FAKE/androidpublisher.html
* New Archive helpers allow to build zip, gzip, bzip2, tar, and tar.gz/tar.bz2 - https://github.com/fsharp/FAKE/pull/727
* Download Status Parameter for HockeyAppHelper - https://github.com/fsharp/FAKE/pull/741
* Added more parameters for HockeyApp Upload API - https://github.com/fsharp/FAKE/pull/723
* `NuGetPack` task allows to set framework references - https://github.com/fsharp/FAKE/pull/721
* New task `NuGetPackDirectly` works without template files.
* Find NuGet.exe in current folder (and sub-folders) first, then look in PATH - https://github.com/fsharp/FAKE/pull/718
* New tutorial about Vagrant - http://fsharp.github.io/FAKE/vagrant.html
* REVERTING: SystemRoot also works on mono - https://github.com/fsharp/FAKE/pull/706 (see [https://github.com/fsharp/FAKE/issues/715](https://github.com/fsharp/FAKE/issues/715))
* BUGFIX: Use DocumentNamespace for Nuspec files - https://github.com/fsharp/FAKE/pull/736
* BUGFIX: Display agent success / error messages in UI for FAKE.Deploy - https://github.com/fsharp/FAKE/pull/735
* BUGFIX: Add build directory for doc generation - https://github.com/fsharp/FAKE/pull/734

## 3.26.0 - 2015-03-25

* Detect GitLab CI as build server - https://github.com/fsharp/FAKE/pull/712

## 3.25.2 - 2015-03-24

* Look into PATH when scanning for NuGet.exe - https://github.com/fsharp/FAKE/pull/708
* SystemRoot also works on mono - https://github.com/fsharp/FAKE/pull/706
* Use EditorConfig - http://editorconfig.org/

## 3.25.1 - 2015-03-24

* More AppVeyor properties added - https://github.com/fsharp/FAKE/pull/704

## 3.25.0 - 2015-03-23

* Look into PATH when scanning for tools - https://github.com/fsharp/FAKE/pull/703

## 3.24.0 - 2015-03-22

* BREAKING CHANGE: Better support for AssemblyMetadata in AssemblyInfoHelper - https://github.com/fsharp/FAKE/pull/694
* Added modules for building VB6 projects with SxS manifest - https://github.com/fsharp/FAKE/pull/697
* Use parameter quoting for Paket helpers

## 3.23.0 - 2015-03-12

* BREAKING CHANGE: Adjusted Xamarin.iOS archive helper params - https://github.com/fsharp/FAKE/pull/693
* New operator </> allows to combine paths similar to @@ but with no trimming operations - https://github.com/fsharp/FAKE/pull/695

## 3.22.0 - 2015-03-12

* Globbing allows to grab folders without a trailing slash
* Removed long time obsolete globbing functions

## 3.21.0 - 2015-03-11

* FAKE allows to run parallel builds - http://fsharp.github.io/FAKE/parallel-build.html

## 3.20.1 - 2015-03-10

* Proper source index - https://github.com/fsharp/FAKE/issues/678

## 3.20.0 - 2015-03-10

* Always use FCS in FAKE and FSI in FAke.Deploy
* Modify VM size on a .csdef for Azure Cloud Services - https://github.com/fsharp/FAKE/pull/687
* Added ZipHelper.ZipOfIncludes - https://github.com/fsharp/FAKE/pull/686
* Added AppVeyorEnvironment.RepoTag & .RepoTagName - https://github.com/fsharp/FAKE/pull/685
* New tutorial about Azure Cloud Service - http://fsharp.github.io/FAKE/azurecloudservices.html
* Added basic support for creating Azure Cloud Services - http://fsharp.github.io/FAKE/apidocs/v5/fake-azure-cloudservices.html
* Added metadata property for AssemblyInfoReplacementParams - https://github.com/fsharp/FAKE/pull/675

## 3.18.0 - 2015-03-04

* Remvoved internal class generated in AssemblyInfo.Vb - https://github.com/fsharp/FAKE/pull/673
* Adding ability to control type library export (/tlb flag) of RegAsm - https://github.com/fsharp/FAKE/pull/668
* Adding ability to run nuget package restore on a visual studio solution - https://github.com/fsharp/FAKE/pull/662
* Add OwnerId, type docs, and better error handling for HockeyAppHelper - https://github.com/fsharp/FAKE/pull/661
* Don't report unit test failure twice to TeamCity - https://github.com/fsharp/FAKE/pull/659
* New tasks for `paket pack` and `paket push`- http://fsprojects.github.io/Paket/index.html
* Allow csproj being passed as a NuSpec file - https://github.com/fsharp/FAKE/pull/644
* Helper for uploading mobile apps to HockeyApp - https://github.com/fsharp/FAKE/pull/656
* SCPHelper does allow copying single files - https://github.com/fsharp/FAKE/issues/671
* BUGFIX: Paket helper should not submit the endpoint if no endpoint was given - https://github.com/fsharp/FAKE/issues/667
* BUGFIX: Paket helper should not override version for project packages - https://github.com/fsharp/FAKE/issues/666
* BUGFIX: Allow endpoint in push task - https://github.com/fsprojects/Paket/pull/652
* BUGFIX: Use correct apikey for paket push - https://github.com/fsharp/FAKE/pull/664

## 3.17.0 - 2015-02-12

* Revert to fsi in Fake.Deploy - https://github.com/fsharp/FAKE/pull/653
* Added MergeByHash option for OpenCover - https://github.com/fsharp/FAKE/pull/650
* New functions to replace text in one or more files using regular expressions - https://github.com/fsharp/FAKE/pull/649
* BUGFIX: Fix SpecFlow MSTest integration - https://github.com/fsharp/FAKE/pull/652
* BUGFIX: Fix TeamCity integration - https://github.com/fsharp/FAKE/pull/651

## 3.15.0 - 2015-02-07

* New VSTest module for working with VSTest.Console - https://github.com/fsharp/FAKE/pull/648
* Add Verbose to argument list for NuGet update - https://github.com/fsharp/FAKE/pull/645
* BUGFIX: Fix jarsigner executing on Windows environment - https://github.com/fsharp/FAKE/pull/640
* Adding UploadTestResultsXml function to the AppVeyor module - https://github.com/fsharp/FAKE/pull/636
* Adding the NoDefaultExcludes NugGet parameter - https://github.com/fsharp/FAKE/pull/637
* Adding `SpecificMachines` option to OctoTools - https://github.com/fsharp/FAKE/pull/631
* Allow to run gacutil on mono
* Ignore unknown project references in MSBuild task - https://github.com/fsharp/FAKE/pull/630

## 3.14.0 - 2015-01-14

* BUGFIX: Added a reset step before starting a deployment - https://github.com/fsharp/FAKE/pull/621
* Report fatal git errors to command line

## 3.13.0 - 2015-01-03

* New FAKE.Lib nuget package which contains the FakeLib - https://github.com/fsharp/FAKE/pull/607
* New AppVeyor properties - https://github.com/fsharp/FAKE/pull/605
* Use FSharp.Core from NuGet - https://github.com/fsharp/FAKE/pull/602
* Build and deploy Azure web jobs - https://github.com/fsharp/FAKE/pull/613

## 3.11.0 - 2014-12-03

* Dual-license under Apache 2 and MS-PL, with Apache as default - https://github.com/fsharp/FAKE/pull/598
* BUGFIX: FSC compilation fix - https://github.com/fsharp/FAKE/pull/601
* BUGFIX: Unescape special MSBuild characters - https://github.com/fsharp/FAKE/pull/600

## 3.10.0 - 2014-11-27

* Support for MSBuild 14.0 - https://github.com/fsharp/FAKE/pull/595
* New C# compiler helper - https://github.com/fsharp/FAKE/pull/592/files
* Added support for NUnit Fixture parameter - https://github.com/fsharp/FAKE/pull/591
* OpenSourcing some DynamicsNAV helpers from gitnav
* BUGFIX: Fix 64bit mode
* BUGFIX: Dynamics NAV helper - "Ignored" tests should report the message

## 3.9.0 - 2014-11-07

* Create a new package with a x64 version - https://github.com/fsharp/FAKE/pull/582
* Added a Xamarin.iOS Archiving helper - https://github.com/fsharp/FAKE/pull/581
* DynamicsNAV helper should use the correct ServiveTier

## 3.8.0 - 2014-10-30

* [xUnit 2](http://xunit.github.io/) support - https://github.com/fsharp/FAKE/pull/575
* New RegistryKey helpers for a 64bit System - https://github.com/fsharp/FAKE/pull/580
* New XDTHelper - https://github.com/fsharp/FAKE/pull/556
* Version NAV 800 added - https://github.com/fsharp/FAKE/pull/576
* Feature/list targets in command line - http://fsharp.github.io/FAKE/specifictargets.html
* Use priority list for nuget.exe selection - https://github.com/fsharp/FAKE/issues/572
* BUGFIX: RoundhouseHelper was setting an incorrect switch for CommandTimoutAdmin - https://github.com/fsharp/FAKE/pull/566

## 3.7.0 - 2014-10-16

* BUGFIX: --single-target didn't work
* NDepend support - https://github.com/fsharp/FAKE/pull/564

## 3.6.0 - 2014-10-14

* FAKE got a new logo - https://github.com/fsharp/FAKE/pull/553
* Use Paket to handle dependencies - http://fsprojects.github.io/Paket/
* Single target mode --single-target - http://fsharp.github.io/FAKE/specifictargets.html
* New recursive copy functions - https://github.com/fsharp/FAKE/pull/559
* NuGetPack allows to manipulate nuspec files - https://github.com/fsharp/FAKE/pull/554
* Support for MSpec --xml parameter - https://github.com/fsharp/FAKE/pull/545
* Make GetPackageVersion work with Paket - http://fsprojects.github.io/Paket/
* Added missing schemaName parameter for Roundhouse helper - https://github.com/fsharp/FAKE/pull/551
* Roundhouse Cleanup - https://github.com/fsharp/FAKE/pull/550
* Update FSharp.Compiler.Service to 0.0.62
* BUGFIX: If site exists then the site will be modified by IISHelper with the given parameters - https://github.com/fsharp/FAKE/pull/548
* BUGFIX: Messages in FSC task to stderr stream can break the build - https://github.com/fsharp/FAKE/pull/546
* BUGFIX: Use AppVeyor's build version instead of the build number - https://github.com/fsharp/FAKE/pull/560

## 3.5.0 - 2014-09-19

* Added new SignToolHelper - https://github.com/fsharp/FAKE/pull/535
* Look first in default path for a tool - https://github.com/fsharp/FAKE/pull/542
* Add support for MSBuild Distributed Loggers - https://github.com/fsharp/FAKE/pull/536
* Don't fail on nuget path scanning - https://github.com/fsharp/FAKE/pull/537

## 3.4.0 - 2014-08-28

* New Xamarin.iOS and Xamarin.Android helpers - https://github.com/fsharp/FAKE/pull/527

## 3.3.0 - 2014-08-25

* Using JSON.NET 6.0.4
* FAKE.Deploy switched to FCS - https://github.com/fsharp/FAKE/pull/519
* FAKE.Deploy WorkDirectory fix - https://github.com/fsharp/FAKE/pull/520
* HipChat notification helper - https://github.com/fsharp/FAKE/pull/523
* Don't crash during tool discovery
* NuGet: support fallback framework groups - https://github.com/fsharp/FAKE/pull/514
* New pushd/popd command in FileUtils - https://github.com/fsharp/FAKE/pull/513
* New AppVeyor properties
* FSC - support of compilation for different versions of F#
* Provide env var access to --fsiargs build script args so works on FAKE
* Adding NGen Install task
* Allow to use gacutil
* Allow to use ngen.exe
* Allow to use all sn.exe features
* Adding DisableVerification for StrongNames
* Adding helpers which allow to strong name assemblies
* Allow to use empty MSBuild targets
* Adding setProcessEnvironVar and clearProcessEnvironVar
* Try to reference local nuspec in order to fix https://github.com/fsprojects/FSharp.TypeProviders.StarterPack/pull/33
* Better log messages to fix https://github.com/fsprojects/FSharp.TypeProviders.StarterPack/pull/33
* Fix fsiargs and -d options - https://github.com/fsharp/FAKE/pull/498 https://github.com/fsharp/FAKE/pull/500
* Change RemoveDuplicateFiles & FixMissingFiles to only save on change - https://github.com/fsharp/FAKE/pull/499

## 3.2.0 - 2014-07-07

* BREAKING CHANGE: API for CreateAssemblyInfoWithConfig was set back to original version
  This resets the breaking change introduced in https://github.com/fsharp/FAKE/pull/471
* Automatic tool search for SpecFlowHelper - https://github.com/fsharp/FAKE/pull/496
* GuardedAwaitObservable was made public by accident - this was fixed
* Add support for remote service admin - https://github.com/fsharp/FAKE/pull/492

## 3.1.0 - 2014-07-04

* New FSC helper allows to call F# compiler directly from FAKE - https://github.com/fsharp/FAKE/pull/485
* "CustomDictionary" support for FxCop - https://github.com/fsharp/FAKE/pull/489
* Check if file exists before delete in AssemblyInfoFile
* Use FSharp.Compiler.Service 0.0.58
* Report all targets if a target error occurs
* Use FSharp.Compiler.Service with better FSharp.Core resolution - https://github.com/fsharp/FSharp.Compiler.Service/issues/156
* Don't break in MSBuildHelper
* Put FSharp.Core.optdata and FSharp.Core.sigdata into nuget package
* Fixed TargetTracing
* Fixed SourceLinking of FAKE
* Added new exception trap for Fsi creation
* -br in command line will run debugger in F# scripts - https://github.com/fsharp/FAKE/pull/483
* Null check in NuGet helper - https://github.com/fsharp/FAKE/pull/482

## 3.0.0 - 2014-06-27

* Use FSharp.Compiler.Service 0.0.57 instead of fsi.exe
* Better error message for registry access
* Fall back to 32bit registry keys if 64bit cannot be found
* Improved SqlServer Disconnect error message
* Log "kill all processes" only when needed
* Try to run as x86 due to Dynamics NAV problems
* Allow to use /gac for FxCop
* Make NuGet description fit into single line
* Use Nuget.Core 2.8.2
* Fix NUnitProcessModel.SeparateProcessModel - https://github.com/fsharp/FAKE/pull/474
* Improved CLI documentation - https://github.com/fsharp/FAKE/pull/472
* Added Visual Basic support to AssemblyFileInfo task and make Namespace optional in config - https://github.com/fsharp/FAKE/pull/471
* Added support for OctoTools ignoreExisting flag - https://github.com/fsharp/FAKE/pull/470
* OctoTools samples fixed - https://github.com/fsharp/FAKE/pull/468 https://github.com/fsharp/FAKE/pull/469
* Added support for FxCop /ignoregeneratedcode parameter - https://github.com/fsharp/FAKE/pull/467
* CreateAssemblyInfo works with nonexisting directories - https://github.com/fsharp/FAKE/pull/466

## 2.18.0 - 2014-06-11

* New (backwards compat) CLI for FAKE that includes FSI cmd args passing - https://github.com/fsharp/FAKE/pull/455
* New updateApplicationSetting method - https://github.com/fsharp/FAKE/pull/462
* Support for msbuild /noconlog - https://github.com/fsharp/FAKE/pull/463
* RoundhouseHelper - https://github.com/fsharp/FAKE/pull/456
* Pass optional arguments to deployment scripts
* Support building source packages without project file
* Display messages when deploy fails
* Fix formatting in FAKE.Deploy docs
* Fix memory usage in FAKE.Deploy
* Increase WebClient's request timeout to 20 minutes - https://github.com/fsharp/FAKE/pull/442
* Mainly Layout fixes and disabling authenticate in FAKE.Deploy https://github.com/fsharp/FAKE/pull/441
* Deploy PDBs via nuget https://github.com/fsharp/FAKE/issues/435
* Release Notes parser should not drop asterisk at end of lines
* Corrected location of @files@ in nuspec sample
* Allow to report tests to AppVeyor
* fix appveyor msbuild logger
* Don't add Teamcity logger if not needed

## 2.17.0 - 2014-05-23

* Fake.Deploy agent requires user authentication
* Remove AutoOpen von AppVeyor
* fix order of arguments in call to CopyFile
* Support MSTest test settings - https://github.com/fsharp/FAKE/pull/428
* If the NAV error file contains no compile errors return the length

## 2.16.0 - 2014-05-21

* Promoted the master branch as default branch and removed develop branch
* Remove AutoOpen from TaskRunnerHelper
* Adding Metadata to AsssemblyInfo
* Analyze the Dynamics NAV log file and report the real error count
* Allow to retrieve version no. from assemblies
* Fix issue with symbol packages in NugetHelper
* Fix issues in the ProcessHelper - https://github.com/fsharp/FAKE/pull/412 and https://github.com/fsharp/FAKE/pull/411
* Allow to register BuildFailureTargets - https://github.com/fsharp/FAKE/issues/407
* UnionConverter no longer needed for Json.Net

## 2.15.0 - 2014-04-24

* Handle problems with ProgramFilesX86 on mono - https://github.com/tpetricek/FsLab/pull/32
* Change the MSBuild 12.0 path settings according to https://github.com/tpetricek/FsLab/pull/32
* Silent mode for MSIHelper - https://github.com/fsharp/FAKE/issues/400

## 2.14.0 - 2014-04-22

* Support for OpenCover - https://github.com/fsharp/FAKE/pull/398
* Support for ReportsGenerator - https://github.com/fsharp/FAKE/pull/399
* Adding AppVeyor environment variables
* New BulkReplaceAssemblyInfoVersions task - https://github.com/fsharp/FAKE/pull/394
* Fixed default nuspec file
* "Getting started" tutorial uses better folder structure
* Allows explicit file specification on the NuGetParams Type
* Fix TypeScript output dir
* Add better docs for the TypeScript compiler.
* Don't call the TypeScript compiler more than once
* New parameters for TypeScript

## 2.13.0 - 2014-04-04

* Enumerate the files lazily in the File|Directory active pattern
* Using Nuget 2.8.1
* Added TypeScript 1.0 support
* Added TypeScript support
* Fixed ProcessTestRunner
* Fixed mono build on Travis

## 2.12.0 - 2014-03-31

* Add getDependencies to NugetHelper
* SourceLink support
* NancyFx instead of ASP.NET MVC for Fake.Deploy
* Allows to execute processes as unit tests.
* Adding SourceLinks
* Move release management back to the local machine (using this document)
* Allow to run MsTest test in isolation
* Fixed Nuget.packSymbols
* Fixed bug in SemVer parser
* New title property in Nuspec parameters
* Added option to disabled FAKE's automatic process killing
* Better AppyVeyor integration
* Added ability to define custom MSBuild loggers
* Fix for getting the branch name with Git >= 1.9
* Added functions to write and delete from registry
* NUnit NoThread, Domain and StopOnError parameters
* Add support for VS2013 MSTest
* Lots of small fixes

## 2.2.0

* Created new packages on nuget:
  * Fake.Deploy - allows to use FAKE scripts in deployment.
  * Fake.Experimental - new stuff where we aren't sure if we want to support it.
  * Fake.Gallio - contains the Gallio runner support.
  * Fake.SQL - Contains tasks for SQL Server.
  * Fake.Core - All the basic features and FAKE.exe.
* Created documentation and tutorials - see http://fsharp.github.io/FAKE/
* New tasks:
  * Added ReleaseNotes parser
  * Added Dynamics NAV helper
  * Added support for MSTest and fixie
  * Parallel NUnit task
  * New AssemblyInfoFile task
  * Support for Octopus Deploy
  * Support for MAGE
  * Suppport for Xamarin's xpkg
  * Many other new tasks
* Fake.Boot
* New Globbing system
* Tons of bug fixes
* Bundles F# 3.0 compiler and FSI.

## 1.72.0

* "RestorePackages" allows to restore nuget packages

## 1.70.0

* FAKE nuget package comes bundles with a fsi.exe
* Self build downloads latest FAKE master via nuget

## 1.66.1

* Fixed bug where FAKE.Deploy didn't run the deploy scripts where used as a windows service
* It's possible to add file loggers for MSBuild
* Fixed path resolution for fsi on *nix
* BREAKING CHANGE: Removed version normalization from NuGet package creation
* Fixes for NUNit compatibility on mono
* Fixes in ProcessHelper for mono compatibility
* Fixes in the mono build
* Improved error reporting in Fake.exe
* Added a SpecFlow helper
* Fixed some issues in file helper routines when working with no existing directory chain

## 1.64.1

* Fixed bug where FAKE didn't run the correct build script

## 1.64.0

* New conditional dependency operator =?>
* BREAKING CHANGE: Some AssemblyInfo task parameters are now option types. See type hints.

## 1.62.0

* New RegAsm task, allows to create TLBs from a dll.
* New MSI task, allows to install or uninstall msi files.
* StringHelper.NormalizeVersion fixed for WiX.

## 1.58.9

* Allow to choose specific nunit-console runner.

## 1.58.6

* Using nuget packages for mspec.
* FAKE tries to kill all MSBuild and FSI processes at the end of a build.

## 1.58.1

* Removed message system for build output. Back to simpler tracing.

## 1.58.0

* ReplaceAssemblyInfoVersions task allows to replace version info in AssemblyVersion-files
* New task ConvertFileToWindowsLineBreaks

## 1.56.10

* Allows to build .sln files

## 1.56.0

* Allows to publish symbols via nuget.exe
* Autotrim trailing .0 from version in order to fullfill nuget standards.

## 1.54.0

* If the publishment of a Nuget package fails, then FAKE will try it again.
* Added Changelog.markdown to FAKE deployment
* Added RequireExactly helper function in order to require a specific nuget dependency.
* NugetHelper.GetPackageVersion - Gets the version no. for a given package in the packages folder.
* EnvironmentHelper.getTargetPlatformDir - Gets the directory for the given target platform.

## 1.52.0

* Some smaller bugfixes
* New dependency syntax with ==> and <=>
* Tracing of StackTrace only if TargetHelper.PrintStackTraceOnError was set to true

## 1.50.0

* New task DeleteDirs allows to delete multiple directories.
* New parameter for NuGet dependencies.

## 1.48.0

* Bundled with docu.exe compiled against .Net 4.0.
* Fixed docu calls to run with full filenames.
* Added targetplatform, target and log switches for ILMerge task.
* Added Git.Information.getLastTag() which gets the last git tag by calling git describe.
* Added Git.Information.getCurrentHash() which gets the last current sha1.

## 1.46.0

* Fixed Nuget support and allows automatic push.

## 1.44.0

* Tracing of all external process starts.
* MSpec support.
