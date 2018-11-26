using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
using EnterpriseWebLibrary.InputValidation;
using Humanizer;
using MoreLinq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A radio-button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public static class FreeFormRadioList {
		/// <summary>
		/// Creates a free-form radio button list.
		/// </summary>
		/// <param name="noSelectionIsValid">Pass a value to cause a selected item ID with the default value (or the empty string when the item ID type is string)
		/// to represent the state in which none of the radio buttons are selected. Note that this is not recommended by the Nielsen Norman Group; see
		/// http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information. If you do pass a value, passing true will cause this no-selection
		/// state to be valid.</param>
		/// <param name="selectedItemId"></param>
		/// <param name="setup">The setup object for the free-form radio list.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this radio-button list for page modification.</param>
		public static FreeFormRadioList<ItemIdType> Create<ItemIdType>(
			bool? noSelectionIsValid, ItemIdType selectedItemId, FreeFormRadioListSetup<ItemIdType> setup = null,
			Action<ItemIdType, Validator> validationMethod = null ) {
			return new FreeFormRadioList<ItemIdType>( noSelectionIsValid, setup, selectedItemId, validationMethod );
		}
	}

	/// <summary>
	/// A radio-button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public class FreeFormRadioList<ItemIdType> {
		private readonly FormValue<ElementId> formValue;
		private readonly bool? noSelectionIsValid;

		private readonly List<( ItemIdType itemId, ElementId buttonId, PageModificationValue<bool> pmv )> itemIdAndButtonIdAndPmvTriples =
			new List<( ItemIdType, ElementId, PageModificationValue<bool> )>();

		private readonly FreeFormRadioListSetup<ItemIdType> listSetup;
		private readonly EwfValidation validation;

		internal FreeFormRadioList(
			bool? noSelectionIsValid, FreeFormRadioListSetup<ItemIdType> setup, ItemIdType selectedItemId, Action<ItemIdType, Validator> validationMethod ) {
			setup = setup ?? FreeFormRadioListSetup.Create<ItemIdType>();

			formValue = RadioButtonGroup.GetFormValue(
				noSelectionIsValid.HasValue,
				() => from i in itemIdAndButtonIdAndPmvTriples select i.buttonId,
				() => from i in itemIdAndButtonIdAndPmvTriples where EwlStatics.AreEqual( i.itemId, selectedItemId ) select i.buttonId,
				v => getStringId( v != null ? itemIdAndButtonIdAndPmvTriples.Single( i => i.buttonId == v ).itemId : getNoSelectionItemId() ),
				rawValue => from itemIdAndButtonIdAndPmv in itemIdAndButtonIdAndPmvTriples
				            let buttonId = itemIdAndButtonIdAndPmv.buttonId
				            where buttonId.Id.Any() && getStringId( itemIdAndButtonIdAndPmv.itemId ) == rawValue
				            select buttonId );

			this.noSelectionIsValid = noSelectionIsValid;
			listSetup = setup;

			if( setup.ItemIdPageModificationValue != null )
				formValue.AddPageModificationValue( setup.ItemIdPageModificationValue, getItemIdFromButtonId );

			foreach( var i in setup.ItemMatchPageModificationSetups )
				formValue.AddPageModificationValue( i.PageModificationValue, v => i.ItemIds.Contains( getItemIdFromButtonId( v ) ) );

			if( validationMethod != null )
				validation = formValue.CreateValidation(
					( postBackValue, validator ) => {
						if( setup.ValidationPredicate != null && !setup.ValidationPredicate( postBackValue.ChangedOnPostBack ) )
							return;

						var postBackItemId = getItemIdFromButtonId( postBackValue.Value );
						if( noSelectionIsValid == false && EwlStatics.AreEqual( postBackItemId, getNoSelectionItemId() ) ) {
							validator.NoteErrorAndAddMessage( "Please make a selection." );
							setup.ValidationErrorNotifier?.Invoke();
							return;
						}

						validationMethod( postBackItemId, validator );
					} );

			EwfPage.Instance.AddControlTreeValidation(
				() => RadioButtonGroup.ValidateControls(
					noSelectionIsValid.HasValue,
					EwlStatics.AreEqual( getNoSelectionItemId(), selectedItemId ),
					from i in itemIdAndButtonIdAndPmvTriples where EwlStatics.AreEqual( i.itemId, selectedItemId ) select i.buttonId,
					from i in itemIdAndButtonIdAndPmvTriples select i.buttonId,
					setup.DisableSingleButtonDetection ) );
		}

		private ItemIdType getItemIdFromButtonId( ElementId buttonId ) =>
			itemIdAndButtonIdAndPmvTriples.Where( i => i.buttonId == buttonId ).Select( i => i.itemId ).FallbackIfEmpty( getNoSelectionItemId() ).Single();

		/// <summary>
		/// Creates a radio button that is part of the list.
		/// </summary>
		/// <param name="listItemId"></param>
		/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the radio button.</param>
		public Checkbox CreateRadioButton( ItemIdType listItemId, IReadOnlyCollection<PhrasingComponent> label, RadioButtonSetup setup = null ) {
			setup = setup ?? RadioButtonSetup.Create();

			validateListItem( listItemId );

			var id = new ElementId();
			formValue.AddPageModificationValue( setup.PageModificationValue, v => v == id );
			itemIdAndButtonIdAndPmvTriples.Add( ( listItemId, id, setup.PageModificationValue ) );

			return new Checkbox(
				formValue,
				id,
				setup,
				label,
				listSetup.SelectionChangedAction,
				() => StringTools.ConcatenateWithDelimiter(
					" ",
					( listSetup.ItemIdPageModificationValue?.GetJsModificationStatements( "'{0}'".FormatWith( getStringId( listItemId ) ) ) ?? "" ).ToCollection()
					.Concat(
						listSetup.ItemMatchPageModificationSetups.Select(
							i => i.PageModificationValue.GetJsModificationStatements( i.ItemIds.Contains( listItemId ) ? "true" : "false" ) ) )
					.Concat( itemIdAndButtonIdAndPmvTriples.Select( i => i.pmv.GetJsModificationStatements( i.buttonId == id ? "true" : "false" ) ) )
					.ToArray() ),
				null,
				listItemId: getStringId( listItemId ) );
		}

		/// <summary>
		/// Creates a flow radio button that is part of the list.
		/// </summary>
		/// <param name="listItemId"></param>
		/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the flow radio button.</param>
		public FlowCheckbox CreateBlockRadioButton( ItemIdType listItemId, IReadOnlyCollection<PhrasingComponent> label, FlowRadioButtonSetup setup = null ) {
			setup = setup ?? FlowRadioButtonSetup.Create();
			return new FlowCheckbox( setup, CreateRadioButton( listItemId, label, setup: setup.RadioButtonSetup ) );
		}

		private void validateListItem( ItemIdType listItemId ) {
			if( noSelectionIsValid.HasValue && EwlStatics.AreEqual( listItemId, getNoSelectionItemId() ) )
				throw new ApplicationException( "You cannot create a radio button with the ID that represents no selection." );
			if( itemIdAndButtonIdAndPmvTriples.Any( i => getStringId( i.itemId ) == getStringId( listItemId ) ) )
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
		}

		private ItemIdType getNoSelectionItemId() => EwlStatics.GetDefaultValue<ItemIdType>( true );

		private string getStringId( ItemIdType id ) => id.ToString();

		public EwfValidation Validation => validation;
	}

	[ Obsolete( "Guaranteed through 28 Feb 2019. Use FreeFormRadioList instead." ) ]
	public static class LegacyFreeFormRadioList {
		/// <summary>
		/// Creates a free-form radio button list.
		/// </summary>
		/// <param name="allowNoSelection">Pass true to cause a selected item ID with the default value (or the empty string when the item ID type is string) to
		/// represent the state in which none of the radio buttons are selected. Note that this is not recommended by the Nielsen Norman Group; see
		/// http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="selectedItemId"></param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this list. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="itemIdPageModificationValue"></param>
		/// <param name="itemMatchPageModificationSetups"></param>
		public static LegacyFreeFormRadioList<ItemIdType> Create<ItemIdType>(
			bool allowNoSelection, ItemIdType selectedItemId, bool disableSingleButtonDetection = false,
			PageModificationValue<ItemIdType> itemIdPageModificationValue = null,
			IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups = null ) {
			return new LegacyFreeFormRadioList<ItemIdType>(
				allowNoSelection,
				disableSingleButtonDetection,
				selectedItemId,
				itemIdPageModificationValue,
				itemMatchPageModificationSetups );
		}
	}

	/// <summary>
	/// A radio button list that allows you to arrange the buttons on the page however you wish. If you want access to the individual selection state of each
	/// radio button and do not need the concept of a selected item ID for the group, use RadioButtonGroup instead.
	/// </summary>
	public class LegacyFreeFormRadioList<ItemIdType>: DisplayLink {
		private readonly bool allowNoSelection;
		private readonly FormValue<CommonCheckBox> formValue;
		private readonly List<Action<PostBackValueDictionary>> displayLinkingSetInitialDisplayMethods = new List<Action<PostBackValueDictionary>>();
		private readonly List<Action> displayLinkingAddJavaScriptMethods = new List<Action>();
		private readonly List<Tuple<ItemIdType, CommonCheckBox>> itemIdsAndCheckBoxes = new List<Tuple<ItemIdType, CommonCheckBox>>();

		internal LegacyFreeFormRadioList(
			bool allowNoSelection, bool disableSingleButtonDetection, ItemIdType selectedItemId, PageModificationValue<ItemIdType> itemIdPageModificationValue,
			IEnumerable<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups ) {
			itemMatchPageModificationSetups = itemMatchPageModificationSetups ?? ImmutableArray<ListItemMatchPageModificationSetup<ItemIdType>>.Empty;

			this.allowNoSelection = allowNoSelection;
			formValue = LegacyRadioButtonGroup.GetFormValue(
				allowNoSelection,
				() => from i in itemIdsAndCheckBoxes select i.Item2,
				() => from i in itemIdsAndCheckBoxes where EwlStatics.AreEqual( i.Item1, selectedItemId ) select i.Item2,
				v => getStringId( v != null ? itemIdsAndCheckBoxes.Single( i => i.Item2 == v ).Item1 : getNoSelectionItemId() ),
				rawValue => from itemIdAndCheckBox in itemIdsAndCheckBoxes
				            let control = (Control)itemIdAndCheckBox.Item2
				            where control.IsOnPage() && getStringId( itemIdAndCheckBox.Item1 ) == rawValue
				            select itemIdAndCheckBox.Item2 );

			EwfPage.Instance.AddControlTreeValidation(
				() => LegacyRadioButtonGroup.ValidateControls(
					allowNoSelection,
					EwlStatics.AreEqual( getNoSelectionItemId(), selectedItemId ),
					itemIdsAndCheckBoxes.Select( i => i.Item2 ),
					disableSingleButtonDetection ) );

			if( itemIdPageModificationValue != null ) {
				formValue.AddPageModificationValue( itemIdPageModificationValue, getItemIdFromCheckBox );
				displayLinkingAddJavaScriptMethods.Add(
					() => {
						foreach( var pair in itemIdsAndCheckBoxes )
							pair.Item2.AddOnClickJsMethod(
								itemIdPageModificationValue.GetJsModificationStatements( "'{0}'".FormatWith( pair.Item1.ObjectToString( true ) ) ) );
					} );
			}
			foreach( var setup in itemMatchPageModificationSetups ) {
				formValue.AddPageModificationValue( setup.PageModificationValue, checkBox => setup.ItemIds.Contains( getItemIdFromCheckBox( checkBox ) ) );
				displayLinkingAddJavaScriptMethods.Add(
					() => {
						foreach( var pair in itemIdsAndCheckBoxes )
							pair.Item2.AddOnClickJsMethod(
								setup.PageModificationValue.GetJsModificationStatements( setup.ItemIds.Contains( pair.Item1 ) ? "true" : "false" ) );
					} );
			}

			EwfPage.Instance.AddDisplayLink( this );
		}

		public void AddDisplayLink( IEnumerable<ItemIdType> itemIds, bool controlsVisibleOnMatch, IEnumerable<WebControl> controls ) {
			itemIds = itemIds.ToArray();
			controls = controls.ToArray();
			displayLinkingSetInitialDisplayMethods.Add(
				formControlValues => {
					var match = itemIds.Contains( GetSelectedItemIdInPostBack( formControlValues ) );
					var visible = ( controlsVisibleOnMatch && match ) || ( !controlsVisibleOnMatch && !match );
					foreach( var i in controls )
						DisplayLinkingOps.SetControlDisplay( i, visible );
				} );
			displayLinkingAddJavaScriptMethods.Add(
				() => {
					foreach( var pair in itemIdsAndCheckBoxes ) {
						DisplayLinkingOps.AddDisplayJavaScriptToCheckBox(
							pair.Item2,
							itemIds.Contains( pair.Item1 ) ? controlsVisibleOnMatch : !controlsVisibleOnMatch,
							controls.ToArray() );
					}
				} );
		}

		/// <summary>
		/// Creates an in-line radio button that is part of the list.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton( ItemIdType listItemId, string label = "", FormAction action = null, bool autoPostBack = false ) {
			validateListItem( listItemId );
			var checkBox =
				new EwfCheckBox( formValue, label, action, () => ImmutableArray<string>.Empty, listItemId: getStringId( listItemId ) ) { AutoPostBack = autoPostBack };
			itemIdsAndCheckBoxes.Add( Tuple.Create<ItemIdType, CommonCheckBox>( listItemId, checkBox ) );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the list.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton(
			ItemIdType listItemId, string label = "", FormAction action = null, bool autoPostBack = false,
			Func<IEnumerable<Control>> nestedControlListGetter = null ) {
			validateListItem( listItemId );
			var checkBox = new BlockCheckBox(
				formValue,
				new BlockCheckBoxSetup( action: action, triggersActionWhenCheckedOrUnchecked: autoPostBack, nestedControlListGetter: nestedControlListGetter ),
				label.ToComponents(),
				() => ImmutableArray<string>.Empty,
				null,
				listItemId: getStringId( listItemId ) );
			itemIdsAndCheckBoxes.Add( Tuple.Create<ItemIdType, CommonCheckBox>( listItemId, checkBox ) );
			return checkBox;
		}

		private void validateListItem( ItemIdType listItemId ) {
			if( allowNoSelection && EwlStatics.AreEqual( listItemId, getNoSelectionItemId() ) )
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
			return getItemIdFromCheckBox( formValue.GetValue( postBackValues ) );
		}

		private ItemIdType getItemIdFromCheckBox( CommonCheckBox checkBox ) {
			var pair = itemIdsAndCheckBoxes.SingleOrDefault( i => i.Item2 == checkBox );
			return pair != null ? pair.Item1 : getNoSelectionItemId();
		}

		private ItemIdType getNoSelectionItemId() {
			return EwlStatics.GetDefaultValue<ItemIdType>( true );
		}

		/// <summary>
		/// Returns true if the selection changed on this post back.
		/// </summary>
		public bool SelectionChangedOnPostBack( PostBackValueDictionary postBackValues ) {
			return formValue.ValueChangedOnPostBack( postBackValues );
		}
	}
}