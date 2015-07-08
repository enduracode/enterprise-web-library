namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class NonexistentInstallation: KnownInstalledInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly KnownSystemLogic knownSystemLogic;
		private readonly KnownInstallationLogic knownInstallationLogic;

		public NonexistentInstallation( GeneralInstallationLogic generalInstallationLogic, KnownSystemLogic knownSystemLogic,
		                                KnownInstallationLogic knownInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.knownSystemLogic = knownSystemLogic;
			this.knownInstallationLogic = knownInstallationLogic;
		}

		public override string ToString() {
			return LatestFullName;
		}

		public int Id { get { return knownInstallationLogic.RsisInstallation.Id; } }

		public string LatestFullName { get { return knownInstallationLogic.RsisInstallation.FullName; } }

		public string LatestFullShortName { get { return knownInstallationLogic.RsisInstallation.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public KnownSystemLogic KnownSystemLogic { get { return knownSystemLogic; } }

		public KnownInstallationLogic KnownInstallationLogic { get { return knownInstallationLogic; } }
	}
}