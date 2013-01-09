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

		private readonly string groupName;
		private readonly bool allowNoSelection;
		private readonly List<CommonCheckBox> checkBoxes = new List<CommonCheckBox>();

		/// <summary>
		/// Creates a radio button group.
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="allowNoSelection">Pass true to allow the state in which none of the radio buttons are selected. Note that this is not recommended by the
		/// Nielsen Norman Group; see http://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ for more information.</param>
		/// <param name="disableSingleButtonDetection">Pass true to allow just a single radio button to be displayed for this group. Use with caution, as this
		/// violates the HTML specification.</param>
		public RadioButtonGroup( string groupName, bool allowNoSelection, bool disableSingleButtonDetection = false ) {
			this.groupName = groupName;
			this.allowNoSelection = allowNoSelection;
			EwfPage.Instance.AddControlTreeValidation(
				() => ValidateControls( allowNoSelection, checkBoxes.All( i => !i.IsChecked ), checkBoxes, disableSingleButtonDetection ) );
		}

		/// <summary>
		/// Creates an in-line radio button that is part of the group.
		/// </summary>
		public EwfCheckBox CreateInlineRadioButton( bool isSelected, string label = "", bool autoPostBack = false ) {
			var checkBox = new EwfCheckBox( isSelected, label: label ) { GroupName = groupName, AutoPostBack = autoPostBack };
			checkBox.PostBackValueSelector =
				isChecked => checkBox.IsOnPage() && isChecked || !allowNoSelection && checkBox == checkBoxes.First( i => ( i as Control ).IsOnPage() );
			checkBoxes.Add( checkBox );
			return checkBox;
		}

		/// <summary>
		/// Creates a block-level radio button that is part of the group.
		/// </summary>
		public BlockCheckBox CreateBlockRadioButton( bool isSelected, string label = "", bool autoPostBack = false ) {
			var checkBox = new BlockCheckBox( isSelected, label: label ) { GroupName = groupName, AutoPostBack = autoPostBack };
			checkBox.PostBackValueSelector =
				isChecked => checkBox.IsOnPage() && isChecked || !allowNoSelection && checkBox == checkBoxes.First( i => ( i as Control ).IsOnPage() );
			checkBoxes.Add( checkBox );
			return checkBox;
		}
	}
}