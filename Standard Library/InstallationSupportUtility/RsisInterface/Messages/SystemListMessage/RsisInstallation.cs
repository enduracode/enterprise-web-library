
using RedStapler.StandardLibrary.Configuration;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage {
	public partial class RsisInstallation {
		public string FullName { get { return InstallationConfiguration.GetFullNameFromSystemAndInstallationNames( ConfigurationLogic.RsisSystemList.GetSystemByInstallationId( Id ).Name, Name ); } }

		public string FullShortName {
			get {
				return InstallationConfiguration.GetFullShortNameFromSystemAndInstallationNames(
					ConfigurationLogic.RsisSystemList.GetSystemByInstallationId( Id ).ShortName, ShortName );
			}
		}

		public override string ToString() {
			return FullName;
		}
	}
}