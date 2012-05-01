using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.SystemGeneral;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	public class ExistingInstallationLogic {
		public const string SystemDatabaseUpdatesFileName = "Database Updates.sql";

		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly InstallationConfiguration runtimeConfiguration;

		public ExistingInstallationLogic( GeneralInstallationLogic generalInstallationLogic, InstallationConfiguration runtimeConfiguration ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.runtimeConfiguration = runtimeConfiguration;
		}

		public InstallationConfiguration RuntimeConfiguration { get { return runtimeConfiguration; } }

		public string DatabaseUpdateFilePath { get { return StandardLibraryMethods.CombinePaths( runtimeConfiguration.ConfigurationFolderPath, SystemDatabaseUpdatesFileName ); } }

		/// <summary>
		/// Creates a text file named "Error Log.txt" in the root of the installation folder and gives NETWORK SERVICE full control over it.
		/// </summary>
		public void CreateFreshLogFile() {
			File.WriteAllText( runtimeConfiguration.ErrorLogFilePath, "" );

			// We need to modify permissions after creating the file so we can inherit instead of wiping out parent settings.
			var info = new FileInfo( runtimeConfiguration.ErrorLogFilePath );
			var security = info.GetAccessControl();
			security.AddAccessRule( new FileSystemAccessRule( "NETWORK SERVICE", FileSystemRights.FullControl, AccessControlType.Allow ) );
			info.SetAccessControl( security );
		}

		/// <summary>
		/// Stops all web sites and services associated with this installation.
		/// </summary>
		public void Stop( bool stopServices ) {
			if( runtimeConfiguration.InstallationType != InstallationType.Development ) {
				foreach( var site in runtimeConfiguration.WebSiteNames )
					stopWebSite( site );
			}
			if( stopServices ) {
				var allServices = ServiceController.GetServices();
				var serviceNames = RuntimeConfiguration.WindowsServices.Select( s => s.InstalledName );
				foreach( var service in allServices.Where( sc => serviceNames.Contains( sc.ServiceName ) && sc.Status != ServiceControllerStatus.Stopped ) ) {
					service.Stop();
					service.WaitForStatusWithTimeOut( ServiceControllerStatus.Stopped );
				}
			}
		}

		public void UninstallServices() {
			var allServices = ServiceController.GetServices();
			foreach( var service in runtimeConfiguration.WindowsServices.Where( s => allServices.Any( sc => sc.ServiceName == s.InstalledName ) ) )
				runInstallutil( service, true );
		}

		/// <summary>
		/// Starts all web sites and services associated with this installation.
		/// </summary>
		public void Start() {
			var allServices = ServiceController.GetServices();
			foreach( var service in RuntimeConfiguration.WindowsServices.Select( s => {
				var serviceController = allServices.SingleOrDefault( sc => sc.ServiceName == s.InstalledName );
				if( serviceController == null ) {
					throw new UserCorrectableException( "The \"" + s.InstalledName +
					                                    "\" service could not be found. Re-install the services for the installation to correct this error." );
				}
				return serviceController;
			} ) ) {
				try {
					service.Start();
				}
				catch( InvalidOperationException e ) {
					const string message = "Failed to start service.";

					// We have seen this happen when an exception was thrown while initializing global logic for the system.
					if( e.InnerException is Win32Exception &&
					    e.InnerException.Message.Contains( "The service did not respond to the start or control request in a timely fashion" ) )
						throw new UserCorrectableException( message, e );

					throw new ApplicationException( message, e );
				}
				service.WaitForStatusWithTimeOut( ServiceControllerStatus.Running );
			}
			if( runtimeConfiguration.InstallationType != InstallationType.Development ) {
				foreach( var site in runtimeConfiguration.WebSiteNames )
					startWebSite( site );
			}
		}

		public void InstallServices() {
			foreach( var service in runtimeConfiguration.WindowsServices ) {
				if( ServiceController.GetServices().Any( sc => sc.ServiceName == service.InstalledName ) )
					throw new UserCorrectableException( "A service could not be installed because one with the same name already exists." );
				runInstallutil( service, false );
			}
		}

		private void runInstallutil( WindowsService service, bool uninstall ) {
			// NOTE: Eliminate this constant and its use when we switch ISU over to .NET 4.
			const string dotNet4RuntimeFolderPath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319";

			StandardLibraryMethods.RunProgram(
				StandardLibraryMethods.CombinePaths( Directory.Exists( dotNet4RuntimeFolderPath ) ? dotNet4RuntimeFolderPath : RuntimeEnvironment.GetRuntimeDirectory(),
				                                     "installutil" ),
				( uninstall ? "/u " : "" ) + "\"" +
				StandardLibraryMethods.CombinePaths( GetWindowsServiceFolderPath( service, true ), service.NamespaceAndAssemblyName + ".exe"
					/* file extension is required */ ) + "\"",
				"",
				true );
		}

		public string GetWindowsServiceFolderPath( WindowsService service, bool useDebugFolderIfDevelopmentInstallation ) {
			var path = StandardLibraryMethods.CombinePaths( generalInstallationLogic.Path, service.Name );
			if( runtimeConfiguration.InstallationType == InstallationType.Development )
				path = StandardLibraryMethods.CombinePaths( path, StandardLibraryMethods.GetProjectOutputFolderPath( useDebugFolderIfDevelopmentInstallation ) );
			return path;
		}

		private static readonly string appCmdPath = StandardLibraryMethods.CombinePaths( Environment.GetEnvironmentVariable( "windir" ),
		                                                                                 @"system32\inetsrv\AppCmd.exe" );

		// NOTE: We do have the power to add and remove web sites here, and we can list just the stopped or just the started sites.
		// NOTE: When we add web sites with the ISU, we should NOT support host headers since WCF services have some restrictions with these. See http://stackoverflow.com/questions/561823/wcf-error-this-collection-already-contains-an-address-with-scheme-http
		private static bool siteExistsInIis( string webSiteName ) {
			if( !File.Exists( appCmdPath ) )
				return false;
			return StandardLibraryMethods.RunProgram( appCmdPath, "list sites", "", true ).Contains( "\"" + webSiteName + "\"" );
		}

		private static void stopWebSite( string webSiteName ) {
			if( siteExistsInIis( webSiteName ) )
				StandardLibraryMethods.RunProgram( appCmdPath, "Stop Site \"" + webSiteName + "\"", "", true );
		}

		private static void startWebSite( string webSiteName ) {
			if( siteExistsInIis( webSiteName ) )
				StandardLibraryMethods.RunProgram( appCmdPath, "Start Site \"" + webSiteName + "\"", "", true );
		}
	}
}