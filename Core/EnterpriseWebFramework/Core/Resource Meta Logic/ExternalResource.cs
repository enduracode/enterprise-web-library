namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A web resource outside of the system.
	/// </summary>
	public sealed class ExternalResource: ResourceInfo {
		private readonly string url;

		/// <summary>
		/// Creates an external resource. Do not pass null or the empty string for url.
		/// </summary>
		public ExternalResource( string url ) {
			this.url = url;
		}

		public override bool UserCanAccessResource => true;
		public override AlternativeResourceMode AlternativeMode => null;

		public override string GetUrl( bool disableAuthorizationCheck = false ) =>
			url.Replace( "~", EwfApp.GetDefaultBaseUrl( ConnectionSecurity.SecureIfPossible.ShouldBeSecureGivenCurrentRequest( false ) ) );
	}
}