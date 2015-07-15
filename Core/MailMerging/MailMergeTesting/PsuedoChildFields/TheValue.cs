using EnterpriseWebLibrary.MailMerging.FieldImplementation;

namespace EnterpriseWebLibrary.MailMerging.MailMergeTesting.PsuedoChildFields {
	public class TheValue: BasicMergeFieldImplementation<PseudoChildRow, string> {
		public string GetDescription() {
			return "The value";
		}

		public string Evaluate( PseudoChildRow row ) {
			return "The value: " + row.Num;
		}
	}
}