using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using Humanizer;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.Configuration.SystemGeneral;
using RedStapler.StandardLibrary.Email;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class ExistingInstallationLogic {
		public const string SystemDatabaseUpdatesFileName = "Database Updates.sql";
		private const int serviceFailureResetPeriod = 3600; // seconds

		private static readonly string appCmdPath = StandardLibraryMethods.CombinePaths(
			Environment.GetEnvironmentVariable( "windir" ),
			@"system32\inetsrv\AppCmd.exe" );

		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly InstallationConfiguration runtimeConfiguration;

		public ExistingInstallationLogic( GeneralInstallationLogic generalInstallationLogic, InstallationConfiguration runtimeConfiguration ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.runtimeConfiguration = runtimeConfiguration;
		}

		public InstallationConfiguration RuntimeConfiguration { get { return runtimeConfiguration; } }

		public string DatabaseUpdateFilePath {
			get { return StandardLibraryMethods.CombinePaths( runtimeConfiguration.ConfigurationFolderPath, SystemDatabaseUpdatesFileName ); }
		}

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
			if( stopServices )
				this.stopServices();
		}

		public void UninstallServices() {
			// Installutil tries to stop services during uninstallation, but doesn't report failure if a service doesn't stop. That's why we stop the services
			// ourselves first.
			stopServices();

			var allServices = ServiceController.GetServices();
			foreach( var service in runtimeConfiguration.WindowsServices.Where( s => allServices.Any( sc => sc.ServiceName == s.InstalledName ) ) )
				runInstallutil( service, true );
		}

		private void stopServices() {
			var allServices = ServiceController.GetServices();
			var serviceNames = RuntimeConfiguration.WindowsServices.Select( s => s.InstalledName );
			foreach( var service in allServices.Where( sc => serviceNames.Contains( sc.ServiceName ) ) ) {
				// Clear failure actions.
				StandardLibraryMethods.RunProgram( "sc", "failure \"{0}\" reset= {1} actions= \"\"".FormatWith( service.ServiceName, serviceFailureResetPeriod ), "", true );

				if( service.Status == ServiceControllerStatus.Stopped )
					continue;
				service.Stop();
				service.WaitForStatusWithTimeOut( ServiceControllerStatus.Stopped );
			}
		}

		/// <summary>
		/// Starts all web sites and services associated with this installation.
		/// </summary>
		public void Start() {
			var allServices = ServiceController.GetServices();
			foreach( var service in RuntimeConfiguration.WindowsServices.Select(
				s => {
					var serviceController = allServices.SingleOrDefault( sc => sc.ServiceName == s.InstalledName );
					if( serviceController == null ) {
						throw new UserCorrectableException(
							"The \"" + s.InstalledName + "\" service could not be found. Re-install the services for the installation to correct this error." );
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

				// Set failure actions.
				const int restartDelay = 60000; // milliseconds
				StandardLibraryMethods.RunProgram(
					"sc",
					"failure \"{0}\" reset= {1} actions= restart/{2}".FormatWith( service.ServiceName, serviceFailureResetPeriod, restartDelay ),
					"",
					true );
				StandardLibraryMethods.RunProgram( "sc", "failureflag \"{0}\" 1".FormatWith( service.ServiceName ), "", true );
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
			try {
				StandardLibraryMethods.RunProgram(
					StandardLibraryMethods.CombinePaths( RuntimeEnvironment.GetRuntimeDirectory(), "installutil" ),
					( uninstall ? "/u " : "" ) + "\"" +
					StandardLibraryMethods.CombinePaths(
						GetWindowsServiceFolderPath( service, true ),
						service.NamespaceAndAssemblyName + ".exe"
						/* file extension is required */ ) + "\"",
					"",
					true );
			}
			catch( Exception e ) {
				const string message = "Installer tool failed.";
				if( e.Message.Contains( typeof( EmailSendingException ).Name ) )
					throw new UserCorrectableException( message, e );
				throw new ApplicationException( message, e );
			}
		}

		public string GetWindowsServiceFolderPath( WindowsService service, bool useDebugFolderIfDevelopmentInstallation ) {
			var path = StandardLibraryMethods.CombinePaths( generalInstallationLogic.Path, service.Name );
			if( runtimeConfiguration.InstallationType == InstallationType.Development )
				path = StandardLibraryMethods.CombinePaths( path, StandardLibraryMethods.GetProjectOutputFolderPath( useDebugFolderIfDevelopmentInstallation ) );
			return path;
		}

		// NOTE: We do have the power to add and remove web sites here, and we can list just the stopped or just the started sites.
		// NOTE: When we add web sites with the ISU, we should NOT support host headers since WCF services have some restrictions with these. See http://stackoverflow.com/questions/561823/wcf-error-this-collection-already-contains-an-address-with-scheme-http
		private bool siteExistsInIis( string webSiteName ) {
			if( !File.Exists( appCmdPath ) )
				return false;
			return StandardLibraryMethods.RunProgram( appCmdPath, "list sites", "", true ).Contains( "\"" + webSiteName + "\"" );
		}

		private void stopWebSite( string webSiteName ) {
			if( siteExistsInIis( webSiteName ) )
				StandardLibraryMethods.RunProgram( appCmdPath, "Stop Site \"" + webSiteName + "\"", "", true );
		}

		private void startWebSite( string webSiteName ) {
			if( siteExistsInIis( webSiteName ) )
				StandardLibraryMethods.RunProgram( appCmdPath, "Start Site \"" + webSiteName + "\"", "", true );
		}
	}
}