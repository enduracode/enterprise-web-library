using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class FontAwesomeIcon: WebControl, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				return new[] { new CssElement( "Icon", "span.fa" ) };
			}
		}

		private readonly IEnumerable<string> classes;

		/// <summary>
		/// Creates a Font Awesome icon.
		/// </summary>
		/// <param name="iconName">The name of the icon. Do not pass null or the empty string.</param>
		/// <param name="additionalClasses">Additional classes that will be added to the icon element.</param>
		public FontAwesomeIcon( string iconName, params string[] additionalClasses ) {
			classes = iconName.ToSingleElementArray().Concat( additionalClasses );
		}

		void ControlTreeDataLoader.LoadData() {
			CssClass = CssClass.ConcatenateWithSpace( "fa {0}".FormatWith( StringTools.ConcatenateWithDelimiter( " ", classes.ToArray() ) ) );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Span; } }
	}
}