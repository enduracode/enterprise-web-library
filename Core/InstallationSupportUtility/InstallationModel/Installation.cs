namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public interface Installation {
		string LatestFullName { get; }
		string LatestFullShortName { get; }
		GeneralInstallationLogic GeneralLogic { get; }
	}
}