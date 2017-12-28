namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class RecognizedInstalledInstallation: KnownInstalledInstallation, RecognizedInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly ExistingInstalledInstallationLogic existingInstalledInstallationLogic;
		private readonly KnownSystemLogic knownSystemLogic;
		private readonly KnownInstallationLogic knownInstallationLogic;
		private readonly RecognizedInstallationLogic recognizedInstallationLogic;

		public RecognizedInstalledInstallation(
			GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
			ExistingInstalledInstallationLogic existingInstalledInstallationLogic, KnownSystemLogic knownSystemLogic,
			RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			this.existingInstalledInstallationLogic = existingInstalledInstallationLogic;
			this.knownSystemLogic = knownSystemLogic;
			knownInstallationLogic = new KnownInstallationLogic(
				SystemListStatics.RsisSystemList.GetInstallationById( existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.Value ) );
			this.recognizedInstallationLogic = recognizedInstallationLogic;
		}

		public override string ToString() {
			return LatestFullName;
		}

		public int Id { get { return knownInstallationLogic.RsisInstallation.Id; } }

		public string LatestFullName { get { return knownInstallationLogic.RsisInstallation.FullName; } }

		public string LatestFullShortName { get { return knownInstallationLogic.RsisInstallation.FullShortName; } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }

		public ExistingInstalledInstallationLogic ExistingInstalledInstallationLogic { get { return existingInstalledInstallationLogic; } }

		public KnownSystemLogic KnownSystemLogic { get { return knownSystemLogic; } }

		public KnownInstallationLogic KnownInstallationLogic { get { return knownInstallationLogic; } }

		public RecognizedInstallationLogic RecognizedInstallationLogic { get { return recognizedInstallationLogic; } }
	}
}