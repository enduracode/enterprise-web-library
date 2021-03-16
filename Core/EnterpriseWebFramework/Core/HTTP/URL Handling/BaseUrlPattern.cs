using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A pattern that can generate and parse application base URLs.
	/// </summary>
	public sealed class BaseUrlPattern {
		internal readonly Func<UrlEncoder, EncodingBaseUrl> Generator;
		internal readonly Func<DecodingBaseUrl, UrlDecoder> Parser;

		/// <summary>
		/// Creates a two-way base URL pattern.
		/// </summary>
		/// <param name="generator">A function that takes an encoder and returns a base URL, or null if the encoder does not match the pattern.</param>
		/// <param name="parser">A function that takes a base URL and returns a decoder, or null if the base URL does not match the pattern.</param>
		public BaseUrlPattern( Func<UrlEncoder, EncodingBaseUrl> generator, Func<DecodingBaseUrl, UrlDecoder> parser ) {
			Generator = generator;
			Parser = parser;
		}
	}
}