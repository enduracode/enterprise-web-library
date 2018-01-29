using EnterpriseWebLibrary.MailMerging.FieldImplementation;

namespace EnterpriseWebLibrary.MailMerging.PseudoTableFields {
	internal class Test: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription() {
			return "Just a test.";
		}

		public string Evaluate( PseudoTableRow row ) {
			return "Test";
		}
	}
}