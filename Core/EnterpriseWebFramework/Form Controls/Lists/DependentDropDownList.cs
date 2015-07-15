using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
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

		// We can't use a dictionary since we want to allow a null key, i.e. a parent item ID that is null.
		private readonly List<Tuple<ParentItemIdType, SelectList<ItemIdType>>> parentItemIdsAndDropDowns = new List<Tuple<ParentItemIdType, SelectList<ItemIdType>>>();

		internal DependentDropDownList( SelectList<ParentItemIdType> parent ) {
			this.parent = parent;
		}

		/// <summary>
		/// Adds a drop down, which will only be visible when the specified parent value is selected.
		/// </summary>
		public void AddDropDown( ParentItemIdType parentItemId, SelectList<ItemIdType> dropDown ) {
			Controls.Add( dropDown );

			if( parentItemIdsAndDropDowns.Any( i => EwlStatics.AreEqual( i.Item1, parentItemId ) ) )
				throw new ApplicationException( "There must not be more than one drop-down list per parent item ID." );
			parentItemIdsAndDropDowns.Add( Tuple.Create( parentItemId, dropDown ) );

			parent.AddDisplayLink( parentItemId.ToSingleElementArray(), true, dropDown.ToSingleElementArray() );
		}

		public ItemIdType ValidateAndGetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues, Validator validator,
		                                                          ParentItemIdType parentSelectedItemIdInPostBack ) {
			return
				parentItemIdsAndDropDowns.Single( i => EwlStatics.AreEqual( i.Item1, parentSelectedItemIdInPostBack ) )
				                         .Item2.ValidateAndGetSelectedItemIdInPostBack( postBackValues, validator );
		}
	}
}