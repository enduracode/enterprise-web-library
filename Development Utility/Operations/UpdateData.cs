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
			var installation = genericInstallation as DevelopmentInstallation;
		}
	}
}