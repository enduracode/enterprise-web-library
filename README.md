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

For servers, the only requirement is Windows Server 2012 R2 with IIS enabled.


### Creating a New System

1.	Visit http://ewl.enterpriseweblibrary.org/create-system to download a ZIP file containing your new starter system. Extract the files into a location of your choice.

2.	Open the solution file in Visual Studio. Restore NuGet packages when prompted, and then restart Visual Studio to enable the EWL PowerShell commands.

3.	In the Package Manager Console, run `Update-DependentLogic`. This will apply some IIS Express configuration, copy some files into the `Web Site` project, and generate a few pieces of code and a Web.config file.

4.	Understand the elements in your new system:

	*	The `Library` project, which contains configuration files, provider classes (which allow parts of EWL's behavior to be customized), and a `GlobalInitializer` class (which gives you a place to initialize static fields when your system starts up). `Library` should also be the home of most of your "business logic" and anything else that you would reuse across multiple applications in your system. Right now your system only contains a single web app, but as it grows, you may need another web app or a different type of application, e.g. a Windows service.

	*	The `Web Site` project, which references `Library` and will contain your pages and other resources.

	More information on this is available from our developers; please [ask for help in the forum](https://community.enterpriseweblibrary.org/).

5.	Run the `Web Site` project and make sure you see the Hello World page.


## Contributing

Code contributions are welcome! We will accept easily-mergeable changes as quickly as possible. We're working on writing up some detailed expectations, but for now, please refer to the opening section of the [excellent Roslyn contribution page](https://github.com/dotnet/roslyn/wiki/Contributing-Code) and read [Don't "Push" Your Pull Requests](http://www.igvita.com/2011/12/19/dont-push-your-pull-requests/).

Please see our [documentation wiki](https://enduracode.fogbugz.com/default.asp?W5) for more information.