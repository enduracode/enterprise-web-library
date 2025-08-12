using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public sealed class DecodingBaseUrl {
	public bool Secure { get; }
	public string Host { get; }
	public int Port { get; }
	public string Path { get; }

	/// <summary>
	/// Gets this base URL’s parameters.
	/// </summary>
	public DecodingUrlParameterCollection Parameters { get; }

	internal DecodingBaseUrl( bool secure, string host, int port, string path, DecodingUrlParameterCollection parameters ) {
		Secure = secure;
		Host = host;
		Port = port;
		Path = path;
		Parameters = parameters;
	}

	/// <summary>
	/// Gets the ID of the web application that is resolving the URL.
	/// </summary>
	public string AppId => Parameters.AppId;
}