namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class AutoCompleteItem {
		// Javascript is case-sensitive. These must be lowercase.
		// ReSharper disable InconsistentNaming
		public readonly string label;
		public readonly string value;
		// ReSharper restore InconsistentNaming

		public AutoCompleteItem( string label, string value ) {
			this.label = label;
			this.value = value;
		}
	}
}