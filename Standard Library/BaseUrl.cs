using System;
using System.Linq;

namespace RedStapler.StandardLibrary {
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

		private readonly string host;
		private readonly int? nonsecurePort;
		private readonly int? securePort;
		private readonly string path;

		/// <summary>
		/// Creates a base URL.
		/// </summary>
		/// <param name="host">Do not pass null. Pass the empty string to use the application's default from the configuration file.</param>
		/// <param name="nonsecurePort">Pass null to use the application's default from the configuration file.</param>
		/// <param name="securePort">Pass null to use the application's default from the configuration file.</param>
		/// <param name="path">Pass null to use the application's default from the configuration file. Pass the empty string to represent the root path.</param>
		public BaseUrl( string host, int? nonsecurePort, int? securePort, string path ) {
			this.host = host;
			this.nonsecurePort = nonsecurePort;
			this.securePort = securePort;
			this.path = path;
		}

		internal BaseUrl CompleteWithDefaults( BaseUrl defaults ) {
			return new BaseUrl( host.Any() ? host : defaults.host, nonsecurePort ?? defaults.nonsecurePort, securePort ?? defaults.securePort, path ?? defaults.path );
		}

		internal string GetUrlString( bool secure ) {
			var port = secure ? securePort.Value : nonsecurePort.Value;
			return GetUrlString( secure, host + ":" + port, path );
		}
	}
}