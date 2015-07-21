using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.DataTree {
	/// <summary>
	/// A child node in a merge data tree, which is a tree of merge fields and data rows.
	/// </summary>
	public interface MergeDataTreeChild<in ParentRowType> {
		/// <summary>
		/// Creates a merge row tree for the specified parent row. Data Tree subsystem use only.
		/// </summary>
		MergeRowTree CreateRowTreeForParentRow( ParentRowType parentRow, MergeDataTreeRemapping remapping );
	}

	/// <summary>
	/// A child node in a merge data tree.
	/// </summary>
	public class MergeDataTreeChild<ParentRowType, RowType>: MergeDataTreeChild<ParentRowType> {
		private readonly string name;
		private readonly ReadOnlyCollection<MergeField<RowType>> fields;
		private readonly Func<ParentRowType, IEnumerable<RowType>> dataRowSelector;
		private readonly ReadOnlyCollection<MergeDataTreeChild<RowType>> children;

		/// <summary>
		/// Create a merge data tree child node. The data row selector is a function that returns the rows that are children of the specified parent row.
		/// </summary>
		public MergeDataTreeChild( string name, ReadOnlyCollection<MergeField<RowType>> fields, Func<ParentRowType, IEnumerable<RowType>> dataRowSelector,
		                           ReadOnlyCollection<MergeDataTreeChild<RowType>> children = null ) {
			this.name = name;
			this.fields = fields;
			this.dataRowSelector = dataRowSelector;
			this.children = children;
		}

		MergeRowTree MergeDataTreeChild<ParentRowType>.CreateRowTreeForParentRow( ParentRowType parentRow, MergeDataTreeRemapping remapping ) {
			remapping = remapping != null && remapping.ChildRemappingsByChildName.ContainsKey( name )
			            	? remapping.ChildRemappingsByChildName[ name ]
			            	: new MergeDataTreeRemapping();
			return MergeDataTreeOps.CreateMergeRowTree( name, fields, dataRowSelector( parentRow ), children, remapping );
		}
	}
}