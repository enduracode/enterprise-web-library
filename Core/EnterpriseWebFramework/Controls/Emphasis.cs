using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 30 September 2017." ) ]
	public sealed class Emphasis: WebControl {
		[ Obsolete( "Guaranteed through 30 September 2017." ) ]
		public Emphasis( string text ): this( text.GetLiteralControl() ) {}

		[ Obsolete( "Guaranteed through 30 September 2017." ) ]
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