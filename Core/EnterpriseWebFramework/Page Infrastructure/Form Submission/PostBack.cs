using System;
using System.Collections.Generic;
using System.Linq;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public class PostBack {
		private static Func<IReadOnlyCollection<DataModification>> dataModificationGetter;

		internal static void Init( Func<IReadOnlyCollection<DataModification>> dataModificationGetter ) {
			PostBack.dataModificationGetter = dataModificationGetter;
		}

		public static string GetCompositeId( params string[] parts ) {
			return StringTools.ConcatenateWithDelimiter( ".", parts );
		}

		/// <summary>
		/// Creates a full post-back, which updates the page's data before executing itself.
		/// </summary>
		/// <param name="id">The ID of this post-back. Must be unique for every post-back in the page. Do not pass null or the empty string.</param>
		/// <param name="forcePageDataUpdate">Pass true to force the page's data update to execute even if no form values changed.</param>
		/// <param name="skipModificationIfNoChanges">Pass true to skip the validations and modification methods if no form values changed, or if no form controls
		/// are included in this post-back.</param>
		/// <param name="firstModificationMethod"></param>
		/// <param name="actionGetter">A method that returns the action EWF will perform if there were no modification errors.</param>
		public static ActionPostBack CreateFull(
			string id = "main", bool forcePageDataUpdate = false, bool skipModificationIfNoChanges = false, Action firstModificationMethod = null,
			Func<PostBackAction> actionGetter = null ) {
			if( !id.Any() )
				throw new ApplicationException( "The post-back must have an ID." );
			return new ActionPostBack( true, null, id, forcePageDataUpdate, skipModificationIfNoChanges, firstModificationMethod, actionGetter, null );
		}

		/// <summary>
		/// Creates an intermediate post-back, which skips the page's data update and only executes itself plus the validations from another data modification.
		/// Supports async post-backs, but does not allow navigation to another page.
		/// </summary>
		/// <param name="updateRegions">The regions of the page that will change as a result of this post-back. If forceFullPagePostBack is true, the regions only
		/// need to include the form controls that will change; other changes are allowed anywhere on the page.</param>
		/// <param name="forceFullPagePostBack">Pass true to force a full-page post-back instead of attempting an async post-back. Note that an async post-back will
		/// automatically fall back to a full-page post-back if the page has changed in any way since it was last sent. This parameter currently has no effect since
		/// async post-backs are not yet implemented; see RSIS goal 478.</param>
		/// <param name="id">The ID of this post-back. Must be unique for every post-back in the page. Do not pass null or the empty string.</param>
		/// <param name="skipModificationIfNoChanges">Pass true to skip the validations and modification methods if no form values changed, or if no form controls
		/// are included in this post-back.</param>
		/// <param name="firstModificationMethod"></param>
		/// <param name="reloadBehaviorGetter">A method that returns the reload behavior, if there were no modification errors. If you do pass a method, the page
		/// will block interaction even for async post-backs. This prevents an abrupt focus change for the user when the page reloads.</param>
		/// <param name="validationDm">The data modification that will have its validations executed if there were no errors in this post-back. Pass null to use the
		/// first of the current data modifications.</param>
		public static ActionPostBack CreateIntermediate(
			IEnumerable<UpdateRegionSet> updateRegions, bool forceFullPagePostBack = false, string id = "main", bool skipModificationIfNoChanges = false,
			Action firstModificationMethod = null, Func<PageReloadBehavior> reloadBehaviorGetter = null, DataModification validationDm = null ) {
			if( !id.Any() )
				throw new ApplicationException( "The post-back must have an ID." );
			return new ActionPostBack(
				forceFullPagePostBack,
				updateRegions,
				id,
				null,
				skipModificationIfNoChanges,
				firstModificationMethod,
				reloadBehaviorGetter != null ? new Func<PostBackAction>( () => new PostBackAction( reloadBehaviorGetter() ) ) : null,
				validationDm ?? dataModificationGetter().First() );
		}

		internal static PostBack CreateDataUpdate() {
			return new PostBack( true, "", false );
		}

		private readonly bool forceFullPagePostBack;
		private readonly string id;
		private readonly bool? forcePageDataUpdate;

		protected PostBack( bool forceFullPagePostBack, string id, bool? forcePageDataUpdate ) {
			this.forceFullPagePostBack = forceFullPagePostBack;
			this.id = id;
			this.forcePageDataUpdate = forcePageDataUpdate;
		}

		internal bool ForceFullPagePostBack => forceFullPagePostBack;
		internal string Id => id;
		internal bool IsIntermediate => !forcePageDataUpdate.HasValue;
		internal bool ForcePageDataUpdate => forcePageDataUpdate.Value;
	}

	public class ActionPostBack: PostBack, DataModification, ValidationList {
		private readonly IEnumerable<UpdateRegionSet> updateRegions;
		private readonly bool skipModificationIfNoChanges;
		private readonly BasicDataModification dataModification;
		private readonly Func<PostBackAction> actionGetter;
		private readonly DataModification validationDm;

		internal ActionPostBack(
			bool forceFullPagePostBack, IEnumerable<UpdateRegionSet> updateRegions, string id, bool? forcePageDataUpdate, bool skipModificationIfNoChanges,
			Action firstModificationMethod, Func<PostBackAction> actionGetter, DataModification validationDm ): base( forceFullPagePostBack, id, forcePageDataUpdate ) {
			this.updateRegions = updateRegions ?? new UpdateRegionSet[ 0 ];
			this.skipModificationIfNoChanges = skipModificationIfNoChanges;

			dataModification = new BasicDataModification();
			if( firstModificationMethod != null )
				dataModification.AddModificationMethod( firstModificationMethod );

			this.actionGetter = actionGetter;
			this.validationDm = validationDm;
		}

		internal IEnumerable<UpdateRegionSet> UpdateRegions => updateRegions;

		void ValidationList.AddValidation( EwfValidation validation ) {
			( (ValidationList)dataModification ).AddValidation( validation );
		}

		/// <summary>
		/// Adds a modification method. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethod( Action modificationMethod ) {
			dataModification.AddModificationMethod( modificationMethod );
		}

		/// <summary>
		/// Adds a list of modification methods. These only execute if all validation methods succeed.
		/// </summary>
		public void AddModificationMethods( IEnumerable<Action> modificationMethods ) {
			dataModification.AddModificationMethods( modificationMethods );
		}

		internal bool Execute( bool formValuesChanged, Action<EwfValidation, IEnumerable<string>> validationErrorHandler, Action<PostBackAction> actionSetter ) {
			PostBackAction action = null;
			return dataModification.Execute(
				skipModificationIfNoChanges,
				formValuesChanged,
				validationErrorHandler,
				performValidationOnly: actionSetter == null,
				actionMethodAndPostModificationMethod: actionGetter != null
					                                       ? new Tuple<Action, Action>( () => action = actionGetter(), () => actionSetter( action ) )
					                                       : null );
		}

		internal DataModification ValidationDm => validationDm;
	}
}