namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface KnownInstalledInstallation: KnownInstallation {
		KnownInstallationLogic KnownInstallationLogic { get; }
	}
}