namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public interface UnrecognizedInstallation: ExistingInstallation {
	UnrecognizedInstallationLogic UnrecognizedInstallationLogic { get; }
}