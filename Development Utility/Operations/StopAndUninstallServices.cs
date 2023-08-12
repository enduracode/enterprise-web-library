using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class StopAndUninstallServices: Operation {
	private static readonly Operation instance = new StopAndUninstallServices();
	public static Operation Instance => instance;
	private StopAndUninstallServices() {}

	bool Operation.IsValid( Installation installation ) => installation is RecognizedDevelopmentInstallation;

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		var installation = (RecognizedDevelopmentInstallation)genericInstallation;
		installation.ExistingInstallationLogic.UninstallServices();
	}
}