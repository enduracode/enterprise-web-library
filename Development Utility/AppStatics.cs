using System.Reflection;
using System.Runtime.Loader;
using System.Xml;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using Serilog;

namespace EnterpriseWebLibrary.DevelopmentUtility;

internal static class AppStatics {
	internal const string MercurialRepositoryFolderName = ".hg";
	internal const string GitRepositoryFolderName = ".git";
	internal const string TewlProjectPath = "Shared/TEWL";
	internal const string StaticFileLogicFolderName = "Logic";
	internal const string ProviderProjectFolderName = "Providers";
	internal const string MySqlProviderProjectName = "MySQL";
	internal const string OracleDatabaseProviderProjectName = "Oracle Database";
	internal const string SqliteProviderProjectName = "SQLite";
	internal const string OpenIdConnectProviderProjectName = "OpenID Connect";
	internal const string SamlProviderProjectName = "SAML";
	internal const string PdfProviderProjectName = "PDF";
	internal const string WordProviderProjectName = "Word";
	internal const string AzureJobDispatcherProjectName = "Azure Job Dispatcher";

	internal static bool NDependIsPresent;

	private class NDependLoadContext: AssemblyLoadContext {
		private readonly HashSet<string> conflictingAssemblies = new( [ "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp" ], StringComparer.Ordinal );

		public NDependLoadContext(): base( "NDepend" ) {
			Resolving += ( _, assemblyName ) => LoadAssembly( assemblyName.Name! );
		}

		protected override Assembly? Load( AssemblyName assemblyName ) {
			var name = assemblyName.Name!;
			return conflictingAssemblies.Contains( name ) ? LoadAssembly( name ) : base.Load( assemblyName );
		}

		public Assembly LoadAssembly( string name ) =>
			LoadFromAssemblyPath(
				EwlStatics.CombinePaths(
					Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
					ConfigurationStatics.MachineConfiguration!.NDependFolderPathInUserProfileFolderEffective,
					"Lib",
					$"{name}.dll" ) );
	}

	internal static void Init() {
		var loggerConfiguration = new LoggerConfiguration();
		loggerConfiguration =
			string.Equals( Environment.GetEnvironmentVariable( "SYSTEM_DEBUG" ), bool.TrueString, StringComparison.Ordinal ) /* Azure DevOps pipeline debug mode */
				? loggerConfiguration.MinimumLevel.Debug()
				: loggerConfiguration.MinimumLevel.Information();
		Log.Logger = loggerConfiguration.WriteTo.Console( outputTemplate: "{Timestamp:MMM'-'dd HH:mm:ss} {Level:u3}  {Message:lj}{NewLine}{Exception}" )
			.CreateLogger();

		NDependIsPresent = ConfigurationStatics.MachineConfiguration is not null && Directory.Exists(
			                   EwlStatics.CombinePaths(
				                   Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
				                   ConfigurationStatics.MachineConfiguration.NDependFolderPathInUserProfileFolderEffective ) );
		if( NDependIsPresent ) {
			var loadContext = new NDependLoadContext();
			AssemblyLoadContext.Default.Resolving += ( _, assemblyName ) => {
				var name = assemblyName.Name;
				return string.Equals( name, "NDepend.API", StringComparison.Ordinal ) ? loadContext.LoadAssembly( name! ) : null;
			};
		}
	}

	internal static bool SystemIsTewl( this DevelopmentInstallation installation ) =>
		string.Equals( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName, "Tewl", StringComparison.Ordinal );

	internal static bool SystemIsSystemManager( this DevelopmentInstallation installation ) =>
		string.Equals( installation.ExistingInstallationLogic.RuntimeConfiguration.SystemShortName, "SystemManager", StringComparison.Ordinal );

	internal static string GetLiteralDateTimeExpression( DateTimeOffset dateTime ) =>
		"DateTimeOffset.Parse( \"" + dateTime.ToString( "o" ) + "\", null, DateTimeStyles.RoundtripKind )";

	internal static bool WebProjectIsLegacy( DevelopmentInstallation installation, WebApplication application ) {
		using var reader = XmlReader.Create(
			EwlStatics.CombinePaths( installation.GeneralLogic.Path, application.Name, application.Name + ".csproj" ),
			new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true } );
		while( reader.Read() )
			if( reader.NodeType == XmlNodeType.Element /* first of these will be Project */ )
				return reader.GetAttribute( "Sdk" ) is null;
		return false;
	}

	// see https://stackoverflow.com/a/1793962/35349
	internal static string NormalizeLineEndingsFromXml( string text ) => text.Replace( Environment.NewLine, "\n" ).Replace( "\n", Environment.NewLine );
}