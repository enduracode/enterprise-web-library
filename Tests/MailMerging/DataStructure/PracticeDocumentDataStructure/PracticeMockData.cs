using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PhysicianDataStructure;
using Tests.MailMerging.DataStructure.PracticeDocumentDataStructure.PracticeManagerDataStructure;

namespace Tests.MailMerging.DataStructure.PracticeDocumentDataStructure;

public class PracticeMockData {
	public readonly string PracticeName;
	internal IEnumerable<PracticeManagerMockData> Managers { get; private set; }
	public IEnumerable<PhysicianMockData> Physicians { get; private set; }

	public PracticeMockData( string practiceName, IEnumerable<PracticeManagerMockData> managers, IEnumerable<PhysicianMockData> physicians ) {
		PracticeName = practiceName;
		Managers = managers;
		Physicians = physicians;
	}
}