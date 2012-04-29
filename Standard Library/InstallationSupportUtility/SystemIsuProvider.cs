namespace RedStapler.StandardLibrary.InstallationSupportUtility {
	/// <summary>
	/// System-specific encryption logic.
	/// </summary>
	public interface SystemIsuProvider {
		/// <summary>
		/// Gets the RSIS HTTP base URL.
		/// </summary>
		string RsisHttpBaseUrl { get; }

		/// <summary>
		/// Gets the RSIS TCP base URL.
		/// </summary>
		string RsisTcpBaseUrl { get; }

		/// <summary>
		/// Gets the RSIS TCP user name.
		/// </summary>
		string RsisTcpUserName { get; }

		/// <summary>
		/// Gets the RSIS TCP password.
		/// </summary>
		string RsisTcpPassword { get; }
	}
}