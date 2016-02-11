# Packaging and Deploying Azure WebJobs

FAKE can be used to zip the output directory of a project and push it to Azure via the [zip controller](https://github.com/projectkudu/kudu/wiki/REST-API#zip).

You'll need to know the ftp details of the website to publish the web jobs to. Also the web jobs need to already exist on the azure dashboard.

In your `build.fsx` add the following:

    type Uri with
        member this.SubDomain = this.Host.Split([|'.'|],2).[0]

    let private jobTypePath webJobType =
        match webJobType with
        | WebJobType.Continuous -> "continuous"
        | WebJobType.Triggered -> "triggered"

    // a function to create webjobs based on the projects structure
    let private createWebJob site name jobType project =
        let path = jobTypePath jobType
        { Name = name
          JobType = jobType
          Project = project
          DirectoryToPackage = sprintf "src/%s/bin/Release" project
          PackageLocation = sprintf "bin/%s/webjobs/%s/%s.zip" site path project }
    
    let webJobs site = [createWebJob site "webjob1" WebJobType.Continuous "MyProject.WebJob1"
                        createWebJob site "webjob2" WebJobType.Triggered "MyProject.WebJob2"]

    let site = Uri("https://yoursite.scm.azurewebsites.net")
    let webSite = {WebSite.Url = site
                   UserName = "$yoursite"
                   Password = "password"
                   WebJobs = webJobs site.SubDomain }

    Target "PackageWebJobs" (fun _ ->
        PackageWebJobs [webSite]
    )
    Target "DeployWebJobs" (fun _ ->
        DeployWebJobs [webSite]
    )

In the dependencies section add the targets to the build order after the build action:

    "Clean"
      ==> "BuildApp"
      ==> "BuildTest"
      ==> "Test"
      ==> "PackageWebJobs"
      ==> "DeployWebJobs"
      ==> "Default"

The will create a zip file in the `bin` folder in the root which contains the contents of the `bin/release` folder of each web job to deploy and push it to azure.

## Caveats
The zip controller will not remove files. 
