using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A radio button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public static class FreeFormRadioList {
		/// <summary>
		/// Creates a free-form radio button list.
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="allowNoSelection">Pass true to cause a selected item ID of null (or empty string when the item ID type is string) to represent the state in
		/// which none of the radio buttons are selected. Note that this is not recommended by the Nielsen Norman Group; see
		/// http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="selectedItemId"></param>
		public static FreeFormRadioList<ItemIdType> Create<ItemIdType>( string groupName, bool allowNoSelection, ItemIdType selectedItemId ) {
			return new FreeFormRadioList<ItemIdType>( groupName, allowNoSelection, selectedItemId );
		}
	}

	/// <summary>
	/// A radio button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public class FreeFormRadioList<ItemIdType> {
		private readonly string groupName;
		private readonly bool allowNoSelection;
		private readonly ItemIdType selectedItemId;
		private readonly List<Tuple<ItemIdType, CommonCheckBox>> itemIdsAndCheckBoxes = new List<Tuple<ItemIdType, CommonCheckBox>>();

		internal FreeFormRadioList( string groupName, bool allowNoSelection, ItemIdType selectedItemId ) {
			this.groupName = groupName;
			this.allowNoSelection = allowNoSelection;
			this.selectedItemId = selectedItemId;

			EwfPage.Instance.AddControlTreeValidation(
				() =>
				RadioButtonGroup.ValidateControls( allowNoSelection,
				                                   StandardLibraryMethods.AreEqual( getNoSelectionItemId(), selectedItemId ),
				                                   itemIdsAndCheckBoxes.Select( i => i.Item2 ) ) );
		}

		/// <summary>
		/// Creates an in-line radio button that is part of the list.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton( ItemIdType listItemId, string label = "" ) {
			var checkBox = new EwfCheckBox( itemIsSelected( listItemId ), label: label ) { GroupName = groupName };
			itemIdsAndCheckBoxes.Add( Tuple.Create( listItemId, checkBox as CommonCheckBox ) );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the list.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton( ItemIdType listItemId, string label = "" ) {
			var checkBox = new BlockCheckBox( itemIsSelected( listItemId ), label: label ) { GroupName = groupName };
			itemIdsAndCheckBoxes.Add( Tuple.Create( listItemId, checkBox as CommonCheckBox ) );
			return checkBox;
		}

		private bool itemIsSelected( ItemIdType listItemId ) {
			if( allowNoSelection && StandardLibraryMethods.AreEqual( listItemId, getNoSelectionItemId() ) )
				throw new ApplicationException( "You cannot create a radio button with the ID that represents no selection." );
			return StandardLibraryMethods.AreEqual( listItemId, selectedItemId );
		}

		/// <summary>
		/// Gets the selected item ID in the post back.
		/// </summary>
		public ItemIdType GetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues ) {
			var itemIdsAndCheckBoxesOnPage = itemIdsAndCheckBoxes.Where( i => ( i.Item2 as Control ).IsOnPage() ).ToArray();
			var selectedPair = itemIdsAndCheckBoxesOnPage.SingleOrDefault( i => i.Item2.IsCheckedInPostBack( postBackValues ) );
			return selectedPair != null ? selectedPair.Item1 : allowNoSelection ? getNoSelectionItemId() : itemIdsAndCheckBoxesOnPage.First().Item1;
		}

		private ItemIdType getNoSelectionItemId() {
			return typeof( ItemIdType ) == typeof( string ) ? (ItemIdType)(object)"" : default( ItemIdType );
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return !EqualityComparer<ItemIdType>.Default.Equals( GetSelectedItemIdInPostBack( postBackValues ), selectedItemId );
		}

		internal IEnumerable<Tuple<ItemIdType, CommonCheckBox>> ItemIdsAndCheckBoxes { get { return itemIdsAndCheckBoxes; } }
	}
}