namespace EnterpriseWebLibrary.DataAccess.CommandWriting;

/// <summary>
/// Not yet documented.
/// </summary>
public interface InlineDbModificationCommand {
	/// <summary>
	/// Not yet documented.
	/// </summary>
	void AddColumnModifications( IEnumerable<InlineDbCommandColumnValue> columnModifications );
}