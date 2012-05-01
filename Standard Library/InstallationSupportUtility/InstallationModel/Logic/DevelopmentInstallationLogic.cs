using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.Configuration.SystemDevelopment;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	public class DevelopmentInstallationLogic {
		public const string SystemDevelopmentConfigurationFileName = "Development.xml";

		private readonly GeneralInstallationLogic generalInstallationLogic;
		private readonly ExistingInstallationLogic existingInstallationLogic;
		private readonly RecognizedInstallationLogic recognizedInstallationLogic;
		private readonly SystemDevelopmentConfiguration developmentConfiguration;
		private readonly List<DatabaseAbstraction.Database> databasesForCodeGeneration;

		public DevelopmentInstallationLogic( GeneralInstallationLogic generalInstallationLogic, ExistingInstallationLogic existingInstallationLogic,
		                                     RecognizedInstallationLogic recognizedInstallationLogic ) {
			this.generalInstallationLogic = generalInstallationLogic;
			this.existingInstallationLogic = existingInstallationLogic;
			this.recognizedInstallationLogic = recognizedInstallationLogic;

			// Do not perform schema validation since the schema file on disk may not match this version of the ISU. This can happen, for example, when you run the
			// released version of the ISU at the same time that you are making changes to the schema file.
			developmentConfiguration =
				XmlOps.DeserializeFromFile<SystemDevelopmentConfiguration>(
					StandardLibraryMethods.CombinePaths( this.existingInstallationLogic.RuntimeConfiguration.ConfigurationFolderPath, SystemDevelopmentConfigurationFileName ),
					false );

			databasesForCodeGeneration = new List<DatabaseAbstraction.Database>();
			if( developmentConfiguration.database != null )
				DatabasesForCodeGeneration.Add( this.recognizedInstallationLogic.Database );
			if( developmentConfiguration.secondaryDatabases != null ) {
				foreach( var secondaryDatabaseInDevelopmentConfiguration in developmentConfiguration.secondaryDatabases ) {
					DatabasesForCodeGeneration.Add(
						this.recognizedInstallationLogic.SecondaryDatabasesIncludedInDataPackages.SingleOrDefault(
							sd => sd.SecondaryDatabaseName == secondaryDatabaseInDevelopmentConfiguration.name ) ??
						DatabaseAbstraction.DatabaseOps.CreateDatabase(
							this.existingInstallationLogic.RuntimeConfiguration.GetSecondaryDatabaseInfo( secondaryDatabaseInDevelopmentConfiguration.name ), new List<string>() ) );
				}
			}
		}

		public SystemDevelopmentConfiguration DevelopmentConfiguration { get { return developmentConfiguration; } }

		public string LibraryPath { get { return StandardLibraryMethods.CombinePaths( generalInstallationLogic.Path, "Library" ); } }

		public List<DatabaseAbstraction.Database> DatabasesForCodeGeneration { get { return databasesForCodeGeneration; } }
	}
}