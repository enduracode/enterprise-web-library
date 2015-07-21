using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.DataAccess;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A label that automatically HTML encodes its contents, and supports EWF ToolTips.
	/// </summary>
	public class EwfLabel: WebControl, ControlTreeDataLoader {
		private readonly Literal label = new Literal();

		/// <summary>
		/// Sets the text on the label. Text will be automatically HTML encoded.
		/// </summary>
		public string Text { set { label.Text = value.GetTextAsEncodedHtml(); } }

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		void ControlTreeDataLoader.LoadData() {
			Controls.Add( label );
			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		/// <summary>
		/// Returns the span tag, which represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Span; } }
	}
}