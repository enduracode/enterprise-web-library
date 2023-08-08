using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An object that creates an HTTP response body.
/// </summary>
public class EwfResponseBodyCreator {
	internal static readonly Encoding TextEncoding = Encoding.UTF8;

	internal readonly Func<string>? TextBodyCreator;
	internal readonly Func<byte[]>? BinaryBodyCreator;
	internal readonly Action<TextWriter>? TextBodyWriter;
	internal readonly Action<Stream>? BinaryBodyWriter;

	/// <summary>
	/// Creates a response-body creator with a method that returns a text body.
	/// </summary>
	public EwfResponseBodyCreator( Func<string> textBodyCreator ) {
		TextBodyCreator = textBodyCreator;
		TextBodyWriter = textWriter => {
			var body = textBodyCreator();
			textWriter.Write( body );
		};
	}

	/// <summary>
	/// Creates a response-body creator with a method that returns a binary body.
	/// </summary>
	public EwfResponseBodyCreator( Func<byte[]> binaryBodyCreator ) {
		BinaryBodyCreator = binaryBodyCreator;
		BinaryBodyWriter = stream => {
			var body = binaryBodyCreator();

			// On 8 August 2023 we encountered an IIS-related issue in which writing more than 256 MiB to the response stream in a single call fails silently,
			// resulting in no data being added to the HTTP response.
			const int bufferSize = 100000000;
			for( var i = 0; i < body.Length; i += bufferSize )
				stream.Write( body, i, Math.Min( body.Length - i, bufferSize ) );
		};
	}

	/// <summary>
	/// Creates a response-body creator with a method that writes a text body to a writer.
	/// </summary>
	public EwfResponseBodyCreator( Action<TextWriter> textBodyWriter ) {
		TextBodyCreator = () => {
			using var writer = new StringWriter();
			textBodyWriter( writer );
			return writer.ToString();
		};
		TextBodyWriter = textBodyWriter;
	}

	/// <summary>
	/// Creates a response-body creator with a method that writes a binary body to a stream.
	/// </summary>
	public EwfResponseBodyCreator( Action<Stream> binaryBodyWriter ) {
		BinaryBodyCreator = () => {
			using var stream = new MemoryStream();
			binaryBodyWriter( stream );
			return stream.ToArray();
		};
		BinaryBodyWriter = binaryBodyWriter;
	}

	internal bool BodyIsText => TextBodyCreator != null;

	internal EwfResponseBodyCreator GetBufferedBodyCreator() {
		if( BodyIsText ) {
			var textBody = TextBodyCreator!();
			return new EwfResponseBodyCreator( () => textBody );
		}
		var binaryBody = BinaryBodyCreator!();
		return new EwfResponseBodyCreator( () => binaryBody );
	}

	internal void WriteToResponse( HttpResponse response ) {
		if( BodyIsText )
			using( var writer = new HttpResponseStreamWriter( response.Body, TextEncoding ) )
				TextBodyWriter!( writer );
		else
			BinaryBodyWriter!( response.Body );
	}
}