namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class UnrecognizedDevelopmentInstallation: DevelopmentInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly DevelopmentInstallationLogic developmentInstallationLogic;

		public UnrecognizedDevelopmentInstallation( GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			developmentInstallationLogic = new DevelopmentInstallationLogic( generalInstallationLogic, existingInstallationLogic, null );
		}

		public override string ToString() {
			return LatestFullName;
		}

		public string LatestFullName { get { return existingInstallationLogic.RuntimeConfiguration.FullName; } }

		public string LatestFullShortName { get { return existingInstallationLogic.RuntimeConfiguration.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }

		public DevelopmentInstallationLogic DevelopmentInstallationLogic { get { return developmentInstallationLogic; } }

		int DevelopmentInstallation.CurrentMajorVersion { get { return 1; } }
		int DevelopmentInstallation.NextBuildNumber { get { return 1; } }
	}
}