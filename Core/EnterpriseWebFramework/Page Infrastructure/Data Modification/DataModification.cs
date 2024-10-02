namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A list of validations and a modification method that execute on a post-back.
/// </summary>
public interface DataModification;

public static class DataModificationExtensionCreators {
	/// <summary>
	/// Concatenates data modifications.
	/// </summary>
	public static IEnumerable<DataModification> Concat( this DataModification first, IEnumerable<DataModification> second ) => second.Prepend( first );

	/// <summary>
	/// Returns a sequence of two data modifications.
	/// </summary>
	public static IEnumerable<DataModification> Append( this DataModification first, DataModification second ) =>
		Enumerable.Empty<DataModification>().Append( first ).Append( second );
}