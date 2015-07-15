using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control to semantically represent typographical conventions with specific meanings.
	/// </summary>
	public sealed class Superscript: WebControl {
		/// <summary>
		/// Constructs a control with the given text.
		/// </summary>
		public Superscript( string text ): this( text.GetLiteralControl() ) {}

		/// <summary>
		/// Constructs a control with the given child controls.
		/// </summary>
		public Superscript( params Control[] controls ) {
			foreach( var c in controls )
				Controls.Add( c );
		}

		/// <summary>
		/// Represents this control in markup.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Sup; } }
	}
}