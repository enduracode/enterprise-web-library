using System.IO;
using System.Security.AccessControl;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class ExistingInstalledInstallationLogic {
		private readonly ExistingInstallationLogic existingInstallationLogic;

		public ExistingInstalledInstallationLogic( ExistingInstallationLogic existingInstallationLogic ) {
			this.existingInstallationLogic = existingInstallationLogic;
		}

		/// <summary>
		/// Creates a text file named "Error Log.txt" in the root of the installation folder and gives NETWORK SERVICE full control over it.
		/// </summary>
		public void CreateFreshLogFile() {
			File.WriteAllText( existingInstallationLogic.RuntimeConfiguration.ErrorLogFilePath, "" );

			// We need to modify permissions after creating the file so we can inherit instead of wiping out parent settings.
			var info = new FileInfo( existingInstallationLogic.RuntimeConfiguration.ErrorLogFilePath );
			var security = info.GetAccessControl();
			security.AddAccessRule( new FileSystemAccessRule( "NETWORK SERVICE", FileSystemRights.FullControl, AccessControlType.Allow ) );
			info.SetAccessControl( security );
		}
	}
}