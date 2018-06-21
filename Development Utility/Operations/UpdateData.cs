using System.Collections.Generic;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

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
		}
	}
}