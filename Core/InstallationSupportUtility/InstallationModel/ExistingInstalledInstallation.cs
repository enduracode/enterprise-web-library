namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public interface ExistingInstalledInstallation: ExistingInstallation {
		ExistingInstalledInstallationLogic ExistingInstalledInstallationLogic { get; }
	}
}