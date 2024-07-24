using System.Collections.Immutable;
using System.Security.AccessControl;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

[ PublicAPI ]
public class ExistingInstalledInstallationLogic {
	public static void UpdateIisApplications( ExistingInstalledInstallationLogic? newLogic, ExistingInstalledInstallationLogic? oldLogic ) {
		var appGetter = new Func<ExistingInstalledInstallationLogic?, IEnumerable<WebApplication>>(
			logic => logic?.existingInstallationLogic.RuntimeConfiguration.WebApplications.Where( i => i.IisAppPoolAndSiteName!.Length > 0 ).Materialize() ??
			         Enumerable.Empty<WebApplication>() );
		var newAppPoolAndSiteNames = new HashSet<string>();
		var newVirtualDirectoryNames = new HashSet<string>();

		foreach( var newApp in appGetter( newLogic ) ) {
			IsuStatics.UpdateIisAppPool( newApp.IisAppPoolAndSiteName! );

			if( newApp.IisApplication is Site site ) {
				IsuStatics.UpdateIisSite( newApp.IisAppPoolAndSiteName!, newApp.IisAppPoolAndSiteName!, newApp.Path, site.HostNames );
				newAppPoolAndSiteNames.Add( newApp.IisAppPoolAndSiteName! );
			}
			else if( newApp.IisApplication is VirtualDirectory virtualDirectory ) {
				IsuStatics.UpdateIisVirtualDirectory( virtualDirectory.Site, virtualDirectory.Name, newApp.IisAppPoolAndSiteName!, newApp.Path );
				newVirtualDirectoryNames.Add( virtualDirectory.Name );
			}
			else
				throw new ApplicationException( "unrecognized IIS application type" );
		}

		foreach( var oldApp in appGetter( oldLogic ) ) {
			if( oldApp.IisApplication is Site ) {
				if( !newAppPoolAndSiteNames.Contains( oldApp.IisAppPoolAndSiteName! ) )
					IsuStatics.DeleteIisSite( oldApp.IisAppPoolAndSiteName! );
			}
			else if( oldApp.IisApplication is VirtualDirectory virtualDirectory ) {
				if( !newVirtualDirectoryNames.Contains( virtualDirectory.Name ) )
					IsuStatics.DeleteIisVirtualDirectory( virtualDirectory.Site, virtualDirectory.Name );
			}
			else
				throw new ApplicationException( "unrecognized IIS application type" );

			if( !newAppPoolAndSiteNames.Contains( oldApp.IisAppPoolAndSiteName! ) )
				IsuStatics.DeleteIisAppPool( oldApp.IisAppPoolAndSiteName! );
		}
	}

	private readonly ExistingInstallationLogic existingInstallationLogic;

	public ExistingInstalledInstallationLogic( ExistingInstallationLogic existingInstallationLogic ) {
		this.existingInstallationLogic = existingInstallationLogic;
	}

	public void PatchLogicForEnvironment() {
		var isWin7 = Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
		if( isWin7 )
			foreach( var i in existingInstallationLogic.RuntimeConfiguration.WebApplications )
				File.WriteAllText(
					i.WebConfigFilePath,
					File.ReadAllText( i.WebConfigFilePath )
						.Replace( "<applicationInitialization doAppInitAfterRestart=\"true\" />", "<!--<applicationInitialization doAppInitAfterRestart=\"true\" />-->" )
						.Replace( "<add name=\"ApplicationInitializationModule\" />", "<!--<add name=\"ApplicationInitializationModule\" />-->" ) );
	}

	/// <summary>
	/// Creates text files in the installation folder and gives NETWORK SERVICE full control over them.
	/// </summary>
	public void CreateFreshLogFiles() {
		createFreshLogFile( existingInstallationLogic.RuntimeConfiguration.ErrorLogFilePath );
		foreach( var i in existingInstallationLogic.RuntimeConfiguration.WebApplications )
			createFreshLogFile( i.DiagnosticLogFilePath );
	}

	private void createFreshLogFile( string filePath ) {
		File.WriteAllText( filePath, "" );

		// We need to modify permissions after creating the file so we can inherit instead of wiping out parent settings.
		var info = new FileInfo( filePath );
		var security = info.GetAccessControl();
		security.AddAccessRule( new FileSystemAccessRule( "NETWORK SERVICE", FileSystemRights.FullControl, AccessControlType.Allow ) );
		info.SetAccessControl( security );
	}

	public IReadOnlyCollection<Tuple<int, string>> GetWebApplicationCertificateIdAndHostNamePairs() =>
		existingInstallationLogic.RuntimeConfiguration.WebApplications.Select( i => i.IisApplication )
			.OfType<Site>()
			.SelectMany( i => i.HostNames )
			.Where( i => i.SecureBinding != null )
			.Select( i => Tuple.Create( i.SecureBinding.CertificateId, i.Name ) )
			.ToImmutableArray();
}