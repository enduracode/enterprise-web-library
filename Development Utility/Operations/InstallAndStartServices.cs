using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class InstallAndStartServices: Operation {
	private static readonly Operation instance = new InstallAndStartServices();
	public static Operation Instance => instance;
	private InstallAndStartServices() {}

	bool Operation.IsValid( Installation installation ) => installation is RecognizedDevelopmentInstallation;

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		var installation = (RecognizedDevelopmentInstallation)genericInstallation;
		installation.ExistingInstallationLogic.InstallServices();
		installation.ExistingInstallationLogic.Start();
	}
}