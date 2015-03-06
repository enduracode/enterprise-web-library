using System.Collections.Generic;

namespace EnterpriseWebLibrary.Tests.MailMerging.DataStructure.TestFileDataStructure {
	public class MergeTestData {
		public string FullName;
		public List<Thing> Things;

		public class Thing {
			public string TheValue;
		}
	}
}