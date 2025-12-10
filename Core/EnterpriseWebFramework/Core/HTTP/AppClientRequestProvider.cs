using System.Net;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// Application-specific logic for the client request.
/// </summary>
public class AppClientRequestProvider {
	/// <summary>
	/// Returns true if the specified request is secure. Override this to be more than just <see cref="HttpRequest.IsHttps"/> if you are using a reverse proxy to
	/// perform SSL termination. Remember that your implementation should support not just live installations, but also development and intermediate
	/// installations. Use <see cref="HttpRequestExtensions.IsLocal(HttpRequest)"/> if you need different behavior for local requests.
	/// </summary>
	protected internal virtual bool RequestIsSecure( HttpRequest request ) => request.IsHttps;

	/// <summary>
	/// Returns the host name for the specified request. Override this if you are using a reverse proxy that is changing the Host header. Include the port number
	/// in the return value if it is not the default port. Never return null. If the host name is unavailable (i.e. the request uses HTTP 1.0 and does not include
	/// a Host header), return the empty string, which will cause a 400 status code to be returned. Remember that your implementation should support not just live
	/// installations, but also development and intermediate installations. Use <see cref="HttpRequestExtensions.IsLocal(HttpRequest)"/> if you need different
	/// behavior for local requests.
	/// </summary>
	protected internal virtual string GetRequestHost( HttpRequest request ) => request.Host.HasValue ? request.Host.Value : "";

	/// <summary>
	/// Returns the base path for the specified request. Override this if you are using a reverse proxy and are changing the base path. Never return null. Return
	/// the empty string to represent the root path. Remember that your implementation should support not just live installations, but also development and
	/// intermediate installations. Use <see cref="HttpRequestExtensions.IsLocal(HttpRequest)"/> if you need different behavior for local requests.
	/// </summary>
	protected internal virtual string GetRequestBasePath( HttpRequest request ) => request.PathBase.HasValue ? request.PathBase.ToUriComponent()[ 1.. ] : "";

	/// <summary>
	/// Returns the client IP address for the specified request. Override this if you are using a reverse proxy. Return a value with a null address only if the
	/// request is not on a TCP connection. If the client IP address is unavailable due to a missing header from the reverse proxy, return null, which will report
	/// an error to the developers and cause a 400 status code to be returned. Remember that your implementation should support not just live installations, but
	/// also development and intermediate installations. Use <see cref="HttpRequestExtensions.IsLocal(HttpRequest)"/> if you need different behavior for local
	/// requests.
	/// </summary>
	protected internal virtual SpecifiedValue<IPAddress?>? GetClientIp( HttpRequest request ) => new( request.HttpContext.Connection.RemoteIpAddress );
}

[ PublicAPI ]
public static class HttpRequestExtensions {
	public static bool IsLocal( this HttpRequest request ) =>
		// from https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/ and https://stackoverflow.com/a/78609181/35349
		request.HttpContext.Connection.RemoteIpAddress is {} ipAddress && IsLocal( ipAddress );

	internal static bool IsLocal( IPAddress clientIp ) => IPAddress.IsLoopback( clientIp );
}