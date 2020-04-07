using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class EwfTableFieldOrItemSetup {
		internal readonly List<string> Classes;
		internal readonly CssLength Size;
		internal readonly TextAlignment TextAlignment;
		internal readonly TableCellVerticalAlignment VerticalAlignment;
		internal readonly ElementActivationBehavior ActivationBehavior;

		internal EwfTableFieldOrItemSetup(
			IEnumerable<string> classes, CssLength size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
			ElementActivationBehavior activationBehavior ) {
			Classes = ( classes ?? new string[ 0 ] ).ToList();
			Size = size;
			TextAlignment = textAlignment;
			VerticalAlignment = verticalAlignment;
			ActivationBehavior = activationBehavior;
		}
	}
}