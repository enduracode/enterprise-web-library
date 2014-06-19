using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibraryTester.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace RedStapler.StandardLibraryTester.MailMerging.MergeFields.PhysicianMergeFields {
	public class PhysicianEmail: BasicMergeFieldImplementation<PhysicianMockData, string> {
		public string GetDescription() {
			return "The physician's email";
		}

		public string Evaluate( PhysicianMockData row ) {
			return row.Email;
		}
	}
}