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
		/// <param name="allowNoSelection">Pass true to cause a selected item ID of null (or empty string when the item ID type is string) to represent the state in
		/// which none of the radio buttons are selected. Note that this is not recommended by the Nielsen Norman Group; see
		/// http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="selectedItemId"></param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		public static FreeFormRadioList<ItemIdType> Create<ItemIdType>( bool allowNoSelection, ItemIdType selectedItemId, bool disableSingleButtonDetection = false ) {
			return new FreeFormRadioList<ItemIdType>( allowNoSelection, disableSingleButtonDetection, selectedItemId );
		}
	}

	/// <summary>
	/// A radio button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public class FreeFormRadioList<ItemIdType>: DisplayLink {
		private readonly bool allowNoSelection;
		private readonly FormValue<CommonCheckBox> formValue;
		private readonly List<Action<PostBackValueDictionary>> displayLinkingSetInitialDisplayMethods = new List<Action<PostBackValueDictionary>>();
		private readonly List<Action> displayLinkingAddJavaScriptMethods = new List<Action>();
		private readonly List<Tuple<ItemIdType, CommonCheckBox>> itemIdsAndCheckBoxes = new List<Tuple<ItemIdType, CommonCheckBox>>();

		internal FreeFormRadioList( bool allowNoSelection, bool disableSingleButtonDetection, ItemIdType selectedItemId ) {
			this.allowNoSelection = allowNoSelection;
			formValue = RadioButtonGroup.GetFormValue( allowNoSelection,
			                                           () => from i in itemIdsAndCheckBoxes select i.Item2,
			                                           () =>
			                                           from i in itemIdsAndCheckBoxes where StandardLibraryMethods.AreEqual( i.Item1, selectedItemId ) select i.Item2,
			                                           v => getStringId( v != null ? itemIdsAndCheckBoxes.Single( i => i.Item2 == v ).Item1 : getNoSelectionItemId() ),
			                                           rawValue => from itemIdAndCheckBox in itemIdsAndCheckBoxes
			                                                       let control = (Control)itemIdAndCheckBox.Item2
			                                                       where control.IsOnPage() && getStringId( itemIdAndCheckBox.Item1 ) == rawValue
			                                                       select itemIdAndCheckBox.Item2 );

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
		public EwfCheckBox CreateInlineRadioButton( ItemIdType listItemId, string label = "", PostBack postBack = null, bool autoPostBack = false ) {
			validateListItem( listItemId );
			var checkBox = new EwfCheckBox( formValue, label, postBack, listItemId: getStringId( listItemId ) ) { AutoPostBack = autoPostBack };
			itemIdsAndCheckBoxes.Add( Tuple.Create<ItemIdType, CommonCheckBox>( listItemId, checkBox ) );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the list.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton( ItemIdType listItemId, string label = "", PostBack postBack = null, bool autoPostBack = false ) {
			validateListItem( listItemId );
			var checkBox = new BlockCheckBox( formValue, label, postBack, listItemId: getStringId( listItemId ) ) { AutoPostBack = autoPostBack };
			itemIdsAndCheckBoxes.Add( Tuple.Create<ItemIdType, CommonCheckBox>( listItemId, checkBox ) );
			return checkBox;
		}

		private void validateListItem( ItemIdType listItemId ) {
			if( allowNoSelection && StandardLibraryMethods.AreEqual( listItemId, getNoSelectionItemId() ) )
				throw new ApplicationException( "You cannot create a radio button with the ID that represents no selection." );
			if( itemIdsAndCheckBoxes.Any( i => getStringId( i.Item1 ) == getStringId( listItemId ) ) )
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
		}

		private string getStringId( ItemIdType id ) {
			return id.ObjectToString( true );
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
			var selectedPair = itemIdsAndCheckBoxes.SingleOrDefault( i => i.Item2 == formValue.GetValue( postBackValues ) );
			return selectedPair != null ? selectedPair.Item1 : getNoSelectionItemId();
		}

		private ItemIdType getNoSelectionItemId() {
			return StandardLibraryMethods.GetDefaultValue<ItemIdType>( true );
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return formValue.ValueChangedOnPostBack( postBackValues );
		}
	}
}