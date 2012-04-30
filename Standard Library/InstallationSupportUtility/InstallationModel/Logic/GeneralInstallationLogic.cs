namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	internal class GeneralInstallationLogic {
		private readonly string path;

		internal GeneralInstallationLogic( string path ) {
			this.path = path;
		}

		/// <summary>
		/// For example, C:\Inetpub\Red Stapler Information System - Live.
		/// </summary>
		internal string Path { get { return path; } }
	}
}