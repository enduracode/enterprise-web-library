using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseWebLibrary.TewlContrib {
	public static class HttpClientTools {
		private class WriterContent: HttpContent {
			private readonly Action<Stream> bodyWriter;

			public WriterContent( Action<Stream> bodyWriter ) {
				this.bodyWriter = bodyWriter;
			}

			protected override Task SerializeToStreamAsync( Stream stream, TransportContext context ) {
				bodyWriter( stream );
				return Task.CompletedTask;
			}

			protected override Task SerializeToStreamAsync( Stream stream, TransportContext context, CancellationToken cancellationToken ) {
				return SerializeToStreamAsync( stream, context );
			}

			protected override void SerializeToStream( Stream stream, TransportContext context, CancellationToken cancellationToken ) {
				bodyWriter( stream );
			}

			protected override bool TryComputeLength( out long length ) {
				length = 0;
				return false;
			}
		}

		public static HttpContent GetRequestContentFromWriter( Action<Stream> bodyWriter ) => new WriterContent( bodyWriter );
	}
}