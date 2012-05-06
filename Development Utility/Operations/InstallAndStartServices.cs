using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations {
	internal class InstallAndStartServices: Operation {
		private static readonly Operation instance = new InstallAndStartServices();
		public static Operation Instance { get { return instance; } }
		private InstallAndStartServices() {}

		bool Operation.IsValid( Installation installation ) {
			return installation is DevelopmentInstallation;
		}

		void Operation.Execute( Installation genericInstallation, OperationResult operationResult ) {
			var installation = genericInstallation as DevelopmentInstallation;
			installation.ExistingInstallationLogic.InstallServices();
			installation.ExistingInstallationLogic.Start();
		}
	}
}