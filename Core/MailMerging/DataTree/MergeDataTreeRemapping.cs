namespace EnterpriseWebLibrary.MailMerging.DataTree;

/// <summary>
/// A remapping configuration for a merge data tree.
/// </summary>
public class MergeDataTreeRemapping {
	private readonly string nodeNameOverride;
	private readonly Dictionary<string, IReadOnlyCollection<string>> oldToNewFieldNames;
	private readonly Dictionary<string, IReadOnlyCollection<MergeDataTreeRemapping>> childRemappingsByChildName;
	private readonly string xmlRowElementName;

	/// <summary>
	/// Creates a merge data tree remapping.
	/// </summary>
	public MergeDataTreeRemapping(
		string nodeNameOverride = "", Dictionary<string, IReadOnlyCollection<string>>? oldToNewFieldNames = null,
		Dictionary<string, IReadOnlyCollection<MergeDataTreeRemapping>>? childRemappingsByChildName = null, string xmlRowElementName = "" ) {
		this.nodeNameOverride = nodeNameOverride;
		this.oldToNewFieldNames = oldToNewFieldNames ?? new Dictionary<string, IReadOnlyCollection<string>>();
		this.childRemappingsByChildName = childRemappingsByChildName ?? new Dictionary<string, IReadOnlyCollection<MergeDataTreeRemapping>>();
		this.xmlRowElementName = xmlRowElementName;
	}

	internal string NodeNameOverride => nodeNameOverride;

	internal IReadOnlyCollection<string> GetFieldNames( string fieldName ) {
		return oldToNewFieldNames.ContainsKey( fieldName ) ? oldToNewFieldNames[ fieldName ] : fieldName.ToCollection();
	}

	internal Dictionary<string, IReadOnlyCollection<MergeDataTreeRemapping>> ChildRemappingsByChildName => childRemappingsByChildName;
	internal string XmlRowElementName => xmlRowElementName;
}