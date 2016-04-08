using System;
using System.IO;
using System.Security.AccessControl;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class ExistingInstalledInstallationLogic {
		private readonly ExistingInstallationLogic existingInstallationLogic;

		public ExistingInstalledInstallationLogic( ExistingInstallationLogic existingInstallationLogic ) {
			this.existingInstallationLogic = existingInstallationLogic;
		}

		public void PatchLogicForEnvironment() {
			var isWin7 = Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
			if( isWin7 ) {
				foreach( var i in existingInstallationLogic.RuntimeConfiguration.WebApplications ) {
					File.WriteAllText(
						i.WebConfigFilePath,
						File.ReadAllText( i.WebConfigFilePath )
							.Replace( "<applicationInitialization doAppInitAfterRestart=\"true\" />", "<!--<applicationInitialization doAppInitAfterRestart=\"true\" />-->" )
							.Replace( "<add name=\"ApplicationInitializationModule\" />", "<!--<add name=\"ApplicationInitializationModule\" />-->" ) );
				}
			}
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