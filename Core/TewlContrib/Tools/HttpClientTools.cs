using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Polly;
using Tewl.IO;

namespace EnterpriseWebLibrary.TewlContrib;

[ PublicAPI ]
public static class HttpClientTools {
	private class WriterContent: HttpContent {
		private readonly Action<Stream> bodyWriter;

		public WriterContent( Action<Stream> bodyWriter ) {
			this.bodyWriter = bodyWriter;
		}

		protected override Task SerializeToStreamAsync( Stream stream, TransportContext? context ) {
			bodyWriter( stream );
			return Task.CompletedTask;
		}

		protected override Task SerializeToStreamAsync( Stream stream, TransportContext? context, CancellationToken cancellationToken ) =>
			SerializeToStreamAsync( stream, context );

		protected override void SerializeToStream( Stream stream, TransportContext? context, CancellationToken cancellationToken ) {
			bodyWriter( stream );
		}

		protected override bool TryComputeLength( out long length ) {
			length = 0;
			return false;
		}
	}

	public static string? GetTextWithRetry( this HttpClient client, string url, bool returnNullIfNotFound = false ) =>
		ExecuteRequestWithRetry(
			true,
			async () => {
				using var response = await client.GetAsync( url, HttpCompletionOption.ResponseHeadersRead );
				if( returnNullIfNotFound && response.StatusCode == HttpStatusCode.NotFound )
					return null;
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			} );

	/// <summary>
	/// Creates the destination path if it does not exist, and downloads the file to that destination path. Use only from a background process that can tolerate a
	/// long delay.
	/// </summary>
	public static void
		DownloadFileWithRetry( string sourceUrl, string destinationFilePath, NetworkCredential? credentials = null, string customAuthorizationHeaderValue = "" ) =>
		Policy.Handle<WebException>( e => e.Response is HttpWebResponse { StatusCode: HttpStatusCode.ServiceUnavailable } )
			.WaitAndRetry( 11, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
			.Execute(
				() => IoMethods.DownloadFile(
					sourceUrl,
					destinationFilePath,
					credentials: credentials,
					customAuthorizationHeaderValue: customAuthorizationHeaderValue ) );

	/// <summary>
	/// Executes a method that makes a request using <see cref="HttpClient"/>, retrying several times with exponential back-off in the event of network problems
	/// or transient failures on the server. Use only from a background process that can tolerate a long delay.
	/// </summary>
	public static void ExecuteRequestWithRetry(
		bool requestIsIdempotent, Func<Task> method, string additionalHandledMessage = "", Action? persistentFailureHandler = null ) {
		var policyBuilder = Policy.HandleInner<HttpRequestException>(
			e => e.InnerException is WebException webException && webException.Message.Contains( "The remote name could not be resolved" ) );

		if( requestIsIdempotent ) {
			policyBuilder.OrInner<TaskCanceledException>() // timeout
				.OrInner<HttpRequestException>(
					e => e.InnerException is WebException { InnerException: SocketException socketException } &&
					     socketException.Message.Contains( "No connection could be made because the target machine actively refused it" ) )
				.OrInner<HttpRequestException>( e => e.Message.Contains( "500 (Internal Server Error)" ) );

			if( additionalHandledMessage.Length > 0 )
				policyBuilder = policyBuilder.OrInner<HttpRequestException>( e => e.Message.Contains( additionalHandledMessage ) );
		}

		var result = policyBuilder.WaitAndRetry( 7, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
			.ExecuteAndCapture(
				() => Policy.HandleInner<HttpRequestException>( e => e.Message.Contains( "503 (Service Unavailable)" ) )
					.WaitAndRetry( 11, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
					.Execute( () => Task.Run( method ).Wait() ) );

		if( result.Outcome == OutcomeType.Successful )
			return;

		if( persistentFailureHandler is not null && result.ExceptionType == ExceptionType.HandledByThisPolicy )
			persistentFailureHandler();
		else
			throw result.FinalException;
	}

	/// <summary>
	/// Executes a method that makes a request using <see cref="HttpClient"/>, retrying several times with exponential back-off in the event of network problems
	/// or transient failures on the server. Use only from a background process that can tolerate a long delay.
	/// </summary>
	public static T ExecuteRequestWithRetry<T>(
		bool requestIsIdempotent, Func<Task<T>> method, string additionalHandledMessage = "", Action? persistentFailureHandler = null ) {
		T? result = default;
		ExecuteRequestWithRetry(
			requestIsIdempotent,
			async () => { result = await method(); },
			additionalHandledMessage: additionalHandledMessage,
			persistentFailureHandler: persistentFailureHandler );
		return result!;
	}

	public static HttpContent GetRequestContentFromWriter( Action<Stream> bodyWriter ) => new WriterContent( bodyWriter );
}