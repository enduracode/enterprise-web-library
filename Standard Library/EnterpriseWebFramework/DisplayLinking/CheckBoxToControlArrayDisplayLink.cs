using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A link between a check box (or radio button) and an array of dependent controls.
	/// </summary>
	public class CheckBoxToControlArrayDisplayLink: DisplayLink {
		/// <summary>
		/// Creates a new check box display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage( CommonCheckBox checkBox, bool controlsVisibleWhenBoxChecked, params WebControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new CheckBoxToControlArrayDisplayLink( checkBox, controlsVisibleWhenBoxChecked, controls ) );
		}

		/// <summary>
		/// Creates a new check box display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage( CommonCheckBox checkBox, bool controlsVisibleWhenBoxChecked, params HtmlControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new CheckBoxToControlArrayDisplayLink( checkBox, controlsVisibleWhenBoxChecked, controls ) );
		}

		private readonly CommonCheckBox checkBox;
		private readonly bool controlsVisibleWhenBoxChecked;
		private readonly Control[] controls;

		private CheckBoxToControlArrayDisplayLink( CommonCheckBox checkBox, bool controlsVisibleWhenBoxChecked, params Control[] controls ) {
			this.checkBox = checkBox;
			this.controlsVisibleWhenBoxChecked = controlsVisibleWhenBoxChecked;
			this.controls = controls;
		}

		void DisplayLink.AddJavaScript() {
			DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( checkBox, controlsVisibleWhenBoxChecked, controls );
			if( checkBox.GroupName.Length > 0 )
				addJavaScriptToOtherRadioButtonsInGroup( EwfPage.Instance );
		}

		private void addJavaScriptToOtherRadioButtonsInGroup( Control control ) {
			var cb = control as CommonCheckBox;
			if( cb != null && cb.GroupName == checkBox.GroupName && cb != checkBox )
				DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( cb, !controlsVisibleWhenBoxChecked, controls );
			foreach( Control childControl in control.Controls )
				addJavaScriptToOtherRadioButtonsInGroup( childControl );
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			foreach( var c in controls ) {
				if( c is WebControl )
					DisplayLinkingOps.SetControlDisplay( c as WebControl, controlsVisibleWhenBoxChecked == checkBox.IsCheckedInPostBack( formControlValues ) );
				else
					DisplayLinkingOps.SetControlDisplay( c as HtmlControl, controlsVisibleWhenBoxChecked == checkBox.IsCheckedInPostBack( formControlValues ) );
			}
		}
	}
}