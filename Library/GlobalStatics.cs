using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.Installation;

namespace EnterpriseWebLibrary {
	public static class GlobalStatics {
		public static readonly string[] ConfigurationXsdFileNames = { "Installation Standard", "Machine", "System Development", "System General" };

		private static InstallationSharedConfiguration installationSharedConfiguration;

		internal static void Init() {
			installationSharedConfiguration = ConfigurationStatics.LoadInstallationSharedConfiguration<InstallationSharedConfiguration>();
		}

		internal static string IntermediateLogInPassword => installationSharedConfiguration.IntermediateLogInPassword;

		/// <summary>
		/// Gets the NDepend folder path relative to the user profile folder. Returns the empty string if NDepend is not present.
		/// </summary>
		public static string NDependFolderPathInUserProfileFolder => installationSharedConfiguration.NDependFolderPathInUserProfileFolder ?? "";
	}
}