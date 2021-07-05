# Packaging and Deploying SQL Databases

<div class="alert alert-info">
    <h5>INFO</h5>
    <p>This documentation is for FAKE version 5.0 or later. The old documentation can be found <a href="legacy-dacpac.html">here</a></p>
</div>

FAKE can be used to create a SQL DACPAC and also deploy it to a SQL Server using the MSDeploy executable. This is installed by default with Visual Studio and with the SQL Server Data Tools (SSDT) package.

DACPACs automatically diff from the source to the destination and generate the SQL script dynamically.

You can read up more on DACPac and MSDeploy arguments [here](https://msdn.microsoft.com/en-us/library/hh550081%28v=vs.103%29.aspx).

## Sample usage

Ensure that you have already built your database project (you can do this with standard MSBuild). Then, use the ``deployDb`` command to deploy the ``dbProject.dacpac`` to the ``myDatabase``.

    open Fake.Sql

    /// the database for local development + compile
    Target.create "DeployLocalDb" (fun _ ->
        let connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Database=MyDatabase;Pooling=False"
        let dacPacPath = "path/to/dbProject.dacpac"
        SqlPackage.deployDb (fun args -> { args with Source = dacPacPath; Destination = connectionString }) |> ignore
    )

The following sample shows how to deploy a database project to Azure using an access token:

    open Fake.Core
    open Fake.Sql

    /// the database for local development + compile
    Target.create "DeployLocalDb" (fun _ ->
        let dacPacPath = "path/to/dbProject.dacpac"
        let accessToken = "your-access-token"
        let connectionString = "Data Source=your-server-name.database.windows.net; Initial Catalog=your-database-name;" 
        SqlPackage.deployDb (fun args -> 
            { args with 
                    Destination = connectionString
                    AccessToken = accessToken
                    Source = dacPacPath 
            }) |> ignore
    )

## Deployment Options

You can optionally specify the type of command to use (again, refer to the documentation above for more detail): -

* Deploy - full deploy to destination
* Script - SQL script
* Report - XML report of diff

In addition, you can also elect to deploy to Dacpac files rather than SQL databases - simply pass in the pass to the dacpac that you wish to generate.


## Arguments

You can provide following arguments (in brackets are given sqlpackage.exe parameters name):

* SqlPackageToolPath - path to sqlpackage.exe
* Action - deployment option (/a)
* AccessToken - An Access token to use in authentication instead of username and password
* Source - specifies a source file to be used as the source of action instead of a database (/SourceFile)
* Destination - specifies a valid SQL Server/Azure connection string to the target database (/TargetConnectionString)
* Timeout - specifies the command timeout in seconds when executing queries against SQL Server (/p:CommandTimeout)
* BlockOnPossibleDataLoss - Specifies that the publish episode should be terminated if there is a possibility of data loss resulting from the publish.operation (/p:BlockOnPossibleDataLoss)
* DropObjectsNotInSource - specifies whether objects that do not exist in the database snapshot (.dacpac) file will be dropped from the target database when you publish to a database (/p:DropObjectsNotInSource) 
* RecreateDb - specifies whether the target database should be updated or whether it should be dropped and re-created when you publish to a database (/p:CreateNewDatabase)
* AdditionalSqlPackageProperties - specifies a name value pair for an properties;{PropertyName}={Value}
* Variables - specifies a name value pair for an action-specific variable;{VariableName}={Value}. The DACPAC file contains the list of valid SQLCMD variables (/v)
* Profile - specifies the file path to a DAC Publish Profile (/pr)

If both DAC Publish Profile file and command line parameters provides the same argument, then the one from command line overrites Publish Profile value. An example: if Profile File has BlockOnPossibleDataLoss set to true and command line set it to false, sqlpackage.exe set BlockOnPossibleDataLoss to false.
