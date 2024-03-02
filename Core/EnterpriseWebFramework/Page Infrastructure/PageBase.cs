#nullable disable
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.UserManagement;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using Serilog;
using StackExchange.Profiling;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// A page in a web application.
/// </summary>
[ PublicAPI ]
public abstract class PageBase: ResourceBase {
	// These strings are duplicated in the JavaScript file.
	internal const string FormId = "ewfForm";
	internal const string HiddenFieldName = "ewfData";

	internal const string ButtonElementName = "ewfButton";

	private static ( Func<Action> pageViewDataModificationMethodGetter, Func<string> javaScriptDocumentReadyFunctionCallGetter ) appProvider;

	private static Func<Func<Func<PageContent>, PageContent>, Func<string>, Func<string>, ( PageContent basicContent, FlowComponent component, FlowComponent
			etherealContainer, FlowComponent jsInitElement, Action dataUpdateModificationMethod, bool isAutoDataUpdater, ActionPostBack pageLoadPostBack )>
		contentGetter;

	[ JsonObject( ItemRequired = Required.Always, MemberSerialization = MemberSerialization.Fields ) ]
	private class HiddenFieldData {
		[ JsonProperty( PropertyName = "firstRequestTime" ) ]
		public readonly Instant FirstRequestTime;

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
			Instant firstRequestTime, ImmutableDictionary<string, JToken> componentStateValuesById, string formValueHash, string lastPostBackFailingDmId,
			string postBackId, string scrollPositionX, string scrollPositionY ) {
			FirstRequestTime = firstRequestTime;
			ComponentStateValuesById = componentStateValuesById;
			FormValueHash = formValueHash;
			LastPostBackFailingDmId = lastPostBackFailingDmId;
			PostBackId = postBackId;
			ScrollPositionX = scrollPositionX;
			ScrollPositionY = scrollPositionY;
		}
	}

	internal static void Init(
		( Func<Action>, Func<string> ) appProvider,
		Func<Func<Func<PageContent>, PageContent>, Func<string>, Func<string>, ( PageContent, FlowComponent, FlowComponent, FlowComponent, Action, bool,
			ActionPostBack )> contentGetter ) {
		EwfValidation.Init(
			() => Current.formState.ValidationPredicate,
			() => Current.formState.DataModifications,
			() => Current.formState.DataModificationsWithValidationsFromOtherElements,
			() => Current.formState.ReportValidationCreated() );
		BasicDataModification.Init(
			RequestStateStatics.NotifyOfSlowDataModification,
			( validation, errorMessages ) => {
				if( !errorMessages.Any() )
					return;

				var current = Current;
				if( !current.modErrorDisplaysByValidation.TryGetValue( validation, out var displays ) )
					throw new ApplicationException( "An undisplayed validation produced errors." );

				var errorsByDisplay = current.requestState.InLineModificationErrorsByDisplay;
				foreach( var displayKey in displays )
					errorsByDisplay[ displayKey ] = errorsByDisplay.TryGetValue( displayKey, out var errors ) ? errors.Concat( errorMessages ) : errorMessages;
			},
			errors => Current.requestState.GeneralModificationErrors = errors );
		FormValueStatics.Init(
			formValue => Current.formValues.Add( formValue ),
			() => Current.formState.DataModifications,
			() => Current.requestState.PostBackValues );
		ComponentStateItem.Init(
			AssertPageTreeNotBuilt,
			() => Current.elementOrIdentifiedComponentIdGetter(),
			id => {
				var valuesById = Current.requestState.ComponentStateValuesById;
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
				if( ( postBack as ActionPostBack )?.ValidationDm is PostBack validationPostBack && Current.GetPostBack( validationPostBack.Id ) != validationPostBack )
					throw new ApplicationException( "The post-back's validation data-modification, if it is a post-back, must have been added to the page." );
			} );
		FormState.Init(
			() => Current.formState,
			dataModifications => {
				if( dataModifications.Contains( Current.dataUpdate ) && dataModifications.Any( i => i != Current.dataUpdate && !( (ActionPostBack)i ).IsIntermediate ) )
					throw new ApplicationException(
						"If the data-update modification is included, it is meaningless to include any full post-backs since these inherently update the page's data." );
			},
			dataModification => dataModification == Current.dataUpdate ? Current.dataUpdatePostBack : (ActionPostBack)dataModification );

		PageBase.appProvider = appProvider;
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
		Current.statusMessages.Add( ( type, message ) );
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

	private PageRequestState requestState;
	private bool postBackValidationDmExecuted;

	private FormState formState;
	internal PageContent BasicContent;
	private PageTree pageTree;
	private Func<string> elementOrIdentifiedComponentIdGetter = () => "";
	private BasicDataModification dataUpdate;
	private readonly PostBack dataUpdatePostBack = PostBack.CreateDataUpdate();
	internal bool? IsAutoDataUpdater;
	private readonly Dictionary<string, PostBack> postBacksById = new();
	private readonly List<FormValue> formValues = new();
	private readonly Dictionary<string, ComponentStateItem> componentStateItemsById = new();
	private IReadOnlyCollection<PageNode> updateRegionLinkerNodes;
	private readonly Dictionary<EwfValidation, List<string>> modErrorDisplaysByValidation = new();
	private readonly HashSet<EwfValidation> validationsWithErrors = new();
	private readonly List<Action> controlTreeValidations = new();
	internal PostBack SubmitButtonPostBack;
	private readonly List<( StatusMessageType, string )> statusMessages = new();

	/// <summary>
	/// Initializes the parameters modification object for this page.
	/// </summary>
	protected abstract void initParametersModification();

	/// <summary>
	/// Gets whether the page takes more than about a second to handle a GET request.
	/// </summary>
	protected internal virtual bool IsSlow => false;

	protected sealed override bool disablesUrlNormalization => base.disablesUrlNormalization;

	protected sealed override ExternalRedirect getRedirect() => base.getRedirect();

	protected sealed override EwfSafeRequestHandler getOrHead() {
		requestState = new PageRequestState();
		return new EwfSafeResponseWriter( executePageViewDataModifications().buildPageAndGetResponse( new SpecifiedValue<string>( null ) ) );
	}

	protected sealed override bool managesDataModificationsInUnsafeRequestMethods => true;

	protected sealed override EwfResponse put() => base.put();
	protected sealed override EwfResponse patch() => base.patch();
	protected sealed override EwfResponse delete() => base.delete();

	protected sealed override EwfResponse post() => ProcessFormSubmissionAndGetResponse( false );

	internal EwfResponse ProcessFormSubmissionAndGetResponse( bool pageBuilt ) {
		IFormCollection formSubmission;
		HiddenFieldData hiddenFieldData;
		try {
			using( MiniProfiler.Current.Step( "EWF - Wait for request body to be received" ) )
				formSubmission = EwfRequest.Current.GetFormSubmission();

			// throws exception if field missing, because Request.Form returns null
			hiddenFieldData = JsonConvert.DeserializeObject<HiddenFieldData>(
				formSubmission[ HiddenFieldName ],
				new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error } );

			requestState = new PageRequestState(
				pageBuilt ? requestState.FirstRequestTime : hiddenFieldData.FirstRequestTime,
				hiddenFieldData.ScrollPositionX,
				hiddenFieldData.ScrollPositionY )
				{
					ComponentStateValuesById = pageBuilt ? requestState.ComponentStateValuesById : hiddenFieldData.ComponentStateValuesById
				};
		}
		catch {
			// Set a 400 status code if there are any problems loading hidden field state. We’re assuming these problems are never the developers’ fault.
			requestState ??= new PageRequestState();
			requestState.GeneralModificationErrors = Translation.ApplicationHasBeenUpdatedAndWeCouldNotInterpretAction.ToCollection();
			return executePageViewDataModifications().buildPageAndGetResponse( null, statusCode: 400 );
		}

		if( !pageBuilt )
			buildPage( hiddenFieldData.LastPostBackFailingDmId );

		var errors = validateFormSubmission( formSubmission, hiddenFieldData.FormValueHash );
		if( errors is not null ) {
			requestState.GeneralModificationErrors = errors;
			return navigateToCurrent( null );
		}

		// Get the post-back object and, if necessary, the last post-back’s failing data modification.
		var postBack = GetPostBack( hiddenFieldData.PostBackId );
		if( postBack is null ) {
			requestState.GeneralModificationErrors = Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction.ToCollection();
			return navigateToCurrent( null );
		}
		var lastPostBackFailingDm = postBack.IsIntermediate && hiddenFieldData.LastPostBackFailingDmId != null
			                            ? hiddenFieldData.LastPostBackFailingDmId.Any()
				                              ? GetPostBack( hiddenFieldData.LastPostBackFailingDmId ) as DataModification
				                              : dataUpdate
			                            : null;
		if( postBack.IsIntermediate && hiddenFieldData.LastPostBackFailingDmId is not null && lastPostBackFailingDm is null ) {
			requestState.GeneralModificationErrors = Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction.ToCollection();
			return navigateToCurrent( null );
		}

		if( postBack.IsIntermediate )
			return executePostBackAndGetResponse( (ActionPostBack)postBack, lastPostBackFailingDm );

		// Execute the page’s data update.
		var dataUpdateExecuted = dataUpdate.Execute( !postBack.ForcePageDataUpdate, changesExist( dataUpdate ) );
		if( modificationErrorsExist )
			return navigateToCurrent( "" );

		if( dataUpdateExecuted )
			commitDataModificationsToRequestState();

		if( postBack is not ActionPostBack actionPostBack ) {
			requestState = new PageRequestState();
			return navigate( ( null, null ), page => page.executePageViewDataModifications().buildPageAndGetResponse( new SpecifiedValue<string>( null ) ) );
		}

		if( !dataUpdateExecuted )
			return executePostBackAndGetResponse( actionPostBack, null );

		var stateItemsAndFormValues = getPostBackStateItemsAndFormValues( actionPostBack );
		return navigate( ( null, null ), page => page.processPostBackAfterDataUpdate( actionPostBack.Id, stateItemsAndFormValues ) );
	}

	private EwfResponse executePostBackAndGetResponse( ActionPostBack postBack, DataModification lastPostBackFailingDm ) {
		( ResourceInfo destination, Func<ResourceInfo, bool> authorizationCheckDisabledPredicate )? navigationBehavior = null;
		var focusKey = "";
		FullResponse fullSecondaryResponse = null;
		var postBackExecuted = postBack.Execute(
			changesExist( postBack ),
			postBackAction => {
				navigationBehavior = postBackAction?.NavigationBehavior;
				focusKey = postBackAction?.ReloadBehavior?.FocusKey ?? "";
				fullSecondaryResponse = postBackAction?.ReloadBehavior?.SecondaryResponse?.GetFullResponse();
			} );
		if( modificationErrorsExist )
			return navigateToCurrent( postBack.Id );

		if( postBackExecuted )
			commitDataModificationsToRequestState();

		Func<PageBase, EwfResponse> actionProcessor;
		if( postBack.IsIntermediate ) {
			var regionSets = postBack.UpdateRegions.ToImmutableHashSet();
			var updateRegions = updateRegionLinkerNodes.SelectMany( i => i.KeyedUpdateRegionLinkers, ( node, keyedLinker ) => ( node, keyedLinker ) )
				.SelectMany(
					nodeLinker => nodeLinker.keyedLinker.linker.PreModificationRegions.Where( i => regionSets.Overlaps( i.Sets ) ),
					( nodeLinker, region ) => ( nodeLinker.node, nodeLinker.keyedLinker.key, region ) )
				.Materialize();
			var staticRegionContents = getStaticRegionContents( updateRegions.Select( i => ( i.node, i.region.ComponentGetter() ) ) );

			requestState.ComponentStateValuesById = componentStateItemsById.Where( i => staticRegionContents.stateItems.Contains( i.Value ) )
				.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson );
			requestState.PostBackValues.RemoveExcept( staticRegionContents.formValues.Select( i => i.GetPostBackValueKey() ) );

			var updateRegionKeysAndArguments = updateRegions.Select( i => ( i.key, i.region.ArgumentGetter() ) ).Materialize();
			actionProcessor = page => {
				page = page.executePageViewDataModifications();
				page.buildPage( null );
				page.assertStaticRegionsUnchanged( updateRegionKeysAndArguments, staticRegionContents.contents );
				return ( postBack.ValidationDm == lastPostBackFailingDm ? SecondaryPostBackOperation.Validate : SecondaryPostBackOperation.ValidateChangesOnly ) ==
				       SecondaryPostBackOperation.Validate
					       ? page.processSecondaryOperationAndGetResponse(
						       postBack.ValidationDm == dataUpdate ? "" : ( (ActionPostBack)postBack.ValidationDm ).Id,
						       SecondaryPostBackOperation.Validate,
						       focusKey )
					       : page.getResponse( new SpecifiedValue<string>( focusKey ) );
			};
		}
		else {
			requestState = new PageRequestState();
			actionProcessor = page => page.executePageViewDataModifications().buildPageAndGetResponse( new SpecifiedValue<string>( focusKey ) );
		}

		return navigate(
			navigationBehavior ?? ( null, null ),
			page => {
				// Store the secondary response right before navigation so that it doesn’t get sent if there is an error before this point.
				if( fullSecondaryResponse is not null )
					RequestStateStatics.SetSecondaryResponseId( SecondaryResponseDataStore.AddResponse( fullSecondaryResponse ) );

				return actionProcessor( page );
			} );
	}

	private EwfResponse buildPageAndGetResponse( SpecifiedValue<string> focusKey, int? statusCode = null ) {
		buildPage( null );
		return getResponse( focusKey, statusCode: statusCode );
	}

	private IReadOnlyCollection<TrustedHtmlString> validateFormSubmission( IFormCollection submission, string formValueHash ) {
		var extraComponentStateValues = requestState.ComponentStateValuesById.Keys.Where( i => !componentStateItemsById.ContainsKey( i ) ).Materialize();
		var invalidComponentStateValues = componentStateItemsById
			.Where( i => !requestState.ComponentStateValuesById.ContainsKey( i.Key ) || i.Value.ValueIsInvalid() )
			.Select( i => i.Key )
			.Materialize();

		var activeFormValues = formValues.Where( i => i.GetPostBackValueKey().Any() ).ToArray();
		var postBackValueKeys = new HashSet<string>( activeFormValues.Select( i => i.GetPostBackValueKey() ) );
		requestState.PostBackValues = new PostBackValueDictionary();
		var extraPostBackValues = requestState.PostBackValues.AddFromRequest(
				submission.Where(
						i => !string.Equals( i.Key, HiddenFieldName, StringComparison.Ordinal ) && !string.Equals( i.Key, ButtonElementName, StringComparison.Ordinal ) )
					.SelectMany( pair => pair.Value.Select( value => KeyValuePair.Create( pair.Key, (object)value ) ) ),
				postBackValueKeys.Contains )
			.Concat(
				requestState.PostBackValues.AddFromRequest( submission.Files.Select( i => KeyValuePair.Create( i.Name, (object)i ) ), postBackValueKeys.Contains ) )
			.Materialize();

		var invalidPostBackValues = activeFormValues.Where( i => i.PostBackValueIsInvalid() ).Select( i => i.GetPostBackValueKey() ).Materialize();
		var formValueHashesDisagree = generateFormValueHash() != formValueHash;

		if( !extraComponentStateValues.Any() && !invalidComponentStateValues.Any() && !extraPostBackValues.Any() && !invalidPostBackValues.Any() &&
		    !formValueHashesDisagree )
			return null;

		if( extraComponentStateValues.Any() )
			Log.Debug( "Form-submission validation failed due to extra component-state values: {Values}", extraComponentStateValues );
		else if( invalidComponentStateValues.Any() )
			Log.Debug(
				"Form-submission validation failed due to invalid component-state values: {@Values}",
				invalidComponentStateValues.Select(
					i => new { Id = i, Value = requestState.ComponentStateValuesById.TryGetValue( i, out var value ) ? value : "missing" } ) );
		else if( extraPostBackValues.Any() )
			Log.Debug( "Form-submission validation failed due to extra post-back values: {Values}", extraPostBackValues );
		else if( invalidPostBackValues.Any() )
			Log.Debug(
				"Form-submission validation failed due to invalid post-back values: {@Values}",
				invalidPostBackValues.Select(
					i => {
						var value = requestState.PostBackValues.GetValue( i );
						return new { Key = i, Value = value is not null ? "{0}".FormatWith( value ) : "missing" };
					} ) );
		else if( formValueHashesDisagree )
			Log.Debug( "Form-submission validation failed due to disagreeing form-value hashes" );

		removeInvalidComponentStateAndPostBackValues();

		return Translation.AnotherUserHasModifiedPageHtml.ToCollection();
	}

	private EwfResponse processPostBackAfterDataUpdate( string postBackId, string stateItemsAndFormValues ) {
		buildPage( null );

		var postBack = (ActionPostBack)GetPostBack( postBackId );
		TrustedHtmlString validatePage() {
			if( postBack is null ) {
				Log.Debug( "Post-back execution failed after the data update because the post-back was missing" );
				return Translation.YouHaveModifiedPageAndWeCouldNotInterpretAction;
			}

			if( !string.Equals( getPostBackStateItemsAndFormValues( postBack ), stateItemsAndFormValues, StringComparison.Ordinal ) ) {
				Log.Debug( "Post-back execution failed after the data update because component-state items and/or form values changed" );
				return Translation.YouHaveModifiedPageAndWeCouldNotInterpretAction;
			}
			var invalidComponentStateValues = componentStateItemsById.Where( i => i.Value.ValueIsInvalid() && i.Value.DataModifications.Contains( postBack ) )
				.Select( i => i.Key )
				.OrderBy( i => i )
				.Materialize();
			if( invalidComponentStateValues.Any() ) {
				Log.Debug(
					"Post-back execution failed after the data update due to component-state values that became invalid: {@Values}",
					invalidComponentStateValues.Select( i => new { Id = i, Value = requestState.ComponentStateValuesById[ i ] } ) );
				return Translation.YouHaveModifiedPageAndWeCouldNotInterpretAction;
			}
			var invalidPostBackValues = formValues
				.Where( i => i.GetPostBackValueKey().Length > 0 && i.PostBackValueIsInvalid() && i.DataModifications.Contains( postBack ) )
				.Select( i => i.GetPostBackValueKey() )
				.OrderBy( i => i )
				.Materialize();
			if( invalidPostBackValues.Any() ) {
				Log.Debug(
					"Post-back execution failed after the data update due to post-back values that became invalid: {@Values}",
					invalidPostBackValues.Select(
						i => {
							var value = requestState.PostBackValues.GetValue( i );
							return new { Key = i, Value = value is not null ? "{0}".FormatWith( value ) : "missing" };
						} ) );
				return Translation.YouHaveModifiedPageAndWeCouldNotInterpretAction;
			}

			return null;
		}
		var error = validatePage();
		if( error is not null ) {
			requestState.GeneralModificationErrors = error.ToCollection();
			removeInvalidComponentStateAndPostBackValues();
			return navigateToCurrent( null );
		}

		return executePostBackAndGetResponse( postBack, null );
	}

	private string getPostBackStateItemsAndFormValues( ActionPostBack postBack ) {
		var builder = new StringBuilder();
		builder.AppendLine( "Component-state items:" );
		foreach( var pair in componentStateItemsById.Where( i => i.Value.DataModifications.Contains( postBack ) ).OrderBy( i => i.Key ) )
			builder.AppendLine( "\t" + pair.Key );
		builder.AppendLine( "Form values:" );
		foreach( var key in formValues.Where( i => i.GetPostBackValueKey().Length > 0 && i.DataModifications.Contains( postBack ) )
			        .Select( i => i.GetPostBackValueKey() )
			        .OrderBy( i => i ) )
			builder.AppendLine( "\t" + key );

		var s = builder.ToString();
		Log.Debug( "Post-back component-state items and form values represented as {Values}", Environment.NewLine + s );
		return s;
	}

	/// <summary>
	/// Removes invalid component-state and post-back values so they don't cause a false developer-mistake exception after the page is rebuilt.
	/// </summary>
	private void removeInvalidComponentStateAndPostBackValues() {
		requestState.ComponentStateValuesById = requestState.ComponentStateValuesById.RemoveRange(
			from i in componentStateItemsById where i.Value.ValueIsInvalid() select i.Key );
		var validPostBackValueKeys = from i in formValues where i.GetPostBackValueKey().Length > 0 && !i.PostBackValueIsInvalid() select i.GetPostBackValueKey();
		requestState.PostBackValues.RemoveExcept( validPostBackValueKeys );
	}

	private EwfResponse navigateToCurrent( string failingDataModificationId ) =>
		navigate(
			null,
			page => {
				page.buildPage( failingDataModificationId );
				page.assertStaticRegionsUnchanged( null, getStaticRegionContents( null ).contents );
				return page.getResponse( null );
			} );

	private void commitDataModificationsToRequestState() {
		RequestStateStatics.AppendStatusMessages( statusMessages );
		RequestStateStatics.RefreshRequestState();
	}

	private EwfResponse processSecondaryOperationAndGetResponse( string dataModificationId, SecondaryPostBackOperation operation, string focusKey ) {
		var dataModification = dataModificationId.Length > 0 ? GetPostBack( dataModificationId ) as DataModification : dataUpdate;
		if( dataModification is null )
			throw getDeveloperMistakeException( "A data modification with an ID of \"{0}\" does not exist.".FormatWith( dataModificationId ) );

		var navigationNeeded = dataModification == dataUpdate
			                       ? dataUpdate.Execute( true, changesExist( dataModification ), performValidationOnly: true )
			                       : ( (ActionPostBack)dataModification ).Execute( changesExist( dataModification ), null );

		if( !navigationNeeded )
			return getResponse( new SpecifiedValue<string>( focusKey ) );

		return navigate(
			null,
			page => {
				page.postBackValidationDmExecuted = true;
				page.buildPage( modificationErrorsExist && operation != SecondaryPostBackOperation.ValidateChangesOnly ? dataModificationId : null );
				page.assertStaticRegionsUnchanged( null, getStaticRegionContents( null ).contents );
				return page.getResponse( new SpecifiedValue<string>( focusKey ) );
			} );
	}

	private bool changesExist( DataModification dataModification ) =>
		componentStateItemsById.Values.Any( i => i.IncludedInChangeDetection && i.DataModifications.Contains( dataModification ) && i.ValueChanged() ) ||
		formValues.Any( i => i.DataModifications.Contains( dataModification ) && i.ValueChangedOnPostBack() );

	// Page-view data modifications. All data modifications that happen simply because of a request and require no other action by the user should happen once per
	// page view, and prior to LoadData so that the modified data can be used in the page if necessary.
	private PageBase executePageViewDataModifications() {
		var modMethods = new List<Action>();
		modMethods.Add( appProvider.pageViewDataModificationMethodGetter() );
		if( RequestState.Instance.UserAccessible ) {
			if( AppTools.User != null )
				modMethods.Add( getLastPageRequestTimeUpdateMethod( AppTools.User ) );
			if( RequestState.Instance.ImpersonatorExists && RequestState.Instance.ImpersonatorUser != null )
				modMethods.Add( getLastPageRequestTimeUpdateMethod( RequestState.Instance.ImpersonatorUser ) );
		}
		modMethods.Add( getPageViewDataModificationMethod() );
		modMethods = modMethods.Where( i => i != null ).ToList();

		if( !modMethods.Any() )
			return this;

		AutomaticDatabaseConnectionManager.Current.ExecuteWithModificationsEnabled(
			() => {
				foreach( var i in modMethods )
					i();
			} );

		commitDataModificationsToRequestState();
		statusMessages.Clear();

		// Re-create page object. A big reason to do this is that some pages execute database queries or other code during initialization in order to prime
		// the data-access cache. The code above resets the cache and we want to re-prime it right away.
		PageBase newPageObject;
		using( MiniProfiler.Current.Step( "EWF - Re-create page object after page-view data modifications" ) )
			newPageObject = (PageBase)ReCreate();
		bool urlChanged;
		using( MiniProfiler.Current.Step( "EWF - Check URL after page-view data modifications" ) )
			urlChanged = newPageObject.GetUrl( false, false ) != GetUrl( false, false );
		if( urlChanged )
			throw getDeveloperMistakeException( "The URL of the page changed after page-view data modifications." );
		bool userAuthorized;
		using( MiniProfiler.Current.Step( "EWF - Check page authorization after page-view data modifications" ) )
			userAuthorized = newPageObject.UserCanAccess;
		DisabledResourceMode disabledMode;
		using( MiniProfiler.Current.Step( "EWF - Check alternative page mode after page-view data modifications" ) )
			disabledMode = newPageObject.AlternativeMode as DisabledResourceMode;
		if( !userAuthorized || disabledMode != null )
			throw getDeveloperMistakeException( "The user lost access to the page or the page became disabled after page-view data modifications." );
		newPageObject.requestState = requestState;
		return nextPageObject = newPageObject;
	}

	/// <summary>
	/// It's important to call this from EwfPage instead of EwfApp because requests for some pages, with their associated images, CSS files, etc., can easily
	/// cause 20-30 server requests, and we only want to update the time stamp once for all of these.
	/// </summary>
	private Action getLastPageRequestTimeUpdateMethod( SystemUser user ) {
		// Only update the request time if a significant amount of time has passed since we did it last. This can dramatically reduce concurrency issues caused by
		// people rapidly assigning tasks to one another in the System Manager or similar situations.
		if( EwfRequest.Current.RequestTime - user.LastRequestTime < Duration.FromMinutes( 60 ) )
			return null;

		// Now we want to do a timestamp-based concurrency check so we don't update the last login date if we know another transaction already did.
		// It is not perfect, but it reduces errors caused by one user doing a long-running request and then doing smaller requests
		// in another browser window while the first one is still running.
		// We have to query in a separate transaction because otherwise snapshot isolation will result in us always getting the original LastRequestTime, even if
		// another transaction has modified its value during this transaction.
		var newlyQueriedUser = new DataAccessState().ExecuteWithThis(
			() => {
				SystemUser getUser() => UserManagementStatics.GetUser( user.UserId, false );
				return ConfigurationStatics.DatabaseExists ? DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen( getUser ) : getUser();
			} );
		if( newlyQueriedUser == null || newlyQueriedUser.LastRequestTime > user.LastRequestTime )
			return null;

		return () => {
			void updateUser() {
				UserManagementStatics.SystemProvider.InsertOrUpdateUser( user.UserId, user.Email, user.Role.RoleId, EwfRequest.Current.RequestTime );
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

	// The warning below also appears on AppStandardPageLogicProvider.GetPageViewDataModificationMethod.
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

	private EwfResponse navigate(
		( ResourceInfo destination, Func<ResourceInfo, bool> authorizationCheckDisabledPredicate )? navigationBehavior,
		Func<PageBase, EwfResponse> actionProcessor ) {
		ResourceInfo destination;
		bool authorizationCheckDisabled;
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
			if( !navigationBehavior.HasValue )
				destination = ReCreate();
			else {
				RequestState.Instance.SetNewUrlParameterValuesEffective( true );
				if( navigationBehavior.Value.destination is null )
					destination = reCreateFromNewParameterValues();
				else if( navigationBehavior.Value.destination is ResourceBase r )
					destination = r.ReCreate();
				else
					destination = navigationBehavior.Value.destination;
			}

			// This GetUrl call is important even for the transfer case below for the same reason that we *actually create* a new page object in every case above.
			// We want to force developers to get an error email if a page modifies data to make itself unauthorized/disabled without specifying a different page as
			// the redirect destination. The resulting transfer would lead the user to an error page.
			authorizationCheckDisabled = navigationBehavior?.authorizationCheckDisabledPredicate?.Invoke( destination ) == true;
			destinationUrl = destination.GetUrl( !authorizationCheckDisabled, !authorizationCheckDisabled );
		}
		catch( Exception e ) {
			throw getDeveloperMistakeException( "The post-modification destination page became invalid.", innerException: e );
		}

		if( destination is PageBase page ) {
			RequestStateStatics.SetClientSideNewUrl( destinationUrl );

			// If the destination page has the same origin as the current page, do a transfer instead of a redirect. Don’t do this if the authorization check was
			// disabled since, if there is a possibility of the destination page sending a 403 status code, we need to always send a 303 code first (below) so the
			// client knows the POST worked.
			if( !authorizationCheckDisabled && Uri.Compare(
				    new Uri( destinationUrl ),
				    new Uri( EwfRequest.Current.Url ),
				    UriComponents.SchemeAndServer,
				    UriFormat.UriEscaped,
				    StringComparison.Ordinal ) == 0 ) {
				page.replaceUrlHandlers();
				RequestState.Instance.SetNewUrlParameterValuesEffective( false );

				page.requestState = requestState;
				nextPageObject = page;
				return actionProcessor( page );
			}

			destinationUrl = RequestStateStatics.StoreRequestStateForContinuation(
				destinationUrl,
				"GET",
				context => {
					page.replaceUrlHandlers();
					RequestState.Instance.SetNewUrlParameterValuesEffective( false );

					if( authorizationCheckDisabled )
						page.HandleRequest( context, false );
					else {
						RequestState.Instance.SetResource( page );

						page.requestState = requestState;
						actionProcessor( page ).WriteToAspNetResponse( context.Response );
					}
				} );
		}

		return EwfResponse.Create(
			ContentTypes.PlainText,
			new EwfResponseBodyCreator( writer => writer.Write( "See Other: {0}".FormatWith( destinationUrl ) ) ),
			statusCodeGetter: () => 303,
			additionalHeaderFieldGetter: () => ( "Location", destinationUrl ).ToCollection() );
	}

	private void replaceUrlHandlers() {
		var urlHandlers = new List<BasicUrlHandler>();
		UrlHandler urlHandler = this;
		do
			urlHandlers.Add( urlHandler );
		while( ( urlHandler = urlHandler.GetParent() ) != null );
		RequestState.Instance.SetUrlHandlers( urlHandlers );
	}

	private void buildPage( string failingDataModificationId ) {
		UrlHandler urlHandler = this;
		do {
			if( urlHandler is ResourceBase resource ) {
				if( urlHandler is PageBase page )
					page.initParametersModification();
				resource.EsAsBaseType?.InitParametersModification();
			}
			else if( urlHandler is EntitySetupBase entitySetup )
				entitySetup.InitParametersModification();
		}
		while( ( urlHandler = urlHandler.GetParent() ) != null );

		formState = new FormState();
		dataUpdate = new BasicDataModification( dataUpdateIsSlow );
		FormAction pageLoadAction = null;
		var elementJsInitStatements = new StringBuilder();
		var content = contentGetter(
			defaultContentGetter => FormState.ExecuteWithDataModificationsAndDefaultAction(
				DataUpdate.ToCollection(),
				() => {
					using( MiniProfiler.Current.Step( "EWF - Get page content" ) )
						return getContent() ?? defaultContentGetter();
				} ),
			() => JsonConvert.SerializeObject(
				new HiddenFieldData(
					requestState.FirstRequestTime,
					/* Put the values in request state so they’re available if the request is continued for a page-load post-back. */
					requestState.ComponentStateValuesById = componentStateItemsById.ToImmutableDictionary( i => i.Key, i => i.Value.ValueAsJson ),
					generateFormValueHash(),
					failingDataModificationId,
					"",
					"",
					"" ),
				Formatting.None ),
			() => getJsInitStatements( elementJsInitStatements.ToString(), pageLoadAction != null ? pageLoadAction.GetJsStatements() : "" ) );
		BasicContent = content.basicContent;
		if( content.dataUpdateModificationMethod != null )
			dataUpdate.AddModificationMethod( content.dataUpdateModificationMethod );
		IsAutoDataUpdater = content.isAutoDataUpdater;
		if( content.pageLoadPostBack != null )
			( pageLoadAction = new PostBackFormAction( content.pageLoadPostBack ) ).AddToPageIfNecessary();
		using( MiniProfiler.Current.Step( "EWF - Build page tree" ) )
			pageTree = new PageTree(
				content.component,
				id => elementOrIdentifiedComponentIdGetter = () => id,
				addModificationErrorDisplaysAndGetErrors,
				requestState.GeneralModificationErrors,
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
	/// Gets whether the page’s data-update modification takes more than about a second to execute.
	/// </summary>
	protected virtual bool dataUpdateIsSlow => false;

	/// <summary>
	/// Returns the page content.
	/// </summary>
	protected virtual PageContent getContent() => null;

	private string generateFormValueHash() {
		var formValueString = new StringBuilder();

		formValueString.AppendLine( "Component-state items:" );
		foreach( var pair in componentStateItemsById.Where( i => i.Value.IncludedInChangeDetection ).OrderBy( i => i.Key ) )
			formValueString.AppendLine( "\t{0}: {1}".FormatWith( pair.Key, pair.Value.DurableValueAsString ) );
		formValueString.AppendLine( "Form values:" );
		foreach( var formValue in formValues.Where( i => i.GetPostBackValueKey().Any() && i.DataModifications.Any() ) )
			formValueString.AppendLine( "\t{0}: {1}".FormatWith( formValue.GetPostBackValueKey(), formValue.GetDurableValueAsString() ) );

		var hash = MD5.Create().ComputeHash( Encoding.ASCII.GetBytes( formValueString.ToString() ) );
		var hashString = "";
		foreach( var b in hash )
			hashString += b.ToString( "x2" );

		Log.Debug( "Form-value hash generated from {Values}", Environment.NewLine + formValueString );

		return hashString;
	}

	private string getJsInitStatements( string elementJsInitStatements, string pageLoadActionStatements ) {
		var scroll = scrollPositionForThisResponse == ScrollPosition.LastPositionOrStatusBar && !ModificationErrorsOccurred;
		var scrollStatement = "";
		if( scroll && requestState.ScrollPositionX != null && requestState.ScrollPositionY != null )
			scrollStatement = "window.scroll(" + requestState.ScrollPositionX + "," + requestState.ScrollPositionY + ");";

		// If the request has a secondary response, configure it now. The JavaScript solution is preferred over a meta tag since apparently it doesn’t cause reload
		// behavior by the browser. See http://josephsmarr.com/2007/06/06/the-hidden-cost-of-meta-refresh-tags.
		var secondaryResponseId = RequestStateStatics.GetSecondaryResponseId();
		var secondaryResponseStatements = "";
		if( secondaryResponseId.HasValue ) {
			var secretAndResponse = SecondaryResponseDataStore.GetSecretAndResponse( secondaryResponseId.Value );
			if( secretAndResponse.HasValue ) {
				var url = new PreBuiltResponse( secondaryResponseId.Value, secretAndResponse.Value.secret ).GetUrl();
				if( !secretAndResponse.Value.response.FileName.Any() )
					secondaryResponseStatements = "var newWindow = window.open( '{0}', '{1}' ); newWindow.focus();".FormatWith( url, "_blank" );
				else
					secondaryResponseStatements = "location.replace( '" + url + "' );";
			}
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
			appProvider.javaScriptDocumentReadyFunctionCallGetter().AppendDelimiter( ";" ),
			javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
			"addSpeculationRules();",
			StringTools.ConcatenateWithDelimiter( " ", scrollStatement, ModificationErrorsOccurred ? "" : pageLoadActionStatements, secondaryResponseStatements )
				.PrependDelimiter( "window.onload = function() { " )
				.AppendDelimiter( " };" ) );
	}

	private ImmutableDictionary<EwfValidation, IReadOnlyCollection<string>> addModificationErrorDisplaysAndGetErrors( string id, ErrorSourceSet errorSources ) =>
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
					var errors = requestState.InLineModificationErrorsByDisplay.TryGetValue( displayKey, out var value ) ? value.Materialize() : new string[ 0 ];

					if( errors.Any() )
						validationsWithErrors.Add( validation );

					return ( validation, errors );
				} )
			.ToImmutableDictionary( i => i.validation, i => i.errors );

	/// <summary>
	/// Gets the time instant at which the page was first requested. This remains constant across intermediate post-backs, but is reset after a full post-back.
	/// </summary>
	public Instant FirstRequestTime => requestState.FirstRequestTime;

	/// <summary>
	/// Gets the page’s data-update modification, which executes on every full post-back prior to the post-back object. WARNING: Do *not* use this for
	/// modifications that should happen because of a specific post-back action, e.g. adding a new item to the database when a button is clicked. There are two
	/// reasons for this. First, there may be other post-back controls such as buttons or lookup boxes on the page, any of which could also cause the update to
	/// execute. Second, by default the update only runs if form values were modified, which would not be the case if a user clicks the button on an add-item
	/// page before entering any data.
	/// </summary>
	public DataModification DataUpdate => dataUpdate;

	/// <summary>
	/// Gets a post-back that updates the page’s data without performing any other actions.
	/// </summary>
	public PostBack DataUpdatePostBack => dataUpdatePostBack;

	internal PostBack GetPostBack( string id ) => postBacksById.TryGetValue( id, out var value ) ? value : null;

	internal void AddControlTreeValidation( Action validation ) {
		controlTreeValidations.Add( validation );
	}

	/// <summary>
	/// Gets the status messages.
	/// </summary>
	internal IEnumerable<( StatusMessageType, string )> StatusMessages => RequestStateStatics.GetStatusMessages().Concat( statusMessages );

	// Pass null for updateRegionKeysAndArguments when modification errors exist or during the validation stage of an intermediate post-back.
	private void assertStaticRegionsUnchanged(
		IReadOnlyCollection<( string key, string arg )> updateRegionKeysAndArguments, string previousStaticRegionContents ) {
		var nodeUpdateRegionLinkersByKey = updateRegionLinkerNodes.SelectMany( i => i.KeyedUpdateRegionLinkers, ( node, keyedLinker ) => ( node, keyedLinker ) )
			.ToImmutableDictionary( i => i.keyedLinker.key );
		var updateRegions = updateRegionKeysAndArguments?.Select(
			keyAndArg => {
				if( !nodeUpdateRegionLinkersByKey.TryGetValue( keyAndArg.key, out var nodeLinker ) )
					throw getDeveloperMistakeException(
						"An update region linker with the key \"{0}\" does not exist. The post-back included {1}; the page contains {2}.".FormatWith(
							keyAndArg.key,
							StringTools.GetEnglishListPhrase( updateRegionKeysAndArguments.Select( i => $"\"{i.key}\"" ), true ),
							nodeUpdateRegionLinkersByKey.Any()
								? StringTools.GetEnglishListPhrase( nodeUpdateRegionLinkersByKey.Select( i => $"\"{i.Key}\"" ), true )
								: "no linkers" ) );
				return ( nodeLinker.node, nodeLinker.keyedLinker.linker.PostModificationRegionGetter( keyAndArg.arg ) );
			} );

		var message = new StringBuilder();
		var staticRegionContents = getStaticRegionContents( updateRegions );
		message.Append(
			!string.Equals( staticRegionContents.contents, previousStaticRegionContents, StringComparison.Ordinal )
				? "Previous static-region contents: " + Environment.NewLine + Environment.NewLine + previousStaticRegionContents + Environment.NewLine +
				  "Current static-region contents: " + Environment.NewLine + Environment.NewLine + staticRegionContents.contents + Environment.NewLine
				: "" );
		message.Append(
			StringTools
				.ConcatenateWithDelimiter( Environment.NewLine, componentStateItemsById.Where( i => i.Value.ValueIsInvalid() ).Select( i => i.Key ).OrderBy( i => i ) )
				.Surround(
					"Component-state items whose values became invalid:" + Environment.NewLine + Environment.NewLine,
					Environment.NewLine + Environment.NewLine ) );
		message.Append(
			StringTools.ConcatenateWithDelimiter(
					Environment.NewLine,
					formValues.Where( i => i.GetPostBackValueKey().Any() && i.PostBackValueIsInvalid() ).Select( i => i.GetPostBackValueKey() ).OrderBy( i => i ) )
				.Surround(
					"Form values whose post-back values became invalid:" + Environment.NewLine + Environment.NewLine,
					Environment.NewLine + Environment.NewLine ) );
		if( message.Length > 0 )
			throw getDeveloperMistakeException(
				" " + ( modificationErrorsExist
					        ?
					        "Post-backs, form controls, component-state items, and modification-error-display keys may not change if modification errors exist."
					        : updateRegions is null
						        ? "Post-backs, form controls, and component-state items may not change during the validation stage of an intermediate post-back."
						        : "Form controls and component-state items outside of update regions may not change on an intermediate post-back." ) + Environment.NewLine +
				Environment.NewLine + message );
	}

	// Pass null for updateRegions when modification errors exist or during the validation stage of an intermediate post-back.
	private ( string contents, ImmutableHashSet<ComponentStateItem> stateItems, IReadOnlyCollection<FormValue> formValues ) getStaticRegionContents(
		IEnumerable<( PageNode node, IEnumerable<PageComponent> components )> updateRegions ) {
		var contents = new StringBuilder();

		if( updateRegions is null ) {
			// It’s probably bad if a developer puts a post-back object in the page because of a modification error. It will be gone on the post-back and cannot be
			// processed.
			contents.AppendLine( "Post-backs:" );
			foreach( var postBack in postBacksById.Values.OrderBy( i => i.Id ) )
				contents.AppendLine( "\t" + postBack.Id );
		}

		var staticNodes = pageTree.GetStaticRegionNodes( updateRegions );
		var staticStateItems = staticNodes.Select( i => i.StateItem ).Where( i => i != null ).ToImmutableHashSet();
		var staticFormValues = staticNodes.Select( i => i.FormValue ).Where( i => i != null ).Distinct().OrderBy( i => i.GetPostBackValueKey() ).Materialize();

		// Intermediate post-backs sometimes have good reason to change durable values outside of update regions, e.g. when updating filters on a search page. We
		// allow this (by omitting durable values here) under the assumption that all durable values are retrieved using a technique such as snapshot isolation to
		// protect against concurrent modifications during the request. If concurrent modifications did occur they would be unintentionally ignored.
		contents.AppendLine( "Component-state items:" );
		foreach( var pair in componentStateItemsById.Where( i => staticStateItems.Contains( i.Value ) ).OrderBy( i => i.Key ) )
			contents.AppendLine( "\t" + pair.Key );
		contents.AppendLine( "Form values:" );
		foreach( var formValue in staticFormValues )
			contents.AppendLine( "\t" + formValue.GetPostBackValueKey() );

		if( modificationErrorsExist ) {
			// Include mod error display keys. They shouldn't change across a transfer when there are modification errors because that could prevent some of the
			// errors from being displayed.
			contents.AppendLine( "Modification-error-display keys:" );
			foreach( var modErrorDisplayKey in modErrorDisplaysByValidation.Values.SelectMany( i => i ) )
				contents.AppendLine( "\t" + modErrorDisplayKey );
		}

		return ( contents.ToString(), staticStateItems, staticFormValues );
	}

	private ApplicationException getDeveloperMistakeException( string messageSentence, Exception innerException = null ) {
		const string firstSentence = "Developer mistake.";
		const string lastSentence =
			"There is a chance that this was caused by non-transactional data changing outside of the request, but it’s more likely that a developer incorrectly modified something.";
		throw new Exception(
			messageSentence.Contains( Environment.NewLine, StringComparison.Ordinal )
				? firstSentence + messageSentence + lastSentence
				: StringTools.ConcatenateWithDelimiter( " ", firstSentence, messageSentence, lastSentence ),
			innerException );
	}

	// Null for focusKey means modification errors occurred.
	private EwfResponse getResponse( SpecifiedValue<string> focusKey, int? statusCode = null ) {
		pageTree.PrepareForRendering(
			focusKey,
			focusKey is null
				? condition => condition.ErrorFocusabilitySources.Validations.Any( i => validationsWithErrors.Contains( i ) ) ||
				               ( condition.ErrorFocusabilitySources.IncludeGeneralErrors && requestState.GeneralModificationErrors.Any() )
				: condition => condition.IsNormallyFocusable );

		return EwfResponse.Create(
			ContentTypes.Html,
			new EwfResponseBodyCreator(
				writer => {
					AuthenticationStatics.UpdateFormsAuthCookieIfNecessary();
					pageTree.WriteMarkup( writer );
				} ),
			statusCodeGetter: () => statusCode,
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
	}

	internal bool ModificationErrorsOccurred => modificationErrorsExist && !postBackValidationDmExecuted;

	private bool modificationErrorsExist => requestState.InLineModificationErrorsByDisplay.Any() || requestState.GeneralModificationErrors.Any();

	/// <summary>
	/// The desired scroll position of the browser when this response is received.
	/// </summary>
	protected virtual ScrollPosition scrollPositionForThisResponse => ScrollPosition.LastPositionOrStatusBar;

	/// <summary>
	/// Gets the function call that should be executed when the jQuery document ready event is fired.
	/// </summary>
	protected virtual string javaScriptDocumentReadyFunctionCall => "";

	public sealed override bool MatchesCurrent() => Equals( Current );

	/// <summary>
	/// Creates a page object using the new parameter value fields in this page.
	/// </summary>
	protected abstract PageBase reCreateFromNewParameterValues();
}