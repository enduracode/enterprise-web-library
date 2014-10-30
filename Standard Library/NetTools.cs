using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;

namespace RedStapler.StandardLibrary {
	/// <summary>
	/// Contains methods that are useful to web applications.
	/// </summary>
	public static class NetTools {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public const string HomeUrl = "~/";

		private const string invisibleDisplayCssStyleName = "none";

		/// <summary>
		/// Combines the given URLs into a single URL with no trailing slash.
		/// </summary>
		/// <param name="one">A URL, not null.</param>
		/// <param name="two">A URL, not null.</param>
		/// <param name="urls">Array or list of URLs to combine in addition to one and two.</param>
		/// <returns>Combined URL string.</returns>
		public static string CombineUrls( string one, string two, params string[] urls ) {
			if( one == null || two == null )
				throw new ArgumentException( "String cannot be null." );

			var combinedUrl = one.Trim( '/' ) + "/" + two.Trim( '/' ) + "/"; // NOTE: Make this more like CombinePaths next time this is changed

			foreach( var url in urls )
				combinedUrl += url.Trim( '/' ) + "/";

			return combinedUrl.TrimEnd( '/' );
		}

		/// <summary>
		/// Given a type, generates a Url based on the namespace and type.
		/// Every level of the namespace up to and including the first one that
		/// ends in with the string "site" (case insensitive) is dropped.
		/// The type "MyControl" in namespace "one.two.three.MyWebSite.Stuff.MyControls"
		/// will return "Stuff/MyControls/MyControl.ascx".
		/// </summary>
		public static string GetRelativeUrlToAscxFromType( Type type ) {
			if( type.FullName.ToLower().IndexOf( "site" ) == -1 )
				throw new ArgumentException( "Type's namespace does not contain the string \"site\" anywhere in its name." );
			var url = type.FullName.Replace( '.', '/' ) + ".ascx";
			while( true ) {
				var parts = url.DissectByChar( '/' );
				var firstLevel = parts[ 0 ];
				url = parts[ 1 ];
				if( firstLevel.ToLower().EndsWith( "site" ) )
					return url;
			}
		}

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
		/// Returns an anchor tag with the specified parameters. Use this only for status messages that are being built after LoadData. In all other cases, use
		/// EwfLink.
		/// </summary>
		public static string BuildBasicLink( string text, string url, bool navigatesInNewWindow ) {
			return "<a href=\"" + url + "\"" + ( navigatesInNewWindow ? " target=\"_blank\"" : "" ) + ">" + text + "</a>";
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
		/// Gets whether a request for the specified URL returns an HTTP status code other than 200 OK.
		/// </summary>
		public static bool LinkIsBroken( string url ) {
			var request = WebRequest.CreateHttp( url );

			request.Method = "HEAD";
			using( var response = request.getResponseIfPossible() )
				return response == null || response.StatusCode != HttpStatusCode.OK;
		}

		private static HttpWebResponse getResponseIfPossible( this HttpWebRequest request ) {
			try {
				return getResponseForAnyStatusCode( request );
			}
			catch( WebException e ) {
				if( new[] { WebExceptionStatus.ConnectFailure, WebExceptionStatus.Timeout }.Contains( e.Status ) )
					return null;
				throw;
			}
		}

		/// <summary>
		/// Gets a response and prevents exceptions caused by HTTP status codes. From http://stackoverflow.com/a/1366869/35349.
		/// </summary>
		private static HttpWebResponse getResponseForAnyStatusCode( this HttpWebRequest request ) {
			try {
				return (HttpWebResponse)request.GetResponse();
			}
			catch( WebException e ) {
				if( e.Response == null || e.Status != WebExceptionStatus.ProtocolError )
					throw;
				return (HttpWebResponse)e.Response;
			}
		}

		/// <summary>
		/// Creates an image with the given text and font and returns a response object.
		/// Text will be all on one line and will not be wider than 800 pixels or higher than 150 pixels.
		/// Do not pass null for text. Passing null for font will result in a generic Sans Serif, 10pt font.
		/// </summary>
		public static EwfResponse CreateImageFromText( string text, Font font ) {
			return new EwfResponse(
				ContentTypes.Png,
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

		internal static string GetAppCookiePath() {
			// It's important that the cookie path not end with a slash. If it does, Internet Explorer will not transmit the cookie if the user requests the root
			// URL of the site without a trailing slash, e.g. integration.redstapler.biz/Todd. One justification for adding a trailing slash to the cookie path is
			// http://stackoverflow.com/questions/2156399/restful-cookie-path-fails-in-ie-without-trailing-slash.
			return HttpRuntime.AppDomainAppVirtualPath;
		}
	}
}