namespace Fake.Api

open Octokit
open Octokit.Internal
open System
open System.Net
open System.Net.Http
open System.IO

/// Contains tasks to interact with [GitHub](https://github.com/) releases
///
/// ### Sample
///
///        Target.create "GitHubRelease" (fun _ ->
///            let token =
///                match Environment.environVarOrDefault "github_token" "" with
///                | s when not (System.String.IsNullOrWhiteSpace s) -> s
///                | _ -> failwith "please set the github_token environment variable to a github personal access token with repro access."
///        
///            let files =
///                runtimes @ [ "portable"; "packages" ]
///                |> List.map (fun n -> sprintf "release/dotnetcore/Fake.netcore/fake-dotnetcore-%s.zip" n)
///        
///            GitHub.createClientWithToken token
///            |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
///            |> GitHub.uploadFiles files
///            |> GitHub.publishDraft
///            |> Async.RunSynchronously)
///
[<RequireQualifiedAccess>]
module GitHub =

    [<NoComparison>]
    type Release =
        { Client : GitHubClient
          Owner : string
          RepoName : string
          Release : Octokit.Release }

    type CreateReleaseParams =
        { /// The name of the release
          Name : string 
          /// The text describing the contents of the release
          Body : string
          /// Indicates whether the release will be created as a draft
          Draft : bool 
          /// Indicates whether the release will be created as a prerelease
          Prerelease : bool
          /// Commit hash or branch name that will be used to create the release tag.
          /// Is not used if the tag already exists.
          /// If left unspecified, and the tag does not already exist, the default branch is used instead.
          TargetCommitish : string }

    let private timeout = TimeSpan.FromMinutes 20.

    let private createHttpClient =
        let handlerFactory () =
            let handler = HttpMessageHandlerFactory.CreateDefault()
#if NETSTANDARD2_0
            // Ensure the default credentials are used with any system-configured proxy
            // https://github.com/dotnet/runtime/issues/25745#issuecomment-378322214
            match handler with
            | :? HttpClientHandler as h ->
                h.DefaultProxyCredentials <- CredentialCache.DefaultCredentials
            | _ -> ()
#endif
            handler

        fun () -> new HttpClientAdapter(Func<_> handlerFactory)

    /// A version of 'reraise' that can work inside computation expressions
    let private captureAndReraise ex =
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw()
        Unchecked.defaultof<_>

    /// Retry the Octokit action count times
    let rec private retry count asyncF =
        async {
            try
                return! asyncF
            with ex ->
                return!
                    match (ex, ex.InnerException) with
                    | (:? AggregateException, (:? AuthorizationException as ex)) -> captureAndReraise ex
                    | _ when count > 0 ->
                        printfn "Something failed, trying again: %O" ex
                        retry (count - 1) asyncF
                    | (ex, _) -> captureAndReraise ex
        }

    /// Retry the Octokit action count times after input succeed
    let private retryWithArg count input asycnF =
        async {
            let! choice = input |> Async.Catch
            match choice with
            | Choice1Of2 input' ->
                return! (asycnF input') |> retry count
            | Choice2Of2 ex ->
                return captureAndReraise ex
        }

    /// Creates a GitHub API v3 client using the specified credentials
    let createClient (user:string) (password:string) =
        async {
            let httpClient = createHttpClient()
            let connection = Connection(ProductHeaderValue("FAKE"), httpClient)
            let github = GitHubClient(connection)
            github.Credentials <- Credentials(user, password)
            github.SetRequestTimeout timeout
            return github
        }

    /// Creates a GitHub API v3 client using the specified token
    let createClientWithToken (token:string) =
        async {
            let httpClient = createHttpClient()
            let connection = Connection(ProductHeaderValue("FAKE"), httpClient)
            let github = GitHubClient(connection)
            github.Credentials <- Credentials(token)
            github.SetRequestTimeout timeout
            return github
        }

    /// Creates a GitHub API v3 client to GitHub Enterprise server at the specified url using the specified credentials
    let createGHEClient url (user:string) (password:string) =
        async {
            let credentials = Credentials(user, password)
            let httpClient = createHttpClient()
            let connection = Connection(ProductHeaderValue("FAKE"), Uri(url), InMemoryCredentialStore(credentials), httpClient, SimpleJsonSerializer())
            let github = GitHubClient(connection)
            github.Credentials <- credentials
            github.SetRequestTimeout timeout
            return github
        }

    /// Creates a GitHub API v3 client to GitHub Enterprise server at the specified url using the specified token
    let createGHEClientWithToken url (token:string) =
        async {
            let credentials = Credentials(token)
            let httpClient = createHttpClient()
            let connection = Connection(ProductHeaderValue("FAKE"), Uri(url), InMemoryCredentialStore(credentials), httpClient, SimpleJsonSerializer())
            let github = GitHubClient(connection)
            github.Credentials <- credentials
            github.SetRequestTimeout timeout
            return github
        }

    /// Creates a GitHub Release for the specified repository and tag name
    /// ## Parameters
    /// - `owner` - the repository's owner
    /// - `repoName` - the repository's name
    /// - `tagName` - the name of the tag to use for this release
    /// - `setParams` - function used to override the default release parameters
    /// - `client` - GitHub API v3 client
    let createRelease owner repoName tagName setParams (client : Async<GitHubClient>) =    
        retryWithArg 5 client <| fun client' -> async {
            let p = 
                { Name = tagName
                  Body = ""
                  Draft = true 
                  Prerelease = false
                  TargetCommitish = "" } |> setParams
            
            let data = NewRelease(tagName)
            data.Name <- p.Name
            data.Body <- p.Body
            data.Draft <- p.Draft
            data.Prerelease <- p.Prerelease
            data.TargetCommitish <- p.TargetCommitish

            let! release = Async.AwaitTask <| client'.Repository.Release.Create(owner, repoName, data)

            let draftWord = if data.Draft then " draft" else ""

            printfn "Created%s release id %d" draftWord release.Id

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Creates a draft GitHub Release for the specified repository and tag name
    /// ## Parameters
    /// - `owner` - the repository's owner
    /// - `repoName` - the repository's name
    /// - `tagName` - the name of the tag to use for this release
    /// - `prerelease` - indicates whether the release will be created as a prerelease
    /// - `notes` - collection of release notes that will be inserted into the body of the release
    /// - `client` - GitHub API v3 client
    let draftNewRelease owner repoName tagName prerelease (notes : seq<string>) client =
        let setParams p = 
            { p with 
                Body = String.Join(Environment.NewLine, notes) 
                Prerelease = prerelease }
        createRelease owner repoName tagName setParams client

    /// Uploads and attaches the specified file to the specified release
    let uploadFile fileName (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let fi = FileInfo(fileName)
            // remove existing asset if it exists
            let! assets =
                Async.AwaitTask <| release'.Client.Repository.Release.GetAllAssets(release'.Owner, release'.RepoName, release'.Release.Id)
            match assets |> Seq.tryFind (fun a -> a.Size <= 0 && a.Name = fi.Name) with
            | Some s ->
                printfn "removing asset '%s' as previous upload failed" s.Name
                do! Async.AwaitTask(release'.Client.Repository.Release.DeleteAsset(release'.Owner, release'.RepoName, s.Id))
            | None -> ()                        

            let archiveContents = File.OpenRead(fi.FullName)
            let assetUpload = ReleaseAssetUpload(fi.Name,"application/octet-stream",archiveContents,Nullable timeout)
            
            let! asset = Async.AwaitTask <| release'.Client.Repository.Release.UploadAsset(release'.Release, assetUpload, Async.DefaultCancellationToken)
            printfn "Uploaded %s" asset.Name
            return release'
        }

    /// Uploads and attaches the specified files to the specified release
    let uploadFiles fileNames (release : Async<Release>) = async {
        let! release' = release
        let releaseW = async { return release' }
        let! _ = Async.Parallel [for f in fileNames -> uploadFile f releaseW ]
        return release'
    }

    /// Publishes the specified release by removing its draft status
    let publishDraft (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let update = release'.Release.ToUpdate()
            update.Draft <- Nullable<bool>(false)
            let! released = Async.AwaitTask <| release'.Client.Repository.Release.Edit(release'.Owner, release'.RepoName, release'.Release.Id, update)
            printfn "Published release %d on GitHub" released.Id
        }

    /// Gets the latest release for the specified repository
    let getLastRelease owner repoName (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            let! release = Async.AwaitTask <| client'.Repository.Release.GetLatest(owner, repoName)

            printfn "Latest release id: %d" release.Id
            printfn "Latest release tag: %s" release.TagName
            printfn "Latest release assets: %d" (Seq.length release.Assets)

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Gets release with the specified tag for the specified repository
    let getReleaseByTag (owner:string) repoName tagName (client : Async<GitHubClient>) =
        retryWithArg 5 client <| fun client' -> async {
            let! releases = client'.Repository.Release.GetAll(owner, repoName) |> Async.AwaitTask
            let matches = releases |> Seq.filter (fun (r: Octokit.Release) -> r.TagName = tagName)

            if Seq.isEmpty matches then
                failwithf "Unable to locate tag %s" tagName

            let release = matches |> Seq.head

            printfn "Release id: %d" release.Id
            printfn "Release tag: %s" release.TagName
            printfn "Release assets: %d" (Seq.length release.Assets)

            return {
                Client = client'
                Owner = owner
                RepoName = repoName
                Release = release }
        }

    /// Downloads the asset with the specified id to the specified destination
    let downloadAsset id destination (release : Async<Release>) =
        retryWithArg 5 release <| fun release' -> async {
            let! asset = Async.AwaitTask <| release'.Client.Repository.Release.GetAsset(release'.Owner,release'.RepoName,id)
            let! resp = Async.AwaitTask <| release'.Client.Connection.Get(Uri(asset.Url), new System.Collections.Generic.Dictionary<string,string>(),"application/octet-stream")

            let bytes = resp.HttpResponse.Body :?> byte[]
            let filename = Path.Combine(destination, asset.Name)

            File.WriteAllBytes(filename, bytes)

            printfn "Downloaded %s" filename
        }

    /// Downloads all assets for the specified release to the specified destination
    let downloadAssets destination (release : Async<Release>) = async {
        let! release' = release
        let releaseW = async { return release' }

        let! _ = Async.Parallel [for f in release'.Release.Assets -> downloadAsset f.Id destination releaseW ]

        ()
    }
