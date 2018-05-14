namespace EnterpriseWebLibrary {
	/// <summary>
	/// Internal and Red Stapler Information System use only.
	/// </summary>
	public static class InstallationFileStatics {
		/// <summary>
		/// Internal and Red Stapler Information System use only.
		/// </summary>
		public const string FilesFolderName = "Files";

		/// <summary>
		/// Installation Support Utility and internal use only.
		/// </summary>
		public static string GetGeneralFilesFolderPath( string installationPath, bool isDevelopmentInstallation ) {
			if( isDevelopmentInstallation )
				return EwlStatics.CombinePaths( installationPath, "Library" );
			return installationPath;
		}
	}
}