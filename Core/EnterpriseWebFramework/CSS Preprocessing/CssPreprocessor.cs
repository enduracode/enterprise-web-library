using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Supports custom elements in CSS selectors.
	/// </summary>
	internal static class CssPreprocessor {
		private const string reservedCustomElementPrefix = "ewf";
		private const string customElementPattern = @"(?<!\.|#)" + reservedCustomElementPrefix + @"\w+";

		// The parsing logic is based on http://www.w3.org/TR/CSS2/syndata.html.
		internal static string TransformCssFile( string sourceCssText ) {
			sourceCssText = RegularExpressions.RemoveMultiLineCStyleComments( sourceCssText );

			var customElementsDetected = from Match match in Regex.Matches( sourceCssText, customElementPattern ) select match.Value;
			customElementsDetected = customElementsDetected.Distinct();
			var knownCustomElements = CssPreprocessingStatics.Elements.Select( ce => reservedCustomElementPrefix + ce.Name );
			var unknownCustomElements = customElementsDetected.Except( knownCustomElements ).ToList();
			if( unknownCustomElements.Any() ) {
				throw new MultiMessageApplicationException(
					unknownCustomElements.Select( e => "\"" + e + "\" begins with the reserved custom element prefix but is not a known custom element." ).ToArray() );
			}

			using( var writer = new StringWriter() ) {
				var buffer = new StringBuilder();
				using( var reader = new StringReader( sourceCssText ) ) {
					char? stringDelimiter = null;
					while( reader.Peek() != -1 ) {
						var c = (char)reader.Read();

						// escaped quote, brace, or other character
						if( c == '\\' ) {
							buffer.Append( c );
							if( reader.Peek() != -1 )
								buffer.Append( (char)reader.Read() );
						}

						// string delimiter
						else if( !stringDelimiter.HasValue && ( c == '\'' || c == '"' ) ) {
							buffer.Append( c );
							stringDelimiter = c;
						}
						else if( stringDelimiter.HasValue && c == stringDelimiter ) {
							buffer.Append( c );
							stringDelimiter = null;
						}

						// selector delimiter
						else if( !stringDelimiter.HasValue && ( c == ',' || c == '{' ) ) {
							writer.Write( getTransformedSelector( buffer.ToString() ) );
							writer.Write( c );
							buffer = new StringBuilder();
						}
						else if( !stringDelimiter.HasValue && c == '}' ) {
							writer.Write( buffer.ToString() );
							writer.Write( c );
							buffer = new StringBuilder();
						}

						// other character
						else
							buffer.Append( c );
					}
				}
				writer.Write( buffer.ToString() );
				return writer.ToString();
			}
		}

		private static string getTransformedSelector( string selectorWithCustomElements ) {
			return StringTools.ConcatenateWithDelimiter( ",", getSelectors( selectorWithCustomElements ).ToArray() );
		}

		private static IEnumerable<string> getSelectors( string selectorWithCustomElements ) {
			var match = Regex.Match( selectorWithCustomElements, customElementPattern );
			if( !match.Success )
				yield return selectorWithCustomElements;
			else {
				foreach( var elementSelector in CssPreprocessingStatics.Elements.Single( i => reservedCustomElementPrefix + i.Name == match.Value ).Selectors ) {
					foreach( var selectorTail in getSelectors( selectorWithCustomElements.Substring( match.Index + match.Length ) ) )
						yield return selectorWithCustomElements.Substring( 0, match.Index ) + elementSelector + selectorTail;
				}
			}
		}
	}
}