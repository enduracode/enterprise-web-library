using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using RedStapler.StandardLibraryTester.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace RedStapler.StandardLibraryTester.MailMerging.MergeFields.PhysicianMergeFields {
	internal class LastName: BasicMergeFieldImplementation<PhysicianMockData, string> {
		public string GetDescription() {
			return "The physician's last name";
		}

		public string Evaluate( PhysicianMockData row ) {
			return row.LastName;
		}
	}
}