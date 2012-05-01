using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
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