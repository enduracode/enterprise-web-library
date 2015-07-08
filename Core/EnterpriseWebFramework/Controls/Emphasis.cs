using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A control to semantically represent language with emphasis.
	/// </summary>
	public sealed class Emphasis: WebControl {
		/// <summary>
		/// Constructs this control with the given text.
		/// </summary>
		public Emphasis( string text ): this( text.GetLiteralControl() ) {}

		/// <summary>
		/// Constructs this control with the given child controls.
		/// </summary>
		public Emphasis( params Control[] controls ) {
			foreach( var c in controls )
				Controls.Add( c );
		}

		/// <summary>
		/// Represents this control in markup.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Em; } }
	}
}