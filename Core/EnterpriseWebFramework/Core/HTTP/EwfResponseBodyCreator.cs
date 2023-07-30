﻿#nullable disable
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An object that creates an HTTP response body.
	/// </summary>
	public class EwfResponseBodyCreator {
		internal static readonly Encoding TextEncoding = Encoding.UTF8;

		internal readonly Func<string> TextBodyCreator;
		internal readonly Func<byte[]> BinaryBodyCreator;
		internal readonly Action<TextWriter> TextBodyWriter;
		internal readonly Action<Stream> BinaryBodyWriter;

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
				stream.Write( body, 0, body.Length );
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
				var textBody = TextBodyCreator();
				return new EwfResponseBodyCreator( () => textBody );
			}
			var binaryBody = BinaryBodyCreator();
			return new EwfResponseBodyCreator( () => binaryBody );
		}

		internal void WriteToResponse( HttpResponse response ) {
			if( BodyIsText )
				using( var writer = new HttpResponseStreamWriter( response.Body, TextEncoding ) )
					TextBodyWriter( writer );
			else
				BinaryBodyWriter( response.Body );
		}
	}
}