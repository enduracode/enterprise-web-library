using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	/// <summary>
	/// A link between a list control and an array of dependent controls.
	/// </summary>
	internal class ListControlToControlArrayDisplayLink: DisplayLink {
		/// <summary>
		/// EwfListControl use only. Do not include controls other than WebControls or HtmlControls. Creates a new list control display link and adds it to the
		/// current EwfPage.
		/// </summary>
		internal static void AddToPage( DropDownList listControl, int selectedIndex, bool controlsVisibleOnIndex, params WebControl[] controls ) {
			EwfPage.Instance.AddDisplayLink( new ListControlToControlArrayDisplayLink( listControl, selectedIndex, controlsVisibleOnIndex, controls ) );
		}

		private readonly DropDownList listControl;
		private readonly int selectedIndex;
		private readonly bool controlsVisibleOnIndex;
		private readonly WebControl[] controls;

		private ListControlToControlArrayDisplayLink( DropDownList listControl, int selectedIndex, bool controlsVisibleOnIndex, params WebControl[] controls ) {
			this.listControl = listControl;
			this.selectedIndex = selectedIndex;
			this.controlsVisibleOnIndex = controlsVisibleOnIndex;
			this.controls = controls;
		}

		void DisplayLink.AddJavaScript() {
			var script = "";
			foreach( var c in controls ) {
				script = StringTools.ConcatenateWithDelimiter( ";",
				                                               script,
				                                               "setElementDisplay( '" + c.ClientID + "', selectedIndex " + ( controlsVisibleOnIndex ? "=" : "!" ) + "= " +
				                                               selectedIndex + " )" );
			}
			listControl.AddJavaScriptEventScript( JavaScriptWriting.JsWritingMethods.onchange, script );
		}

		void DisplayLink.SetInitialDisplay( PostBackValueDictionary formControlValues ) {
			foreach( var c in controls ) {
				var visible = ( controlsVisibleOnIndex && listControl.SelectedIndex == selectedIndex ) ||
				              ( !controlsVisibleOnIndex && listControl.SelectedIndex != selectedIndex );
				DisplayLinkingOps.SetControlDisplay( c, visible );
			}
		}
	}
}