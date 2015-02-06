using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel {
	public class DevelopmentInstallationLogic {
		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly DatabaseAbstraction.Database database;
		private readonly List<DatabaseAbstraction.Database> databasesForCodeGeneration;

		public DevelopmentInstallationLogic(
			GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
			RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;

			database = recognizedInstallationLogic != null
				           ? recognizedInstallationLogic.Database
				           : DatabaseAbstraction.DatabaseOps.CreateDatabase( existingInstallationLogic.RuntimeConfiguration.PrimaryDatabaseInfo, new List<string>() );

			var developmentConfiguration = existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration;
			databasesForCodeGeneration = new List<DatabaseAbstraction.Database>();
			if( developmentConfiguration.database != null )
				DatabasesForCodeGeneration.Add( database );
			if( developmentConfiguration.secondaryDatabases != null ) {
				foreach( var secondaryDatabaseInDevelopmentConfiguration in developmentConfiguration.secondaryDatabases ) {
					DatabasesForCodeGeneration.Add(
						( recognizedInstallationLogic != null
							  ? recognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages.SingleOrDefault(
								  sd => sd.SecondaryDatabaseName == secondaryDatabaseInDevelopmentConfiguration.name )
							  : null ) ??
						DatabaseAbstraction.DatabaseOps.CreateDatabase(
							this.existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( secondaryDatabaseInDevelopmentConfiguration.name ),
							new List<string>() ) );
				}
			}
		}

		public SystemDevelopmentConfiguration DevelopmentConfiguration {
			get { return existingInstallationLogic.RuntimeConfiguration.SystemDevelopmentConfiguration; }
		}

		public string LibraryPath { get { return StandardLibraryMethods.CombinePaths( generalInstallationLogic.Path, "Library" ); } }

		public DatabaseAbstraction.Database Database { get { return database; } }

		public List<DatabaseAbstraction.Database> DatabasesForCodeGeneration { get { return databasesForCodeGeneration; } }

		public bool SystemIsEwl {
			get {
				var shortName = existingInstallationLogic.RuntimeConfiguration.SystemShortName;
				return shortName.StartsWith( "Ewl" ) && shortName != "EwlWebSite";
			}
		}
	}
}