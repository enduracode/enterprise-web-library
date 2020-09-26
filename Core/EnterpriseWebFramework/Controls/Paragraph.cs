using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	[ Obsolete( "Guaranteed through 30 November 2020." ) ]
	public class LegacyParagraph: WebControl, ControlTreeDataLoader {
		private readonly List<Control> codeControls = new List<Control>();

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public LegacyParagraph() {}

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public LegacyParagraph( string text ): this( text.ToComponents().GetControls().ToArray() ) {}

		[ Obsolete( "Guaranteed through 30 November 2020." ) ]
		public LegacyParagraph( params Control[] childControls ): this() {
			codeControls.AddRange( childControls );
		}

		/// <summary>
		/// Adds the specified child controls.
		/// </summary>
		public void AddChildControls( params Control[] controls ) {
			codeControls.AddRange( controls );
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( codeControls );
		}

		/// <summary>
		/// Returns the p tag.
		/// </summary>
		protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.P;
	}
}