﻿/// Contains tasks for building Xamarin.iOS and Xamarin.Android apps
module Fake.XamarinHelper

open System
open System.IO
open System.Text.RegularExpressions

let private executeCommand command args =
    Shell.Exec(command, args)
    |> fun result -> if result <> 0 then failwithf "%s exited with error %d" command result

/// The package restore paramater type
type XamarinComponentRestoreParams = {
    /// Path to xamarin-component.exe, defaults to checking tools/xpkg
    ToolPath: string
}

/// The default package restore parameters
let XamarinComponentRestoreDefaults = {
    ToolPath = findToolInSubPath "xamarin-component.exe" (currentDirectory @@ "tools" @@ "xpkg")
}

/// Restores NuGet packages and Xamarin Components for a project or solution
/// ## Parameters
///  - `setParams` - Function used to override the default package restore parameters
let RestoreComponents setParams projectFile =
    let restoreComponents project param =
        executeCommand param.ToolPath ("restore " + project)

    XamarinComponentRestoreDefaults
    |> setParams
    |> restoreComponents projectFile

/// The iOS build paramater type
type iOSBuildParams = {
    /// (Required) Path to solution or project file
    ProjectPath: string
    /// Build target, defaults to Build
    Target: string
    /// Build configuration, defaults to 'Debug|iPhoneSimulator'
    Configuration: string
    /// Path to mdtool, defaults to Xamarin Studio's usual path
    MDToolPath: string
}

/// The default iOS build parameters
let iOSBuildDefaults = {
    ProjectPath = ""
    Target = "Build"
    Configuration = "Debug|iPhoneSimulator"
    MDToolPath = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool"
}

/// Builds a project or solution using Xamarin's iOS build tools
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
let iOSBuild setParams =
    let validateParams param =
        if param.ProjectPath = "" then failwith "You must specify a project to build"

        param

    let buildProject param =
        let args = String.Format(@"-v build -t:{0} ""-c:{1}"" {2}", param.Target, param.Configuration, param.ProjectPath)
        executeCommand param.MDToolPath args

    iOSBuildDefaults
    |> setParams
    |> validateParams
    |> buildProject

type AndroidAbiTarget = 
    | X86
    | ArmEabi
    | ArmEabiV7a
    | Universal

type AndroidPackageAbiParam = 
    | OneApkForAll
    | SpecificAbis of AndroidAbiTarget list

let AllAndroidAbiTargets = 
    AndroidPackageAbiParam.SpecificAbis
        ( [ AndroidAbiTarget.X86
            AndroidAbiTarget.ArmEabi
            AndroidAbiTarget.ArmEabiV7a
            AndroidAbiTarget.Universal ] )

/// The Android packaging parameter type
type AndroidPackageParams = {
    /// (Required) Path to the Android project file (not the solution file!)
    ProjectPath: string
    /// Build configuration, defaults to 'Release'
    Configuration: string
    /// Output path for build, defaults to 'bin/Release'
    OutputPath: string
    /// Build an APK Targetting One ABI (used to reduce the size of the APK and support different CPU architectures)
    PackageAbiTargets : AndroidPackageAbiParam
}

/// The default Android packaging parameters
let AndroidPackageDefaults = {
    ProjectPath = ""
    Configuration = "Release"
    OutputPath = "bin/Release"
    PackageAbiTargets = AndroidPackageAbiParam.OneApkForAll
}

/// Packages a Xamarin.Android app, returning a FileInfo object for the unsigned APK file
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
let AndroidPackage setParams =
    let validateParams param =
        if param.ProjectPath = "" then failwith "You must specify a project to package"
        param

    let buildPackages param = 
        MSBuild param.OutputPath "PackageForAndroid" [ "Configuration", param.Configuration ] [ param.ProjectPath ] |> ignore

    let getPattern target = 
        match target with
            | AndroidAbiTarget.X86 -> "*-x86.apk"
            | AndroidAbiTarget.ArmEabi -> "*-armeabi.apk"
            | AndroidAbiTarget.ArmEabiV7a -> "*-armeabi-v7a.apk"
            | _ -> "*.apk"

    let mostRecentFileInDirMatching path target = 
        let pattern = target |> getPattern
        directoryInfo path
        |> filesInDirMatching pattern
        |> Seq.filter (
                fun file -> if target = AndroidAbiTarget.Universal then 
                                file.Name.EndsWith("-x86.apk") |> not 
                                    && file.Name.EndsWith("-armeabi.apk") |> not
                                    && file.Name.EndsWith("-armeabi-v7a.apk") |> not
                            else true )
        |> Seq.sortBy (fun file -> file.LastWriteTime)
        |> Seq.last

    let createPackage param =
        param |> buildPackages |> ignore
        [ mostRecentFileInDirMatching param.OutputPath  AndroidAbiTarget.Universal ]

    let createPackageAbiSpecificApk (param, targets) =
        param |> buildPackages |> ignore
        let dir = directoryInfo param.OutputPath
        seq { for t in targets do 
                yield mostRecentFileInDirMatching param.OutputPath t
            } |> Seq.toList

    let param = AndroidPackageDefaults |> setParams |> validateParams

    match param.PackageAbiTargets with
        | AndroidPackageAbiParam.OneApkForAll -> param |> createPackage
        | AndroidPackageAbiParam.SpecificAbis targets -> (param,targets) |> createPackageAbiSpecificApk 

// Parameters for signing and aligning an Android package
type AndroidSignAndAlignParams = {
    /// (Required) Path to keystore used to sign the app
    KeystorePath: string
    /// (Required) Password for keystore
    KeystorePassword: string
    /// (Required) Alias for keystore
    KeystoreAlias: string
    /// Path to jarsigner tool, defaults to assuming it is in your path
    JarsignerPath: string
    /// Path to zipalign tool, defaults to assuming it is in your path
    ZipalignPath: string
}

/// The default Android signing and aligning parameters
let AndroidSignAndAlignDefaults = {
    KeystorePath = ""
    KeystorePassword = ""
    KeystoreAlias = ""
    JarsignerPath = "jarsigner"
    ZipalignPath = "zipalign"
}

/// Signs and aligns a Xamarin.Android package, returning a FileInfo object for the signed APK file
/// ## Parameters
///  - `setParams` - Function used to override the default build parameters
///  - `apkFile` - FileInfo object for an unsigned APK file to sign and align
let AndroidSignAndAlign setParams apkFiles =
    let validateParams param =
        if param.KeystorePath = "" then failwith "You must specify a keystore to use"
        if param.KeystorePassword = "" then failwith "You must provide the keystore's password"
        if param.KeystoreAlias = "" then failwith "You must provide the keystore's alias"

        param
    
    let quotesSurround (s:string) = if EnvironmentHelper.isMono then sprintf "'%s'" s else sprintf "\"%s\"" s
    
    let signAndAlign (file:FileInfo) (param:AndroidSignAndAlignParams) =
        let fullSignedFilePath = Regex.Replace(file.FullName, ".apk$", "-Signed.apk")
        let jarsignerArgs = String.Format("-sigalg SHA1withRSA -digestalg SHA1 -keystore {0} -storepass {1} -signedjar {2} {3} {4}", 
                                quotesSurround(param.KeystorePath), param.KeystorePassword, quotesSurround(fullSignedFilePath), file.FullName, param.KeystoreAlias)
        
        executeCommand param.JarsignerPath jarsignerArgs

        let fullAlignedFilePath = Regex.Replace(fullSignedFilePath, "-Signed.apk$", "-SignedAndAligned.apk")
        let zipalignArgs = String.Format("-f -v 4 {0} {1}", fullSignedFilePath, fullAlignedFilePath)
        executeCommand param.ZipalignPath zipalignArgs

        fileInfo fullAlignedFilePath

    [ for apkFile in apkFiles ->
        AndroidSignAndAlignDefaults
            |> setParams
            |> validateParams
            |> signAndAlign apkFile ]

/// The iOS archive paramater type
type iOSArchiveParams = {
    /// Path to desired solution file. If not provided, mdtool finds the first solution in the current directory.
    /// Although mdtool can take a project file, the archiving seems to fail to work without a solution.
    SolutionPath: string
    /// Project name within a solution file
    ProjectName: string
    /// Build configuration, defaults to 'Debug|iPhoneSimulator'
    Configuration: string
    /// Path to mdtool, defaults to Xamarin Studio's usual path
    MDToolPath: string
}

/// The default iOS archive parameters
let iOSArchiveDefaults = {
    SolutionPath = ""
    ProjectName = ""
    Configuration = "Debug|iPhoneSimulator"
    MDToolPath = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool"
}
    
/// Archive a project using Xamarin's iOS archive tools
/// ## Parameters
///  - `setParams` - Function used to override the default archive parameters
let iOSArchive setParams =
    let archiveProject param =
        let projectNameArg = if param.ProjectName <> "" then String.Format("-p:{0} ", param.ProjectName) else ""
        let args = String.Format(@"-v archive ""-c:{0}"" {1}{2}", param.Configuration, projectNameArg, param.SolutionPath)
        executeCommand param.MDToolPath args

    iOSArchiveDefaults
        |> setParams
        |> archiveProject