using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Web;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Contains methods that are useful to web applications.
	/// </summary>
	public static class NetTools {
		/// <summary>
		/// Encodes the given text as HTML, replacing instances of \n with &lt;br/&gt; and optionally replacing the empty string with a non-breaking space.
		/// </summary>
		public static string GetTextAsEncodedHtml( this string text, bool returnNonBreakingSpaceIfEmpty = true ) {
			if( text.IsNullOrWhiteSpace() && returnNonBreakingSpaceIfEmpty )
				return "&nbsp;";
			return HttpUtility.HtmlEncode( text ).Replace( "\n", "<br/>" );
		}

		/// <summary>
		/// Creates an image with the given text and font and returns a response object.
		/// Text will be all on one line and will not be wider than 800 pixels or higher than 150 pixels.
		/// Do not pass null for text. Passing null for font will result in a generic Sans Serif, 10pt font.
		/// </summary>
		public static EwfResponse CreateImageFromText( string text, Font font ) {
			return EwfResponse.Create(
				TewlContrib.ContentTypes.Png,
				new EwfResponseBodyCreator(
					stream => {
						font = font ?? new Font( FontFamily.GenericSansSerif, 10 );

						const int startingBitmapWidth = 800;
						const int startingBitmapHeight = 150;

						var b = new Bitmap( startingBitmapWidth, startingBitmapHeight );
						var g = Graphics.FromImage( b );
						g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
						g.Clear( Color.White );

						// Find the size of the text we're drawing
						var stringFormat = new StringFormat();
						stringFormat.SetMeasurableCharacterRanges( new[] { new CharacterRange( 0, text.Length ) } );
						var textRegion = g.MeasureCharacterRanges( text, font, new Rectangle( 0, 0, startingBitmapWidth, startingBitmapHeight ), stringFormat ).Single();

						// Draw the text, crop our image to size, make transparent and save to stream.
						g.DrawString( text, font, Brushes.Black, new PointF() );
						var finalImage = b.Clone( textRegion.GetBounds( g ), b.PixelFormat );
						finalImage.MakeTransparent( Color.White );
						finalImage.Save( stream, ImageFormat.Png );
					} ) );
		}

		internal static bool IsWebApp() {
			return HttpContext.Current != null;
		}
	}
}