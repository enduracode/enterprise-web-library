using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PseudoTableFields {
	internal class FullName: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription() {
			return "Someone's full name";
		}

		public string Evaluate( PseudoTableRow row ) {
			return row.FullName;
		}
	}
}