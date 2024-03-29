﻿#nullable disable
namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A web resource outside of the system.
/// </summary>
public sealed class ExternalResource: ResourceInfo {
	private readonly string url;

	/// <summary>
	/// Creates an external resource. Do not pass null or the empty string for url.
	/// </summary>
	public ExternalResource( string url ) {
		this.url = url;
	}

	public override bool UserCanAccess => true;
	public override AlternativeResourceMode AlternativeMode => null;
	internal override string GetUrl( bool ensureUserCanAccessResource, bool ensureResourceNotDisabled ) => url;
}