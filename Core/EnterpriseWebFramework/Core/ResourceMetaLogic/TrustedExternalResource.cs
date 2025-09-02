namespace EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;

/// <summary>
/// A trusted web resource outside of the system.
/// </summary>
public sealed class TrustedExternalResource: TrustedResourceInfo {
	private readonly ExternalResource resource;

	/// <summary>
	/// Creates a trusted external resource. Use with caution.
	/// </summary>
	public TrustedExternalResource( ExternalResource resource ) {
		this.resource = resource;
	}

	public override bool UserCanAccess => resource.UserCanAccess;
	public override AlternativeResourceMode? AlternativeMode => resource.AlternativeMode;

	internal override EwfUrl GetEwfUrl( bool ensureUserCanAccessResource, bool ensureResourceNotDisabled ) =>
		resource.GetEwfUrl( ensureUserCanAccessResource, ensureResourceNotDisabled );

	protected internal override IEnumerable<NestedUrl?> GetNestedUrls() => resource.GetNestedUrls();
}