using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A pattern that can generate and parse URL segments.
	/// </summary>
	public sealed class UrlPattern {
		/// <summary>
		/// Creates a two-way pattern for a single URL segment.
		/// </summary>
		/// <param name="generator">A function that takes an encoder and returns a segment, or null if the encoder does not match the pattern.</param>
		/// <param name="parser">A function that takes a segment and returns a decoder, or null if the segment does not match the pattern.</param>
		public static UrlPattern Create( Func<UrlEncoder, EncodingUrlSegment> generator, Func<DecodingUrlSegment, UrlDecoder> parser ) =>
			new UrlPattern( generator, parser );

		/// <summary>
		/// Creates a one-way pattern, which can parse multiple URL segments to support legacy URLs.
		/// </summary>
		/// <param name="parser">A function that takes a segment and returns a decoder, or null if the segment does not match the pattern.</param>
		public static UrlPattern CreateOneWay( Func<DecodingUrlSegment, UrlDecoder> parser ) => throw new NotImplementedException();

		internal readonly Func<UrlEncoder, EncodingUrlSegment> Generator;
		internal readonly Func<DecodingUrlSegment, UrlDecoder> Parser;

		private UrlPattern( Func<UrlEncoder, EncodingUrlSegment> generator, Func<DecodingUrlSegment, UrlDecoder> parser ) {
			Generator = generator;
			Parser = parser;
		}
	}
}