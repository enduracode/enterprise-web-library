using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface KnownInstallation: Installation {
		KnownSystemLogic KnownSystemLogic { get; }
	}
}