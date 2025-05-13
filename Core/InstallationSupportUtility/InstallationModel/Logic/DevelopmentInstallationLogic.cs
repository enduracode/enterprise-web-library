using EnterpriseWebLibrary.Configuration.SystemDevelopment;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class DevelopmentInstallationLogic {
	private readonly GeneralInstallationLogic generalInstallationLogic;
	private readonly ExistingInstallationLogic existingInstallationLogic;
	private readonly List<DatabaseAbstraction.Database> databasesForCodeGeneration;

	public DevelopmentInstallationLogic(
		GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		RecognizedInstallationLogic? recognizedInstallationLogic ) {
		this.generalInstallationLogic = generalInstallationLogic;
		this.existingInstallationLogic = existingInstallationLogic;

		var developmentConfiguration = existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration!;
		databasesForCodeGeneration = [ ];
		if( developmentConfiguration.database != null )
			databasesForCodeGeneration.Add( existingInstallationLogic.Database );
		if( developmentConfiguration.secondaryDatabases != null )
			foreach( var secondaryDatabaseInDevelopmentConfiguration in developmentConfiguration.secondaryDatabases )
				databasesForCodeGeneration.Add(
					recognizedInstallationLogic?.SecondaryDatabasesIncludedInDataPackages.SingleOrDefault(
						sd => sd.SecondaryDatabaseName == secondaryDatabaseInDevelopmentConfiguration.name ) ?? DatabaseAbstraction.DatabaseOps.CreateDatabase(
						this.existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( secondaryDatabaseInDevelopmentConfiguration.name ) ) );
	}

	public SystemDevelopmentConfiguration DevelopmentConfiguration => existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration!;

	public string LibraryPath => EwlStatics.CombinePaths( generalInstallationLogic.Path, "Library" );

	public IReadOnlyCollection<DatabaseAbstraction.Database> DatabasesForCodeGeneration => databasesForCodeGeneration;

	public bool SystemIsEwl => existingInstallationLogic.RuntimeConfiguration.SystemIsEwl;
}