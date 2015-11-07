using System;
using System.Collections.Generic;
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
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayLinking;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A System.Web.UI.Page that contains special Red Stapler Enterprise Web Framework logic. Requires that view state and session state be enabled.
	/// </summary>
	public abstract class EwfPage: Page {
		// This string is duplicated in the JavaScript file.
		private const string postBackHiddenFieldName = "ewfPostBack";

		internal const string ButtonElementName = "ewfButton";

		private static Func<IEnumerable<ResourceInfo>> cssInfoCreator;

		internal new static void Init( Func<IEnumerable<ResourceInfo>> cssInfoCreator ) {
			EwfPage.cssInfoCreator = cssInfoCreator;
		}

		/// <summary>
		/// Returns the currently executing EwfPage, or null if the currently executing page is not an EwfPage.
		/// </summary>
		public static EwfPage Instance { get { return HttpContext.Current.CurrentHandler as EwfPage; } }

		/// <summary>
		/// Add a status message of the given type to the status message collection. Message is not HTML-encoded. It is possible to have
		/// tags as part of the text.
		/// </summary>
		public static void AddStatusMessage( StatusMessageType type, string messageHtml ) {
			Instance.statusMessages.Add( new Tuple<StatusMessageType, string>( type, messageHtml ) );
		}

		private Control contentContainer;
		private Control etherealPlace;
		private readonly BasicDataModification dataUpdate = new BasicDataModification();
		private readonly PostBack dataUpdatePostBack = PostBack.CreateDataUpdate();
		private readonly Dictionary<Control, List<EtherealControl>> etherealControlsByControl = new Dictionary<Control, List<EtherealControl>>();
		private readonly Dictionary<string, PostBack> postBacksById = new Dictionary<string, PostBack>();
		private readonly List<FormValue> formValues = new List<FormValue>();
		private readonly List<DisplayLink> displayLinks = new List<DisplayLink>();
		private readonly List<UpdateRegionLinker> updateRegionLinkers = new List<UpdateRegionLinker>();
		private readonly Dictionary<EwfValidation, List<string>> modErrorDisplaysByValidation = new Dictionary<EwfValidation, List<string>>();
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
		public PageState PageState { get { return AppRequestState.Instance.EwfPageRequestState.PageState; } }

		/// <summary>
		/// Creates a new page. Do not call this yourself.
		/// </summary>
		protected EwfPage() {
			// Use the entire page as the default content container.
			contentContainer = this;

			// We suspect that this disables browser detection for the entire request, not just the page.
			ClientTarget = "uplevel";
		}

		/// <summary>
		/// Executes EWF logic in addition to the standard ASP.NET PreInit logic.
		/// </summary>
		protected override sealed void OnPreInit( EventArgs e ) {
			// This logic should happen before the page gets the PreInit event in case it wants to determine the master based on parameters.
			// NOTE: If the entity setup is a master page, we need to delay this call until after PreInit.
			initEntitySetupAndCreateInfoObjects();

			base.OnPreInit( e );
		}

		private void initEntitySetupAndCreateInfoObjects() {
			AppRequestState.Instance.UserDisabledByPage = true;
			try {
				initEntitySetup();
				if( EsAsBaseType != null )
					EsAsBaseType.CreateInfoFromQueryString();
				createInfoFromQueryString();

				// If the request doesn't match the page's specified security level, redirect with the proper level. Do this before ensuring that the user can access the
				// page since in certificate authentication systems this can be affected by the connection security level.
				//
				// When goal 448 (Clean URLs) is complete, we want to do full URL normalization during request dispatching, like we do with shortcut URLs. We probably
				// should only do this on GET requests since we don't want to wipe out post backs.
				if( connectionSecurityIncorrect )
					NetTools.Redirect( InfoAsBaseType.GetUrl( false, false, true ) );
			}
			finally {
				AppRequestState.Instance.UserDisabledByPage = false;
			}

			// This logic depends on the authenticated user and on page and entity setup info objects.
			if( !InfoAsBaseType.UserCanAccessResource ) {
				throw new AccessDeniedException(
					ConfigurationStatics.IsIntermediateInstallation && !InfoAsBaseType.IsIntermediateInstallationPublicResource &&
					!AppRequestState.Instance.IntermediateUserExists,
					InfoAsBaseType.LogInPage );
			}

			var disabledMode = InfoAsBaseType.AlternativeMode as DisabledResourceMode;
			if( disabledMode != null )
				throw new PageDisabledException( disabledMode.Message );

			if( responseWriter != null ) {
				Response.ClearHeaders();
				Response.ClearContent();
				responseWriter.WriteResponse();

				// Calling Response.End() is not a good practice; see http://stackoverflow.com/q/1087777/35349. We should be able to remove this call when we separate
				// EWF from Web Forms. This is EnduraCode goal 790.
				Response.End();
			}
		}

		/// <summary>
		/// Loads the entity display setup for the page, if one exists.
		/// </summary>
		protected abstract void initEntitySetup();

		/// <summary>
		/// Gets the response writer for this page. NOTE: We should re-implement this such that the classes that override this are plain old HTTP handlers instead of pages.
		/// </summary>
		protected virtual EwfSafeResponseWriter responseWriter { get { return null; } }

		/// <summary>
		/// Performs EWF activities in addition to the normal InitComplete activities.
		/// </summary>
		protected override sealed void OnInitComplete( EventArgs e ) {
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
			if( requestState.StaticRegionContents == null ||
			    ( !requestState.ModificationErrorsExist && dmIdAndSecondaryOp != null &&
			      new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains( dmIdAndSecondaryOp.Item2 ) ) ) {
				var modMethods = new List<Action>();
				if( !ConfigurationStatics.MachineIsStandbyServer ) {
					modMethods.Add( EwfApp.Instance.GetPageViewDataModificationMethod() );
					if( AppRequestState.Instance.UserAccessible ) {
						if( AppTools.User != null )
							modMethods.Add( getLastPageRequestTimeUpdateMethod( AppTools.User ) );
						if( AppRequestState.Instance.ImpersonatorExists && AppRequestState.Instance.ImpersonatorUser != null )
							modMethods.Add( getLastPageRequestTimeUpdateMethod( AppRequestState.Instance.ImpersonatorUser ) );
					}
					modMethods.Add( getPageViewDataModificationMethod() );
				}
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
				if( EsAsBaseType != null )
					EsAsBaseType.ClearInfo();
				clearInfo();
				AppRequestState.Instance.UserDisabledByPage = true;
				try {
					if( EsAsBaseType != null )
						EsAsBaseType.CreateInfoFromQueryString();
					createInfoFromQueryString();
					if( connectionSecurityIncorrect )
						throw getPossibleDeveloperMistakeException( "The connection security of the page changed after page-view data modifications." );
				}
				finally {
					AppRequestState.Instance.UserDisabledByPage = false;
				}
				if( !InfoAsBaseType.UserCanAccessResource || InfoAsBaseType.AlternativeMode is DisabledResourceMode )
					throw getPossibleDeveloperMistakeException( "The user lost access to the page or the page became disabled after page-view data modifications." );
			}

			onLoadData();

			if( requestState.StaticRegionContents != null ) {
				var updateRegionLinkersByKey = updateRegionLinkers.ToDictionary( i => i.Key );
				var updateRegionControls = requestState.UpdateRegionKeysAndArguments.SelectMany(
					keyAndArg => {
						UpdateRegionLinker linker;
						if( !updateRegionLinkersByKey.TryGetValue( keyAndArg.Item1, out linker ) )
							throw getPossibleDeveloperMistakeException( "An update region linker with the key \"{0}\" does not exist.".FormatWith( keyAndArg.Item1 ) );
						return linker.PostModificationRegionGetter( keyAndArg.Item2 );
					} );

				var staticRegionContents = getStaticRegionContents( updateRegionControls );
				if( staticRegionContents.Item1 != requestState.StaticRegionContents ||
				    formValues.Any( i => i.GetPostBackValueKey().Any() && i.PostBackValueIsInvalid( requestState.PostBackValues ) ) ) {
					throw getPossibleDeveloperMistakeException(
						requestState.ModificationErrorsExist
							? "Form controls, modification-error-display keys, and post-back IDs may not change if modification errors exist." +
							  " (IMPORTANT: This exception may have been thrown because EWL Goal 588 hasn't been completed. See the note in the goal about the EwfPage bug and disregard the rest of this error message.)"
							: new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains( dmIdAndSecondaryOp.Item2 )
								  ? "Form controls outside of update regions may not change on an intermediate post-back."
								  : "Form controls and post-back IDs may not change during the validation stage of an intermediate post-back." );
				}
			}

			if( !requestState.ModificationErrorsExist && dmIdAndSecondaryOp != null && dmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.Validate ) {
				var secondaryDm = dmIdAndSecondaryOp.Item1.Any() ? GetPostBack( dmIdAndSecondaryOp.Item1 ) as DataModification : dataUpdate;
				if( secondaryDm == null )
					throw getPossibleDeveloperMistakeException( "A data modification with an ID of \"{0}\" does not exist.".FormatWith( dmIdAndSecondaryOp.Item1 ) );

				var navigationNeeded = true;
				executeWithDataModificationExceptionHandling(
					() => {
						if( secondaryDm == dataUpdate ) {
							navigationNeeded = dataUpdate.Execute(
								true,
								formValues.Any( i => i.ValueChangedOnPostBack( requestState.PostBackValues ) ),
								handleValidationErrors,
								performValidationOnly: true );
						}
						else {
							var formValuesChanged =
								GetDescendants( contentContainer )
									.OfType<FormControl>()
									.Any( i => i.FormValue != null && i.FormValue.ValueChangedOnPostBack( requestState.PostBackValues ) );
							navigationNeeded = ( (ActionPostBack)secondaryDm ).Execute( formValuesChanged, handleValidationErrors, null );
						}

						if( navigationNeeded ) {
							requestState.DmIdAndSecondaryOp = Tuple.Create( dmIdAndSecondaryOp.Item1, SecondaryPostBackOperation.NoOperation );
							requestState.SetStaticAndUpdateRegionState( getStaticRegionContents( new Control[ 0 ] ).Item1, new Tuple<string, string>[ 0 ] );
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
			if( ( DateTime.Now - user.LastRequestDateTime ) < TimeSpan.FromMinutes( 60 ) )
				return null;

			// Now we want to do a timestamp-based concurrency check so we don't update the last login date if we know another transaction already did.
			// It is not perfect, but it reduces errors caused by one user doing a long-running request and then doing smaller requests
			// in another browser window while the first one is still running.
			// We have to query in a separate transaction because otherwise snapshot isolation will result in us always getting the original LastRequestDatetime, even if
			// another transaction has modified its value during this transaction.
			var newlyQueriedUser = new DataAccessState().ExecuteWithThis(
				() => {
					Func<User> userGetter = () => UserManagementStatics.GetUser( user.UserId, false );
					return ConfigurationStatics.DatabaseExists ? DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen( userGetter ) : userGetter();
				} );
			if( newlyQueriedUser == null || newlyQueriedUser.LastRequestDateTime > user.LastRequestDateTime )
				return null;

			return () => {
				Action userUpdater = () => {
					var externalAuthProvider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
					if( FormsAuthStatics.FormsAuthEnabled ) {
						var formsAuthCapableUser = (FormsAuthCapableUser)user;
						FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
							user.UserId,
							user.Email,
							user.Role.RoleId,
							DateTime.Now,
							formsAuthCapableUser.Salt,
							formsAuthCapableUser.SaltedPassword,
							formsAuthCapableUser.MustChangePassword );
					}
					else if( externalAuthProvider != null )
						externalAuthProvider.InsertOrUpdateUser( user.UserId, user.Email, user.Role.RoleId, DateTime.Now );
				};
				if( ConfigurationStatics.DatabaseExists ) {
					DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction(
						() => {
							try {
								userUpdater();
							}
							catch( DbConcurrencyException ) {
								// Since this method is called on every page request, concurrency errors are common. They are caused when an authenticated user makes one request
								// and then makes another before ASP.NET has finished processing the first. Since we are only updating the last request date and time, we don't
								// need to get an error email if the update fails.
								throw new DoNotCommitException();
							}
						} );
				}
				else
					userUpdater();
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
		protected abstract void clearInfo();

		/// <summary>
		/// Creates the info object for this page based on the query parameters of the request.
		/// </summary>
		protected abstract void createInfoFromQueryString();

		private bool connectionSecurityIncorrect { get { return InfoAsBaseType.ShouldBeSecureGivenCurrentRequest != EwfApp.Instance.RequestIsSecure( Request ); } }

		/// <summary>
		/// Loads hidden field state. We use this instead of LoadViewState because the latter doesn't get called during post backs on which the page structure
		/// changes.
		/// </summary>
		protected override sealed object LoadPageStateFromPersistenceMedium() {
			string formValueHash = null;
			string lastPostBackFailingDmId = null;
			try {
				// Based on our implementation of SavePageStateToPersistenceMedium, the base implementation of LoadPageStateFromPersistenceMedium will return a Pair
				// with no First object.
				var pair = base.LoadPageStateFromPersistenceMedium() as Pair;

				var savedState = PageState.CreateFromViewState( (object[])pair.Second );
				AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState(
					savedState.Item1,
					Request.Form[ "__SCROLLPOSITIONX" ],
					Request.Form[ "__SCROLLPOSITIONY" ] );
				formValueHash = (string)savedState.Item2[ 0 ];
				lastPostBackFailingDmId = (string)savedState.Item2[ 1 ];
			}
			catch {
				// Set a 400 status code if there are any problems loading hidden field state. We're assuming these problems are never the developers' fault.
				if( AppRequestState.Instance.EwfPageRequestState == null )
					AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( PageState.CreateForNewPage(), null, null );
				Response.StatusCode = 400;
				Response.TrySkipIisCustomErrors = true;
				AppRequestState.Instance.EwfPageRequestState.TopModificationErrors =
					Translation.ApplicationHasBeenUpdatedAndWeCouldNotInterpretAction.ToSingleElementArray();
				resetPage();
			}

			onLoadData();

			var requestState = AppRequestState.Instance.EwfPageRequestState;
			ResourceInfo redirectInfo = null;
			FullResponse fullSecondaryResponse = null;
			executeWithDataModificationExceptionHandling(
				() => {
					validateFormSubmission( formValueHash );

					// Get the post-back object and, if necessary, the last post-back's failing data modification.
					var postBackId = Request.Form[ postBackHiddenFieldName ]; // returns null if field missing
					var postBack = postBackId != null ? GetPostBack( postBackId ) : null;
					if( postBack == null )
						throw new DataModificationException( Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction );
					var lastPostBackFailingDm = postBack.IsIntermediate && lastPostBackFailingDmId != null
						                            ? lastPostBackFailingDmId.Any() ? GetPostBack( lastPostBackFailingDmId ) as DataModification : dataUpdate
						                            : null;
					if( postBack.IsIntermediate && lastPostBackFailingDmId != null && lastPostBackFailingDm == null )
						throw new DataModificationException( Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction );

					// Execute the page's data update.
					var dmExecuted = false;
					if( !postBack.IsIntermediate ) {
						try {
							dmExecuted |= dataUpdate.Execute(
								!postBack.ForcePageDataUpdate,
								formValues.Any( i => i.ValueChangedOnPostBack( requestState.PostBackValues ) ),
								handleValidationErrors );
						}
						catch {
							AppRequestState.Instance.EwfPageRequestState.DmIdAndSecondaryOp = Tuple.Create( "", SecondaryPostBackOperation.NoOperation );
							throw;
						}
					}

					// Execute the post-back.
					var actionPostBack = postBack as ActionPostBack;
					if( actionPostBack != null ) {
						var formValuesChanged =
							GetDescendants( contentContainer )
								.OfType<FormControl>()
								.Any( i => i.FormValue != null && i.FormValue.ValueChangedOnPostBack( requestState.PostBackValues ) );
						try {
							dmExecuted |= actionPostBack.Execute(
								formValuesChanged,
								handleValidationErrors,
								postBackAction => {
									if( postBackAction == null )
										return;
									redirectInfo = postBackAction.Resource;
									if( postBackAction.SecondaryResponse != null )
										fullSecondaryResponse = postBackAction.SecondaryResponse.GetFullResponse();
								} );
						}
						catch {
							AppRequestState.Instance.EwfPageRequestState.DmIdAndSecondaryOp = Tuple.Create( actionPostBack.Id, SecondaryPostBackOperation.NoOperation );
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
						var preModRegions =
							updateRegionLinkers.SelectMany(
								i => i.PreModificationRegions,
								( linker, region ) => new { region.Set, region.ControlGetter, linker.Key, region.ArgumentGetter } ).Where( i => regionSets.Contains( i.Set ) ).ToArray();
						var staticRegionContents = getStaticRegionContents( preModRegions.SelectMany( i => i.ControlGetter() ) );

						requestState.PostBackValues.RemoveExcept( staticRegionContents.Item2.Select( i => i.GetPostBackValueKey() ) );
						requestState.DmIdAndSecondaryOp = Tuple.Create(
							actionPostBack.ValidationDm == dataUpdate ? "" : ( (ActionPostBack)actionPostBack.ValidationDm ).Id,
							actionPostBack.ValidationDm == lastPostBackFailingDm ? SecondaryPostBackOperation.Validate : SecondaryPostBackOperation.ValidateChangesOnly );
						requestState.SetStaticAndUpdateRegionState( staticRegionContents.Item1, preModRegions.Select( i => Tuple.Create( i.Key, i.ArgumentGetter() ) ).ToArray() );
					}
					else
						requestState.PostBackValues = null;
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
			if( EsAsBaseType != null ) {
				using( MiniProfiler.Current.Step( "EWF - Load entity-setup data" ) )
					EsAsBaseType.LoadData();
			}
			using( MiniProfiler.Current.Step( "EWF - Load page data" ) )
				loadData();
			using( MiniProfiler.Current.Step( "EWF - Load control data" ) )
				loadDataForControlAndChildren( this );

			foreach( var i in controlTreeValidations )
				i();

			var duplicatePostBackValueKeys = formValues.Select( i => i.GetPostBackValueKey() ).Where( i => i.Any() ).GetDuplicates().ToArray();
			if( duplicatePostBackValueKeys.Any() )
				throw new ApplicationException( "Duplicate post-back-value keys exist: " + StringTools.ConcatenateWithDelimiter( ", ", duplicatePostBackValueKeys ) + "." );

			// Using this approach of initializing the hidden field to the submit button's post-back gives the enter key good behavior with Internet Explorer when
			// there is one text box on the page.
			// The empty string we're using when no submit button exists is arbitrary and meaningless; it should never actually be submitted.
			ClientScript.RegisterHiddenField( postBackHiddenFieldName, SubmitButtonPostBack != null ? SubmitButtonPostBack.Id : "" );

			// Set the initial client-side display state of all controls involved in display linking. This step will most likely be eliminated or undergo major
			// changes when we move EWF away from the Web Forms control model, so we haven't put much thought into exactly where it should go, but it should probably
			// happen after LoadData is called on all controls.
			foreach( var displayLink in displayLinks )
				displayLink.SetInitialDisplay( AppRequestState.Instance.EwfPageRequestState.PostBackValues );

			// Add inter-element JavaScript. This must be done after LoadData is called on all controls so that all controls have IDs.
			foreach( var displayLink in displayLinks )
				displayLink.AddJavaScript();

			// This must be after LoadData is called on all controls since certain logic, e.g. setting the focused control, can depend on the results of LoadData.
			addJavaScriptStartUpLogic();

			// This must happen after LoadData and before modifications are executed.
			statusMessages.Clear();
		}

		private void addMetadataAndFaviconLinks() {
			Header.Controls.Add(
				new HtmlMeta
					{
						Name = "application-name",
						Content = EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName
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
							Text =
								"<script type=\"text/javascript\" src=\"http" + ( EwfApp.Instance.RequestIsSecure( Request ) ? "s" : "" ) + "://use.typekit.com/" +
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
						Text =
							"<script type=\"text/javascript\" src=\"" + this.GetClientUrl( EwfApp.MetaLogicFactory.CreateModernizrJavaScriptInfo().GetUrl( false, false, false ) ) +
							"\"></script>"
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
				sw.WriteLine( "ga('create', '" + EwfApp.Instance.GoogleAnalyticsWebPropertyId + "', 'auto');" );
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
			var controlTreeDataLoader = control as ControlTreeDataLoader;
			if( controlTreeDataLoader != null )
				controlTreeDataLoader.LoadData();

			foreach( var child in control.Controls.Cast<Control>().Where( i => i != etherealPlace ) )
				loadDataForControlAndChildren( child );

			List<EtherealControl> etherealControls;
			if( !etherealControlsByControl.TryGetValue( control, out etherealControls ) )
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
		public DataModification DataUpdate { get { return dataUpdate; } }

		/// <summary>
		/// Gets a post-back that updates the page's data without performing any other actions.
		/// </summary>
		public PostBack DataUpdatePostBack { get { return dataUpdatePostBack; } }

		/// <summary>
		/// Gets whether the page forces a post-back when a link is clicked.
		/// </summary>
		public virtual bool IsAutoDataUpdater { get { return false; } }

		internal void AddEtherealControl( Control parent, EtherealControl etherealControl ) {
			List<EtherealControl> etherealControls;
			if( !etherealControlsByControl.TryGetValue( parent, out etherealControls ) ) {
				etherealControls = new List<EtherealControl>();
				etherealControlsByControl.Add( parent, etherealControls );
			}
			etherealControls.Add( etherealControl );
		}

		internal void AddPostBack( PostBack postBack ) {
			PostBack existingPostBack;
			if( !postBacksById.TryGetValue( postBack.Id, out existingPostBack ) )
				postBacksById.Add( postBack.Id, postBack );
			else if( existingPostBack != postBack )
				throw new ApplicationException( "A post-back with an ID of \"{0}\" already exists in the page.".FormatWith( existingPostBack.Id ) );
		}

		internal PostBack GetPostBack( string id ) {
			PostBack value;
			return postBacksById.TryGetValue( id, out value ) ? value : null;
		}

		internal void AddFormValue( FormValue formValue ) {
			formValues.Add( formValue );
		}

		/// <summary>
		/// Adds a display mapping to this page.
		/// </summary>
		internal void AddDisplayLink( DisplayLink displayLink ) {
			displayLinks.Add( displayLink );
		}

		internal void AddUpdateRegionLinker( UpdateRegionLinker linker ) {
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
				modErrorDisplaysByValidation.Add( validation, key.ToSingleElementArray().ToList() );

			// We want to ignore all of the problems that could happen, such as the key not existing in the dictionary. This problem will be shown in a more helpful
			// way when we compare form control hashes after a transfer.
			//
			// Avoid using exceptions here if possible. This method is sometimes called many times during a request, and we've seen exceptions take as long as 50 ms
			// each when debugging.
			IEnumerable<string> value;
			return AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay.TryGetValue( key, out value ) ? value : new string[ 0 ];
		}

		internal void AddControlTreeValidation( Action validation ) {
			controlTreeValidations.Add( validation );
		}

		/// <summary>
		/// Notifies this page that only the form controls within the specified control should be checked for modifications and used to set default focus.
		/// </summary>
		public void SetContentContainer( Control control ) {
			contentContainer = control;
		}

		/// <summary>
		/// EWL use only. Gets the status messages.
		/// </summary>
		public IEnumerable<Tuple<StatusMessageType, string>> StatusMessages {
			get { return StandardLibrarySessionState.Instance.StatusMessages.Concat( statusMessages ); }
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

			List<EtherealControl> etherealControls;
			if( !etherealControlsByControl.TryGetValue( control, out etherealControls ) )
				etherealControls = new List<EtherealControl>();
			var etherealChildren = etherealControls.Select( i => Tuple.Create( (Control)i.Control, new Func<string>( i.GetJsInitStatements ) ) );

			var descendants = new List<Tuple<Control, Func<string>>>();
			foreach( var child in normalChildren.Concat( etherealChildren ).Where( i => predicate( i.Item1 ) ) ) {
				descendants.Add( child );
				descendants.AddRange( getDescendants( child.Item1, predicate ) );
			}
			return descendants;
		}

		private void addJavaScriptStartUpLogic() {
			var controlInitStatements = getDescendants( this, i => true ).Where( i => i.Item2 != null ).Select( i => i.Item2() ).Aggregate( ( a, b ) => a + b );

			MaintainScrollPositionOnPostBack = true;
			var requestState = AppRequestState.Instance.EwfPageRequestState;
			var scroll = scrollPositionForThisResponse == ScrollPosition.LastPositionOrStatusBar &&
			             ( !requestState.ModificationErrorsExist ||
			               ( requestState.DmIdAndSecondaryOp != null &&
			                 new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains(
				                 requestState.DmIdAndSecondaryOp.Item2 ) ) );

			// If a transfer happened on this request and we're on the same page and we want to scroll, get coordinates from the per-request data in EwfApp.
			var scrollStatement = "";
			if( scroll && requestState.ScrollPositionX != null && requestState.ScrollPositionY != null )
				scrollStatement = "window.scrollTo(" + requestState.ScrollPositionX + "," + requestState.ScrollPositionY + ");";

			// If the page has requested a client-side redirect, configure it now. The JavaScript solution is preferred over a meta tag since apparently it doesn't
			// cause reload behavior by the browser. See http://josephsmarr.com/2007/06/06/the-hidden-cost-of-meta-refresh-tags.
			string clientSideNavigationUrl;
			bool clientSideNavigationInNewWindow;
			int? clientSideNavigationDelay;
			StandardLibrarySessionState.Instance.GetClientSideNavigationSetup(
				out clientSideNavigationUrl,
				out clientSideNavigationInNewWindow,
				out clientSideNavigationDelay );
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
				"$( document ).ready( function() { " +
				StringTools.ConcatenateWithDelimiter(
					" ",
					"OnDocumentReady();",
					controlInitStatements,
					EwfApp.Instance.JavaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					StringTools.ConcatenateWithDelimiter( " ", scrollStatement, clientSideNavigationStatements )
					.PrependDelimiter( "window.onload = function() { " )
					.AppendDelimiter( " };" ) ) + " } );",
				true );

			setFocus();
		}

		/// <summary>
		/// The desired scroll position of the browser when this response is received.
		/// </summary>
		protected virtual ScrollPosition scrollPositionForThisResponse { get { return ScrollPosition.LastPositionOrStatusBar; } }

		/// <summary>
		/// Gets the function call that should be executed when the jQuery document ready event is fired.
		/// </summary>
		protected virtual string javaScriptDocumentReadyFunctionCall { get { return ""; } }

		private void setFocus() {
			// A SetFocus call takes precedence over a control specified via the controlWithInitialFocus property.
			var controlWithInitialFocusId = AppRequestState.Instance.EwfPageRequestState.ControlWithInitialFocusId;

			// If there was no control specified with SetFocus, default to showing the control with initial focus.
			if( controlWithInitialFocusId == null ) {
				var cachedControlWithInitialFocus = controlWithInitialFocus;
				if( cachedControlWithInitialFocus != null )
					controlWithInitialFocusId = cachedControlWithInitialFocus.UniqueID;
			}

			if( controlWithInitialFocusId != null ) {
				// We use FindControl because it will actually blow up if the control can't be found. Using the string overload of SetFocus, on the other hand, will
				// silently do nothing.
				var control = FindControl( controlWithInitialFocusId );

				if( control is ControlWithCustomFocusLogic )
					( control as ControlWithCustomFocusLogic ).SetFocus();
				else
					base.SetFocus( control );
			}
		}

		/// <summary>
		/// The control that receives focus when the page is loaded by the browser.
		/// </summary>
		protected virtual Control controlWithInitialFocus { get { return GetDescendants( contentContainer ).FirstOrDefault( i => i is FormControl ); } }

		private void executeWithDataModificationExceptionHandling( Action method ) {
			try {
				method();
			}
			catch( Exception e ) {
				var ewfException = e.GetChain().OfType<DataModificationException>().FirstOrDefault();
				if( ewfException == null )
					throw;
				AppRequestState.Instance.EwfPageRequestState.TopModificationErrors = ewfException.Messages;
				AppRequestState.Instance.EwfPageRequestState.SetStaticAndUpdateRegionState(
					getStaticRegionContents( new Control[ 0 ] ).Item1,
					new Tuple<string, string>[ 0 ] );
			}
		}

		private void validateFormSubmission( string formValueHash ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

			var webFormsHiddenFields = new[]
				{ "__EVENTTARGET", "__EVENTARGUMENT", "__LASTFOCUS", "__VIEWSTATE", "__SCROLLPOSITIONX", "__SCROLLPOSITIONY", "__VIEWSTATEGENERATOR" };
			var activeFormValues = formValues.Where( i => i.GetPostBackValueKey().Any() ).ToArray();
			var postBackValueKeys = new HashSet<string>( activeFormValues.Select( i => i.GetPostBackValueKey() ) );
			requestState.PostBackValues = new PostBackValueDictionary();
			var extraPostBackValuesExist =
				requestState.PostBackValues.AddFromRequest(
					Request.Form.Cast<string>().Except( new[] { postBackHiddenFieldName, ButtonElementName }.Concat( webFormsHiddenFields ) ),
					postBackValueKeys.Contains,
					key => Request.Form[ key ] ) |
				requestState.PostBackValues.AddFromRequest( Request.Files.Cast<string>(), postBackValueKeys.Contains, key => Request.Files[ key ] );

			// Make sure data didn't change under this page's feet since the last request.
			var invalidPostBackValuesExist = activeFormValues.Any( i => i.PostBackValueIsInvalid( requestState.PostBackValues ) );
			var formValueHashesDisagree = generateFormValueHash() != formValueHash;
			if( extraPostBackValuesExist || invalidPostBackValuesExist || formValueHashesDisagree ) {
				// Remove invalid post-back values so they don't cause a false developer-mistake exception after the transfer.
				var validPostBackValueKeys = from i in activeFormValues where !i.PostBackValueIsInvalid( requestState.PostBackValues ) select i.GetPostBackValueKey();
				requestState.PostBackValues.RemoveExcept( validPostBackValueKeys );

				throw new DataModificationException( Translation.AnotherUserHasModifiedPageHtml );
			}
		}

		private void handleValidationErrors( EwfValidation validation, IEnumerable<string> errorMessages ) {
			if( !modErrorDisplaysByValidation.ContainsKey( validation ) || !errorMessages.Any() )
				return;
			foreach( var displayKey in modErrorDisplaysByValidation[ validation ] ) {
				var errorsByDisplay = AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay;
				errorsByDisplay[ displayKey ] = errorsByDisplay.ContainsKey( displayKey ) ? errorsByDisplay[ displayKey ].Concat( errorMessages ) : errorMessages;
			}
		}

		/// <summary>
		/// Sets the focus to the specified control. Call this only during event handlers, and use the controlWithInitialFocus property instead if you wish to set
		/// the focus to the same control on all requests. Do not call this during LoadData; it uses the UniqueID of the specified control, which may not be defined
		/// in LoadData if the control hasn't been added to the page.
		/// </summary>
		public new void SetFocus( Control control ) {
			AppRequestState.Instance.EwfPageRequestState.ControlWithInitialFocusId = control.UniqueID;
		}

		private Tuple<string, IEnumerable<FormValue>> getStaticRegionContents( IEnumerable<Control> updateRegionControls ) {
			var contents = new StringBuilder();

			updateRegionControls = new HashSet<Control>( updateRegionControls );
			var staticFormValues =
				GetDescendants( this, predicate: i => !updateRegionControls.Contains( i ) )
					.OfType<FormControl>()
					.Select( i => i.FormValue )
					.Where( i => i != null )
					.Distinct()
					.OrderBy( i => i.GetPostBackValueKey() )
					.ToArray();
			foreach( var formValue in staticFormValues ) {
				contents.Append( formValue.GetPostBackValueKey() );
				contents.Append( formValue.GetDurableValueAsString() );
			}

			var requestState = AppRequestState.Instance.EwfPageRequestState;
			if( requestState.ModificationErrorsExist ) {
				// Include mod error display keys. They shouldn't change across a transfer when there are modification errors because that could prevent some of the
				// errors from being displayed.
				foreach( var modErrorDisplayKey in modErrorDisplaysByValidation.Values.SelectMany( i => i ) )
					contents.Append( modErrorDisplayKey + " " );
			}

			if( requestState.ModificationErrorsExist ||
			    ( requestState.DmIdAndSecondaryOp != null && requestState.DmIdAndSecondaryOp.Item2 == SecondaryPostBackOperation.NoOperation ) ) {
				// It's probably bad if a developer puts a post-back object in the page because of a modification error. It will be gone on the post-back and cannot be
				// processed.
				foreach( var postBack in postBacksById.Values.OrderBy( i => i.Id ) )
					contents.Append( postBack.Id );
			}

			return Tuple.Create<string, IEnumerable<FormValue>>( contents.ToString(), staticFormValues );
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
					destination = createInfoFromNewParameterValues();

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

			NetTools.Redirect( destinationUrl );
		}

		/// <summary>
		/// Creates a page info object using the new parameter value fields in this page.
		/// </summary>
		protected abstract PageInfo createInfoFromNewParameterValues();

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

		protected override sealed void OnPreRender( EventArgs eventArgs ) {
			base.OnPreRender( eventArgs );

			StandardLibrarySessionState.Instance.StatusMessages.Clear();
			StandardLibrarySessionState.Instance.ClearClientSideNavigation();


			// Direct response object modifications. These should happen once per page view; they are not needed in redirect responses.

			FormsAuthStatics.UpdateFormsAuthCookieIfNecessary();

			// Without this header, certain sites could be forced into compatibility mode due to the Compatibility View Blacklist maintained by Microsoft.
			Response.AppendHeader( "X-UA-Compatible", "IE=edge" );
		}

		/// <summary>
		/// Saves view state.
		/// </summary>
		protected override sealed object SaveViewState() {
			// This is the last possible place in the life cycle this could go; view state is saved right after this.
			foreach( Control child in Controls )
				child.EnableViewState = false;

			return base.SaveViewState();
		}

		/// <summary>
		/// Saves hidden field state.
		/// </summary>
		protected override sealed void SavePageStateToPersistenceMedium( object state ) {
			var rs = AppRequestState.Instance.EwfPageRequestState;
			var failingDmId = rs.ModificationErrorsExist && rs.DmIdAndSecondaryOp != null &&
			                  rs.DmIdAndSecondaryOp.Item2 != SecondaryPostBackOperation.ValidateChangesOnly
				                  ? rs.DmIdAndSecondaryOp.Item1
				                  : null;
			base.SavePageStateToPersistenceMedium( PageState.GetViewStateArray( new object[] { generateFormValueHash(), failingDmId } ) );
		}

		private string generateFormValueHash() {
			var formValueString = new StringBuilder();
			foreach( var formValue in formValues.Where( i => i.GetPostBackValueKey().Any() ) ) {
				formValueString.Append( formValue.GetPostBackValueKey() );
				formValueString.Append( formValue.GetDurableValueAsString() );
			}

			var hash = MD5.Create().ComputeHash( Encoding.ASCII.GetBytes( formValueString.ToString() ) );
			var hashString = "";
			foreach( var b in hash )
				hashString += b.ToString( "x2" );
			return hashString;
		}
	}
}