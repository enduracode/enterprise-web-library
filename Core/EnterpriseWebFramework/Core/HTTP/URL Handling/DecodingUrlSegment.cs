using System.Globalization;
using System.Text.RegularExpressions;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class DecodingUrlSegment {
		private readonly string segment;
		private readonly DecodingUrlParameterCollection parameters;

		internal DecodingUrlSegment( string segment, DecodingUrlParameterCollection parameters ) {
			this.segment = segment;
			this.parameters = parameters;
		}

		/// <summary>
		/// Gets this segment.
		/// </summary>
		public string Segment => segment;

		/// <summary>
		/// Returns whether the segment has a version string.
		/// </summary>
		public bool HasVersionString( out ( string segment, string versionString ) components ) {
			var match = Regex.Match( segment, @"^(?<segment>.*)--v(?<version>[A-Za-z0-9]+)\z" );
			if( !match.Success ) {
				components = default( ( string, string ) );
				return false;
			}
			components = ( match.Groups[ "segment" ].Value, match.Groups[ "version" ].Value );
			return true;
		}

		/// <summary>
		/// Returns whether the segment is a positive int.
		/// </summary>
		public bool IsPositiveInt( out int value ) => int.TryParse( segment, NumberStyles.None, CultureInfo.InvariantCulture, out value ) && value >= 1;

		/// <summary>
		/// Gets this segment’s parameters.
		/// </summary>
		public DecodingUrlParameterCollection Parameters => parameters;
	}
}