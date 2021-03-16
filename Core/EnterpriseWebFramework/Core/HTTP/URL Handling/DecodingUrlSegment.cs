namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class DecodingUrlSegment {
		private readonly string segment;
		private readonly DecodingUrlParameterCollection parameters;

		internal DecodingUrlSegment( string segment, DecodingUrlParameterCollection parameters ) {
			this.segment = segment;
			this.parameters = parameters;
		}

		/// <summary>
		/// Gets this segment’s parameters.
		/// </summary>
		public DecodingUrlParameterCollection Parameters => parameters;
	}
}