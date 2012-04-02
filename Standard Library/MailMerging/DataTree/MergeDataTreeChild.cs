using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RedStapler.StandardLibrary.MailMerging.Fields;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.DataTree {
	/// <summary>
	/// A child node in a merge data tree, which is a tree of merge fields and data rows.
	/// </summary>
	public interface MergeDataTreeChild<ParentRowType> {
		/// <summary>
		/// Creates a merge row child for the specified parent row. Data Tree subsystem use only.
		/// </summary>
		MergeRowChild CreateMergeRowChildForParentRow( ParentRowType parentRow );
	}

	/// <summary>
	/// A child node in a merge data tree.
	/// </summary>
	public class MergeDataTreeChild<ParentRowType, RowType>: MergeDataTreeChild<ParentRowType> {
		private readonly Func<ParentRowType, MergeRowChild> mergeRowChildCreator;

		/// <summary>
		/// Create a merge data tree child node. The data row selector is a function that returns the rows that are children of the specified parent row. Null may
		/// be specified for children if there are no child nodes.
		/// </summary>
		public MergeDataTreeChild( string name, ReadOnlyCollection<MergeField<RowType>> fields, Func<ParentRowType, IEnumerable<RowType>> dataRowSelector,
		                           ReadOnlyCollection<MergeDataTreeChild<RowType>> children ) {
			mergeRowChildCreator = parentRow => new MergeRowChild( name, MergeDataTreeOps.CreateMergeRows( fields, dataRowSelector( parentRow ), children ) );
		}

		MergeRowChild MergeDataTreeChild<ParentRowType>.CreateMergeRowChildForParentRow( ParentRowType parentRow ) {
			return mergeRowChildCreator( parentRow );
		}
	}
}