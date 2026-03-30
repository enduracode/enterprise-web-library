using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Serilog;
using Tewl.IO;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations;

internal class UpdateData: Operation {
	private static readonly Operation instance = new UpdateData();
	public static Operation Instance => instance;
	private UpdateData() {}

	bool Operation.IsValid( Installation installation ) => installation is DevelopmentInstallation;

	void Operation.Execute( Installation genericInstallation, IReadOnlyList<string> arguments, OperationResult operationResult ) {
		var installation = (DevelopmentInstallation)genericInstallation;

		var sourceName = arguments[ 0 ];
		if( sourceName == "Default" )
			sourceName = "";
		var forceNewPackageDownload = bool.Parse( arguments[ 1 ] );

		DataSource? source;
		var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
		if( recognizedInstallation is not null ) {
			var sources = SystemManagerConnectionStatics.SystemList.GetDataUpdateSources( recognizedInstallation );
			if( sourceName.Any() ) {
				source = sources.SingleOrDefault( i => i.ShortName.Equals( sourceName, StringComparison.Ordinal ) ) is {} specifiedSource
					         ? new DataSource( specifiedSource )
					         : null;
				if( source is null )
					throw new UserCorrectableException( "The specified source does not exist." );
			}
			else {
				source = ( sources.FirstOrDefault( i => i.DataPackageSize.HasValue ) ?? sources.FirstOrDefault() ) is {} defaultSource
					         ? new DataSource( defaultSource )
					         : null;
				if( source is null )
					throw new UserCorrectableException( "No sources exist." );
			}
		}
		else {
			var sources = getAzureSources( installation );
			if( sourceName.Any() ) {
				source = sources.SingleOrDefault( i => i.shortName.Equals( sourceName, StringComparison.Ordinal ) ) is {} specifiedSource
					         ? new DataSource(
						         installation,
						         specifiedSource.name,
						         specifiedSource.shortName,
						         specifiedSource.AzureHosting.TenantId,
						         specifiedSource.InstallationTypeConfiguration is LiveInstallationConfiguration ? InstallationType.Live : InstallationType.Intermediate )
					         : null;
				if( source is null )
					throw new UserCorrectableException( "The specified source does not exist." );
			}
			else
				source = sources.FirstOrDefault() is {} defaultSource
					         ? new DataSource(
						         installation,
						         defaultSource.name,
						         defaultSource.shortName,
						         defaultSource.AzureHosting.TenantId,
						         defaultSource.InstallationTypeConfiguration is LiveInstallationConfiguration ? InstallationType.Live : InstallationType.Intermediate )
					         : null;
		}

		var databases = installation.ExistingInstallationLogic.Database.ToCollection()
			.Concat(
				recognizedInstallation?.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages ??
				Enumerable.Empty<InstallationSupportUtility.DatabaseAbstraction.Database>() )
			.Materialize();
		if( databases.SelectMany( i => {
			   try {
				   return DatabaseOps.GetDatabaseTables( i );
			   }
			   catch {
				   return [ ];
			   }
		   } )
		   .Any( i => i.hasModTable ) )
			Log.Information( "Cached tables exist. Please restart any running applications to prevent them from using stale data." );

		DataUpdateStatics.DownloadDataPackageAndGetDataUpdateMethod( installation, false, source, forceNewPackageDownload, operationResult )();

		foreach( var database in databases )
			DatabaseOps.ClearModificationTables( database );
	}

	private IReadOnlyCollection<InstallationStandardConfigurationInstalledInstallation> getAzureSources( DevelopmentInstallation installation ) {
		var installations = new List<InstallationStandardConfigurationInstalledInstallation>();
		foreach( var installationConfigurationFolderPath in Directory.GetDirectories(
			        EwlStatics.CombinePaths(
				        installation.ExistingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath,
				        InstallationConfiguration.InstallationConfigurationFolderName,
				        InstallationConfiguration.InstallationsFolderName ) ) ) {
			if( new[] { InstallationConfiguration.DevelopmentInstallationFolderName, AppStatics.MercurialRepositoryFolderName, AppStatics.GitRepositoryFolderName }
			   .Contains( Path.GetFileName( installationConfigurationFolderPath ) ) )
				continue;

			var installedInstallation = XmlOps.DeserializeFromFile<InstallationStandardConfiguration>(
					EwlStatics.CombinePaths( installationConfigurationFolderPath, InstallationConfiguration.InstallationStandardConfigurationFileName ),
					false )
				.installedInstallation;

			if( installedInstallation.AzureHosting is null )
				continue;

			// Development installations can only update data from intermediate installations.
			if( installedInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration )
				continue;

			installations.Add( installedInstallation );
		}
		return installations;
	}
}