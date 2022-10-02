using Tewl.Tools;

namespace EnterpriseWebLibrary {
	public class BaseUrl {
		internal static string GetUrlString( bool secure, string host, string path ) {
			// Creating the URI allows us to remove explicit default ports from the URL.
			Uri url;
			try {
				url = new Uri( ( secure ? "https" : "http" ) + "://" + host );
			}
			catch( Exception e ) {
				throw new ApplicationException( "Failed to initialize URL. Host was \"" + host + "\".", e );
			}

			return url.Scheme + "://" + url.Host + ( url.IsDefaultPort ? "" : ( ":" + url.Port ) ) + path.PrependDelimiter( "/" );
		}

		internal static ( bool secure, string host, int port, string path ) GetComponents( string urlString ) {
			var url = new Uri( urlString );
			return ( url.Scheme == "https", url.Host, url.Port, url.AbsolutePath.Substring( "/".Length ) );
		}

		internal readonly string Host;
		private readonly int? nonsecurePort;
		private readonly int? securePort;
		internal readonly string Path;

		/// <summary>
		/// Creates a base URL.
		/// </summary>
		/// <param name="host">Do not pass null. Pass the empty string to use the application's default from the configuration file.</param>
		/// <param name="nonsecurePort">Pass null to use the application's default from the configuration file.</param>
		/// <param name="securePort">Pass null to use the application's default from the configuration file.</param>
		/// <param name="path">Pass null to use the application's default from the configuration file. Pass the empty string to represent the root path.</param>
		public BaseUrl( string host, int? nonsecurePort, int? securePort, string path ) {
			Host = host;
			this.nonsecurePort = nonsecurePort;
			this.securePort = securePort;
			Path = path;
		}

		internal BaseUrl CompleteWithDefaults( BaseUrl defaults ) {
			return new BaseUrl(
				Host.Any() ? Host : defaults.Host,
				nonsecurePort ?? defaults.nonsecurePort,
				securePort ?? defaults.securePort,
				Path ?? defaults.Path );
		}

		internal string GetUrlString( bool secure ) {
			var port = secure ? securePort.Value : nonsecurePort.Value;
			return GetUrlString( secure, Host + ":" + port, Path );
		}
	}
}