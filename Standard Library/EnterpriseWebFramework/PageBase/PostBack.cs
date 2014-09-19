using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public class PostBack {
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
		/// <param name="firstTopValidationMethod"></param>
		/// <param name="firstModificationMethod"></param>
		/// <param name="actionGetter">A method that returns the action EWF will perform if there were no modification errors.</param>
		public static ActionPostBack CreateFull(
			string id = "main", bool forcePageDataUpdate = false, bool skipModificationIfNoChanges = false,
			Action<PostBackValueDictionary, Validator> firstTopValidationMethod = null, Action firstModificationMethod = null, Func<PostBackAction> actionGetter = null ) {
			if( !id.Any() )
				throw new ApplicationException( "The post-back must have an ID." );
			return new ActionPostBack(
				true,
				null,
				id,
				forcePageDataUpdate,
				skipModificationIfNoChanges,
				firstTopValidationMethod,
				firstModificationMethod,
				actionGetter,
				null );
		}

		/// <summary>
		/// Creates an intermediate post-back, which skips the page's data update and only executes itself plus the validations from another data modification.
		/// Supports async post-backs, but does not allow navigation to another page.
		/// </summary>
		/// <param name="updateRegions">The regions of the page that will change as a result of this post-back. If forceFullPagePostBack is true, the regions only
		/// need to include the form controls that will change; other changes are allowed anywhere on the page.</param>
		/// <param name="validationDm">The data modification that will have its validations executed if there were no errors in this post-back.</param>
		/// <param name="forceFullPagePostBack">Pass true to force a full-page post-back instead of attempting an async post-back. Note that an async post-back will
		/// automatically fall back to a full-page post-back if the page has changed in any way since it was last sent. This parameter currently has no effect since
		/// async post-backs are not yet implemented; see RSIS goal 478.</param>
		/// <param name="id">The ID of this post-back. Must be unique for every post-back in the page. Do not pass null or the empty string.</param>
		/// <param name="skipModificationIfNoChanges">Pass true to skip the validations and modification methods if no form values changed, or if no form controls
		/// are included in this post-back.</param>
		/// <param name="firstTopValidationMethod"></param>
		/// <param name="firstModificationMethod"></param>
		/// <param name="fileGetter">A method that returns the file EWF will send if there were no modification errors.</param>
		public static ActionPostBack CreateIntermediate(
			IEnumerable<UpdateRegionSet> updateRegions, DataModification validationDm, bool forceFullPagePostBack = false, string id = "main",
			bool skipModificationIfNoChanges = false, Action<PostBackValueDictionary, Validator> firstTopValidationMethod = null, Action firstModificationMethod = null,
			Func<FileCreator> fileGetter = null ) {
			if( !id.Any() )
				throw new ApplicationException( "The post-back must have an ID." );
			if( validationDm == null )
				throw new ApplicationException( "A validation data-modification is required." );
			return new ActionPostBack(
				forceFullPagePostBack,
				updateRegions,
				id,
				null,
				skipModificationIfNoChanges,
				firstTopValidationMethod,
				firstModificationMethod,
				fileGetter != null ? new Func<PostBackAction>( () => new PostBackAction( fileGetter() ) ) : null,
				validationDm );
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

		internal bool ForceFullPagePostBack { get { return forceFullPagePostBack; } }
		internal string Id { get { return id; } }
		internal bool IsIntermediate { get { return !forcePageDataUpdate.HasValue; } }
		internal bool ForcePageDataUpdate { get { return forcePageDataUpdate.Value; } }
	}

	public class ActionPostBack: PostBack, DataModification, ValidationListInternal {
		private readonly IEnumerable<UpdateRegionSet> updateRegions;
		private readonly bool skipModificationIfNoChanges;
		private readonly BasicDataModification dataModification;
		private readonly Func<PostBackAction> actionGetter;
		private readonly DataModification validationDm;

		internal ActionPostBack(
			bool forceFullPagePostBack, IEnumerable<UpdateRegionSet> updateRegions, string id, bool? forcePageDataUpdate, bool skipModificationIfNoChanges,
			Action<PostBackValueDictionary, Validator> firstTopValidationMethod, Action firstModificationMethod, Func<PostBackAction> actionGetter,
			DataModification validationDm ): base( forceFullPagePostBack, id, forcePageDataUpdate ) {
			this.updateRegions = updateRegions ?? new UpdateRegionSet[ 0 ];
			this.skipModificationIfNoChanges = skipModificationIfNoChanges;

			dataModification = new BasicDataModification();
			if( firstTopValidationMethod != null )
				dataModification.AddTopValidationMethod( firstTopValidationMethod );
			if( firstModificationMethod != null )
				dataModification.AddModificationMethod( firstModificationMethod );

			this.actionGetter = actionGetter;
			this.validationDm = validationDm;
		}

		internal IEnumerable<UpdateRegionSet> UpdateRegions { get { return updateRegions; } }

		void ValidationListInternal.AddValidation( Validation validation ) {
			( (ValidationListInternal)dataModification ).AddValidation( validation );
		}

		/// <summary>
		/// Adds all validations from the specified basic validation list.
		/// </summary>
		public void AddValidations( BasicValidationList validationList ) {
			dataModification.AddValidations( validationList );
		}

		/// <summary>
		/// Adds a validation method whose errors are displayed at the top of the window.
		/// </summary>
		public void AddTopValidationMethod( Action<PostBackValueDictionary, Validator> validationMethod ) {
			dataModification.AddTopValidationMethod( validationMethod );
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

		internal bool Execute( bool formValuesChanged, Action<Validation, IEnumerable<string>> validationErrorHandler, Action<PostBackAction> actionSetter ) {
			return dataModification.Execute(
				skipModificationIfNoChanges,
				formValuesChanged,
				validationErrorHandler,
				performValidationOnly: actionSetter == null,
				additionalMethod: actionGetter != null ? new Action( () => actionSetter( actionGetter() ) ) : null );
		}

		internal DataModification ValidationDm { get { return validationDm; } }
	}
}