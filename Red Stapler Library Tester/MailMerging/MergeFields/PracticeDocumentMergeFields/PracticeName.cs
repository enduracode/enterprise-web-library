using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibraryTester.MailMerging.DataStructure.PracticeDocumentDataStructure;

namespace RedStapler.StandardLibraryTester.MailMerging.MergeFields.PracticeDocumentMergeFields {
	public class PracticeName: BasicMergeFieldImplementation<PracticeMockData, string> {
		public string GetDescription() {
			return "The Practice's name";
		}

		public string Evaluate( PracticeMockData row ) {
			return row.PracticeName;
		}
	}
}