using System.Collections.Generic;
using System.Web.UI;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the control list.
	/// </summary>
	public class ControlListItem {
		internal readonly IEnumerable<Control> ChildControls;
		internal readonly string Id;
		internal readonly UpdateRegionSet UpdateRegionSet;
		internal readonly UpdateRegionSet RemovalUpdateRegionSet;

		/// <summary>
		/// Creates a control-list item.
		/// </summary>
		/// <param name="childControls">The item content.</param>
		/// <param name="updateRegionSet">The intermediate-post-back update-region set that this item will be a part of.</param>
		public ControlListItem( IEnumerable<Control> childControls, UpdateRegionSet updateRegionSet = null ) {
			ChildControls = childControls;
			UpdateRegionSet = updateRegionSet;
		}

		/// <summary>
		/// Creates a control-list item.
		/// </summary>
		/// <param name="childControls">The item content.</param>
		/// <param name="id">The ID of the item. This is required if you're adding the item on an intermediate post-back or want to remove the item on an
		/// intermediate post-back. Do not pass null.</param>
		/// <param name="updateRegionSet">The intermediate-post-back update-region set that this item will be a part of.</param>
		/// <param name="removalUpdateRegionSet">The intermediate-post-back update-region set that this item's removal will be a part of.</param>
		public ControlListItem( IEnumerable<Control> childControls, string id, UpdateRegionSet updateRegionSet = null, UpdateRegionSet removalUpdateRegionSet = null ) {
			ChildControls = childControls;
			Id = id;
			UpdateRegionSet = updateRegionSet;
			RemovalUpdateRegionSet = removalUpdateRegionSet;
		}
	}
}