using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary.Configuration.InstallationStandard;
using RedStapler.StandardLibrary.Configuration.SystemGeneral;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.Configuration {
	/// <summary>
	/// The elements of installation configuration that the standard library understands.
	/// </summary>
	public class InstallationConfiguration {
		/// <summary>
		/// Red Stapler Information System use only.
		/// </summary>
		public const string ConfigurationFolderName = "Configuration";

		/// <summary>
		/// Red Stapler Information System use only.
		/// </summary>
		public const string InstallationConfigurationFolderName = "Installation";

		/// <summary>
		/// Red Stapler Information System use only.
		/// </summary>
		public const string InstallationsFolderName = "Installations";

		/// <summary>
		/// Red Stapler Information System use only.
		/// </summary>
		public const string DevelopmentInstallationFolderName = "Development";

		/// <summary>
		/// Red Stapler Information System use only.
		/// </summary>
		public const string InstallationStandardConfigurationFileName = "Standard.xml";

		/// <summary>
		/// Returns true if an installed installation exists at the specified path.
		/// </summary> 
		public static bool InstalledInstallationExists( string installationPath ) {
			// Consider this installation "installed" if a Configuration folder exists at the root of the installation folder.
			return Directory.Exists( StandardLibraryMethods.CombinePaths( installationPath, ConfigurationFolderName ) );
		}

		/// <summary>
		/// Gets the full name of the given system and installation.  For example, 'Red Stapler Information System - Live'.
		/// </summary>
		public static string GetFullNameFromSystemAndInstallationNames( string systemName, string installationName ) {
			return systemName + " - " + installationName;
		}

		/// <summary>
		/// Gets the full short name of the given system and installation.  For example, 'RsisLive'.
		/// </summary>
		public static string GetFullShortNameFromSystemAndInstallationNames( string systemShortName, string installationShortName ) {
			return systemShortName + installationShortName;
		}

		private readonly string installationPath;
		private readonly string configurationFolderPath;
		private readonly SystemGeneralConfiguration systemGeneralConfiguration;
		private readonly InstallationStandardConfiguration installationStandardConfiguration;
		private readonly string installationCustomConfigurationFilePath;
		private readonly bool isDevelopmentInstallation;

		/// <summary>
		/// Creates a new installation configuration.
		/// </summary>
		public InstallationConfiguration( string installationPath, bool isDevelopmentInstallation ) {
			this.installationPath = installationPath;

			// The EWL configuration folder is not inside any particular app's folder the way that Web.config and app.config are. This is for two reasons. First, EWL
			// configuration is system-wide (technically installation-wide) and not app-specific like Web.config and app.config. Second, it could be disastrous to
			// have EWL configuration files inside a web app's folder since then these files, which often contain database passwords and other sensitive information,
			// could potentially be served up to users.
			configurationFolderPath =
				StandardLibraryMethods.CombinePaths( InstallationFileStatics.GetGeneralFilesFolderPath( installationPath, isDevelopmentInstallation ),
				                                     ConfigurationFolderName );


			// Do not perform schema validation for non-development installations because the schema files won't be available on non-development machines. For
			// development installations, also do not perform schema validation since the schema files on disk may not match this version of the Standard Library.
			// This can happen, for example, when you are trying to run a system using the released or last built version of the library at the same time that you
			// are making changes to one of the schema files.
			//
			// Another reason to not perform schema validation for development installations is that we may create sample solutions and send them to tech support
			// people for troubleshooting. These people may not have the Standard Library solution in the right location on their machines, or they may not have it at
			// all. In either of these cases we would not have access to the schema files.

			// system general configuration
			var systemGeneralConfigurationFilePath = StandardLibraryMethods.CombinePaths( ConfigurationFolderPath, "General.xml" );
			systemGeneralConfiguration = XmlOps.DeserializeFromFile<SystemGeneralConfiguration>( systemGeneralConfigurationFilePath, false );

			var installationConfigurationFolderPath = isDevelopmentInstallation
				                                          ? StandardLibraryMethods.CombinePaths( ConfigurationFolderPath,
				                                                                                 InstallationConfigurationFolderName,
				                                                                                 InstallationsFolderName,
				                                                                                 DevelopmentInstallationFolderName )
				                                          : StandardLibraryMethods.CombinePaths( ConfigurationFolderPath, InstallationConfigurationFolderName );

			// installation standard configuration
			var installationStandardConfigurationFilePath = StandardLibraryMethods.CombinePaths( installationConfigurationFolderPath,
			                                                                                     InstallationStandardConfigurationFileName );
			installationStandardConfiguration = XmlOps.DeserializeFromFile<InstallationStandardConfiguration>( installationStandardConfigurationFilePath, false );


			// installation custom configuration
			installationCustomConfigurationFilePath = StandardLibraryMethods.CombinePaths( installationConfigurationFolderPath, "Custom.xml" );

			this.isDevelopmentInstallation = isDevelopmentInstallation;
		}

		/// <summary>
		/// Gets the name of the system.
		/// </summary>
		public string SystemName { get { return systemGeneralConfiguration.systemName; } }

		/// <summary>
		/// Gets the short name of the system.
		/// </summary>
		public string SystemShortName { get { return systemGeneralConfiguration.systemShortName; } }

		/// <summary>
		/// Gets the full name of this installation.  For example, 'Red Stapler Information System - Live'.
		/// </summary>
		public string FullName { get { return GetFullNameFromSystemAndInstallationNames( SystemName, InstallationName ); } }

		/// <summary>
		/// Gets the full short name of this installation.  For example, 'RsisLive'.
		/// </summary>
		public string FullShortName { get { return GetFullShortNameFromSystemAndInstallationNames( SystemShortName, InstallationShortName ); } }

		/// <summary>
		/// Gets the email default from name.
		/// </summary>
		public string EmailDefaultFromName {
			get {
				return installationStandardConfiguration.EmailDefaultFromNameAndAddressOverride != null
					       ? installationStandardConfiguration.EmailDefaultFromNameAndAddressOverride.Name
					       : AppTools.SystemProvider.EmailDefaultFromName;
			}
		}

		/// <summary>
		/// Gets the email default from address.
		/// </summary>
		public string EmailDefaultFromAddress {
			get {
				return installationStandardConfiguration.EmailDefaultFromNameAndAddressOverride != null
					       ? installationStandardConfiguration.EmailDefaultFromNameAndAddressOverride.EmailAddress
					       : AppTools.SystemProvider.EmailDefaultFromAddress;
			}
		}

		/// <summary>
		/// Gets a list of the web applications in the system.
		/// </summary>
		public SystemGeneralConfigurationApplication[] WebApplications { get { return systemGeneralConfiguration.WebApplications ?? new SystemGeneralConfigurationApplication[ 0 ]; } }

		/// <summary>
		/// Gets a list of the services in the system.
		/// </summary>
		public IEnumerable<WindowsService> WindowsServices {
			get {
				return systemGeneralConfiguration.WindowsServices != null
					       ? systemGeneralConfiguration.WindowsServices.Select( ws => new WindowsService( ws, FullShortName ) )
					       : new WindowsService[ 0 ];
			}
		}

		/// <summary>
		/// Gets a list of the developers for the system.
		/// </summary>
		public List<NameAndEmailAddress> Developers { get { return new List<NameAndEmailAddress>( systemGeneralConfiguration.developers ); } }

		/// <summary>
		/// Gets a list of SQL commands to be run against the database after an UpdateData operation on a non-live installation.
		/// </summary>
		public string[] PrimaryDatabaseLiveToIntermediateConversionCommands { get { return systemGeneralConfiguration.PrimaryDatabaseLiveToIntermediateConversionCommands ?? new string[ 0 ]; } }

		/// <summary>
		/// Gets the RSIS installation ID for the installation.
		/// </summary>
		public int? RsisInstallationId { get { return installationStandardConfiguration.rsisInstallationIdSpecified ? installationStandardConfiguration.rsisInstallationId as int? : null; } }

		/// <summary>
		/// Gets the name of the installation.
		/// </summary>
		public string InstallationName { get { return isDevelopmentInstallation ? "Development" : installationStandardConfiguration.installedInstallation.name; } }

		/// <summary>
		/// Gets the short name of the installation.
		/// </summary>
		public string InstallationShortName { get { return isDevelopmentInstallation ? "Dev" : installationStandardConfiguration.installedInstallation.shortName; } }

		/// <summary>
		/// Gets the type of the installation.
		/// </summary>
		public InstallationType InstallationType {
			get {
				if( isDevelopmentInstallation )
					return InstallationType.Development;
				bool isLive;
				if( installationStandardConfiguration.installedInstallation.typeIdSpecified )
					isLive = installationStandardConfiguration.installedInstallation.typeId == 0;
				else
					isLive = installationStandardConfiguration.installedInstallation.IsLiveInstallation;
				return isLive ? InstallationType.Live : InstallationType.Intermediate;
			}
		}

		internal string SmtpServer { get { return installationStandardConfiguration.smtpServer ?? ""; } }

		internal string BaseUrlOverride { get { return installationStandardConfiguration.BaseUrlOverride ?? ""; } }

		internal bool DisableNonPreferredDomainChecking { get { return installationStandardConfiguration.DisableNonPreferredDomainChecking; } }

		internal string CertificateEmailAddressOverride { get { return installationStandardConfiguration.CertificateEmailAddressOverride ?? ""; } }

		/// <summary>
		/// Gets a list of the administrators for the installation.
		/// </summary>
		public List<InstallationStandardNameAndEmailAddress> Administrators { get { return new List<InstallationStandardNameAndEmailAddress>( installationStandardConfiguration.administrators ); } }

		/// <summary>
		/// Gets a database information object corresponding to the primary database for this configuration. Returns null if there is no database configured.
		/// </summary>
		public DatabaseInfo PrimaryDatabaseInfo { get { return installationStandardConfiguration.database != null ? getDatabaseInfo( "", installationStandardConfiguration.database ) : null; } }

		/// <summary>
		/// Gets a database information object corresponding to the secondary database for this configuration with the specified name.
		/// </summary>
		public DatabaseInfo GetSecondaryDatabaseInfo( string name ) {
			foreach( var secondaryDatabase in installationStandardConfiguration.SecondaryDatabases ) {
				if( secondaryDatabase.Name == name )
					return getDatabaseInfo( secondaryDatabase.Name, secondaryDatabase.Database );
			}
			throw new ApplicationException( "No secondary database exists with the specified name." );
		}

		private DatabaseInfo getDatabaseInfo( string secondaryDatabaseName, Database database ) {
			if( database is SqlServerDatabase ) {
				var sqlServerDatabase = database as SqlServerDatabase;
				return new SqlServerInfo( secondaryDatabaseName,
				                          sqlServerDatabase.server,
				                          sqlServerDatabase.SqlServerAuthenticationLogin != null ? sqlServerDatabase.SqlServerAuthenticationLogin.LoginName : null,
				                          sqlServerDatabase.SqlServerAuthenticationLogin != null ? sqlServerDatabase.SqlServerAuthenticationLogin.Password : null,
				                          sqlServerDatabase.database ?? FullShortName,
				                          true,
				                          sqlServerDatabase.FullTextCatalog );
			}
			if( database is MySqlDatabase ) {
				var mySqlDatabase = database as MySqlDatabase;
				return new MySqlInfo( secondaryDatabaseName, mySqlDatabase.database ?? FullShortName.CamelToEnglish().EnglishToOracle(), true );
			}
			if( database is OracleDatabase ) {
				var oracleDatabase = database as OracleDatabase;
				return new OracleInfo( secondaryDatabaseName,
				                       oracleDatabase.tnsName,
				                       oracleDatabase.userAndSchema,
				                       oracleDatabase.password,
				                       !oracleDatabase.SupportsConnectionPoolingSpecified || oracleDatabase.SupportsConnectionPooling,
				                       !oracleDatabase.SupportsLinguisticIndexesSpecified || oracleDatabase.SupportsLinguisticIndexes );
			}
			throw new ApplicationException( "Unknown database type." );
		}

		/// <summary>
		/// Only applicable for installed installations.  Returns a list of web sites associated with this installed installation.  This is
		/// used for things like knowing which web sites to stop when upgrading.
		/// </summary>
		public string[] WebSiteNames {
			get {
				return ( isDevelopmentInstallation || installationStandardConfiguration.installedInstallation.webSiteNames == null )
					       ? new string[ 0 ]
					       : installationStandardConfiguration.installedInstallation.webSiteNames;
			}
		}

		internal string InstallationPath { get { return installationPath; } }

		/// <summary>
		/// Gets the path of the configuration folder.
		/// </summary>
		public string ConfigurationFolderPath { get { return configurationFolderPath; } }

		internal string InstallationCustomConfigurationFilePath { get { return installationCustomConfigurationFilePath; } }

		/// <summary>
		/// The file path for the error log file for this installation. ("Error Log.txt" in the root of the installation folder).
		/// </summary>
		public string ErrorLogFilePath { get { return StandardLibraryMethods.CombinePaths( installationPath, "Error Log.txt" ); } }
	}
}