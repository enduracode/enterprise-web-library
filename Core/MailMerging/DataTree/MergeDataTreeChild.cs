using EnterpriseWebLibrary.MailMerging.Fields;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.MailMerging.DataTree;

/// <summary>
/// A child node in a merge data tree, which is a tree of merge fields and data rows.
/// </summary>
public interface MergeDataTreeChild<in ParentRowType> {
	/// <summary>
	/// Creates merge row trees for the specified parent row. Data Tree subsystem use only.
	/// </summary>
	IEnumerable<MergeRowTree> CreateRowTreesForParentRow( ParentRowType? parentRow, MergeDataTreeRemapping parentRemapping );
}

/// <summary>
/// A child node in a merge data tree.
/// </summary>
public class MergeDataTreeChild<ParentRowType, RowType>: MergeDataTreeChild<ParentRowType> {
	private readonly string name;
	private readonly IReadOnlyCollection<MergeField<RowType>> fields;
	private readonly Func<ParentRowType?, IEnumerable<RowType?>> dataRowSelector;
	private readonly IReadOnlyCollection<MergeDataTreeChild<RowType>>? children;

	/// <summary>
	/// Create a merge data tree child node. The data row selector is a function that returns the rows that are children of the specified parent row.
	/// </summary>
	public MergeDataTreeChild(
		string name, IReadOnlyCollection<MergeField<RowType>> fields, Func<ParentRowType?, IEnumerable<RowType?>> dataRowSelector,
		IReadOnlyCollection<MergeDataTreeChild<RowType>>? children = null ) {
		MergeDataTreeOps.AssertFieldNamesUnique( fields );

		this.name = name;
		this.fields = fields;
		this.dataRowSelector = dataRowSelector;
		this.children = children;
	}

	IEnumerable<MergeRowTree> MergeDataTreeChild<ParentRowType>.CreateRowTreesForParentRow( ParentRowType? parentRow, MergeDataTreeRemapping parentRemapping ) {
		if( !parentRemapping.ChildRemappingsByChildName.TryGetValue( name, out var remappings ) )
			remappings = new MergeDataTreeRemapping().ToCollection();
		return remappings.Select( remapping => MergeDataTreeOps.CreateMergeRowTree( name, fields, dataRowSelector( parentRow ), children, remapping ) );
	}
}