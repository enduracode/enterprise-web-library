using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	internal class KnownInstallationLogic {
		private readonly RsisInstallation rsisInstallation;

		internal KnownInstallationLogic( RsisInstallation rsisInstallation ) {
			this.rsisInstallation = rsisInstallation;
		}

		/// <summary>
		/// Use only if this installation exists in RSIS. This information comes from the System List.
		/// </summary>
		internal RsisInstallation RsisInstallation { get { return rsisInstallation; } }
	}
}