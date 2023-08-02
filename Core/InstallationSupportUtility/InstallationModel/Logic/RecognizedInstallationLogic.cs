namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class RecognizedInstallationLogic {
	private readonly ExistingInstallationLogic existingInstallationLogic;
	private readonly KnownSystemLogic knownSystemLogic;
	private readonly List<DatabaseAbstraction.Database> secondaryDatabasesIncludedInDataPackages;

	public RecognizedInstallationLogic( ExistingInstallationLogic existingInstallationLogic, KnownSystemLogic knownSystemLogic ) {
		this.existingInstallationLogic = existingInstallationLogic;
		this.knownSystemLogic = knownSystemLogic;

		var rsisSystem = this.knownSystemLogic.RsisSystem;

		var rsisSecondaryDatabases = rsisSystem.SecondaryDatabases.Where( sd => sd.DataPackageRank.HasValue ).OrderBy( sd => sd.DataPackageRank!.Value );
		secondaryDatabasesIncludedInDataPackages = new List<DatabaseAbstraction.Database>();
		foreach( var rsisSecondaryDatabase in rsisSecondaryDatabases ) {
			var secondaryDatabaseInfo = this.existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( rsisSecondaryDatabase.Name );
			secondaryDatabasesIncludedInDataPackages.Add( DatabaseAbstraction.DatabaseOps.CreateDatabase( secondaryDatabaseInfo ) );
		}
	}

	public List<DatabaseAbstraction.Database> SecondaryDatabasesIncludedInDataPackages => secondaryDatabasesIncludedInDataPackages;
}