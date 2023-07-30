#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A pattern that can generate and parse URL segments.
/// </summary>
public sealed class UrlPattern {
	internal readonly Func<UrlEncoder, EncodingUrlSegment?> Generator;
	internal readonly Func<DecodingUrlSegment, UrlDecoder?> Parser;

	/// <summary>
	/// Creates a two-way pattern for a URL segment.
	/// </summary>
	/// <param name="generator">A function that takes an encoder and returns a segment, or null if the encoder does not match the pattern.</param>
	/// <param name="parser">A function that takes a segment and returns a decoder, or null if the segment does not match the pattern.</param>
	public UrlPattern( Func<UrlEncoder, EncodingUrlSegment?> generator, Func<DecodingUrlSegment, UrlDecoder?> parser ) {
		Generator = generator;
		Parser = parser;
	}
}