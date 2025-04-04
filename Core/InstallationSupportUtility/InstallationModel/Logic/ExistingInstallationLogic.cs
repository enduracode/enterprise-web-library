using System.ComponentModel;
using System.ServiceProcess;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

[ PublicAPI ]
public class ExistingInstallationLogic {
	public const string SystemDatabaseUpdatesFileName = "Database Updates.sql";
	private const int serviceFailureResetPeriod = 3600; // seconds

	private readonly GeneralInstallationLogic generalInstallationLogic;
	private readonly InstallationConfiguration runtimeConfiguration;
	private readonly Database database;

	public ExistingInstallationLogic( GeneralInstallationLogic generalInstallationLogic, InstallationConfiguration runtimeConfiguration ) {
		this.generalInstallationLogic = generalInstallationLogic;
		this.runtimeConfiguration = runtimeConfiguration;

		database = DatabaseOps.CreateDatabase( runtimeConfiguration.PrimaryDatabaseInfo );
	}

	public InstallationConfiguration RuntimeConfiguration => runtimeConfiguration;

	public string GetWindowsServiceFolderPath( Configuration.SystemGeneral.WindowsService service, bool useDebugFolderIfDevelopmentInstallation ) {
		var path = EwlStatics.CombinePaths( generalInstallationLogic.Path, service.Name );
		if( runtimeConfiguration.InstallationType == InstallationType.Development )
			path = EwlStatics.CombinePaths(
				path,
				ConfigurationStatics.GetProjectOutputFolderPath( useDebugFolderIfDevelopmentInstallation, runtimeIdentifier: "win-x64" ) );
		return path;
	}

	public Database Database => database;

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

	public void UninstallServices() {
		stopServices();

		var allServices = ServiceController.GetServices();
		foreach( var service in runtimeConfiguration.WindowsServices.Where( s => allServices.Any( sc => sc.ServiceName == s.InstalledName ) ) )
			TewlContrib.ProcessTools.RunProgram( "sc", "delete \"{0}\"".FormatWith( service.InstalledName ), "", true );
	}

	/// <summary>
	/// Starts all web applications and services associated with this installation.
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
		if( runtimeConfiguration.InstallationType != InstallationType.Development )
			foreach( var iisAppPoolName in runtimeConfiguration.WebApplications.Select( i => i.IisAppPoolAndSiteName! ).Where( i => i.Length > 0 ) )
				IsuStatics.StartIisAppPool( iisAppPoolName );
	}

	/// <summary>
	/// Stops all web applications and services associated with this installation.
	/// </summary>
	public void Stop( bool stopServices ) {
		if( runtimeConfiguration.InstallationType != InstallationType.Development )
			foreach( var iisAppPoolName in runtimeConfiguration.WebApplications.Select( i => i.IisAppPoolAndSiteName! ).Where( i => i.Length > 0 ) )
				IsuStatics.StopIisAppPool( iisAppPoolName );
		if( stopServices )
			this.stopServices();
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

	public string MigrateData() {
		var databaseUpdateFilePath = EwlStatics.CombinePaths( runtimeConfiguration.ConfigurationFolderPath, SystemDatabaseUpdatesFileName );
		var linesInScriptOnHd = getNumberOfLinesInDatabaseScript( databaseUpdateFilePath );

		// We don't want to ask the database for the line number if there is no script.
		if( linesInScriptOnHd is not null ) {
			int lineMarker;
			try {
				lineMarker = database.GetLineMarker();
			}
			catch( Exception e ) {
				const string message = "Failed to get line marker.";
				if( runtimeConfiguration.InstallationType == InstallationType.Development )
					throw new UserCorrectableException( message, e );
				throw UserCorrectableException.CreateSecondaryException( message, e );
			}

			// We don't want to execute blank scripts against the database because this will cause an error with read-only databases.
			if( lineMarker != linesInScriptOnHd ) {
				using( var sw = new StringWriter() ) {
					// If the string writer's value is not the empty string, it will end with the line terminator string.
					using( var tr = new StreamReader( File.OpenRead( databaseUpdateFilePath ) ) ) {
						// Read and discard all text before the marker line.
						for( var i = 0; i < lineMarker; i++ )
							tr.ReadLine();

						// Store all text on and after the marker line and move the marker to the end of the file.
						for( string? lineText; ( lineText = tr.ReadLine() ) != null; lineMarker += 1 )
							sw.WriteLine( lineText );
					}

					try {
						database.ExecuteSqlScriptInTransaction( sw.ToString() );
					}
					catch( Exception e ) {
						const string message = "Failed to update database logic.";
						if( runtimeConfiguration.InstallationType == InstallationType.Development )
							throw new UserCorrectableException( message, e );
						throw UserCorrectableException.CreateSecondaryException( message, e );
					}
				}
				database.UpdateLineMarker( lineMarker );
			}
		}

		var output = "";
		var migratorExists = runtimeConfiguration.InstallationType == InstallationType.Development
			                     ? File.Exists(
				                     EwlStatics.CombinePaths(
					                     generalInstallationLogic.Path,
					                     IsuStatics.DataMigratorProjectName,
					                     $"{IsuStatics.DataMigratorProjectName}.csproj" ) )
			                     : Directory.Exists( EwlStatics.CombinePaths( generalInstallationLogic.Path, IsuStatics.DataMigratorProjectName ) );
		if( migratorExists ) {
			if( runtimeConfiguration.InstallationType == InstallationType.Development )
				try {
					TewlContrib.ProcessTools.RunProgram(
						"dotnet",
						"build \"{0}\" --configuration {1}".FormatWith(
							EwlStatics.CombinePaths( generalInstallationLogic.Path, IsuStatics.DataMigratorProjectName ),
							"Debug" ),
						"",
						true );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( $"Failed to build {IsuStatics.DataMigratorProjectName}.", e );
				}

			try {
				output = TewlContrib.ProcessTools.RunProgram(
						EwlStatics.CombinePaths(
							generalInstallationLogic.Path,
							IsuStatics.DataMigratorProjectName,
							runtimeConfiguration.InstallationType == InstallationType.Development
								? ConfigurationStatics.GetProjectOutputFolderPath( true, runtimeIdentifier: "win-x64" )
								: "",
							IsuStatics.DataMigratorNamespaceAndAssemblyName ),
						"",
						"",
						true )
					.TrimEnd();
			}
			catch( Exception e ) {
				const string message = "Failed to migrate data.";
				if( runtimeConfiguration.InstallationType == InstallationType.Development )
					throw new UserCorrectableException( message, e );
				throw UserCorrectableException.CreateSecondaryException( message, e );
			}
		}
		return output;
	}

	/// <summary>
	/// Returns null if no database script exists on the hard drive.
	/// </summary>
	private int? getNumberOfLinesInDatabaseScript( string databaseUpdateFilePath ) {
		if( !File.Exists( databaseUpdateFilePath ) )
			return null;

		var lines = 0;
		using var reader = new StreamReader( File.OpenRead( databaseUpdateFilePath ) );
		while( reader.ReadLine() != null )
			lines++;
		return lines;
	}
}