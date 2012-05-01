namespace RedStapler.StandardLibrary.InstallationSupportUtility.InstallationModel.Logic {
	public class GeneralInstallationLogic {
		private readonly string path;

		public GeneralInstallationLogic( string path ) {
			this.path = path;
		}

		/// <summary>
		/// For example, C:\Inetpub\Red Stapler Information System - Live.
		/// </summary>
		public string Path { get { return path; } }
	}
}