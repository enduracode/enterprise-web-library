using System;
using System.Linq;
using System.Reflection;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class Program {
		[ MTAThread ]
		private static int Main( string[] args ) {
			AppTools.Init( "Development Utility", true, new GlobalLogic() );
			return AppTools.ExecuteAppWithStandardExceptionHandling( () => {
				try {
					// Get installation.
					var installationPath = args[ 0 ];
					var installation = getInstallation( installationPath );

					// Get operation.
					var operations = AssemblyTools.BuildSingletonDictionary<Operation, string>( Assembly.GetExecutingAssembly(), i => i.GetType().Name );
					var operationName = args[ 1 ];
					if( !operations.ContainsKey( operationName ) )
						throw new UserCorrectableException( operationName + " is not a known operation." );
					var operation = operations[ operationName ];

					if( !operation.IsValid( installation ) )
						throw new UserCorrectableException( "The " + operation.GetType().Name + " operation cannot be performed on this installation." );
					operation.Execute( installation, new OperationResult() );
				}
				catch( Exception e ) {
					Output.WriteTimeStampedError( e.ToString() );
					if( !( e is UserCorrectableException ) )
						throw;
				}
			} );
		}

		private static DevelopmentInstallation getInstallation( string path ) {
			var generalInstallationLogic = new GeneralInstallationLogic( path );
			var existingInstallationLogic = new ExistingInstallationLogic( generalInstallationLogic, new InstallationConfiguration( path, true ) );

			if( existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.HasValue ) {
				ConfigurationLogic.Init();
				SystemListStatics.RefreshSystemList();
				var knownSystemLogic =
					new KnownSystemLogic(
						SystemListStatics.RsisSystemList.Systems.Single(
							i => i.DevelopmentInstallationId == existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.Value ) );
				var recognizedInstallationLogic = new RecognizedInstallationLogic( existingInstallationLogic, knownSystemLogic );
				return new RecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic, knownSystemLogic, recognizedInstallationLogic );
			}

			return new UnrecognizedDevelopmentInstallation( generalInstallationLogic, existingInstallationLogic );
		}
	}
}