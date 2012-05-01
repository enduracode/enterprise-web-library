using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface ExistingInstallation: Installation {
		ExistingInstallationLogic ExistingInstallationLogic { get; }
	}
}