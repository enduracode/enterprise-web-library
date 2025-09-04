using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core;

public sealed class TrustedUrl: NestedUrl, IEquatable<TrustedUrl> {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static readonly TrustedUrl Invalid = new( null, null );

	private static readonly LocalDatePattern datePattern = LocalDatePattern.CreateWithInvariantCulture( "uuuuMMdd" );

	private static Func<Func<IReadOnlyCollection<BasicUrlHandler>?>, IReadOnlyCollection<BasicUrlHandler>?>? urlResolverExecutor;
	private static Func<IReadOnlyCollection<BasicUrlHandler>, Func<TrustedUrl>, TrustedUrl>? urlHandlerOverrideMethodExecutor;

	public static void Init(
		Func<Func<IReadOnlyCollection<BasicUrlHandler>?>, IReadOnlyCollection<BasicUrlHandler>?> urlResolverExecutor,
		Func<IReadOnlyCollection<BasicUrlHandler>, Func<TrustedUrl>, TrustedUrl> urlHandlerOverrideMethodExecutor ) {
		TrustedUrl.urlResolverExecutor = urlResolverExecutor;
		TrustedUrl.urlHandlerOverrideMethodExecutor = urlHandlerOverrideMethodExecutor;
	}

	public static string Serialize( TrustedUrl trustedUrl, string serializationAppId ) {
		var date = EwfRequest.Current!.RequestTime.InUtc().Date;
		var serializedUrl = EwfUrl.Serialize(
			trustedUrl.url,
			appId => appId.Equals( serializationAppId, StringComparison.Ordinal ) ? "" :
			         serializationAppId.Equals( EwfConfigurationStatics.AppConfiguration.PublicId, StringComparison.Ordinal ) ? appId :
			         throw new Exception(
				         "The application that is serializing the trusted URL has not initialized the URL-generation functionality of the application containing the resource." ),
			datePattern.Format( date ) );
		return serializedUrl + EwfUrl.AdditionalDataSeparator + getHmac( serializedUrl + serializationAppId, date );
	}

	public static TrustedUrl Deserialize( string serializedTrustedUrl, string serializationAppId ) {
		var hashIndex = serializedTrustedUrl.LastIndexOf( EwfUrl.AdditionalDataSeparator );
		if( hashIndex < 0 )
			throw new ArgumentException();
		var serializedUrl = serializedTrustedUrl[ ..hashIndex ];

		var date = datePattern.Parse( EwfUrl.Deserialize( serializedUrl, appId => appId.Length == 0 ? serializationAppId : appId, out var url ) ).GetValueOrThrow();
		if( !SystemSpecificLogicStatics.BestEffortDateIsValid( date ) ||
		    !getHmac( serializedUrl + serializationAppId, date ).Equals( serializedTrustedUrl[ ( hashIndex + 1 ).. ], StringComparison.Ordinal ) || url is null )
			return Invalid;

		if( url.IsExternal )
			return new TrustedUrl( new TrustedExternalResource( new ExternalResource( url.Url ) ), null );

		var handlers = urlResolverExecutor!( () => UrlHandlingStatics.GetUrlResolver( url.AppId )( url.BaseUrlString, url.AppRelativeUrl ) );
		return handlers?.Last() is TrustedResourceInfo resource
			       ? urlHandlerOverrideMethodExecutor!( handlers, () => new TrustedUrl( resource, null ) )
			       : new TrustedUrl( null, url );
	}

	private static string getHmac( string data, LocalDate date ) =>
		Convert.ToBase64String( SystemSpecificLogicStatics.GetBestEffortDataHasher( date ).ComputeHash( new UTF8Encoding( false ).GetBytes( data ) ) )
			.TrimEnd( '=' )
			.Replace( '+', '.' )
			.Replace( '/', '_' );

	private static int getNestedUrlDepth( WebItem? webItem ) =>
		webItem?.GetNestedUrls().Where( i => i is not null ).Select( i => i! ) is {} nestedUrls && nestedUrls.Any()
			? nestedUrls.Max( i => i.GetNestedUrlDepth() ) + 1
			: 0;

	internal readonly WebItem? WebItem;

	[ JsonProperty ]
	private readonly EwfUrl? url;

	internal TrustedUrl( WebItem? webItem, EwfUrl? invalidUrl ) {
		if( getNestedUrlDepth( webItem ) > 2 )
			webItem = EwfConfigurationStatics.GetDefaultBaseResource();

		WebItem = webItem;
		url = invalidUrl ?? WebItem?.GetEwfUrl( false, false );
	}

	public TrustedResourceInfo GetResourceOrThrow() => TryGetResource( out var resource ) ? resource : throw new InvalidOperationException( "invalid URL" );

	public bool TryGetResource( [ NotNullWhen( true ) ] out TrustedResourceInfo? resource ) {
		resource = WebItem as TrustedResourceInfo;
		return resource is not null;
	}

	internal EwfUrl? GetUrl() => url;

	int NestedUrl.GetNestedUrlDepth() => getNestedUrlDepth( WebItem );

	public override bool Equals( object? obj ) => Equals( obj as TrustedUrl );
	public bool Equals( TrustedUrl? other ) => other is not null && EwlStatics.AreEqual( url, other.url );
	public override int GetHashCode() => ( WebItem, url ).GetHashCode();
}

public static class TrustedUrlExtensionCreators {
	/// <summary>
	/// Creates a trusted URL for this resource.
	/// </summary>
	public static TrustedUrl ToTrustedUrl( this TrustedResourceInfo resource ) => new( resource, null );
}