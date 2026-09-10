namespace Tests.MailMerging.DataStructure.TestFileDataStructure;

public class MergeTestData {
	public class Thing {
		public required string TheValue;
	}

	public required string FullName;
	public required List<Thing> Things;
}