namespace EnterpriseWebLibrary.InstallationSupportUtility.RsisInterface.Messages.SystemListMessage {
	public partial class SystemList {
		public RsisInstallation GetInstallationById( int installationId ) {
			foreach( var system in Systems ) {
				foreach( var installation in system.InstalledInstallations ) {
					if( installation.Id == installationId )
						return installation;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the system that contains the specified installation ID. Accepts IDs for both development installations and installed installations.
		/// </summary>
		public SoftwareSystem GetSystemByInstallationId( int installationId ) {
			foreach( var system in Systems ) {
				if( system.DevelopmentInstallationId == installationId )
					return system;
				foreach( var installation in system.InstalledInstallations ) {
					if( installation.Id == installationId )
						return system;
				}
			}
			return null;
		}
	}
}