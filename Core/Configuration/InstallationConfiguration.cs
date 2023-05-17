using System.Collections.Immutable;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.Configuration.SystemGeneral;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using MoreLinq;
using Tewl.IO;

namespace EnterpriseWebLibrary.Configuration;

/// <summary>
/// The elements of installation configuration that the standard library understands.
/// </summary>
public class InstallationConfiguration {
	/// <summary>
	/// Installation Support Utility and private use only.
	/// </summary>
	public const string ConfigurationFolderName = "Configuration";

	/// <summary>
	/// Development Utility use only.
	/// </summary>
	public const string SystemDevelopmentConfigurationFileName = "Development.xml";

	/// <summary>
	/// Installation Support Utility and private use only.
	/// </summary>
	public const string InstallationConfigurationFolderName = "Installation";

	/// <summary>
	/// Installation Support Utility and private use only.
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
	/// Development Utility and private use only.
	/// </summary>
	public const string InstallationSharedConfigurationFileName = "Shared" + FileExtensions.Xml;

	/// <summary>
	/// ISU and internal use only.
	/// </summary>
	public const string AsposeLicenseFolderName = "Aspose Licenses";

	/// <summary>
	/// Returns true if an installed installation exists at the specified path.
	/// </summary> 
	public static bool InstalledInstallationExists( string installationPath ) {
		// Consider this installation "installed" if a Configuration folder exists at the root of the installation folder.
		return Directory.Exists( EwlStatics.CombinePaths( installationPath, ConfigurationFolderName ) );
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
	private readonly SystemDevelopment.SystemDevelopmentConfiguration systemDevelopmentConfiguration;
	private readonly InstallationStandardConfiguration installationStandardConfiguration;
	private readonly IReadOnlyCollection<WebApplication> webApplications;
	private readonly string installationCustomConfigurationFilePath;
	private readonly string installationSharedConfigurationFilePath;

	/// <summary>
	/// Creates a new installation configuration.
	/// </summary>
	public InstallationConfiguration( string installationPath, bool isDevelopmentInstallation ) {
		this.installationPath = installationPath;

		// The EWL configuration folder is not inside any particular app's folder the way that Web.config and app.config are. This is for two reasons. First, EWL
		// configuration is system-wide (technically installation-wide) and not app-specific like Web.config and app.config. Second, it could be disastrous to
		// have EWL configuration files inside a web app's folder since then these files, which often contain database passwords and other sensitive information,
		// could potentially be served up to users.
		configurationFolderPath = EwlStatics.CombinePaths(
			InstallationFileStatics.GetGeneralFilesFolderPath( installationPath, isDevelopmentInstallation ),
			ConfigurationFolderName );


		// Do not perform schema validation for non-development installations because the schema files may not be available. For development installations, also
		// do not perform schema validation since the schema files may not match this version of the library. This can happen, for example, when you are trying to
		// run a system using an unreleased version of the library that contains schema changes.

		// system general configuration
		var systemGeneralConfigurationFilePath = EwlStatics.CombinePaths( ConfigurationFolderPath, "General.xml" );
		systemGeneralConfiguration = XmlOps.DeserializeFromFile<SystemGeneralConfiguration>( systemGeneralConfigurationFilePath, false );

		// system development configuration
		if( isDevelopmentInstallation )
			systemDevelopmentConfiguration = XmlOps.DeserializeFromFile<SystemDevelopment.SystemDevelopmentConfiguration>(
				EwlStatics.CombinePaths( configurationFolderPath, SystemDevelopmentConfigurationFileName ),
				false );

		var installationConfigurationFolderPath = isDevelopmentInstallation
			                                          ? EwlStatics.CombinePaths(
				                                          ConfigurationFolderPath,
				                                          InstallationConfigurationFolderName,
				                                          InstallationsFolderName,
				                                          DevelopmentInstallationFolderName )
			                                          : EwlStatics.CombinePaths( ConfigurationFolderPath, InstallationConfigurationFolderName );

		// installation standard configuration
		var installationStandardConfigurationFilePath = EwlStatics.CombinePaths( installationConfigurationFolderPath, InstallationStandardConfigurationFileName );
		installationStandardConfiguration = XmlOps.DeserializeFromFile<InstallationStandardConfiguration>( installationStandardConfigurationFilePath, false );


		var systemWebApplicationElements = systemGeneralConfiguration.WebApplications ?? new SystemGeneralConfigurationApplication[ 0 ];
		webApplications = ( from systemElement in systemWebApplicationElements
		                    let name = systemElement.Name
		                    let supportsSecureConnections = systemElement.SupportsSecureConnections
		                    select isDevelopmentInstallation
			                           ?
			                           new WebApplication(
				                           name,
				                           installationPath,
				                           supportsSecureConnections,
				                           SystemShortName,
				                           systemWebApplicationElements.AtLeast( 2 ),
				                           systemDevelopmentConfiguration.webProjects.Single( i => i.name == name ) )
			                           : InstallationType == InstallationType.Live
				                           ? new WebApplication(
					                           name,
					                           installationPath,
					                           supportsSecureConnections,
					                           LiveInstallationConfiguration.WebApplications.Single( i => i.Name == name ),
					                           FullShortName,
					                           systemWebApplicationElements.AtLeast( 2 ) )
				                           : new WebApplication(
					                           name,
					                           installationPath,
					                           true,
					                           IntermediateInstallationConfiguration.WebApplications.Single( i => i.Name == name ),
					                           FullShortName,
					                           systemWebApplicationElements.AtLeast( 2 ) ) ).ToImmutableArray();

		// installation custom configuration
		installationCustomConfigurationFilePath = EwlStatics.CombinePaths( installationConfigurationFolderPath, "Custom" + FileExtensions.Xml );

		// installation shared configuration
		installationSharedConfigurationFilePath = EwlStatics.CombinePaths(
			isDevelopmentInstallation
				? EwlStatics.CombinePaths( ConfigurationFolderPath, InstallationConfigurationFolderName, InstallationsFolderName )
				: installationConfigurationFolderPath,
			InstallationSharedConfigurationFileName );
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
	/// Gets a list of the web applications in the system.
	/// </summary>
	public IReadOnlyCollection<WebApplication> WebApplications { get { return webApplications; } }

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
	/// Installation Support Utility use only.
	/// </summary>
	public SystemGeneral.Database PrimaryDatabaseSystemConfiguration => systemGeneralConfiguration.Database;

	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public SystemGeneral.Database GetSecondaryDatabaseSystemConfiguration( string name ) =>
		systemGeneralConfiguration.SecondaryDatabases.SingleOrDefault( i => i.Name == name )?.Database;

	internal bool SystemIsEwl => SystemShortName == "Ewl";

	/// <summary>
	/// Gets the RSIS installation ID for the installation.
	/// </summary>
	public int? RsisInstallationId {
		get { return installationStandardConfiguration.rsisInstallationIdSpecified ? installationStandardConfiguration.rsisInstallationId as int? : null; }
	}

	/// <summary>
	/// Gets the name of the installation.
	/// </summary>
	public string InstallationName { get { return isDevelopmentInstallation ? "Development" : installationStandardConfiguration.installedInstallation.name; } }

	/// <summary>
	/// Gets the short name of the installation.
	/// </summary>
	public string InstallationShortName { get { return isDevelopmentInstallation ? "Dev" : installationStandardConfiguration.installedInstallation.shortName; } }

	/// <summary>
	/// Gets a list of the administrators for the installation.
	/// </summary>
	public List<InstallationStandardNameAndEmailAddress> Administrators {
		get { return new List<InstallationStandardNameAndEmailAddress>( installationStandardConfiguration.administrators ); }
	}

	/// <summary>
	/// Gets a database information object corresponding to the primary database for this configuration. Returns null if there is no database configured.
	/// </summary>
	public DatabaseInfo PrimaryDatabaseInfo {
		get { return installationStandardConfiguration.database != null ? getDatabaseInfo( "", installationStandardConfiguration.database ) : null; }
	}

	/// <summary>
	/// Gets a database information object corresponding to the secondary database for this configuration with the specified name.
	/// </summary>
	public DatabaseInfo GetSecondaryDatabaseInfo( string name ) {
		var secondaryDatabase = installationStandardConfiguration.SecondaryDatabases.SingleOrDefault( i => i.Name == name );
		return secondaryDatabase != null
			       ? getDatabaseInfo( secondaryDatabase.Name, secondaryDatabase.Database )
			       : throw new ApplicationException( "No secondary database exists with the specified name." );
	}

	private DatabaseInfo getDatabaseInfo( string secondaryDatabaseName, InstallationStandard.Database database ) {
		if( database is SqlServerDatabase ) {
			var sqlServerDatabase = database as SqlServerDatabase;
			return new SqlServerInfo(
				secondaryDatabaseName,
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
			return new OracleInfo(
				secondaryDatabaseName,
				oracleDatabase.tnsName,
				oracleDatabase.userAndSchema,
				oracleDatabase.password,
				!oracleDatabase.SupportsConnectionPoolingSpecified || oracleDatabase.SupportsConnectionPooling,
				!oracleDatabase.SupportsLinguisticIndexesSpecified || oracleDatabase.SupportsLinguisticIndexes );
		}
		throw new ApplicationException( "Unknown database type." );
	}

	/// <summary>
	/// Gets the type of the installation.
	/// </summary>
	public InstallationType InstallationType {
		get {
			return isDevelopmentInstallation ? InstallationType.Development :
			       installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration ? InstallationType.Live :
			       InstallationType.Intermediate;
		}
	}

	private bool isDevelopmentInstallation { get { return systemDevelopmentConfiguration != null; } }

	public SystemDevelopment.SystemDevelopmentConfiguration SystemDevelopmentConfiguration { get { return systemDevelopmentConfiguration; } }

	internal LiveInstallationConfiguration LiveInstallationConfiguration {
		get { return (LiveInstallationConfiguration)installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration; }
	}

	internal IntermediateInstallationConfiguration IntermediateInstallationConfiguration {
		get { return (IntermediateInstallationConfiguration)installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration; }
	}

	internal string InstallationPath { get { return installationPath; } }

	/// <summary>
	/// Gets the path of the configuration folder.
	/// </summary>
	public string ConfigurationFolderPath { get { return configurationFolderPath; } }

	internal string InstallationCustomConfigurationFilePath => installationCustomConfigurationFilePath;

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public string InstallationSharedConfigurationFilePath => installationSharedConfigurationFilePath;

	/// <summary>
	/// ISU and internal use only.
	/// </summary>
	public string AsposeLicenseFolderPath => EwlStatics.CombinePaths( configurationFolderPath, AsposeLicenseFolderName );

	/// <summary>
	/// The file path for the error log file for this installation. ("Error Log.txt" in the root of the installation folder).
	/// </summary>
	internal string ErrorLogFilePath => EwlStatics.CombinePaths( installationPath, "Error Log" + FileExtensions.Txt );
}