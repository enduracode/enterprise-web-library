#nullable disable
using System;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class PostBackAction {
		internal ( ResourceInfo, Func<ResourceInfo, bool> )? NavigationBehavior { get; }
		internal PageReloadBehavior ReloadBehavior { get; }

		/// <summary>
		/// Creates an action that will navigate to the specified resource.
		/// </summary>
		/// <param name="resource">Pass null for no navigation.</param>
		/// <param name="authorizationCheckDisabledPredicate">A function that takes the effective destination resource and returns whether navigation is allowed if
		/// the authenticated user cannot access it. Use with caution.</param>
		public PostBackAction( ResourceInfo resource, Func<ResourceInfo, bool> authorizationCheckDisabledPredicate = null ) {
			NavigationBehavior = ( resource, authorizationCheckDisabledPredicate );
		}

		/// <summary>
		/// Creates an action that will reload the page.
		/// </summary>
		/// <param name="reloadBehavior">The reload behavior.</param>
		public PostBackAction( PageReloadBehavior reloadBehavior ) {
			ReloadBehavior = reloadBehavior;
		}
	}
}