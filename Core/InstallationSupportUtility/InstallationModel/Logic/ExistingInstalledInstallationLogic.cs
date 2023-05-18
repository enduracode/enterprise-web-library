﻿using System.Collections.Immutable;
using System.Security.AccessControl;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class ExistingInstalledInstallationLogic {
	public static void UpdateIisApplications( ExistingInstalledInstallationLogic newLogic, ExistingInstalledInstallationLogic oldLogic ) {
		var appGetter = new Func<ExistingInstalledInstallationLogic, IEnumerable<WebApplication>>(
			logic => logic?.existingInstallationLogic.RuntimeConfiguration.WebApplications.Where( i => i.IisApplication != null ).Materialize() ??
			         Enumerable.Empty<WebApplication>() );
		var newApps = appGetter( newLogic );
		var oldApps = appGetter( oldLogic );

		if( newApps.Any() )
			IsuStatics.UpdateIisAppPool(
				newLogic.existingInstallationLogic.IisAppPoolName,
				usesClassicClr: newLogic.existingInstallationLogic.RuntimeConfiguration.WebApplications.All(
					i => !File.ReadAllText( i.WebConfigFilePath ).Contains( "<aspNetCore" ) ) );

		var newSiteNames = new HashSet<string>();
		var newVirtualDirectoryNames = new HashSet<string>();
		foreach( var app in newApps ) {
			if( app.IisApplication is Site site ) {
				IsuStatics.UpdateIisSite( getIisSiteName( newLogic, app ), newLogic.existingInstallationLogic.IisAppPoolName, app.Path, site.HostNames );
				newSiteNames.Add( getIisSiteName( newLogic, app ) );
				continue;
			}
			if( app.IisApplication is VirtualDirectory virtualDirectory ) {
				IsuStatics.UpdateIisVirtualDirectory( virtualDirectory.Site, virtualDirectory.Name, newLogic.existingInstallationLogic.IisAppPoolName, app.Path );
				newVirtualDirectoryNames.Add( virtualDirectory.Name );
				continue;
			}
			throw new ApplicationException( "unrecognized IIS application type" );
		}

		foreach( var app in oldApps ) {
			if( app.IisApplication is Site ) {
				if( !newSiteNames.Contains( getIisSiteName( oldLogic, app ) ) )
					IsuStatics.DeleteIisSite( getIisSiteName( oldLogic, app ) );
				continue;
			}
			if( app.IisApplication is VirtualDirectory virtualDirectory ) {
				if( !newVirtualDirectoryNames.Contains( virtualDirectory.Name ) )
					IsuStatics.DeleteIisVirtualDirectory( virtualDirectory.Site, virtualDirectory.Name );
				continue;
			}
			throw new ApplicationException( "unrecognized IIS application type" );
		}

		if( oldApps.Any() && ( !newApps.Any() || newLogic.existingInstallationLogic.IisAppPoolName != oldLogic.existingInstallationLogic.IisAppPoolName ) )
			IsuStatics.DeleteIisAppPool( oldLogic.existingInstallationLogic.IisAppPoolName );
	}

	private static string getIisSiteName( ExistingInstalledInstallationLogic logic, WebApplication app ) =>
		"{0} - {1}".FormatWith( logic.existingInstallationLogic.RuntimeConfiguration.FullShortName, app.Name );

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