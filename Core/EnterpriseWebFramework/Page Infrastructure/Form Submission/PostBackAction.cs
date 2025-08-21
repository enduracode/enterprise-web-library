using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public sealed class PostBackAction {
	internal ( ResourceInfo?, Func<ResourceInfo, bool>? )? NavigationBehavior { get; }
	internal PageReloadBehavior? ReloadBehavior { get; }

	/// <summary>
	/// Creates an action that will navigate to the specified resource.
	/// </summary>
	/// <param name="resource">Pass null for no navigation.</param>
	/// <param name="authorizationCheckDisabledPredicate">A function that takes the effective destination resource and returns whether navigation is allowed if
	/// the authenticated user cannot access it. Use with caution.</param>
	public PostBackAction( TrustedResourceInfo? resource, Func<ResourceInfo, bool>? authorizationCheckDisabledPredicate = null ) {
		NavigationBehavior = ( resource, authorizationCheckDisabledPredicate );
	}

	/// <summary>
	/// Creates an action that will reload the page.
	/// </summary>
	/// <param name="reloadBehavior">The reload behavior.</param>
	public PostBackAction( PageReloadBehavior reloadBehavior ) {
		ReloadBehavior = reloadBehavior;
	}

	internal PostBackAction( ExternalResource? resource ) {
		NavigationBehavior = ( resource, null );
	}
}

[ PublicAPI ]
public static class PostBackActionExtensionCreators {
	/// <summary>
	/// Creates an action that will navigate to this external resource. Call on null for no navigation.
	/// </summary>
	public static PostBackAction ToPostBackAction( this ExternalResource? resource ) => new( resource );
}