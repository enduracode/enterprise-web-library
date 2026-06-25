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
	/// Development Utility and private use only.
	/// </summary>
	public const string SystemGeneralConfigurationFileName = "General.xml";

	/// <summary>
	/// Installation Support Utility and private use only.
	/// </summary>
	public const string InstallationConfigurationFolderName = "Installation";

	/// <summary>
	/// Installation Support Utility and private use only.
	/// </summary>
	public const string InstallationsFolderName = "Installations";

	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public const string DevelopmentInstallationFolderName = "Development";

	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public const string InstallationStandardConfigurationFileName = "Standard" + FileExtensions.Xml;

	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public const string InstallationCustomConfigurationFileName = "Custom" + FileExtensions.Xml;

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
	public SystemDevelopment.SystemDevelopmentConfiguration? SystemDevelopmentConfiguration { get; }
	private readonly InstallationStandardConfiguration installationStandardConfiguration;

	/// <summary>
	/// Gets a list of the web applications in the system.
	/// </summary>
	public IReadOnlyCollection<WebApplication> WebApplications { get; }

	private readonly string installationCustomConfigurationFilePath;
	private readonly string installationSharedConfigurationFilePath;

	/// <summary>
	/// Development Utility and private use only.
	/// </summary>
	public bool? SystemUsesLegacyEwl { get; }

	/// <summary>
	/// Creates a new installation configuration.
	/// </summary>
	public InstallationConfiguration( string installationPath, bool isDevelopmentInstallation ) {
		this.installationPath = installationPath;

		// legacy .NET and EWL support
		if( isDevelopmentInstallation ) {
			var libraryProjectFile = File.ReadAllText(
				EwlStatics.CombinePaths( InstallationFileStatics.GetGeneralFilesFolderPath( installationPath, true ), "Library.csproj" ) );
			if( libraryProjectFile.Contains( "<TargetFramework>net4", StringComparison.OrdinalIgnoreCase ) )
				SystemUsesLegacyEwl = libraryProjectFile.Contains( """<PackageReference Include="Ewl""", StringComparison.OrdinalIgnoreCase );
		}

		// The configuration folder is not inside any particular app’s folder because it is system-wide (technically installation-wide) and not app-specific.
		configurationFolderPath = EwlStatics.CombinePaths(
			InstallationFileStatics.GetGeneralFilesFolderPath( installationPath, isDevelopmentInstallation ),
			ConfigurationFolderName + ( SystemUsesLegacyEwl == true ? " New" : "" ) );


		// Do not perform schema validation for non-development installations because the schema files may not be available. For development installations, also
		// do not perform schema validation since the schema files may not match this version of the library. This can happen, for example, when you are trying to
		// run a system using an unreleased version of the library that contains schema changes.

		// system general configuration
		var systemGeneralConfigurationFilePath = EwlStatics.CombinePaths( ConfigurationFolderPath, SystemGeneralConfigurationFileName );
		systemGeneralConfiguration = XmlOps.DeserializeFromFile<SystemGeneralConfiguration>( systemGeneralConfigurationFilePath, false );

		// system development configuration
		if( isDevelopmentInstallation )
			SystemDevelopmentConfiguration = XmlOps.DeserializeFromFile<SystemDevelopment.SystemDevelopmentConfiguration>(
				EwlStatics.CombinePaths( configurationFolderPath, "Development.xml" ),
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


		var systemWebApplicationElements = systemGeneralConfiguration.WebApplications ?? Enumerable.Empty<SystemGeneralConfigurationApplication>();
		WebApplications = systemWebApplicationElements.Select( ( element, index ) => {
				var name = element.Name;
				var supportsSecureConnections = element.SupportsSecureConnections;
				return isDevelopmentInstallation
					       ?
					       new WebApplication(
						       name,
						       installationPath,
						       supportsSecureConnections,
						       index,
						       SystemShortName,
						       systemWebApplicationElements.AtLeast( 2 ),
						       SystemDevelopmentConfiguration!.GetWebProject( name ) )
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
							       systemWebApplicationElements.AtLeast( 2 ) );
			} )
			.Materialize();

		// installation custom configuration
		installationCustomConfigurationFilePath = EwlStatics.CombinePaths( installationConfigurationFolderPath, InstallationCustomConfigurationFileName );

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
	public string SystemName => systemGeneralConfiguration.systemName;

	/// <summary>
	/// Gets the short name of the system.
	/// </summary>
	public string SystemShortName => systemGeneralConfiguration.systemShortName;

	/// <summary>
	/// Gets the full name of this installation.  For example, 'Red Stapler Information System - Live'.
	/// </summary>
	public string FullName => GetFullNameFromSystemAndInstallationNames( SystemName, InstallationName );

	/// <summary>
	/// Gets the full short name of this installation.  For example, 'RsisLive'.
	/// </summary>
	public string FullShortName => GetFullShortNameFromSystemAndInstallationNames( SystemShortName, InstallationShortName );

	/// <summary>
	/// Gets a list of the services in the system.
	/// </summary>
	public IEnumerable<WindowsService> WindowsServices =>
		systemGeneralConfiguration.WindowsServices is null
			? [ ]
			: systemGeneralConfiguration.WindowsServices.Select( ws => new WindowsService( ws, FullShortName ) );

	/// <summary>
	/// Gets a list of the developers for the system.
	/// </summary>
	public IReadOnlyCollection<NameAndEmailAddress> Developers => systemGeneralConfiguration.developers;

	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public SystemGeneral.Database PrimaryDatabaseSystemConfiguration => systemGeneralConfiguration.Database;

	/// <summary>
	/// Installation Support Utility use only.
	/// </summary>
	public SystemGeneral.Database? GetSecondaryDatabaseSystemConfiguration( string name ) =>
		systemGeneralConfiguration.SecondaryDatabases.SingleOrDefault( i => i.Name == name )?.Database;

	internal bool SystemIsEwl => SystemShortName == "Ewl";

	/// <summary>
	/// Gets the RSIS installation ID for the installation.
	/// </summary>
	public int? RsisInstallationId => installationStandardConfiguration.rsisInstallationIdSpecified ? installationStandardConfiguration.rsisInstallationId : null;

	/// <summary>
	/// Gets the name of the installation.
	/// </summary>
	public string InstallationName => isDevelopmentInstallation ? "Development" : installationStandardConfiguration.installedInstallation.name;

	/// <summary>
	/// Gets the short name of the installation.
	/// </summary>
	public string InstallationShortName => isDevelopmentInstallation ? "Dev" : installationStandardConfiguration.installedInstallation.shortName;

	/// <summary>
	/// Gets a list of the administrators for the installation.
	/// </summary>
	public IReadOnlyCollection<InstallationStandardNameAndEmailAddress> Administrators => installationStandardConfiguration.administrators;

	/// <summary>
	/// Gets a database information object corresponding to the primary database for this configuration. Returns null if there is no database configured.
	/// </summary>
	public DatabaseInfo? PrimaryDatabaseInfo =>
		installationStandardConfiguration.database != null ? getDatabaseInfo( "", installationStandardConfiguration.database ) : null;

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
		if( database is SqlServerDatabase sqlServerDatabase )
			return new SqlServerInfo(
				secondaryDatabaseName,
				sqlServerDatabase.server,
				sqlServerDatabase.SqlServerAuthenticationLogin?.LoginName,
				sqlServerDatabase.SqlServerAuthenticationLogin?.Password,
				sqlServerDatabase.database ?? FullShortName,
				true,
				sqlServerDatabase.FullTextCatalog );
		if( database is MySqlDatabase mySqlDatabase )
			return new MySqlInfo( secondaryDatabaseName, mySqlDatabase.database ?? FullShortName.CamelToEnglish().EnglishToOracle(), true );
		if( database is OracleDatabase oracleDatabase )
			return new OracleInfo(
				secondaryDatabaseName,
				oracleDatabase.tnsName,
				oracleDatabase.userAndSchema,
				oracleDatabase.password,
				!oracleDatabase.SupportsConnectionPoolingSpecified || oracleDatabase.SupportsConnectionPooling,
				!oracleDatabase.SupportsLinguisticIndexesSpecified || oracleDatabase.SupportsLinguisticIndexes );
		if( database is FolderDatabase folderDatabase )
			return new FolderDatabaseInfo( secondaryDatabaseName, folderDatabase.FolderPath );
		throw new ApplicationException( "Unknown database type." );
	}

	/// <summary>
	/// Gets the type of the installation.
	/// </summary>
	public InstallationType InstallationType =>
		isDevelopmentInstallation ? InstallationType.Development :
		installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration ? InstallationType.Live :
		InstallationType.Intermediate;

	private bool isDevelopmentInstallation => SystemDevelopmentConfiguration is not null;

	internal string DeveloperNotificationEmailFromAddress => installationStandardConfiguration.installedInstallation.DeveloperNotificationEmailFromAddress;

	internal ulong DebugLogMaxSizeBytes {
		get {
			const ulong bytesPerGb = 1_000_000_000;
			return installationStandardConfiguration.installedInstallation.DebugLogMaxSizeGbSpecified
				       ? installationStandardConfiguration.installedInstallation.DebugLogMaxSizeGb * bytesPerGb
				       : 10 * bytesPerGb;
		}
	}

	/// <summary>
	/// Installation Support Utility and internal use only.
	/// </summary>
	public AzureHosting? AzureHosting => installationStandardConfiguration.installedInstallation.AzureHosting;

	internal LiveInstallationConfiguration LiveInstallationConfiguration =>
		(LiveInstallationConfiguration)installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration;

	internal IntermediateInstallationConfiguration IntermediateInstallationConfiguration =>
		(IntermediateInstallationConfiguration)installationStandardConfiguration.installedInstallation.InstallationTypeConfiguration;

	/// <summary>
	/// Development Utility and internal use only.
	/// </summary>
	public string InstallationPath => installationPath;

	/// <summary>
	/// Gets the path of the configuration folder.
	/// </summary>
	public string ConfigurationFolderPath => configurationFolderPath;

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