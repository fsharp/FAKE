﻿module Fake.DeploymentHelper
    
open System
open System.Configuration
open System.IO
open System.Net
open Fake
open Fake.HttpClientHelper

type VersionInfo =
| Specific of string
| Predecessor of int
    with 
        static member Parse(s:string) =
            let s = s.ToLower()
            let predecessorPrefix = "head~"
            if s.StartsWith predecessorPrefix then 
                s.Replace(predecessorPrefix,"") |> Int32.Parse |> Predecessor 
            else
                Specific s

let private extractNuspecFromPackageFile packageFileName =   
    packageFileName
    |> ZipHelper.UnzipFirstMatchingFileInMemory (fun ze -> ze.Name.EndsWith ".nuspec") 
    |> NuGetHelper.getNuspecProperties

let mutable deploymentRootDir = "deployments/"

let getActiveReleases dir = 
    Files [dir] [deploymentRootDir @@ "**/active/*.nupkg"] []
    |> Seq.map extractNuspecFromPackageFile

let getActiveReleaseFor dir (app : string) = 
    Files [dir] [deploymentRootDir + app + "/active/*.nupkg"] []
    |> Seq.map extractNuspecFromPackageFile
    |> Seq.head

let getAllReleases dir = 
    Files [dir] [deploymentRootDir @@ "**/*.nupkg"] [] 
    |> Seq.map extractNuspecFromPackageFile

let getAllReleasesFor dir (app : string) = 
    Files [dir] [deploymentRootDir + app + "/**/*.nupkg"] [] 
    |> Seq.map extractNuspecFromPackageFile

let getBackupFor dir (app : string) (version : string) =
    let backupFileName =  app + "." + version + ".nupkg"
    dir @@ deploymentRootDir @@ app @@ "backups"
    |> FindFirstMatchingFile backupFileName

let unpack workDir isRollback packageBytes =
    let tempFile = Path.GetTempFileName()
    WriteBytesToFile tempFile packageBytes

    let package = extractNuspecFromPackageFile tempFile   
        
    let activeDir = workDir @@ deploymentRootDir @@ package.Id @@ "active"   
    let newActiveFilePath = activeDir @@ package.FileName

    match TryFindFirstMatchingFile "*.nupkg" activeDir with
    | Some activeFilePath ->
        let backupDir = workDir @@ deploymentRootDir @@ package.Id @@ "backups"
    
        ensureDirectory backupDir
        if not isRollback then
            MoveFile backupDir activeFilePath
    | None -> ()
    
    CleanDir activeDir
    Unzip activeDir tempFile
    File.Delete tempFile

    WriteBytesToFile newActiveFilePath packageBytes

    let scriptFile = FindFirstMatchingFile "*.fsx" activeDir
    package, scriptFile
    
let doDeployment packageName script =
    try
        let workingDirectory = DirectoryName script
        
        if FSIHelper.runBuildScriptAt workingDirectory true (FullName script) Seq.empty then 
            Success
        else 
            Failure(Exception "Deployment script didn't run successfully")
    with e -> Failure e
              
let runDeploymentFromPackageFile workDir packageFileName =
    try
      let packageBytes =  ReadFileAsBytes packageFileName
      let package,scriptFile = unpack workDir false packageBytes
      doDeployment package.Name scriptFile        
    with e -> Failure e

let rollback dir workDir (app : string) (version : string) =
    try 
        let currentPackageFileName = 
            Files [dir] [deploymentRootDir + app + "/active/*.nupkg"] []
            |> Seq.head

        let backupPackageFileName = getBackupFor dir app version
        if currentPackageFileName = backupPackageFileName then 
            Failure "Cannot rollback to currently active version"
        else 
            let package,scriptFile = unpack workDir true (backupPackageFileName |> ReadFileAsBytes)
            match doDeployment package.Name scriptFile with
            | Success -> RolledBack
            | x -> x
    with
        | :? FileNotFoundException as e -> Failure (sprintf "Failed to rollback to %s %s could not find package file or deployment script file ensure the version is within the backup directory and the deployment script is in the root directory of the *.nupkg file" app version)
        | _ as e -> Failure("Rollback failed: " + e.Message)

let getVersionFromNugetFileName (app:string) (fileName:string) = 
    Path.GetFileName(fileName).ToLower().Replace(".nupkg","").Replace(app.ToLower() + ".","")

let getPreviousPackageVersionFromBackup dir app versions = 
    let currentPackageFileName = 
        Files [dir] [deploymentRootDir + app + "/active/*.nupkg"] []
        |> Seq.head 
        |> getVersionFromNugetFileName app

    Files [dir] [deploymentRootDir + app + "/backups/*.nupkg"] []
    |> Seq.map (getVersionFromNugetFileName app)
    |> Seq.filter (fun x -> x < currentPackageFileName)
    |> Seq.toList
    |> List.sort
    |> List.rev
    |> Seq.skip (versions - 1)
    |> Seq.head

let rollbackTo dir app version =
    let newVersion =
        match VersionInfo.Parse version with
        | Specific version -> version
        | Predecessor p -> getPreviousPackageVersionFromBackup dir app p

    rollback dir app newVersion