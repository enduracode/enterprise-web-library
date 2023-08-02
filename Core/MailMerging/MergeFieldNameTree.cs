namespace EnterpriseWebLibrary.MailMerging;

/// <summary>
/// A tree of merge field names.
/// </summary>
public class MergeFieldNameTree {
	private readonly IEnumerable<string> fieldNames;
	private readonly IEnumerable<Tuple<string, MergeFieldNameTree>> childNamesAndChildren;

	public MergeFieldNameTree( IEnumerable<string> fieldNames, IEnumerable<Tuple<string, MergeFieldNameTree>>? childNamesAndChildren = null ) {
		this.fieldNames = fieldNames;
		this.childNamesAndChildren = childNamesAndChildren ?? Enumerable.Empty<Tuple<string, MergeFieldNameTree>>();
	}

	public IEnumerable<string> FieldNames => fieldNames;
	public IEnumerable<Tuple<string, MergeFieldNameTree>> ChildNamesAndChildren => childNamesAndChildren;
}