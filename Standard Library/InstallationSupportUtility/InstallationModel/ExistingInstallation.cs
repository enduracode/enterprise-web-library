using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	internal interface ExistingInstallation: Installation {
		ExistingInstallationLogic ExistingInstallationLogic { get; }
	}
}