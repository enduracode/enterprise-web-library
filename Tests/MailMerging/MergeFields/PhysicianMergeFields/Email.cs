using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace Tests.MailMerging.MergeFields.PhysicianMergeFields;

internal class Email: BasicMergeFieldImplementation<PhysicianMockData, string> {
	public string GetDescription() {
		return "The physician's email";
	}

	public string Evaluate( PhysicianMockData row ) {
		return row.Email;
	}
}