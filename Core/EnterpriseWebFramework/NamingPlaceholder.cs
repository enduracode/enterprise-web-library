using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A placeholder control that is a naming container.
	/// </summary>
	public class NamingPlaceholder: Control, INamingContainer, ControlTreeDataLoader {
		private readonly IEnumerable<Control> childControls;
		private readonly UpdateRegionSet updateRegionSet;

		/// <summary>
		/// Creates a naming placeholder. Add all child controls now; do not use AddControlsReturnThis at any time.
		/// </summary>
		/// <param name="childControls"></param>
		/// <param name="updateRegionSet">The intermediate-post-back update-region set that this naming placeholder will be a part of.</param>
		public NamingPlaceholder( IEnumerable<Control> childControls, UpdateRegionSet updateRegionSet = null ) {
			this.childControls = childControls.ToArray();
			this.updateRegionSet = updateRegionSet;
		}

		void ControlTreeDataLoader.LoadData() {
			this.AddControlsReturnThis( childControls );

			EwfPage.Instance.AddUpdateRegionLinker( new UpdateRegionLinker( this,
			                                                                "",
			                                                                updateRegionSet != null
				                                                                ? new PreModificationUpdateRegion( updateRegionSet, this.ToSingleElementArray, () => "" )
					                                                                  .ToSingleElementArray()
				                                                                : new PreModificationUpdateRegion[ 0 ],
			                                                                arg => this.ToSingleElementArray() ) );
		}
	}
}