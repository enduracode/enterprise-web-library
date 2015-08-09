# [Enterprise Web Library](http://enterpriseweblibrary.org/)

The Enterprise Web Library (EWL) is an extremely-opinionated NuGet package designed to help you build web-based enterprise software on the .NET Framework. See [our web site](http://enterpriseweblibrary.org/) for more information.


## Table of Contents

*	[Introduction](#introduction)
*	[Getting Started](#getting-started)
*	[Documentation](#documentation)
*	[Support](#support)
*	[Contributing](#contributing)


## Introduction

[William Gross](http://wgross.net/) and Greg Smalter launched this project back in 2004, in closed-source form, as a "standard library" that would eliminate a lot of the duplication that existed across the various line-of-business web applications that they maintained for their clients. It was successful, and after steady improvement each year, they open-sourced the entire library in 2012. William has been the lead developer ever since. EWL uses the [MIT License](http://opensource.org/licenses/MIT) and is in production use at [several organizations](http://enterpriseweblibrary.org/usedby/).


## Getting Started

**Please note:** While EWL has been in production use for many years, this Getting Started guide is brand new. If you run into problems please let us know in our [community forum](https://community.enterpriseweblibrary.org/) and one of the developers will help you out.


### Requirements

*	Windows 8.1, .NET Framework 4.5.1, IIS Express 8.5 (.NET Core support is on the roadmap but please [speak up](https://community.enterpriseweblibrary.org/) if you're interested!)
*	Visual Studio 2013 (recommended)
* SQL Server 2012 or later, MySQL 5.5, or Oracle Database 12c (if you want a relational database)

For servers, the only requirement is Windows Server 2012 R2 with IIS enabled.


### Creating a New System

1.	Visit http://ewl.enterpriseweblibrary.org/create-system to download a ZIP file containing your new starter system. Extract the files into a location of your choice.

2.	Open the solution file in Visual Studio. In the Package Manager Console, run `Get-Project -All | Install-Package Ewl`. Save all files in the solution (Ctrl-Shift-S).

3.	Again in the Package Manager Console, run `Update-DependentLogic`. This will do the following:

	*	Apply some IIS Express configuration

	*	Copy some files into the `Web Site` project, and update the project file to reference them

	*	Generate a few pieces of code in both projects, and update the Web.config file

4.	Understand the elements of your new system:

	*	The `Library` project, which contains configuration files, provider classes (which allow parts of EWL's behavior to be customized), and a `GlobalInitializer` class (which gives you a place to initialize static fields when your system starts up). `Library` should also be the home of most of your "business logic" and anything else that you would reuse across multiple applications in your system. Right now your system only contains a single web app, but as it grows, you may need another web app or a different type of application, e.g. a Windows service.

	*	The `Web Site` project, which references `Library` and will contain your pages and other resources.

	More information on this is available from our developers; please [ask for help in the forum](https://community.enterpriseweblibrary.org/).

5.	Run the `Web Site` project. If you see a page that says "The page you requested is no longer available", everything is working and you can begin building your system.


### Adding a Database

1.	Create a SQL Server, MySQL, or Oracle database using your preferred tool. We recommend the following naming conventions:

	*	For SQL Server, name your database `SystemShortNameDev`, replacing `SystemShortName` with the value from your `General.xml` configuration file.

	*	For MySQL, name your schema `system_short_name_dev`, replacing `system_short_name` with a lowercased, underscore-separated version of the value from your `General.xml` configuration file.

	*	For Oracle, name your schema whatever you like. We have no convention. The MySQL convention may work.

	Additionally, for SQL Server, we suggest using this script to create the database (after replacing `DatabaseName` and `Path`):

	```SQL
	USE Master

	CREATE DATABASE DatabaseName ON (
	NAME = Data,
			FILENAME = 'Path\DatabaseNameData.mdf',
			SIZE = 100MB,
			FILEGROWTH = 15% )
	LOG ON
	( NAME = Log,
			FILENAME = 'Path\DatabaseNameLog.ldf',
			SIZE = 10MB,
			MAXSIZE = 1000MB,
			FILEGROWTH = 100MB );
	GO
	```

2.	Run one of the following scripts to initialize your database for EWL usage: [SQL Server](Documentation/ReadMeSupplements/DatabaseInitScripts.md#sql-server), [MySQL](Documentation/ReadMeSupplements/DatabaseInitScripts.md#mysql), [Oracle](Documentation/ReadMeSupplements/DatabaseInitScripts.md#oracle).

3.	Add the `<database>` element to the installation configuration files (i.e. the `Standard.xml` files in `Library/Configuration/Installation/Installations`) after the `<administrators>` element:

	* For SQL Server, use `<database xsi:type="SqlServerDatabase" />`. Use the `<database>` child element if you did not follow the naming convention above.

	* For MySQL, use `<database xsi:type="MySqlDatabase" />`. Use the `<database>` child element if you did not follow the naming convention above.

	* For Oracle, use `<database xsi:type="OracleDatabase">` with the `<tnsName>`, `<userAndSchema>`, and `<password>` child elements.

4.	Add the `<database>` element to the development configuration file (i.e. `Library/Configuration/Development.xml`) after the `<webProjects>` element.

Now, when you run `Update-DependentLogic`, data-access code will be generated for your database.


### Deploying Your System

This section is more theoretical than practical since it's generally not a good practice to deploy enterprise software by hand, and without a continuous integration infrastructure.

1.	In the Package Manager Console, run `ExportLogic`.

2.	Somehow copy the exported logic and configuration to the server.

3.	Create and initialize a database using the appropriate steps above. The database name should be slightly different; replace `Dev` or `_dev` with the short name of your installation from the configuration file. Also, for SQL Server, grant the `NETWORK SERVICE` account reader/writer access to the database.

4.	Set up an IIS web site or virtual directory that points at the appropriate folder in the logic that you copied to the server in step 2. Run the application pool as `NETWORK SERVICE`.

Some of the EWL developers use an internal product called the EWL System Manager, which builds, tests, and deploys EWL systems automatically. It's like an ultra-opinionated version of Heroku or AppHarbor. It can be run in the cloud or on-premises. [Let us know in the forum](https://community.enterpriseweblibrary.org/) if you are interested in using this.


## Documentation

Our documentation is included in this repository, in the Documentation directory. See the [table of contents](Documentation/TableOfContents.md).


## Support


###	Q & A

We use the [enterprise-web-library tag](http://stackoverflow.com/questions/tagged/enterprise-web-library) on Stack Overflow for all Q&A. Please post your questions there.


### Community Forum

Visit our [Community Forum](https://community.enterpriseweblibrary.org/), a place for EWL developers to hang out and talk about whatever is on their mind.


### Bug Reports and Feature Requests

We currently use an internal task tracker, but please create an issue on GitHub if you encounter a bug or have a feature request.


## Contributing

Code contributions are welcome! We will accept easily-mergeable changes as quickly as possible. We're working on writing up some detailed expectations, but for now, please refer to the opening section of the [excellent Roslyn contribution page](https://github.com/dotnet/roslyn/wiki/Contributing-Code) and read [Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/).

Please see our [documentation wiki](https://enduracode.fogbugz.com/default.asp?W5) for more information.