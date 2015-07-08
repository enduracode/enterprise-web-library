namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface ExistingInstallation: Installation {
		ExistingInstallationLogic ExistingInstallationLogic { get; }
	}
}