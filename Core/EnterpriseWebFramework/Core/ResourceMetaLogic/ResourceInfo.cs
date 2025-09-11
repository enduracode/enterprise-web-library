namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

/// <summary>
/// A base set of functionality that can be used to discover information about a resource before actually requesting it.
/// </summary>
public abstract class ResourceInfo: WebItem {
	/// <summary>
	/// Gets whether the authenticated user is authorized to access this resource.
	/// </summary>
	public abstract bool UserCanAccess { get; }

	/// <summary>
	/// Gets the alternative mode for this resource, or null if it is in normal mode. Do not call this from the createAlternativeMode method of an ancestor; doing
	/// so will result in a stack overflow.
	/// </summary>
	public abstract AlternativeResourceMode? AlternativeMode { get; }

	/// <summary>
	/// Returns an absolute URL that can be used to request the resource.
	/// </summary>
	/// <param name="disableAuthorizationCheck">Pass true to allow a URL to be returned that the authenticated user cannot access. Use with caution. Might be
	/// useful if you are adding the URL to an email message or otherwise displaying it outside the application.</param>
	public string GetUrl( bool disableAuthorizationCheck = false ) => GetEwfUrl( !disableAuthorizationCheck, true ).Url;

	EwfUrl WebItem.GetEwfUrl( bool ensureUserCanAccessItem, bool ensureItemNotDisabled ) => GetEwfUrl( ensureUserCanAccessItem, ensureItemNotDisabled );

	internal abstract EwfUrl GetEwfUrl( bool ensureUserCanAccessResource, bool ensureResourceNotDisabled );

	IEnumerable<NestedUrl?> WebItem.GetNestedUrls() => GetNestedUrls();

	protected internal abstract IEnumerable<NestedUrl?> GetNestedUrls();

	WebItem WebItem.ReCreate() => ReCreate();

	internal virtual ResourceInfo ReCreate() => this;
}