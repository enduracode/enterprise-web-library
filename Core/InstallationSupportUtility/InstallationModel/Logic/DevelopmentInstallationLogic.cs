using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.Configuration.SystemDevelopment;

namespace EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel {
	public class DevelopmentInstallationLogic {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly List<DatabaseAbstraction.Database> databasesForCodeGeneration;

		public DevelopmentInstallationLogic(
			GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
			RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;

			var developmentConfiguration = existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration;
			databasesForCodeGeneration = new List<DatabaseAbstraction.Database>();
			if( developmentConfiguration.database != null )
				DatabasesForCodeGeneration.Add( existingInstallationLogic.Database );
			if( developmentConfiguration.secondaryDatabases != null )
				foreach( var secondaryDatabaseInDevelopmentConfiguration in developmentConfiguration.secondaryDatabases )
					DatabasesForCodeGeneration.Add(
						( recognizedInstallationLogic != null
							  ? recognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages.SingleOrDefault(
								  sd => sd.SecondaryDatabaseName == secondaryDatabaseInDevelopmentConfiguration.name )
							  : null ) ?? DatabaseAbstraction.DatabaseOps.CreateDatabase(
							this.existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( secondaryDatabaseInDevelopmentConfiguration.name ) ) );
		}

		public SystemDevelopmentConfiguration DevelopmentConfiguration {
			get { return existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration; }
		}

		public string LibraryPath { get { return EwlStatics.CombinePaths( generalInstallationLogic.Path, "Library" ); } }

		public List<DatabaseAbstraction.Database> DatabasesForCodeGeneration { get { return databasesForCodeGeneration; } }

		public bool SystemIsEwl => existingInstallationLogic.RuntimeConfiguration.SystemIsEwl;
	}
}