using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A radio button group. Use this when you want access to the individual selection state of each radio button and do not need the concept of a selected item
	/// ID for the group. Otherwise use FreeFormRadioList.
	/// </summary>
	public class RadioButtonGroup {
		internal static FormValue<CommonCheckBox> GetFormValue( bool allowsNoSelection, Func<IEnumerable<CommonCheckBox>> allCheckBoxesGetter,
		                                                        Func<IEnumerable<CommonCheckBox>> checkedCheckBoxesGetter,
		                                                        Func<CommonCheckBox, string> stringValueSelector,
		                                                        Func<string, IEnumerable<CommonCheckBox>> checkedCheckBoxesInPostBackGetter ) {
			return new FormValue<CommonCheckBox>( () => checkedCheckBoxesGetter().FirstOrDefault(),
			                                      () => {
				                                      var firstCheckBoxOnPage = allCheckBoxesGetter().Select( i => (Control)i ).FirstOrDefault( i => i.IsOnPage() );
				                                      return firstCheckBoxOnPage != null ? firstCheckBoxOnPage.UniqueID : "";
			                                      },
			                                      stringValueSelector,
			                                      rawValue => {
				                                      if( rawValue != null ) {
					                                      var selectedButton = checkedCheckBoxesInPostBackGetter( rawValue ).SingleOrDefault();
					                                      return selectedButton != null
						                                             ? PostBackValueValidationResult<CommonCheckBox>.CreateValidWithValue( selectedButton )
						                                             : PostBackValueValidationResult<CommonCheckBox>.CreateInvalid();
				                                      }
				                                      return allowsNoSelection
					                                             ? PostBackValueValidationResult<CommonCheckBox>.CreateValidWithValue( null )
					                                             : PostBackValueValidationResult<CommonCheckBox>.CreateInvalid();
			                                      } );
		}

		internal static void ValidateControls( bool allowsNoSelection, bool inNoSelectionState, IEnumerable<CommonCheckBox> checkBoxes,
		                                       bool disableSingleButtonDetection ) {
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
		private readonly List<Tuple<CommonCheckBox, bool>> checkBoxesAndSelectionStates = new List<Tuple<CommonCheckBox, bool>>();

		/// <summary>
		/// Creates a radio button group.
		/// </summary>
		/// <param name="allowNoSelection">Pass true to allow the state in which none of the radio buttons are selected. Note that this is not recommended by the
		/// Nielsen Norman Group; see http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this group. Use with caution, as this
		/// violates the HTML specification.</param>
		public RadioButtonGroup( bool allowNoSelection, bool disableSingleButtonDetection = false ) {
			formValue = GetFormValue( allowNoSelection,
			                          () => from i in checkBoxesAndSelectionStates select i.Item1,
			                          () => from i in checkBoxesAndSelectionStates where i.Item2 select i.Item1,
			                          v => v != null ? ( (Control)v ).UniqueID : "",
			                          rawValue => from checkBoxAndSelectionState in checkBoxesAndSelectionStates
			                                      let control = (Control)checkBoxAndSelectionState.Item1
			                                      where control.IsOnPage() && control.UniqueID == rawValue
			                                      select checkBoxAndSelectionState.Item1 );

			EwfPage.Instance.AddControlTreeValidation(
				() =>
				ValidateControls( allowNoSelection,
				                  checkBoxesAndSelectionStates.All( i => !i.Item2 ),
				                  checkBoxesAndSelectionStates.Select( i => i.Item1 ),
				                  disableSingleButtonDetection ) );
		}

		[ Obsolete( "Guaranteed through 30 November 2013. Please use the other constructor." ) ]
		public RadioButtonGroup( string groupName, bool allowNoSelection, bool disableSingleButtonDetection = false )
			: this( allowNoSelection, disableSingleButtonDetection: disableSingleButtonDetection ) {}

		/// <summary>
		/// Creates an in-line radio button that is part of the group.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton( bool isSelected, string label = "", bool autoPostBack = false ) {
			var checkBox = new EwfCheckBox( formValue, label ) { AutoPostBack = autoPostBack };
			checkBoxesAndSelectionStates.Add( Tuple.Create<CommonCheckBox, bool>( checkBox, isSelected ) );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the group.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton( bool isSelected, string label = "", bool autoPostBack = false ) {
			var checkBox = new BlockCheckBox( formValue, label ) { AutoPostBack = autoPostBack };
			checkBoxesAndSelectionStates.Add( Tuple.Create<CommonCheckBox, bool>( checkBox, isSelected ) );
			return checkBox;
		}
	}
}