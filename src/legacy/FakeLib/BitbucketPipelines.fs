/// Contains code to configure FAKE for Bitbucket Pipelines integration
[<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
module Fake.BitbucketPipelines

/// Bitbucket Pipelines environment variables as [described](https://confluence.atlassian.com/bitbucket/environment-variables-in-bitbucket-pipelines-794502608.html)
[<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
type BitbucketPipelinesEnvironment = 
    
    /// The commit hash of a commit that kicked off the build
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member Commit = environVar "BITBUCKET_COMMIT"

    /// The branch on which the build was kicked off. This value is only available on branches.
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member Branch = environVar "BITBUCKET_BRANCH"
    
    /// The tag of a commit that kicked off the build. This value is only available on tags.
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member Tag = environVar "BITBUCKET_TAG"
    
    /// The URL-friendly version of a repository name.
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member RepoSlug = environVar "BITBUCKET_REPO_SLUG"
    
    /// The name of the account in which the repository lives
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member RepoOwner = environVar "BITBUCKET_REPO_OWNER"
    
    /// The absolute path of the directory that the repository is cloned into within the Docker container.
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member CloneDir = environVar "BITBUCKET_CLONE_DIR"
    
    /// Default value is true. Gets set whenever a pipeline runs.
    [<System.Obsolete("use Fake.BuildServer.Bitbucket instead (FAKE0001 - package: Fake.BuildServer.Bitbucket)")>]
    static member CI = environVar "CI"
