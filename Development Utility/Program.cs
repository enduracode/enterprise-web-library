using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.IO;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class Program {
		[ MTAThread ]
		private static int Main( string[] args ) {
			GlobalInitializationOps.InitStatics( new GlobalInitializer(), "Development Utility", true );
			try {
				return GlobalInitializationOps.ExecuteAppWithStandardExceptionHandling(
					() => {
						try {
							AppStatics.Init();

							if( args.Length < 2 )
								throw new UserCorrectableException( "You must specify the installation path as the first argument and the operation name as the second." );

							// If installations folder does not exist, create from template if possible.
							var installationPath = args[ 0 ];
							var configurationFolderPath = getInstallationsFolderPath( installationPath, false );
							if( !Directory.Exists( configurationFolderPath ) ) {
								var templateFolderPath = getInstallationsFolderPath( installationPath, true );
								if( Directory.Exists( templateFolderPath ) )
									IoMethods.CopyFolder( templateFolderPath, configurationFolderPath, false );
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
							var operationName = args[ 1 ];
							if( !operations.ContainsKey( operationName ) )
								throw new UserCorrectableException( operationName + " is not a known operation." );
							var operation = operations[ operationName ];

							if( !operation.IsValid( installation ) )
								throw new UserCorrectableException( "The " + operation.GetType().Name + " operation cannot be performed on this installation." );
							operation.Execute( installation, args.Skip( 2 ), new OperationResult() );
						}
						catch( Exception e ) {
							Output.WriteTimeStampedError( e.ToString() );
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
				ConfigurationLogic.Init();
				SystemListStatics.RefreshSystemList();
				var knownSystemLogic = new KnownSystemLogic(
					SystemListStatics.RsisSystemList.Systems.Single(
						i => i.DevelopmentInstallationId == existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.Value ) );
				var recognizedInstallationLogic = new RecognizedInstallationLogic( existingInstallationLogic, knownSystemLogic );
				return new RecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic, knownSystemLogic, recognizedInstallationLogic );
			}

			return new UnrecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic );
		}
	}
}