namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A list of validations and a modification method that execute on a post-back.
/// </summary>
public interface DataModification;

public class DataModificationsParameter {
	private readonly IEnumerable<DataModification> sequence;

	internal DataModificationsParameter( IEnumerable<DataModification> sequence ) {
		this.sequence = sequence;
	}

	/// <summary>
	/// Returns a new parameter with the specified data-modification actions added to this parameter.
	/// </summary>
	public DataModificationsParameter Add( DataModificationsParameter dataModificationActions ) => new( sequence.Concat( dataModificationActions.sequence ) );

	internal IReadOnlyCollection<DataModification> GetCollection() => sequence.Materialize();
}

public static class DataModificationsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with the specified data-modification actions added to this action.
	/// </summary>
	public static DataModificationsParameter Add( this DataModification dataModificationAction, DataModificationsParameter dataModificationActions ) =>
		new DataModificationsParameter( [ dataModificationAction ] ).Add( dataModificationActions );
}