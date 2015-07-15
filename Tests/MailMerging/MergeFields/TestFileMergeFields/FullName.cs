using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.TestFileDataStructure;

namespace EnterpriseWebLibrary.Tests.MailMerging.MergeFields.TestFileMergeFields {
	public class FullName: BasicMergeFieldImplementation<MergeTestData, string> {
		public string GetDescription() {
			return "This is a full name.";
		}

		public string Evaluate( MergeTestData row ) {
			return row.FullName;
		}
	}
}