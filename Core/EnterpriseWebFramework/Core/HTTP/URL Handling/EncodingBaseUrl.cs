namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class EncodingBaseUrl {
		internal readonly BaseUrl BaseUrl;
		internal readonly EncodingUrlParameterCollection Parameters;

		/// <summary>
		/// Creates a base URL.
		/// </summary>
		/// <param name="baseUrl">The base URL. Do not pass null.</param>
		/// <param name="parameters">The parameters.</param>
		public EncodingBaseUrl( BaseUrl baseUrl, EncodingUrlParameterCollection parameters = null ) {
			BaseUrl = baseUrl;
			Parameters = parameters ?? new EncodingUrlParameterCollection();
		}
	}
}