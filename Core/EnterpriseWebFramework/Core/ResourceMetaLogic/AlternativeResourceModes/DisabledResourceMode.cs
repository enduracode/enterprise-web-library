namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;

/// <summary>
/// A mode that prevents a resource or entity setup from being accessed.
/// </summary>
public class DisabledResourceMode: AlternativeResourceMode {
	/// <summary>
	/// Gets the message.
	/// </summary>
	public string Message { get; }

	/// <summary>
	/// Creates a disabled page mode object.
	/// </summary>
	public DisabledResourceMode( string message ) {
		Message = message;
	}
}