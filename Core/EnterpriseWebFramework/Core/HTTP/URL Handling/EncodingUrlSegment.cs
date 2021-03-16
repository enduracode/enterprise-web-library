namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class EncodingUrlSegment {
		internal readonly string Segment;
		internal readonly EncodingUrlParameterCollection Parameters;

		private EncodingUrlSegment( string segment, EncodingUrlParameterCollection parameters ) {
			Segment = segment;
			Parameters = parameters ?? new EncodingUrlParameterCollection();
		}
	}
}