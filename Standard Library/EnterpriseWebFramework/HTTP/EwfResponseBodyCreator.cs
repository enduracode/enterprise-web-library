using System;
using System.IO;
using System.Web;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that creates an HTTP response body.
	/// </summary>
	public class EwfResponseBodyCreator {
		internal readonly Action<TextWriter> TextBodyWriter;
		internal readonly Action<Stream> BinaryBodyWriter;

		/// <summary>
		/// Creates a response-body creator with a method that returns a text body.
		/// </summary>
		public EwfResponseBodyCreator( Func<string> textBodyCreator ) {
			TextBodyWriter = textWriter => {
				var body = textBodyCreator();
				textWriter.Write( body );
			};
		}

		/// <summary>
		/// Creates a response-body creator with a method that returns a binary body.
		/// </summary>
		public EwfResponseBodyCreator( Func<byte[]> binaryBodyCreator ) {
			BinaryBodyWriter = stream => {
				var body = binaryBodyCreator();
				stream.Write( body, 0, body.Length );
			};
		}

		/// <summary>
		/// Creates a response-body creator with a method that writes a text body to a writer.
		/// </summary>
		public EwfResponseBodyCreator( Action<TextWriter> textBodyWriter ) {
			TextBodyWriter = textBodyWriter;
		}

		/// <summary>
		/// Creates a response-body creator with a method that writes a binary body to a stream.
		/// </summary>
		public EwfResponseBodyCreator( Action<Stream> binaryBodyWriter ) {
			BinaryBodyWriter = binaryBodyWriter;
		}

		internal void WriteToResponse( HttpResponse response ) {
			if( TextBodyWriter != null )
				TextBodyWriter( response.Output );
			else
				BinaryBodyWriter( response.OutputStream );
		}
	}
}