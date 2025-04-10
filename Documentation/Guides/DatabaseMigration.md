# Database migration

Last updated for Enterprise Web Library version 83.


If you need to make changes to your database schema or your reference data, you can use the `Data Migrator` project. If this project is not yet included in your solution, locate the generated `Data Migrator/Data Migrator.ewlt.csproj`, change the extension to just `.csproj`, add the project to your solution, and reference the same version of EWL that the other projects do.

Add migration classes to the project (see https://fluentmigrator.github.io/ for more info), and run `Update-DependentLogic`. This will run your migrations against your local copy of the database. Every installation of your system stores the migrations that have already been applied to it, so when you commit your new migration classes, and deploy the system to a server, all of your changes that have not already run against that installation’s database will run. **Please note:** If you don’t use the EWL System Manager, you’ll need to call `DataMigrator.exe` as part of deployment.

## Examples

See https://fluentmigrator.github.io/.

If you need cursor-type logic of looping through rows and doing something for each one, use `Execute.WithConnection`, which provides a raw `IDbConnection` and transaction. Then you can query that with Dapper (https://github.com/DapperLib/Dapper) to cleanly access results.