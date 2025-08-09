namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal sealed class EwfUrl {
	private const char separator = '|';
	internal const char AdditionalDataSeparator = '-';

	public static string Serialize( EwfUrl? url, Func<string, string> appIdSelector, string additionalData ) =>
		url is null ? additionalData :
		url.externalUrl is not null ? url.externalUrl + separator + additionalData :
		url.baseUrlString + separator + url.appRelativeUrl + separator + appIdSelector( url.appId! ).AppendDelimiter( AdditionalDataSeparator.ToString() ) +
		additionalData;

	public static string Deserialize( string serializedUrl, Func<string, string> appIdSelector, out EwfUrl? url ) {
		var components = serializedUrl.Separate( separator.ToString(), false );
		if( components.Count == 1 ) {
			url = null;
			return serializedUrl;
		}

		if( components.Count == 2 ) {
			url = new EwfUrl( components[ 0 ] );
			return components[ 1 ];
		}

		var dataIndex = components[ ^1 ].IndexOf( AdditionalDataSeparator ) + 1;
		url = new EwfUrl( components[ 0 ], components[ 1 ], appIdSelector( dataIndex > 0 ? components[ 2 ][ ..( dataIndex - 1 ) ] : "" ) );
		return components[ 2 ][ dataIndex.. ];
	}

	private readonly string? baseUrlString;
	private readonly string? appRelativeUrl;
	private readonly string? appId;
	private readonly string? externalUrl;

	public EwfUrl( string baseUrlString, string appRelativeUrl, string appId ) {
		this.baseUrlString = baseUrlString;
		this.appRelativeUrl = appRelativeUrl;
		this.appId = appId;
	}

	public EwfUrl( string externalUrl ) {
		this.externalUrl = externalUrl;
	}

	public string Url => externalUrl ?? baseUrlString + appRelativeUrl;

	public bool IsExternal => externalUrl is not null;

	public string BaseUrlString => baseUrlString!;

	public string AppRelativeUrl => appRelativeUrl!.StartsWith( '/' ) ? appRelativeUrl[ 1.. ] : appRelativeUrl;

	public string AppId => appId!;

	public EwfUrl AddFragmentIdentifier( string fragmentIdentifier ) {
		fragmentIdentifier = fragmentIdentifier.PrependDelimiter( "#" );
		return IsExternal ? new EwfUrl( externalUrl + fragmentIdentifier ) : new EwfUrl( baseUrlString!, appRelativeUrl + fragmentIdentifier, appId! );
	}
}