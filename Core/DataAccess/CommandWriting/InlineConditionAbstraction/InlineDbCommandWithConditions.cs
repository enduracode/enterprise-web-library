namespace EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

/// <summary>
/// EWL use only.
/// </summary>
public interface InlineDbCommandWithConditions {
	/// <summary>
	/// EWL use only.
	/// </summary>
	void AddConditions( IEnumerable<InlineDbCommandCondition> conditions );
}