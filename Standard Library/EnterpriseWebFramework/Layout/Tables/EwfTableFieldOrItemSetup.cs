using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	internal class EwfTableFieldOrItemSetup {
		internal readonly List<string> Classes;
		internal readonly Unit Size;
		internal readonly TextAlignment TextAlignment;
		internal readonly TableCellVerticalAlignment VerticalAlignment;
		internal readonly ClickScript ClickScript;
		internal readonly string ToolTip;
		internal readonly Control ToolTipControl;

		internal EwfTableFieldOrItemSetup( IEnumerable<string> classes, Unit? size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
		                                   ClickScript clickScript, string toolTip, Control toolTipControl ) {
			Classes = ( classes ?? new string[0] ).ToList();
			Size = size ?? Unit.Empty;
			TextAlignment = textAlignment;
			VerticalAlignment = verticalAlignment;
			ClickScript = clickScript;
			ToolTip = toolTip;
			ToolTipControl = toolTipControl;
		}
	}
}