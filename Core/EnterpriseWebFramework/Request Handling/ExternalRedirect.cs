#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP redirect to an external resource.
	/// </summary>
	public sealed class ExternalRedirect {
		internal ExternalResource Resource;
		internal bool IsPermanent;

		/// <summary>
		/// Creates an external redirect.
		/// </summary>
		/// <param name="resource">The external resource. Do not pass null.</param>
		/// <param name="isPermanent">Pass true to use HTTP 308 (Permanent Redirect) instead of HTTP 307 (Temporary Redirect).</param>
		public ExternalRedirect( ExternalResource resource, bool isPermanent ) {
			Resource = resource;
			IsPermanent = isPermanent;
		}
	}
}