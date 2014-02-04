using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// An item group in a column primary table.
	/// </summary>
	public class ColumnPrimaryItemGroup {
		internal readonly Control GroupName;
		internal readonly ReadOnlyCollection<Tuple<string, Action>> GroupActions;
		internal readonly ClickScript GroupHeadClickScript;
		internal readonly ReadOnlyCollection<EwfTableItem> Items;

		/// <summary>
		/// Creates an item group.
		/// </summary>
		/// <param name="groupName">A control that contains the name of the group and any other information you want in the group head</param>
		/// <param name="groupActions">Group action buttons</param>
		/// <param name="groupHeadClickScript">The click script for the group head</param>
		/// <param name="items">The items</param>
		public ColumnPrimaryItemGroup( Control groupName, IEnumerable<Tuple<string, Action>> groupActions = null, ClickScript groupHeadClickScript = null,
		                               IEnumerable<EwfTableItem> items = null ) {
			GroupName = groupName;
			GroupActions = ( groupActions ?? new Tuple<string, Action>[0] ).ToList().AsReadOnly();
			GroupHeadClickScript = groupHeadClickScript;
			Items = ( items ?? new EwfTableItem[0] ).ToList().AsReadOnly();
		}
	}
}