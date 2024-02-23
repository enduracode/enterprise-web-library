using System.Web;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// An exception that is thrown during data modification.
/// </summary>
public class DataModificationException: Exception {
	internal Action? ModificationMethod { get; }
	internal IReadOnlyCollection<TrustedHtmlString> HtmlMessages { get; }

	/// <summary>
	/// Creates a new exception with the specified message.
	/// </summary>
	/// <param name="message">The message that describes the exception.</param>
	/// <param name="modificationMethod">The modification method, which can perform logging but must not change the page in any way. Use with caution.</param>
	public DataModificationException( string message, Action? modificationMethod = null ): this( [ message ], modificationMethod: modificationMethod ) {}

	/// <summary>
	/// Creates a new exception with the specified messages.
	/// </summary>
	/// <param name="messages">The messages that describe the exception.</param>
	/// <param name="modificationMethod">The modification method, which can perform logging but must not change the page in any way. Use with caution.</param>
	public DataModificationException( IEnumerable<string> messages, Action? modificationMethod = null ): this(
		messages.Select( i => new TrustedHtmlString( HttpUtility.HtmlEncode( i ) ) ).Materialize(),
		modificationMethod: modificationMethod ) {}

	internal DataModificationException( IReadOnlyCollection<TrustedHtmlString> messages, Action? modificationMethod = null ): base(
		StringTools.ConcatenateWithDelimiter( Environment.NewLine, messages.Select( i => i.Html ) ) ) {
		ModificationMethod = modificationMethod;
		HtmlMessages = messages;
	}
}