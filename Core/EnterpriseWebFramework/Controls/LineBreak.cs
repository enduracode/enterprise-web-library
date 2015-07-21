using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A line break.
	/// </summary>
	public class LineBreak: WebControl {
		/// <summary>
		/// Renders this control.
		/// </summary>
		protected override void Render( HtmlTextWriter writer ) {
			writer.WriteBreak();
		}
	}
}