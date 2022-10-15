using System.ComponentModel;
using System.ServiceProcess;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using Humanizer;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class ExistingInstallationLogic {
		public const string SystemDatabaseUpdatesFileName = "Database Updates.sql";
		private const int serviceFailureResetPeriod = 3600; // seconds

		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly InstallationConfiguration runtimeConfiguration;
		private readonly DatabaseAbstraction.Database database;

		public ExistingInstallationLogic( GeneralInstallationLogic generalInstallationLogic, InstallationConfiguration runtimeConfiguration ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.runtimeConfiguration = runtimeConfiguration;

			database = DatabaseAbstraction.DatabaseOps.CreateDatabase( runtimeConfiguration.PrimaryDatabaseInfo );
		}

		public InstallationConfiguration RuntimeConfiguration => runtimeConfiguration;

		public DatabaseAbstraction.Database Database => database;

		public string DatabaseUpdateFilePath => EwlStatics.CombinePaths( runtimeConfiguration.ConfigurationFolderPath, SystemDatabaseUpdatesFileName );

		/// <summary>
		/// Stops all web sites and services associated with this installation.
		/// </summary>
		public void Stop( bool stopServices ) {
			if( runtimeConfiguration.WebApplications.Any( i => i.IisApplication != null ) && runtimeConfiguration.InstallationType != InstallationType.Development )
				IsuStatics.StopIisAppPool( IisAppPoolName );
			if( stopServices )
				this.stopServices();
		}

		public void UninstallServices() {
			stopServices();

			var allServices = ServiceController.GetServices();
			foreach( var service in runtimeConfiguration.WindowsServices.Where( s => allServices.Any( sc => sc.ServiceName == s.InstalledName ) ) )
				TewlContrib.ProcessTools.RunProgram( "sc", "delete \"{0}\"".FormatWith( service.InstalledName ), "", true );
		}

		private void stopServices() {
			var allServices = ServiceController.GetServices();
			var serviceNames = RuntimeConfiguration.WindowsServices.Select( s => s.InstalledName );
			foreach( var service in allServices.Where( sc => serviceNames.Contains( sc.ServiceName ) ) ) {
				TewlContrib.ProcessTools.RunProgram( "sc", "config \"{0}\" start= demand".FormatWith( service.ServiceName ), "", true );

				// Clear failure actions.
				TewlContrib.ProcessTools.RunProgram(
					"sc",
					"failure \"{0}\" reset= {1} actions= \"\"".FormatWith( service.ServiceName, serviceFailureResetPeriod ),
					"",
					true );

				if( service.Status == ServiceControllerStatus.Stopped )
					continue;
				if( service.Status != ServiceControllerStatus.StopPending ) {
					if( service.Status == ServiceControllerStatus.StartPending )
						service.WaitForStatusWithTimeOut( ServiceControllerStatus.Running );
					service.Stop();
				}
				service.WaitForStatusWithTimeOut( ServiceControllerStatus.Stopped );
			}
		}

		/// <summary>
		/// Starts all web sites and services associated with this installation.
		/// </summary>
		public void Start() {
			var allServices = ServiceController.GetServices();
			foreach( var service in RuntimeConfiguration.WindowsServices ) {
				var serviceController = allServices.SingleOrDefault( sc => sc.ServiceName == service.InstalledName );
				if( serviceController == null ) {
					TelemetryStatics.ReportFault(
						"Failed to start the \"{0}\" service because it is missing. Re-install the services for the installation to correct this error.".FormatWith(
							service.InstalledName ) );
					continue;
				}

				try {
					serviceController.Start();
				}
				catch( InvalidOperationException e ) {
					const string message = "Failed to start service.";

					// We have seen this happen when an exception was thrown while initializing global logic for the system.
					if( e.InnerException is Win32Exception &&
					    e.InnerException.Message.Contains( "The service did not respond to the start or control request in a timely fashion" ) )
						throw new UserCorrectableException( message, e );

					throw new ApplicationException( message, e );
				}
				serviceController.WaitForStatusWithTimeOut( ServiceControllerStatus.Running );

				TewlContrib.ProcessTools.RunProgram( "sc", "config \"{0}\" start= delayed-auto".FormatWith( serviceController.ServiceName ), "", true );

				// Set failure actions.
				const int restartDelay = 60000; // milliseconds
				TewlContrib.ProcessTools.RunProgram(
					"sc",
					"failure \"{0}\" reset= {1} actions= restart/{2}".FormatWith( serviceController.ServiceName, serviceFailureResetPeriod, restartDelay ),
					"",
					true );
				TewlContrib.ProcessTools.RunProgram( "sc", "failureflag \"{0}\" 1".FormatWith( serviceController.ServiceName ), "", true );
			}
			if( runtimeConfiguration.WebApplications.Any( i => i.IisApplication != null ) && runtimeConfiguration.InstallationType != InstallationType.Development )
				IsuStatics.StartIisAppPool( IisAppPoolName );
		}

		public void InstallServices() {
			foreach( var service in runtimeConfiguration.WindowsServices ) {
				if( ServiceController.GetServices().Any( sc => sc.ServiceName == service.InstalledName ) )
					throw new UserCorrectableException( "A service could not be installed because one with the same name already exists." );
				TewlContrib.ProcessTools.RunProgram(
					"sc",
					"create \"{0}\" binpath= \"{1}\" obj= \"NT AUTHORITY\\NetworkService\"".FormatWith(
						service.InstalledName,
						EwlStatics.CombinePaths( GetWindowsServiceFolderPath( service, true ), service.NamespaceAndAssemblyName + ".exe" ) ),
					"",
					true );
				TewlContrib.ProcessTools.RunProgram(
					"sc",
					"description \"{0}\" \"{1}\"".FormatWith( service.InstalledName, "Performs actions for {0}.".FormatWith( runtimeConfiguration.SystemName ) ),
					"",
					true );
			}
		}

		internal string IisAppPoolName => runtimeConfiguration.FullShortName;

		public string GetWindowsServiceFolderPath( WindowsService service, bool useDebugFolderIfDevelopmentInstallation ) {
			var path = EwlStatics.CombinePaths( generalInstallationLogic.Path, service.Name );
			if( runtimeConfiguration.InstallationType == InstallationType.Development )
				path = EwlStatics.CombinePaths(
					path,
					ConfigurationStatics.GetProjectOutputFolderPath( useDebugFolderIfDevelopmentInstallation, runtimeIdentifier: "win10-x64" ) );
			return path;
		}
	}
}