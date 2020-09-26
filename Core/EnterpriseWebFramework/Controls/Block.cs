using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 30 November 2020." ) ]
	public class Block: WebControl, ControlTreeDataLoader {
		private readonly Control[] childControls;

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public Block( params Control[] childControls ) {
			this.childControls = childControls;
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( childControls );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}