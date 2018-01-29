using EnterpriseWebLibrary.MailMerging.FieldImplementation;

namespace EnterpriseWebLibrary.MailMerging.MailMergeTesting.PseudoTableFields {
	internal class FullName: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription() {
			return "Someone's full name";
		}

		public string Evaluate( PseudoTableRow row ) {
			return row.FullName;
		}
	}
}