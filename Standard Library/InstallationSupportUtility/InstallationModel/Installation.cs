using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	internal interface Installation {
		int Id { get; }
		string LatestFullName { get; }
		string LatestFullShortName { get; }
		GeneralInstallationLogic GeneralLogic { get; }
	}
}