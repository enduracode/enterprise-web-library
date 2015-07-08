using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking {
	internal static class DisplayLinkingOps {
		internal static void SetControlDisplay( WebControl control, bool visible ) {
			var c = control;
			if( visible )
				c.Style.Remove( HtmlTextWriterStyle.Display );
			else
				c.Style.Add( HtmlTextWriterStyle.Display, "none" );
		}

		internal static void AddDisplayJavaScriptToCheckBox( CommonCheckBox checkBox, bool controlsVisibleWhenBoxChecked, params WebControl[] controls ) {
			var s = "";
			foreach( var c in controls )
				s += "setElementDisplay( '" + c.ClientID + "', " + ( controlsVisibleWhenBoxChecked ? "" : "!" ) + "checked );";
			checkBox.AddOnClickJsMethod( s );
		}
	}
}