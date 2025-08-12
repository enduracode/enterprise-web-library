using System.ComponentModel;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public abstract class UrlEncoder {
	/// <summary>
	/// Gets the ID of the web application that is generating the URL.
	/// </summary>
	public string AppId { get; }

	protected UrlEncoder( string appId ) {
		AppId = appId;
	}

	/// <summary>
	/// Generated code and internal use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public abstract IReadOnlyCollection<( string name, string value, bool isSegmentParameter )> GetRemainingParameters();

	/// <summary>
	/// Generated code and internal use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public abstract void ResetState();
}