using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	internal interface KnownInstalledInstallation: KnownInstallation {
		KnownInstallationLogic KnownInstallationLogic { get; }
	}
}