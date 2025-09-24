using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure;

namespace Tests.MailMerging.MergeFields.PracticeDocumentMergeFields;

public class PracticeName: BasicMergeFieldImplementation<PracticeMockData, string> {
	public string GetDescription() {
		return "The Practice's name";
	}

	public string Evaluate( PracticeMockData row ) {
		return row.PracticeName;
	}
}