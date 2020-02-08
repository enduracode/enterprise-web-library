using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Controls {
	internal class EwfTableFieldOrItemSetup {
		internal readonly List<string> Classes;
		internal readonly Unit Size;
		internal readonly TextAlignment TextAlignment;
		internal readonly TableCellVerticalAlignment VerticalAlignment;
		internal readonly ElementActivationBehavior ActivationBehavior;
		internal readonly string ToolTip;
		internal readonly Control ToolTipControl;

		internal EwfTableFieldOrItemSetup(
			IEnumerable<string> classes, Unit? size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
			ElementActivationBehavior activationBehavior, string toolTip, Control toolTipControl ) {
			Classes = ( classes ?? new string[ 0 ] ).ToList();
			Size = size ?? Unit.Empty;
			TextAlignment = textAlignment;
			VerticalAlignment = verticalAlignment;
			ActivationBehavior = activationBehavior;
			ToolTip = toolTip;
			ToolTipControl = toolTipControl;
		}
	}
}