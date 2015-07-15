using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using EnterpriseWebLibrary.Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace EnterpriseWebLibrary.Tests.MailMerging.MergeFields.PhysicianMergeFields {
	internal class Email: BasicMergeFieldImplementation<PhysicianMockData, string> {
		public string GetDescription() {
			return "The physician's email";
		}

		public string Evaluate( PhysicianMockData row ) {
			return row.Email;
		}
	}
}