using EnterpriseWebLibrary.MailMerging.FieldImplementation;

namespace EnterpriseWebLibrary.MailMerging.PsuedoChildFields {
	public class Another: BasicMergeFieldImplementation<PseudoChildRow, string> {
		public string GetDescription() {
			return "Another value";
		}

		public string Evaluate( PseudoChildRow row ) {
			return "The value: " + row.Num * 2;
		}
	}
}