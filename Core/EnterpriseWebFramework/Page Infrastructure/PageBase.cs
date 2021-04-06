﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A page in a web application.
	/// </summary>
	public abstract class PageBase: ResourceBase {
		// These strings are duplicated in the JavaScript file.
		internal const string FormId = "ewfForm";
		internal const string HiddenFieldName = "ewfData";

		internal const string ButtonElementName = "ewfButton";

		private static Func<PageContent, Func<string>, Func<string>, ( PageContent basicContent, FlowComponent component, FlowComponent etherealContainer,
			FlowComponent jsInitElement, Action dataUpdateModificationMethod, bool isAutoDataUpdater )> contentGetter;

		[ JsonObject( ItemRequired = Required.Always, MemberSerialization = MemberSerialization.Fields ) ]
		private class HiddenFieldData {
			[ JsonProperty( PropertyName = "componentState" ) ]
			public readonly ImmutableDictionary<string, JToken> ComponentStateValuesById;

			[ JsonProperty( PropertyName = "formValueHash" ) ]
			public readonly string FormValueHash;

			[ JsonProperty( PropertyName = "failingDm", Required = Required.AllowNull ) ]
			public readonly string LastPostBackFailingDmId;

			// This property name is duplicated in the JavaScript file.
			[ JsonProperty( PropertyName = "postBack" ) ]
			public readonly string PostBackId;

			// This property name is duplicated in the JavaScript file.
			[ JsonProperty( PropertyName = "scrollPositionX" ) ]
			public readonly string ScrollPositionX;

			// This property name is duplicated in the JavaScript file.
			[ JsonProperty( PropertyName = "scrollPositionY" ) ]
			public readonly string ScrollPositionY;

			public HiddenFieldData(
				ImmutableDictionary<string, JToken> componentStateValuesById, string formValueHash, string lastPostBackFailingDmId, string postBackId,
				string scrollPositionX, string scrollPositionY ) {
				ComponentStateValuesById = componentStateValuesById;
				FormValueHash = formValueHash;
				LastPostBackFailingDmId = lastPostBackFailingDmId;
				PostBackId = postBackId;
				ScrollPositionX = scrollPositionX;
				ScrollPositionY = scrollPositionY;
			}
		}

		internal static void Init(
			Func<PageContent, Func<string>, Func<string>, ( PageContent, FlowComponent, FlowComponent, FlowComponent, Action, bool )> contentGetter ) {
			EwfValidation.Init(
				() => Current.formState.ValidationPredicate,
				() => Current.formState.DataModifications,
				() => Current.formState.DataModificationsWithValidationsFromOtherElements,
				() => Current.formState.ReportValidationCreated() );
			FormValueStatics.Init(
				formValue => Current.formValues.Add( formValue ),
				() => Current.formState.DataModifications,
				() => AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			ComponentStateItem.Init(
				AssertPageTreeNotBuilt,
				() => Current.elementOrIdentifiedComponentIdGetter(),
				id => {
					var valuesById = AppRequestState.Instance.EwfPageRequestState.ComponentStateValuesById;
					return valuesById != null && valuesById.TryGetValue( id, out var value ) ? value : null;
				},
				() => Current.formState.DataModifications,
				( id, item ) => Current.componentStateItemsById.Add( id, item ) );
			PostBack.Init( () => Current.formState.DataModifications );
			PostBackFormAction.Init(
				postBack => {
					if( !Current.postBacksById.TryGetValue( postBack.Id, out var existingPostBack ) )
						Current.postBacksById.Add( postBack.Id, postBack );
					else if( existingPostBack != postBack )
						throw new ApplicationException( "A post-back with an ID of \"{0}\" already exists in the page.".FormatWith( existingPostBack.Id ) );
				},
				postBack => {
					if( Current.GetPostBack( postBack.Id ) != postBack )
						throw new ApplicationException( "The post-back must have been added to the page." );
					if( ( postBack as ActionPostBack )?.ValidationDm is PostBack validationPostBack &&
					    Current.GetPostBack( validationPostBack.Id ) != validationPostBack )
						throw new ApplicationException( "The post-back's validation data-modification, if it is a post-back, must have been added to the page." );
				} );
			FormState.Init(
				() => Current.formState,
				dataModifications => {
					if( dataModifications.Contains( Current.dataUpdate ) &&
					    dataModifications.Any( i => i != Current.dataUpdate && !( (ActionPostBack)i ).IsIntermediate ) )
						throw new ApplicationException(
							"If the data-update modification is included, it is meaningless to include any full post-backs since these inherently update the page's data." );
				},
				dataModification => dataModification == Current.dataUpdate ? Current.dataUpdatePostBack : (ActionPostBack)dataModification );
			PageBase.contentGetter = contentGetter;
		}

		/// <summary>
		/// Gets the currently executing page, or null if the currently executing resource is not a page.
		/// </summary>
		public new static PageBase Current {
			get {
				if( !( ResourceBase.Current is PageBase pageObject ) )
					return null;
				PageBase next;
				while( ( next = pageObject.nextPageObject ) != null )
					pageObject = next;
				return pageObject;
			}
		}

		/// <summary>
		/// Add a status message of the given type to the status message collection.
		/// </summary>
		public static void AddStatusMessage( StatusMessageType type, string message ) {
			Current.statusMessages.Add( new Tuple<StatusMessageType, string>( type, message ) );
		}

		internal static void AssertPageTreeNotBuilt() {
			if( Current.formState == null )
				throw new ApplicationException( "The page tree has already been built." );
		}

		internal static void AssertPageTreeBuilt() {
			if( Current.formState != null )
				throw new ApplicationException( "The page tree has not yet been built." );
		}

		private PageBase nextPageObject;

		private FormState formState;
		internal PageContent BasicContent;
		private PageTree pageTree;
		private Func<string> elementOrIdentifiedComponentIdGetter = () => "";
		private readonly BasicDataModification dataUpdate = new BasicDataModification();
		private readonly PostBack dataUpdatePostBack = PostBack.CreateDataUpdate();
		internal bool? IsAutoDataUpdater;
		private readonly Dictionary<string, PostBack> postBacksById = new Dictionary<string, PostBack>();
		private readonly List<FormValue> formValues = new List<FormValue>();
		private readonly Dictionary<string, ComponentStateItem> componentStateItemsById = new Dictionary<string, ComponentStateItem>();
		private IReadOnlyCollection<PageNode> updateRegionLinkerNodes;
		private readonly Dictionary<EwfValidation, List<string>> modErrorDisplaysByValidation = new Dictionary<EwfValidation, List<string>>();
		private readonly HashSet<EwfValidation> validationsWithErrors = new HashSet<EwfValidation>();
		private readonly List<Action> controlTreeValidations = new List<Action>();
		internal PostBack SubmitButtonPostBack;
		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();

		/// <summary>
		/// Gets the parameters modification object for this page. 
		/// </summary>
		public abstract ParametersModificationBase ParametersModificationAsBaseType { get; }

		protected sealed override ExternalRedirect getRedirect() => base.getRedirect();

		protected sealed override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( processViewAndGetResponse() );

		private EwfResponse processViewAndGetResponse() {
			if( AppRequestState.Instance.EwfPageRequestState == null ) {
				if( StandardLibrarySessionState.Instance.EwfPageRequestState != null ) {
					AppRequestState.Instance.EwfPageRequestState = StandardLibrarySessionState.Instance.EwfPageRequestState;
					StandardLibrarySessionState.Instance.EwfPageRequestState = null;
				}
				else
					AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( null, null );
			}

			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var dmIdAndSecondaryOp = requestState.DmIdAndSecondaryOp;

			// Page-view data modifications. All data modifications that happen simply because of a request and require no other action by the user should happen once
			// per page view, and prior to LoadData so that the modified data can be used in the page if necessary.
			if( requestState.StaticRegionContents == null || ( !requestState.ModificationErrorsExist && dmIdAndSecondaryOp != null &&
			                                                   new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains(
				                                                   dmIdAndSecondaryOp.Item2 ) ) ) {
				var modMethods = new List<Action>();
				modMethods.Add( EwfApp.Instance.GetPageViewDataModificationMethod() );
				if( AppRequestState.Instance.UserAccessible ) {
					if( AppTools.User != null )
						modMethods.Add( getLastPageRequestTimeUpdateMethod( AppTools.User ) );
					if( AppRequestState.Instance.ImpersonatorExists && AppRequestState.Instance.ImpersonatorUser != null )
						modMethods.Add( getLastPageRequestTimeUpdateMethod( AppRequestState.Instance.ImpersonatorUser ) );
				}
				modMethods.Add( getPageViewDataModificationMethod() );
				modMethods = modMethods.Where( i => i != null ).ToList();

				if( modMethods.Any() ) {
					DataAccessState.Current.DisableCache();
					try {
						foreach( var i in modMethods )
							i();
						AppRequestState.AddNonTransactionalModificationMethod(
							() => {
								StandardLibrarySessionState.Instance.StatusMessages.AddRange( statusMessages );
								statusMessages.Clear();
							} );
						AppRequestState.Instance.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
					}
					finally {
						DataAccessState.Current.ResetCache();
					}
				}

				// Re-create page object. A big reason to do this is that some pages execute database queries or other code during initialization in order to prime the
				// data-access cache. The code above resets the cache and we want to re-prime it right away.
				AppRequestState.Instance.UserDisabledByResource = true;
				try {
					using( MiniProfiler.Current.Step( "EWF - Re-create page object after page-view data modifications" ) )
						nextPageObject = reCreate();

					bool urlChanged;
					using( MiniProfiler.Current.Step( "EWF - Check URL after page-view data modifications" ) )
						urlChanged = nextPageObject.GetUrl( false, false, true ) != AppRequestState.Instance.Url;
					if( urlChanged )
						throw getPossibleDeveloperMistakeException( "The URL of the page changed after page-view data modifications." );
				}
				finally {
					AppRequestState.Instance.UserDisabledByResource = false;
				}
				bool userAuthorized;
				using( MiniProfiler.Current.Step( "EWF - Check page authorization after page-view data modifications" ) )
					userAuthorized = nextPageObject.UserCanAccessResource;
				DisabledResourceMode disabledMode;
				using( MiniProfiler.Current.Step( "EWF - Check alternative page mode after page-view data modifications" ) )
					disabledMode = nextPageObject.AlternativeMode as DisabledResourceMode;
				if( !userAuthorized || disabledMode != null )
					throw getPossibleDeveloperMistakeException( "The user lost access to the page or the page became disabled after page-view data modifications." );
				return nextPageObject.processSecondaryOperationAndGetResponse();
			}

			return processSecondaryOperationAndGetResponse();
		}

		private EwfResponse processSecondaryOperationAndGetResponse() {
			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var dmIdAndSecondaryOp = requestState.DmIdAndSecondaryOp;

			onLoadData();

			if( requestState.StaticRegionContents != null ) {
				var nodeUpdateRegionLinkersByKey = updateRegionLinkerNodes.SelectMany( i => i.KeyedUpdateRegionLinkers, ( node, keyedLinker ) => ( node, keyedLinker ) )
					.ToImmutableDictionary( i => i.keyedLinker.key );
				var updateRegions = requestState.UpdateRegionKeysAndArguments.Select(
					keyAndArg => {
						if( !nodeUpdateRegionLinkersByKey.TryGetValue( keyAndArg.Item1, out var nodeLinker ) )
							throw getPossibleDeveloperMistakeException( "An update region linker with the key \"{0}\" does not exist.".FormatWith( keyAndArg.Item1 ) );
						return ( nodeLinker.node, nodeLinker.keyedLinker.linker.PostModificationRegionGetter( keyAndArg.Item2 ) );
					} );

				var staticRegionContents = getStaticRegionContents( updateRegions );
				if( staticRegionContents.contents != requestState.StaticRegionContents || componentStateItemsById.Values.Any( i => i.ValueIsInvalid() ) ||
				    formValues.Any( i => i.GetPostBackValueKey().Any() && i.PostBackValueIsInvalid() ) )
					throw getPossibleDeveloperMistakeException(
						requestState.ModificationErrorsExist
							?
							"Post-backs, form controls, component-state items, and modification-error-display keys may not change if modification errors exist." +
							" (IMPORTANT: This exception may have been thrown because EWL Goal 588 hasn't been completed. See the note in the goal about the EwfPage bug and disregard the rest of this error message.)"
							: new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains( dmIdAndSecondaryOp.Item2 )
								? "Form controls and component-state items outside of update regions may not change on an intermediate post-back."
								: "Post-backs, form controls, and component-state items may not change during the validation stage of an intermediate post-back." );
			}

			if( !requestState.ModificationErrorsExist && dmIdAndSecondaryOp != null && dmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.Validate ) {
				var secondaryDm = dmIdAndSecondaryOp.Item1.Any() ? GetPostBack( dmIdAndSecondaryOp.Item1 ) as DataModification : dataUpdate;
				if( secondaryDm == null )
					throw getPossibleDeveloperMistakeException( "A data modification with an ID of \"{0}\" does not exist.".FormatWith( dmIdAndSecondaryOp.Item1 ) );

				var navigationNeeded = true;
				executeWithDataModificationExceptionHandling(
					() => {
						var changesExist = componentStateItemsById.Values.Any( i => i.DataModifications.Contains( secondaryDm ) && i.ValueChanged() ) || formValues.Any(
							                   i => i.DataModifications.Contains( secondaryDm ) && i.ValueChangedOnPostBack() );
						if( secondaryDm == dataUpdate )
							navigationNeeded = dataUpdate.Execute( true, changesExist, handleValidationErrors, performValidationOnly: true );
						else
							navigationNeeded = ( (ActionPostBack)secondaryDm ).Execute( changesExist, handleValidationErrors, null );

						if( navigationNeeded ) {
							requestState.DmIdAndSecondaryOp = Tuple.Create( dmIdAndSecondaryOp.Item1, SecondaryPostBackOperation.NoOperation );
							requestState.SetStaticAndUpdateRegionState( getStaticRegionContents( null ).contents, new Tuple<string, string>[ 0 ] );
						}
					} );
				if( navigationNeeded )
					return navigate( null, null );
			}

			return getResponse();
		}

		/// <summary>
		/// It's important to call this from EwfPage instead of EwfApp because requests for some pages, with their associated images, CSS files, etc., can easily
		/// cause 20-30 server requests, and we only want to update the time stamp once for all of these.
		/// </summary>
		private Action getLastPageRequestTimeUpdateMethod( User user ) {
			// Only update the request time if a significant amount of time has passed since we did it last. This can dramatically reduce concurrency issues caused by
			// people rapidly assigning tasks to one another in the System Manager or similar situations.
			if( AppRequestState.RequestTime - user.LastRequestTime < Duration.FromMinutes( 60 ) )
				return null;

			// Now we want to do a timestamp-based concurrency check so we don't update the last login date if we know another transaction already did.
			// It is not perfect, but it reduces errors caused by one user doing a long-running request and then doing smaller requests
			// in another browser window while the first one is still running.
			// We have to query in a separate transaction because otherwise snapshot isolation will result in us always getting the original LastRequestTime, even if
			// another transaction has modified its value during this transaction.
			var newlyQueriedUser = new DataAccessState().ExecuteWithThis(
				() => {
					User getUser() => UserManagementStatics.GetUser( user.UserId, false );
					return ConfigurationStatics.DatabaseExists ? DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen( getUser ) : getUser();
				} );
			if( newlyQueriedUser == null || newlyQueriedUser.LastRequestTime > user.LastRequestTime )
				return null;

			return () => {
				void updateUser() {
					if( FormsAuthStatics.FormsAuthEnabled ) {
						var formsAuthCapableUser = (FormsAuthCapableUser)user;
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							user.UserId,
							user.Email,
							user.Role.RoleId,
							AppRequestState.RequestTime,
							formsAuthCapableUser.Salt,
							formsAuthCapableUser.SaltedPassword,
							formsAuthCapableUser.MustChangePassword );
					}
					else
						( UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider )?.InsertOrUpdateUser(
							user.UserId,
							user.Email,
							user.Role.RoleId,
							AppRequestState.RequestTime );
				}
				if( ConfigurationStatics.DatabaseExists )
					DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction(
						() => {
							try {
								updateUser();
							}
							catch( DbConcurrencyException ) {
								// Since this method is called on every page request, concurrency errors are common. They are caused when an authenticated user makes one request
								// and then makes another before ASP.NET has finished processing the first. Since we are only updating the last request date and time, we don't
								// need to get an error email if the update fails.
								throw new DoNotCommitException();
							}
						} );
				else
					updateUser();
			};
		}

		// The warning below also appears on EwfApp.GetPageViewDataModificationMethod.
		/// <summary>
		/// Returns a method that executes data modifications that happen simply because of a request and require no other action by the user. Returns null if there
		/// are no modifications, which can improve page performance since the data-access cache does not need to be reset.
		/// 
		/// WARNING: Don't ever use this to correct for missing loadData preconditions. For example, do not create a page that requires a user preferences row to
		/// exist and then use a page-view data modification to create the row if it is missing. Page-view data modifications will not execute before the first
		/// loadData call on post-back requests, and we provide no mechanism to do this because it would allow developers to accidentally cause false user
		/// concurrency errors by modifying data that affects the rendering of the page.
		/// </summary>
		protected virtual Action getPageViewDataModificationMethod() {
			return null;
		}

		/// <summary>
		/// EWF use only.
		/// </summary>
		protected abstract PageBase reCreate();

		protected sealed override EwfResponse put() => base.put();
		protected sealed override EwfResponse patch() => base.patch();
		protected sealed override EwfResponse delete() => base.delete();

		protected sealed override EwfResponse post() {
			HiddenFieldData hiddenFieldData;
			try {
				// throws exception if field missing, because Request.Form returns null
				hiddenFieldData = JsonConvert.DeserializeObject<HiddenFieldData>(
					HttpContext.Current.Request.Form[ HiddenFieldName ],
					new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error } );

				AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( hiddenFieldData.ScrollPositionX, hiddenFieldData.ScrollPositionY );
				AppRequestState.Instance.EwfPageRequestState.ComponentStateValuesById = hiddenFieldData.ComponentStateValuesById;
			}
			catch {
				// Set a 400 status code if there are any problems loading hidden field state. We're assuming these problems are never the developers' fault.
				if( AppRequestState.Instance.EwfPageRequestState == null )
					AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( null, null );
				HttpContext.Current.Response.StatusCode = 400;
				HttpContext.Current.Response.TrySkipIisCustomErrors = true;
				AppRequestState.Instance.EwfPageRequestState.FocusKey = "";
				AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors =
					Translation.ApplicationHasBeenUpdatedAndWeCouldNotInterpretAction.ToCollection();
				nextPageObject = reCreate();
				return nextPageObject.processViewAndGetResponse();
			}

			onLoadData();

			ResourceInfo redirectInfo = null;
			FullResponse fullSecondaryResponse = null;
			executeWithDataModificationExceptionHandling(
				() => {
					validateFormSubmission( hiddenFieldData.FormValueHash );

					// Get the post-back object and, if necessary, the last post-back's failing data modification.
					var postBack = GetPostBack( hiddenFieldData.PostBackId );
					if( postBack == null )
						throw new DataModificationException( Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction );
					var lastPostBackFailingDm = postBack.IsIntermediate && hiddenFieldData.LastPostBackFailingDmId != null
						                            ? hiddenFieldData.LastPostBackFailingDmId.Any()
							                              ?
							                              GetPostBack( hiddenFieldData.LastPostBackFailingDmId ) as DataModification
							                              : dataUpdate
						                            : null;
					if( postBack.IsIntermediate && hiddenFieldData.LastPostBackFailingDmId != null && lastPostBackFailingDm == null )
						throw new DataModificationException( Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction );

					// Execute the page's data update.
					var requestState = AppRequestState.Instance.EwfPageRequestState;
					bool changesExist( DataModification dataModification ) =>
						componentStateItemsById.Values.Any( i => i.DataModifications.Contains( dataModification ) && i.ValueChanged() ) || formValues.Any(
							i => i.DataModifications.Contains( dataModification ) && i.ValueChangedOnPostBack() );
					var dmExecuted = false;
					if( !postBack.IsIntermediate )
						try {
							dmExecuted |= dataUpdate.Execute( !postBack.ForcePageDataUpdate, changesExist( dataUpdate ), handleValidationErrors );
						}
						catch {
							requestState.DmIdAndSecondaryOp = Tuple.Create( "", SecondaryPostBackOperation.NoOperation );
							throw;
						}

					// Execute the post-back.
					var actionPostBack = postBack as ActionPostBack;
					if( actionPostBack != null ) {
						requestState.FocusKey = "";
						try {
							dmExecuted |= actionPostBack.Execute(
								changesExist( actionPostBack ),
								handleValidationErrors,
								postBackAction => {
									redirectInfo = postBackAction?.Resource;
									requestState.FocusKey = postBackAction?.ReloadBehavior?.FocusKey ?? "";
									fullSecondaryResponse = postBackAction?.ReloadBehavior?.SecondaryResponse?.GetFullResponse();
								} );
						}
						catch {
							requestState.DmIdAndSecondaryOp = Tuple.Create( actionPostBack.Id, SecondaryPostBackOperation.NoOperation );
							throw;
						}
					}

					if( dmExecuted ) {
						AppRequestState.AddNonTransactionalModificationMethod( () => StandardLibrarySessionState.Instance.StatusMessages.AddRange( statusMessages ) );
						try {
							AppRequestState.Instance.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
						}
						finally {
							DataAccessState.Current.ResetCache();
						}
					}

					if( postBack.IsIntermediate ) {
						var regionSets = actionPostBack.UpdateRegions.ToImmutableHashSet();
						var updateRegions = updateRegionLinkerNodes.SelectMany( i => i.KeyedUpdateRegionLinkers, ( node, keyedLinker ) => ( node, keyedLinker ) )
							.SelectMany(
								nodeLinker => nodeLinker.keyedLinker.linker.PreModificationRegions.Where( i => regionSets.Overlaps( i.Sets ) ),
								( nodeLinker, region ) => ( nodeLinker.node, nodeLinker.keyedLinker.key, region ) )
							.Materialize();
						var staticRegionContents = getStaticRegionContents( updateRegions.Select( i => ( i.node, i.region.ComponentGetter() ) ) );

						requestState.ComponentStateValuesById = componentStateItemsById.Where( i => staticRegionContents.stateItems.Contains( i.Value ) )
							.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson );
						requestState.PostBackValues.RemoveExcept( staticRegionContents.formValues.Select( i => i.GetPostBackValueKey() ) );
						requestState.DmIdAndSecondaryOp = Tuple.Create(
							actionPostBack.ValidationDm == dataUpdate ? "" : ( (ActionPostBack)actionPostBack.ValidationDm ).Id,
							actionPostBack.ValidationDm == lastPostBackFailingDm ? SecondaryPostBackOperation.Validate : SecondaryPostBackOperation.ValidateChangesOnly );
						requestState.SetStaticAndUpdateRegionState(
							staticRegionContents.contents,
							updateRegions.Select( i => Tuple.Create( i.key, i.region.ArgumentGetter() ) ).Materialize() );
					}
					else
						AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( null, null );
				} );

			return navigate( redirectInfo, AppRequestState.Instance.EwfPageRequestState.ModificationErrorsExist ? null : fullSecondaryResponse );
		}

		private void onLoadData() {
			var elementJsInitStatements = new StringBuilder();

			formState = new FormState();
			var content = contentGetter(
				FormState.ExecuteWithDataModificationsAndDefaultAction(
					DataUpdate.ToCollection(),
					() => {
						using( MiniProfiler.Current.Step( "EWF - Get page content" ) )
							return getContent();
					} ),
				() => {
					var rs = AppRequestState.Instance.EwfPageRequestState;
					var failingDmId =
						rs.ModificationErrorsExist && rs.DmIdAndSecondaryOp != null && rs.DmIdAndSecondaryOp.Item2 != SecondaryPostBackOperation.ValidateChangesOnly
							? rs.DmIdAndSecondaryOp.Item1
							: null;

					return JsonConvert.SerializeObject(
						new HiddenFieldData(
							componentStateItemsById.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson ),
							generateFormValueHash(),
							failingDmId,
							"",
							"",
							"" ),
						Formatting.None );
				},
				() => getJsInitStatements( elementJsInitStatements.ToString() ) );
			BasicContent = content.basicContent;
			if( content.dataUpdateModificationMethod != null )
				dataUpdate.AddModificationMethod( content.dataUpdateModificationMethod );
			IsAutoDataUpdater = content.isAutoDataUpdater;
			using( MiniProfiler.Current.Step( "EWF - Build page tree" ) )
				pageTree = new PageTree(
					content.component,
					id => elementOrIdentifiedComponentIdGetter = () => id,
					addModificationErrorDisplaysAndGetErrors,
					content.etherealContainer,
					content.jsInitElement,
					elementJsInitStatements );
			formState = null;

			var activeStateItems = pageTree.AllNodes.Select( i => i.StateItem ).Where( i => i != null ).ToImmutableHashSet();
			foreach( var i in componentStateItemsById.Where( i => !activeStateItems.Contains( i.Value ) ).Select( i => i.Key ).Materialize() )
				componentStateItemsById.Remove( i );

			updateRegionLinkerNodes = pageTree.AllNodes.Where( i => i.KeyedUpdateRegionLinkers != null ).Materialize();

			var duplicatePostBackValueKeys = formValues.Select( i => i.GetPostBackValueKey() ).Where( i => i.Any() ).GetDuplicates().ToArray();
			if( duplicatePostBackValueKeys.Any() )
				throw new ApplicationException(
					"Duplicate post-back-value keys exist: " + StringTools.ConcatenateWithDelimiter( ", ", duplicatePostBackValueKeys ) + "." );

			foreach( var i in controlTreeValidations )
				i();

			foreach( var i in formValues )
				i.SetPageModificationValues();

			// This must happen after LoadData and before modifications are executed.
			statusMessages.Clear();
		}

		/// <summary>
		/// Returns the page content.
		/// </summary>
		protected virtual PageContent getContent() => null;

		private ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> addModificationErrorDisplaysAndGetErrors(
			string id, ErrorSourceSet errorSources ) =>
			errorSources.Validations.Select(
					( validation, index ) => {
						var displayKey = id + index;
						if( modErrorDisplaysByValidation.ContainsKey( validation ) )
							modErrorDisplaysByValidation[ validation ].Add( displayKey );
						else
							modErrorDisplaysByValidation.Add( validation, displayKey.ToCollection().ToList() );

						// We want to ignore all of the problems that could happen, such as the key not existing in the dictionary. This problem will be shown in a more
						// helpful way when we compare form control hashes after a transfer.
						//
						// Avoid using exceptions here if possible. This method is sometimes called many times during a request, and we've seen exceptions take as long as
						// 50 ms each when debugging.
						var errors = AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay.TryGetValue( displayKey, out var value )
							             ? value.Materialize()
							             : new string[ 0 ];

						if( errors.Any() )
							validationsWithErrors.Add( validation );

						return ( validation, errors );
					} )
				.ToImmutableDictionary( i => i.validation, i => i.errors );

		/// <summary>
		/// Gets the page's data-update modification, which executes on every full post-back prior to the post-back object. WARNING: Do *not* use this for
		/// modifications that should happen because of a specific post-back action, e.g. adding a new item to the database when a button is clicked. There are two
		/// reasons for this. First, there may be other post-back controls such as buttons or lookup boxes on the page, any of which could also cause the update to
		/// execute. Second, by default the update only runs if form values were modified, which would not be the case if a user clicks the button on an add-item
		/// page before entering any data.
		/// </summary>
		public DataModification DataUpdate => dataUpdate;

		/// <summary>
		/// Gets a post-back that updates the page's data without performing any other actions.
		/// </summary>
		public PostBack DataUpdatePostBack => dataUpdatePostBack;

		internal PostBack GetPostBack( string id ) => postBacksById.TryGetValue( id, out var value ) ? value : null;

		internal void AddControlTreeValidation( Action validation ) {
			controlTreeValidations.Add( validation );
		}

		/// <summary>
		/// EWL use only. Gets the status messages.
		/// </summary>
		public IEnumerable<Tuple<StatusMessageType, string>> StatusMessages => StandardLibrarySessionState.Instance.StatusMessages.Concat( statusMessages );

		private void executeWithDataModificationExceptionHandling( Action method ) {
			try {
				method();
			}
			catch( Exception e ) {
				var dmException = e.GetChain().OfType<DataModificationException>().FirstOrDefault();
				if( dmException == null )
					throw;
				AppRequestState.Instance.EwfPageRequestState.FocusKey = "";
				AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors = dmException.HtmlMessages;
				AppRequestState.Instance.EwfPageRequestState.SetStaticAndUpdateRegionState( getStaticRegionContents( null ).contents, new Tuple<string, string>[ 0 ] );
			}
		}

		private void validateFormSubmission( string formValueHash ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

			var activeFormValues = formValues.Where( i => i.GetPostBackValueKey().Any() ).ToArray();
			var postBackValueKeys = new HashSet<string>( activeFormValues.Select( i => i.GetPostBackValueKey() ) );
			requestState.PostBackValues = new PostBackValueDictionary();
			var extraPostBackValuesExist = requestState.ComponentStateValuesById.Keys.Any( i => !componentStateItemsById.ContainsKey( i ) ) |
			                               requestState.PostBackValues.AddFromRequest(
				                               HttpContext.Current.Request.Form.Cast<string>().Except( new[] { HiddenFieldName, ButtonElementName } ),
				                               postBackValueKeys.Contains,
				                               key => HttpContext.Current.Request.Form[ key ] ) | requestState.PostBackValues.AddFromRequest(
				                               HttpContext.Current.Request.Files.Cast<string>(),
				                               postBackValueKeys.Contains,
				                               key => HttpContext.Current.Request.Files[ key ] );

			// Make sure data didn't change under this page's feet since the last request.
			var invalidPostBackValuesExist =
				componentStateItemsById.Any( i => !requestState.ComponentStateValuesById.ContainsKey( i.Key ) || i.Value.ValueIsInvalid() ) ||
				activeFormValues.Any( i => i.PostBackValueIsInvalid() );
			var formValueHashesDisagree = generateFormValueHash() != formValueHash;
			if( extraPostBackValuesExist || invalidPostBackValuesExist || formValueHashesDisagree ) {
				// Remove invalid post-back values so they don't cause a false developer-mistake exception after the transfer.
				requestState.ComponentStateValuesById = requestState.ComponentStateValuesById.RemoveRange(
					from i in componentStateItemsById where i.Value.ValueIsInvalid() select i.Key );
				var validPostBackValueKeys = from i in activeFormValues where !i.PostBackValueIsInvalid() select i.GetPostBackValueKey();
				requestState.PostBackValues.RemoveExcept( validPostBackValueKeys );

				throw new DataModificationException( Translation.AnotherUserHasModifiedPageHtml.ToCollection() );
			}
		}

		private string generateFormValueHash() {
			var formValueString = new StringBuilder();
			foreach( var pair in componentStateItemsById.Where( i => i.Value.DataModifications.Any() ).OrderBy( i => i.Key ) ) {
				formValueString.Append( pair.Key );
				formValueString.Append( pair.Value.DurableValue );
			}
			foreach( var formValue in formValues.Where( i => i.GetPostBackValueKey().Any() && i.DataModifications.Any() ) ) {
				formValueString.Append( formValue.GetPostBackValueKey() );
				formValueString.Append( formValue.GetDurableValueAsString() );
			}

			var hash = MD5.Create().ComputeHash( Encoding.ASCII.GetBytes( formValueString.ToString() ) );
			var hashString = "";
			foreach( var b in hash )
				hashString += b.ToString( "x2" );
			return hashString;
		}

		private void handleValidationErrors( EwfValidation validation, IEnumerable<string> errorMessages ) {
			if( !errorMessages.Any() )
				return;
			if( !modErrorDisplaysByValidation.ContainsKey( validation ) )
				throw new ApplicationException( "An undisplayed validation produced errors." );
			foreach( var displayKey in modErrorDisplaysByValidation[ validation ] ) {
				var errorsByDisplay = AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay;
				errorsByDisplay[ displayKey ] = errorsByDisplay.ContainsKey( displayKey ) ? errorsByDisplay[ displayKey ].Concat( errorMessages ) : errorMessages;
			}
		}

		private ( string contents, ImmutableHashSet<ComponentStateItem> stateItems, IReadOnlyCollection<FormValue> formValues ) getStaticRegionContents(
			IEnumerable<( PageNode node, IEnumerable<PageComponent> components )> updateRegions ) {
			var contents = new StringBuilder();

			var staticNodes = pageTree.GetStaticRegionNodes( updateRegions );
			var staticStateItems = staticNodes.Select( i => i.StateItem ).Where( i => i != null ).ToImmutableHashSet();
			var staticFormValues = staticNodes.Select( i => i.FormValue ).Where( i => i != null ).Distinct().OrderBy( i => i.GetPostBackValueKey() ).Materialize();

			foreach( var pair in componentStateItemsById.Where( i => staticStateItems.Contains( i.Value ) ).OrderBy( i => i.Key ) ) {
				contents.Append( pair.Key );
				contents.Append( pair.Value.DurableValue );
			}
			foreach( var formValue in staticFormValues ) {
				contents.Append( formValue.GetPostBackValueKey() );
				contents.Append( formValue.GetDurableValueAsString() );
			}

			var requestState = AppRequestState.Instance.EwfPageRequestState;
			if( requestState.ModificationErrorsExist )
				// Include mod error display keys. They shouldn't change across a transfer when there are modification errors because that could prevent some of the
				// errors from being displayed.
				foreach( var modErrorDisplayKey in modErrorDisplaysByValidation.Values.SelectMany( i => i ) )
					contents.Append( modErrorDisplayKey + " " );

			if( requestState.ModificationErrorsExist ||
			    ( requestState.DmIdAndSecondaryOp != null && requestState.DmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.NoOperation ) )
				// It's probably bad if a developer puts a post-back object in the page because of a modification error. It will be gone on the post-back and cannot be
				// processed.
				foreach( var postBack in postBacksById.Values.OrderBy( i => i.Id ) )
					contents.Append( postBack.Id );

			return ( contents.ToString(), staticStateItems, staticFormValues );
		}

		private EwfResponse navigate( ResourceInfo destination, FullResponse secondaryResponse ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

			string destinationUrl;
			try {
				// Determine the final navigation destination. If a destination is already specified and it is the current page or a page with the same entity setup,
				// replace any default optional parameter values it may have with new values from this post-back. If a destination isn't specified, make it the current
				// page with new parameter values from this post back. At the end of this block, destination is always newly created with fresh data that reflects any
				// data modifications that may have occurred (except when the destination is an external resource). It's important that every case below *actually
				// creates* a new resource object to guard against this scenario:
				// 1. A page modifies data such that a previously-created destination resource object that is then used here is no longer valid because it would throw
				//    an exception from init if it were re-created.
				// 2. The page redirects, or transfers, to this destination, leading the user to an error page without developers being notified. This is bad behavior.
				// It would also be a problem if the destination were the current page object since it could then contain dirty state from this post-back after
				// navigation.
				if( requestState.ModificationErrorsExist ||
				    ( requestState.DmIdAndSecondaryOp != null && requestState.DmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.NoOperation ) )
					destination = CloneAndReplaceDefaultsIfPossible( true );
				else if( destination == null )
					destination = reCreateFromNewParameterValues();
				else if( destination is ResourceBase r )
					destination = r.CloneAndReplaceDefaultsIfPossible( false );
				nextPageObject = destination as PageBase;

				// This GetUrl call is important even for the transfer case below for the same reason that we *actually create* a new page object in every case above.
				// We want to force developers to get an error email if a page modifies data to make itself unauthorized/disabled without specifying a different page as
				// the redirect destination. The resulting transfer would lead the user to an error page.
				destinationUrl = destination.GetUrl();
			}
			catch( Exception e ) {
				throw getPossibleDeveloperMistakeException( "The post-modification destination page became invalid.", innerException: e );
			}

			// Put the secondary response into session state right before navigation so that it doesn't get sent if there is an error before this point.
			if( secondaryResponse != null ) {
				// It's important that we put the response in session state first since it's used by the Info.init method of the pre-built-response page.
				StandardLibrarySessionState.Instance.ResponseToSend = secondaryResponse;
				StandardLibrarySessionState.Instance.SetClientSideNavigation(
					EwfApp.MetaLogicFactory.CreatePreBuiltResponsePageInfo().GetUrl(),
					!secondaryResponse.FileName.Any(),
					null );
			}

			// If the redirect destination is identical to the current page, do a transfer instead of a redirect.
			if( nextPageObject?.IsIdenticalToCurrent() == true ) {
				AppRequestState.Instance.ClearUserAndImpersonator();
				return nextPageObject.processViewAndGetResponse();
			}

			// If the redirect destination is the current page, but with different query parameters, save request state in session state until the next request.
			if( destination.GetType() == GetType() )
				StandardLibrarySessionState.Instance.EwfPageRequestState = requestState;

			HttpContext.Current.Response.StatusCode = 303;
			return EwfResponse.Create(
				ContentTypes.PlainText,
				new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
				additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
		}

		/// <summary>
		/// Creates a page info object using the new parameter value fields in this page.
		/// </summary>
		protected abstract PageInfo reCreateFromNewParameterValues();

		private ApplicationException getPossibleDeveloperMistakeException( string messageSentence, Exception innerException = null ) {
			var sentences = new[]
				{
					"Possible developer mistake.", messageSentence,
					"There is a chance that this was caused by something outside the request, but it's more likely that a developer incorrectly modified something."
				};
			throw new ApplicationException( StringTools.ConcatenateWithDelimiter( " ", sentences ), innerException );
		}

		private EwfResponse getResponse() {
			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var modificationErrorsOccurred = requestState.ModificationErrorsExist &&
			                                 ( requestState.DmIdAndSecondaryOp == null ||
			                                   !new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains(
				                                   requestState.DmIdAndSecondaryOp.Item2 ) );

			Func<FocusabilityCondition, bool> isFocusablePredicate;
			if( modificationErrorsOccurred )
				isFocusablePredicate = condition => condition.ErrorFocusabilitySources.Validations.Any( i => validationsWithErrors.Contains( i ) ) ||
				                                    ( condition.ErrorFocusabilitySources.IncludeGeneralErrors &&
				                                      AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors.Any() );
			else
				isFocusablePredicate = condition => condition.IsNormallyFocusable;

			pageTree.PrepareForRendering( modificationErrorsOccurred, isFocusablePredicate );


			// Direct response object modifications. These should happen once per page view; they are not needed in redirect responses.

			FormsAuthStatics.UpdateFormsAuthCookieIfNecessary();

			var response = EwfResponse.Create(
				ContentTypes.Html,
				new EwfResponseBodyCreator( pageTree.WriteMarkup ),
				additionalHeaderFieldGetter: () => {
					var headerFields = new List<( string, string )>();

					if( !ConfigurationStatics.IsLiveInstallation )
						headerFields.Add( ( "X-Robots-Tag", "noindex, nofollow" ) );
					else if( !AllowsSearchEngineIndexing )
						headerFields.Add( ( "X-Robots-Tag", "noindex" ) );

					// Without this header, certain sites could be forced into compatibility mode due to the Compatibility View Blacklist maintained by Microsoft.
					headerFields.Add( ( "X-UA-Compatible", "IE=edge" ) );

					return headerFields;
				} );


			StandardLibrarySessionState.Instance.StatusMessages.Clear();
			StandardLibrarySessionState.Instance.ClearClientSideNavigation();

			return response;
		}

		private string getJsInitStatements( string elementJsInitStatements ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var scroll = scrollPositionForThisResponse == ScrollPosition.LastPositionOrStatusBar &&
			             ( !requestState.ModificationErrorsExist || ( requestState.DmIdAndSecondaryOp != null &&
			                                                          new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }
				                                                          .Contains( requestState.DmIdAndSecondaryOp.Item2 ) ) );
			var scrollStatement = "";
			if( scroll && requestState.ScrollPositionX != null && requestState.ScrollPositionY != null )
				scrollStatement = "window.scroll(" + requestState.ScrollPositionX + "," + requestState.ScrollPositionY + ");";

			// If the page has requested a client-side redirect, configure it now. The JavaScript solution is preferred over a meta tag since apparently it doesn't
			// cause reload behavior by the browser. See http://josephsmarr.com/2007/06/06/the-hidden-cost-of-meta-refresh-tags.
			StandardLibrarySessionState.Instance.GetClientSideNavigationSetup(
				out var clientSideNavigationUrl,
				out var clientSideNavigationInNewWindow,
				out var clientSideNavigationDelay );
			var clientSideNavigationStatements = "";
			if( clientSideNavigationUrl.Any() ) {
				var url = clientSideNavigationUrl;
				if( clientSideNavigationInNewWindow )
					clientSideNavigationStatements = "var newWindow = window.open( '{0}', '{1}' ); newWindow.focus();".FormatWith( url, "_blank" );
				else
					clientSideNavigationStatements = "location.replace( '" + url + "' );";
				if( clientSideNavigationDelay.HasValue )
					clientSideNavigationStatements = "setTimeout( \"" + clientSideNavigationStatements + "\", " + clientSideNavigationDelay.Value * 1000 + " );";
			}

			return StringTools.ConcatenateWithDelimiter(
				" ",
				"OnDocumentReady();",
				"$( '#{0}' ).submit( function( e, postBackId ) {{ postBackRequestStarting( e, postBackId !== undefined ? postBackId : '{1}' ); }} );".FormatWith(
					FormId,
					SubmitButtonPostBack != null
						? SubmitButtonPostBack.Id
						: "" /* This empty string we're using when no submit button exists is arbitrary and meaningless; it should never actually be submitted. */ ),
				elementJsInitStatements,
				EwfApp.Instance.JavaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
				javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
				StringTools.ConcatenateWithDelimiter( " ", scrollStatement, clientSideNavigationStatements )
					.PrependDelimiter( "window.onload = function() { " )
					.AppendDelimiter( " };" ) );
		}

		/// <summary>
		/// The desired scroll position of the browser when this response is received.
		/// </summary>
		protected virtual ScrollPosition scrollPositionForThisResponse => ScrollPosition.LastPositionOrStatusBar;

		/// <summary>
		/// Gets the function call that should be executed when the jQuery document ready event is fired.
		/// </summary>
		protected virtual string javaScriptDocumentReadyFunctionCall => "";
	}
}