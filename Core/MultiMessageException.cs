namespace EnterpriseWebLibrary;

/// <summary>
/// An exception with multiple messages.
/// </summary>
public class MultiMessageException: Exception {
	/// <summary>
	/// Gets the messages that describe the exception.
	/// </summary>
	public IReadOnlyCollection<string> Messages { get; }

	/// <summary>
	/// Creates an exception with the specified messages.
	/// </summary>
	public MultiMessageException( IEnumerable<string> messages ): base( StringTools.ConcatenateWithDelimiter( Environment.NewLine, messages ) ) {
		Messages = messages.Materialize();
	}
}