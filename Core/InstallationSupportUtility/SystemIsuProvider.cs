using System.IO;

namespace EnterpriseWebLibrary.InstallationSupportUtility {
	/// <summary>
	/// System-specific installation support utility logic.
	/// </summary>
	public interface SystemIsuProvider {
		/// <summary>
		/// Writes members in the general provider class.
		/// </summary>
		void WriteGeneralProviderMembers( TextWriter writer );

		/// <summary>
		/// Gets the NDepend folder path relative to the user profile folder. Returns the empty string if NDepend is not present.
		/// </summary>
		string NDependFolderPathInUserProfileFolder { get; }
	}
}