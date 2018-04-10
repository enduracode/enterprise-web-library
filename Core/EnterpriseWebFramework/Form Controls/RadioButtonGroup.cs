using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A radio button group. Use this when you want access to the individual selection state of each radio button and do not need the concept of a selected item
	/// ID for the group. Otherwise use FreeFormRadioList.
	/// </summary>
	public class RadioButtonGroup {
		internal static FormValue<CommonCheckBox> GetFormValue(
			bool allowsNoSelection, Func<IEnumerable<CommonCheckBox>> allCheckBoxesGetter, Func<IEnumerable<CommonCheckBox>> checkedCheckBoxesGetter,
			Func<CommonCheckBox, string> stringValueSelector, Func<string, IEnumerable<CommonCheckBox>> checkedCheckBoxesInPostBackGetter ) {
			return new FormValue<CommonCheckBox>(
				() => checkedCheckBoxesGetter().FirstOrDefault(),
				() => {
					var firstCheckBoxOnPage = allCheckBoxesGetter().Select( i => (Control)i ).FirstOrDefault( i => i.IsOnPage() );
					return firstCheckBoxOnPage != null ? firstCheckBoxOnPage.UniqueID : "";
				},
				stringValueSelector,
				rawValue => {
					if( rawValue != null ) {
						var selectedButton = checkedCheckBoxesInPostBackGetter( rawValue ).SingleOrDefault();
						return selectedButton != null
							       ? PostBackValueValidationResult<CommonCheckBox>.CreateValid( selectedButton )
							       : PostBackValueValidationResult<CommonCheckBox>.CreateInvalid();
					}
					return allowsNoSelection
						       ? PostBackValueValidationResult<CommonCheckBox>.CreateValid( null )
						       : PostBackValueValidationResult<CommonCheckBox>.CreateInvalid();
				} );
		}

		internal static void ValidateControls(
			bool allowsNoSelection, bool inNoSelectionState, IEnumerable<CommonCheckBox> checkBoxes, bool disableSingleButtonDetection ) {
			Control selectedButton = null;
			if( !allowsNoSelection || !inNoSelectionState ) {
				var selectedButtons = checkBoxes.Where( i => i.IsChecked ).ToArray();
				if( selectedButtons.Count() != 1 )
					throw new ApplicationException( "If a radio button group is not in the no-selection state, then exactly one radio button must be selected." );
				selectedButton = selectedButtons.Single() as Control;
			}

			var checkBoxesOnPage = checkBoxes.Where( i => ( i as Control ).IsOnPage() ).ToArray();
			if( checkBoxesOnPage.Any() ) {
				if( selectedButton != null && !selectedButton.IsOnPage() )
					throw new ApplicationException( "The selected radio button must be on the page." );
				if( !disableSingleButtonDetection && checkBoxesOnPage.Count() < 2 ) {
					const string link = "http://developers.whatwg.org/states-of-the-type-attribute.html#radio-button-state-%28type=radio%29";
					throw new ApplicationException( "A radio button group must contain more than one element; see " + link + "." );
				}
			}
		}

		private readonly FormValue<CommonCheckBox> formValue;

		private readonly List<Tuple<CommonCheckBox, bool, PageModificationValue<bool>>> checkBoxesAndSelectionStatesAndPageModificationValues =
			new List<Tuple<CommonCheckBox, bool, PageModificationValue<bool>>>();

		/// <summary>
		/// Creates a radio button group.
		/// </summary>
		/// <param name="allowNoSelection">Pass true to allow the state in which none of the radio buttons are selected. Note that this is not recommended by the
		/// Nielsen Norman Group; see http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this group. Use with caution, as this
		/// violates the HTML specification.</param>
		public RadioButtonGroup( bool allowNoSelection, bool disableSingleButtonDetection = false ) {
			formValue = GetFormValue(
				allowNoSelection,
				() => from i in checkBoxesAndSelectionStatesAndPageModificationValues select i.Item1,
				() => from i in checkBoxesAndSelectionStatesAndPageModificationValues where i.Item2 select i.Item1,
				v => v != null ? ( (Control)v ).UniqueID : "",
				rawValue => from checkBoxAndSelectionState in checkBoxesAndSelectionStatesAndPageModificationValues
				            let control = (Control)checkBoxAndSelectionState.Item1
				            where control.IsOnPage() && control.UniqueID == rawValue
				            select checkBoxAndSelectionState.Item1 );

			EwfPage.Instance.AddControlTreeValidation(
				() => ValidateControls(
					allowNoSelection,
					checkBoxesAndSelectionStatesAndPageModificationValues.All( i => !i.Item2 ),
					checkBoxesAndSelectionStatesAndPageModificationValues.Select( i => i.Item1 ),
					disableSingleButtonDetection ) );
		}

		/// <summary>
		/// Creates an in-line radio button that is part of the group.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton(
			bool isSelected, string label = "", FormAction action = null, bool autoPostBack = false, PageModificationValue<bool> pageModificationValue = null ) {
			EwfCheckBox checkBox = null;
			checkBox = new EwfCheckBox(
				formValue,
				label,
				action,
				() => checkBoxesAndSelectionStatesAndPageModificationValues.Where( i => i.Item3 != null )
					.Select( i => i.Item3.GetJsModificationStatements( i.Item1 == checkBox ? "true" : "false" ) ) ) { AutoPostBack = autoPostBack };
			checkBoxesAndSelectionStatesAndPageModificationValues.Add(
				Tuple.Create<CommonCheckBox, bool, PageModificationValue<bool>>( checkBox, isSelected, pageModificationValue ) );

			if( pageModificationValue != null )
				formValue.AddPageModificationValue( pageModificationValue, value => value == checkBox );

			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the group.
		/// </summary>
		/// <param name="isSelected"></param>
		/// <param name="validationMethod">The validation method. Do not pass null.</param>
		/// <param name="label"></param>
		/// <param name="action"></param>
		/// <param name="autoPostBack"></param>
		/// <param name="pageModificationValue"></param>
		/// <param name="nestedControlListGetter"></param>
		/// <returns></returns>
		public BlockCheckBox CreateBlockRadioButton(
			bool isSelected, Action<PostBackValue<bool>, Validator> validationMethod, string label = "", FormAction action = null, bool autoPostBack = false,
			PageModificationValue<bool> pageModificationValue = null, Func<IEnumerable<Control>> nestedControlListGetter = null ) {
			BlockCheckBox checkBox = null;
			var validation = formValue.CreateValidation(
				( postBackValue, validator ) => validationMethod(
					new PostBackValue<bool>( postBackValue.Value == checkBox, postBackValue.ChangedOnPostBack ),
					validator ) );

			checkBox = new BlockCheckBox(
				formValue,
				new BlockCheckBoxSetup( action: action, triggersActionWhenCheckedOrUnchecked: autoPostBack, nestedControlListGetter: nestedControlListGetter ),
				label.ToComponents(),
				() => checkBoxesAndSelectionStatesAndPageModificationValues.Where( i => i.Item3 != null )
					.Select( i => i.Item3.GetJsModificationStatements( i.Item1 == checkBox ? "true" : "false" ) ),
				validation );
			checkBoxesAndSelectionStatesAndPageModificationValues.Add(
				Tuple.Create<CommonCheckBox, bool, PageModificationValue<bool>>( checkBox, isSelected, pageModificationValue ) );

			if( pageModificationValue != null )
				formValue.AddPageModificationValue( pageModificationValue, value => value == checkBox );

			return checkBox;
		}
	}
}