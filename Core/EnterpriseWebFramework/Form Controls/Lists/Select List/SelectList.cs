using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using Humanizer;
using JetBrains.Annotations;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// This control should never support custom-text scenarios. An essential element of SelectList is that each item has both a label and an ID, and custom text
	// cannot meet this requirement. TextControl would be a more appropriate place to implement custom-text "combo boxes".

	/// <summary>
	/// A drop-down or radio-button list.
	/// </summary>
	public static class SelectList {
		internal static readonly ElementClass DropDownClass = new ElementClass( "ewfDropDown" );

		// This class name is used by the third-party select-css file.
		internal static readonly ElementClass SelectCssClass = new ElementClass( "select-css" );

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() =>
				new[]
					{
						new CssElement( "DropDownList", "select.{0}".FormatWith( SelectCssClass.ClassName ), ".chosen-container" ),
						new CssElement( "DropDownListContainer", "div.{0}".FormatWith( DropDownClass.ClassName ) )
					};
		}

		public static IEnumerable<SelectListItem<bool?>> GetYesNoItems() {
			return GetTrueFalseItems( "Yes", "No" );
		}

		public static IEnumerable<SelectListItem<bool?>> GetTrueFalseItems( string trueLabel, string falseLabel ) {
			return new[] { SelectListItem.Create<bool?>( true, trueLabel ), SelectListItem.Create<bool?>( false, falseLabel ) };
		}

		/// <summary>
		/// Creates a radio-button list.
		/// </summary>
		/// <param name="setup">The setup object for the radio list.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear and therefore none of the radio buttons will be
		/// selected if the selected item ID has the default value and none of the list items do.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this radio-button list for page modification.</param>
		public static SelectList<ItemIdType> CreateRadioList<ItemIdType>(
			RadioListSetup<ItemIdType> setup, ItemIdType selectedItemId, string defaultValueItemLabel = "", Action<ItemIdType, Validator> validationMethod = null ) =>
			new SelectList<ItemIdType>(
				setup.DisplaySetup,
				setup.UseHorizontalLayout,
				null,
				null,
				setup.IsReadOnly,
				setup.Classes,
				setup.UnlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				null,
				null,
				setup.Items,
				setup.FreeFormSetup.DisableSingleButtonDetection,
				selectedItemId,
				"",
				setup.Action,
				setup.FreeFormSetup.SelectionChangedAction,
				setup.FreeFormSetup.ItemIdPageModificationValue,
				setup.FreeFormSetup.ItemMatchPageModificationSetups,
				setup.FreeFormSetup.ValidationPredicate,
				setup.FreeFormSetup.ValidationErrorNotifier,
				validationMethod );

		/// <summary>
		/// Creates a drop-down list.
		/// </summary>
		/// <param name="setup">The setup object for the drop-down.</param>
		/// <param name="selectedItemId">The ID of the selected item. This must either match a list item or be the default value of the type, unless an unlisted
		/// selected item label getter is passed.</param>
		/// <param name="defaultValueItemLabel">The label of the default-value item, which will appear first, and only if none of the list items have an ID with the
		/// default value. Do not pass null. If you pass the empty string, no default-value item will appear.</param>
		/// <param name="placeholderIsValid">Pass true if you would like the list to include a default-value placeholder that is considered a valid selection.
		/// This will only be included if none of the list items have an ID with the default value and the default-value item label is the empty string. If you pass
		/// false, the list will still include a default-value placeholder if the selected item ID has the default value and none of the list items do, but in this
		/// case the placeholder will not be considered a valid selection.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public static SelectList<ItemIdType> CreateDropDown<ItemIdType>(
			DropDownSetup<ItemIdType> setup, ItemIdType selectedItemId, string defaultValueItemLabel = "", bool placeholderIsValid = false,
			Action<ItemIdType, Validator> validationMethod = null ) =>
			new SelectList<ItemIdType>(
				setup.DisplaySetup,
				null,
				setup.UseNativeControl,
				setup.Width,
				setup.IsReadOnly,
				setup.Classes,
				setup.UnlistedSelectedItemLabelGetter,
				defaultValueItemLabel,
				placeholderIsValid,
				setup.PlaceholderText,
				setup.Items,
				null,
				selectedItemId,
				setup.AutoFillTokens,
				setup.Action,
				setup.SelectionChangedAction,
				setup.ItemIdPageModificationValue,
				setup.ItemMatchPageModificationSetups,
				setup.ValidationPredicate,
				setup.ValidationErrorNotifier,
				validationMethod );
	}

	/// <summary>
	/// A drop-down or radio-button list.
	/// </summary>
	public class SelectList<ItemIdType>: FormControl<FlowComponent> {
		private class ListItem {
			private readonly SelectListItem<ItemIdType> item;
			private readonly bool isValid;
			private readonly bool isPlaceholder;

			internal ListItem( SelectListItem<ItemIdType> item, bool isValid, bool isPlaceholder ) {
				this.item = item;
				this.isValid = isValid;
				this.isPlaceholder = isPlaceholder;
			}

			internal SelectListItem<ItemIdType> Item => item;

			internal string StringId =>
				// Represent the default value with the empty string to support drop-down list placeholders. The HTML spec states that the "placeholder label option"
				// must have a value of the empty string. See https://html.spec.whatwg.org/multipage/forms.html#the-select-element.
				EwlStatics.AreEqual( item.Id, EwlStatics.GetDefaultValue<ItemIdType>( false ) ) ? "" : item.Id.ToString();

			internal bool IsValid => isValid;
			internal bool IsPlaceholder => isPlaceholder;
		}

		public FormControlLabeler Labeler { get; }
		public FlowComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		internal SelectList(
			DisplaySetup displaySetup, bool? useHorizontalRadioLayout, bool? useNativeDropDownControl, ContentBasedLength width, bool isReadOnly,
			ElementClassSet classes, Func<ItemIdType, string> unlistedSelectedItemLabelGetter, string defaultValueItemLabel, bool? placeholderIsValid,
			string placeholderText, IEnumerable<SelectListItem<ItemIdType>> listItems, bool? disableSingleRadioButtonDetection, ItemIdType selectedItemId,
			string autoFillTokens, FormAction action, FormAction selectionChangedAction, PageModificationValue<ItemIdType> itemIdPageModificationValue,
			IReadOnlyCollection<ListItemMatchPageModificationSetup<ItemIdType>> itemMatchPageModificationSetups, Func<bool, bool> validationPredicate,
			Action validationErrorNotifier, Action<ItemIdType, Validator> validationMethod ) {
			var items = listItems.Select( i => new ListItem( i, true, false ) ).ToImmutableArray();
			items = getInitialItems(
					!useHorizontalRadioLayout.HasValue,
					unlistedSelectedItemLabelGetter,
					defaultValueItemLabel,
					placeholderIsValid,
					placeholderText,
					items,
					selectedItemId )
				.Concat( items )
				.ToImmutableArray();
			if( items.All( i => !i.IsValid ) )
				throw new ApplicationException( "There must be at least one valid selection in the list." );

			ImmutableDictionary<string, SelectListItem<ItemIdType>> itemsByStringId;
			try {
				itemsByStringId = items.ToImmutableDictionary( i => i.StringId, i => i.Item );
			}
			catch( ArgumentException ) {
				throw new ApplicationException( "Item IDs, when converted to strings, must be unique." );
			}

			if( useHorizontalRadioLayout.HasValue ) {
				var freeFormList = FreeFormRadioList.Create(
					items.All( i => i.IsValid ) ? null : (bool?)false,
					selectedItemId,
					setup: FreeFormRadioListSetup.Create(
						disableSingleButtonDetection: disableSingleRadioButtonDetection.Value,
						selectionChangedAction: selectionChangedAction,
						itemIdPageModificationValue: itemIdPageModificationValue,
						itemMatchPageModificationSetups: itemMatchPageModificationSetups,
						validationPredicate: validationPredicate,
						validationErrorNotifier: validationErrorNotifier ),
					validationMethod: validationMethod );

				var radioButtons = from i in items
				                   where i.IsValid
				                   select freeFormList.CreateRadioButton(
					                   i.Item.Id,
					                   label: i.Item.Label.ToComponents(),
					                   setup: isReadOnly
						                          ? RadioButtonSetup.CreateReadOnly()
						                          : RadioButtonSetup.Create( action: new SpecifiedValue<FormAction>( action ) ) );
				PageComponent = new GenericFlowContainer(
					useHorizontalRadioLayout.Value
						? new LineList( from i in radioButtons select (LineListItem)i.PageComponent.ToCollection().ToComponentListItem() ).ToCollection<FlowComponent>()
						: new StackList( from i in radioButtons select i.PageComponent.ToCollection().ToComponentListItem() ).ToCollection(),
					displaySetup: displaySetup,
					classes: classes );

				Validation = freeFormList.Validation;
			}
			else {
				itemIdPageModificationValue = itemIdPageModificationValue ?? new PageModificationValue<ItemIdType>();

				Labeler = new FormControlLabeler();

				var id = new ElementId();
				var formValue = new FormValue<ItemIdType>(
					() => selectedItemId,
					() => isReadOnly ? "" : id.Id,
					v => v.ObjectToString( true ),
					rawValue => rawValue != null && itemsByStringId.ContainsKey( rawValue )
						            ? PostBackValueValidationResult<ItemIdType>.CreateValid( itemsByStringId[ rawValue ].Id )
						            : PostBackValueValidationResult<ItemIdType>.CreateInvalid() );

				// Drop-down lists need a container to allow Chosen to be easily shown and hidden.
				PageComponent = new DisplayableElement(
					containerContext => new DisplayableElementData(
						displaySetup,
						() => new DisplayableElementLocalData(
							"div",
							focusDependentData: new DisplayableElementFocusDependentData(
								includeIdAttribute: !isReadOnly,
								jsInitStatements: !isReadOnly
									                  ? SubmitButton.GetImplicitSubmissionKeyPressStatements( action, useNativeDropDownControl.Value )
										                  .Surround( "$( '#{0}' ).keypress( function( e ) {{ ".FormatWith( containerContext.Id ), " } );" )
									                  : "" ) ),
						classes: SelectList.DropDownClass.Add( classes ?? ElementClassSet.Empty ),
						clientSideIdReferences: id.ToCollection(),
						children: new DisplayableElement(
							context => {
								if( !isReadOnly ) {
									action?.AddToPageIfNecessary();
									selectionChangedAction?.AddToPageIfNecessary();
								}

								return new DisplayableElementData(
									null,
									() => {
										var attributes = new List<ElementAttribute>();
										if( isReadOnly )
											attributes.Add( new ElementAttribute( "disabled" ) );
										else
											attributes.Add( new ElementAttribute( "name", containerContext.Id ) );
										if( autoFillTokens.Any() )
											attributes.Add( new ElementAttribute( "autocomplete", autoFillTokens ) );
										if( width != null )
											attributes.Add( new ElementAttribute( "style", "width: {0}".FormatWith( ( (CssLength)width ).Value ) ) );

										return new DisplayableElementLocalData(
											"select",
											new FocusabilityCondition( !isReadOnly ),
											isFocused => {
												if( isFocused )
													attributes.Add( new ElementAttribute( "autofocus" ) );
												return new DisplayableElementFocusDependentData(
													attributes: attributes,
													includeIdAttribute: true,
													jsInitStatements: StringTools.ConcatenateWithDelimiter(
														" ",
														selectionChangedAction != null
															? "$( '#{0}' ).change( function() {{ {1} }} );".FormatWith( context.Id, selectionChangedAction.GetJsStatements() )
															: "",
														StringTools.ConcatenateWithDelimiter(
																" ",
																( itemIdPageModificationValue?.GetJsModificationStatements( "$( this ).val()" ) ?? "" ).ToCollection()
																.Concat(
																	itemMatchPageModificationSetups.Select(
																		setup => setup.PageModificationValue.GetJsModificationStatements(
																			"[ {0} ].indexOf( $( this ).val() ) != -1".FormatWith(
																				StringTools.ConcatenateWithDelimiter(
																					", ",
																					setup.ItemIds.Select( i => "'" + i.ObjectToString( true ) + "'" ).ToArray() ) ) ) ) )
																.ToArray() )
															.Surround( "$( '#{0}' ).change( function() {{ ".FormatWith( context.Id ), " } );" ),
														getChosenLogic( useNativeDropDownControl.Value, width, items, isFocused )
															.Surround( "$( '#{0}' )".FormatWith( context.Id ), ";" ) ) );
											} );
									},
									classes: SelectList.SelectCssClass,
									clientSideIdReferences: Labeler.ControlId.ToCollection(),
									children: items.Select(
											i => getOption(
												i.StringId,
												i.IsPlaceholder ? "" : i.Item.Label,
												() => EwlStatics.AreEqual( i.Item.Id, itemIdPageModificationValue.Value ) ) )
										.Materialize() );
							} ).ToCollection() ),
					formValue: formValue );

				formValue.AddPageModificationValue( itemIdPageModificationValue, v => v );
				foreach( var i in itemMatchPageModificationSetups )
					formValue.AddPageModificationValue( i.PageModificationValue, v => i.ItemIds.Contains( v ) );

				if( validationMethod != null )
					Validation = formValue.CreateValidation(
						( postBackValue, validator ) => {
							if( validationPredicate != null && !validationPredicate( postBackValue.ChangedOnPostBack ) )
								return;

							if( !items.Single( i => EwlStatics.AreEqual( i.Item.Id, postBackValue.Value ) ).IsValid ) {
								validator.NoteErrorAndAddMessage( "Please make a selection." );
								validationErrorNotifier?.Invoke();
								return;
							}

							validationMethod( postBackValue.Value, validator );
						} );
			}
		}

		private IEnumerable<ListItem> getInitialItems(
			bool isDropDown, Func<ItemIdType, string> unlistedSelectedItemLabelGetter, string defaultValueItemLabel, bool? placeholderIsValid, string placeholderText,
			IReadOnlyCollection<ListItem> items, ItemIdType selectedItemId ) {
			var itemIdDefaultValue = EwlStatics.GetDefaultValue<ItemIdType>( true );
			var selectedItemIdHasDefaultValue = EwlStatics.AreEqual( selectedItemId, itemIdDefaultValue );

			if( !items.Any( i => EwlStatics.AreEqual( i.Item.Id, selectedItemId ) ) && !selectedItemIdHasDefaultValue ) {
				if( unlistedSelectedItemLabelGetter == null )
					throw new ApplicationException( "The selected item ID must either match a list item or be the default value of the type." );
				yield return new ListItem( SelectListItem.Create( selectedItemId, unlistedSelectedItemLabelGetter( selectedItemId ) + " (invalid)" ), true, false );
			}

			if( items.Any( i => EwlStatics.AreEqual( i.Item.Id, itemIdDefaultValue ) ) )
				yield break;

			var includeDefaultValueItemOrValidPlaceholder = defaultValueItemLabel.Any() || ( isDropDown && placeholderIsValid.Value );
			if( !selectedItemIdHasDefaultValue && !includeDefaultValueItemOrValidPlaceholder )
				yield break;

			var isPlaceholder = isDropDown && !defaultValueItemLabel.Any();
			yield return new ListItem(
				SelectListItem.Create( itemIdDefaultValue, isPlaceholder ? placeholderText : defaultValueItemLabel ),
				includeDefaultValueItemOrValidPlaceholder,
				isPlaceholder );
		}

		private string getChosenLogic( bool useNativeControl, ContentBasedLength width, ImmutableArray<ListItem> items, bool isFocused ) {
			var placeholderItem = items.SingleOrDefault( i => i.IsPlaceholder );

			// Chosen’s allow_single_deselect only works if the placeholder is the first item.
			var chosenLogic = !useNativeControl && ( placeholderItem == null || placeholderItem == items.First() )
				                  ? ".chosen( {{ {0} }} )".FormatWith(
					                  StringTools.ConcatenateWithDelimiter(
						                  ", ",
						                  placeholderItem != null && placeholderItem.IsValid ? "allow_single_deselect: true" : "",
						                  placeholderItem != null
							                  // Don't let the placeholder value be the empty string since this seems to confuse Chosen.
							                  ? "placeholder_text_single: '{0}'".FormatWith(
								                  placeholderItem.Item.Label.Any() ? HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) : " " )
							                  : "",
						                  "search_contains: true",
						                  "width: '{0}'".FormatWith( width != null ? ( (CssLength)width ).Value : "" ) ) )
				                  : "";
			if( isFocused )
				chosenLogic = chosenLogic.PrependDelimiter( ".on( 'chosen:ready', function() { $( this ).trigger( 'chosen:activate' ) } )" );

			// Do this after .chosen since we only want it to affect the native select.
			var placeholderTextLogic = placeholderItem != null
				                           ? ".children().eq( {0} ).text( '{1}' )".FormatWith(
					                           items.IndexOf( placeholderItem ),
					                           HttpUtility.JavaScriptStringEncode( placeholderItem.Item.Label ) )
				                           : "";

			return chosenLogic + placeholderTextLogic;
		}

		private FlowComponent getOption( string value, string label, Func<bool> selectedGetter ) =>
			new ElementComponent(
				context => new ElementData(
					() => {
						var attributes = new List<ElementAttribute>();
						attributes.Add( new ElementAttribute( "value", value ) );
						if( selectedGetter() )
							attributes.Add( new ElementAttribute( "selected" ) );

						return new ElementLocalData( "option", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
					},
					children: label.ToComponents( disableNewlineReplacement: true ) ) );
	}
}