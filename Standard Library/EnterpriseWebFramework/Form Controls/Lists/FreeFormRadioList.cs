using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;

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
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		public static FreeFormRadioList<ItemIdType> Create<ItemIdType>( string groupName, bool allowNoSelection, ItemIdType selectedItemId,
		                                                                bool disableSingleButtonDetection = false ) {
			return new FreeFormRadioList<ItemIdType>( groupName, allowNoSelection, disableSingleButtonDetection, selectedItemId );
		}
	}

	/// <summary>
	/// A radio button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public class FreeFormRadioList<ItemIdType>: DisplayLink {
		private readonly string groupName;
		private readonly bool allowNoSelection;
		private readonly ItemIdType selectedItemId;
		private readonly List<Action<PostBackValueDictionary>> displayLinkingSetInitialDisplayMethods = new List<Action<PostBackValueDictionary>>();
		private readonly List<Action> displayLinkingAddJavaScriptMethods = new List<Action>();
		private readonly List<Tuple<ItemIdType, CommonCheckBox>> itemIdsAndCheckBoxes = new List<Tuple<ItemIdType, CommonCheckBox>>();

		internal FreeFormRadioList( string groupName, bool allowNoSelection, bool disableSingleButtonDetection, ItemIdType selectedItemId ) {
			this.groupName = groupName;
			this.allowNoSelection = allowNoSelection;
			this.selectedItemId = selectedItemId;

			EwfPage.Instance.AddControlTreeValidation(
				() =>
				RadioButtonGroup.ValidateControls( allowNoSelection,
				                                   StandardLibraryMethods.AreEqual( getNoSelectionItemId(), selectedItemId ),
				                                   itemIdsAndCheckBoxes.Select( i => i.Item2 ),
				                                   disableSingleButtonDetection ) );

			EwfPage.Instance.AddDisplayLink( this );
		}

		public void AddDisplayLink( IEnumerable<ItemIdType> itemIds, bool controlsVisibleOnMatch, IEnumerable<WebControl> controls ) {
			itemIds = itemIds.ToArray();
			controls = controls.ToArray();
			displayLinkingSetInitialDisplayMethods.Add( formControlValues => {
				var match = itemIds.Contains( GetSelectedItemIdInPostBack( formControlValues ) );
				var visible = ( controlsVisibleOnMatch && match ) || ( !controlsVisibleOnMatch && !match );
				foreach( var i in controls )
					DisplayLinkingOps.SetControlDisplay( i, visible );
			} );
			displayLinkingAddJavaScriptMethods.Add( () => {
				foreach( var pair in itemIdsAndCheckBoxes ) {
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( pair.Item2,
					                                                  itemIds.Contains( pair.Item1 ) ? controlsVisibleOnMatch : !controlsVisibleOnMatch,
					                                                  controls.ToArray() );
				}
			} );
		}

		/// <summary>
		/// Creates an in-line radio button that is part of the list.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton( ItemIdType listItemId, string label = "", bool autoPostBack = false ) {
			var checkBox = new EwfCheckBox( itemIsSelected( listItemId ), label: label ) { GroupName = groupName, AutoPostBack = autoPostBack };
			itemIdsAndCheckBoxes.Add( Tuple.Create( listItemId, checkBox as CommonCheckBox ) );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the list.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton( ItemIdType listItemId, string label = "", bool autoPostBack = false ) {
			var checkBox = new BlockCheckBox( itemIsSelected( listItemId ), label: label ) { GroupName = groupName, AutoPostBack = autoPostBack };
			itemIdsAndCheckBoxes.Add( Tuple.Create( listItemId, checkBox as CommonCheckBox ) );
			return checkBox;
		}

		private bool itemIsSelected( ItemIdType listItemId ) {
			if( allowNoSelection && StandardLibraryMethods.AreEqual( listItemId, getNoSelectionItemId() ) )
				throw new ApplicationException( "You cannot create a radio button with the ID that represents no selection." );
			return StandardLibraryMethods.AreEqual( listItemId, selectedItemId );
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			foreach( var i in displayLinkingSetInitialDisplayMethods )
				i( formControlValues );
		}

		void DisplayLink.AddJavaScript() {
			foreach( var i in displayLinkingAddJavaScriptMethods )
				i();
		}

		/// <summary>
		/// Gets the selected item ID in the post back.
		/// </summary>
		public ItemIdType GetSelectedItemIdInPostBack( PostBackValueDictionary postBackValues ) {
			var itemIdsAndCheckBoxesOnPage = itemIdsAndCheckBoxes.Where( i => ( i.Item2 as Control ).IsOnPage() ).ToArray();
			if( !itemIdsAndCheckBoxesOnPage.Any() )
				return selectedItemId;
			var selectedPair = itemIdsAndCheckBoxesOnPage.SingleOrDefault( i => i.Item2.IsCheckedInPostBack( postBackValues ) );
			return selectedPair != null ? selectedPair.Item1 : allowNoSelection ? getNoSelectionItemId() : itemIdsAndCheckBoxesOnPage.First().Item1;
		}

		private ItemIdType getNoSelectionItemId() {
			return StandardLibraryMethods.GetDefaultValue<ItemIdType>( true );
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return !StandardLibraryMethods.AreEqual( GetSelectedItemIdInPostBack( postBackValues ), selectedItemId );
		}
	}
}