using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Humanizer;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	// We can't nest this inside the class below because of the type parameter.
	internal class CheckboxListCssElementCreator: ControlCssElementCreator {
		internal static readonly ElementClass ListClass = new ElementClass( "ewfCheckboxList" );
		internal static readonly ElementClass ActionContainerClass = new ElementClass( "ewfClA" );
		internal static readonly ElementClass ContentContainerClass = new ElementClass( "ewfClC" );

		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			return new[]
				{
					new CssElement( "CheckboxList", "div.{0}".FormatWith( ListClass.ClassName ) ),
					new CssElement( "CheckboxListActionContainer", "div.{0}".FormatWith( ActionContainerClass.ClassName ) ),
					new CssElement( "CheckboxListContentContainer", "div.{0}".FormatWith( ContentContainerClass.ClassName ) )
				};
		}
	}

	/// <summary>
	/// A checkbox list, which allows multiple items to be selected.
	/// NOTE: Consider using something like the multi select feature of http://harvesthq.github.com/chosen/ to provide a space-saving mode for this control.
	/// </summary>
	public sealed class CheckboxList<ItemIdType>: FormControl<FlowComponent> {
		public FlowComponent PageComponent { get; }
		public EwfValidation Validation { get; }

		/// <summary>
		/// Creates a checkbox list.
		/// </summary>
		/// <param name="setup">The setup object for the checkbox list. Do not pass null.</param>
		/// <param name="value">The selected-item IDs.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public CheckboxList(
			CheckboxListSetup<ItemIdType> setup, IEnumerable<ItemIdType> value, Action<IReadOnlyCollection<ItemIdType>, Validator> validationMethod = null ) {
			var valueSet = value.ToImmutableHashSet();

			var selectedItemIdsInPostBack = new List<ItemIdType>();
			var selectionChangedOnPostBack = false;
			var checkboxes = setup.Items.Select(
					i => new FlowCheckbox(
						valueSet.Contains( i.Id ),
						i.Label.ToComponents(),
						setup: FlowCheckboxSetup.Create(
							highlightedWhenChecked: true,
							action: new SpecifiedValue<FormAction>( setup.Action ),
							valueChangedAction: setup.SelectionChangedAction ),
						validationMethod: ( postBackValue, validator ) => {
							if( postBackValue.Value )
								selectedItemIdsInPostBack.Add( i.Id );
							selectionChangedOnPostBack = selectionChangedOnPostBack || postBackValue.ChangedOnPostBack;
						} ) )
				.Materialize();

			var contentContainerId = new ElementId();
			PageComponent = new GenericFlowContainer(
				( setup.IncludeSelectAndDeselectAllButtons
					  ? new GenericFlowContainer(
						  new InlineList(
							  new EwfButton(
									  new StandardButtonStyle(
										  "Select All",
										  buttonSize: ButtonSize.ShrinkWrap,
										  icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-check-square-o" ) ) ),
									  behavior: new CustomButtonBehavior(
										  () => "$( '#{0}' ).find( 'input[type=checkbox]:not(:checked)' ).click();".FormatWith( contentContainerId.Id ) ) ).ToCollection()
								  .ToComponentListItem()
								  .ToCollection()
								  .Append(
									  new EwfButton(
											  new StandardButtonStyle(
												  "Deselect All",
												  buttonSize: ButtonSize.ShrinkWrap,
												  icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-square-o" ) ) ),
											  behavior: new CustomButtonBehavior(
												  () => "$( '#{0}' ).find( 'input[type=checkbox]:checked' ).click();".FormatWith( contentContainerId.Id ) ) ).ToCollection()
										  .ToComponentListItem() ) ).ToCollection(),
						  classes: CheckboxListCssElementCreator.ActionContainerClass ).ToCollection()
					  : Enumerable.Empty<FlowComponent>() ).Append(
					new DisplayableElement(
						context => new DisplayableElementData(
							null,
							() => new DisplayableElementLocalData(
								"div",
								focusDependentData: new DisplayableElementFocusDependentData(
									attributes: setup.MinColumnWidth != null
										            ? new ElementAttribute( "style", "column-width: {0}".FormatWith( ( (CssLength)setup.MinColumnWidth ).Value ) ).ToCollection()
										            : null,
									includeIdAttribute: true ) ),
							classes: CheckboxListCssElementCreator.ContentContainerClass,
							clientSideIdReferences: contentContainerId.ToCollection(),
							children: new RawList( from i in checkboxes select i.PageComponent.ToComponentListItem() ).ToCollection() ) ) )
				.Materialize(),
				displaySetup: setup.DisplaySetup,
				classes: CheckboxListCssElementCreator.ListClass );

			if( validationMethod != null )
				Validation = new EwfValidation(
					validator => {
						if( setup.ValidationPredicate != null && !setup.ValidationPredicate( selectionChangedOnPostBack ) )
							return;
						validationMethod( selectedItemIdsInPostBack, validator );
					} );
		}

		FormControlLabeler FormControl<FlowComponent>.Labeler => null;
	}
}