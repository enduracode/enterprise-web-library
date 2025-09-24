using EnterpriseWebLibrary.MailMerging.FieldImplementation;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PracticeManagerDataStructure;

namespace Tests.MailMerging.MergeFields.PracticeManagerMergeFields;

public class Email: BasicMergeFieldImplementation<PracticeManagerMockData, string> {
	public string GetDescription() {
		return "The practice manager's email";
	}

	public string Evaluate( PracticeManagerMockData row ) {
		return row.EmailAddress;
	}
}