using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using RedStapler.StandardLibrary.Configuration.Machine;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.StandardLibrary.Configuration {
	public static class ConfigurationStatics {
		/// <summary>
		/// EWL Core and Development Utility use only.
		/// </summary>
		public const string ProvidersFolderAndNamespaceName = "Providers";

		/// <summary>
		/// Gets the path of the Red Stapler folder on the machine.
		/// </summary>
		public static string RedStaplerFolderPath { get; private set; }

		/// <summary>
		/// EWL Core and ISU use only.
		/// </summary>
		public static MachineConfiguration MachineConfiguration { get; private set; }

		internal static Assembly AppAssembly { get; private set; }
		internal static InstallationConfiguration InstallationConfiguration { get; private set; }
		private static Type globalInitializerType { get; set; }

		/// <summary>
		/// EWL use only.
		/// </summary>
		public static SystemGeneralProvider SystemGeneralProvider { get; private set; }

		internal static string AppName { get; private set; }
		internal static bool IsClientSideProgram { get; private set; }

		internal static void Init(
			bool useRelativeInstallationPath, Type globalInitializerType, string appName, bool isClientSideProgram, ref string initializationLog ) {
			RedStaplerFolderPath = Environment.GetEnvironmentVariable( "RedStaplerFolderPath" ) ?? @"C:\Red Stapler";

			initializationLog += Environment.NewLine + "About to load machine config";

			// Load machine configuration.
			var machineConfigXmlFilePath = StandardLibraryMethods.CombinePaths( RedStaplerFolderPath, "Machine Configuration.xml" );
			if( File.Exists( machineConfigXmlFilePath ) ) {
				// Do not perform schema validation since the schema file won't be available on non-development machines.
				MachineConfiguration = XmlOps.DeserializeFromFile<MachineConfiguration>( machineConfigXmlFilePath, false );
			}

			initializationLog += Environment.NewLine + "About to initialize stack trace";

			// Assume the first assembly up the call stack that is not this assembly is the application assembly.
			var stackFrames = new StackTrace().GetFrames();
			if( stackFrames == null )
				throw new ApplicationException( "No stack trace available." );
			AppAssembly = stackFrames.Select( frame => frame.GetMethod().DeclaringType.Assembly ).First( assembly => assembly != Assembly.GetExecutingAssembly() );

			initializationLog += Environment.NewLine + "Stack trace initialized";

			// Determine the installation path and load configuration information.
			string installationPath;
			bool isDevelopmentInstallation;
			if( NetTools.IsWebApp() ) {
				initializationLog += Environment.NewLine + "Is a web app";

				installationPath = StandardLibraryMethods.CombinePaths( HttpRuntime.AppDomainAppPath, ".." );
				isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
			}
			else {
				initializationLog += Environment.NewLine + "Is not a web app";

				// Assume this is an installed installation. If this assumption turns out to be wrong, consider it a development installation. Installed executables are
				// one level below the installation folder.
				if( useRelativeInstallationPath )
					installationPath = "..";
				else {
					var assemblyFolderPath = Path.GetDirectoryName( AppAssembly.Location );
					installationPath = StandardLibraryMethods.CombinePaths( assemblyFolderPath, ".." );
				}
				isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
				if( isDevelopmentInstallation )
					installationPath = StandardLibraryMethods.CombinePaths( installationPath, "..", ".." ); // Visual Studio puts executables inside bin\Debug.
			}
			initializationLog += Environment.NewLine + "Successfully determined installation path";
			InstallationConfiguration = new InstallationConfiguration( MachineIsStandbyServer, installationPath, isDevelopmentInstallation );
			initializationLog += Environment.NewLine + "Successfully loaded installation configuration";

			ConfigurationStatics.globalInitializerType = globalInitializerType;
			SystemGeneralProvider = GetSystemLibraryProvider( "General" ) as SystemGeneralProvider;
			if( SystemGeneralProvider == null )
				throw new ApplicationException( "General provider not found in system" );

			AppName = appName;
			IsClientSideProgram = isClientSideProgram;
		}

		public static bool MachineIsStandbyServer {
			get { return MachineConfiguration != null && MachineConfiguration.IsStandbyServerSpecified && MachineConfiguration.IsStandbyServer; }
		}

		/// <summary>
		/// Returns the default base URL for the specified web application. This will never have a trailing slash.
		/// </summary>
		public static string GetWebApplicationDefaultBaseUrl( string applicationName, bool secure ) {
			return InstallationConfiguration.WebApplications.Single( i => i.Name == applicationName ).DefaultBaseUrl.GetUrlString( secure );
		}

		/// <summary>
		/// Loads installation-specific custom configuration information.
		/// </summary>
		public static T LoadInstallationCustomConfiguration<T>() {
			// Do not perform schema validation for non-development installations because the schema file won't be available on non-development machines. Do not
			// perform schema validation for development installations because we may create sample solutions and send them to tech support people for
			// troubleshooting, and these people may not put the solution in the proper location on disk. In this case we would not have access to the schema since
			// we use absolute paths in the XML files to refer to the schema files.
			return XmlOps.DeserializeFromFile<T>( InstallationConfiguration.InstallationCustomConfigurationFilePath, false );
		}

		internal static object GetSystemLibraryProvider( string providerName ) {
			var systemLibraryAssembly = globalInitializerType.Assembly;
			var typeName = globalInitializerType.Namespace + ".Configuration." + ProvidersFolderAndNamespaceName + "." + providerName;
			return systemLibraryAssembly.GetType( typeName ) != null ? systemLibraryAssembly.CreateInstance( typeName ) : null;
		}

		internal static ApplicationException CreateProviderNotFoundException( string providerName ) {
			return
				new ApplicationException(
					providerName + " provider not found in system. To implement, create a class named " + providerName + @" in Library\Configuration\" +
					ProvidersFolderAndNamespaceName + " and implement the System" + providerName + "Provider interface." );
		}
	}
}