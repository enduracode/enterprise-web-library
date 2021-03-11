using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class DecodingUrlSegment {
		private readonly string segment;
		private readonly ILookup<string, string> parameters;
		private readonly HashSet<string> accessedParameters;

		internal DecodingUrlSegment(
			string segment, IReadOnlyCollection<( string name, string value )> segmentParameters,
			IReadOnlyCollection<( string name, string value )> queryParameters ) {
			this.segment = segment;
			parameters = segmentParameters.Concat( queryParameters ).ToLookup( i => i.name, i => i.value );
			accessedParameters = new HashSet<string>( parameters.Count );
		}

		/// <summary>
		/// Returns the value of the parameter with the specified name, or null if the parameter is not present.
		/// </summary>
		/// <param name="name">Do not pass null or the empty string.</param>
		public string GetParameter( string name ) {
			accessedParameters.Add( name );
			return getParameter( name );
		}

		internal string GetRemainingParameter( string name ) {
			if( accessedParameters.Contains( name ) )
				throw new ApplicationException( "The {0} parameter was already accessed by the parser.".FormatWith( name ) );
			return getParameter( name );
		}

		private string getParameter( string name ) {
			var matches = parameters[ name ].Materialize();
			return matches.Count > 1
				       ? throw new ResourceNotAvailableException( "Multiple {0} parameters exist.".FormatWith( name ), null )
				       : matches.SingleOrDefault();
		}
	}
}