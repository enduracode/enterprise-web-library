using System;
using System.Linq;
using System.Reflection;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace EnterpriseWebLibrary.DevelopmentUtility {
	internal static class Program {
		[ MTAThread ]
		private static int Main( string[] args ) {
			AppTools.Init( "Development Utility", true, new GlobalLogic() );
			return AppTools.ExecuteAppWithStandardExceptionHandling( () => {
				// NOTE: Make the machine configuration file optional.
				ConfigurationLogic.Init();

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
					throw new UserCorrectableException( "The " + operation.GetType().Name + " operation cannot be performed on installation " + installation.Id + "." );
				operation.Execute( installation, new OperationResult() );
			} );
		}

		private static DevelopmentInstallation getInstallation( string path ) {
			var generalInstallationLogic = new GeneralInstallationLogic( path );
			var existingInstallationLogic = new ExistingInstallationLogic( generalInstallationLogic, new InstallationConfiguration( path, true ) );

			// NOTE: Only do this if there is an RSIS installation ID in the runtime configuration.
			SystemListStatics.RefreshSystemList();
			var rsisSystem =
				SystemListStatics.RsisSystemList.Systems.Single( i => i.DevelopmentInstallationId == existingInstallationLogic.RuntimeConfiguration.RsisInstallationId );
			var knownSystemLogic = new KnownSystemLogic( rsisSystem );
			var recognizedInstallationLogic = new RecognizedInstallationLogic( existingInstallationLogic, knownSystemLogic );
			return new DevelopmentInstallation( generalInstallationLogic, existingInstallationLogic, knownSystemLogic, recognizedInstallationLogic );
		}
	}
}