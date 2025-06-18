using System.Net;
using System.Text;
using System.Threading.Tasks;
using EnterpriseWebLibrary.SystemSpecificLogic;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public class EwfRequest {
	private static AppClientRequestProvider? defaultProvider;
	private static SystemProviderReference<AppClientRequestProvider>? provider;
	private static Func<HttpRequest>? currentRequestGetter;
	private static Func<Instant>? requestTimeGetter;
	private static Func<string>? urlGetter;
	private static Action<Duration>? networkWaitTimeAdder;

	internal static void Init(
		SystemProviderReference<AppClientRequestProvider> provider, Func<HttpRequest> currentRequestGetter, Func<Instant>? requestTimeGetter,
		Func<string>? urlGetter, Action<Duration> networkWaitTimeAdder ) {
		defaultProvider = new AppClientRequestProvider();
		EwfRequest.provider = provider;
		EwfRequest.currentRequestGetter = currentRequestGetter;
		EwfRequest.requestTimeGetter = requestTimeGetter;
		EwfRequest.urlGetter = urlGetter;
		EwfRequest.networkWaitTimeAdder = networkWaitTimeAdder;
	}

	internal static AppClientRequestProvider AppProvider => provider!.GetProvider( returnNullIfNotFound: true ) ?? defaultProvider!;

	/// <summary>
	/// Gets the current request, or null if called outside of a request or from a non-web application.
	/// </summary>
	public static EwfRequest? Current {
		get {
			var request = currentRequestGetter?.Invoke();
			return request is not null ? new EwfRequest( request ) : null;
		}
	}

	internal readonly HttpRequest AspNetRequest;

	private EwfRequest( HttpRequest aspNetRequest ) {
		AspNetRequest = aspNetRequest;
	}

	/// <summary>
	/// Gets the time instant for the current request.
	/// </summary>
	public Instant RequestTime => requestTimeGetter!();

	/// <summary>
	/// This is the absolute URL for the request. Absolute means the entire URL, including the scheme, host, path, and query string. Use with caution, as this may
	/// not be a normalized URL. You likely should call PageBase.Current.GetUrl instead.
	/// </summary>
	public string Url => urlGetter!();

	/// <summary>
	/// Returns true if this request is secure.
	/// </summary>
	public bool IsSecure => AppProvider.RequestIsSecure( AspNetRequest );

	/// <summary>
	/// Gets the request headers.
	/// </summary>
	public IHeaderDictionary Headers => AspNetRequest.Headers;

	/// <summary>
	/// Parses the request body as a form and returns the values.
	/// </summary>
	public IFormCollection GetFormSubmission() {
		var requestBodyReadBeginTime = Clock.GetCurrentTime();
		var formSubmission = Task.Run( async () => await AspNetRequest.ReadFormAsync() ).Result;
		networkWaitTimeAdder!( Clock.GetCurrentTime() - requestBodyReadBeginTime );

		return formSubmission;
	}

	/// <summary>
	/// Executes a method that reads a text request body.
	/// </summary>
	public void ExecuteWithBodyReader( Action<TextReader> method ) {
		using var reader = new StreamReader(
			AspNetRequest.Body,
			encoding: AspNetRequest.GetTypedHeaders().ContentType!.Encoding ?? Encoding.UTF8,
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
	/// Gets the client IP address, or null if the request is not on a TCP connection.
	/// </summary>
	public IPAddress? ClientIp => AppProvider.GetClientIp( AspNetRequest );

	/// <summary>
	/// Gets whether the request is from the local computer.
	/// </summary>
	internal bool IsLocal {
		get {
			// From https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core/

			var clientIp = ClientIp;
			var connection = AspNetRequest.HttpContext.Connection;
			if( clientIp is not null )
				return connection.LocalIpAddress is not null ? clientIp.Equals( connection.LocalIpAddress ) : IPAddress.IsLoopback( clientIp );

			// for in memory TestServer or when dealing with default connection info
			if( clientIp is null && connection.LocalIpAddress is null )
				return true;

			return false;
		}
	}
}