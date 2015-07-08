using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the list form controls that contains a change handler.
	/// </summary>
	public class ChangeBasedListItem {
		/// <summary>
		/// Creates a change based list item.
		/// </summary>
		public static ChangeBasedListItem<IdType> Create<IdType>( IdType id, string label, Action<bool> changeHandler ) {
			return new ChangeBasedListItem<IdType>( SelectListItem.Create( id, label ), changeHandler );
		}
	}

	/// <summary>
	/// An item for the list form controls that contains a change handler.
	/// </summary>
	public class ChangeBasedListItem<IdType> {
		private readonly SelectListItem<IdType> item;
		private readonly Action<bool> changeHandler;

		internal ChangeBasedListItem( SelectListItem<IdType> item, Action<bool> changeHandler ) {
			this.item = item;
			this.changeHandler = changeHandler;
		}

		internal SelectListItem<IdType> Item { get { return item; } }
		internal Action<bool> ChangeHandler { get { return changeHandler; } }
	}
}