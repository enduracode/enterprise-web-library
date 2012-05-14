using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public interface Installation {
		string LatestFullName { get; }
		string LatestFullShortName { get; }
		GeneralInstallationLogic GeneralLogic { get; }
	}
}