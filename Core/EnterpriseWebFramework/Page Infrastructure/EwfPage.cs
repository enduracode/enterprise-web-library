using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
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
	/// A System.Web.UI.Page that contains special Red Stapler Enterprise Web Framework logic. Requires that view state and session state be enabled.
	/// </summary>
	public abstract class EwfPage: Page {
		// This string is duplicated in the JavaScript file.
		private const string hiddenFieldName = "ewfData";

		internal const string ButtonElementName = "ewfButton";

		private static Func<IEnumerable<EtherealComponent>> browsingModalBoxCreator;
		private static Func<IEnumerable<ResourceInfo>> cssInfoCreator;

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

			public HiddenFieldData(
				ImmutableDictionary<string, JToken> componentStateValuesById, string formValueHash, string lastPostBackFailingDmId, string postBackId ) {
				ComponentStateValuesById = componentStateValuesById;
				FormValueHash = formValueHash;
				LastPostBackFailingDmId = lastPostBackFailingDmId;
				PostBackId = postBackId;
			}
		}

		internal new static void Init( Func<IEnumerable<EtherealComponent>> browsingModalBoxCreator, Func<IEnumerable<ResourceInfo>> cssInfoCreator ) {
			EwfValidation.Init(
				() => Instance.formState.ValidationPredicate,
				() => Instance.formState.DataModifications,
				() => Instance.formState.DataModificationsWithValidationsFromOtherElements,
				() => Instance.formState.ReportValidationCreated() );
			FormValueStatics.Init(
				formValue => Instance.formValues.Add( formValue ),
				() => Instance.formState.DataModifications,
				() => AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			ComponentStateItem.Init(
				AssertPageTreeNotBuilt,
				() => Instance.elementOrIdentifiedComponentIdGetter(),
				id => {
					var valuesById = AppRequestState.Instance.EwfPageRequestState.ComponentStateValuesById;
					return valuesById != null && valuesById.TryGetValue( id, out var value ) ? value : null;
				},
				() => Instance.formState.DataModifications,
				( id, item ) => Instance.componentStateItemsById.Add( id, item ) );
			PostBack.Init( () => Instance.formState.DataModifications );
			PostBackFormAction.Init(
				postBack => {
					if( !Instance.postBacksById.TryGetValue( postBack.Id, out var existingPostBack ) )
						Instance.postBacksById.Add( postBack.Id, postBack );
					else if( existingPostBack != postBack )
						throw new ApplicationException( "A post-back with an ID of \"{0}\" already exists in the page.".FormatWith( existingPostBack.Id ) );
				},
				postBack => {
					if( Instance.GetPostBack( postBack.Id ) != postBack )
						throw new ApplicationException( "The post-back must have been added to the page." );
					if( ( postBack as ActionPostBack )?.ValidationDm is PostBack validationPostBack &&
					    Instance.GetPostBack( validationPostBack.Id ) != validationPostBack )
						throw new ApplicationException( "The post-back's validation data-modification, if it is a post-back, must have been added to the page." );
				} );
			FormState.Init(
				() => Instance.formState,
				dataModifications => {
					if( dataModifications.Contains( Instance.dataUpdate ) &&
					    dataModifications.Any( i => i != Instance.dataUpdate && !( (ActionPostBack)i ).IsIntermediate ) )
						throw new ApplicationException(
							"If the data-update modification is included, it is meaningless to include any full post-backs since these inherently update the page's data." );
				},
				dataModification => dataModification == Instance.dataUpdate ? Instance.dataUpdatePostBack : (ActionPostBack)dataModification );
			EwfPage.browsingModalBoxCreator = browsingModalBoxCreator;
			EwfPage.cssInfoCreator = cssInfoCreator;
		}

		/// <summary>
		/// Returns the currently executing EwfPage, or null if the currently executing page is not an EwfPage.
		/// </summary>
		public static EwfPage Instance => HttpContext.Current.CurrentHandler as EwfPage;

		/// <summary>
		/// Add a status message of the given type to the status message collection.
		/// </summary>
		public static void AddStatusMessage( StatusMessageType type, string message ) {
			Instance.statusMessages.Add( new Tuple<StatusMessageType, string>( type, message ) );
		}

		internal static void AssertPageTreeNotBuilt() {
			if( Instance.formState == null )
				throw new ApplicationException( "The page tree has already been built." );
		}

		internal static void AssertPageTreeBuilt() {
			if( Instance.formState != null )
				throw new ApplicationException( "The page tree has not yet been built." );
		}

		private Control etherealPlace;
		private FormState formState;
		private Func<string> elementOrIdentifiedComponentIdGetter = () => "";
		internal readonly ModalBoxId BrowsingModalBoxId = new ModalBoxId();
		private readonly BasicDataModification dataUpdate = new BasicDataModification();
		private readonly PostBack dataUpdatePostBack = PostBack.CreateDataUpdate();

		internal readonly Dictionary<PageComponent, IReadOnlyCollection<Control>> ControlsByComponent =
			new Dictionary<PageComponent, IReadOnlyCollection<Control>>();

		private readonly Dictionary<Control, List<EtherealControl>> etherealControlsByControl = new Dictionary<Control, List<EtherealControl>>();
		private readonly Dictionary<string, PostBack> postBacksById = new Dictionary<string, PostBack>();
		private readonly List<FormValue> formValues = new List<FormValue>();
		private readonly Dictionary<string, ComponentStateItem> componentStateItemsById = new Dictionary<string, ComponentStateItem>();
		private readonly List<LegacyUpdateRegionLinker> updateRegionLinkers = new List<LegacyUpdateRegionLinker>();
		private readonly Dictionary<EwfValidation, List<string>> modErrorDisplaysByValidation = new Dictionary<EwfValidation, List<string>>();
		internal readonly HashSet<EwfValidation> ValidationsWithErrors = new HashSet<EwfValidation>();
		internal readonly Dictionary<Control, List<AutofocusCondition>> AutofocusConditionsByControl = new Dictionary<Control, List<AutofocusCondition>>();
		private readonly List<Action> controlTreeValidations = new List<Action>();
		internal PostBack SubmitButtonPostBack;
		private readonly List<Tuple<StatusMessageType, string>> statusMessages = new List<Tuple<StatusMessageType, string>>();

		/// <summary>
		/// Returns the entity setup for this page, if one exists.
		/// </summary>
		public abstract EntitySetupBase EsAsBaseType { get; }

		/// <summary>
		/// Gets the page info object for this page. Necessary so we can validate query parameters and ensure authenticated user can access the page.
		/// </summary>
		public abstract PageInfo InfoAsBaseType { get; }

		/// <summary>
		/// Gets the parameters modification object for this page. 
		/// </summary>
		public abstract ParametersModificationBase ParametersModificationAsBaseType { get; }

		/// <summary>
		/// Gets the page state for this page.
		/// </summary>
		public PageState PageState => AppRequestState.Instance.EwfPageRequestState.PageState;

		/// <summary>
		/// Creates a new page. Do not call this yourself.
		/// </summary>
		protected EwfPage() {
			// We suspect that this disables browser detection for the entire request, not just the page.
			ClientTarget = "uplevel";
		}

		/// <summary>
		/// Executes EWF logic in addition to the standard ASP.NET PreInit logic.
		/// </summary>
		protected sealed override void OnPreInit( EventArgs e ) {
			// This logic should happen before the page gets the PreInit event in case it wants to determine the master based on parameters.
			initEntitySetupAndCreateInfoObjects();

			base.OnPreInit( e );
		}

		private void initEntitySetupAndCreateInfoObjects() {
			AppRequestState.Instance.UserDisabledByPage = true;
			try {
				using( MiniProfiler.Current.Step( "EWF - Create page info" ) )
					createInfoFromQueryString();

				// If the request doesn't match the page's specified security level, redirect with the proper level. Do this before ensuring that the user can access the
				// page since in certificate authentication systems this can be affected by the connection security level.
				//
				// When goal 448 (Clean URLs) is complete, we want to do full URL normalization during request dispatching, like we do with shortcut URLs.
				bool connectionSecurityIncorrect;
				using( MiniProfiler.Current.Step( "EWF - Check connection security" ) )
					connectionSecurityIncorrect = getConnectionSecurityIncorrect();
				if( connectionSecurityIncorrect )
					NetTools.Redirect( InfoAsBaseType.GetUrl( false, false, true ) );
			}
			finally {
				AppRequestState.Instance.UserDisabledByPage = false;
			}

			// This logic depends on the authenticated user and on page and entity setup info objects.
			bool userCanAccessResource;
			using( MiniProfiler.Current.Step( "EWF - Check page authorization" ) )
				userCanAccessResource = InfoAsBaseType.UserCanAccessResource;
			if( !userCanAccessResource )
				throw new AccessDeniedException(
					ConfigurationStatics.IsIntermediateInstallation && !InfoAsBaseType.IsIntermediateInstallationPublicResource &&
					!AppRequestState.Instance.IntermediateUserExists,
					InfoAsBaseType.LogInPage );

			DisabledResourceMode disabledMode;
			using( MiniProfiler.Current.Step( "EWF - Check alternative page mode" ) )
				disabledMode = InfoAsBaseType.AlternativeMode as DisabledResourceMode;
			if( disabledMode != null )
				throw new PageDisabledException( disabledMode.Message );

			var cachedRequestHandler = requestHandler;
			if( cachedRequestHandler != null ) {
				Response.ClearHeaders();
				Response.ClearContent();
				cachedRequestHandler.WriteResponse();

				// Calling Response.End() is not a good practice; see http://stackoverflow.com/q/1087777/35349. We should be able to remove this call when we separate
				// EWF from Web Forms. This is EnduraCode goal 790.
				Response.End();
			}
		}

		/// <summary>
		/// Creates the info object for this page based on the query parameters of the request.
		/// </summary>
		protected abstract void createInfoFromQueryString();

		/// <summary>
		/// Gets the request handler for this page, which will override the page.
		/// </summary>
		protected virtual EwfSafeRequestHandler requestHandler => null;

		/// <summary>
		/// Performs EWF activities in addition to the normal InitComplete activities.
		/// </summary>
		protected sealed override void OnInitComplete( EventArgs e ) {
			base.OnInitComplete( e );
			if( IsPostBack )
				return;

			if( AppRequestState.Instance.EwfPageRequestState != null )
				PageState.ClearCustomStateControlKeys();
			else if( StandardLibrarySessionState.Instance.EwfPageRequestState != null ) {
				AppRequestState.Instance.EwfPageRequestState = StandardLibrarySessionState.Instance.EwfPageRequestState;
				StandardLibrarySessionState.Instance.EwfPageRequestState = null;
				PageState.ClearCustomStateControlKeys();
			}
			else
				AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( PageState.CreateForNewPage(), null, null );

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

				// Re-create info objects. A big reason to do this is that some info objects execute database queries or other code in order to prime the data-access
				// cache. The code above resets the cache and we want to re-prime it right away.
				AppRequestState.Instance.UserDisabledByPage = true;
				try {
					using( MiniProfiler.Current.Step( "EWF - Re-create page info after page-view data modifications" ) )
						reCreateInfo();

					bool connectionSecurityIncorrect;
					using( MiniProfiler.Current.Step( "EWF - Check connection security after page-view data modifications" ) )
						connectionSecurityIncorrect = getConnectionSecurityIncorrect();
					if( connectionSecurityIncorrect )
						throw getPossibleDeveloperMistakeException( "The connection security of the page changed after page-view data modifications." );
				}
				finally {
					AppRequestState.Instance.UserDisabledByPage = false;
				}
				bool userCanAccessResource;
				using( MiniProfiler.Current.Step( "EWF - Check page authorization after page-view data modifications" ) )
					userCanAccessResource = InfoAsBaseType.UserCanAccessResource;
				DisabledResourceMode disabledMode;
				using( MiniProfiler.Current.Step( "EWF - Check alternative page mode after page-view data modifications" ) )
					disabledMode = InfoAsBaseType.AlternativeMode as DisabledResourceMode;
				if( !userCanAccessResource || disabledMode != null )
					throw getPossibleDeveloperMistakeException( "The user lost access to the page or the page became disabled after page-view data modifications." );
			}

			onLoadData();

			if( requestState.StaticRegionContents != null ) {
				var updateRegionLinkersByKey = updateRegionLinkers.ToDictionary( i => i.Key );
				var updateRegionControls = requestState.UpdateRegionKeysAndArguments.SelectMany(
					keyAndArg => {
						if( !updateRegionLinkersByKey.TryGetValue( keyAndArg.Item1, out var linker ) )
							throw getPossibleDeveloperMistakeException( "An update region linker with the key \"{0}\" does not exist.".FormatWith( keyAndArg.Item1 ) );
						return linker.PostModificationRegionGetter( keyAndArg.Item2 );
					} );

				var staticRegionContents = getStaticRegionContents( updateRegionControls );
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
							requestState.SetStaticAndUpdateRegionState( getStaticRegionContents( new Control[ 0 ] ).contents, new Tuple<string, string>[ 0 ] );
						}
					} );
				if( navigationNeeded )
					navigate( null, null );
			}
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
		protected abstract void reCreateInfo();

		private bool getConnectionSecurityIncorrect() {
			return InfoAsBaseType.ShouldBeSecureGivenCurrentRequest != EwfApp.Instance.RequestIsSecure( Request );
		}

		/// <summary>
		/// We use this instead of LoadViewState because the latter doesn't get called during post backs on which the page structure changes.
		/// </summary>
		protected sealed override object LoadPageStateFromPersistenceMedium() {
			HiddenFieldData hiddenFieldData = null;
			try {
				// throws exception if field missing, because Request.Form returns null
				hiddenFieldData = JsonConvert.DeserializeObject<HiddenFieldData>(
					Request.Form[ hiddenFieldName ],
					new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error } );

				// Based on our implementation of SavePageStateToPersistenceMedium, the base implementation of LoadPageStateFromPersistenceMedium will return a Pair
				// with no First object.
				var pair = base.LoadPageStateFromPersistenceMedium() as Pair;

				AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState(
					PageState.CreateFromViewState( (object[])pair.Second ),
					Request.Form[ "__SCROLLPOSITIONX" ],
					Request.Form[ "__SCROLLPOSITIONY" ] );
				AppRequestState.Instance.EwfPageRequestState.ComponentStateValuesById = hiddenFieldData.ComponentStateValuesById;
			}
			catch {
				// Set a 400 status code if there are any problems loading hidden field state. We're assuming these problems are never the developers' fault.
				if( AppRequestState.Instance.EwfPageRequestState == null )
					AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( PageState.CreateForNewPage(), null, null );
				Response.StatusCode = 400;
				Response.TrySkipIisCustomErrors = true;
				AppRequestState.Instance.EwfPageRequestState.FocusKey = "";
				AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors =
					Translation.ApplicationHasBeenUpdatedAndWeCouldNotInterpretAction.ToCollection();
				resetPage();
			}

			onLoadData();

			var requestState = AppRequestState.Instance.EwfPageRequestState;
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
						var regionSets = new HashSet<UpdateRegionSet>( actionPostBack.UpdateRegions );
						var preModRegions = updateRegionLinkers.SelectMany(
								i => i.PreModificationRegions,
								( linker, region ) => new { region.Sets, region.ControlGetter, linker.Key, region.ArgumentGetter } )
							.Where( i => regionSets.Overlaps( i.Sets ) )
							.ToArray();
						var staticRegionContents = getStaticRegionContents( preModRegions.SelectMany( i => i.ControlGetter() ) );

						requestState.ComponentStateValuesById = componentStateItemsById.Where( i => staticRegionContents.stateItems.Contains( i.Value ) )
							.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson );
						requestState.PostBackValues.RemoveExcept( staticRegionContents.formValues.Select( i => i.GetPostBackValueKey() ) );
						requestState.DmIdAndSecondaryOp = Tuple.Create(
							actionPostBack.ValidationDm == dataUpdate ? "" : ( (ActionPostBack)actionPostBack.ValidationDm ).Id,
							actionPostBack.ValidationDm == lastPostBackFailingDm ? SecondaryPostBackOperation.Validate : SecondaryPostBackOperation.ValidateChangesOnly );
						requestState.SetStaticAndUpdateRegionState(
							staticRegionContents.contents,
							preModRegions.Select( i => Tuple.Create( i.Key, i.ArgumentGetter() ) ).ToArray() );
					}
					else {
						requestState.ComponentStateValuesById = null;
						requestState.PostBackValues = null;
					}
				} );

			navigate( redirectInfo, requestState.ModificationErrorsExist ? null : fullSecondaryResponse );
			return null;
		}

		/// <summary>
		/// This needs to be called after the page state dictionary has been created or restored.
		/// </summary>
		private void onLoadData() {
			// This can go anywhere in the lifecycle.

			addMetadataAndFaviconLinks();
			addTypekitLogicIfNecessary();
			Header.AddControlsReturnThis( from i in cssInfoCreator() select getStyleSheetLink( this.GetClientUrl( i.GetUrl( false, false, false ) ) ) );
			addModernizrLogic();
			addGoogleAnalyticsLogicIfNecessary();
			addJavaScriptIncludes();


			// Set the page title. This should be done before LoadData to support pages or entity setups that want to set their own title.
			Title = StringTools.ConcatenateWithDelimiter(
				" - ",
				EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName,
				ResourceInfo.CombineResourcePathStrings(
					ResourceInfo.ResourcePathSeparator,
					InfoAsBaseType.ParentResourceEntityPathString,
					InfoAsBaseType.ResourceFullName ) );

			Form.Controls.Add( etherealPlace = new PlaceHolder() );

			formState = new FormState();
			browsingModalBoxCreator().AddEtherealControls( Form );
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				DataUpdate.ToCollection(),
				() => {
					using( MiniProfiler.Current.Step( "EWF - Load page data" ) )
						loadData();
				} );
			Form.AddControlsReturnThis(
				new ElementNode(
						context => new ElementNodeData(
							hiddenFieldName,
							() => {
								var attributes = new List<Tuple<string, string>>();
								attributes.Add( Tuple.Create( "type", "hidden" ) );
								attributes.Add( Tuple.Create( "name", hiddenFieldName ) );

								var rs = AppRequestState.Instance.EwfPageRequestState;
								var failingDmId =
									rs.ModificationErrorsExist && rs.DmIdAndSecondaryOp != null && rs.DmIdAndSecondaryOp.Item2 != SecondaryPostBackOperation.ValidateChangesOnly
										? rs.DmIdAndSecondaryOp.Item1
										: null;

								attributes.Add(
									Tuple.Create(
										"value",
										JsonConvert.SerializeObject(
											new HiddenFieldData(
												componentStateItemsById.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson ),
												generateFormValueHash(),
												failingDmId,
												"" ),
											Formatting.None ) ) );

								return new ElementNodeLocalData( "input", new ElementNodeFocusDependentData( attributes, true, "" ) );
							} ) ).ToCollection()
					.GetControls() );
			using( MiniProfiler.Current.Step( "EWF - Load control data" ) )
				loadDataForControlAndChildren( this );
			formState = null;

			var activeStateItems = GetDescendants( this ).OfType<ElementNode>().SelectMany( i => i.StateItems ).ToImmutableHashSet();
			foreach( var i in componentStateItemsById.Where( i => !activeStateItems.Contains( i.Value ) ).Select( i => i.Key ).Materialize() )
				componentStateItemsById.Remove( i );

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

		private void addMetadataAndFaviconLinks() {
			Header.Controls.Add(
				new HtmlMeta
					{
						Name = "application-name", Content = EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName
					} );

			// Chrome start URL
			Header.Controls.Add( new HtmlMeta { Name = "application-url", Content = this.GetClientUrl( NetTools.HomeUrl ) } );

			// IE9 start URL
			Header.Controls.Add( new HtmlMeta { Name = "msapplication-starturl", Content = this.GetClientUrl( NetTools.HomeUrl ) } );

			var faviconPng48X48 = EwfApp.Instance.FaviconPng48X48;
			if( faviconPng48X48 != null && faviconPng48X48.UserCanAccessResource ) {
				var link = new HtmlLink { Href = this.GetClientUrl( faviconPng48X48.GetUrl( true, true, false ) ) };
				link.Attributes.Add( "rel", "icon" );
				link.Attributes.Add( "sizes", "48x48" );
				Header.Controls.Add( link );
			}

			var favicon = EwfApp.Instance.Favicon;
			if( favicon != null && favicon.UserCanAccessResource ) {
				var link = new HtmlLink { Href = this.GetClientUrl( favicon.GetUrl( true, true, false ) ) };
				link.Attributes.Add( "rel", "shortcut icon" ); // rel="shortcut icon" is deprecated and will be replaced with rel="icon".
				Header.Controls.Add( link );
			}

			Header.Controls.Add( new HtmlMeta { Name = "viewport", Content = "initial-scale=1" } );
		}

		private void addTypekitLogicIfNecessary() {
			if( EwfApp.Instance.TypekitId.Length > 0 ) {
				Header.Controls.Add(
					new Literal
						{
							Text = "<script type=\"text/javascript\" src=\"http" + ( EwfApp.Instance.RequestIsSecure( Request ) ? "s" : "" ) + "://use.typekit.com/" +
							       EwfApp.Instance.TypekitId + ".js\"></script>"
						} );
				Header.Controls.Add( new Literal { Text = "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" } );
			}
		}

		private Control getStyleSheetLink( string url ) {
			var l = new HtmlLink { Href = url };
			l.Attributes.Add( "rel", "stylesheet" );
			l.Attributes.Add( "type", "text/css" );
			return l;
		}

		private void addModernizrLogic() {
			Header.Controls.Add(
				new Literal
					{
						Text = "<script type=\"text/javascript\" src=\"" +
						       this.GetClientUrl( EwfApp.MetaLogicFactory.CreateModernizrJavaScriptInfo().GetUrl( false, false, false ) ) + "\"></script>"
					} );
		}

		private void addGoogleAnalyticsLogicIfNecessary() {
			if( EwfApp.Instance.GoogleAnalyticsWebPropertyId.Length == 0 )
				return;
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "<script>" );
				sw.WriteLine( "(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){" );
				sw.WriteLine( "(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o)," );
				sw.WriteLine( "m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)" );
				sw.WriteLine( "})(window,document,'script','//www.google-analytics.com/analytics.js','ga');" );

				var userId = EwfApp.Instance.GetGoogleAnalyticsUserId();
				sw.WriteLine(
					"ga('create', '" + EwfApp.Instance.GoogleAnalyticsWebPropertyId + "', 'auto'{0});",
					userId.Any() ? ", {{'userId': '{0}'}}".FormatWith( userId ) : "" );

				sw.WriteLine( "ga('send', 'pageview');" );
				sw.WriteLine( "</script>" );
				Header.Controls.Add( new Literal { Text = sw.ToString() } );
			}
		}

		private void addJavaScriptIncludes() {
			foreach( var url in from i in EwfApp.MetaLogicFactory.CreateJavaScriptInfos() select i.GetUrl( false, false, false ) )
				ClientScript.RegisterClientScriptInclude( GetType(), "ewf" + url, this.GetClientUrl( url ) );
			ClientScript.RegisterClientScriptBlock( GetType(), "stackExchangeMiniProfiler", MiniProfiler.RenderIncludes().ToHtmlString(), false );
			foreach( var url in from i in EwfApp.Instance.GetJavaScriptFiles() select i.GetUrl( false, false, false ) )
				ClientScript.RegisterClientScriptInclude( GetType(), "systemSpecificFile" + url, this.GetClientUrl( url ) );
		}

		/// <summary>
		/// Loads and displays data on the page. This is a replacement for the Init event that provides access to EWF page state.
		/// </summary>
		protected virtual void loadData() {}

		private void loadDataForControlAndChildren( Control control ) {
			elementOrIdentifiedComponentIdGetter = () => control.ClientID;

			if( control is ControlTreeDataLoader controlTreeDataLoader ) {
				FormState.Current.SetForNextElement();

				// This master-page hack will go away when EnduraCode goal 790 is complete. At that point master pages will be nothing more than components.
				if( control is MasterPage )
					FormState.ExecuteWithDataModificationsAndDefaultAction( DataUpdate.ToCollection(), controlTreeDataLoader.LoadData );
				else
					controlTreeDataLoader.LoadData();
			}

			foreach( var child in control.Controls.Cast<Control>().Where( i => i != etherealPlace ) )
				loadDataForControlAndChildren( child );

			if( !etherealControlsByControl.TryGetValue( control, out var etherealControls ) )
				etherealControls = new List<EtherealControl>();
			if( etherealControls.Any() ) {
				var np = new NamingPlaceholder(
					etherealControls.Select(
						i => {
							// This is kind of a hack, but it's an easy way to make sure ethereal controls are initially hidden.
							i.Control.Style.Add( HtmlTextWriterStyle.Display, "none" );

							return i.Control;
						} ) ) { ID = "ethereal{0}".FormatWith( control.UniqueID.Replace( "$", "" ) ) };
				etherealPlace.AddControlsReturnThis( np );
				( (ControlTreeDataLoader)np ).LoadData();
			}
			foreach( var child in etherealControls )
				loadDataForControlAndChildren( child.Control );
		}

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

		/// <summary>
		/// Gets whether the page forces a post-back when a link is clicked.
		/// </summary>
		public virtual bool IsAutoDataUpdater => false;

		internal void AddEtherealControl( Control parent, EtherealControl etherealControl ) {
			if( !etherealControlsByControl.TryGetValue( parent, out var etherealControls ) ) {
				etherealControls = new List<EtherealControl>();
				etherealControlsByControl.Add( parent, etherealControls );
			}
			etherealControls.Add( etherealControl );
		}

		internal PostBack GetPostBack( string id ) => postBacksById.TryGetValue( id, out var value ) ? value : null;

		internal void AddUpdateRegionLinker( LegacyUpdateRegionLinker linker ) {
			updateRegionLinkers.Add( linker );
		}

		/// <summary>
		/// If you are using the results of this method to create controls, put them in a naming container so that when the controls differ before and after a
		/// transfer, other parts of the page such as form control IDs do not get affected.
		/// </summary>
		internal IEnumerable<string> AddModificationErrorDisplayAndGetErrors( Control control, string keySuffix, EwfValidation validation ) {
			var key = control.UniqueID + keySuffix;
			if( modErrorDisplaysByValidation.ContainsKey( validation ) )
				modErrorDisplaysByValidation[ validation ].Add( key );
			else
				modErrorDisplaysByValidation.Add( validation, key.ToCollection().ToList() );

			// We want to ignore all of the problems that could happen, such as the key not existing in the dictionary. This problem will be shown in a more helpful
			// way when we compare form control hashes after a transfer.
			//
			// Avoid using exceptions here if possible. This method is sometimes called many times during a request, and we've seen exceptions take as long as 50 ms
			// each when debugging.
			return AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay.TryGetValue( key, out var value ) ? value : new string[ 0 ];
		}

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
				AppRequestState.Instance.EwfPageRequestState.SetStaticAndUpdateRegionState(
					getStaticRegionContents( new Control[ 0 ] ).contents,
					new Tuple<string, string>[ 0 ] );
			}
		}

		private void validateFormSubmission( string formValueHash ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

			var webFormsHiddenFields = new[]
				{
					"__EVENTTARGET", "__EVENTARGUMENT", "__LASTFOCUS", "__VIEWSTATE", "__SCROLLPOSITIONX", "__SCROLLPOSITIONY", "__VIEWSTATEGENERATOR"
				};
			var activeFormValues = formValues.Where( i => i.GetPostBackValueKey().Any() ).ToArray();
			var postBackValueKeys = new HashSet<string>( activeFormValues.Select( i => i.GetPostBackValueKey() ) );
			requestState.PostBackValues = new PostBackValueDictionary();
			var extraPostBackValuesExist = requestState.ComponentStateValuesById.Keys.Any( i => !componentStateItemsById.ContainsKey( i ) ) |
			                               requestState.PostBackValues.AddFromRequest(
				                               Request.Form.Cast<string>().Except( new[] { hiddenFieldName, ButtonElementName }.Concat( webFormsHiddenFields ) ),
				                               postBackValueKeys.Contains,
				                               key => Request.Form[ key ] ) | requestState.PostBackValues.AddFromRequest(
				                               Request.Files.Cast<string>(),
				                               postBackValueKeys.Contains,
				                               key => Request.Files[ key ] );

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
			IEnumerable<Control> updateRegionControls ) {
			var contents = new StringBuilder();

			updateRegionControls = new HashSet<Control>( updateRegionControls );
			var staticDescendants = GetDescendants( this, predicate: i => !updateRegionControls.Contains( i ) ).OfType<ElementNode>().Materialize();
			var staticStateItems = staticDescendants.SelectMany( i => i.StateItems ).ToImmutableHashSet();
			var staticFormValues = staticDescendants.Select( i => i.FormValue )
				.Where( i => i != null )
				.Distinct()
				.OrderBy( i => i.GetPostBackValueKey() )
				.Materialize();

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

		private void navigate( ResourceInfo destination, FullResponse secondaryResponse ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

			string destinationUrl;
			try {
				// Determine the final redirect destination. If a destination is already specified and it is the current page or a page with the same entity setup,
				// replace any default optional parameter values it may have with new values from this post back. If a destination isn't specified, make it the current
				// page with new parameter values from this post back. At the end of this block, redirectInfo is always newly created with fresh data that reflects any
				// changes that may have occurred in EH methods. It's important that every case below *actually creates* a new page info object to guard against this
				// scenario:
				// 1. A page modifies data such that a previously created redirect destination page info object that is then used here is no longer valid because it
				//    would throw an exception from init if it were re-created.
				// 2. The page redirects, or transfers, to this destination, leading the user to an error page without developers being notified. This is bad behavior.
				if( requestState.ModificationErrorsExist ||
				    ( requestState.DmIdAndSecondaryOp != null && requestState.DmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.NoOperation ) )
					destination = InfoAsBaseType.CloneAndReplaceDefaultsIfPossible( true );
				else if( destination != null )
					destination = destination.CloneAndReplaceDefaultsIfPossible( false );
				else
					destination = reCreateFromNewParameterValues();

				// This GetUrl call is important even for the transfer case below for the same reason that we *actually create* a new page info object in every case
				// above. We want to force developers to get an error email if a page modifies data to make itself unauthorized/disabled without specifying a different
				// page as the redirect destination. The resulting transfer would lead the user to an error page.
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
			if( destination.IsIdenticalToCurrent() ) {
				AppRequestState.Instance.ClearUserAndImpersonator();
				resetPage();
			}

			// If the redirect destination is the current page, but with different query parameters, save request state in session state until the next request.
			if( destination.GetType() == InfoAsBaseType.GetType() )
				StandardLibrarySessionState.Instance.EwfPageRequestState = requestState;

			// When we separate EWF from Web Forms, we want this to become an HTTP 303 redirect, if it isn�t already.
			NetTools.Redirect( destinationUrl );
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

		private void resetPage() {
			Server.Transfer( Request.AppRelativeCurrentExecutionFilePath );
		}

		protected sealed override void OnPreRender( EventArgs eventArgs ) {
			base.OnPreRender( eventArgs );

			foreach( var i in GetDescendants( this ) )
				( i as ElementNode )?.InitLocalData();

			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var modificationErrorsOccurred = requestState.ModificationErrorsExist &&
			                                 ( requestState.DmIdAndSecondaryOp == null ||
			                                   !new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains(
				                                   requestState.DmIdAndSecondaryOp.Item2 ) );

			Func<FocusabilityCondition, bool> isFocusablePredicate;
			if( modificationErrorsOccurred )
				isFocusablePredicate = condition => condition.ErrorFocusabilitySources.Validations.Any( i => ValidationsWithErrors.Contains( i ) ) ||
				                                    ( condition.ErrorFocusabilitySources.IncludeGeneralErrors &&
				                                      AppRequestState.Instance.EwfPageRequestState.GeneralModificationErrors.Any() );
			else
				isFocusablePredicate = condition => condition.IsNormallyFocusable;

			var autofocusInfo = getAutofocusInfo( this, modificationErrorsOccurred, isFocusablePredicate );
			if( !modificationErrorsOccurred && autofocusInfo.activeRegionsExist && autofocusInfo.focusedElement == null )
				throw new ApplicationException( "The active autofocus regions do not contain any focusable elements." );

			addJavaScriptStartUpLogic( autofocusInfo.focusedElement );


			// Direct response object modifications. These should happen once per page view; they are not needed in redirect responses.

			FormsAuthStatics.UpdateFormsAuthCookieIfNecessary();

			if( !ConfigurationStatics.IsLiveInstallation )
				Response.AppendHeader( "X-Robots-Tag", "noindex, nofollow" );
			else if( !InfoAsBaseType.AllowsSearchEngineIndexing )
				Response.AppendHeader( "X-Robots-Tag", "noindex" );

			// Without this header, certain sites could be forced into compatibility mode due to the Compatibility View Blacklist maintained by Microsoft.
			Response.AppendHeader( "X-UA-Compatible", "IE=edge" );


			StandardLibrarySessionState.Instance.StatusMessages.Clear();
			StandardLibrarySessionState.Instance.ClearClientSideNavigation();
		}

		private ( bool activeRegionsExist, ElementNode focusedElement ) getAutofocusInfo(
			Control control, bool inActiveRegion, Func<FocusabilityCondition, bool> isFocusablePredicate ) {
			if( !inActiveRegion && AutofocusConditionsByControl.TryGetValue( control, out var conditions ) )
				inActiveRegion = conditions.Any( i => i.IsTrue( AppRequestState.Instance.EwfPageRequestState.FocusKey ) );

			if( inActiveRegion && control is ElementNode element && isFocusablePredicate( element.FocusabilityCondition ) )
				return ( true, element );

			if( !etherealControlsByControl.TryGetValue( control, out var etherealControls ) )
				etherealControls = new List<EtherealControl>();
			var autofocusInfo = ( activeRegionsExist: inActiveRegion, focusedElement: (ElementNode)null );
			foreach( var child in control.Controls.Cast<Control>().Where( i => i != etherealPlace ).Concat( from i in etherealControls select i.Control ) ) {
				var childInfo = getAutofocusInfo( child, inActiveRegion, isFocusablePredicate );
				autofocusInfo.activeRegionsExist = autofocusInfo.activeRegionsExist || childInfo.activeRegionsExist;
				autofocusInfo.focusedElement = childInfo.focusedElement;
				if( autofocusInfo.focusedElement != null )
					break;
			}

			return autofocusInfo;
		}

		private void addJavaScriptStartUpLogic( ElementNode focusedElement ) {
			focusedElement?.SetIsFocused();
			var controlInitStatements = getDescendants( this, i => true )
				.Where( i => i.Item2 != null )
				.Select( i => i.Item2() )
				.Aggregate( new StringBuilder(), ( builder, statements ) => builder.Append( statements ), i => i.ToString() );

			MaintainScrollPositionOnPostBack = true;
			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var scroll = scrollPositionForThisResponse == ScrollPosition.LastPositionOrStatusBar &&
			             ( !requestState.ModificationErrorsExist || ( requestState.DmIdAndSecondaryOp != null &&
			                                                          new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }
				                                                          .Contains( requestState.DmIdAndSecondaryOp.Item2 ) ) );

			// If a transfer happened on this request and we're on the same page and we want to scroll, get coordinates from the per-request data in EwfApp.
			var scrollStatement = "";
			if( scroll && requestState.ScrollPositionX != null && requestState.ScrollPositionY != null )
				scrollStatement = "window.scrollTo(" + requestState.ScrollPositionX + "," + requestState.ScrollPositionY + ");";

			// If the page has requested a client-side redirect, configure it now. The JavaScript solution is preferred over a meta tag since apparently it doesn't
			// cause reload behavior by the browser. See http://josephsmarr.com/2007/06/06/the-hidden-cost-of-meta-refresh-tags.
			StandardLibrarySessionState.Instance.GetClientSideNavigationSetup(
				out var clientSideNavigationUrl,
				out var clientSideNavigationInNewWindow,
				out var clientSideNavigationDelay );
			var clientSideNavigationStatements = "";
			if( clientSideNavigationUrl.Any() ) {
				var url = this.GetClientUrl( clientSideNavigationUrl );
				if( clientSideNavigationInNewWindow )
					clientSideNavigationStatements = "var newWindow = window.open( '{0}', '{1}' ); newWindow.focus();".FormatWith( url, "_blank" );
				else
					clientSideNavigationStatements = "location.replace( '" + url + "' );";
				if( clientSideNavigationDelay.HasValue )
					clientSideNavigationStatements = "setTimeout( \"" + clientSideNavigationStatements + "\", " + clientSideNavigationDelay.Value * 1000 + " );";
			}

			ClientScript.RegisterClientScriptBlock(
				GetType(),
				"jQueryDocumentReadyBlock",
				"$( document ).ready( function() { " + StringTools.ConcatenateWithDelimiter(
					" ",
					"OnDocumentReady();",
					"$( '#aspnetForm' ).submit( function( e, postBackId ) {{ postBackRequestStarting( e, postBackId !== undefined ? postBackId : '{0}' ); }} );"
						.FormatWith(
							SubmitButtonPostBack != null
								? SubmitButtonPostBack.Id
								: "" /* This empty string we're using when no submit button exists is arbitrary and meaningless; it should never actually be submitted. */ ),
					controlInitStatements,
					EwfApp.Instance.JavaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					StringTools.ConcatenateWithDelimiter( " ", scrollStatement, clientSideNavigationStatements )
						.PrependDelimiter( "window.onload = function() { " )
						.AppendDelimiter( " };" ) ) + " } );",
				true );
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public IEnumerable<Control> GetDescendants( Control control, Func<Control, bool> predicate = null ) {
			return from i in getDescendants( control, predicate ?? ( i => true ) ) select i.Item1;
		}

		private IEnumerable<Tuple<Control, Func<string>>> getDescendants( Control control, Func<Control, bool> predicate ) {
			var normalChildren = from i in control.Controls.Cast<Control>()
			                     where i != etherealPlace
			                     let jsControl = i as ControlWithJsInitLogic
			                     select Tuple.Create( i, jsControl != null ? new Func<string>( jsControl.GetJsInitStatements ) : null );

			if( !etherealControlsByControl.TryGetValue( control, out var etherealControls ) )
				etherealControls = new List<EtherealControl>();
			var etherealChildren = etherealControls.Select( i => Tuple.Create( (Control)i.Control, new Func<string>( i.GetJsInitStatements ) ) );

			var descendants = new List<Tuple<Control, Func<string>>>();
			foreach( var child in normalChildren.Concat( etherealChildren ).Where( i => predicate( i.Item1 ) ) ) {
				descendants.Add( child );
				descendants.AddRange( getDescendants( child.Item1, predicate ) );
			}
			return descendants;
		}

		/// <summary>
		/// The desired scroll position of the browser when this response is received.
		/// </summary>
		protected virtual ScrollPosition scrollPositionForThisResponse => ScrollPosition.LastPositionOrStatusBar;

		/// <summary>
		/// Gets the function call that should be executed when the jQuery document ready event is fired.
		/// </summary>
		protected virtual string javaScriptDocumentReadyFunctionCall => "";

		/// <summary>
		/// Saves view state.
		/// </summary>
		protected sealed override object SaveViewState() {
			// This is the last possible place in the life cycle this could go; view state is saved right after this.
			foreach( Control child in Controls )
				child.EnableViewState = false;

			return base.SaveViewState();
		}

		protected sealed override void SavePageStateToPersistenceMedium( object state ) {
			base.SavePageStateToPersistenceMedium( PageState.GetViewStateArray() );
		}
	}
}