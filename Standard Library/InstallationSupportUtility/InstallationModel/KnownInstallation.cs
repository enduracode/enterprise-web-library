using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	internal interface KnownInstallation: Installation {
		KnownSystemLogic KnownSystemLogic { get; }
	}
}