﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using EnterpriseWebLibrary.Configuration.Machine;
using Humanizer;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Configuration {
	public static class ConfigurationStatics {
		/// <summary>
		/// EWL Core and Development Utility use only.
		/// </summary>
		public const string ProvidersFolderAndNamespaceName = "Providers";

		/// <summary>
		/// Gets the path of the EWL folder on the machine.
		/// </summary>
		public static string EwlFolderPath { get; private set; }

		/// <summary>
		/// EWL and ISU use only.
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
		internal static bool IsClientSideApp { get; private set; }

		internal static void Init( string assemblyFolderPath, Type globalInitializerType, string appName, bool isClientSideApp, ref string initializationLog ) {
			EwlFolderPath = Environment.GetEnvironmentVariable( "{0}FolderPath".FormatWith( EwlStatics.EwlInitialism.EnglishToPascal() ) ) ??
			                @"C:\{0}".FormatWith( EwlStatics.EwlName );

			initializationLog += Environment.NewLine + "About to load machine config";

			// Load machine configuration.
			var machineConfigFilePath = EwlStatics.CombinePaths( EwlFolderPath, "Machine Configuration.xml" );
			if( File.Exists( machineConfigFilePath ) )
				// Do not perform schema validation since the schema file won't be available on non-development machines.
				try {
					MachineConfiguration = XmlOps.DeserializeFromFile<MachineConfiguration>( machineConfigFilePath, false );
				}
				catch {
					// The alt file allows us to smoothly transition all machines in the case of schema changes that break deserialization.
					var altFilePath = EwlStatics.CombinePaths( EwlFolderPath, "Machine Configuration Alt.xml" );
					if( !File.Exists( altFilePath ) )
						throw;
					MachineConfiguration = XmlOps.DeserializeFromFile<MachineConfiguration>( altFilePath, false );
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

				installationPath = EwlStatics.CombinePaths( HttpRuntime.AppDomainAppPath, ".." );
				isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
			}
			else {
				initializationLog += Environment.NewLine + "Is not a web app";

				// Assume this is an installed installation. If this assumption turns out to be wrong, consider it a development installation. Installed executables are
				// one level below the installation folder.
				installationPath = EwlStatics.CombinePaths( assemblyFolderPath.Any() ? assemblyFolderPath : Path.GetDirectoryName( AppAssembly.Location ), ".." );
				isDevelopmentInstallation = !InstallationConfiguration.InstalledInstallationExists( installationPath );
				if( isDevelopmentInstallation )
					installationPath = EwlStatics.CombinePaths( installationPath, "..", ".." ); // Visual Studio puts executables inside bin\Debug.
			}
			initializationLog += Environment.NewLine + "Successfully determined installation path";
			InstallationConfiguration = new InstallationConfiguration( installationPath, isDevelopmentInstallation );
			initializationLog += Environment.NewLine + "Successfully loaded installation configuration";

			ConfigurationStatics.globalInitializerType = globalInitializerType;
			SystemGeneralProvider = GetSystemLibraryProvider<SystemGeneralProvider>( "General" ).GetProvider( returnNullIfNotFound: true );
			if( SystemGeneralProvider == null )
				throw new ApplicationException( "General provider not found in system" );

			AppName = appName;
			IsClientSideApp = isClientSideApp;
		}

		/// <summary>
		/// Gets the name of the system.
		/// </summary>
		public static string SystemName { get { return InstallationConfiguration.SystemName; } }

		/// <summary>
		/// Returns the default base URL for the specified web application. This will never have a trailing slash.
		/// </summary>
		public static string GetWebApplicationDefaultBaseUrl( string applicationName, bool secure ) {
			return InstallationConfiguration.WebApplications.Single( i => i.Name == applicationName ).DefaultBaseUrl.GetUrlString( secure );
		}

		internal static bool DatabaseExists { get { return InstallationConfiguration.PrimaryDatabaseInfo != null; } }

		internal static string CertificateEmailAddressOverride { get { return InstallationConfiguration.CertificateEmailAddressOverride; } }

		internal static bool IsDevelopmentInstallation { get { return InstallationConfiguration.InstallationType == InstallationType.Development; } }

		/// <summary>
		/// Gets whether this is a live installation. Use with caution. If you do not deliberately test code that only runs in live installations, you may not
		/// discover problems with it until it is live.
		/// </summary>
		public static bool IsLiveInstallation { get { return InstallationConfiguration.InstallationType == InstallationType.Live; } }

		/// <summary>
		/// Framework use only.
		/// </summary>
		public static bool IsIntermediateInstallation { get { return InstallationConfiguration.InstallationType == InstallationType.Intermediate; } }

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string InstallationPath { get { return InstallationConfiguration.InstallationPath; } }

		/// <summary>
		/// Gets the path of the Files folder for the system.
		/// </summary>
		public static string FilesFolderPath {
			get {
				return EwlStatics.CombinePaths(
					InstallationFileStatics.GetGeneralFilesFolderPath(
						InstallationConfiguration.InstallationPath,
						InstallationConfiguration.InstallationType == InstallationType.Development ),
					InstallationFileStatics.FilesFolderName );
			}
		}

		/// <summary>
		/// Development Utility use only.
		/// </summary>
		public static string ServerSideConsoleAppRelativeFolderPath {
			get { return InstallationConfiguration.InstallationType == InstallationType.Development ? EwlStatics.GetProjectOutputFolderPath( true ) : ""; }
		}


		// Do not perform schema validation for non-development installations because the schema file won't be available on non-development machines. Do not perform
		// schema validation for development installations because we may create sample solutions and send them to tech support people for troubleshooting, and
		// these people may not put the solution in the proper location on disk. In this case we would not have access to the schema since we use absolute paths in
		// the XML files to refer to the schema files.

		/// <summary>
		/// Loads installation-specific custom configuration information.
		/// </summary>
		public static T LoadInstallationCustomConfiguration<T>() {
			return XmlOps.DeserializeFromFile<T>( InstallationConfiguration.InstallationCustomConfigurationFilePath, false );
		}

		/// <summary>
		/// Loads installation configuration information that is shared across installations.
		/// </summary>
		public static T LoadInstallationSharedConfiguration<T>() {
			return XmlOps.DeserializeFromFile<T>( InstallationConfiguration.InstallationSharedConfigurationFilePath, false );
		}


		internal static SystemProviderReference<ProviderType> GetSystemLibraryProvider<ProviderType>( string providerName ) where ProviderType: class =>
			new SystemProviderGetter(
				globalInitializerType.Assembly,
				globalInitializerType.Namespace + ".Configuration." + ProvidersFolderAndNamespaceName,
				getProviderNotFoundErrorMessage ).GetProvider<ProviderType>( providerName );

		private static string getProviderNotFoundErrorMessage( string providerName ) =>
			providerName + " provider not found in system. To implement, create a class named " + providerName + @" in Library\Configuration\" +
			ProvidersFolderAndNamespaceName + " and implement the System" + providerName + "Provider interface.";
	}
}