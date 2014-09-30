using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibraryTester.MailMerging.DataStructure.TestFileDataStructure;

namespace RedStapler.StandardLibraryTester.MailMerging.MergeFields.TestFileMergeFields {
	public class FullName: BasicMergeFieldImplementation<MergeTestData, string> {
		public string GetDescription() {
			return "This is a full name.";
		}

		public string Evaluate( MergeTestData row ) {
			return row.FullName;
		}
	}
}