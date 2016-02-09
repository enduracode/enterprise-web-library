using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A placeholder control that is a naming container.
	/// </summary>
	public class NamingPlaceholder: Control, INamingContainer, ControlTreeDataLoader {
		private readonly IEnumerable<Control> childControls;
		private readonly IEnumerable<UpdateRegionSet> updateRegionSets;

		/// <summary>
		/// Creates a naming placeholder. Add all child controls now; do not use AddControlsReturnThis at any time.
		/// </summary>
		/// <param name="childControls"></param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this naming placeholder will be a part of.</param>
		public NamingPlaceholder( IEnumerable<Control> childControls, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			this.childControls = childControls.ToArray();
			this.updateRegionSets = updateRegionSets;
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( childControls );

			EwfPage.Instance.AddUpdateRegionLinker(
				new UpdateRegionLinker(
					this,
					"",
					new PreModificationUpdateRegion( updateRegionSets, this.ToSingleElementArray, () => "" ).ToSingleElementArray(),
					arg => this.ToSingleElementArray() ) );
		}
	}
}