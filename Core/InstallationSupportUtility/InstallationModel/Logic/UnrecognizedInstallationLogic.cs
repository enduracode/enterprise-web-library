using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;

public class UnrecognizedInstallationLogic {
	public IReadOnlyCollection<Database> AllSecondaryDatabases { get; }

	public UnrecognizedInstallationLogic( ExistingInstallationLogic existingInstallationLogic ) {
		AllSecondaryDatabases = existingInstallationLogic.RuntimeConfiguration.SecondaryDatabaseNames.Select( i =>
				DatabaseOps.CreateDatabase( existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( i ) ) )
			.Materialize();
	}
}