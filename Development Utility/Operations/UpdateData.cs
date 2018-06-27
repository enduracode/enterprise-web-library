using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class UpdateData: Operation {
		private static readonly Operation instance = new UpdateData();
		public static Operation Instance => instance;
		private UpdateData() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
			var installation = (DevelopmentInstallation)genericInstallation;

			var source = arguments[ 0 ];
			if( source == "Default" )
				source = "";
			var forceNewPackageDownload = bool.Parse( arguments[ 1 ] );

			RsisInstallation sourceInstallation;
			if( installation is RecognizedDevelopmentInstallation recognizedInstallation ) {
				var sources = SystemListStatics.RsisSystemList.GetDataUpdateSources( recognizedInstallation );
				if( source.Any() ) {
					sourceInstallation = sources.SingleOrDefault( i => i.ShortName == source );
					if( sourceInstallation == null )
						throw new UserCorrectableException( "The specified source does not exist." );
				}
				else {
					sourceInstallation = sources.FirstOrDefault();
					if( sourceInstallation == null )
						throw new UserCorrectableException( "No sources exist." );
				}
			}
			else {
				if( source.Any() )
					throw new UserCorrectableException( "Source-specification is not currently supported." );
				sourceInstallation = null;
			}

			DataUpdateStatics.DownloadDataPackageAndGetDataUpdateMethod( installation, false, sourceInstallation, forceNewPackageDownload, operationResult )();
		}
	}
}