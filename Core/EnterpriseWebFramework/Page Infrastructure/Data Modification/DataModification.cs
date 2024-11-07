namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A list of validations and a modification method that execute on a post-back.
/// </summary>
public interface DataModification;

public class DataModificationsParameter {
	private readonly IEnumerable<DataModification> sequence;
	internal readonly Lazy<IReadOnlyCollection<DataModification>> Collection;

	internal DataModificationsParameter( IEnumerable<DataModification> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<DataModification>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s data-modification actions plus the specified actions.
	/// </summary>
	public DataModificationsParameter Add( DataModificationsParameter dataModificationActions ) => new( sequence.Concat( dataModificationActions.sequence ) );
}

public static class DataModificationsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this data-modification action plus the specified actions.
	/// </summary>
	public static DataModificationsParameter Add( this DataModification dataModificationAction, DataModificationsParameter dataModificationActions ) =>
		new DataModificationsParameter( [ dataModificationAction ] ).Add( dataModificationActions );

	/// <summary>
	/// Returns a parameter with the data-modification actions in this sequence.
	/// </summary>
	public static DataModificationsParameter ToParameter( this IEnumerable<DataModification> dataModificationActions ) => new( dataModificationActions );
}