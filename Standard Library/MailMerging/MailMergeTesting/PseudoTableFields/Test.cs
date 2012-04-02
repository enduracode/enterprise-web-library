using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PseudoTableFields {
	internal class Test: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription( DBConnection cn ) {
			return "Just a test.";
		}

		public string Evaluate( DBConnection cn, PseudoTableRow row ) {
			return "Test";
		}
	}
}