using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// A block with no special meaning.
	/// </summary>
	public class Block: WebControl, ControlTreeDataLoader {
		private readonly Control[] childControls;

		/// <summary>
		/// Creates a block. Add all child controls now; do not use AddControlsReturnThis at any time.
		/// </summary>
		public Block( params Control[] childControls ) {
			this.childControls = childControls;
		}

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			this.AddControlsReturnThis( childControls );
		}

		/// <summary>
		/// Returns the tag that represents this control in HTML.
		/// </summary>
		protected override HtmlTextWriterTag TagKey { get { return HtmlTextWriterTag.Div; } }
	}
}