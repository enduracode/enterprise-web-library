using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the control list.
	/// </summary>
	public class ControlListItem {
		internal readonly IEnumerable<Control> ChildControls;
		internal readonly string Id;
		internal readonly IEnumerable<UpdateRegionSet> UpdateRegionSets;
		internal readonly IEnumerable<UpdateRegionSet> RemovalUpdateRegionSets;

		/// <summary>
		/// Creates a control-list item.
		/// </summary>
		/// <param name="childControls">The item content.</param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		public ControlListItem( IEnumerable<Control> childControls, IEnumerable<UpdateRegionSet> updateRegionSets = null ) {
			ChildControls = childControls;
			UpdateRegionSets = updateRegionSets;
		}

		/// <summary>
		/// Creates a control-list item.
		/// </summary>
		/// <param name="childControls">The item content.</param>
		/// <param name="id">The ID of the item. This is required if you're adding the item on an intermediate post-back or want to remove the item on an
		/// intermediate post-back. Do not pass null.</param>
		/// <param name="updateRegionSets">The intermediate-post-back update-region sets that this item will be a part of.</param>
		/// <param name="removalUpdateRegionSets">The intermediate-post-back update-region sets that this item's removal will be a part of.</param>
		public ControlListItem(
			IEnumerable<Control> childControls, string id, IEnumerable<UpdateRegionSet> updateRegionSets = null,
			IEnumerable<UpdateRegionSet> removalUpdateRegionSets = null ) {
			ChildControls = childControls;
			Id = id;
			UpdateRegionSets = updateRegionSets;
			RemovalUpdateRegionSets = removalUpdateRegionSets;
		}
	}
}