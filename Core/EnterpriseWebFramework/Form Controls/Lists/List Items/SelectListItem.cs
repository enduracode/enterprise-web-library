using System;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An item for the list form controls.
	/// </summary>
	public class SelectListItem {
		/// <summary>
		/// Creates a list item.
		/// </summary>
		public static SelectListItem<IdType> Create<IdType>( IdType id, string label ) {
			return new SelectListItem<IdType>( id, label );
		}
	}

	/// <summary>
	/// An item for the list form controls.
	/// </summary>
	public class SelectListItem<IdType> {
		private readonly IdType id;
		private readonly string label;

		internal SelectListItem( IdType id, string label ) {
			if( typeof( IdType ) == typeof( string ) && (string)(object)id == null ) {
				throw new ApplicationException(
					"You cannot specify null for the value of a string; this could cause problems with drop-down lists since null and the empty string must be represented the same way in the HTML option element." );
			}
			this.id = id;
			this.label = label;
		}

		internal IdType Id { get { return id; } }
		internal string Label { get { return label; } }
	}
}