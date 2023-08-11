# Getting started

**Please note:** While EWL has been in production use for many years, this Getting Started guide is brand new. If you run into problems please let us know in our [community forum](https://community.enterpriseweblibrary.org/) and one of the developers will help you out.

Last updated for Enterprise Web Library version 78.


## Requirements

*	Windows 10 or later
*	Visual Studio 2022 or later (recommended), or .NET 7 SDK
*	SQL Server 2022 or later, MySQL 8.0, or Oracle Database 12c (if you want a relational database)

For servers, the only requirement is Windows Server 2019 or later with IIS enabled.


## Creating a new system

1.	Visit http://ewl.enterpriseweblibrary.org/create-system to download a ZIP file containing your new starter system. Extract the files into a location of your choice.

2.	Open the solution file in Visual Studio. In the Package Manager Console, run `Get-Project -All | Install-Package Ewl`.

3.	Again in the Package Manager Console, run `Update-DependentLogic`. This will copy some web-framework files into the solution, and generate a few pieces of code in both projects. It will also apply some IIS Express configuration (if installed) in case you wish to use this as a local web server instead of Kestrel.

4.	Understand the elements of your new system:

	*	The `Library` project, which contains configuration files, provider classes (which allow parts of EWL’s behavior to be customized), and a `GlobalInitializer` class (which gives you a place to initialize static fields when your system starts up). `Library` should also be the home of most of your "business logic" and anything else that you would reuse across multiple applications in your system. Right now your system only contains a single web app, but as it grows, you may need another web app or a different type of application, e.g. a Windows service.

	*	The `Website` project, which references `Library` and will contain your pages and other resources.

	More information on this is available from our developers; please [ask for help in the forum](https://community.enterpriseweblibrary.org/).

5.	Run the `Website` project. If you see a home page that reads “Welcome to the Enterprise Web Library!”, everything is working and you can begin building your system.


## Adding a database

1.	Add the `<database>` element to the three installation configuration files (i.e. the `Standard.xml` files in `Library/Configuration/Installation/Installations`) after the closing `</administrators>` tag:

	* For SQL Server, use `<database xsi:type="SqlServerDatabase" />`. The database name will be `SystemShortNameDev`, `SystemShortName` being replaced with the value from your `General.xml` configuration file. Use the `<database>` child element to override this naming convention.

	* For MySQL, use `<database xsi:type="MySqlDatabase" />`. The schema name will be `system_short_name_dev`, `system_short_name` being replaced with a lowercased, underscore-separated version of the value from your `General.xml` configuration file. Use the `<database>` child element to override this naming convention.

	* For Oracle, use `<database xsi:type="OracleDatabase">` with the `<tnsName>`, `<userAndSchema>`, and `<password>` child elements. Name your schema whatever you like. We have no convention. The MySQL convention may work.

2.	In the Package Manager Console, run `Update-Data`. This will create (or re-create) the database.

3.	Add the `<database>` element to the development configuration file (i.e. `Library/Configuration/Development.xml`) after the `<webProjects>` element.

Now, when you run `Update-DependentLogic`, data-access code will be generated for your database.


## Deploying your system

This section is more theoretical than practical since it’s generally not a good practice to deploy enterprise software by hand, and without a continuous integration infrastructure.

1.	In the Package Manager Console, run `ExportLogic`.

2.	Somehow copy the exported logic and configuration to the server.

3.	Create and initialize a database using the appropriate steps above. The database name should be slightly different; replace `Dev` or `_dev` with the short name of your installation from the configuration file. Also, for SQL Server, grant the `NETWORK SERVICE` account reader/writer access to the database.

4.	Set up an IIS website or virtual directory that points at the appropriate folder in the logic that you copied to the server in step 2. Run the application pool as `NETWORK SERVICE`.

Some of the EWL developers use an internal product called the EWL System Manager, which builds, tests, and deploys EWL systems automatically. It’s like an ultra-opinionated version of Heroku or AppHarbor. It can be run in the cloud or on-premises. [Let us know in the forum](https://community.enterpriseweblibrary.org/) if you are interested in using this.


## Learning more

If you’d like to learn how to build a web application, check out the [web framework guide](WebFramework.md). Or see the [table of contents](../TableOfContents.md) for a full list of documentation.