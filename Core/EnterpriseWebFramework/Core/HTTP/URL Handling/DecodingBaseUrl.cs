#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class DecodingBaseUrl {
		private readonly bool secure;
		private readonly string host;
		private readonly int port;
		private readonly string path;
		private readonly DecodingUrlParameterCollection parameters;

		internal DecodingBaseUrl( bool secure, string host, int port, string path, DecodingUrlParameterCollection parameters ) {
			this.secure = secure;
			this.host = host;
			this.port = port;
			this.path = path;
			this.parameters = parameters;
		}

		public bool Secure => secure;
		public string Host => host;
		public int Port => port;
		public string Path => path;

		/// <summary>
		/// Gets this base URL’s parameters.
		/// </summary>
		public DecodingUrlParameterCollection Parameters => parameters;
	}
}