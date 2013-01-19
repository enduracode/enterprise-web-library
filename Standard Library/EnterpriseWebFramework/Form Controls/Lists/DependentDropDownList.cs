using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A drop down list whose visible options depend upon the selected value in another drop down list.
	/// </summary>
	public static class DependentDropDownList {
		public static DependentDropDownList<ItemIdType, ParentItemIdType> Create<ItemIdType, ParentItemIdType>( SelectList<ParentItemIdType> parent ) {
			return new DependentDropDownList<ItemIdType, ParentItemIdType>( parent );
		}
	}

	/// <summary>
	/// A drop down list whose visible options depend upon the selected value in another drop down list.
	/// </summary>
	public class DependentDropDownList<ItemIdType, ParentItemIdType>: Control, INamingContainer {
		private readonly SelectList<ParentItemIdType> parent;
		private readonly Dictionary<ParentItemIdType, SelectList<ItemIdType>> dropDownsByParentItemId = new Dictionary<ParentItemIdType, SelectList<ItemIdType>>();

		internal DependentDropDownList( SelectList<ParentItemIdType> parent ) {
			this.parent = parent;
		}

		/// <summary>
		/// Adds a drop down, which will only be visible when the specified parent value is selected.
		/// </summary>
		public void AddDropDown( ParentItemIdType parentItemId, SelectList<ItemIdType> dropDown ) {
			Controls.Add( dropDown );
			dropDownsByParentItemId.Add( parentItemId, dropDown );
			parent.AddDisplayLink( parentItemId.ToSingleElementArray(), true, dropDown.ToSingleElementArray() );
		}

		public ItemIdType ValidateAndGetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues, Validator validator,
		                                                          ParentItemIdType parentSelectedItemIdInPostBack ) {
			return dropDownsByParentItemId[ parentSelectedItemIdInPostBack ].ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
		}
	}
}