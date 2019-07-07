using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking {
	internal static class DisplayLinkingOps {
		internal static void SetControlDisplay( WebControl control, bool visible ) {
			var c = control;
			if( visible )
				c.Style.Remove( HtmlTextWriterStyle.Display );
			else
				c.Style.Add( HtmlTextWriterStyle.Display, "none" );
		}
	}
}