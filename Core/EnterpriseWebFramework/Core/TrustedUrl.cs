using System.Diagnostics.CodeAnalysis;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;
using NodaTime.Text;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core;

public sealed class TrustedUrl {
	private static readonly LocalDatePattern datePattern = LocalDatePattern.CreateWithInvariantCulture( "uuuuMMdd" );

	private static Func<Func<BasicUrlHandler>, BasicUrlHandler>? urlResolverExecutor;

	public static void Init( Func<Func<BasicUrlHandler>, BasicUrlHandler> urlResolverExecutor ) {
		TrustedUrl.urlResolverExecutor = urlResolverExecutor;
	}

	public static string Serialize( TrustedUrl trustedUrl ) {
		var url = trustedUrl.invalidUrl ?? trustedUrl.resource?.GetEwfUrl( false, false );
		var date = datePattern.Format( EwfRequest.Current!.RequestTime.InUtc().Date );
		return EwfUrl.Serialize( url, date );
	}

	public static TrustedUrl Deserialize( string serializedTrustedUrl ) {
		var date = datePattern.Parse( EwfUrl.Deserialize( serializedTrustedUrl, out var url ) );
		if( date.GetValueOrThrow() < SystemSpecificLogicStatics.BestEffortCutoffDate || url is null )
			return new TrustedUrl( null, null );

		if( url.IsExternal )
			return new TrustedUrl( new TrustedExternalResource( new ExternalResource( url.Url ) ), null );

		var resolvedHandler = urlResolverExecutor!( () => UrlHandlingStatics.GetUrlResolver()( url.BaseUrlString, url.AppRelativeUrl ) );
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
}

public static class TrustedUrlExtensionCreators {
	/// <summary>
	/// Creates a trusted URL for this resource.
	/// </summary>
	public static TrustedUrl ToTrustedUrl( this TrustedResourceInfo resource ) => new( resource, null );
}