using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A paragraph.
	/// </summary>
	public class LegacyParagraph: WebControl, ControlTreeDataLoader {
		private readonly List<Control> codeControls = new List<Control>();

		/// <summary>
		/// Creates a paragraph with no child controls.
		/// </summary>
		public LegacyParagraph() {}

		/// <summary>
		/// Creates a paragraph with the given text as a Literal control.
		/// </summary>
		public LegacyParagraph( string text ): this( text.ToComponents().GetControls().ToArray() ) {}

		/// <summary>
		/// Creates a paragraph with the specified child controls.
		/// </summary>
		public LegacyParagraph( params Control[] childControls ): this() {
			codeControls.AddRange( childControls );
		}

		/// <summary>
		/// EWF ToolTip to display on this control. Setting ToolTipControl will ignore this property.
		/// </summary>
		public override string ToolTip { get; set; }

		/// <summary>
		/// Control to display inside the tool tip. Do not pass null. This will ignore the ToolTip property.
		/// </summary>
		public Control ToolTipControl { get; set; }

		/// <summary>
		/// Adds the specified child controls.
		/// </summary>
		public void AddChildControls( params Control[] controls ) {
			codeControls.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( codeControls );
			if( ToolTip != null || ToolTipControl != null )
				new ToolTip( ToolTipControl ?? EnterpriseWebFramework.Controls.ToolTip.GetToolTipTextControl( ToolTip ), this );
		}

		/// <summary>
		/// Returns the p tag.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.P;
	}
}