using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;
using Newtonsoft.Json;
using NodaTime.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core;

public sealed class TrustedUrl: IEquatable<TrustedUrl> {
	/// <summary>
	/// Generated code use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public static readonly TrustedUrl Invalid = new( null, null );

	private static readonly LocalDatePattern datePattern = LocalDatePattern.CreateWithInvariantCulture( "uuuuMMdd" );

	private static Func<Func<BasicUrlHandler?>, BasicUrlHandler?>? urlResolverExecutor;

	public static void Init( Func<Func<BasicUrlHandler?>, BasicUrlHandler?> urlResolverExecutor ) {
		TrustedUrl.urlResolverExecutor = urlResolverExecutor;
	}

	public static string Serialize( TrustedUrl trustedUrl, string serializationAppId ) =>
		EwfUrl.Serialize(
			trustedUrl.url,
			appId => appId.Equals( serializationAppId, StringComparison.Ordinal ) ? "" :
			         serializationAppId.Equals( EwfConfigurationStatics.AppConfiguration.PublicId, StringComparison.Ordinal ) ? appId :
			         throw new Exception(
				         "The application that is serializing the trusted URL has not initialized the URL-generation functionality of the application containing the resource." ),
			datePattern.Format( EwfRequest.Current!.RequestTime.InUtc().Date ) );

	public static TrustedUrl Deserialize( string serializedTrustedUrl, string serializationAppId ) {
		var date = datePattern.Parse( EwfUrl.Deserialize( serializedTrustedUrl, appId => appId.Length == 0 ? serializationAppId : appId, out var url ) );
		if( date.GetValueOrThrow() < SystemSpecificLogicStatics.BestEffortCutoffDate || url is null )
			return Invalid;

		if( url.IsExternal )
			return new TrustedUrl( new TrustedExternalResource( new ExternalResource( url.Url ) ), null );

		var resolvedHandler = urlResolverExecutor!( () => UrlHandlingStatics.GetUrlResolver( url.AppId )( url.BaseUrlString, url.AppRelativeUrl ) );
		return resolvedHandler is TrustedResourceInfo resource ? new TrustedUrl( resource, null ) : new TrustedUrl( null, url );
	}

	private readonly TrustedResourceInfo? resource;
	private readonly EwfUrl? invalidUrl;

	internal TrustedUrl( TrustedResourceInfo? resource, EwfUrl? invalidUrl ) {
		this.resource = resource;
		this.invalidUrl = invalidUrl;
	}

	public bool TryGetResource( [ NotNullWhen( true ) ] out TrustedResourceInfo? resource ) {
		resource = this.resource;
		return resource is not null;
	}

	[ JsonProperty ]
	private EwfUrl? url => invalidUrl ?? resource?.GetEwfUrl( false, false );

	public override bool Equals( object? obj ) => Equals( obj as TrustedUrl );
	public bool Equals( TrustedUrl? other ) => other is not null && EwlStatics.AreEqual( url, other.url );
	public override int GetHashCode() => ( resource, invalidUrl ).GetHashCode();
}

public static class TrustedUrlExtensionCreators {
	/// <summary>
	/// Creates a trusted URL for this resource.
	/// </summary>
	public static TrustedUrl ToTrustedUrl( this TrustedResourceInfo resource ) => new( resource, null );
}