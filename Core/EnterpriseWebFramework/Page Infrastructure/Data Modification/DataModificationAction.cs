namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A list of validations and a modification method that execute on a post-back.
/// </summary>
public interface DataModificationAction;

public class DataModificationActionsParameter {
	private readonly IEnumerable<DataModificationAction> sequence;
	internal readonly Lazy<IReadOnlyCollection<DataModificationAction>> Collection;

	internal DataModificationActionsParameter( IEnumerable<DataModificationAction> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<DataModificationAction>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s data-modification actions plus the specified actions.
	/// </summary>
	public DataModificationActionsParameter Add( DataModificationActionsParameter dataModificationActions ) =>
		new( sequence.Concat( dataModificationActions.sequence ) );
}

public static class DataModificationsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this data-modification action plus the specified actions.
	/// </summary>
	public static DataModificationActionsParameter Add(
		this DataModificationAction dataModificationAction, DataModificationActionsParameter dataModificationActions ) =>
		new DataModificationActionsParameter( [ dataModificationAction ] ).Add( dataModificationActions );

	/// <summary>
	/// Returns a parameter with the data-modification actions in this sequence.
	/// </summary>
	public static DataModificationActionsParameter ToParameter( this IEnumerable<DataModificationAction> dataModificationActions ) =>
		new( dataModificationActions );
}