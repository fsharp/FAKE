﻿module Fake.DeploymentHelper
    
    open System
    open System.IO
    open System.Net
    open Fake
    open Newtonsoft.Json

    type DeploymentResponse = {
            Success : bool
            Id : string
            Version : string
            Error : obj
        }
        with 
            static member Sucessful(packageId, version) = 
                {
                    Success = true;
                    Id = packageId;
                    Version = version;
                    Error = null;
                }

            static member Failure(packageId, version, error) =
                {
                    Success = false;
                    Id = packageId;
                    Version = version;
                    Error = error;
                }

            override x.ToString() = 
                if x.Success 
                then sprintf "Deployment of %s %s successful" x.Id x.Version
                else sprintf "Deployment of %s %s failed\n\n%A" x.Id x.Version x.Error

    type DeploymentPackage = {
            Id : string
            Version : string
            Script : byte[]
            Package : byte[]
        }
        with
            member x.TargetDir = 
                x.Id + "_" + (x.Version.Replace('.','_'))

            override x.ToString() = 
                x.Id + " " + x.Version

    let createDeploymentPackageFromZip packageName version fakescript archive output =
        ensureDirectory output
        let package = {
            Id = packageName
            Version = version
            Script = File.ReadAllBytes(Path.GetFullPath(fakescript))
            Package = File.ReadAllBytes(Path.GetFullPath(archive))
        }
        IO.File.WriteAllBytes(Path.Combine(output,packageName + ".fakepkg"), Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(package)))
        File.Delete(archive)

    let createDeploymentPackageFromDirectory packageName version fakescript dir output =
        let archive = packageName + ".zip"
        let files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
        Zip dir archive files
        createDeploymentPackageFromZip packageName version fakescript archive output

    let ensureDeployDir (package : DeploymentPackage) = 
        let path = Path.Combine("Work", package.TargetDir)
        ensureDirectory path
        path,package

    let unpack (dir,package) =
        let archive = package.Id + ".zip"
        File.WriteAllBytes(archive, package.Package)
        Unzip dir archive
        File.Delete archive
        let script = Path.Combine(dir, package.Id + ".fsx")
        File.WriteAllBytes(script, package.Script)
        script, package

    let prepare = ensureDeployDir >> unpack
    
    let doDeployment package = 
       let (script, _) = prepare package
       let workingDirectory = Path.GetDirectoryName(script)
       let fakeLibTarget = Path.Combine(workingDirectory, "FakeLib.dll")
       if  not <| File.Exists(fakeLibTarget) then File.Copy("FakeLib.dll", fakeLibTarget)
       (FSIHelper.runBuildScriptAt workingDirectory true (Path.GetFullPath(script)) Seq.empty, package)
       
    let runDeployment package = 
        try
            doDeployment package |> Choice1Of2
        with e ->
            Choice2Of2(e)

    let runDeploymentFromPackage packagePath = 
        try
            runDeployment (JsonConvert.DeserializeObject<DeploymentPackage>(File.ReadAllText(packagePath)))
        with e -> 
            Choice2Of2(e)


    let postDeploymentPackage url packagePath = 
        let result = ref None
        let waitHandle = new Threading.AutoResetEvent(false)
        let handle (event : UploadDataCompletedEventArgs) =
            if event.Cancelled 
            then 
                result := Some <| Choice2Of2(OperationCanceledException() :> exn)
                waitHandle.Set() |> ignore
            elif event.Error <> null
            then 
                result := Some <| Choice2Of2(event.Error)
                waitHandle.Set() |> ignore
            else
                use ms = new MemoryStream(event.Result)
                use sr = new StreamReader(ms, Text.Encoding.UTF8)
                let res = sr.ReadToEnd()
                result := Some <| Choice1Of2(JsonConvert.DeserializeObject<DeploymentResponse>(res))
                waitHandle.Set() |> ignore

        let uri = new Uri(url, UriKind.Absolute)
        let client = new WebClient()
        let mutable uploaded = false
        client.Headers.Add(HttpRequestHeader.ContentType, "application/fake")
        client.UploadDataCompleted |> Event.add handle
        client.UploadDataAsync(uri, "POST", File.ReadAllBytes(packagePath))
        waitHandle.WaitOne() |> ignore
        !result
        
        

     
        



