using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibrary.MailMerging.MailMergeTesting.PsuedoChildFields {
	public class TheValue: BasicMergeFieldImplementation<PseudoChildRow, string> {
		public string GetDescription( DBConnection cn ) {
			return "The value";
		}

		public string Evaluate( DBConnection cn, PseudoChildRow row ) {
			return "The value: " + row.Num.ToString();
		}
	}
}