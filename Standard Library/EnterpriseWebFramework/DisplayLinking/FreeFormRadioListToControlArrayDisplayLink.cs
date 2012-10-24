using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A link between a free-form radio button list and an array of dependent controls.
	/// </summary>
	public class FreeFormRadioListToControlArrayDisplayLink: DisplayLink {
		/// <summary>
		/// Creates a new display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage( FreeFormRadioList radioList, string selectedValue, bool controlsVisibleOnValue, params WebControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new FreeFormRadioListToControlArrayDisplayLink( radioList, selectedValue, controlsVisibleOnValue, controls ) );
		}

		/// <summary>
		/// Creates a new display link and adds it to the current EwfPage.
		/// </summary>
		public static void AddToPage( FreeFormRadioList radioList, string selectedValue, bool controlsVisibleOnValue, params HtmlControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new FreeFormRadioListToControlArrayDisplayLink( radioList, selectedValue, controlsVisibleOnValue, controls ) );
		}

		/// <summary>
		/// Framework use only. Do not include controls other than WebControls or HtmlControls. Creates a new display link and adds it to the current EwfPage.
		/// </summary>
		internal static void AddToPage( FreeFormRadioList radioList, string selectedValue, bool controlsVisibleOnValue, params Control[] controls ) {
			EwfPage.Instance.AddDisplayLink( new FreeFormRadioListToControlArrayDisplayLink( radioList, selectedValue, controlsVisibleOnValue, controls ) );
		}

		private readonly FreeFormRadioList radioList;
		private readonly string selectedValue;
		private readonly bool controlsVisibleOnValue;
		private readonly Control[] controls;

		private FreeFormRadioListToControlArrayDisplayLink( FreeFormRadioList radioList, string selectedValue, bool controlsVisibleOnValue, params Control[] controls ) {
			this.radioList = radioList;
			this.selectedValue = selectedValue;
			this.controlsVisibleOnValue = controlsVisibleOnValue;
			this.controls = controls;
		}

		void DisplayLink.AddJavaScript() {
			foreach( var kvp in radioList.ValuesAndRadioButtons ) {
				if( kvp.Key == selectedValue )
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( kvp.Value, controlsVisibleOnValue, controls );
				else
					DisplayLinkingOps.AddDisplayJavaScriptToCheckBox( kvp.Value, !controlsVisibleOnValue, controls );
			}
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			foreach( var c in controls ) {
				var visible = ( controlsVisibleOnValue && radioList.SelectedValue == selectedValue ) ||
				              ( !controlsVisibleOnValue && radioList.SelectedValue != selectedValue );
				if( c is WebControl )
					DisplayLinkingOps.SetControlDisplay( c as WebControl, visible );
				else
					DisplayLinkingOps.SetControlDisplay( c as HtmlControl, visible );
			}
		}
	}
}