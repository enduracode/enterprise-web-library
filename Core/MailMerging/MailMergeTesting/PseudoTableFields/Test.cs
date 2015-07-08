using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PseudoTableFields {
	internal class Test: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription() {
			return "Just a test.";
		}

		public string Evaluate( PseudoTableRow row ) {
			return "Test";
		}
	}
}