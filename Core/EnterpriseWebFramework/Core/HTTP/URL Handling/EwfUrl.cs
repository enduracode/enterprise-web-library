namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal sealed class EwfUrl {
	private const char separator = '|';

	public static string Serialize( EwfUrl? url, string additionalData ) =>
		( url is null ? "" : ( url.externalUrl ?? url.baseUrlString + separator + url.appRelativeUrl ) + separator ) + additionalData;

	public static string Deserialize( string serializedUrl, out EwfUrl? url ) {
		var components = serializedUrl.Separate( separator.ToString(), false );
		url = components.Count switch { 1 => null, 2 => new EwfUrl( components[ 0 ] ), _ => new EwfUrl( components[ 0 ], components[ 1 ] ) };
		return components[ ^1 ];
	}

	private readonly string? baseUrlString;
	private readonly string? appRelativeUrl;
	private readonly string? externalUrl;

	public EwfUrl( string baseUrlString, string appRelativeUrl ) {
		this.baseUrlString = baseUrlString;
		this.appRelativeUrl = appRelativeUrl;
	}

	public EwfUrl( string externalUrl ) {
		this.externalUrl = externalUrl;
	}

	public string Url => externalUrl ?? baseUrlString + appRelativeUrl;

	public bool IsExternal => externalUrl is not null;

	public string BaseUrlString => baseUrlString!;

	public string AppRelativeUrl => appRelativeUrl!.StartsWith( '/' ) ? appRelativeUrl[ 1.. ] : appRelativeUrl;

	public EwfUrl AddFragmentIdentifier( string fragmentIdentifier ) {
		fragmentIdentifier = fragmentIdentifier.PrependDelimiter( "#" );
		return IsExternal ? new EwfUrl( externalUrl + fragmentIdentifier ) : new EwfUrl( baseUrlString!, appRelativeUrl + fragmentIdentifier );
	}
}