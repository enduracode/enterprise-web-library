using System.Web.UI;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A placeholder control that is a naming container.
	/// </summary>
	public class NamingPlaceholder: Control, INamingContainer, ControlTreeDataLoader {
		private readonly Control[] childControls;

		/// <summary>
		/// Creates a naming placeholder. Add all child controls now; do not use AddControlsReturnThis at any time.
		/// </summary>
		public NamingPlaceholder( params Control[] childControls ) {
			this.childControls = childControls;
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( childControls );
		}
	}
}