using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class RecognizedDevelopmentInstallation: RecognizedInstallation, DevelopmentInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly KnownSystemLogic knownSystemLogic;
		private readonly RecognizedInstallationLogic recognizedInstallationLogic;
		private readonly DevelopmentInstallationLogic developmentInstallationLogic;

		public RecognizedDevelopmentInstallation( GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		                                          KnownSystemLogic knownSystemLogic, RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			this.knownSystemLogic = knownSystemLogic;
			this.recognizedInstallationLogic = recognizedInstallationLogic;
			developmentInstallationLogic = new DevelopmentInstallationLogic( generalInstallationLogic, existingInstallationLogic, recognizedInstallationLogic );
		}

		public override string ToString() {
			return LatestFullName;
		}

		public int Id { get { return knownSystemLogic.RsisSystem.DevelopmentInstallationId; } }

		public string LatestFullName { get { return existingInstallationLogic.RuntimeConfiguration.FullName; } }

		public string LatestFullShortName { get { return existingInstallationLogic.RuntimeConfiguration.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }

		public KnownSystemLogic KnownSystemLogic { get { return knownSystemLogic; } }

		public RecognizedInstallationLogic RecognizedInstallationLogic { get { return recognizedInstallationLogic; } }

		public DevelopmentInstallationLogic DevelopmentInstallationLogic { get { return developmentInstallationLogic; } }

		int DevelopmentInstallation.CurrentMajorVersion { get { return knownSystemLogic.RsisSystem.CurrentMajorVersion; } }
		int DevelopmentInstallation.NextBuildNumber { get { return knownSystemLogic.RsisSystem.NextBuildNumber; } }
	}
}