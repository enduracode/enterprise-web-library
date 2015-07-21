namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public interface ExistingInstallation: Installation {
		ExistingInstallationLogic ExistingInstallationLogic { get; }
	}
}