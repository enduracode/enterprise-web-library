using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.SystemSpecificLogic;
using NodaTime.Text;
using Tewl.IO;
using static MoreLinq.Extensions.AtLeastExtension;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class UpdateDependentLogic: Operation {
	private const string generatedCodeFolderName = "Generated Code";
	private static readonly string serverSideConsoleAppJsonArgument = "{0}UseJsonArguments".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );
	private const string unitTestNamespaceAndAssemblyName = "Tests";

	private static readonly Operation instance = new UpdateDependentLogic();
	public static Operation Instance => instance;
	private UpdateDependentLogic() {}

	bool Operation.IsValid( Installation installation ) => installation is DevelopmentInstallation;

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		var installation = (DevelopmentInstallation)genericInstallation;

		if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true ) {
			StatusStatics.SetStatus( "Running legacy Update-DependentLogic command." );
			var referenceSubstrings = File.ReadAllLines( EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, "Library.csproj" ) )
				.Select( i => i.Trim() )
				.First( i => i.StartsWith( """<PackageReference Include="Ewl""", StringComparison.Ordinal ) )
				.Separate( "\"", false );
			var id = referenceSubstrings[ 1 ];
			var version = referenceSubstrings[ 3 ];
			Console.WriteLine(
				TewlContrib.ProcessTools.RunProgram(
						EwlStatics.CombinePaths(
							ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development
								? EwlStatics.CombinePaths( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), @".nuget\packages" )
								: EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.InstallationPath, @"..\..\.." ),
							id,
							version,
							@"Development Utility\EnterpriseWebLibrary.DevelopmentUtility" ),
						$"""
						 "{genericInstallation.GeneralLogic.Path}" UpdateAllDependentLogic
						 """,
						"",
						true )
					.TrimEnd() );
			StatusStatics.SetStatus( "Ran legacy Update-DependentLogic command." );
		}

		// This block exists because of https://enduracode.kilnhg.com/Review/K164316.
		try {
			IsuStatics.ConfigureIis( false );
			StatusStatics.SetStatus( "Configured IIS." );
		}
		catch {
			StatusStatics.SetStatus( "Did not configure IIS." );
		}

		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.UpdateFileEncodingsSpecified )
			StatusStatics.SetStatus(
				$"Warning: {nameof(installation.DevelopmentInstallationLogic.DevelopmentConfiguration.UpdateFileEncodings)} is present in configuration; please remove it when updates are complete." );

		var bomlessEncoding = new UTF8Encoding( false );
		foreach( var filePath in IoMethods.GetFilePathsInFolder( installation.GeneralLogic.Path, searchPattern: "*.cs", searchOption: SearchOption.AllDirectories )
			        .OrderBy( i => i ) ) {
			if( Path.GetFileName( filePath ).Count( i => i == '.' ) > 1 )
				continue;
			if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true &&
			    new[] { "ISU.cs", "MetaLogicFactory.cs" }.Contains( Path.GetFileName( filePath ), StringComparer.Ordinal ) )
				continue;

			// see https://stackoverflow.com/a/27976558/35349
			using( var reader = new StreamReader( filePath, bomlessEncoding ) ) {
				reader.Peek();
				if( !reader.CurrentEncoding.Equals( bomlessEncoding ) )
					continue;
			}

			var config = installation.DevelopmentInstallationLogic.DevelopmentConfiguration;
			if( !config.UpdateFileEncodingsSpecified || !config.UpdateFileEncodings ) {
				StatusStatics.SetStatus( $"Warning: {filePath} does not have a byte-order mark (BOM); please update its encoding." );
				continue;
			}

			var win1252Encoding = CodePagesEncodingProvider.Instance.GetEncoding( 1252 );
			if( win1252Encoding is null )
				throw new Exception();
			File.WriteAllText( filePath, File.ReadAllText( filePath, win1252Encoding ), Encoding.UTF8 );
		}

		if( !installation.SystemIsTewl() )
			generateDataMigratorProjectCode( installation );

		StatusStatics.SetStatus( "Migrating data." );
		if( installation.ExistingInstallationLogic.MigrateData() is { Length: > 0 } output )
			Console.WriteLine( output );
		StatusStatics.SetStatus( "Migrated data." );

		if( !installation.SystemIsTewl() )
			try {
				copyInFileDependencies( installation );
			}
			catch( Exception e ) {
				const string message = "Failed to copy file dependencies into the installation. Please try the operation again.";
				if( e is UnauthorizedAccessException or IOException )
					throw new UserCorrectableException( message, e );
				throw new Exception( message, e );
			}

		// Generate code.
		if( installation.DevelopmentInstallationLogic.SystemIsEwl ) {
			generateCodeForProject(
				installation,
				"",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName ),
				"EnterpriseWebLibrary",
				writer => {
					writer.WriteLine( "using System;" );
					writer.WriteLine( "using System.Collections.Generic;" );
					writer.WriteLine( "using System.Diagnostics.CodeAnalysis;" );
					writer.WriteLine( "using System.Globalization;" );
					writer.WriteLine( "using System.Linq;" );
					writer.WriteLine( "using System.Threading;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
					writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing;" );
					writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework.Core;" );
					writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;" );
					writer.WriteLine( "using Newtonsoft.Json;" );
					writer.WriteLine( "using Newtonsoft.Json.Linq;" );
					writer.WriteLine( "using NodaTime;" );
					writer.WriteLine( "using NodaTime.Text;" );
					writer.WriteLine( "using Tewl.InputValidation;" );
					writer.WriteLine( "using Tewl.Tools;" );
					writer.WriteLine();
					writer.WriteLine( "namespace EnterpriseWebLibrary {" );
					writer.WriteLine( "partial class EwlStatics {" );
					CodeGenerationStatics.AddSummaryDocComment( writer, "The date/time at which this version of EWL was built." );
					writer.WriteLine(
						"public static readonly DateTimeOffset EwlBuildDateTime = {0};".FormatWith( AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) ) );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
					writer.WriteLine();
					CodeGeneration.WebFramework.WebFrameworkStatics.Generate(
						writer,
						EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName ),
						"EnterpriseWebLibrary",
						true,
						generatedCodeFolderName.ToCollection(),
						StaticFile.FrameworkStaticFilesSourceFolderPath,
						"",
						out var resourceSerializationWriter );
					writer.WriteLine();
					writer.WriteLine( "namespace EnterpriseWebLibrary.EnterpriseWebFramework {" );
					writer.WriteLine( "internal static class ResourceSerializationStatics {" );
					resourceSerializationWriter( "" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				} );
			generateCodeForProject(
				installation,
				"Development Utility",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Development Utility" ),
				"EnterpriseWebLibrary.DevelopmentUtility",
				_ => {},
				runtimeIdentifier: "win-x64" );
			generateCodeForProject(
				installation,
				"MySQL Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.MySqlProviderProjectName ),
				"EnterpriseWebLibrary.MySql",
				_ => {} );
			generateCodeForProject(
				installation,
				"Oracle Database Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.OracleDatabaseProviderProjectName ),
				"EnterpriseWebLibrary.OracleDatabase",
				_ => {} );
			generateCodeForProject(
				installation,
				"OpenID Connect Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.OpenIdConnectProviderProjectName ),
				"EnterpriseWebLibrary.OpenIdConnect",
				_ => {} );
			generateCodeForProject(
				installation,
				"SAML Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.SamlProviderProjectName ),
				"EnterpriseWebLibrary.Saml",
				_ => {} );
			generateCodeForProject(
				installation,
				"PDF Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.PdfProviderProjectName ),
				"EnterpriseWebLibrary.Pdf",
				_ => {} );
			generateCodeForProject(
				installation,
				"Word Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.ProviderProjectFolderName, AppStatics.WordProviderProjectName ),
				"EnterpriseWebLibrary.Word",
				_ => {} );
		}
		if( installation.SystemIsTewl() )
			generateCodeForProject(
				installation,
				"",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.TewlProjectPath ),
				"Tewl",
				writer => {
					writer.WriteLine( "using System.Globalization;" );
					writer.WriteLine();
					writer.WriteLine( "namespace Tewl;" );
					writer.WriteLine();
					writer.WriteLine( "partial class TewlStatics {" );
					CodeGenerationStatics.AddSummaryDocComment( writer, "The date/time at which this version of TEWL was built." );
					writer.WriteLine(
						"public static readonly DateTimeOffset TewlBuildDateTime = {0};".FormatWith( AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) ) );
					writer.WriteLine( "}" );
				} );
		generateLibraryCode( installation );
		foreach( var i in installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Select( ( app, index ) => ( app, index ) ) )
			generateWebProjectCode( installation, i.app, i.index );
		foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices )
			generateWindowsServiceCode( installation, service );
		foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable )
			generateServerSideConsoleProjectCode( installation, project );
		if( !installation.SystemIsTewl() )
			generateDataCleanerProject( installation );
		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null )
			generateCodeForProject(
				installation,
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name,
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name ),
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.NamespaceAndAssemblyName,
				_ => {},
				runtimeIdentifier: "win-x64",
				selfContained: true );
		if( !installation.SystemIsTewl() )
			generateUnitTestProjectCode( installation );

		generateXmlSchemaLogicForInstallationConfigurationFile( installation, "Custom" );
		generateXmlSchemaLogicForInstallationConfigurationFile( installation, "Shared" );
		generateXmlSchemaLogicForOtherFiles( installation );

		using( var writer = new StreamWriter( EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Directory.Build.props" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( "<!-- generated by {0} to provide the target framework for the initial dotnet restore -->".FormatWith( EwlStatics.EwlInitialism ) );
			writer.WriteLine( "<Project>" );
			writer.WriteLine( "<PropertyGroup>" );
			writer.WriteLine( "<TargetFramework>{0}</TargetFramework>".FormatWith( ConfigurationStatics.TargetFramework ) );
			writer.WriteLine( "</PropertyGroup>" );
			writer.WriteLine( "</Project>" );
		}

		generateEditorConfig(
			installation.GeneralLogic.Path,
			writer => {
				writer.WriteLine( "dotnet_style_collection_initializer = false" );
				writer.WriteLine( "csharp_style_prefer_primary_constructors = false" );
				writer.WriteLine( "dotnet_diagnostic.IDE0051.severity = none" );
				writer.WriteLine( "dotnet_diagnostic.IDE1006.severity = none" );
			} );
		updateReSharperSettings( installation );

		if( !installation.DevelopmentInstallationLogic.SystemIsEwl && !installation.SystemIsTewl() ) {
			if( Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.MercurialRepositoryFolderName ) ) )
				updateIgnoreFile( installation, false );
			if( File.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, ".gitignore" ) ) )
				updateIgnoreFile( installation, true );
		}
	}

	private void generateDataMigratorProjectCode( DevelopmentInstallation installation ) {
		var projectPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, IsuStatics.DataMigratorProjectName );

		if( !File.Exists( EwlStatics.CombinePaths( projectPath, $"{IsuStatics.DataMigratorProjectName}.csproj" ) ) ) {
			IoMethods.DeleteFolder( projectPath );
			Directory.CreateDirectory( projectPath );
			using var writer = new StreamWriter( EwlStatics.CombinePaths( projectPath, $"{IsuStatics.DataMigratorProjectName}.ewlt.csproj" ), false, Encoding.UTF8 );
			writer.WriteLine(
				"""
				<Project Sdk="Microsoft.NET.Sdk">

				  <PropertyGroup>
				    <OutputType>Exe</OutputType>
				  </PropertyGroup>

				</Project>
				""" );
		}

		generateEditorConfig( projectPath, writer => { writer.WriteLine( "resharper_unused_type_global_highlighting = none" ); } );

		generateCodeForProject(
			installation,
			IsuStatics.DataMigratorProjectName,
			projectPath,
			IsuStatics.DataMigratorNamespaceAndAssemblyName,
			writer => {
				var providerName = StringTools.ConcatenateWithDelimiter(
					".",
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName,
					SystemSpecificLogicStatics.ProvidersFolderAndNamespaceName,
					ExternalFunctionalityStatics.ProviderName );
				var providerExpression =
					File.Exists( EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, externalFunctionalityProviderPath ) )
						? $"new {providerName}()"
						: "null";
				writer.Write( $"return DataMigrationOps.MigrateData( {providerExpression} );" );
			},
			runtimeIdentifier: "win-x64" );
	}

	private void copyInFileDependencies( DevelopmentInstallation installation ) {
		if( installation is RecognizedDevelopmentInstallation recognizedInstallation )
			recognizedInstallation.KnownSystemLogic.DownloadAsposeLicenses( installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath );

		if( installation.DevelopmentInstallationLogic.SystemIsEwl )
			foreach( var fileName in GlobalStatics.ConfigurationXsdFileNames )
				IoMethods.CopyFile(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.CoreProjectName, "Configuration", fileName + FileExtensions.Xsd ),
					EwlStatics.CombinePaths(
						InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
						InstallationFileStatics.FilesFolderName,
						fileName + FileExtensions.Xsd ) );

		// If web projects exist for this installation, copy in web-framework static files.
		else if( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Any() ) {
			var webFrameworkStaticFilesFolderPath = EwlStatics.CombinePaths(
				installation.GeneralLogic.Path,
				InstallationFileStatics.WebFrameworkStaticFilesFolderName );
			IoMethods.DeleteFolder( webFrameworkStaticFilesFolderPath );
			IoMethods.CopyFolder(
				StaticFile.GetFrameworkStaticFilesFolderPath( ConfigurationStatics.InstallationConfiguration ),
				webFrameworkStaticFilesFolderPath,
				false );
			if( ConfigurationStatics.InstallationConfiguration.InstallationType == InstallationType.Development )
				IoMethods.DeleteFolder( EwlStatics.CombinePaths( webFrameworkStaticFilesFolderPath, AppStatics.StaticFileLogicFolderName ) );
		}
	}

	private void generateLibraryCode( DevelopmentInstallation installation ) {
		generateCodeForProject(
			installation,
			"Library",
			installation.DevelopmentInstallationLogic.LibraryPath,
			installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName,
			writer => {
				if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue )
					writer.WriteLine( "#if EWL_NEW" );

				// Don't add "using System" here. It will create a huge number of ReSharper warnings in the generated code file.
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Data;" ); // Necessary for stored procedure logic
				writer.WriteLine( "using System.Data.Common;" );
				writer.WriteLine( "using System.Diagnostics;" ); // Necessary for ServerSideConsoleAppStatics
				writer.WriteLine( "using System.Diagnostics.CodeAnalysis;" );
				writer.WriteLine( "using System.Linq;" );
				writer.WriteLine( "using System.Threading;" ); // used by LazyThreadSafetyMode in TableRetrievalStatics
				if( !installation.SystemIsTewl() ) {
					writer.WriteLine( "using EnterpriseWebLibrary;" );
					writer.WriteLine( "using EnterpriseWebLibrary.Caching;" );
					writer.WriteLine( "using EnterpriseWebLibrary.Collections;" ); // Necessary for row constants
					writer.WriteLine( "using EnterpriseWebLibrary.Configuration;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.RetrievalCaching;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.RevisionHistory;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.StandardModification;" );
					writer.WriteLine( "using EnterpriseWebLibrary.Email;" );
					writer.WriteLine( "using Newtonsoft.Json;" );
					writer.WriteLine( "using Newtonsoft.Json.Linq;" );
					writer.WriteLine( "using NodaTime;" );
					writer.WriteLine( "using Tewl.InputValidation;" );
					writer.WriteLine( "using Tewl.Tools;" );
				}

				if( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Any() ) {
					writer.WriteLine();
					writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
					writer.WriteLine( "public static class WebApplicationNames {" );
					foreach( var i in installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications )
						writer.WriteLine( "public const string {0} = \"{1}\";".FormatWith( EwlStatics.GetCSharpIdentifier( i.Name.EnglishToPascal() ), i.Name ) );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}
				writer.WriteLine();
				TypedCssClassStatics.Generate(
					installation.DevelopmentInstallationLogic.LibraryPath.ToCollection()
						.Concat( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Select( i => i.Path ) ),
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName,
					writer );
				writer.WriteLine();
				generateServerSideConsoleAppStatics( writer, installation );
				CodeGeneration.DataAccess.DataAccessStatics.GenerateDataAccessCode( writer, installation );

				var emailTemplateFolderPath = EwlStatics.CombinePaths(
					InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
					InstallationFileStatics.FilesFolderName,
					EmailTemplate.TemplateFolderName );
				if( Directory.Exists( emailTemplateFolderPath ) ) {
					writer.WriteLine();
					writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
					writer.WriteLine( "public static class EmailTemplates {" );
					foreach( var i in IoMethods.GetFileNamesInFolder( emailTemplateFolderPath, searchPattern: "*.html" ) )
						writer.WriteLine(
							"public static readonly EmailTemplateName {0} = new EmailTemplateName( \"{1}\" );".FormatWith(
								EwlStatics.GetCSharpIdentifier( Path.GetFileNameWithoutExtension( i ).EnglishToPascal() ),
								i ) );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}

				if( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Any() ) {
					writer.WriteLine();
					CodeGeneration.WebFramework.WebFrameworkStatics.Generate(
						writer,
						installation.DevelopmentInstallationLogic.LibraryPath,
						installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName,
						false,
						InstallationConfiguration.ConfigurationFolderName.ToCollection()
							.Concat(
								installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true
									? $"{InstallationConfiguration.ConfigurationFolderName} New".ToCollection()
									: [ ] )
							.Append( InstallationFileStatics.FilesFolderName )
							.Append( generatedCodeFolderName ),
						null,
						null,
						out var resourceSerializationWriter );
					writer.WriteLine();
					writer.WriteLine(
						"namespace {0}.Providers {{".FormatWith( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName ) );
					writer.WriteLine( "internal class ResourceSerialization: SystemResourceSerializationProvider {" );
					resourceSerializationWriter( "SystemResourceSerializationProvider" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}

				if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue )
					writer.WriteLine( "#endif" );
			},
			includeWebFrameworkUsingDirectives: !installation.SystemIsTewl() );
	}

	private void generateServerSideConsoleAppStatics( TextWriter writer, DevelopmentInstallation installation ) {
		if( !installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable.Any() )
			return;

		writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
		writer.WriteLine( "public static class ServerSideConsoleAppStatics {" );
		foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable ) {
			writer.WriteLine(
				"public static void Start" + project.Name.EnglishToPascal() +
				"( IEnumerable<string> arguments, string input, string errorMessageIfAlreadyRunning = \"\" ) {" );
			writer.WriteLine( "if( errorMessageIfAlreadyRunning.Any() && Process.GetProcessesByName( \"" + project.NamespaceAndAssemblyName + "\" ).Any() )" );
			writer.WriteLine( "throw new DataModificationException( errorMessageIfAlreadyRunning );" );

			var programPath = "EwlStatics.CombinePaths( ConfigurationStatics.InstallationConfiguration.InstallationPath, \"" + project.Name +
			                  "\", ConfigurationStatics.ServerSideConsoleAppRelativeFolderPath, \"" + project.NamespaceAndAssemblyName + "\" )";
			var runProgramExpression =
				"EnterpriseWebLibrary.TewlContrib.ProcessTools.RunProgram( {0}, \"{1}\", Newtonsoft.Json.JsonConvert.SerializeObject( arguments, Newtonsoft.Json.Formatting.None ) + System.Environment.NewLine + input, false )"
					.FormatWith( programPath, serverSideConsoleAppJsonArgument );

			writer.WriteLine( "if( EwfRequest.Current is not null )" );
			writer.WriteLine( "AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod( () => " + runProgramExpression + " );" );
			writer.WriteLine( "else" );
			writer.WriteLine( runProgramExpression + ";" );

			writer.WriteLine( "}" );
		}
		writer.WriteLine( "}" );
		writer.WriteLine( "}" );
	}

	private void generateWebProjectCode( DevelopmentInstallation installation, WebApplication application, int index ) {
		var project = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.GetWebProject( application.Name );

		Directory.CreateDirectory( EwlStatics.CombinePaths( application.Path, StaticFile.AppStaticFilesFolderName ) );

		generateCodeForProject(
			installation,
			project.name,
			application.Path,
			project.NamespaceAndAssemblyName,
			writer => {
				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Collections.ObjectModel;" );
				writer.WriteLine( "using System.Diagnostics.CodeAnalysis;" );
				writer.WriteLine( "using System.Globalization;" );
				writer.WriteLine( "using System.Linq;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( "using Newtonsoft.Json;" );
				writer.WriteLine( "using Newtonsoft.Json.Linq;" );
				writer.WriteLine( "using NodaTime;" );
				writer.WriteLine( "using NodaTime.Text;" );
				writer.WriteLine( "using Tewl.InputValidation;" );
				writer.WriteLine( "using Tewl.Tools;" );
				writer.WriteLine();
				writer.WriteLine( "namespace {0}.Providers {{".FormatWith( project.NamespaceAndAssemblyName ) );
				writer.WriteLine( "internal partial class RequestDispatching: AppRequestDispatchingProvider {" );
				writer.WriteLine(
					"protected override UrlPattern GetStaticFilesFolderUrlPattern( string urlSegment ) => StaticFiles.FolderSetup.UrlPatterns.Literal( urlSegment );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
				writer.WriteLine();
				CodeGeneration.WebFramework.WebFrameworkStatics.Generate(
					writer,
					application.Path,
					project.NamespaceAndAssemblyName,
					false,
					generatedCodeFolderName.ToCollection(),
					StaticFile.AppStaticFilesFolderName,
					$"RequestDispatchingStatics.GetAppProvider( applicationName: {installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName}.WebApplicationNames.{EwlStatics.GetCSharpIdentifier( project.name.EnglishToPascal() )} ).GetFrameworkUrlParent()",
					out var resourceSerializationWriter );
				writer.WriteLine();
				writer.WriteLine( "namespace {0}.Providers {{".FormatWith( project.NamespaceAndAssemblyName ) );
				writer.WriteLine( "internal class ResourceSerialization: AppResourceSerializationProvider {" );
				resourceSerializationWriter( "AppResourceSerializationProvider" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			},
			runtimeIdentifier: "win-x64",
			includeWebFrameworkUsingDirectives: true );

		var configurationFilesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "Web Project Configuration" );
		try {
			File.WriteAllText(
				application.WebConfigFilePath,
				File.ReadAllText( EwlStatics.CombinePaths( configurationFilesFolderPath, "web.config" ) )
					.Replace( "@@InitializationTimeoutSeconds", DurationPattern.CreateWithInvariantCulture( "%S" ).Format( EwfOps.InitializationTimeout ) ),
				Encoding.UTF8 );
		}
		catch( Exception e ) {
			const string message = "Failed to write web configuration file.";
			if( e is UnauthorizedAccessException )
				throw new UserCorrectableException( message, e );
			throw new ApplicationException( message, e );
		}
		using( var writer = new StreamWriter( EwlStatics.CombinePaths( application.Path, "Directory.Build.targets" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( "<Project>" );
			writer.WriteLine( """<ItemGroup><Content Remove="web.config" /></ItemGroup>""" );
			writer.WriteLine( """<ItemGroup><None Include="web.config" /></ItemGroup>""" );
			writer.WriteLine( "</Project>" );
		}

		Directory.CreateDirectory( EwlStatics.CombinePaths( application.Path, "Properties" ) );
		File.WriteAllText(
			EwlStatics.CombinePaths( application.Path, @"Properties\launchSettings.json" ),
			File.ReadAllText( EwlStatics.CombinePaths( configurationFilesFolderPath, "launchSettings.json" ) )
				.Replace( "@@NonsecurePort", ( 44311 + index * 2 ).ToString() )
				.Replace( "@@SecurePort", ( 44310 + index * 2 ).ToString() )
				.Replace(
					"@@Path",
					installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName +
					( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.AtLeast( 2 ) ? application.Name.EnglishToPascal() : "" ) ),
			Encoding.UTF8 );
	}

	private void generateWindowsServiceCode( DevelopmentInstallation installation, WindowsService service ) {
		generateCodeForProject(
			installation,
			service.Name,
			EwlStatics.CombinePaths( installation.GeneralLogic.Path, service.Name ),
			service.NamespaceAndAssemblyName,
			writer => {
				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.ComponentModel;" );
				writer.WriteLine( "using System.ServiceProcess;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( "using EnterpriseWebLibrary.WindowsServiceFramework;" );
				writer.WriteLine();
				writer.WriteLine( "namespace " + service.NamespaceAndAssemblyName + " {" );

				writer.WriteLine( "internal static partial class Program {" );

				writer.WriteLine( "[ MTAThread ]" );
				writer.WriteLine( "private static void Main() {" );
				writer.WriteLine( "SystemInitializer? globalInitializer = null;" );
				writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
				writer.WriteLine( "var dataAccessState = new System.Lazy<DataAccessState>( () => new DataAccessState() );" );
				writer.WriteLine(
					"GlobalInitializationOps.InitStatics( globalInitializer!, \"{0}\", false, mainDataAccessStateGetter: () => dataAccessState.Value!, useLongDatabaseTimeouts: true );"
						.FormatWith( service.Name ) );
				writer.WriteLine( "try {" );
				writer.WriteLine(
					"TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( () => ServiceBase.Run( new ServiceBaseAdapter( new " + service.Name.EnglishToPascal() +
					"() ) ) );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer? globalInitializer );" );

				writer.WriteLine( "}" );

				writer.WriteLine( "internal partial class " + service.Name.EnglishToPascal() + ": WindowsServiceBase {" );
				writer.WriteLine( "internal " + service.Name.EnglishToPascal() + "() {}" );
				writer.WriteLine( "string WindowsServiceBase.Name { get { return \"" + service.Name + "\"; } }" );
				writer.WriteLine( "}" );

				writer.WriteLine( "}" );
			},
			runtimeIdentifier: "win-x64" );
	}

	private void generateDataCleanerProject( DevelopmentInstallation installation ) {
		var projectPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, IsuStatics.DataCleanerProjectName );

		IoMethods.DeleteFolder( projectPath );
		Directory.CreateDirectory( projectPath );
		using( var writer = new StreamWriter( EwlStatics.CombinePaths( projectPath, $"{IsuStatics.DataCleanerProjectName}.csproj" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( "<Project Sdk=\"Microsoft.NET.Sdk\">" );
			writer.WriteLine( "<PropertyGroup>" );
			writer.WriteLine( "<OutputType>Exe</OutputType>" );
			writer.WriteLine( "</PropertyGroup>" );
			writer.WriteLine( "<ItemGroup>" );
			writer.WriteLine(
				$"""<ProjectReference Include="..\Library\{( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue ? "Library New" : "Library" )}.csproj" />""" );
			writer.WriteLine( "</ItemGroup>" );
			writer.WriteLine( "</Project>" );
		}

		using( var writer = new StreamWriter( EwlStatics.CombinePaths( projectPath, "Program.cs" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( $"namespace {IsuStatics.DataCleanerNamespaceAndAssemblyName};" );
			writer.WriteLine();
			writer.WriteLine( "partial class Program {" );
			writer.WriteLine( "static partial void ewlMain( IReadOnlyList<string> arguments ) {" );
			writer.WriteLine( "DataCleanupOps.CleanUpData();" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		generateServerSideConsoleProjectCode(
			installation,
			new ServerSideConsoleProject { Name = IsuStatics.DataCleanerProjectName, NamespaceAndAssemblyName = IsuStatics.DataCleanerNamespaceAndAssemblyName } );
	}

	private void generateServerSideConsoleProjectCode( DevelopmentInstallation installation, ServerSideConsoleProject project ) {
		generateCodeForProject(
			installation,
			project.Name,
			EwlStatics.CombinePaths( installation.GeneralLogic.Path, project.Name ),
			project.NamespaceAndAssemblyName,
			writer => {
				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( $"using {installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName};" );
				writer.WriteLine();
				writer.WriteLine( "namespace {0};".FormatWith( project.NamespaceAndAssemblyName ) );
				writer.WriteLine();
				writer.WriteLine( "internal static partial class Program {" );

				writer.WriteLine( "private static int Main( string[] args ) {" );
				writer.WriteLine( "var dataAccessState = new Lazy<DataAccessState>( () => new DataAccessState() );" );
				writer.WriteLine(
					"GlobalInitializationOps.InitStatics( new GlobalInitializer(), \"{0}\", false, mainDataAccessStateGetter: () => dataAccessState.Value! );".FormatWith(
						project.Name ) );
				writer.WriteLine( "try {" );
				writer.WriteLine( "return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( () => {" );

				// See https://stackoverflow.com/a/44135529/35349.
				writer.WriteLine( "Console.SetIn( new StreamReader( Console.OpenStandardInput(), Console.InputEncoding, false, 4096 ) );" );

				writer.WriteLine(
					"ewlMain( args.Length > 0 && string.Equals( args[ 0 ], \"{0}\", StringComparison.Ordinal ) ? Newtonsoft.Json.JsonConvert.DeserializeObject<ImmutableArray<string>>( Console.ReadLine()! ) : args );"
						.FormatWith( serverSideConsoleAppJsonArgument ) );
				writer.WriteLine( "} );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "static partial void ewlMain( IReadOnlyList<string> arguments );" );

				writer.WriteLine( "}" );
			},
			runtimeIdentifier: "win-x64" );
	}

	private void generateUnitTestProjectCode( DevelopmentInstallation installation ) {
		var projectPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, UnitTestingInitializationOps.UnitTestProjectName );

		if( !File.Exists( EwlStatics.CombinePaths( projectPath, $"{UnitTestingInitializationOps.UnitTestProjectName}.csproj" ) ) ) {
			IoMethods.DeleteFolder( projectPath );
			Directory.CreateDirectory( projectPath );
			using var writer = new StreamWriter(
				EwlStatics.CombinePaths( projectPath, $"{UnitTestingInitializationOps.UnitTestProjectName}.ewlt.csproj" ),
				false,
				Encoding.UTF8 );
			writer.WriteLine(
				$"""
				 <Project Sdk="Microsoft.NET.Sdk">

				   <ItemGroup>
				     <ProjectReference Include="..\Library\{( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue ? "Library New" : "Library" )}.csproj" />
				   </ItemGroup>

				 </Project>
				 """ );
		}

		// Use a runtime identifier because this project ends up compiling like a console app (due to the Microsoft.NET.Test.Sdk dependency above) rather than a class library.
		generateCodeForProject(
			installation,
			UnitTestingInitializationOps.UnitTestProjectName,
			projectPath,
			unitTestNamespaceAndAssemblyName,
			writer => {
				writer.WriteLine( "using NUnit.Framework;" );
				writer.WriteLine( $"using {installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName};" );
				writer.WriteLine();
				writer.WriteLine( "[ SetUpFixture ]" );
				writer.WriteLine( "public partial class NUnitInitializer {" );
				writer.WriteLine( "private class AppInitializer: SystemInitializer {" );
				writer.WriteLine( "void SystemInitializer.InitStatics() => initStatics();" );
				writer.WriteLine( "void SystemInitializer.CleanUpStatics() => cleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine();
				CodeGenerationStatics.AddSummaryDocComment( writer, "Performs unit-testing-specific initialization." );
				writer.WriteLine( "static partial void initStatics();" );
				CodeGenerationStatics.AddSummaryDocComment( writer, "Performs unit-testing-specific cleanup." );
				writer.WriteLine( "static partial void cleanUpStatics();" );
				writer.WriteLine();
				writer.WriteLine( "[ OneTimeSetUp ]" );
				writer.WriteLine(
					"public void InitStatics() => UnitTestingInitializationOps.InitStatics( new GlobalInitializer(), appInitializer: new AppInitializer() );" );
				writer.WriteLine( "[ OneTimeTearDown ]" );
				writer.WriteLine( "public void CleanUpStatics() => UnitTestingInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
			},
			runtimeIdentifier: "win-x64" );
	}

	private void generateCodeForProject(
		DevelopmentInstallation installation, string projectName, string projectPath, string assemblyNameAndRootNamespace, Action<TextWriter> codeWriter,
		string runtimeIdentifier = "", bool selfContained = false, bool includeWebFrameworkUsingDirectives = false ) {
		using( var writer = new StreamWriter( EwlStatics.CombinePaths( projectPath, "Directory.Build.props" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( "<Project>" );
			writer.WriteLine( "<PropertyGroup>" );

			var projectFilePaths = ".csproj".ToCollection()
				.Append( ".ewlt.csproj" )
				.Select( extension => EwlStatics.CombinePaths( projectPath, Path.GetFileName( projectPath ) + extension ) );
			var projectFile =
				installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue && projectName.Equals( "Library", StringComparison.Ordinal )
					? File.ReadAllText( EwlStatics.CombinePaths( projectPath, "Library New.csproj" ) )
					: projectFilePaths.Where( File.Exists ).Select( File.ReadAllText ).FirstOrDefault();
			if( projectFile is null ) {
				StatusStatics.SetStatus( "Warning: Failed to locate the project file for {0}.".FormatWith( projectName ) );
				projectFile = "";
			}

			void writeMsBuildProperty( string property ) {
				writer.WriteLine( property );
				if( projectFile.Contains( property, StringComparison.OrdinalIgnoreCase ) )
					StatusStatics.SetStatus(
						"Warning: The project file for {0} contains {1}, which is generated automatically by {2}.".FormatWith(
							projectName,
							property,
							EwlStatics.EwlInitialism ) );
			}

			// common MSBuild properties; see https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties
			writeMsBuildProperty( "<AssemblyName>{0}</AssemblyName>".FormatWith( assemblyNameAndRootNamespace ) );
			writeMsBuildProperty( "<RootNamespace>{0}</RootNamespace>".FormatWith( assemblyNameAndRootNamespace ) );
			if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue &&
			    projectName.Equals( "Library", StringComparison.Ordinal ) ) {
				writeMsBuildProperty( """<BaseOutputPath Condition="'$(MSBuildProjectName)'=='Library New'">bin New</BaseOutputPath>""" );
				writeMsBuildProperty( """<BaseIntermediateOutputPath Condition="'$(MSBuildProjectName)'=='Library New'">obj New</BaseIntermediateOutputPath>""" );
				writeMsBuildProperty( """<DefineConstants Condition="'$(MSBuildProjectName)'=='Library New'">EWL_NEW</DefineConstants>""" );
			}

			// framework properties; see https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#framework-properties
			writeMsBuildProperty( "<TargetFramework>{0}</TargetFramework>".FormatWith( ConfigurationStatics.TargetFramework ) );

			writeMsBuildProperty( "<Version>{0}</Version>".FormatWith( "{0}.0.{1}.0".FormatWith( installation.CurrentMajorVersion, installation.NextBuildNumber ) ) );

			// assembly attributes; see https://docs.microsoft.com/en-us/dotnet/standard/assembly/set-attributes
			writeMsBuildProperty( "<GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>" );
			writeMsBuildProperty( "<Product>{0}</Product>".FormatWith( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName ) );
			writeMsBuildProperty(
				"<AssemblyTitle>{0}</AssemblyTitle>".FormatWith(
					installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + projectName.PrependDelimiter( " - " ) ) );

			// package properties; see https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-target
			writeMsBuildProperty( "<PackageVersion>0</PackageVersion>" ); // Clear since we create both prerelease and stable packages with different version numbers.

			// publish-related properties; see https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#publish-related-properties
			if( runtimeIdentifier.Any() ) {
				writeMsBuildProperty( "<RuntimeIdentifier>{0}</RuntimeIdentifier>".FormatWith( runtimeIdentifier ) );
				if( selfContained )
					writeMsBuildProperty( "<SelfContained>true</SelfContained>" );
			}

			// build-related properties; see https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#build-related-properties
			writeMsBuildProperty( "<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>" );
			writeMsBuildProperty( "<Nullable>enable</Nullable>" );
			writeMsBuildProperty( "<CopyDebugSymbolFilesFromPackages>true</CopyDebugSymbolFilesFromPackages>" );

			if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue &&
			    projectName.Equals( "Library", StringComparison.Ordinal ) ) {
				writeMsBuildProperty(
					$"""<DefaultItemExcludesInProjectFolder Condition="'$(MSBuildProjectName)'=='Library New'">$(DefaultItemExcludesInProjectFolder);Directory.Build.props;Directory.Build.targets;**/*{CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension};bin/**;obj/**;Generated Code/ISU.cs</DefaultItemExcludesInProjectFolder>""" );
				writeMsBuildProperty(
					$"""<DefaultItemExcludesInProjectFolder Condition="'$(MSBuildProjectName)'=='Library'">$(DefaultItemExcludesInProjectFolder);Directory.Build.props;Directory.Build.targets;**/*{CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension};bin New/**;obj New/**</DefaultItemExcludesInProjectFolder>""" );
			}
			else
				writeMsBuildProperty(
					$"<DefaultItemExcludesInProjectFolder>$(DefaultItemExcludesInProjectFolder);Directory.Build.props;Directory.Build.targets;**/*{CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension}</DefaultItemExcludesInProjectFolder>" );

			// runtime configuration properties; see https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#runtime-configuration-properties
			if( runtimeIdentifier.Any() )
				writeMsBuildProperty( "<GarbageCollectionAdaptationMode>0</GarbageCollectionAdaptationMode>" );

			// see https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#configuring-nuget-audit
			writeMsBuildProperty( "<NuGetAuditMode>direct</NuGetAuditMode>" );

			// affects only web apps; see https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config?view=aspnetcore-6.0
			writeMsBuildProperty( "<IsTransformWebConfigDisabled>true</IsTransformWebConfigDisabled>" );

			writer.WriteLine( "</PropertyGroup>" );


			// items; see https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#items

			if( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue &&
			    projectName.Equals( "Library", StringComparison.Ordinal ) )
				writer.WriteLine( """<ItemGroup Condition="'$(MSBuildProjectName)'=='Library New'">""" );
			else
				writer.WriteLine( "<ItemGroup>" );

			if( projectName.Equals( IsuStatics.DataMigratorProjectName, StringComparison.Ordinal ) && File.Exists(
				    EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, externalFunctionalityProviderPath ) ) )
				writer.WriteLine( $"""<Compile Include="..\Library\{externalFunctionalityProviderPath}" Visible="false" />""" );

			if( projectName.Equals( UnitTestingInitializationOps.UnitTestProjectName, StringComparison.Ordinal ) ) {
				writer.WriteLine( """<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />""" );

				writer.WriteLine( """<PackageReference Include="NUnit.Analyzers" Version="4.10.0">""" );
				writer.WriteLine( "<PrivateAssets>all</PrivateAssets>" );
				writer.WriteLine( "<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>" );
				writer.WriteLine( "</PackageReference>" );

				writer.WriteLine( """<PackageReference Include="NUnit3TestAdapter" Version="5.2.0" />""" );
			}
			else if( !installation.DevelopmentInstallationLogic.SystemIsEwl || !projectName.EndsWith( " Provider", StringComparison.Ordinal ) )
				writer.WriteLine( $"""<InternalsVisibleTo Include="{unitTestNamespaceAndAssemblyName}" />""" );

			writer.WriteLine( """<Using Include="System" />""" );
			writer.WriteLine( """<Using Include="System.Collections.Generic" />""" );
			writer.WriteLine( """<Using Include="System.IO" />""" );
			writer.WriteLine( """<Using Include="System.Linq" />""" );

			if( !installation.SystemIsTewl() ) {
				writer.WriteLine( """<Using Include="EnterpriseWebLibrary" />""" );
				writer.WriteLine( """<Using Include="EnterpriseWebLibrary.DataValueManagement" />""" );
				if( includeWebFrameworkUsingDirectives ) {
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ComponentDisplay" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.ElementBase.Classification" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Ethereal" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Flow" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.ContentInfrastructure.GeneralContentModels.Phrasing" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.Core" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic" />""" );
					writer.WriteLine( """<Using Include="EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes" />""" );
				}
			}
			writer.WriteLine( """<Using Include="Tewl" />""" );
			writer.WriteLine( """<Using Include="Tewl.Tools" />""" );

			writer.WriteLine( """<Using Include="Humanizer.StringExtensions"><Static>True</Static></Using>""" );

			writer.WriteLine( "</ItemGroup>" );


			writer.WriteLine( "</Project>" );
		}

		var generatedCodeFolderPath = EwlStatics.CombinePaths( projectPath, generatedCodeFolderName );
		if( !installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue )
			IoMethods.DeleteFolder( generatedCodeFolderPath );
		Directory.CreateDirectory( generatedCodeFolderPath );
		using( var writer = new StreamWriter( EwlStatics.CombinePaths( generatedCodeFolderPath, "Main.g.cs" ), false, Encoding.UTF8 ) ) {
			writer.WriteLine( "#nullable enable" );
			codeWriter( writer );
		}
	}

	private string externalFunctionalityProviderPath =>
		EwlStatics.CombinePaths( SystemSpecificLogicStatics.ProvidersFolderAndNamespaceName, ExternalFunctionalityStatics.ProviderName + ".cs" );

	private void generateXmlSchemaLogicForInstallationConfigurationFile( DevelopmentInstallation installation, string schemaFileName ) {
		var schemaPathInProject = EwlStatics.CombinePaths(
			$"""{( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true ? $"{InstallationConfiguration.ConfigurationFolderName} New" : InstallationConfiguration.ConfigurationFolderName )}\Installation""",
			schemaFileName + FileExtensions.Xsd );
		if( File.Exists( EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, schemaPathInProject ) ) )
			generateXmlSchemaLogic(
				installation.DevelopmentInstallationLogic.LibraryPath,
				schemaPathInProject,
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".Configuration.Installation",
				$"Installation {schemaFileName} Configuration.cs",
				true );
	}

	private void generateXmlSchemaLogicForOtherFiles( DevelopmentInstallation installation ) {
		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.xmlSchemas != null )
			foreach( var xmlSchema in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.xmlSchemas )
				generateXmlSchemaLogic(
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, xmlSchema.project ),
					xmlSchema.pathInProject,
					xmlSchema.@namespace,
					xmlSchema.codeFileName,
					xmlSchema.useSvcUtil );
	}

	private void generateXmlSchemaLogic( string projectPath, string schemaPathInProject, string nameSpace, string codeFileName, bool useSvcUtil ) {
		var projectGeneratedCodeFolderPath = EwlStatics.CombinePaths( projectPath, generatedCodeFolderName );
		if( useSvcUtil )
			try {
				TewlContrib.ProcessTools.RunProgram(
					EwlStatics.CombinePaths( AppStatics.DotNetToolsFolderPath, "SvcUtil" ),
					"/d:\"" + projectGeneratedCodeFolderPath + "\" /noLogo \"" + EwlStatics.CombinePaths( projectPath, schemaPathInProject ) + "\" /o:\"" + codeFileName +
					"\" /dconly /n:*," + nameSpace + " /ser:DataContractSerializer",
					"",
					true );
			}
			catch( Exception e ) {
				throw new UserCorrectableException( "Failed to generate XML schema logic using SvcUtil.", e );
			}
		else {
			Directory.CreateDirectory( projectGeneratedCodeFolderPath );
			try {
				TewlContrib.ProcessTools.RunProgram(
					EwlStatics.CombinePaths( AppStatics.DotNetToolsFolderPath, "xsd" ),
					"/nologo \"" + EwlStatics.CombinePaths( projectPath, schemaPathInProject ) + "\" /c /n:" + nameSpace + " /o:\"" + projectGeneratedCodeFolderPath +
					"\"",
					"",
					true );
			}
			catch( Exception e ) {
				throw new UserCorrectableException( "Failed to generate XML schema logic using xsd.", e );
			}
			var outputCodeFilePath = EwlStatics.CombinePaths( projectGeneratedCodeFolderPath, Path.GetFileNameWithoutExtension( schemaPathInProject ) + ".cs" );
			var desiredCodeFilePath = EwlStatics.CombinePaths( projectGeneratedCodeFolderPath, codeFileName );
			if( outputCodeFilePath != desiredCodeFilePath )
				try {
					IoMethods.MoveFile( outputCodeFilePath, desiredCodeFilePath );
				}
				catch( IOException e ) {
					throw new UserCorrectableException( "Failed to move the generated code file for an XML schema. Please try the operation again.", e );
				}
		}
	}

	private void generateEditorConfig( string folderPath, Action<TextWriter> lineWriter ) {
		using var writer = new StreamWriter( EwlStatics.CombinePaths( folderPath, ".editorconfig" ), false, new UTF8Encoding( false ) );
		writer.WriteLine( "# generated by {0}".FormatWith( EwlStatics.EwlInitialism ) );
		writer.WriteLine( "[*.cs]" );
		lineWriter( writer );
	}

	private void updateReSharperSettings( DevelopmentInstallation installation ) {
		const string defaultSettingsFileName = $"{EwlStatics.EwlInitialism} ReSharper Settings.DotSettings";
		if( !installation.SystemIsTewl() )
			File.WriteAllText(
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".sln.DotSettings" ),
				$"""
				 <wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				 	<s:String x:Key="/Default/Environment/InjectedLayers/FileInjectedLayer/=05EBF8F119D84B4B92F9F0399ECB948E/RelativePath/@EntryValue">..\Library\{(
						 installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true ? $"{InstallationConfiguration.ConfigurationFolderName} New" :
							 InstallationConfiguration.ConfigurationFolderName )}\ReSharper Settings.DotSettings</s:String>
				 	<s:Boolean x:Key="/Default/Environment/InjectedLayers/FileInjectedLayer/=05EBF8F119D84B4B92F9F0399ECB948E/@KeyIndexDefined">True</s:Boolean>
				 	<s:String x:Key="/Default/Environment/InjectedLayers/FileInjectedLayer/=87BDBF1965C117468DCA5BFA8DB0DF75/RelativePath/@EntryValue">..\Library\{generatedCodeFolderName}\{defaultSettingsFileName}</s:String>
				 	<s:Boolean x:Key="/Default/Environment/InjectedLayers/FileInjectedLayer/=87BDBF1965C117468DCA5BFA8DB0DF75/@KeyIndexDefined">True</s:Boolean>
				 	<s:Boolean x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File05EBF8F119D84B4B92F9F0399ECB948E/@KeyIndexDefined">True</s:Boolean>
				 	<s:String x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File05EBF8F119D84B4B92F9F0399ECB948E/DisplayName/@EntryValue">Custom</s:String>
				 	<s:Double x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File05EBF8F119D84B4B92F9F0399ECB948E/RelativePriority/@EntryValue">0.5</s:Double>
				 	<s:Boolean x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File87BDBF1965C117468DCA5BFA8DB0DF75/@KeyIndexDefined">True</s:Boolean>
				 	<s:String x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File87BDBF1965C117468DCA5BFA8DB0DF75/DisplayName/@EntryValue">{EwlStatics.EwlInitialism} Defaults</s:String>
				 	<s:Double x:Key="/Default/Environment/InjectedLayers/InjectedLayerCustomization/=File87BDBF1965C117468DCA5BFA8DB0DF75/RelativePriority/@EntryValue">1</s:Double></wpf:ResourceDictionary>
				 """,
				Encoding.UTF8 );

		IoMethods.CopyFile(
			EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, "ReSharper Settings.DotSettings" ),
			EwlStatics.CombinePaths(
				installation.SystemIsTewl()
					? EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Shared" )
					: EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, generatedCodeFolderName ),
				defaultSettingsFileName ) );
	}

	private void updateIgnoreFile( DevelopmentInstallation installation, bool forGit ) {
		var filePath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, forGit ? ".gitignore" : ".hgignore" );
		var lines = File.Exists( filePath ) ? File.ReadAllLines( filePath ) : Enumerable.Empty<string>();
		IoMethods.DeleteFile( filePath );
		using TextWriter writer = new StreamWriter( filePath );

		const string regionBegin = $"# {EwlStatics.EwlInitialism}-REGION";
		const string regionEnd = $"# END-{EwlStatics.EwlInitialism}-REGION";

		var dataMigratorProjectExists = File.Exists(
			EwlStatics.CombinePaths( installation.GeneralLogic.Path, IsuStatics.DataMigratorProjectName, $"{IsuStatics.DataMigratorProjectName}.csproj" ) );

		const string unitTestProject = UnitTestingInitializationOps.UnitTestProjectName;
		var unitTestProjectExists = File.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, unitTestProject, $"{unitTestProject}.csproj" ) );

		writer.WriteLine( regionBegin );
		if( !forGit )
			writer.WriteLine( "syntax: glob" );
		writer.WriteLine();
		writer.WriteLine( ".vs/" );
		writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".sln.DotSettings" );
		writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".sln.DotSettings.user" );
		writer.WriteLine( $"{InstallationFileStatics.WebFrameworkStaticFilesFolderName}/" );
		if( !dataMigratorProjectExists )
			writer.WriteLine( $"{IsuStatics.DataMigratorProjectName}/" );
		writer.WriteLine( $"{IsuStatics.DataCleanerProjectName}/" );
		if( !unitTestProjectExists )
			writer.WriteLine( $"{unitTestProject}/" );
		writer.WriteLine( "Error Log.txt" );
		writer.WriteLine( "*.csproj.user" );
		writer.WriteLine( "*" + CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension );
		writer.WriteLine();
		writer.WriteLine( "Solution Files/bin/" );
		writer.WriteLine( "Solution Files/obj/" );
		writer.WriteLine();
		writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue ? "Library/bin New/" : "Library/bin/" );
		writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl.HasValue ? "Library/obj New/" : "Library/obj/" );
		writer.WriteLine(
			$"Library/{
				( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemUsesLegacyEwl == true ? $"{InstallationConfiguration.ConfigurationFolderName} New" :
					  InstallationConfiguration.ConfigurationFolderName )}/{InstallationConfiguration.AsposeLicenseFolderName}/" );
		writer.WriteLine( "Library/Directory.Build.props" );
		writer.WriteLine( "Library/Generated Code/" );

		foreach( var app in installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications ) {
			writer.WriteLine();
			writer.WriteLine( app.Name + "/bin/" );
			writer.WriteLine( app.Name + "/obj/" );
			writer.WriteLine( app.Name + "/web.config" );
			writer.WriteLine( app.Name + "/Directory.Build.props" );
			writer.WriteLine( app.Name + "/Directory.Build.targets" );
			writer.WriteLine( app.Name + "/Generated Code/" );
			writer.WriteLine( app.Name + "/Properties/launchSettings.json" );
		}

		foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices ) {
			writer.WriteLine();
			writer.WriteLine( service.Name + "/bin/" );
			writer.WriteLine( service.Name + "/obj/" );
			writer.WriteLine( service.Name + "/Directory.Build.props" );
			writer.WriteLine( service.Name + "/Generated Code/" );
		}

		foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable ) {
			writer.WriteLine();
			writer.WriteLine( project.Name + "/bin/" );
			writer.WriteLine( project.Name + "/obj/" );
			writer.WriteLine( project.Name + "/Directory.Build.props" );
			writer.WriteLine( project.Name + "/Generated Code/" );
		}

		if( dataMigratorProjectExists ) {
			writer.WriteLine();
			writer.WriteLine( IsuStatics.DataMigratorProjectName + "/bin/" );
			writer.WriteLine( IsuStatics.DataMigratorProjectName + "/obj/" );
			writer.WriteLine( IsuStatics.DataMigratorProjectName + "/Directory.Build.props" );
			writer.WriteLine( IsuStatics.DataMigratorProjectName + "/Generated Code/" );
		}

		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
			writer.WriteLine();
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/bin/" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/obj/" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/Directory.Build.props" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/Generated Code/" );
		}

		if( unitTestProjectExists ) {
			writer.WriteLine();
			writer.WriteLine( unitTestProject + "/bin/" );
			writer.WriteLine( unitTestProject + "/obj/" );
			writer.WriteLine( unitTestProject + "/Directory.Build.props" );
			writer.WriteLine( unitTestProject + "/Generated Code/" );
		}

		writer.WriteLine();
		writer.WriteLine( regionEnd );

		var skipping = false;
		foreach( var line in lines ) {
			if( line == regionBegin )
				skipping = true;
			if( !skipping )
				writer.WriteLine( line );
			if( line == regionEnd )
				skipping = false;
		}
	}
}