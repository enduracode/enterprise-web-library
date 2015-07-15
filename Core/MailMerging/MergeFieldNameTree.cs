using System;
using System.Collections.Generic;

namespace EnterpriseWebLibrary.MailMerging {
	/// <summary>
	/// A tree of merge field names.
	/// </summary>
	public class MergeFieldNameTree {
		private readonly IEnumerable<string> fieldNames;
		private readonly IEnumerable<Tuple<string, MergeFieldNameTree>> childNamesAndChildren;

		public MergeFieldNameTree( IEnumerable<string> fieldNames, IEnumerable<Tuple<string, MergeFieldNameTree>> childNamesAndChildren = null ) {
			this.fieldNames = fieldNames;
			this.childNamesAndChildren = childNamesAndChildren ?? new Tuple<string, MergeFieldNameTree>[ 0 ];
		}

		public IEnumerable<string> FieldNames { get { return fieldNames; } }
		public IEnumerable<Tuple<string, MergeFieldNameTree>> ChildNamesAndChildren { get { return childNamesAndChildren; } }
	}
}