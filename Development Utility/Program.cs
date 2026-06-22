using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility;

internal static class Program {
	[ MTAThread ]
	private static int Main( string[] args ) {
		GlobalInitializationOps.InitStatics( new GlobalInitializer(), "Development Utility", true );
		try {
			return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling( () => {
				try {
					Console.WriteLine(
						"{0} Development Utility, installation path {1}".FormatWith(
							EwlStatics.EwlInitialism,
							Path.GetFullPath( ConfigurationStatics.InstallationConfiguration.InstallationPath ) ) );

					AppStatics.Init();

					if( args.Length < 1 )
						throw new UserCorrectableException( "You must specify the operation name as the first argument." );

					// Create installations folder from template if necessary.
					var installationPath = Environment.CurrentDirectory;
					var templateFolderPath = getInstallationsFolderPath( installationPath, true );
					var message = "";
					if( !Directory.Exists( templateFolderPath ) )
						message = "No installation-configuration template exists.";
					else {
						var configurationFolderPath = getInstallationsFolderPath( installationPath, false );
						if( IoMethods.GetFilePathsInFolder( configurationFolderPath, searchOption: SearchOption.AllDirectories ).Any() )
							message = "Installation configuration already exists.";
						else {
							IoMethods.CopyFolder( templateFolderPath, configurationFolderPath, false );
							Log.Information( "Created installation configuration from template." );
						}
					}

					if( args[ 0 ] == "create-installation-configuration" ) {
						if( message.Any() )
							throw new UserCorrectableException( message );
						return;
					}

					// Get installation.
					DevelopmentInstallation installation;
					try {
						installation = getInstallation( installationPath );
					}
					catch( Exception e ) {
						throw new UserCorrectableException( "The installation at \"" + installationPath + "\" is invalid.", e );
					}

					// Get operation.
					var operations = AssemblyTools.BuildSingletonDictionary<Operation, string>( Assembly.GetExecutingAssembly(), i => i.GetType().Name );

					// This temporary code supports migration to new kebab-cased operation names, and the rename of UpdateDependentLogic to Sync.
					foreach( var i in operations.Materialize() )
						operations.Add( i.Key.CamelToEnglish().ToUrlSlug(), i.Value );
					operations.Add( "sync", operations[ "UpdateDependentLogic" ] );

					var operationName = args[ 0 ];
					if( !operations.TryGetValue( operationName, out var operation ) )
						throw new UserCorrectableException( operationName + " is not a known operation." );

					if( !operation.IsValid( installation ) )
						throw new UserCorrectableException( "The " + operation.GetType().Name + " operation cannot be performed on this installation." );
					operation.Execute( installation, args.Skip( 1 ).MaterializeAsList(), new OperationResult() );
				}
				catch( Exception e ) {
					Log.Error( e.ToString() );
					if( e is UserCorrectableException )
						throw new DoNotEmailOrLogException();
					throw;
				}
			} );
		}
		finally {
			GlobalInitializationOps.CleanUpStatics();
		}
	}

	private static string getInstallationsFolderPath( string installationPath, bool useTemplate ) =>
		EwlStatics.CombinePaths(
			InstallationFileStatics.GetGeneralFilesFolderPath( installationPath, true ),
			InstallationConfiguration.ConfigurationFolderName,
			InstallationConfiguration.InstallationConfigurationFolderName,
			useTemplate ? "{0} Template".FormatWith( InstallationConfiguration.InstallationsFolderName ) : InstallationConfiguration.InstallationsFolderName );

	private static DevelopmentInstallation getInstallation( string path ) {
		var generalInstallationLogic = new GeneralInstallationLogic( path );
		var existingInstallationLogic = new ExistingInstallationLogic( generalInstallationLogic, new InstallationConfiguration( path, true ) );

		if( existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.HasValue ) {
			SystemManagerConnectionStatics.Init();
			var knownSystemLogic = new KnownSystemLogic(
				SystemManagerConnectionStatics.SystemList.Systems.Single( i =>
					i.DevelopmentInstallationId == existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.Value ) );
			var recognizedInstallationLogic = new RecognizedInstallationLogic( existingInstallationLogic, knownSystemLogic );
			return new RecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic, knownSystemLogic, recognizedInstallationLogic );
		}

		return new UnrecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic );
	}
}