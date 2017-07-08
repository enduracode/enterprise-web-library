using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 30 September 2017." ) ]
	public sealed class Strong: WebControl {
		[ Obsolete( "Guaranteed through 30 September 2017." ) ]
		public Strong( string text ): this( text.GetLiteralControl() ) {}

		[ Obsolete( "Guaranteed through 30 September 2017." ) ]
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