using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;

namespace Tests.MailMerging.MergeFields.PhysicianMergeFields;

internal class FirstName: BasicMergeFieldImplementation<PhysicianMockData, string> {
	public string GetDescription() {
		return "The physician's first name";
	}

	public string Evaluate( PhysicianMockData row ) {
		return row.FirstName;
	}
}