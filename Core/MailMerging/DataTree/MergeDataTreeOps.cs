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
		IReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType?> dataRows, IReadOnlyCollection<MergeDataTreeChild<RowType>>? children = null,
		MergeDataTreeRemapping? remapping = null ) {
		AssertFieldNamesUnique( fields );
		return CreateMergeRowTree( "Rows", fields, dataRows, children, remapping ?? new MergeDataTreeRemapping() );
	}

	internal static void AssertFieldNamesUnique<RowType>( IReadOnlyCollection<MergeField<RowType>> fields ) {
		var duplicateNames = fields.Select( i => i.Name ).GetDuplicates().Materialize();
		if( duplicateNames.Any() )
			throw new Exception( $"Duplicate merge-field names exist: {StringTools.ConcatenateWithDelimiter( ", ", duplicateNames )}." );

		var duplicateMsWordNames = fields.Select( i => i.MsWordName ).GetDuplicates().Materialize();
		if( duplicateMsWordNames.Any() )
			throw new Exception( $"Duplicate merge-field names exist for Microsoft Word: {StringTools.ConcatenateWithDelimiter( ", ", duplicateMsWordNames )}." );
	}

	internal static MergeRowTree CreateMergeRowTree<RowType>(
		string nodeName, IReadOnlyCollection<MergeField<RowType>> fields, IEnumerable<RowType?> dataRows,
		IReadOnlyCollection<MergeDataTreeChild<RowType>>? children, MergeDataTreeRemapping remapping ) =>
		new(
			remapping.NodeNameOverride.Any() ? remapping.NodeNameOverride : nodeName,
			dataRows.Select(
				row => new MergeRow(
					from field in fields
					from name in remapping.GetFieldNames( field.Name )
					select field.CreateValue( name, field.MsWordName, field.GetDescription, () => row ),
					children?.SelectMany( child => child.CreateRowTreesForParentRow( row, remapping ) ) ?? [ ] ) ),
			remapping.XmlRowElementName.Any() ? remapping.XmlRowElementName : "Row" );
}