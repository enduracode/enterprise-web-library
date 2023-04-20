using System.Net;
using System.Text;
using System.Threading.Tasks;
using EnterpriseWebLibrary.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class EwfRequest {
	private static AppRequestBaseUrlProvider baseUrlDefaultProvider;
	private static SystemProviderReference<AppRequestBaseUrlProvider> baseUrlProvider;
	private static Func<HttpRequest> currentRequestGetter;
	private static Action<Duration> networkWaitTimeAdder;

	internal static void Init(
		SystemProviderReference<AppRequestBaseUrlProvider> baseUrlProvider, Func<HttpRequest> currentRequestGetter, Action<Duration> networkWaitTimeAdder ) {
		baseUrlDefaultProvider = new AppRequestBaseUrlProvider();
		EwfRequest.baseUrlProvider = baseUrlProvider;
		EwfRequest.currentRequestGetter = currentRequestGetter;
		EwfRequest.networkWaitTimeAdder = networkWaitTimeAdder;
	}

	internal static AppRequestBaseUrlProvider AppBaseUrlProvider => baseUrlProvider.GetProvider( returnNullIfNotFound: true ) ?? baseUrlDefaultProvider;

	/// <summary>
	/// Gets the current request, or null if called outside of a request or from a non-web application.
	/// </summary>
	public static EwfRequest Current {
		get {
			var request = currentRequestGetter?.Invoke();
			return request != null ? new EwfRequest( request ) : null;
		}
	}

	internal readonly HttpRequest AspNetRequest;

	private EwfRequest( HttpRequest aspNetRequest ) {
		AspNetRequest = aspNetRequest;
	}

	/// <summary>
	/// Returns true if this request is secure.
	/// </summary>
	public bool IsSecure => AppBaseUrlProvider.RequestIsSecure( AspNetRequest );

	/// <summary>
	/// Gets the request headers.
	/// </summary>
	public IHeaderDictionary Headers => AspNetRequest.Headers;

	/// <summary>
	/// Parses the request body as a form and returns the values.
	/// </summary>
	public IFormCollection GetFormSubmission() {
		var requestBodyReadBeginTime = SystemClock.Instance.GetCurrentInstant();
		var formSubmission = Task.Run( async () => await AspNetRequest.ReadFormAsync() ).Result;
		networkWaitTimeAdder( SystemClock.Instance.GetCurrentInstant() - requestBodyReadBeginTime );

		return formSubmission;
	}

	/// <summary>
	/// Executes a method that reads a text request body.
	/// </summary>
	public void ExecuteWithBodyReader( Action<TextReader> method ) {
		using var reader = new StreamReader(
			AspNetRequest.Body,
			encoding: AspNetRequest.GetTypedHeaders().ContentType.Encoding ?? Encoding.UTF8,
			detectEncodingFromByteOrderMarks: false,
			leaveOpen: true );
		method( reader );
	}

	/// <summary>
	/// Executes a method that reads a binary request body.
	/// </summary>
	public void ExecuteWithBodyStream( Action<Stream> method ) {
		method( AspNetRequest.Body );
	}

	/// <summary>
	/// Gets whether the request is from the local computer.
	/// </summary>
	internal bool IsLocal {
		get {
			// From https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/

			var connection = AspNetRequest.HttpContext.Connection;
			if( connection.RemoteIpAddress != null )
				return connection.LocalIpAddress != null
					       ? connection.RemoteIpAddress.Equals( connection.LocalIpAddress )
					       : IPAddress.IsLoopback( connection.RemoteIpAddress );

			// for in memory TestServer or when dealing with default connection info
			if( connection.RemoteIpAddress == null && connection.LocalIpAddress == null )
				return true;

			return false;
		}
	}
}