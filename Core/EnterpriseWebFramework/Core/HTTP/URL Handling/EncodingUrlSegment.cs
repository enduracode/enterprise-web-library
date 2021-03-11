using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class EncodingUrlSegment {
		private readonly string segment;
		private readonly IEnumerable<( string name, string value )> segmentParameters;
		private readonly IEnumerable<( string name, string value )> queryParameters;

		private EncodingUrlSegment(
			string segment, IEnumerable<( string name, string value )> segmentParameters, IEnumerable<( string name, string value )> queryParameters ) {
			this.segment = segment;
			this.segmentParameters = ( segmentParameters ?? Enumerable.Empty<( string, string )>() ).Materialize();
			this.queryParameters = ( queryParameters ?? Enumerable.Empty<( string, string )>() ).Materialize();
		}

		internal ( string segment, IEnumerable<( string name, string value )> segmentParameters, IEnumerable<( string name, string value )> queryParameters )
			GetComponents( UrlEncoder urlEncoder ) {
			var remainingParameters = urlEncoder.GetRemainingParameters().Materialize();
			var specifiedParameters = segmentParameters.Concat( queryParameters ).Select( i => i.name ).ToImmutableHashSet();
			foreach( var i in remainingParameters )
				if( specifiedParameters.Contains( i.name ) )
					throw new ApplicationException( "The {0} parameter was already specified by the generator.".FormatWith( i.name ) );

			return ( segment, segmentParameters.Concat( remainingParameters.Where( i => i.isSegmentParameter ).Select( i => ( i.name, i.value ) ) ),
				       queryParameters.Concat( remainingParameters.Where( i => !i.isSegmentParameter ).Select( i => ( i.name, i.value ) ) ) );
		}
	}
}