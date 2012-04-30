using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	internal class KnownSystemLogic {
		private readonly SoftwareSystem rsisSystem;

		internal KnownSystemLogic( SoftwareSystem rsisSystem ) {
			this.rsisSystem = rsisSystem;
		}

		internal SoftwareSystem RsisSystem { get { return rsisSystem; } }
	}
}