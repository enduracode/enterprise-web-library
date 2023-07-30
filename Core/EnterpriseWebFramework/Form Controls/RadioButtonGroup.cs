#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Tewl.InputValidation;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A radio button group. Use this when you want access to the individual selection state of each radio button and do not need the concept of a selected item
	/// ID for the group. Otherwise use FreeFormRadioList.
	/// </summary>
	public class RadioButtonGroup {
		internal static FormValue<ElementId> GetFormValue(
			Func<IEnumerable<( ElementId id, bool isReadOnly, bool isSelected )>> buttonGetter, Func<ElementId, string> stringValueSelector,
			Func<string, IEnumerable<ElementId>> selectedButtonIdInPostBackGetter, bool allowsNoSelection ) {
			FormValue<ElementId> formValue = null;
			return formValue = new FormValue<ElementId>(
				       () => buttonGetter().Where( i => i.isSelected ).Select( i => i.id ).FirstOrDefault(),
				       () => buttonGetter().Where( i => i.id.Id.Any() && !i.isReadOnly ).Select( i => i.id ).FirstOrDefault()?.Id ?? "",
				       stringValueSelector,
				       rawValue => {
					       if( rawValue != null ) {
						       var selectedButtonId = selectedButtonIdInPostBackGetter( rawValue ).SingleOrDefault();
						       return selectedButtonId != null
							              ? PostBackValueValidationResult<ElementId>.CreateValid( selectedButtonId )
							              : PostBackValueValidationResult<ElementId>.CreateInvalid();
					       }

					       var durableValue = formValue.GetDurableValue();
					       if( durableValue != null ) {
						       var button = buttonGetter().Single( i => i.id == durableValue );
						       if( !button.id.Id.Any() || button.isReadOnly )
							       return PostBackValueValidationResult<ElementId>.CreateValid( durableValue );
					       }

					       return allowsNoSelection
						              ? PostBackValueValidationResult<ElementId>.CreateValid( null )
						              : PostBackValueValidationResult<ElementId>.CreateInvalid();
				       } );
		}

		internal static void ValidateControls(
			bool allowsNoSelection, bool inNoSelectionState, IEnumerable<( ElementId id, bool isReadOnly, bool isSelected )> buttons,
			bool disableSingleButtonDetection ) {
			if( ( !allowsNoSelection || !inNoSelectionState ) && buttons.Count( i => i.isSelected ) != 1 )
				throw new ApplicationException( "If a radio button group is not in the no-selection state, then exactly one radio button must be selected." );

			var activeButtons = buttons.Where( i => i.id.Id.Any() && !i.isReadOnly ).Materialize();
			if( activeButtons.Any() && !disableSingleButtonDetection && activeButtons.Count < 2 ) {
				const string link = "http://developers.whatwg.org/states-of-the-type-attribute.html#radio-button-state-%28type=radio%29";
				throw new ApplicationException( "A radio button group must contain more than one element; see " + link + "." );
			}
		}

		private readonly FormValue<ElementId> formValue;

		private readonly List<( ElementId id, bool isReadOnly, bool value, PageModificationValue<bool> pmv )> buttonIdAndIsReadOnlyAndValueAndPmvQuadruples =
			new List<( ElementId, bool, bool, PageModificationValue<bool> )>();

		private readonly FormAction selectionChangedAction;

		/// <summary>
		/// Creates a radio button group.
		/// </summary>
		/// <param name="allowNoSelection">Pass true to allow the state in which none of the radio buttons are selected. Note that this is not recommended by the
		/// Nielsen Norman Group; see http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this group. Use with caution, as this
		/// violates the HTML specification.</param>
		/// <param name="selectionChangedAction">The action that will occur when the selection is changed. Pass null for no action.</param>
		public RadioButtonGroup( bool allowNoSelection, bool disableSingleButtonDetection = false, FormAction selectionChangedAction = null ) {
			formValue = GetFormValue(
				() => from i in buttonIdAndIsReadOnlyAndValueAndPmvQuadruples select ( i.id, i.isReadOnly, i.value ),
				v => v?.Id ?? "",
				rawValue => from quadruple in buttonIdAndIsReadOnlyAndValueAndPmvQuadruples
				            let id = quadruple.id
				            where id.Id.Any() && !quadruple.isReadOnly && id.Id == rawValue
				            select id,
				allowNoSelection );

			this.selectionChangedAction = selectionChangedAction;

			PageBase.Current.AddControlTreeValidation(
				() => ValidateControls(
					allowNoSelection,
					buttonIdAndIsReadOnlyAndValueAndPmvQuadruples.All( i => !i.value ),
					from i in buttonIdAndIsReadOnlyAndValueAndPmvQuadruples select ( i.id, i.isReadOnly, i.value ),
					disableSingleButtonDetection ) );
		}

		/// <summary>
		/// Creates a radio button that is part of the group.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the radio button.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using this control for page modification.</param>
		public Checkbox CreateRadioButton(
			bool value, IReadOnlyCollection<PhrasingComponent> label, RadioButtonSetup setup = null,
			Action<PostBackValue<bool>, Validator> validationMethod = null ) {
			setup = setup ?? RadioButtonSetup.Create();

			var id = new ElementId();
			formValue.AddPageModificationValue( setup.PageModificationValue, v => v == id );
			buttonIdAndIsReadOnlyAndValueAndPmvQuadruples.Add( ( id, setup.IsReadOnly, value, setup.PageModificationValue ) );

			return new Checkbox(
				formValue,
				id,
				setup,
				label,
				selectionChangedAction,
				() => StringTools.ConcatenateWithDelimiter(
					" ",
					buttonIdAndIsReadOnlyAndValueAndPmvQuadruples.Select( i => i.pmv.GetJsModificationStatements( i.id == id ? "true" : "false" ) ).ToArray() ),
				validationMethod != null
					? formValue.CreateValidation(
						( postBackValue, validator ) => validationMethod(
							new PostBackValue<bool>( postBackValue.Value == id, postBackValue.ChangedOnPostBack ),
							validator ) )
					: null );
		}

		/// <summary>
		/// Creates a flow radio button that is part of the group.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="label">The radio button label. Do not pass null. Pass an empty collection for no label.</param>
		/// <param name="setup">The setup object for the flow radio button.</param>
		/// <param name="validationMethod">The validation method. Pass null if you’re only using the radio button for page modification.</param>
		public FlowCheckbox CreateFlowRadioButton(
			bool value, IReadOnlyCollection<PhrasingComponent> label, FlowRadioButtonSetup setup = null,
			Action<PostBackValue<bool>, Validator> validationMethod = null ) {
			setup = setup ?? FlowRadioButtonSetup.Create();
			return new FlowCheckbox( setup, CreateRadioButton( value, label, setup: setup.RadioButtonSetup, validationMethod: validationMethod ) );
		}
	}
}