using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// The configuration for a button that performs an action on selected items in a list or table.
	/// </summary>
	public static class SelectedItemAction {
		/// <summary>
		/// Creates an action with full-post-back behavior.
		/// </summary>
		/// <param name="text">Do not pass null or the empty string.</param>
		/// <param name="modificationMethod">A modification method that takes a collection of selected-item IDs.</param>
		/// <param name="displaySetup"></param>
		/// <param name="icon">The icon.</param>
		/// <param name="confirmationDialogContent">Pass a value to open a confirmation dialog box with a button that triggers the action.</param>
		/// <param name="actionGetter">A method that returns the action EWF will perform if there were no modification errors.</param>
		public static SelectedItemAction<IdType> CreateWithFullPostBackBehavior<IdType>(
			string text, Action<IReadOnlyCollection<IdType>> modificationMethod, DisplaySetup displaySetup = null, ActionComponentIcon icon = null,
			IReadOnlyCollection<FlowComponent> confirmationDialogContent = null, Func<PostBackAction> actionGetter = null ) =>
			new SelectedItemAction<IdType>(
				( postBackIdBase, selectedItemIdGetter ) => {
					var postBack = PostBack.CreateFull(
						id: PostBack.GetCompositeId( postBackIdBase, text ),
						firstModificationMethod: () => modificationMethod( selectedItemIdGetter() ),
						actionGetter: actionGetter );
					return ( postBack,
						       new ButtonSetup(
							       text,
							       displaySetup: displaySetup,
							       behavior: confirmationDialogContent != null
								                 ? (ButtonBehavior)new ConfirmationButtonBehavior( confirmationDialogContent, postBack: postBack )
								                 : new PostBackBehavior( postBack: postBack ),
							       icon: icon ) );
				} );

		/// <summary>
		/// Creates an action with intermediate-post-back behavior.
		/// </summary>
		/// <param name="text">Do not pass null or the empty string.</param>
		/// <param name="updateRegions">The regions of the page that will change as a result of this post-back. If forceFullPagePostBack is true, the regions only
		/// need to include the form controls that will change; other changes are allowed anywhere on the page.</param>
		/// <param name="modificationMethod">A modification method that takes a collection of selected-item IDs.</param>
		/// <param name="displaySetup"></param>
		/// <param name="icon">The icon.</param>
		/// <param name="confirmationDialogContent">Pass a value to open a confirmation dialog box with a button that triggers the action.</param>
		/// <param name="forceFullPagePostBack">Pass true to force a full-page post-back instead of attempting an async post-back. Note that an async post-back will
		/// automatically fall back to a full-page post-back if the page has changed in any way since it was last sent.</param>
		/// <param name="reloadBehaviorGetter">A method that returns the reload behavior, if there were no modification errors. If you do pass a method, the page
		/// will block interaction even for async post-backs. This prevents an abrupt focus change for the user when the page reloads.</param>
		/// <param name="validationDm">The data modification that will have its validations executed if there were no errors in this post-back. Pass null to use the
		/// first of the current data modifications.</param>
		public static SelectedItemAction<IdType> CreateWithIntermediatePostBackBehavior<IdType>(
			string text, IEnumerable<UpdateRegionSet> updateRegions, Action<IReadOnlyCollection<IdType>> modificationMethod, DisplaySetup displaySetup = null,
			ActionComponentIcon icon = null, IReadOnlyCollection<FlowComponent> confirmationDialogContent = null, bool forceFullPagePostBack = false,
			Func<PageReloadBehavior> reloadBehaviorGetter = null, DataModification validationDm = null ) {
			validationDm = validationDm ?? FormState.Current.DataModifications.First();
			return new SelectedItemAction<IdType>(
				( postBackIdBase, selectedItemIdGetter ) => {
					var postBack = PostBack.CreateIntermediate(
						updateRegions,
						forceFullPagePostBack: forceFullPagePostBack,
						id: PostBack.GetCompositeId( postBackIdBase, text ),
						firstModificationMethod: () => modificationMethod( selectedItemIdGetter() ),
						reloadBehaviorGetter: reloadBehaviorGetter,
						validationDm: validationDm );
					return ( postBack,
						       new ButtonSetup(
							       text,
							       displaySetup: displaySetup,
							       behavior: confirmationDialogContent != null
								                 ? (ButtonBehavior)new ConfirmationButtonBehavior( confirmationDialogContent, postBack: postBack )
								                 : new PostBackBehavior( postBack: postBack ),
							       icon: icon ) );
				} );
		}
	}

	/// <summary>
	/// An action performed on selected items in a list or table.
	/// </summary>
	public sealed class SelectedItemAction<IdType> {
		private readonly Func<string, Func<IReadOnlyCollection<IdType>>, ( DataModification, ButtonSetup )> postBackAndButtonGetter;

		internal SelectedItemAction( Func<string, Func<IReadOnlyCollection<IdType>>, ( DataModification, ButtonSetup )> postBackAndButtonGetter ) {
			this.postBackAndButtonGetter = postBackAndButtonGetter;
		}

		internal ( DataModification postBack, ButtonSetup button ) GetPostBackAndButton(
			string postBackIdBase, Func<IReadOnlyCollection<IdType>> selectedItemIdGetter ) =>
			postBackAndButtonGetter( postBackIdBase, selectedItemIdGetter );
	}

	public static class SelectedItemActionExtensionCreators {
		/// <summary>
		/// Concatenates selected-item actions.
		/// </summary>
		public static IEnumerable<SelectedItemAction<IdType>> Concat<IdType>(
			this SelectedItemAction<IdType> first, IEnumerable<SelectedItemAction<IdType>> second ) =>
			second.Prepend( first );

		/// <summary>
		/// Returns a sequence of two selected-item actions.
		/// </summary>
		public static IEnumerable<SelectedItemAction<IdType>> Append<IdType>( this SelectedItemAction<IdType> first, SelectedItemAction<IdType> second ) =>
			Enumerable.Empty<SelectedItemAction<IdType>>().Append( first ).Append( second );
	}
}