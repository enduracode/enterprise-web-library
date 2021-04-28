using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using EnterpriseWebLibrary.TewlContrib;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal static class UrlHandlingStatics {
		private static Func<string, string, BasicUrlHandler> urlResolver;

		internal static void Init( Func<string, string, BasicUrlHandler> urlResolver ) {
			UrlHandlingStatics.urlResolver = urlResolver;
		}

		internal static string GetCanonicalUrl( BasicUrlHandler basicHandler, bool secure ) {
			UrlHandler parent = null;
			if( basicHandler is UrlHandler handler ) {
				parent = handler.GetParent();
				if( parent != null ) {
					var pair = parent.GetCanonicalHandlerPair( handler );
					parent = pair.parent;
					basicHandler = pair.child;
				}
			}
			var encoder = basicHandler.GetEncoder();

			var segments = new List<string>();
			string query = null;
			( IEnumerable<( string name, string value )> segmentParameters, IEnumerable<( string name, string value )> queryParameters ) parameters;
			while( parent != null ) {
				EncodingUrlSegment segment = null;
				foreach( var i in parent.GetChildPatterns() ) {
					segment = i.Generator( encoder );
					if( segment != null )
						break;
				}
				if( segment == null )
					throw new ApplicationException( "The handler does not match any of the parent’s child URL patterns." );
				if( segment.Segment.Length == 0 )
					throw new ApplicationException( "The segment must not be the empty string." );
				parameters = segment.Parameters.Get( encoder );
				segments.Add( generateSegment( segment.Segment, generateSegmentParameters( parameters.segmentParameters ) ) );
				if( query == null )
					query = generateQuery( parameters.queryParameters );

				encoder = parent.GetEncoder();
				parent = parent.GetParent();
			}

			EncodingBaseUrl baseUrl = null;
			foreach( var i in EwfApp.Instance.GetBaseUrlPatterns() ) {
				baseUrl = i.Generator( encoder );
				if( baseUrl != null )
					break;
			}
			if( baseUrl == null )
				throw new ApplicationException( "The handler does not match any of the base URL patterns for the application." );
			parameters = baseUrl.Parameters.Get( encoder );
			var baseUrlParameters = generateSegmentParameters( parameters.segmentParameters );
			if( query == null )
				query = generateQuery( parameters.queryParameters );

			var baseUrlString = baseUrl.BaseUrl.GetUrlString( secure );
			var path = generatePath( baseUrlParameters, segments );
			var appRelativeUrl = generateAppRelativeUrl( path, query );

			var resolvedHandler = urlResolver( baseUrlString, appRelativeUrl );
			if( !EwlStatics.AreEqual( resolvedHandler, basicHandler ) )
				throw new ApplicationException( "The handler’s canonical URL does not resolve back to the same handler." );

			return baseUrlString + ( path.Length > 0 ? "/" : "" ) + appRelativeUrl;
		}

		internal static IReadOnlyCollection<BasicUrlHandler> ResolveUrl( string baseUrlString, string appRelativeUrl ) {
			var handlers = new List<BasicUrlHandler>();

			var urlComponents = parseAppRelativeUrl( appRelativeUrl );
			var pathComponents = parsePath( urlComponents.path );

			var baseUrlComponents = BaseUrl.GetComponents( baseUrlString );
			var baseUrl = new DecodingBaseUrl(
				baseUrlComponents.secure,
				baseUrlComponents.host,
				baseUrlComponents.port,
				baseUrlComponents.path,
				new DecodingUrlParameterCollection(
					parseSegmentParameters( pathComponents.baseUrlParameters ),
					pathComponents.segments.Any() ? Enumerable.Empty<( string, string )>() : parseQuery( urlComponents.query ) ) );
			UrlDecoder decoder = null;
			foreach( var i in EwfApp.Instance.GetBaseUrlPatterns() ) {
				decoder = i.Parser( baseUrl );
				if( decoder != null )
					break;
			}
			if( decoder == null )
				return null;
			var basicHandler = decoder.GetUrlHandler( baseUrl.Parameters );
			handlers.Add( basicHandler );

			for( var segmentIndex = 0; segmentIndex < pathComponents.segments.Count; segmentIndex += 1 ) {
				if( !( basicHandler is UrlHandler handler ) )
					return null;

				var segmentComponents = parseSegment( pathComponents.segments[ segmentIndex ] );
				if( segmentComponents.segment.Length == 0 )
					return null;
				var segment = new DecodingUrlSegment(
					segmentComponents.segment,
					new DecodingUrlParameterCollection(
						parseSegmentParameters( segmentComponents.parameters ),
						segmentIndex < pathComponents.segments.Count - 1 ? Enumerable.Empty<( string, string )>() : parseQuery( urlComponents.query ) ) );
				decoder = null;
				foreach( var i in handler.GetChildPatterns() ) {
					decoder = i.Parser( segment );
					if( decoder != null )
						break;
				}
				if( decoder == null )
					return null;
				basicHandler = decoder.GetUrlHandler( segment.Parameters );
				handlers.Add( basicHandler );
			}

			return handlers;
		}

		private static string generateAppRelativeUrl( string path, string query ) => path + query.PrependDelimiter( "?" );

		private static ( string path, string query ) parseAppRelativeUrl( string appRelativeUrl ) {
			var queryIndex = appRelativeUrl.IndexOf( '?' );
			return queryIndex >= 0 ? ( appRelativeUrl.Remove( queryIndex ), appRelativeUrl.Substring( queryIndex + 1 ) ) : ( appRelativeUrl, "" );
		}

		private static string generatePath( string baseUrlParameters, IEnumerable<string> segments ) =>
			StringTools.ConcatenateWithDelimiter( "/", baseUrlParameters.PrependDelimiter( ";" ).ToCollection().Concat( segments ) );

		private static ( string baseUrlParameters, IReadOnlyList<string> segments ) parsePath( string path ) {
			if( !path.Any() )
				return ( "", Enumerable.Empty<string>().MaterializeAsList() );
			var segments = path.Separate( "/", false );
			var firstSegment = segments.First();
			return firstSegment[ 0 ] == ';' ? ( firstSegment.Substring( 1 ), segments.Skip( 1 ).MaterializeAsList() ) : ( "", segments );
		}

		private static string generateSegment( string segment, string parameters ) => segment + parameters.PrependDelimiter( ";" );

		private static ( string segment, string parameters ) parseSegment( string segment ) {
			var semicolonIndex = segment.IndexOf( ';' );
			return semicolonIndex >= 0 ? ( segment.Remove( semicolonIndex ), segment.Substring( semicolonIndex + 1 ) ) : ( segment, "" );
		}

		private static string generateSegmentParameters( IEnumerable<( string name, string value )> parameters ) {
			parameters = parameters.Materialize();
			if( parameters.Any( i => i.name.Length == 0 ) )
				throw new ApplicationException( "The parameter must have a name." );
			return StringTools.ConcatenateWithDelimiter( ";", from i in parameters select WebUtility.UrlEncode( i.name ) + '=' + WebUtility.UrlEncode( i.value ) );
		}

		private static IEnumerable<( string name, string value )> parseSegmentParameters( string segmentParameters ) =>
			from parameter in segmentParameters.Separate( ";", false )
			let equalsIndex = parameter.IndexOf( '=' )
			where equalsIndex > 0
			select ( WebUtility.UrlDecode( parameter.Remove( equalsIndex ) ), WebUtility.UrlDecode( parameter.Substring( equalsIndex + 1 ) ) );

		private static string generateQuery( IEnumerable<( string name, string value )> parameters ) {
			parameters = parameters.Materialize();
			if( parameters.Any( i => i.name.Length == 0 ) )
				throw new ApplicationException( "The parameter must have a name." );
			return StringTools.ConcatenateWithDelimiter( "&", from i in parameters select WebUtility.UrlEncode( i.name ) + '=' + WebUtility.UrlEncode( i.value ) );
		}

		private static IEnumerable<( string name, string value )> parseQuery( string query ) {
			if( !query.Any() )
				return Enumerable.Empty<( string, string )>();
			var parameters = HttpUtility.ParseQueryString( query );
			return from i in Enumerable.Range( 0, parameters.Count ) select ( parameters.GetKey( i ), parameters.Get( i ) );
		}
	}
}