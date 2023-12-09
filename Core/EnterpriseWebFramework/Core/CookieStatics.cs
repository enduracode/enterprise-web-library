#nullable disable
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class CookieStatics {
	// Remove after https://github.com/dotnet/aspnetcore/issues/52580 is resolved.
	internal const string EmptyValue = "EwlEmpty";

	private static Func<IReadOnlyCollection<( string, string, CookieOptions )>> responseCookieGetter;
	private static Action<string, string, CookieOptions> responseCookieAdder;

	internal static void Init(
		Func<IReadOnlyCollection<( string, string, CookieOptions )>> responseCookieGetter, Action<string, string, CookieOptions> responseCookieAdder ) {
		CookieStatics.responseCookieGetter = responseCookieGetter;
		CookieStatics.responseCookieAdder = responseCookieAdder;
	}

	/// <summary>
	/// Gets the value associated with the specified cookie name. Searches only the request.
	/// </summary>
	public static bool TryGetCookieValueFromRequestOnly( string name, out string value, bool omitNamePrefix = false ) {
		var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
		if( !EwfRequest.Current.AspNetRequest.Cookies.TryGetValue( ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name, out value ) )
			return false;
		if( string.Equals( value, EmptyValue ) )
			value = "";
		return true;
	}

	/// <summary>
	/// Gets the value associated with the specified cookie name, by first searching the response cookies and then searching the request. A null value represents
	/// a cookie that is being cleared. Throws an exception if multiple cookies with the specified name have been added to the response.
	/// </summary>
	public static bool TryGetCookieValueFromResponseOrRequest( string name, out string value, bool omitNamePrefix = false ) {
		var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
		var responseCookies = ResponseCookies
			.Where( i => string.Equals( i.name, ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name, StringComparison.Ordinal ) )
			.Materialize();
		if( responseCookies.Any() ) {
			value = responseCookies.Single().value;
			return true;
		}

		return TryGetCookieValueFromRequestOnly( name, out value, omitNamePrefix: omitNamePrefix );
	}

	/// <summary>
	/// Gets the cookies that have already been added to the response. A null value represents a cookie that is being cleared.
	/// </summary>
	public static IReadOnlyCollection<( string name, string value, CookieOptions options )> ResponseCookies => responseCookieGetter();

	public static void SetCookie(
		string name, string value, Instant? expires, bool secure, bool httpOnly, string domain = null, string path = null, bool omitNamePrefix = false ) {
		var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
		responseCookieAdder(
			nameAndDomainAndPath.Item1,
			value,
			new CookieOptions
				{
					Domain = nameAndDomainAndPath.Item2,
					Path = nameAndDomainAndPath.Item3,
					Expires = expires?.ToDateTimeUtc(),
					Secure = secure,
					HttpOnly = httpOnly,
					SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax
				} );
	}

	public static void ClearCookie( string name, string domain = null, string path = null, bool omitNamePrefix = false ) {
		var nameAndDomainAndPath = getNameAndDomainAndPath( name, domain, path, omitNamePrefix );
		responseCookieAdder( nameAndDomainAndPath.Item1, null, new CookieOptions { Domain = nameAndDomainAndPath.Item2, Path = nameAndDomainAndPath.Item3 } );
	}

	private static Tuple<string, string, string> getNameAndDomainAndPath( string name, string domain, string path, bool omitNamePrefix ) {
		var defaultAttributes = EwfConfigurationStatics.AppConfiguration.DefaultCookieAttributes;
		var defaultBaseUrl = new Uri( RequestState.Instance.BaseUrl );

		domain ??= defaultAttributes.Domain ?? "";

		// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root URL
		// of the application without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
		// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
		path ??= defaultAttributes.Path;
		path = path != null ? "/" + path : defaultBaseUrl.AbsolutePath;

		// Ensure that the domain and path of the cookie are in scope for both the request URL and page URL. These two URLs can be different on requests that
		// transfer to the log-in page, etc.
		var requestUrls = new List<string> { EwfRequest.Current.Url };
		if( PageBase.Current != null )
			requestUrls.Add( PageBase.Current.GetUrl( false, false ) );
		foreach( var url in requestUrls ) {
			var uri = new Uri( url );
			if( domain.Any() && !( "." + uri.Host ).EndsWith( "." + domain ) )
				throw new ApplicationException( "The cookie domain of \"{0}\" is not in scope for \"{1}\".".FormatWith( domain, url ) );
			if( path != "/" && !( uri.AbsolutePath + "/" ).StartsWith( path + "/" ) )
				throw new ApplicationException( "The cookie path of \"{0}\" is not in scope for \"{1}\".".FormatWith( path, url ) );
		}
		if( !domain.Any() ) {
			var requestHosts = requestUrls.Select( i => new Uri( i ).Host );
			if( requestHosts.Distinct().Count() > 1 )
				throw new ApplicationException(
					"The cookie domain could arbitrarily be either {0} depending upon the request URL.".FormatWith(
						StringTools.ConcatenateWithDelimiter( " or ", requestHosts.ToArray() ) ) );
		}

		return Tuple.Create( ( omitNamePrefix ? "" : defaultAttributes.NamePrefix ?? "" ) + name, domain, path );
	}
}