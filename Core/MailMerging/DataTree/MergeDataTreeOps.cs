using System.Collections.ObjectModel;
using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.DataTree;

/// <summary>
/// Contains methods that perform data tree operations.
/// </summary>
public static class MergeDataTreeOps {
	/// <summary>
	/// Creates a merge row tree from the specified fields, data rows, and children, which together represent a merge data tree.
	/// </summary>
	public static MergeRowTree CreateRowTree<RowType>(
		ReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType> dataRows, ReadOnlyCollection<MergeDataTreeChild<RowType>>? children = null,
		MergeDataTreeRemapping? remapping = null ) {
		return CreateMergeRowTree( "Rows", fields, dataRows, children, remapping ?? new MergeDataTreeRemapping() );
	}

	internal static MergeRowTree CreateMergeRowTree<RowType>(
		string nodeName, ReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType> dataRows, ReadOnlyCollection<MergeDataTreeChild<RowType>>? children,
		MergeDataTreeRemapping remapping ) {
		return new MergeRowTree(
			remapping.NodeNameOverride.Any() ? remapping.NodeNameOverride : nodeName,
			dataRows.Select(
				row => new MergeRow(
					from field in fields
					from name in remapping.GetFieldNames( field.Name )
					select field.CreateValue( name, field.MsWordName, field.GetDescription, () => row ),
					children?.SelectMany( child => child.CreateRowTreesForParentRow( row, remapping ) ) ?? new MergeRowTree[ 0 ] ) ),
			remapping.XmlRowElementName.Any() ? remapping.XmlRowElementName : "Row" );
	}
}