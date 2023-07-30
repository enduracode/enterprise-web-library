namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A parent of a resource or entity setup.
/// </summary>
public interface ResourceParent: UrlHandler {
	/// <summary>
	/// Gets the parent of this parent, or null if there isn’t one.
	/// </summary>
	ResourceParent? Parent { get; }

	/// <summary>
	/// Gets the name of this parent.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets whether the authenticated user is authorized to access this parent.
	/// </summary>
	bool UserCanAccess { get; }

	/// <summary>
	/// Gets the log-in page to use for this parent, or null for default behavior.
	/// </summary>
	ResourceBase LogInPage { get; }

	/// <summary>
	/// Gets the alternative mode for this parent, or null if it is in normal mode.
	/// </summary>
	AlternativeResourceMode AlternativeMode { get; }

	/// <summary>
	/// Gets the desired security setting for requests to this parent.
	/// </summary>
	ConnectionSecurity ConnectionSecurity { get; }

	bool AllowsSearchEngineIndexing { get; }
}