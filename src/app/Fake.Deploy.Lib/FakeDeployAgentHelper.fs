﻿/// Contains a http helper functions for FAKE.Deploy.
module Fake.FakeDeployAgentHelper

open System
open System.IO
open System.Net
open System.Text
open System.Web
open HttpListenerHelper
open Fake.SshRsaModule

/// Authentication token received from a successful login
type AuthToken = 
    | AuthToken of Guid

let mutable private authToken : Guid option = None

/// A http response type.
type Response = 
    { Messages : seq<Fake.ProcessHelper.ConsoleMessage>
      Exception : obj
      IsError : bool }

/// Deployment result type.
type DeploymentResponse = 
    | Success of Response
    | Failure of Response
    | QueryResult of seq<NuSpecPackage>

let private wrapFailure = 
    function 
    | Choice1Of2(Message msg) -> msg
    | Choice1Of2(Exception exn) -> 
        { Messages = Seq.empty
          IsError = true
          Exception = exn }
        |> Failure
    | Choice2Of2 exn -> 
        { Messages = Seq.empty
          IsError = true
          Exception = exn }
        |> Failure

type Url = string
type Action = string
type FilePath = string

type Deployment = 
    { PackageFileName : FilePath
      Url : Url
      Timeout : TimeSpan
      Arguments : string list
      AuthToken : AuthToken option }

let private defaultTimeout = TimeSpan.FromMinutes(20.)

let private defaultDeployment = 
    { PackageFileName = ""
      Url = ""
      Timeout = defaultTimeout
      Arguments = []
      AuthToken = None }

[<Literal>]
let scriptArgumentsHeaderName = "X-FAKE-Script-Arguments"

let private webRequest (url : Url) (action : Action) (timeout : TimeSpan) = 
    let req = WebRequest.Create url :?> HttpWebRequest
    req.Method <- action
    req.Timeout <- int timeout.TotalMilliseconds
    req.ContentType <- "application/fake"
    req.Headers.Add("fake-deploy-use-http-response-messages", "true")
    match authToken with
    | None -> ()
    | Some t -> req.Headers.Add("AuthToken", string t)
    req

let private downloadString (request : HttpWebRequest) = 
    use responseStream = request.GetRequestStream()
    use ms = new MemoryStream()
    responseStream.CopyTo ms
    Encoding.UTF8.GetString(ms.ToArray())

/// Gets the http response from the given URL and runs it with the given function.
let private get timeout f url = 
    try 
        let msg = webRequest url "GET" timeout |> downloadString
        try 
            match msg |> Json.deserialize with
            | Message msg -> 
                f msg
                |> Message
                |> Choice1Of2
            | Exception exn -> Exception exn |> Choice1Of2
        with _ -> 
            f msg
            |> Message
            |> Choice1Of2
    with exn -> Choice2Of2 exn

let private uploadData (action : Action) (url : Url) (body : byte []) timeout = 
    let req = webRequest url action timeout
    use reqStream = req.GetRequestStream()
    reqStream.Write(body, 0, body.Length)
    use respStream = req.GetResponse().GetResponseStream()
    let ms = new MemoryStream()
    respStream.CopyTo ms
    ms.ToArray()

let private uploadFile (action : Action) (url : Url) (file : FilePath) (args : string []) timeout = 
    let req = webRequest url action timeout
    req.Headers.Add(scriptArgumentsHeaderName, args |> toHeaderValue)
    req.AllowWriteStreamBuffering <- false
    use fileStream = File.OpenRead file
    req.ContentLength <- fileStream.Length
    use reqStream = req.GetRequestStream()
    fileStream.CopyTo reqStream
    use respStream = req.GetResponse().GetResponseStream()
    let ms = new MemoryStream()
    respStream.CopyTo ms
    ms.ToArray()

/// sends the given body using the given action (POST or PUT) to the given url
let private processResponse (response : byte []) = 
    try 
        use ms = new MemoryStream(response)
        use sr = new StreamReader(ms, Text.Encoding.UTF8)
        let msg = sr.ReadToEnd()
        try 
            match msg |> Json.deserialize with
            | Message msg -> 
                Json.deserialize<DeploymentResponse> msg
                |> Message
                |> Choice1Of2
            | Exception exn -> Exception exn |> Choice1Of2
        with _ -> 
            msg
            |> Json.deserialize<DeploymentResponse>
            |> Message
            |> Choice1Of2
    with exn -> Choice2Of2 exn

/// Posts the given file to the given URL.
let private post url file timeout = uploadFile "POST" url file timeout >> processResponse

/// Puts the given body to the given URL.
let private put url timeout = uploadData "PUT" url timeout >> processResponse

type DeployStatus = 
    | Active
    | Inactive

type App = 
    { Name : string
      Version : string }

let buildExceptionString (r : Response) = 
    let msgs = 
        r.Messages
        |> Seq.map (fun m -> sprintf "  %s %s" (m.Timestamp.ToString("yyyy-MM-dd hh::mm:ss.fff")) m.Message)
        |> fun arr -> String.Join("\r\n", arr)
    sprintf "%O\r\n\r\nDeploy messages\r\n{\r\n%s\r\n}\r\n" r.Exception msgs

/// Authenticate against the given server with the given userId and private key
let authenticate server userId serverpathToPrivateKeyFile passwordForPrivateKey = 
    let privateKey = loadPrivateKey serverpathToPrivateKeyFile passwordForPrivateKey
    let challenge = REST.ExecuteGetCommand null null (server + "/login/" + userId)
    
    let signature = 
        challenge
        |> Convert.FromBase64String
        |> privateKey.Sign
        |> Convert.ToBase64String
    
    let postData = 
        sprintf "challenge=%s&signature=%s" (HttpUtility.UrlEncode challenge) (HttpUtility.UrlEncode signature)
    let response = REST.ExecutePost (server + "/login") "x" "x" postData
    authToken <- response.Trim([| '"' |])
                 |> Guid.Parse
                 |> Some
    authToken

/// Returns all releases of the given app from the given server.
let getReleasesFor server appname status = 
    if String.IsNullOrEmpty(appname) then server + "/deployments?status=" + status
    else server + "/deployments/" + appname + "?status=" + status
    |> get defaultTimeout (Json.deserialize<DeploymentResponse>)

/// Performs a rollback of the given app on the server.
let rollbackTo server appname version = 
    put (server + "/deployments/" + appname + "?version=" + version) [||] defaultTimeout |> wrapFailure

/// Returns all active releases from the given server.
let getAllActiveReleases server = getReleasesFor server null "active" |> wrapFailure

/// Returns the active release of the given app from the given server.
let getActiveReleasesFor server appname = getReleasesFor server appname "active" |> wrapFailure

/// Returns all releases of the given app from the given server.
let getAllReleasesFor server appname = 
    if String.IsNullOrEmpty(appname) then server + "/deployments/"
    else server + "/deployments/" + appname + "/"
    |> get defaultTimeout (Json.deserialize<DeploymentResponse>)
    |> wrapFailure

/// Returns all releases from the given server.
let getAllReleases server = getAllReleasesFor server null

/// Posts a deployment package to the given URL.
let postDeploymentPackage url packageFileName args = post url packageFileName args defaultTimeout |> wrapFailure


/// Posts a deployment package to the given URL, executes the script inside it with given arguments and handles the response.
let deployPackage (f : Deployment -> Deployment) =
    let d = f { defaultDeployment with 
                    AuthToken = 
                        match authToken with
                        | Some x -> Some ( AuthToken x)
                        | None -> None }
    authToken <- 
        match d.AuthToken with
        | Some a -> Some (match a with | AuthToken b -> b)
        | None -> None
    let result = post d.Url d.PackageFileName (d.Arguments |> Array.ofList) d.Timeout |> wrapFailure
    match result with
    | Success _ -> tracefn "Deployment of %s successful" d.PackageFileName
    | Failure exn -> failwithf "Deployment of %A failed\r\n%s" d.PackageFileName (buildExceptionString exn)
    | response -> failwithf "Deployment of %A failed\r\n%A" d.PackageFileName response

/// Posts a deployment package to the given URL, executes the script inside it with given arguments and handles the response.
/// Deprecated, use DeployPackage
[<Obsolete("Use deployPackage")>]
let DeployPackageWithArgs url packageFileName args = 
    deployPackage (fun x -> { x with Url = url; PackageFileName = packageFileName; Arguments = args |> List.ofArray })

/// Posts a deployment package to the given URL and handles the response.
/// Deprecated, use DeployPackage
[<Obsolete("Use deployPackage")>]
let DeployPackage url packageFileName = DeployPackageWithArgs url packageFileName [||]

/// Performs a rollback of the given app at the given URL and handles the response.
let RollbackPackage url appName version = 
    match rollbackTo url appName version with
    | Success _ -> tracefn "Rollback of %s to %s successful" appName version
    | Failure exn -> failwithf "Deployment of %s to %s failed\r\n%s" appName version (buildExceptionString exn)
    | response -> failwithf "Deployment of %s to %s failed\r\n%A" appName version response
