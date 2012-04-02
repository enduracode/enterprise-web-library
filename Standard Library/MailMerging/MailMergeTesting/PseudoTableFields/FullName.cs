using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PseudoTableFields {
	internal class FullName: BasicMergeFieldImplementation<PseudoTableRow, string> {
		public string GetDescription( DBConnection cn ) {
			return "Someone's full name";
		}

		public string Evaluate( DBConnection cn, PseudoTableRow row ) {
			return row.FullName;
		}
	}
}