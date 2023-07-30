#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class EncodingUrlParameterCollection {
		private readonly IReadOnlyCollection<( string name, string value )> segmentParameters;
		private readonly IReadOnlyCollection<( string name, string value )> queryParameters;

		/// <summary>
		/// Creates a parameter collection.
		/// </summary>
		/// <param name="segmentParameters">The parameters that will be included in the handler’s own URL as well as the URLs of its descendants.</param>
		/// <param name="queryParameters">The parameters that will be included in the handler’s own URL but not the URLs of its descendants.</param>
		public EncodingUrlParameterCollection(
			IEnumerable<( string name, string value )> segmentParameters = null, IEnumerable<( string name, string value )> queryParameters = null ) {
			this.segmentParameters = ( segmentParameters ?? Enumerable.Empty<( string, string )>() ).Materialize();
			this.queryParameters = ( queryParameters ?? Enumerable.Empty<( string, string )>() ).Materialize();
		}

		internal ( IEnumerable<( string name, string value )> segmentParameters, IEnumerable<( string name, string value )> queryParameters ) Get(
			UrlEncoder urlEncoder ) {
			var remainingParameters = urlEncoder.GetRemainingParameters().Materialize();
			var specifiedParameters = segmentParameters.Concat( queryParameters ).Select( i => i.name ).ToImmutableHashSet();
			foreach( var i in remainingParameters )
				if( specifiedParameters.Contains( i.name ) )
					throw new ApplicationException( "The {0} parameter was already specified by the generator.".FormatWith( i.name ) );

			return ( segmentParameters.Concat( remainingParameters.Where( i => i.isSegmentParameter ).Select( i => ( i.name, i.value ) ) ),
				       queryParameters.Concat( remainingParameters.Where( i => !i.isSegmentParameter ).Select( i => ( i.name, i.value ) ) ) );
		}
	}
}