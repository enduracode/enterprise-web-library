using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

namespace EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage {
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

		/// <summary>
		/// Returns a list of installations that are appropriate to update data from, given the installation. Installation Support Utility use only.
		/// </summary>
		public IEnumerable<RsisInstallation> GetDataUpdateSources( RecognizedInstallation installation ) {
			var availableInstallations = GetSystemByInstallationId( installation.Id ).InstalledInstallations.ToList();

			// Development installations can only update data from intermediate installations.
			if( installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType == InstallationType.Development )
				availableInstallations = availableInstallations.Where( inst => inst.InstallationTypeElements is IntermediateInstallationElements ).ToList();

			// Live installations can only update data from themselves or other live installations.
			if( installation.ExistingInstallationLogic.RuntimeConfiguration.InstallationType == InstallationType.Live )
				availableInstallations = availableInstallations.Where( inst => inst.InstallationTypeElements is LiveInstallationElements ).ToList();
			return availableInstallations;
		}
	}
}