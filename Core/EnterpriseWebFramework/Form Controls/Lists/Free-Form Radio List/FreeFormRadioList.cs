#nullable disable
using JetBrains.Annotations;
using MoreLinq;
using Tewl.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

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
[ PublicAPI ]
public class FreeFormRadioList<ItemIdType> {
	private readonly FormValue<ElementId> formValue;
	private readonly bool? noSelectionIsValid;

	private readonly List<( ItemIdType itemId, ElementId buttonId, bool isReadOnly, PageModificationValue<bool> pmv )>
		itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples = new();

	private readonly FreeFormRadioListSetup<ItemIdType> listSetup;
	private readonly EwfValidation validation;

	internal FreeFormRadioList(
		bool? noSelectionIsValid, FreeFormRadioListSetup<ItemIdType> setup, ItemIdType selectedItemId, Action<ItemIdType, Validator> validationMethod ) {
		setup ??= FreeFormRadioListSetup.Create<ItemIdType>();

		formValue = RadioButtonGroup.GetFormValue(
			() => from i in itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples select ( i.buttonId, i.isReadOnly, EwlStatics.AreEqual( i.itemId, selectedItemId ) ),
			v => getStringId( v != null ? itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples.Single( i => i.buttonId == v ).itemId : getNoSelectionItemId() ),
			rawValue => from quadruple in itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples
			            let buttonId = quadruple.buttonId
			            where buttonId.Id.Any() && !quadruple.isReadOnly && getStringId( quadruple.itemId ) == rawValue
			            select buttonId,
			noSelectionIsValid.HasValue );

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

		PageBase.Current.AddControlTreeValidation(
			() => RadioButtonGroup.ValidateControls(
				noSelectionIsValid.HasValue,
				EwlStatics.AreEqual( getNoSelectionItemId(), selectedItemId ),
				from i in itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples select ( i.buttonId, i.isReadOnly, EwlStatics.AreEqual( i.itemId, selectedItemId ) ),
				setup.DisableSingleButtonDetection ) );
	}

	private ItemIdType getItemIdFromButtonId( ElementId buttonId ) =>
		itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples.Where( i => i.buttonId == buttonId )
			.Select( i => i.itemId )
			.FallbackIfEmpty( getNoSelectionItemId() )
			.Single();

	/// <summary>
	/// Creates a radio button that is part of the list.
	/// </summary>
	/// <param name="listItemId"></param>
	/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
	/// <param name="setup">The setup object for the radio button.</param>
	public Checkbox CreateRadioButton( ItemIdType listItemId, IReadOnlyCollection<PhrasingComponent> label, RadioButtonSetup setup = null ) {
		setup = setup?.AddPmv() ?? RadioButtonSetup.Create( pageModificationValue: new PageModificationValue<bool>() );

		validateListItem( listItemId );

		var id = new ElementId();
		formValue.AddPageModificationValue( setup.PageModificationValue, v => v == id );
		itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples.Add( ( listItemId, id, setup.IsReadOnly, setup.PageModificationValue ) );

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
				.Concat( itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples.Select( i => i.pmv.GetJsModificationStatements( i.buttonId == id ? "true" : "false" ) ) )
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
	public FlowCheckbox CreateFlowRadioButton( ItemIdType listItemId, IReadOnlyCollection<PhrasingComponent> label, FlowRadioButtonSetup setup = null ) {
		setup = setup?.AddPmv() ?? FlowRadioButtonSetup.Create( pageModificationValue: new PageModificationValue<bool>() );
		return new FlowCheckbox( setup, CreateRadioButton( listItemId, label, setup: setup.RadioButtonSetup ) );
	}

	private void validateListItem( ItemIdType listItemId ) {
		if( noSelectionIsValid.HasValue && EwlStatics.AreEqual( listItemId, getNoSelectionItemId() ) )
			throw new ApplicationException( "You cannot create a radio button with the ID that represents no selection." );
		if( itemIdAndButtonIdAndIsReadOnlyAndPmvQuadruples.Any( i => getStringId( i.itemId ) == getStringId( listItemId ) ) )
			throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
	}

	private ItemIdType getNoSelectionItemId() => EwlStatics.GetDefaultValue<ItemIdType>( true );

	private string getStringId( ItemIdType id ) => id.ObjectToString( true );

	public EwfValidation Validation => validation;
}