namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	internal class EwfTableFieldOrItemSetup {
		internal readonly ElementClassSet Classes;
		internal readonly CssLength Size;
		internal readonly TextAlignment TextAlignment;
		internal readonly TableCellVerticalAlignment VerticalAlignment;
		internal readonly ElementActivationBehavior ActivationBehavior;

		internal EwfTableFieldOrItemSetup(
			ElementClassSet classes, CssLength size, TextAlignment textAlignment, TableCellVerticalAlignment verticalAlignment,
			ElementActivationBehavior activationBehavior ) {
			Classes = classes ?? ElementClassSet.Empty;
			Size = size;
			TextAlignment = textAlignment;
			VerticalAlignment = verticalAlignment;
			ActivationBehavior = activationBehavior;
		}
	}
}