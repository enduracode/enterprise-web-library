namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A desired security setting for a request.
	/// </summary>
	public enum ConnectionSecurity {
		/// <summary>
		/// The request should use standard http.
		/// </summary>
		NonSecure,

		/// <summary>
		/// The request should use https if the application supports it.
		/// </summary>
		SecureIfPossible,

		/// <summary>
		/// The security setting should match that of the current request.
		/// </summary>
		MatchingCurrentRequest
	}
}