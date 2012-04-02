using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control to semantically represent langauge with importance.
	/// </summary>
	public sealed class Strong: WebControl {
		/// <summary>
		/// Constructs a control with the given text.
		/// </summary>
		public Strong( string text ): this( text.GetLiteralControl() ) {}

		/// <summary>
		/// Constructs a control with the given child controls.
		/// </summary>
		public Strong( params Control[] controls ) {
			foreach( var c in controls )
				Controls.Add( c );
		}

		/// <summary>
		/// Represents this control in markup.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Strong; } }
	}
}