using System;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the list form controls that contains a change handler.
	/// </summary>
	public class ChangeBasedListItem {
		/// <summary>
		/// Creates a change based list item.
		/// </summary>
		public static ChangeBasedListItem<IdType> Create<IdType>( IdType id, string label, Action<DBConnection, bool> changeHandler ) {
			return new ChangeBasedListItem<IdType>( EwfListItem.Create( id, label ), changeHandler );
		}
	}

	/// <summary>
	/// An item for the list form controls that contains a change handler.
	/// </summary>
	public class ChangeBasedListItem<IdType> {
		private readonly EwfListItem<IdType> item;
		private readonly Action<DBConnection, bool> changeHandler;

		internal ChangeBasedListItem( EwfListItem<IdType> item, Action<DBConnection, bool> changeHandler ) {
			this.item = item;
			this.changeHandler = changeHandler;
		}

		internal EwfListItem<IdType> Item { get { return item; } }
		internal Action<DBConnection, bool> ChangeHandler { get { return changeHandler; } }
	}
}