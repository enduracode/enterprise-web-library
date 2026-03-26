using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
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

		var source = arguments[ 0 ];
		if( source == "Default" )
			source = "";
		var forceNewPackageDownload = bool.Parse( arguments[ 1 ] );

		RsisInstallation? sourceInstallation;
		var recognizedInstallation = installation as RecognizedDevelopmentInstallation;
		if( recognizedInstallation is not null ) {
			var sources = SystemManagerConnectionStatics.SystemList.GetDataUpdateSources( recognizedInstallation );
			if( source.Any() ) {
				sourceInstallation = sources.SingleOrDefault( i => i.ShortName == source );
				if( sourceInstallation == null )
					throw new UserCorrectableException( "The specified source does not exist." );
			}
			else {
				sourceInstallation = sources.FirstOrDefault( i => i.DataPackageSize.HasValue ) ?? sources.FirstOrDefault();
				if( sourceInstallation == null )
					throw new UserCorrectableException( "No sources exist." );
			}
		}
		else {
			if( source.Any() )
				throw new UserCorrectableException( "Source-specification is not currently supported." );
			sourceInstallation = null;
		}

		var databases = installation.ExistingInstallationLogic.Database.ToCollection()
			.Concat( recognizedInstallation?.RecognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages ?? Enumerable.Empty<Database>() )
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

		DataUpdateStatics.DownloadDataPackageAndGetDataUpdateMethod( installation, false, sourceInstallation, forceNewPackageDownload, operationResult )();

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