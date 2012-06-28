using RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class KnownSystemLogic {
		private readonly SoftwareSystem rsisSystem;

		public KnownSystemLogic( SoftwareSystem rsisSystem ) {
			this.rsisSystem = rsisSystem;
		}

		public SoftwareSystem RsisSystem { get { return rsisSystem; } }
	}
}