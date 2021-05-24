using System;
using System.Globalization;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class EncodingUrlSegment {
		/// <summary>
		/// Creates a URL segment.
		/// </summary>
		/// <param name="segment">Do not pass null.</param>
		/// <param name="parameters"></param>
		public static EncodingUrlSegment Create( string segment, EncodingUrlParameterCollection parameters = null ) =>
			new EncodingUrlSegment( segment, parameters );

		/// <summary>
		/// Creates a URL segment with a version string.
		/// </summary>
		/// <param name="segment">Do not pass null.</param>
		/// <param name="versionString">Do not pass null or the empty string. Must contain only alphanumeric characters.</param>
		/// <param name="parameters"></param>
		public static EncodingUrlSegment CreateWithVersionString( string segment, string versionString, EncodingUrlParameterCollection parameters = null ) =>
			versionString.Length > 0 && versionString == versionString.RemoveNonAlphanumericCharacters()
				? new EncodingUrlSegment( "{0}--v{1}".FormatWith( segment, versionString ), parameters )
				: throw new ArgumentOutOfRangeException( nameof(versionString) );

		/// <summary>
		/// Creates a URL segment from a positive int.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="parameters"></param>
		public static EncodingUrlSegment CreatePositiveInt( int value, EncodingUrlParameterCollection parameters = null ) =>
			value >= 1
				? new EncodingUrlSegment( value.ToString( "D", CultureInfo.InvariantCulture ), parameters )
				: throw new ArgumentOutOfRangeException( nameof(value) );

		internal readonly string Segment;
		internal readonly EncodingUrlParameterCollection Parameters;

		private EncodingUrlSegment( string segment, EncodingUrlParameterCollection parameters ) {
			Segment = segment;
			Parameters = parameters ?? new EncodingUrlParameterCollection();
		}
	}
}