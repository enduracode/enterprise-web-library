using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the list form controls that contains a change handler and knows if it is selected.
	/// </summary>
	public class ChangeBasedListItemWithSelectionState {
		/// <summary>
		/// Creates a change based list item.
		/// </summary>
		public static ChangeBasedListItemWithSelectionState<IdType> Create<IdType>( IdType id, string label, Action<bool> changeHandler, bool isSelected,
		                                                                            bool? isSelectedInUi = null ) {
			return new ChangeBasedListItemWithSelectionState<IdType>( ChangeBasedListItem.Create( id, label, changeHandler ), isSelected, isSelectedInUi ?? isSelected );
		}
	}

	/// <summary>
	/// An item for the list form controls that contains a change handler and knows if it is selected.
	/// </summary>
	public class ChangeBasedListItemWithSelectionState<IdType> {
		private readonly ChangeBasedListItem<IdType> item;
		private readonly bool isSelected;
		private readonly bool isSelectedInUi;

		internal ChangeBasedListItemWithSelectionState( ChangeBasedListItem<IdType> item, bool isSelected, bool isSelectedInUi ) {
			this.item = item;
			this.isSelected = isSelected;
			this.isSelectedInUi = isSelectedInUi;
		}

		internal ChangeBasedListItem<IdType> Item { get { return item; } }
		internal bool IsSelected { get { return isSelected; } }
		internal bool IsSelectedInUi { get { return isSelectedInUi; } }
	}
}