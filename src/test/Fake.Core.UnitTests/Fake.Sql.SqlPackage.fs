module Fake.Sql.SqlPackageTests

open Fake.Core
open Fake.Sql
open Expecto

[<Tests>]
let tests =
  testList "Fake.Sql.SqlPackage.Tests" [
    testCase "Test that it produces correct command when using access token" <| fun _ ->
      let action = "Publish"
      let outputPath = ""
      let additionalParameters = ""
      let variables = ""
      let args: SqlPackage.DeployDbArgs = 
        { SqlPackageToolPath = ""
          Action = SqlPackage.DeployAction.Deploy
          AccessToken = "my-access-token"
          Source = ""
          Destination = "Data Source=your-server-name.database.windows.net; Initial Catalog=your-database-name;"
          Timeout = None
          BlockOnPossibleDataLoss = None
          DropObjectsNotInSource = None
          RecreateDb = None
          AdditionalSqlPackageProperties = []
          Variables = []
          Profile = "" }

      let cmd = SqlPackage.formatArgument args action outputPath additionalParameters variables "AccessToken"

      Expect.equal cmd
        (sprintf "/AccessToken:\"my-access-token\" ") "expected access token to be enclosed in quotes"

  ]
