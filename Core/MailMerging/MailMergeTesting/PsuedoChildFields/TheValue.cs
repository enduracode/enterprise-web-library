using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PsuedoChildFields {
	public class TheValue: BasicMergeFieldImplementation<PseudoChildRow, string> {
		public string GetDescription() {
			return "The value";
		}

		public string Evaluate( PseudoChildRow row ) {
			return "The value: " + row.Num;
		}
	}
}