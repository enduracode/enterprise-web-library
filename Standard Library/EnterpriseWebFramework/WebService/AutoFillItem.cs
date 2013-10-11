namespace RedStapler.StandardLibrary.EnterpriseWebFramework.WebService {
	public class AutoFillItem {
		// Javascript is case-sensitive. These must be lowercase.
		// ReSharper disable InconsistentNaming
		public readonly string label;
		public readonly string value;
		// ReSharper restore InconsistentNaming

		public AutoFillItem( string label, string value ) {
			this.label = label;
			this.value = value;
		}
	}
}