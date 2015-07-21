using EnterpriseWebLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class KnownInstallationLogic {
		private readonly RsisInstallation rsisInstallation;

		public KnownInstallationLogic( RsisInstallation rsisInstallation ) {
			this.rsisInstallation = rsisInstallation;
		}

		/// <summary>
		/// Use only if this installation exists in RSIS. This information comes from the System List.
		/// </summary>
		public RsisInstallation RsisInstallation { get { return rsisInstallation; } }
	}
}