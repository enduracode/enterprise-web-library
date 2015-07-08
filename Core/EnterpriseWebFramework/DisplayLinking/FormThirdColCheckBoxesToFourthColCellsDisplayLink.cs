using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A mapping between all check boxes in the third column of a form and all subsequent cells to the right of those checkboxes. When a check box is checked, the cells will be visible.
	/// </summary>
	public class FormThirdColCheckBoxesToFourthColCellsDisplayLink: DisplayLink {
		/// <summary>
		/// Creates a new display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage( Panel form ) {
			EwfPage.Instance.AddDisplayLink( new FormThirdColCheckBoxesToFourthColCellsDisplayLink( form ) );
		}

		private readonly Panel form;

		private FormThirdColCheckBoxesToFourthColCellsDisplayLink( Panel form ) {
			this.form = form;
		}

		void DisplayLink.AddJavaScript() {
			linkThirdColCheckBoxesToFourthColCellsDisplay( null );
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			linkThirdColCheckBoxesToFourthColCellsDisplay( formControlValues );
		}

		private void linkThirdColCheckBoxesToFourthColCellsDisplay( PostBackValueDictionary formControlValues ) {
			foreach( Control formChild in form.Controls ) {
				var panelIndex = 0;
				foreach( Control rowChild in formChild.Controls ) {
					if( rowChild is Panel ) {
						panelIndex += 1;
						if( panelIndex == 3 ) {
							linkFirstPanelCheckBoxToNextPanelDisplay( formControlValues, rowChild );
							break;
						}
					}
				}
			}
		}

		private static void linkFirstPanelCheckBoxToNextPanelDisplay( PostBackValueDictionary formControlValues, Control outerPanel ) {
			var panelIndex = 0;
			BlockCheckBox checkBox = null;
			Panel panel = null;
			foreach( var control in outerPanel.Controls.OfType<Panel>() ) {
				panelIndex += 1;
				if( panelIndex == 1 )
					checkBox = getFirstCheckBox( control );
				else if( panelIndex == 2 )
					panel = control;
			}
			if( checkBox != null && panel != null ) {
				if( formControlValues == null )
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( checkBox, true, panel );
				else
					DisplayLinkingOps.SetControlDisplay( panel, checkBox.IsCheckedInPostBack( formControlValues ) );
			}
		}

		private static BlockCheckBox getFirstCheckBox( Control parent ) {
			foreach( Control child in parent.Controls ) {
				if( child is EwfCheckBox )
					return child as BlockCheckBox;
				else if( getFirstCheckBox( child ) != null )
					return getFirstCheckBox( child );
			}
			return null;
		}
	}
}