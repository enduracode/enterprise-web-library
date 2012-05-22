using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface DevelopmentInstallation: ExistingInstallation {
		DevelopmentInstallationLogic DevelopmentInstallationLogic { get; }
		int CurrentMajorVersion { get; }
		int NextBuildNumber { get; }
	}
}