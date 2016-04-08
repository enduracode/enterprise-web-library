namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class UnrecognizedInstalledInstallation: ExistingInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly ExistingInstalledInstallationLogic existingInstalledInstallationLogic;

		public UnrecognizedInstalledInstallation(
			GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
			ExistingInstalledInstallationLogic existingInstalledInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			this.existingInstalledInstallationLogic = existingInstalledInstallationLogic;
		}

		public override string ToString() {
			return LatestFullName;
		}

		public string LatestFullName { get { return existingInstallationLogic.RuntimeConfiguration.FullName; } }

		public string LatestFullShortName { get { return existingInstallationLogic.RuntimeConfiguration.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }

		public ExistingInstalledInstallationLogic ExistingInstalledInstallationLogic { get { return existingInstalledInstallationLogic; } }
	}
}