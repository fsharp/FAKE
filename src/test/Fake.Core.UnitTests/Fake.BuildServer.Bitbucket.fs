module Fake.BuildServer.BitbucketTests

open Fake.BuildServer
open Fake.Core
open Expecto

[<Tests>]
let tests =
  testList "Fake.BuildServer.Bitbucket.Tests" [
    test "Test it gets Bitbucket commit env var" {
        Environment.setEnvironVar "BITBUCKET_COMMIT" "123456789"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.Commit "123456789" "Expected '123456789' for the commit env var"
    }

    test "Test it gets Bitbucket branch env var" {
        Environment.setEnvironVar "BITBUCKET_BRANCH" "feature/bitbucket-in-fake"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.Branch "feature/bitbucket-in-fake" "Expected 'feature/bitbucket-in-fake' for the branch env var"
    }

    test "Test it gets Bitbucket tag env var" {
        Environment.setEnvironVar "BITBUCKET_TAG" "v5.20.4"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.Tag "v5.20.4" "Expected 'v5.20.4' for the tag env var"
    }

    test "Test it gets Bitbucket repository slug env var" {
        Environment.setEnvironVar "BITBUCKET_REPO_SLUG" "fake"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.RepoSlug "fake" "Expected 'fake' for the repository slug env var"
    }

    test "Test it gets Bitbucket repository owner env var" {
        Environment.setEnvironVar "BITBUCKET_REPO_OWNER" "open-source"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.RepoOwner "open-source" "Expected 'open-source' for the repository owner env var"
    }

    test "Test it gets Bitbucket repository UUID env var" {
        Environment.setEnvironVar "BITBUCKET_REPO_UUID" "444a3ecd-9f4b-4dc3-8fc0-44b33cf375d9"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.RepoUuid "444a3ecd-9f4b-4dc3-8fc0-44b33cf375d9" "Expected '444a3ecd-9f4b-4dc3-8fc0-44b33cf375d9' for the repository UUID env var"
    }

    test "Test it gets Bitbucket repository full name env var" {
        Environment.setEnvironVar "BITBUCKET_REPO_FULL_NAME" "FAKE - F# Make"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.RepoFullName "FAKE - F# Make" "Expected 'FAKE - F# Make' for the repository full name env var"
    }

    test "Test it gets Bitbucket clone direcroty env var" {
        Environment.setEnvironVar "BITBUCKET_CLONE_DIR" "C:/users/fake"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.CloneDir "C:/users/fake" "Expected 'C:/users/fake' for the clone directory env var"
    }

    test "Test it gets Bitbucket CI env var" {
        Environment.setEnvironVar "CI" "true"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.CI "true" "Expected 'true' for the CI env var"
    }
    
    test "Test it gets Bitbucket build number env var" {
        Environment.setEnvironVar "BITBUCKET_BUILD_NUMBER" "54321"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.BuildNumber "54321" "Expected '54321' for the build number env var"
    }
    
    test "Test it gets Bitbucket PR ID env var" {
        Environment.setEnvironVar "BITBUCKET_PR_ID" "2222"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.PrId "2222" "Expected '2222' for the pull request env var"
    }
    
    test "Test it gets Bitbucket deployment environment env var" {
        Environment.setEnvironVar "BITBUCKET_DEPLOYMENT_ENVIRONMENT" "production"
        Expect.equal BitbucketPipelines.BitbucketPipelinesEnvironment.DeploymentEnvironment "production" "Expected 'production' for the deployment environment env var"
    }

  ]
