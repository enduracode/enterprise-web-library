using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RedStapler.StandardLibrary.MailMerging.Fields;
using RedStapler.StandardLibrary.MailMerging.RowTree;

namespace RedStapler.StandardLibrary.MailMerging.DataTree {
	/// <summary>
	/// Contains methods that perform data tree operations.
	/// </summary>
	public static class MergeDataTreeOps {
		/// <summary>
		/// Creates a merge row tree from the specified fields, data rows, and children, which together represent a merge data tree.
		/// </summary>
		public static MergeRowTree CreateRowTree<RowType>( ReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType> dataRows,
		                                                   ReadOnlyCollection<MergeDataTreeChild<RowType>> children = null, MergeDataTreeRemapping remapping = null ) {
			return CreateMergeRowTree( "Rows", fields, dataRows, children, remapping ?? new MergeDataTreeRemapping() );
		}

		internal static MergeRowTree CreateMergeRowTree<RowType>( string nodeName, ReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType> dataRows,
		                                                          ReadOnlyCollection<MergeDataTreeChild<RowType>> children, MergeDataTreeRemapping remapping ) {
			return new MergeRowTree( remapping.NodeNameOverride.Any() ? remapping.NodeNameOverride : nodeName,
			                         dataRows.Select(
				                         row =>
				                         new MergeRow( fields.Select( i => i.CreateValue( remapping.GetFieldName( i.Name ), i.MsWordName, i.GetDescription, () => row ) ),
				                                       children != null ? children.Select( i => i.CreateRowTreeForParentRow( row, remapping ) ) : new MergeRowTree[ 0 ] ) ),
			                         remapping.XmlRowElementName.Any() ? remapping.XmlRowElementName : "Row" );
		}
	}
}