using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class UnrecognizedDevelopmentInstallation: DevelopmentInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;

		public UnrecognizedDevelopmentInstallation( GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
		}

		public override string ToString() {
			return LatestFullName;
		}

		public int? Id { get { return null; } }

		public string LatestFullName { get { return existingInstallationLogic.RuntimeConfiguration.FullName; } }

		public string LatestFullShortName { get { return existingInstallationLogic.RuntimeConfiguration.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }
	}
}