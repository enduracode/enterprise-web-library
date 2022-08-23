using System.Collections.Immutable;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Humanizer;
using Tewl;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class UpdateDependentLogic: Operation {
		private const string generatedCodeFolderName = "Generated Code";

		private static readonly Operation instance = new UpdateDependentLogic();
		public static Operation Instance => instance;
		private UpdateDependentLogic() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
			IsuStatics.ConfigureIis( true, false );
			Console.WriteLine( "Configured IIS Express." );

			// This block exists because of https://enduracode.kilnhg.com/Review/K164316.
			try {
				IsuStatics.ConfigureIis( false, false );
				Console.WriteLine( "Configured full IIS." );
			}
			catch {
				Console.WriteLine( "Did not configure full IIS." );
			}

			var installation = genericInstallation as DevelopmentInstallation;

			DatabaseOps.UpdateDatabaseLogicIfUpdateFileExists(
				installation.ExistingInstallationLogic.Database,
				installation.ExistingInstallationLogic.DatabaseUpdateFilePath,
				true );

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
					writer => {
						writer.WriteLine( "using System;" );
						writer.WriteLine( "using System.Collections.Generic;" );
						writer.WriteLine( "using System.Globalization;" );
						writer.WriteLine( "using System.Linq;" );
						writer.WriteLine( "using System.Threading;" );
						writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
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
							"" );
					} );
				generateCodeForProject(
					installation,
					"Development Utility",
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, "Development Utility" ),
					_ => {} );
				generateCodeForProject(
					installation,
					"SAML Provider",
					EwlStatics.CombinePaths( installation.GeneralLogic.Path, EwlStatics.SamlProviderProjectPath ),
					_ => {} );
			}
			generateLibraryCode( installation );
			foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ] )
				generateWebConfigAndCodeForWebProject( installation, webProject );
			foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices )
				generateWindowsServiceCode( installation, service );
			foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable )
				generateServerSideConsoleProjectCode( installation, project );
			if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null )
				generateCodeForProject(
					installation,
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name,
					EwlStatics.CombinePaths(
						installation.GeneralLogic.Path,
						installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name ),
					_ => {} );

			generateXmlSchemaLogicForInstallationConfigurationFile( installation, "Custom" );
			generateXmlSchemaLogicForInstallationConfigurationFile( installation, "Shared" );
			generateXmlSchemaLogicForOtherFiles( installation );

			if( !installation.DevelopmentInstallationLogic.SystemIsEwl &&
			    Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.MercurialRepositoryFolderName ) ) )
				updateMercurialIgnoreFile( installation );
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
			else {
				// If web projects exist for this installation, copy in web-framework static files.
				if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects != null ) {
					var webFrameworkStaticFilesFolderPath = EwlStatics.CombinePaths(
						installation.GeneralLogic.Path,
						InstallationFileStatics.WebFrameworkStaticFilesFolderName );
					IoMethods.DeleteFolder( webFrameworkStaticFilesFolderPath );
					IoMethods.CopyFolder(
						EwlStatics.CombinePaths( ConfigurationStatics.InstallationPath, InstallationFileStatics.WebFrameworkStaticFilesFolderName ),
						webFrameworkStaticFilesFolderPath,
						false );
				}
			}
		}

		private void generateLibraryCode( DevelopmentInstallation installation ) {
			generateCodeForProject(
				installation,
				"Library",
				installation.DevelopmentInstallationLogic.LibraryPath,
				writer => {
					// Don't add "using System" here. It will create a huge number of ReSharper warnings in the generated code file.
					writer.WriteLine( "using System.Collections.Generic;" );
					writer.WriteLine( "using System.Data;" ); // Necessary for stored procedure logic
					writer.WriteLine( "using System.Data.Common;" );
					writer.WriteLine( "using System.Diagnostics;" ); // Necessary for ServerSideConsoleAppStatics
					writer.WriteLine( "using System.Linq;" );
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
					writer.WriteLine( "using NodaTime;" );
					writer.WriteLine( "using Tewl.InputValidation;" );
					writer.WriteLine( "using Tewl.Tools;" );

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
						installation.GeneralLogic.Path,
						installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName,
						writer );
					writer.WriteLine();
					generateServerSideConsoleAppStatics( writer, installation );
					generateDataAccessCode( writer, installation );

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
						null );
				} );
		}

		private void generateServerSideConsoleAppStatics( TextWriter writer, DevelopmentInstallation installation ) {
			writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
			writer.WriteLine( "public static class ServerSideConsoleAppStatics {" );
			foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable ) {
				writer.WriteLine(
					"public static void Start" + project.Name.EnglishToPascal() +
					"( IEnumerable<string> arguments, string input, string errorMessageIfAlreadyRunning = \"\" ) {" );
				writer.WriteLine( "if( errorMessageIfAlreadyRunning.Any() && Process.GetProcessesByName( \"" + project.NamespaceAndAssemblyName + "\" ).Any() )" );
				writer.WriteLine( "throw new DataModificationException( errorMessageIfAlreadyRunning );" );

				var programPath = "EwlStatics.CombinePaths( ConfigurationStatics.InstallationPath, \"" + project.Name +
				                  "\", ConfigurationStatics.ServerSideConsoleAppRelativeFolderPath, \"" + project.NamespaceAndAssemblyName + "\" )";
				var runProgramExpression = "EnterpriseWebLibrary.TewlContrib.ProcessTools.RunProgram( " + programPath +
				                           ", \"\", Newtonsoft.Json.JsonConvert.SerializeObject( arguments, Newtonsoft.Json.Formatting.None ) + System.Environment.NewLine + input, false )";

				writer.WriteLine( "if( EwfApp.Instance != null && AppRequestState.Instance != null )" );
				writer.WriteLine( "AppRequestState.AddNonTransactionalModificationMethod( () => " + runProgramExpression + " );" );
				writer.WriteLine( "else" );
				writer.WriteLine( runProgramExpression + ";" );

				writer.WriteLine( "}" );
			}
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void generateDataAccessCode( TextWriter writer, DevelopmentInstallation installation ) {
			var baseNamespace = installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".DataAccess";
			foreach( var database in installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration )
				try {
					generateDataAccessCodeForDatabase(
						database,
						installation.DevelopmentInstallationLogic.LibraryPath,
						writer,
						baseNamespace,
						database.SecondaryDatabaseName.Length == 0
							? installation.DevelopmentInstallationLogic.DevelopmentConfiguration.database
							: installation.DevelopmentInstallationLogic.DevelopmentConfiguration.secondaryDatabases.Single(
								sd => sd.name == database.SecondaryDatabaseName ) );
				}
				catch( Exception e ) {
					throw UserCorrectableException.CreateSecondaryException(
						"An exception occurred while generating data access logic for the {0}.".FormatWith( DatabaseOps.GetDatabaseNounPhrase( database ) ),
						e );
				}
			if( installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Any( d => d.SecondaryDatabaseName.Length > 0 ) ) {
				writer.WriteLine();
				writer.WriteLine( "namespace " + baseNamespace + " {" );
				writer.WriteLine( "public class SecondaryDatabaseNames {" );
				foreach( var secondaryDatabase in
				        installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Where( d => d.SecondaryDatabaseName.Length > 0 ) )
					writer.WriteLine( "public const string " + secondaryDatabase.SecondaryDatabaseName + " = \"" + secondaryDatabase.SecondaryDatabaseName + "\";" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}
		}

		private void generateDataAccessCodeForDatabase(
			InstallationSupportUtility.DatabaseAbstraction.Database database, string libraryBasePath, TextWriter writer, string baseNamespace,
			EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
			var tableNames = DatabaseOps.GetDatabaseTables( database ).ToImmutableArray();

			ensureTablesExist( tableNames, configuration.SmallTables, "small" );
			ensureTablesExist( tableNames, configuration.TablesUsingRowVersionedDataCaching, "row-versioned data caching" );
			ensureTablesExist( tableNames, configuration.revisionHistoryTables, "revision history" );

			ensureTablesExist( tableNames, configuration.WhitelistedTables, "whitelisted" );
			tableNames = tableNames
				.Where( table => configuration.WhitelistedTables == null || configuration.WhitelistedTables.Any( i => i.EqualsIgnoreCase( table ) ) )
				.ToImmutableArray();

			database.ExecuteDbMethod(
				delegate( DBConnection cn ) {
					// database logic access - standard
					writer.WriteLine();
					TableConstantStatics.Generate( cn, writer, baseNamespace, database, tableNames );

					// database logic access - custom
					writer.WriteLine();
					RowConstantStatics.Generate( cn, writer, baseNamespace, database, configuration );

					// retrieval and modification commands - standard
					writer.WriteLine();
					CommandConditionStatics.Generate( cn, writer, baseNamespace, database, tableNames );

					writer.WriteLine();
					var tableRetrievalNamespaceDeclaration = TableRetrievalStatics.GetNamespaceDeclaration( baseNamespace, database );
					TableRetrievalStatics.Generate( cn, writer, tableRetrievalNamespaceDeclaration, database, tableNames, configuration );

					writer.WriteLine();
					var modNamespaceDeclaration = StandardModificationStatics.GetNamespaceDeclaration( baseNamespace, database );
					StandardModificationStatics.Generate( cn, writer, modNamespaceDeclaration, database, tableNames, configuration );

					foreach( var tableName in tableNames ) {
						TableRetrievalStatics.WritePartialClass( cn, libraryBasePath, tableRetrievalNamespaceDeclaration, database, tableName );
						StandardModificationStatics.WritePartialClass(
							cn,
							libraryBasePath,
							modNamespaceDeclaration,
							database,
							tableName,
							CodeGeneration.DataAccess.DataAccessStatics.IsRevisionHistoryTable( tableName, configuration ) );
					}

					// retrieval and modification commands - custom
					writer.WriteLine();
					QueryRetrievalStatics.Generate( cn, writer, baseNamespace, database, configuration );
					writer.WriteLine();
					CustomModificationStatics.Generate( cn, writer, baseNamespace, database, configuration );

					// other commands
					if( cn.DatabaseInfo is SqlServerInfo ) {
						writer.WriteLine();
						writer.WriteLine( "namespace {0} {{".FormatWith( baseNamespace ) );
						writer.WriteLine( "public static class {0}MainSequence {{".FormatWith( database.SecondaryDatabaseName ) );
						writer.WriteLine(
							"public static int GetNextValue() => {0}Modification.MainSequenceModification.InsertRow();".FormatWith( database.SecondaryDatabaseName ) );
						writer.WriteLine( "}" );
						writer.WriteLine( "}" );
					}
					else if( cn.DatabaseInfo is OracleInfo ) {
						writer.WriteLine();
						SequenceStatics.Generate( cn, writer, baseNamespace, database );
						writer.WriteLine();
						ProcedureStatics.Generate( cn, writer, baseNamespace, database );
					}
				} );
		}

		private void ensureTablesExist( IReadOnlyCollection<string> databaseTables, IEnumerable<string> specifiedTables, string tableAdjective ) {
			if( specifiedTables == null )
				return;
			var nonexistentTables = specifiedTables.Where( specifiedTable => databaseTables.All( i => !i.EqualsIgnoreCase( specifiedTable ) ) ).ToArray();
			if( nonexistentTables.Any() )
				throw new UserCorrectableException(
					tableAdjective.CapitalizeString() + " " + ( nonexistentTables.Length > 1 ? "tables" : "table" ) + " " +
					StringTools.GetEnglishListPhrase( nonexistentTables.Select( i => "'" + i + "'" ), true ) + " " + ( nonexistentTables.Length > 1 ? "do" : "does" ) +
					" not exist." );
		}

		private void generateWebConfigAndCodeForWebProject( DevelopmentInstallation installation, WebProject project ) {
			var application = installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Single( i => i.Name == project.name );

			// This must be done before web meta logic generation, which can be affected by the contents of Web.config files.
			WebConfigStatics.GenerateWebConfig( application, project );

			Directory.CreateDirectory( EwlStatics.CombinePaths( application.Path, StaticFile.AppStaticFilesFolderName ) );

			generateCodeForProject(
				installation,
				project.name,
				application.Path,
				writer => {
					writer.WriteLine( "using System;" );
					writer.WriteLine( "using System.Collections.Generic;" );
					writer.WriteLine( "using System.Collections.ObjectModel;" );
					writer.WriteLine( "using System.Globalization;" );
					writer.WriteLine( "using System.Linq;" );
					writer.WriteLine( "using System.Threading;" );
					writer.WriteLine( "using EnterpriseWebLibrary;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
					writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
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
						"RequestDispatchingStatics.AppProvider.GetFrameworkUrlParent()" );
				} );
		}

		private void generateWindowsServiceCode( DevelopmentInstallation installation, WindowsService service ) {
			generateCodeForProject(
				installation,
				service.Name,
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, service.Name ),
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
					writer.WriteLine( "SystemInitializer globalInitializer = null;" );
					writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
					writer.WriteLine( "var dataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );" );
					writer.WriteLine(
						"GlobalInitializationOps.InitStatics( globalInitializer, \"{0}\", false, mainDataAccessStateGetter: () => dataAccessState.Value, useLongDatabaseTimeouts: true );"
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

					writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer globalInitializer );" );

					writer.WriteLine( "}" );

					writer.WriteLine( "internal partial class " + service.Name.EnglishToPascal() + ": WindowsServiceBase {" );
					writer.WriteLine( "internal " + service.Name.EnglishToPascal() + "() {}" );
					writer.WriteLine( "string WindowsServiceBase.Name { get { return \"" + service.Name + "\"; } }" );
					writer.WriteLine( "}" );

					writer.WriteLine( "}" );
				} );
		}

		private void generateServerSideConsoleProjectCode( DevelopmentInstallation installation, ServerSideConsoleProject project ) {
			generateCodeForProject(
				installation,
				project.Name,
				EwlStatics.CombinePaths( installation.GeneralLogic.Path, project.Name ),
				writer => {
					writer.WriteLine( "using System;" );
					writer.WriteLine( "using System.Collections.Generic;" );
					writer.WriteLine( "using System.Collections.Immutable;" );
					writer.WriteLine( "using System.IO;" );
					writer.WriteLine( "using System.Threading;" );
					writer.WriteLine( "using EnterpriseWebLibrary;" );
					writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
					writer.WriteLine();
					writer.WriteLine( "namespace " + project.NamespaceAndAssemblyName + " {" );
					writer.WriteLine( "internal static partial class Program {" );

					writer.WriteLine( "[ MTAThread ]" );
					writer.WriteLine( "private static int Main( string[] args ) {" );
					writer.WriteLine( "SystemInitializer globalInitializer = null;" );
					writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
					writer.WriteLine( "var dataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );" );
					writer.WriteLine(
						"GlobalInitializationOps.InitStatics( globalInitializer, \"" + project.Name +
						"\", false, mainDataAccessStateGetter: () => dataAccessState.Value );" );
					writer.WriteLine( "try {" );
					writer.WriteLine( "return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( () => {" );

					// See https://stackoverflow.com/a/44135529/35349.
					writer.WriteLine( "Console.SetIn( new StreamReader( Console.OpenStandardInput(), Console.InputEncoding, false, 4096 ) );" );

					writer.WriteLine( "ewlMain( Newtonsoft.Json.JsonConvert.DeserializeObject<ImmutableArray<string>>( Console.ReadLine() ) );" );
					writer.WriteLine( "} );" );
					writer.WriteLine( "}" );
					writer.WriteLine( "finally {" );
					writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );

					writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer globalInitializer );" );
					writer.WriteLine( "static partial void ewlMain( IReadOnlyList<string> arguments );" );

					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				} );
		}

		private void generateCodeForProject( DevelopmentInstallation installation, string projectName, string projectPath, Action<TextWriter> codeWriter ) {
			using( var writer = new StreamWriter( EwlStatics.CombinePaths( projectPath, "Directory.Build.props" ), false, Encoding.UTF8 ) ) {
				writer.WriteLine( "<Project>" );
				writer.WriteLine( "<PropertyGroup>" );

				writer.WriteLine( "<Version>{0}</Version>".FormatWith( "{0}.0.{1}.0".FormatWith( installation.CurrentMajorVersion, installation.NextBuildNumber ) ) );

				// assembly attributes; see https://docs.microsoft.com/en-us/dotnet/standard/assembly/set-attributes
				writer.WriteLine( "<GenerateAssemblyCompanyAttribute>false</GenerateAssemblyCompanyAttribute>" );
				writer.WriteLine( "<Product>{0}</Product>".FormatWith( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName ) );
				writer.WriteLine(
					"<AssemblyTitle>{0}</AssemblyTitle>".FormatWith(
						installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + projectName.PrependDelimiter( " - " ) ) );

				// package properties; see https://docs.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-target
				writer.WriteLine( "<PackageVersion>0</PackageVersion>" ); // Clear since we create both prerelease and stable packages with different version numbers.

				writer.WriteLine( "</PropertyGroup>" );
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
						"/d:\"" + projectGeneratedCodeFolderPath + "\" /noLogo \"" + EwlStatics.CombinePaths( projectPath, schemaPathInProject ) + "\" /o:\"" +
						codeFileName + "\" /dconly /n:*," + nameSpace + " /ser:DataContractSerializer",
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

		private void updateMercurialIgnoreFile( DevelopmentInstallation installation ) {
			var filePath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, ".hgignore" );
			var lines = File.Exists( filePath ) ? File.ReadAllLines( filePath ) : new string[ 0 ];
			IoMethods.DeleteFile( filePath );
			using( TextWriter writer = new StreamWriter( filePath ) ) {
				const string regionBegin = "# EWL-REGION";
				const string regionEnd = "# END-EWL-REGION";

				writer.WriteLine( regionBegin );
				writer.WriteLine( "syntax: glob" );
				writer.WriteLine();
				writer.WriteLine( ".vs/{0}/v16/.suo".FormatWith( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName ) );
				writer.WriteLine( ".vs/{0}/v16/Server/sqlite3/".FormatWith( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName ) );
				writer.WriteLine( "packages/" );
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
				writer.WriteLine( "Library/Generated Code/" );

				foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ] ) {
					writer.WriteLine();
					writer.WriteLine( webProject.name + "/bin/" );
					writer.WriteLine( webProject.name + "/obj/" );
					writer.WriteLine( webProject.name + "/Generated Code/" );
				}

				foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices ) {
					writer.WriteLine();
					writer.WriteLine( service.Name + "/bin/" );
					writer.WriteLine( service.Name + "/obj/" );
					writer.WriteLine( service.Name + "/Generated Code/" );
				}

				foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable ) {
					writer.WriteLine();
					writer.WriteLine( project.Name + "/bin/" );
					writer.WriteLine( project.Name + "/obj/" );
					writer.WriteLine( project.Name + "/Generated Code/" );
				}

				if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
					writer.WriteLine();
					writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name + "/bin/" );
					writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name + "/obj/" );
					writer.WriteLine( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name + "/Generated Code/" );
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
	}
}