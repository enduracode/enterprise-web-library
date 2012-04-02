using System.Collections.Generic;

namespace RedStapler.StandardLibrary.MailMerging.RowTree {
	/// <summary>
	/// A child of a merge row.
	/// </summary>
	public class MergeRowChild {
		/// <summary>
		/// Gets the name of this child.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the merge row's child rows of a specific data type.
		/// </summary>
		public IEnumerable<MergeRow> Rows { get; private set; }

		internal MergeRowChild( string name, IEnumerable<MergeRow> rows ) {
			Name = name;
			Rows = rows;
		}
	}
}