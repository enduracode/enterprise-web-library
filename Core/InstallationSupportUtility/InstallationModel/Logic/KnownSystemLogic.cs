using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class KnownSystemLogic {
		private readonly SoftwareSystem rsisSystem;

		public KnownSystemLogic( SoftwareSystem rsisSystem ) {
			this.rsisSystem = rsisSystem;
		}

		public SoftwareSystem RsisSystem => rsisSystem;
	}
}