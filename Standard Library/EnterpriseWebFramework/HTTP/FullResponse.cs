namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	internal class FullResponse {
		internal readonly string ContentType;
		internal readonly string FileName;

		// One of these should always be null.
		internal readonly string TextBody;
		internal readonly byte[] BinaryBody;

		public FullResponse( string contentType, string fileName, string textBody ) {
			ContentType = contentType;
			FileName = fileName;
			TextBody = textBody;
		}

		public FullResponse( string contentType, string fileName, byte[] binaryBody ) {
			ContentType = contentType;
			FileName = fileName;
			BinaryBody = binaryBody;
		}
	}
}