namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public interface UrlDecoder {
		/// <summary>
		/// Framework use only.
		/// </summary>
		BasicUrlHandler GetUrlHandler( DecodingUrlSegment urlSegment );
	}
}