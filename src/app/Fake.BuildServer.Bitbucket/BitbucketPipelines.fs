/// Contains code to configure FAKE for Bitbucket Pipelines integration
namespace Fake.BuildServer

open System
open System.IO
open Fake.Core

[<RequireQualifiedAccess>]
module BitbucketPipelines =
    type BitbucketPipelinesEnvironment =
        /// The commit hash of a commit that kicked off the build
        static member Commit = Environment.environVar "BITBUCKET_COMMIT"

        /// The branch on which the build was kicked off. This value is only available on branches.
        static member Branch = Environment.environVar "BITBUCKET_BRANCH"
        
        /// The tag of a commit that kicked off the build. This value is only available on tags.
        static member Tag = Environment.environVar "BITBUCKET_TAG"
        
        /// The URL-friendly version of a repository name.
        static member RepoSlug = Environment.environVar "BITBUCKET_REPO_SLUG"
        
        /// The name of the account in which the repository lives
        static member RepoOwner = Environment.environVar "BITBUCKET_REPO_OWNER"
        
        /// The UUID of the repository.
        static member RepoUuid = Environment.environVar "BITBUCKET_REPO_UUID"

        /// The full name of the repository (everything that comes after http://bitbucket.org/).
        static member RepoFullName = Environment.environVar "BITBUCKET_REPO_FULL_NAME"
        
        /// The absolute path of the directory that the repository is cloned into within the Docker container.
        static member CloneDir = Environment.environVar "BITBUCKET_CLONE_DIR"
        
        /// Default value is true. Gets set whenever a pipeline runs.
        static member CI = Environment.environVar "CI"

        /// The unique identifier for a build. It increments with each build and can be used to create unique artifact names.
        static member BuildNumber = Environment.environVar "BITBUCKET_BUILD_NUMBER"

        /// The pull request ID Only available on a pull request triggered build.
        static member PrId = Environment.environVar "BITBUCKET_PR_ID"

        /// The URL friendly version of the environment name.
        static member DeploymentEnvironment = Environment.environVar "BITBUCKET_DEPLOYMENT_ENVIRONMENT"
