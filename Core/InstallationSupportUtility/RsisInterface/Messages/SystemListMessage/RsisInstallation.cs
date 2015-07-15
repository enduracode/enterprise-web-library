using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage {
	public partial class RsisInstallation {
		public string FullName { get { return InstallationConfiguration.GetFullNameFromSystemAndInstallationNames( SystemListStatics.RsisSystemList.GetSystemByInstallationId( Id ).Name, Name ); } }

		public string FullShortName {
			get {
				return InstallationConfiguration.GetFullShortNameFromSystemAndInstallationNames(
					SystemListStatics.RsisSystemList.GetSystemByInstallationId( Id ).ShortName, ShortName );
			}
		}

		public override string ToString() {
			return FullName;
		}
	}
}