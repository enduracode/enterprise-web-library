using RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class RecognizedInstalledInstallation: KnownInstalledInstallation, RecognizedInstallation {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly KnownSystemLogic knownSystemLogic;
		private readonly KnownInstallationLogic knownInstallationLogic;
		private readonly RecognizedInstallationLogic recognizedInstallationLogic;

		public RecognizedInstalledInstallation( GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		                                        KnownSystemLogic knownSystemLogic, RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			this.knownSystemLogic = knownSystemLogic;
			knownInstallationLogic =
				new KnownInstallationLogic( SystemListStatics.RsisSystemList.GetInstallationById( existingInstallationLogic.RuntimeConfiguration.RsisInstallationId.Value ) );
			this.recognizedInstallationLogic = recognizedInstallationLogic;
		}

		public override string ToString() {
			return LatestFullName;
		}

		public int Id { get { return knownInstallationLogic.RsisInstallation.Id; } }

		public string LatestFullName { get { return knownInstallationLogic.RsisInstallation.FullName; } }

		public string LatestFullShortName { get { return knownInstallationLogic.RsisInstallation.FullShortName; } }

		// NOTE: This is a total duplication of InstallationsRetrieval. ISU can't get an InstallationsRetrieval object and the web site can't get an ISU Installation object.
		// It's crap, but I'm not going to let it stop me from doing this for a third time.
		public string TransactionLogBackupsPath { get { return StandardLibraryMethods.CombinePaths( ConfigurationLogic.TransactionLogBackupsPath, LatestFullShortName ); } }

		public string DownloadedTransactionLogsFolderPath { get { return StandardLibraryMethods.CombinePaths( ConfigurationLogic.DownloadedTransactionLogsFolderPath, LatestFullShortName ); } }

		public GeneralInstallationLogic GeneralLogic { get { return generalInstallationLogic; } }

		public ExistingInstallationLogic ExistingInstallationLogic { get { return existingInstallationLogic; } }

		public KnownSystemLogic KnownSystemLogic { get { return knownSystemLogic; } }

		public KnownInstallationLogic KnownInstallationLogic { get { return knownInstallationLogic; } }

		public RecognizedInstallationLogic RecognizedInstallationLogic { get { return recognizedInstallationLogic; } }
	}
}