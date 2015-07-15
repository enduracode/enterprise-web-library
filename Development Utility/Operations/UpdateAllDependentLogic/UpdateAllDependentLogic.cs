using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems.StandardModification;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebConfig;
using Humanizer;
using EnterpriseWebLibrary;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	// NOTE: Rename this, and the containing folder, to UpdateDependentLogic. Also rename the batch file in Solution Files and the batch file in each person's EWL
	// Configuration repository. Remember to fix the CruiseControl.NET config generator and the place in ExportLogic where we reference the batch file name when
	// packaging general files.
	internal class UpdateAllDependentLogic: Operation {
		private const string asposeLicenseFileName = "Aspose.Total.lic";

		private static readonly Operation instance = new UpdateAllDependentLogic();
		public static Operation Instance { get { return instance; } }
		private UpdateAllDependentLogic() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			IsuStatics.ConfigureIis( true );
			Console.WriteLine( "Configured IIS Express." );

			// This block exists because of https://enduracode.kilnhg.com/Review/K164316.
			try {
				IsuStatics.ConfigureIis( false );
				Console.WriteLine( "Configured full IIS." );
			}
			catch {
				Console.WriteLine( "Did not configure full IIS." );
			}

			var installation = genericInstallation as DevelopmentInstallation;

			DatabaseOps.UpdateDatabaseLogicIfUpdateFileExists(
				installation.DevelopmentInstallationLogic.Database,
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
					AppStatics.CoreProjectName,
					writer => {
						writer.WriteLine( "using System;" );
						writer.WriteLine( "using System.Globalization;" );
						writer.WriteLine( "using System.Reflection;" );
						writer.WriteLine( "using System.Runtime.InteropServices;" );
						writer.WriteLine();
						writeAssemblyInfo( writer, installation, "" );
						writer.WriteLine();
						writer.WriteLine( "namespace EnterpriseWebLibrary {" );
						writer.WriteLine( "partial class EwlStatics {" );
						CodeGenerationStatics.AddSummaryDocComment( writer, "The date/time at which this version of EWL was built." );
						writer.WriteLine(
							"public static readonly DateTimeOffset EwlBuildDateTime = {0};".FormatWith( AppStatics.GetLiteralDateTimeExpression( DateTimeOffset.UtcNow ) ) );
						writer.WriteLine( "}" );
						writer.WriteLine( "}" );
					} );
				generateCodeForProject(
					installation,
					"Development Utility",
					writer => {
						writer.WriteLine( "using System.Reflection;" );
						writer.WriteLine( "using System.Runtime.InteropServices;" );
						writeAssemblyInfo( writer, installation, "Development Utility" );
					} );
			}
			generateLibraryCode( installation );
			foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ] )
				generateWebConfigAndCodeForWebProject( installation, webProject );
			foreach( var service in installation.ExistingInstallationLogic.RuntimeConfiguration.WindowsServices )
				generateWindowsServiceCode( installation, service );
			foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable )
				generateServerSideConsoleProjectCode( installation, project );
			if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject != null ) {
				generateCodeForProject(
					installation,
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name,
					writer => {
						writer.WriteLine( "using System.Reflection;" );
						writer.WriteLine( "using System.Runtime.InteropServices;" );
						writeAssemblyInfo( writer, installation, installation.DevelopmentInstallationLogic.DevelopmentConfiguration.clientSideAppProject.name );
					} );
			}

			generateXmlSchemaLogicForCustomInstallationConfigurationXsd( installation );
			generateXmlSchemaLogicForOtherXsdFiles( installation );

			if( !installation.DevelopmentInstallationLogic.SystemIsEwl && Directory.Exists( EwlStatics.CombinePaths( installation.GeneralLogic.Path, ".hg" ) ) )
				updateMercurialIgnoreFile( installation );
		}

		private void copyInEwlFiles( DevelopmentInstallation installation ) {
			if( installation.DevelopmentInstallationLogic.SystemIsEwl ) {
				foreach( var fileName in GlobalStatics.ConfigurationXsdFileNames ) {
					IoMethods.CopyFile(
						EwlStatics.CombinePaths( installation.GeneralLogic.Path, AppStatics.CoreProjectName, "Configuration", fileName + FileExtensions.Xsd ),
						EwlStatics.CombinePaths(
							InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
							InstallationFileStatics.FilesFolderName,
							fileName + FileExtensions.Xsd ) );
				}
			}
			else {
				var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
				if( recognizedInstallation == null || !recognizedInstallation.SystemIsEwlCacheCoordinator ) {
					var asposeLicenseFilePath = EwlStatics.CombinePaths( ConfigurationStatics.ConfigurationFolderPath, asposeLicenseFileName );
					if( File.Exists( asposeLicenseFilePath ) ) {
						IoMethods.CopyFile(
							asposeLicenseFilePath,
							EwlStatics.CombinePaths(
								InstallationFileStatics.GetGeneralFilesFolderPath( installation.GeneralLogic.Path, true ),
								InstallationFileStatics.FilesFolderName,
								asposeLicenseFileName ) );
					}
				}

				// If web projects exist for this installation, copy appropriate files into them.
				if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects != null ) {
					foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects )
						copyInWebProjectFiles( installation, webProject );
				}
			}
		}

		private void copyInWebProjectFiles( Installation installation, WebProject webProject ) {
			var webProjectFilesFolderPath = EwlStatics.CombinePaths( ConfigurationStatics.InstallationPath, AppStatics.WebProjectFilesFolderName );
			var webProjectPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, webProject.name );

			// Copy Ewf folder and customize namespaces in .aspx, .ascx, .master, and .cs files.
			var webProjectEwfFolderPath = EwlStatics.CombinePaths( webProjectPath, StaticFileHandler.EwfFolderName );
			IoMethods.DeleteFolder( webProjectEwfFolderPath );
			IoMethods.CopyFolder( EwlStatics.CombinePaths( webProjectFilesFolderPath, StaticFileHandler.EwfFolderName ), webProjectEwfFolderPath, false );
			IoMethods.RecursivelyRemoveReadOnlyAttributeFromItem( webProjectEwfFolderPath );
			var matchingFiles = new List<string>();
			matchingFiles.AddRange( Directory.GetFiles( webProjectEwfFolderPath, "*.aspx", SearchOption.AllDirectories ) );
			matchingFiles.AddRange( Directory.GetFiles( webProjectEwfFolderPath, "*.ascx", SearchOption.AllDirectories ) );
			matchingFiles.AddRange( Directory.GetFiles( webProjectEwfFolderPath, "*.master", SearchOption.AllDirectories ) );
			matchingFiles.AddRange( Directory.GetFiles( webProjectEwfFolderPath, "*.cs", SearchOption.AllDirectories ) );
			foreach( var filePath in matchingFiles )
				File.WriteAllText( filePath, customizeNamespace( File.ReadAllText( filePath ), webProject ) );

			IoMethods.CopyFile(
				EwlStatics.CombinePaths( webProjectFilesFolderPath, AppStatics.StandardLibraryFilesFileName ),
				EwlStatics.CombinePaths( webProjectPath, AppStatics.StandardLibraryFilesFileName ) );
			IoMethods.RecursivelyRemoveReadOnlyAttributeFromItem( EwlStatics.CombinePaths( webProjectPath, AppStatics.StandardLibraryFilesFileName ) );
		}

		private string customizeNamespace( string text, WebProject webProject ) {
			return text.Replace( "EnterpriseWebLibrary.WebSite", webProject.NamespaceAndAssemblyName );
		}

		private void generateLibraryCode( DevelopmentInstallation installation ) {
			var libraryGeneratedCodeFolderPath = EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, "Generated Code" );
			Directory.CreateDirectory( libraryGeneratedCodeFolderPath );
			var isuFilePath = EwlStatics.CombinePaths( libraryGeneratedCodeFolderPath, "ISU.cs" );
			IoMethods.DeleteFile( isuFilePath );
			using( TextWriter writer = new StreamWriter( isuFilePath ) ) {
				// Don't add "using System" here. It will create a huge number of ReSharper warnings in the generated code file.
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Data;" ); // Necessary for stored procedure logic
				writer.WriteLine( "using System.Data.Common;" );
				writer.WriteLine( "using System.Diagnostics;" ); // Necessary for ServerSideConsoleAppStatics
				writer.WriteLine( "using System.Linq;" );
				writer.WriteLine( "using System.Reflection;" );
				writer.WriteLine( "using System.Runtime.InteropServices;" );
				writer.WriteLine( "using System.Web.UI;" );
				writer.WriteLine( "using System.Web.UI.WebControls;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.Caching;" );
				writer.WriteLine( "using EnterpriseWebLibrary.Collections;" ); // Necessary for row constants
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.RetrievalCaching;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.RevisionHistory;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess.StandardModification;" );
				writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
				writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;" );
				writer.WriteLine( "using EnterpriseWebLibrary.InputValidation;" );

				writer.WriteLine();
				writeAssemblyInfo( writer, installation, "Library" );
				writer.WriteLine();
				var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
				if( ConfigurationLogic.SystemProviderExists && !installation.DevelopmentInstallationLogic.SystemIsEwl &&
				    ( recognizedInstallation == null || !recognizedInstallation.SystemIsEwlCacheCoordinator ) )
					generateGeneralProvider( writer, installation );
				if( installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications.Any() ) {
					writer.WriteLine();
					writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
					writer.WriteLine( "public static class WebApplicationNames {" );
					foreach( var i in installation.ExistingInstallationLogic.RuntimeConfiguration.WebApplications )
						writer.WriteLine( "public const string {0} = \"{1}\";".FormatWith( EwlStatics.GetCSharpIdentifierSimple( i.Name.EnglishToPascal() ), i.Name ) );
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
			}
		}

		private void generateGeneralProvider( TextWriter writer, DevelopmentInstallation installation ) {
			writer.WriteLine(
				"namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".Configuration.Providers {" );
			writer.WriteLine( "internal partial class General: SystemGeneralProvider {" );
			ConfigurationLogic.SystemProvider.WriteGeneralProviderMembers( writer );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void generateServerSideConsoleAppStatics( TextWriter writer, DevelopmentInstallation installation ) {
			writer.WriteLine( "namespace " + installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + " {" );
			writer.WriteLine( "public static class ServerSideConsoleAppStatics {" );
			foreach( var project in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.ServerSideConsoleProjectsNonNullable ) {
				writer.WriteLine(
					"public static void Start" + project.Name.EnglishToPascal() + "( string arguments, string input, string errorMessageIfAlreadyRunning = \"\" ) {" );
				writer.WriteLine( "if( errorMessageIfAlreadyRunning.Any() && Process.GetProcessesByName( \"" + project.NamespaceAndAssemblyName + "\" ).Any() )" );
				writer.WriteLine( "throw new DataModificationException( errorMessageIfAlreadyRunning );" );

				var programPath = "EwlStatics.CombinePaths( ConfigurationStatics.InstallationPath, \"" + project.Name +
				                  "\", ConfigurationStatics.ServerSideConsoleAppRelativeFolderPath, \"" + project.NamespaceAndAssemblyName + "\" )";
				var runProgramExpression = "EwlStatics.RunProgram( " + programPath + ", arguments, input, false )";

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
			foreach( var database in installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration ) {
				try {
					generateDataAccessCodeForDatabase(
						database,
						installation.DevelopmentInstallationLogic.LibraryPath,
						writer,
						baseNamespace,
						database.SecondaryDatabaseName.Length == 0
							? installation.DevelopmentInstallationLogic.DevelopmentConfiguration.database
							: installation.DevelopmentInstallationLogic.DevelopmentConfiguration.secondaryDatabases.Single( sd => sd.name == database.SecondaryDatabaseName ) );
				}
				catch( Exception e ) {
					throw UserCorrectableException.CreateSecondaryException(
						"An exception occurred while generating data access logic for the " +
						( database.SecondaryDatabaseName.Length == 0 ? "primary" : database.SecondaryDatabaseName + " secondary" ) + " database.",
						e );
				}
			}
			if( installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Any( d => d.SecondaryDatabaseName.Length > 0 ) ) {
				writer.WriteLine();
				writer.WriteLine( "namespace " + baseNamespace + " {" );
				writer.WriteLine( "public class SecondaryDatabaseNames {" );
				foreach( var secondaryDatabase in installation.DevelopmentInstallationLogic.DatabasesForCodeGeneration.Where( d => d.SecondaryDatabaseName.Length > 0 ) )
					writer.WriteLine( "public const string " + secondaryDatabase.SecondaryDatabaseName + " = \"" + secondaryDatabase.SecondaryDatabaseName + "\";" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}
		}

		private void generateDataAccessCodeForDatabase(
			EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction.Database database, string libraryBasePath, TextWriter writer, string baseNamespace,
			EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
			// Ensure that all tables specified in the configuration file actually exist.
			var tableNames = DatabaseOps.GetDatabaseTables( database );
			ensureTablesExist( tableNames, configuration.SmallTables, "small" );
			ensureTablesExist( tableNames, configuration.TablesUsingRowVersionedDataCaching, "row-versioned data caching" );
			ensureTablesExist( tableNames, configuration.revisionHistoryTables, "revision history" );

			database.ExecuteDbMethod(
				delegate( DBConnection cn ) {
					// database logic access - standard
					if( !configuration.EveryTableHasKeySpecified || configuration.EveryTableHasKey ) {
						writer.WriteLine();
						TableConstantStatics.Generate( cn, writer, baseNamespace, database );
					}

					// database logic access - custom
					writer.WriteLine();
					RowConstantStatics.Generate( cn, writer, baseNamespace, database, configuration );

					// retrieval and modification commands - standard
					if( !configuration.EveryTableHasKeySpecified || configuration.EveryTableHasKey ) {
						writer.WriteLine();
						CommandConditionStatics.Generate( cn, writer, baseNamespace, database );

						writer.WriteLine();
						var tableRetrievalNamespaceDeclaration = TableRetrievalStatics.GetNamespaceDeclaration( baseNamespace, database );
						TableRetrievalStatics.Generate( cn, writer, tableRetrievalNamespaceDeclaration, database, configuration );

						writer.WriteLine();
						var modNamespaceDeclaration = StandardModificationStatics.GetNamespaceDeclaration( baseNamespace, database );
						StandardModificationStatics.Generate( cn, writer, modNamespaceDeclaration, database, configuration );

						foreach( var tableName in DatabaseOps.GetDatabaseTables( database ) ) {
							TableRetrievalStatics.WritePartialClass( cn, libraryBasePath, tableRetrievalNamespaceDeclaration, database, tableName );
							StandardModificationStatics.WritePartialClass(
								cn,
								libraryBasePath,
								modNamespaceDeclaration,
								database,
								tableName,
								CodeGeneration.DataAccess.DataAccessStatics.IsRevisionHistoryTable( tableName, configuration ) );
						}
					}

					// retrieval and modification commands - custom
					writer.WriteLine();
					QueryRetrievalStatics.Generate( cn, writer, baseNamespace, database, configuration );
					writer.WriteLine();
					CustomModificationStatics.Generate( cn, writer, baseNamespace, database, configuration );

					// other commands
					if( cn.DatabaseInfo is OracleInfo ) {
						writer.WriteLine();
						SequenceStatics.Generate( cn, writer, baseNamespace, database );
						writer.WriteLine();
						ProcedureStatics.Generate( cn, writer, baseNamespace, database );
					}
				} );
		}

		private void ensureTablesExist( IEnumerable<string> databaseTables, IEnumerable<string> specifiedTables, string tableAdjective ) {
			if( specifiedTables == null )
				return;
			var nonexistentTables = specifiedTables.Where( specifiedTable => databaseTables.All( i => !i.EqualsIgnoreCase( specifiedTable ) ) ).ToArray();
			if( nonexistentTables.Any() ) {
				throw new UserCorrectableException(
					tableAdjective.CapitalizeString() + " " + ( nonexistentTables.Count() > 1 ? "tables" : "table" ) + " " +
					StringTools.GetEnglishListPhrase( nonexistentTables.Select( i => "'" + i + "'" ), true ) + " " + ( nonexistentTables.Count() > 1 ? "do" : "does" ) +
					" not exist." );
			}
		}

		private void generateWebConfigAndCodeForWebProject( DevelopmentInstallation installation, WebProject webProject ) {
			var webProjectPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, webProject.name );

			// This must be done before web meta logic generation, which can be affected by the contents of Web.config files.
			WebConfigStatics.GenerateWebConfig( webProject, webProjectPath );

			var webProjectGeneratedCodeFolderPath = EwlStatics.CombinePaths( webProjectPath, "Generated Code" );
			Directory.CreateDirectory( webProjectGeneratedCodeFolderPath );
			var webProjectIsuFilePath = EwlStatics.CombinePaths( webProjectGeneratedCodeFolderPath, "ISU.cs" );
			IoMethods.DeleteFile( webProjectIsuFilePath );
			using( TextWriter writer = new StreamWriter( webProjectIsuFilePath ) ) {
				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Collections.Generic;" );
				writer.WriteLine( "using System.Collections.ObjectModel;" );
				writer.WriteLine( "using System.Globalization;" );
				writer.WriteLine( "using System.Linq;" );
				writer.WriteLine( "using System.Reflection;" );
				writer.WriteLine( "using System.Runtime.InteropServices;" );
				writer.WriteLine( "using System.Web;" );
				writer.WriteLine( "using System.Web.UI;" );
				writer.WriteLine( "using System.Web.UI.WebControls;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework;" );
				writer.WriteLine( "using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;" );
				writer.WriteLine( "using EnterpriseWebLibrary.InputValidation;" );
				writer.WriteLine();
				writeAssemblyInfo( writer, installation, webProject.name );
				writer.WriteLine();
				CodeGeneration.WebMetaLogic.WebMetaLogicStatics.Generate( writer, webProjectPath, webProject );
			}
		}

		private void generateWindowsServiceCode( DevelopmentInstallation installation, WindowsService service ) {
			var serviceProjectGeneratedCodeFolderPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, service.Name, "Generated Code" );
			Directory.CreateDirectory( serviceProjectGeneratedCodeFolderPath );
			var isuFilePath = EwlStatics.CombinePaths( serviceProjectGeneratedCodeFolderPath, "ISU.cs" );
			IoMethods.DeleteFile( isuFilePath );
			using( TextWriter writer = new StreamWriter( isuFilePath ) ) {
				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.ComponentModel;" );
				writer.WriteLine( "using System.Reflection;" );
				writer.WriteLine( "using System.Runtime.InteropServices;" );
				writer.WriteLine( "using System.ServiceProcess;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine( "using EnterpriseWebLibrary.WindowsServiceFramework;" );
				writer.WriteLine();
				writeAssemblyInfo( writer, installation, service.Name );
				writer.WriteLine();
				writer.WriteLine( "namespace " + service.NamespaceAndAssemblyName + " {" );

				writer.WriteLine( "internal static partial class Program {" );

				writer.WriteLine( "[ MTAThread ]" );
				writer.WriteLine( "private static void Main() {" );
				writer.WriteLine( "InitStatics();" );
				writer.WriteLine( "try {" );
				writer.WriteLine(
					"TelemetryStatics.ExecuteBlockWithStandardExceptionHandling( () => ServiceBase.Run( new ServiceBaseAdapter( new " + service.Name.EnglishToPascal() +
					"() ) ) );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "internal static void InitStatics() {" );
				writer.WriteLine( "SystemInitializer globalInitializer = null;" );
				writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
				writer.WriteLine( "var dataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );" );
				writer.WriteLine(
					"GlobalInitializationOps.InitStatics( globalInitializer, \"" + service.Name +
					"\" + \" Executable\", false, mainDataAccessStateGetter: () => dataAccessState.Value );" );
				writer.WriteLine( "}" );

				writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer globalInitializer );" );

				writer.WriteLine( "}" );

				writer.WriteLine( "[ RunInstaller( true ) ]" );
				writer.WriteLine( "public class Installer: System.Configuration.Install.Installer {" );

				writer.WriteLine( "public Installer() {" );
				writer.WriteLine( "Program.InitStatics();" );
				writer.WriteLine( "try {" );
				writer.WriteLine( "var code = GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( delegate {" );
				writer.WriteLine( "Installers.Add( WindowsServiceMethods.CreateServiceProcessInstaller() );" );
				writer.WriteLine( "Installers.Add( WindowsServiceMethods.CreateServiceInstaller( new " + service.Name.EnglishToPascal() + "() ) );" );
				writer.WriteLine( "} );" );
				writer.WriteLine( "if( code != 0 )" );
				writer.WriteLine(
					"throw new ApplicationException( \"Service installer objects could not be created. More information should be available in a separate error email from the service executable.\" );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "}" );

				writer.WriteLine( "internal partial class " + service.Name.EnglishToPascal() + ": WindowsServiceBase {" );
				writer.WriteLine( "internal " + service.Name.EnglishToPascal() + "() {}" );
				writer.WriteLine( "string WindowsServiceBase.Name { get { return \"" + service.Name + "\"; } }" );
				writer.WriteLine( "}" );

				writer.WriteLine( "}" );
			}
		}

		private void generateServerSideConsoleProjectCode( DevelopmentInstallation installation, ServerSideConsoleProject project ) {
			var projectGeneratedCodeFolderPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, project.Name, "Generated Code" );
			Directory.CreateDirectory( projectGeneratedCodeFolderPath );
			var isuFilePath = EwlStatics.CombinePaths( projectGeneratedCodeFolderPath, "ISU.cs" );
			IoMethods.DeleteFile( isuFilePath );
			using( TextWriter writer = new StreamWriter( isuFilePath ) ) {
				writer.WriteLine( "using System;" );
				writer.WriteLine( "using System.Reflection;" );
				writer.WriteLine( "using System.Runtime.InteropServices;" );
				writer.WriteLine( "using System.Threading;" );
				writer.WriteLine( "using EnterpriseWebLibrary;" );
				writer.WriteLine( "using EnterpriseWebLibrary.DataAccess;" );
				writer.WriteLine();
				writeAssemblyInfo( writer, installation, project.Name );
				writer.WriteLine();
				writer.WriteLine( "namespace " + project.NamespaceAndAssemblyName + " {" );
				writer.WriteLine( "internal static partial class Program {" );

				writer.WriteLine( "[ MTAThread ]" );
				writer.WriteLine( "private static int Main( string[] args ) {" );
				writer.WriteLine( "SystemInitializer globalInitializer = null;" );
				writer.WriteLine( "initGlobalInitializer( ref globalInitializer );" );
				writer.WriteLine( "var dataAccessState = new ThreadLocal<DataAccessState>( () => new DataAccessState() );" );
				writer.WriteLine(
					"GlobalInitializationOps.InitStatics( globalInitializer, \"" + project.Name + "\", false, mainDataAccessStateGetter: () => dataAccessState.Value );" );
				writer.WriteLine( "try {" );
				writer.WriteLine( "return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( () => ewlMain( args ) );" );
				writer.WriteLine( "}" );
				writer.WriteLine( "finally {" );
				writer.WriteLine( "GlobalInitializationOps.CleanUpStatics();" );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );

				writer.WriteLine( "static partial void initGlobalInitializer( ref SystemInitializer globalInitializer );" );
				writer.WriteLine( "static partial void ewlMain( string[] args );" );

				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}
		}

		private void generateCodeForProject( DevelopmentInstallation installation, string projectName, Action<TextWriter> codeWriter ) {
			var generatedCodeFolderPath = EwlStatics.CombinePaths( installation.GeneralLogic.Path, projectName, "Generated Code" );
			Directory.CreateDirectory( generatedCodeFolderPath );
			var isuFilePath = EwlStatics.CombinePaths( generatedCodeFolderPath, "ISU.cs" );
			IoMethods.DeleteFile( isuFilePath );
			using( TextWriter writer = new StreamWriter( isuFilePath ) )
				codeWriter( writer );
		}

		private void writeAssemblyInfo( TextWriter writer, DevelopmentInstallation installation, string projectName ) {
			writeAssemblyAttribute(
				writer,
				"AssemblyTitle",
				"\"" + installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + projectName.PrependDelimiter( " - " ) + "\"" );
			writeAssemblyAttribute( writer, "AssemblyProduct", "\"" + installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + "\"" );
			writeAssemblyAttribute( writer, "ComVisible", "false" );
			writeAssemblyAttribute( writer, "AssemblyVersion", "\"" + installation.CurrentMajorVersion + ".0." + installation.NextBuildNumber + ".0\"" );
		}

		private void writeAssemblyAttribute( TextWriter writer, string name, string value ) {
			writer.WriteLine( "[ assembly: " + name + "( " + value + " ) ]" );
		}

		private void generateXmlSchemaLogicForCustomInstallationConfigurationXsd( DevelopmentInstallation installation ) {
			const string customInstallationConfigSchemaPathInProject = @"Configuration\Installation\Custom.xsd";
			if( File.Exists( EwlStatics.CombinePaths( installation.DevelopmentInstallationLogic.LibraryPath, customInstallationConfigSchemaPathInProject ) ) ) {
				generateXmlSchemaLogic(
					installation.DevelopmentInstallationLogic.LibraryPath,
					customInstallationConfigSchemaPathInProject,
					installation.DevelopmentInstallationLogic.DevelopmentConfiguration.LibraryNamespaceAndAssemblyName + ".Configuration.Installation",
					"Installation Custom Configuration.cs",
					true );
			}
		}

		private void generateXmlSchemaLogicForOtherXsdFiles( DevelopmentInstallation installation ) {
			if( installation.DevelopmentInstallationLogic.DevelopmentConfiguration.xmlSchemas != null ) {
				foreach( var xmlSchema in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.xmlSchemas ) {
					generateXmlSchemaLogic(
						EwlStatics.CombinePaths( installation.GeneralLogic.Path, xmlSchema.project ),
						xmlSchema.pathInProject,
						xmlSchema.@namespace,
						xmlSchema.codeFileName,
						xmlSchema.useSvcUtil );
				}
			}
		}

		private void generateXmlSchemaLogic( string projectPath, string schemaPathInProject, string nameSpace, string codeFileName, bool useSvcUtil ) {
			var projectGeneratedCodeFolderPath = EwlStatics.CombinePaths( projectPath, "Generated Code" );
			if( useSvcUtil ) {
				try {
					EwlStatics.RunProgram(
						EwlStatics.CombinePaths( AppStatics.DotNetToolsFolderPath, "SvcUtil" ),
						"/d:\"" + projectGeneratedCodeFolderPath + "\" /noLogo \"" + EwlStatics.CombinePaths( projectPath, schemaPathInProject ) + "\" /o:\"" + codeFileName +
						"\" /dconly /n:*," + nameSpace + " /ser:DataContractSerializer",
						"",
						true );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Failed to generate XML schema logic using SvcUtil.", e );
				}
			}
			else {
				Directory.CreateDirectory( projectGeneratedCodeFolderPath );
				try {
					EwlStatics.RunProgram(
						EwlStatics.CombinePaths( AppStatics.DotNetToolsFolderPath, "xsd" ),
						"/nologo \"" + EwlStatics.CombinePaths( projectPath, schemaPathInProject ) + "\" /c /n:" + nameSpace + " /o:\"" + projectGeneratedCodeFolderPath + "\"",
						"",
						true );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Failed to generate XML schema logic using xsd.", e );
				}
				var outputCodeFilePath = EwlStatics.CombinePaths( projectGeneratedCodeFolderPath, Path.GetFileNameWithoutExtension( schemaPathInProject ) + ".cs" );
				var desiredCodeFilePath = EwlStatics.CombinePaths( projectGeneratedCodeFolderPath, codeFileName );
				if( outputCodeFilePath != desiredCodeFilePath ) {
					try {
						IoMethods.MoveFile( outputCodeFilePath, desiredCodeFilePath );
					}
					catch( IOException e ) {
						throw new UserCorrectableException( "Failed to move the generated code file for an XML schema. Please try the operation again.", e );
					}
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
				writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".v12.suo" );
				writer.WriteLine( "packages/" );
				writer.WriteLine( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemName + ".sln.DotSettings.user" );
				writer.WriteLine( "Error Log.txt" );
				writer.WriteLine( "*.csproj.user" );
				writer.WriteLine( "*" + CodeGeneration.DataAccess.DataAccessStatics.CSharpTemplateFileExtension );
				writer.WriteLine();
				writer.WriteLine( "Solution Files/bin/" );
				writer.WriteLine( "Solution Files/obj/" );
				writer.WriteLine();
				writer.WriteLine( "Library/bin/" );
				writer.WriteLine( "Library/obj/" );
				writer.WriteLine( "Library/Generated Code/" );
				writer.WriteLine( "Library/" + InstallationFileStatics.FilesFolderName + "/" + asposeLicenseFileName );

				foreach( var webProject in installation.DevelopmentInstallationLogic.DevelopmentConfiguration.webProjects ?? new WebProject[ 0 ] ) {
					writer.WriteLine();
					writer.WriteLine( webProject.name + "/bin/" );
					writer.WriteLine( webProject.name + "/obj/" );
					writer.WriteLine( webProject.name + "/" + StaticFileHandler.EwfFolderName + "/" );
					writer.WriteLine( webProject.name + "/" + AppStatics.StandardLibraryFilesFileName );
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