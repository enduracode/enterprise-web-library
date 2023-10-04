using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace EnterpriseWebLibrary.TewlContrib;

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
		Policy.HandleInner<HttpRequestException>(
				e => e.InnerException is WebException webException && webException.Message.Contains( "The remote name could not be resolved" ) )
			.OrInner<TaskCanceledException>() // timeout
			.OrInner<HttpRequestException>(
				e => e.InnerException is WebException webException && webException.InnerException is SocketException socketException &&
				     socketException.Message.Contains( "No connection could be made because the target machine actively refused it" ) )
			.OrInner<HttpRequestException>( e => e.Message.Contains( "500 (Internal Server Error)" ) )
			.OrInner<HttpRequestException>( e => e.Message.Contains( "503 (Service Unavailable)" ) )
			.WaitAndRetry( 11, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
			.Execute(
				() => Task.Run(
						async () => {
							using var response = await client.GetAsync( url, HttpCompletionOption.ResponseHeadersRead );
							if( returnNullIfNotFound && response.StatusCode == HttpStatusCode.NotFound )
								return null;
							response.EnsureSuccessStatusCode();
							return await response.Content.ReadAsStringAsync();
						} )
					.Result );

	public static HttpContent GetRequestContentFromWriter( Action<Stream> bodyWriter ) => new WriterContent( bodyWriter );
}