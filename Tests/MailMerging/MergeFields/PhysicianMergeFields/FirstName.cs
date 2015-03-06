using RedStapler.StandardLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace EnterpriseWebLibrary.Tests.MailMerging.MergeFields.PhysicianMergeFields {
	internal class FirstName: BasicMergeFieldImplementation<PhysicianMockData, string> {
		public string GetDescription() {
			return "The physician's first name";
		}

		public string Evaluate( PhysicianMockData row ) {
			return row.FirstName;
		}
	}
}