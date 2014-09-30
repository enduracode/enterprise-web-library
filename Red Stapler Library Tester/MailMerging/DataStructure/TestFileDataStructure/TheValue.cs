using RedStapler.StandardLibrary.MailMerging.FieldImplementation;

namespace RedStapler.StandardLibraryTester.MailMerging.DataStructure.TestFileDataStructure {
	public class TheValue: BasicMergeFieldImplementation<MergeTestData.Thing, string> {
		public string GetDescription() {
			return "This is a thing.";
		}

		public string Evaluate( MergeTestData.Thing row ) {
			return row.TheValue;
		}
	}
}