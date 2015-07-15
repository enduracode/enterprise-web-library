namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public interface KnownInstalledInstallation: KnownInstallation {
		KnownInstallationLogic KnownInstallationLogic { get; }
	}
}