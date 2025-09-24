using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace Tests.MailMerging.MergeFields.PhysicianMergeFields;

public class PhysicianEmail: BasicMergeFieldImplementation<PhysicianMockData, string> {
	public string GetDescription() {
		return "The physician's email";
	}

	public string Evaluate( PhysicianMockData row ) {
		return row.Email;
	}
}