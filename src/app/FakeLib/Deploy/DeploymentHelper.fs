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
    !! (dir @@ deploymentRootDir @@ "**/active/*.nupkg")
      |> Seq.map extractNuspecFromPackageFile

let getActiveReleaseFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/active/*.nupkg") 
      |> Seq.map extractNuspecFromPackageFile
      |> Seq.head

let getAllReleases dir = 
    !! (dir @@ deploymentRootDir @@ "**/*.nupkg")
      |> Seq.map extractNuspecFromPackageFile

let getAllReleasesFor dir (app : string) = 
    !! (dir @@ deploymentRootDir + app + "/**/*.nupkg") 
      |> Seq.map extractNuspecFromPackageFile

let getStatistics() = 
    getMachineEnvironment()

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
        let (result, messages) = FSIHelper.executeFSI workingDirectory (FullName script) Seq.empty 
        if result then 
            Success { Messages = messages; IsError = false; Exception = null }
        else 
            Failure { Messages = messages; IsError = true; Exception = (Exception "Deployment script didn't run successfully") }
    with e ->
        Failure { Messages = Seq.empty; IsError = true; Exception = e }
              
let runDeploymentFromPackageFile workDir packageFileName =
    try
      let packageBytes =  ReadFileAsBytes packageFileName
      let package,scriptFile = unpack workDir false packageBytes
      doDeployment package.Name scriptFile        
    with e ->
       Failure { Messages = Seq.empty; IsError = true; Exception = e }

let rollback workDir (app : string) (version : string) =
    try 
        let currentPackageFileName = 
            !! (workDir @@ deploymentRootDir + app + "/active/*.nupkg") 
            |> Seq.head

        let backupPackageFileName = getBackupFor workDir app version
        if currentPackageFileName = backupPackageFileName 
        then Failure { Messages = Seq.empty; IsError = true; Exception = (Exception "Cannot rollback to currently active version") }
        else 
            let package,scriptFile = unpack workDir true (backupPackageFileName |> ReadFileAsBytes)
            doDeployment package.Name scriptFile
    with
        | :? FileNotFoundException as e ->
            let msg = sprintf "Failed to rollback to %s %s could not find package file or deployment script file ensure the version is within the backup directory and the deployment script is in the root directory of the *.nupkg file" app version
            Failure { Messages = [{ IsError = true; Message = "Rollback failed: File not found"; Timestamp = DateTimeOffset.UtcNow }]; IsError = true; Exception = (Exception msg) }
        | _ as e -> 
            Failure { Messages = [{ IsError = true; Message = "Rollback failed"; Timestamp = DateTimeOffset.UtcNow }]; IsError = true; Exception = e }

let getVersionFromNugetFileName (app:string) (fileName:string) = 
    Path.GetFileName(fileName).ToLower().Replace(".nupkg","").Replace(app.ToLower() + ".","")

let getPreviousPackageVersionFromBackup dir app versions = 
    let rootPath = dir @@ deploymentRootDir + app
    let currentPackageFileName = 
        !! (rootPath + "/active/*.nupkg") 
        |> Seq.head 
        |> getVersionFromNugetFileName app

    let backupPath = rootPath + "/backups/"

    !! (backupPath + "*.nupkg")
        |> Seq.map (getVersionFromNugetFileName app)
        |> Seq.filter (fun x -> x < currentPackageFileName)
        |> Seq.toList
        |> List.sort
        |> List.rev
        |> Seq.skip (versions - 1)
        |> Seq.head

let rollbackTo workDir app version =
    try
        let newVersion =
            match VersionInfo.Parse version with
            | Specific version -> version
            | Predecessor p -> getPreviousPackageVersionFromBackup workDir app p

        rollback workDir app newVersion
    with e ->
        Failure { Messages = [{ IsError = true; Message = sprintf "Rollback to version (%s-%s) failed" app version; Timestamp = DateTimeOffset.UtcNow }]; IsError = true; Exception = e }