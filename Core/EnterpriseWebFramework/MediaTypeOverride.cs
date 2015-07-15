namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A file extension and the Internet media type that the framework will use when serving a static file with that extension.
	/// </summary>
	public class MediaTypeOverride {
		internal readonly string FileExtension;
		internal readonly string MediaType;

		/// <summary>
		/// Creates a media-type override. A static file with the specified extension will be served with the specified media type, regardless of the framework's
		/// default media type for that extension.
		/// </summary>
		/// <param name="fileExtension">The file extension, which must include the leading dot.</param>
		/// <param name="mediaType">The Internet media type. Do not pass null. If you pass the empty string, the framework will not include a Content-Type header in
		/// the response.</param>
		public MediaTypeOverride( string fileExtension, string mediaType ) {
			FileExtension = fileExtension;
			MediaType = mediaType;
		}
	}
}