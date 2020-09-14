using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
using Tewl.Tools;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// Contains methods that are useful to web applications.
	/// </summary>
	public static class NetTools {
		/// <summary>
		/// EWL use only.
		/// </summary>
		public const string HomeUrl = "~/";

		private const string invisibleDisplayCssStyleName = "none";

		/// <summary>
		/// Encodes the given text as HTML, replacing instances of \n with &lt;br/&gt; and optionally replacing the empty string with a non-breaking space.
		/// </summary>
		public static string GetTextAsEncodedHtml( this string text, bool returnNonBreakingSpaceIfEmpty = true ) {
			if( text.IsNullOrWhiteSpace() && returnNonBreakingSpaceIfEmpty )
				return "&nbsp;";
			return HttpUtility.HtmlEncode( text ).Replace( "\n", "<br/>" );
		}

		/// <summary>
		/// Returns the full url of the page that redirected to the current location.
		/// </summary>
		public static string ReferringUrl {
			get { return ( HttpContext.Current.Request.UrlReferrer != null ? HttpContext.Current.Request.UrlReferrer.AbsoluteUri : "" ); }
		}

		/// <summary>
		/// Redirects to the given URL. Throws a ThreadAbortedException. NOTE: Make this internal.
		/// </summary>
		public static void Redirect( string url ) {
			HttpContext.Current.Response.Redirect( url );
		}

		/// <summary>
		/// Sets the initial visibility of a web control. This can be used in tandem with DisplayLinking.
		/// </summary>
		public static void SetInitialDisplay( this WebControl control, bool visible ) {
			DisplayLinkingOps.SetControlDisplay( control, visible );
		}

		/// <summary>
		/// Sets the initial visibility of the given control to the opposite of what it currently is.
		/// </summary>
		internal static void ToggleInitialDisplay( this WebControl control ) {
			SetInitialDisplay( control, control.Style[ HtmlTextWriterStyle.Display ] == invisibleDisplayCssStyleName );
		}

		/// <summary>
		/// Adds the specified JavaScript to the specified event handler of the specified control. Do not pass null for script. Use JsWritingMethods constants for events.
		/// To add an onsubmit event, use ClientScript.RegisterOnSubmitStatement instead.
		/// A semicolon will be added to the end of the script.
		/// </summary>
		public static void AddJavaScriptEventScript( this WebControl control, string jsEventConstant, string script ) {
			control.Attributes[ jsEventConstant ] += script + ";";
		}

		/// <summary>
		/// Adds the specified JavaScript to the specified event handler of the specified control. Do not pass null for script. Use JsWritingMethods constants for events.
		/// To add an onsubmit event, use ClientScript.RegisterOnSubmitStatement instead.
		/// A semicolon will be added to the end of the script.
		/// </summary>
		public static void AddJavaScriptEventScript( this HtmlControl control, string jsEventConstant, string script ) {
			control.Attributes[ jsEventConstant ] += script + ";";
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

		public static HttpCookie GetCookie( string name ) {
			// Check the response collection first in case we set the cookie earlier in this request. The Response.Cookies indexer has the side effect of creating a
			// cookie if one doesn't already exist; we do the Allkeys.Any check to prevent this from happening.
			if( HttpContext.Current.Response.Cookies.AllKeys.Any( i => i == name ) )
				return HttpContext.Current.Response.Cookies[ name ];

			return HttpContext.Current.Request.Cookies[ name ];
		}

		internal static bool IsWebApp() {
			return HttpContext.Current != null;
		}
	}
}