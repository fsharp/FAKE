/// Contains support for various build servers
namespace Fake.BuildServer

open Fake.Core
open Fake.IO

/// native support for GitHub Actions specific APIs.
/// The general documentation on how to use CI server integration can be found [here](/buildserver.html).
/// This module does not provide any special APIs please use FAKE APIs and they should integrate into this CI server.
/// If some integration is not working as expected or you have features you would like to use directly please open an issue.
[<RequireQualifiedAccess>]
module GitHubActions =

    /// https://help.github.com/en/actions/configuring-and-managing-workflows/using-environment-variables#default-environment-variables
    type Environment =

        /// Always set to true, if present.
        static member CI = Environment.environVarAsBoolOrDefault "CI"
        /// The path to the GitHub home directory used to store user data.
        /// For example, /github/home.
        static member Home = Environment.environVar "HOME"
        /// The name of the workflow.
        static member Workflow = Environment.environVar "GITHUB_WORKFLOW"
        /// A unique number for each run within a repository.
        /// This number does not change if you re-run the workflow run.
        static member RunId = Environment.environVar "GITHUB_RUN_ID"
        /// A unique number for each run of a particular workflow in a repository.
        /// This number begins at 1 for the workflow's first run, and increments with each new run.
        /// This number does not change if you re-run the workflow run.
        static member RunNumber = Environment.environVar "GITHUB_RUN_NUMBER"
        /// The unique identifier (id) of the action.
        static member Action = Environment.environVar "GITHUB_ACTION"
        /// Always set to true when GitHub Actions is running the workflow.
        /// You can use this variable to differentiate when tests are being run locally or by GitHub Actions.
        static member Actions = Environment.environVarAsBoolOrDefault "GITHUB_ACTIONS"
        /// The name of the person or app that initiated the workflow. For example, octocat.
        static member Actor = Environment.environVar "GITHUB_ACTOR"
        /// The owner and repository name. For example, octocat/Hello-World.
        static member Repository = Environment.environVar "GITHUB_REPOSITORY"
        /// The name of the webhook event that triggered the workflow.
        static member EventName = Environment.environVar "GITHUB_EVENT_NAME"
        /// The path of the file with the complete webhook event payload.
        /// For example, /github/workflow/event.json.
        static member EventPath = Environment.environVar "GITHUB_EVENT_PATH"
        /// The GitHub workspace directory path.
        /// The workspace directory contains a subdirectory with a copy of your repository if your workflow uses the actions/checkout action.
        /// If you don't use the actions/checkout action, the directory will be empty.
        /// For example, /home/runner/work/my-repo-name/my-repo-name.
        static member Workspace = Environment.environVar "GITHUB_WORKSPACE"
        /// The commit SHA that triggered the workflow.
        /// For example, ffac537e6cbbf934b08745a378932722df287a53.
        static member Sha = Environment.environVar "GITHUB_SHA"
        /// The branch or tag ref that triggered the workflow.
        /// For example, refs/heads/feature-branch-1.
        /// If neither a branch or tag is available for the event type, the variable will not exist.
        static member Ref = Environment.environVar "GITHUB_REF"
        /// Only set for forked repositories. The branch of the head repository.
        static member HeadRef = Environment.environVar "GITHUB_HEAD_REF"
        /// Only set for forked repositories. The branch of the base repository.
        static member BaseRef = Environment.environVar "GITHUB_BASE_REF"
