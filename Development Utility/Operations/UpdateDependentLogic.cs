using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Tewl.IO;
using static MoreLinq.Extensions.AtLeastExtension;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class UpdateDependentLogic: Operation {
	private const string generatedCodeFolderName = "Generated Code";
	private static readonly string serverSideConsoleAppJsonArgument = "{0}UseJsonArguments".FormatWith( EwlStatics.EwlInitialism.ToLowerInvariant() );

	private static readonly Operation instance = new UpdateDependentLogic();
	public static Operation Instance => instance;
	private UpdateDependentLogic() {}

	bool Operation.IsValid( Installation installation ) => installation is DevelopmentInstallation;

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		// This block exists because of https://enduracode.kilnhg.com/Review/K164316.
		try {
			IsuStatics.ConfigureIis( false );
			StatusStatics.SetStatus( "Configured IIS." );
		}
		catch {
			StatusStatics.SetStatus( "Did not configure IIS." );
		}

		var installation = (DevelopmentInstallation)genericInstallation;

		DatabaseOps.UpdateDatabaseLogicIfUpdateFileExists(
			installation.ExistingInstallationLogic.Database,
			installation.ExistingInstallationLogic.DatabaseUpdateFilePath,
			true );

		if( !installation.SystemIsTewl() )
			try {
				copyInEwlFiles( installation );
			}
			catch( Exception e ) {
				var message = "Failed to copy {0} files into the installation. Please try the operation again.".FormatWith( EwlStatics.EwlName );
				if( e is UnauthorizedAccessException || e is IOException )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
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
					writer.WriteLine( "using Newtonsoft.Json;" );
					writer.WriteLine( "using Newtonsoft.Json.Linq;" );
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
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.ProviderProjectFolderName, EwlStatics.MySqlProviderProjectName ),
				"EnterpriseWebLibrary.MySql",
				_ => {} );
			generateCodeForProject(
				installation,
				"Oracle Database Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.ProviderProjectFolderName, EwlStatics.OracleDatabaseProviderProjectName ),
				"EnterpriseWebLibrary.OracleDatabase",
				_ => {} );
			generateCodeForProject(
				installation,
				"OpenID Connect Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.ProviderProjectFolderName, EwlStatics.OpenIdConnectProviderProjectName ),
				"EnterpriseWebLibrary.OpenIdConnect",
				_ => {} );
			generateCodeForProject(
				installation,
				"SAML Provider",
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.ProviderProjectFolderName, EwlStatics.SamlProviderProjectName ),
				"EnterpriseWebLibrary.Saml",
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
		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null )
			generateCodeForProject(
				installation,
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name,
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name ),
				installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.NamespaceAndAssemblyName,
				_ => {},
				runtimeIdentifier: "win-x64",
				selfContained: true );

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

		if( !installation.DevelopmentInstallationLogic.SystemIsEwl && !installation.SystemIsTewl() ) {
			if( Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.MercurialRepositoryFolderName ) ) )
				updateIgnoreFile( installation, false );
			if( File.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, ".gitignore" ) ) )
				updateIgnoreFile( installation, true );
		}
	}

	private void copyInEwlFiles( DevelopmentInstallation installation ) {
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
					writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
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
							.Append( InstallationFileStatics.FilesFolderName )
							.Append( generatedCodeFolderName ),
						null,
						null,
						out var resourceSerializationWriter );
					writer.WriteLine();
					writer.WriteLine(
						"namespace {0}.Configuration.Providers {{".FormatWith(
							installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName ) );
					writer.WriteLine( "internal class ResourceSerialization: SystemResourceSerializationProvider {" );
					resourceSerializationWriter( "SystemResourceSerializationProvider" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}
			} );
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
				writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
				writer.WriteLine( "using Newtonsoft.Json;" );
				writer.WriteLine( "using Newtonsoft.Json.Linq;" );
				writer.WriteLine( "using NodaTime;" );
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
					.Replace( "@@InitializationTimeoutSeconds", EwfOps.InitializationTimeoutSeconds.ToString() ),
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

	private void generateServerSideConsoleProjectCode( DevelopmentInstallation installation, ServerSideConsoleProject project ) {
		generateCodeForProject(
			installation,
			project.Name,
			EwlStatics.CombinePaths( installation.GeneralLogic.Path, project.Name ),
			project.NamespaceAndAssemblyName,
			writer => {
				writer.WriteLine( "using System.Collections.Immutable;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine();
				writer.WriteLine( "namespace {0};".FormatWith( project.NamespaceAndAssemblyName ) );
				writer.WriteLine();
				writer.WriteLine( "internal static partial class Program {" );

				writer.WriteLine( "private static int Main( string[] args ) {" );
				writer.WriteLine( "SystemInitializer? globalInitializer = null;" );
				writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
				writer.WriteLine( "var dataAccessState = new System.Lazy<DataAccessState>( () => new DataAccessState() );" );
				writer.WriteLine(
					"GlobalInitializationOps.InitStatics( globalInitializer!, \"{0}\", false, mainDataAccessStateGetter: () => dataAccessState.Value! );".FormatWith(
						project.Name ) );
				writer.WriteLine( "try {" );
				writer.WriteLine( "return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( () => {" );

				// See https://stackoverflow.com/a/44135529/35349.
				writer.WriteLine( "Console.SetIn( new StreamReader( Console.OpenStandardInput(), Console.InputEncoding, false, 4096 ) );" );

				writer.WriteLine(
					"ewlMain( args.Any() && string.Equals( args[ 0 ], \"{0}\", StringComparison.Ordinal ) ? Newtonsoft.Json.JsonConvert.DeserializeObject<ImmutableArray<string>>( Console.ReadLine()! ) : args );"
						.FormatWith( serverSideConsoleAppJsonArgument ) );
				writer.WriteLine( "} );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer? globalInitializer );" );
				writer.WriteLine( "static partial void ewlMain( IReadOnlyList<string> arguments );" );

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

			var projectFilePath = EwlStatics.CombinePaths( projectPath, "{0}.csproj".FormatWith( Path.GetFileName( projectPath ) ) );
			var projectFile = "";
			if( File.Exists( projectFilePath ) )
				projectFile = File.ReadAllText( projectFilePath );
			else
				StatusStatics.SetStatus( "Warning: Failed to locate the project file for {0}.".FormatWith( projectName ) );

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
			writeMsBuildProperty( "<Nullable>enable</Nullable>" );
			writeMsBuildProperty( "<CopyDebugSymbolFilesFromPackages>true</CopyDebugSymbolFilesFromPackages>" );

			writeMsBuildProperty(
				"<DefaultItemExcludesInProjectFolder>$(DefaultItemExcludesInProjectFolder);Directory.Build.props;**/*.ewlt.cs</DefaultItemExcludesInProjectFolder>" );

			// affects only web apps; see https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config?view=aspnetcore-6.0
			writeMsBuildProperty( "<IsTransformWebConfigDisabled>true</IsTransformWebConfigDisabled>" );

			writer.WriteLine( "</PropertyGroup>" );
			writer.WriteLine( "<ItemGroup>" );

			writer.WriteLine( "<Using Include=\"System\" />" );
			writer.WriteLine( "<Using Include=\"System.Collections.Generic\" />" );
			writer.WriteLine( "<Using Include=\"System.IO\" />" );
			writer.WriteLine( "<Using Include=\"System.Linq\" />" );

			if( !installation.SystemIsTewl() ) {
				writer.WriteLine( "<Using Include=\"EnterpriseWebLibrary\" />" );
				if( includeWebFrameworkUsingDirectives )
					writer.WriteLine( "<Using Include=\"EnterpriseWebLibrary.EnterpriseWebFramework\" />" );
			}
			writer.WriteLine( "<Using Include=\"Tewl\" />" );
			writer.WriteLine( "<Using Include=\"Tewl.Tools\" />" );

			writer.WriteLine( "<Using Include=\"Humanizer.StringExtensions\"><Static>True</Static></Using>" );

			writer.WriteLine( "</ItemGroup>" );
			writer.WriteLine( "</Project>" );
		}

		var generatedCodeFolderPath = EwlStatics.CombinePaths( projectPath, generatedCodeFolderName );
		Directory.CreateDirectory( generatedCodeFolderPath );
		var isuFilePath = EwlStatics.CombinePaths( generatedCodeFolderPath, "ISU.cs" );
		IoMethods.DeleteFile( isuFilePath );
		using( TextWriter writer = new StreamWriter( isuFilePath ) )
			codeWriter( writer );
	}

	private void generateXmlSchemaLogicForInstallationConfigurationFile( DevelopmentInstallation installation, string schemaFileName ) {
		var schemaPathInProject = EwlStatics.CombinePaths( @"Configuration\Installation", schemaFileName + FileExtensions.Xsd );
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

	private void updateIgnoreFile( DevelopmentInstallation installation, bool forGit ) {
		var filePath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, forGit ? ".gitignore" : ".hgignore" );
		var lines = File.Exists( filePath ) ? File.ReadAllLines( filePath ) : Enumerable.Empty<string>();
		IoMethods.DeleteFile( filePath );
		using TextWriter writer = new StreamWriter( filePath );

		const string regionBegin = "# EWL-REGION";
		const string regionEnd = "# END-EWL-REGION";

		writer.WriteLine( regionBegin );
		if( !forGit )
			writer.WriteLine( "syntax: glob" );
		writer.WriteLine();
		writer.WriteLine( ".vs/" );
		writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".sln.DotSettings.user" );
		writer.WriteLine( "{0}/".FormatWith( InstallationFileStatics.WebFrameworkStaticFilesFolderName ) );
		writer.WriteLine( "Error Log.txt" );
		writer.WriteLine( "*.csproj.user" );
		writer.WriteLine( "*" + CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension );
		writer.WriteLine();
		writer.WriteLine( "Solution Files/bin/" );
		writer.WriteLine( "Solution Files/obj/" );
		writer.WriteLine();
		writer.WriteLine( "Library/bin/" );
		writer.WriteLine( "Library/obj/" );
		writer.WriteLine( $"Library/{InstallationConfiguration.ConfigurationFolderName}/{InstallationConfiguration.AsposeLicenseFolderName}/" );
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

		if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
			writer.WriteLine();
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/bin/" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/obj/" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/Directory.Build.props" );
			writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.Name + "/Generated Code/" );
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