using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class StopAndUninstallServices: Operation {
		private static readonly Operation instance = new StopAndUninstallServices();
		public static Operation Instance { get { return instance; } }
		private StopAndUninstallServices() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is RecognizedDevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			var installation = genericInstallation as RecognizedDevelopmentInstallation;
			installation.ExistingInstallationLogic.UninstallServices();
		}
	}
}