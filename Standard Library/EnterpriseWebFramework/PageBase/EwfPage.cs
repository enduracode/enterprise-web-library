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
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebFileSending;
using RedStapler.StandardLibrary.WebSessionState;
using StackExchange.Profiling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A System.Web.UI.Page that contains special Red Stapler Enterprise Web Framework logic. Requires that view state and session state be enabled.
	/// </summary>
	public abstract class EwfPage: Page {
		// This string is duplicated in the JavaScript file.
		private const string postBackHiddenFieldName = "ewfPostBack";

		internal const string ButtonElementName = "ewfButton";

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
		private readonly BasicDataModification dataUpdate = new BasicDataModification();
		private readonly PostBack dataUpdatePostBack = PostBack.CreateDataUpdate();
		private readonly Queue<EtherealControl> etherealControls = new Queue<EtherealControl>();
		private readonly Dictionary<string, PostBack> postBacksById = new Dictionary<string, PostBack>();
		private readonly List<FormValue> formValues = new List<FormValue>();
		private readonly List<DisplayLink> displayLinks = new List<DisplayLink>();
		private readonly List<UpdateRegionLinker> updateRegionLinkers = new List<UpdateRegionLinker>();
		private readonly Dictionary<Validation, List<string>> modErrorDisplaysByValidation = new Dictionary<Validation, List<string>>();
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
				// NOTE: Also redirect if the domain isn't correct. Probably only do this on GET requests since we don't want to wipe out post backs.
				if( InfoAsBaseType.ShouldBeSecureGivenCurrentRequest != Request.IsSecureConnection )
					NetTools.Redirect( InfoAsBaseType.GetUrl( false, false, true ) );
			}
			finally {
				AppRequestState.Instance.UserDisabledByPage = false;
			}

			// This logic depends on the authenticated user and on page and entity setup info objects.
			if( !InfoAsBaseType.UserCanAccessPageAndAllControls ) {
				throw new AccessDeniedException(
					AppTools.IsIntermediateInstallation && !InfoAsBaseType.IsIntermediateInstallationPublicPage && !AppRequestState.Instance.IntermediateUserExists,
					InfoAsBaseType.LogInPage );
			}

			var disabledMode = InfoAsBaseType.AlternativeMode as DisabledPageMode;
			if( disabledMode != null )
				throw new PageDisabledException( disabledMode.Message );

			if( fileCreator != null )
				fileCreator.CreateFile().WriteToResponse( sendsFileInline );
		}

		/// <summary>
		/// Loads the entity display setup for the page, if one exists.
		/// </summary>
		protected abstract void initEntitySetup();

		/// <summary>
		/// Creates the info object for this page based on the query parameters of the request.
		/// </summary>
		protected abstract void createInfoFromQueryString();

		/// <summary>
		/// Gets the FileCreator for this page. NOTE: We should re-implement this such that the classes that override this are plain old HTTP handlers instead of pages.
		/// </summary>
		protected virtual FileCreator fileCreator { get { return null; } }

		/// <summary>
		/// Gets whether the page sends its file inline or as an attachment. NOTE: We should re-implement this such that the classes that override this are plain old HTTP handlers instead of pages.
		/// </summary>
		protected virtual bool sendsFileInline { get { return true; } }

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

			onLoadData();

			var requestState = AppRequestState.Instance.EwfPageRequestState;
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
							? "Form controls, modification-error-display keys, and post-back IDs may not change if modification errors exist."
							: new[] { SecondaryPostBackOperation.Validate, SecondaryPostBackOperation.ValidateChangesOnly }.Contains( requestState.DmIdAndSecondaryOp.Item2 )
								  ? "Form controls outside of update regions may not change on an intermediate post-back."
								  : "Form controls and post-back IDs may not change during the validation stage of an intermediate post-back." );
				}
			}

			var dmIdAndSecondaryOp = requestState.DmIdAndSecondaryOp;
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
								contentContainer.GetDescendants()
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
					navigate( null );
			}
		}

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
			PageInfo redirectInfo = null;
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
							contentContainer.GetDescendants()
								.OfType<FormControl>()
								.Any( i => i.FormValue != null && i.FormValue.ValueChangedOnPostBack( requestState.PostBackValues ) );
						try {
							dmExecuted |= actionPostBack.Execute(
								formValuesChanged,
								handleValidationErrors,
								postBackAction => {
									if( postBackAction == null )
										return;
									redirectInfo = postBackAction.Page;
									if( postBackAction.File != null )
										StandardLibrarySessionState.Instance.FileToBeDownloaded = postBackAction.File.CreateFile();
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
						catch {
							DataAccessState.Current.ResetCache();
							throw;
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

			navigate( redirectInfo );
			return null;
		}

		/// <summary>
		/// This needs to be called after the page state dictionary has been created or restored.
		/// </summary>
		private void onLoadData() {
			// This can go anywhere in the lifecycle.

			// We need this header for two reasons. The most important reason is that without it, certain sites (such as MIT sites) will be forced into compatibility
			// mode due to the Compatibility View Blacklist maintained by Microsoft. Also, this prevents future versions of IE from rendering things differently
			// before we get a chance to check it and update the UI.
			Response.AppendHeader( "X-UA-Compatible", "IE=10" );

			addMetadataAndFaviconLinks();
			addTypekitLogicIfNecessary();
			addStyleSheetLinks();
			addModernizrLogic();
			addGoogleAnalyticsLogicIfNecessary();
			addJavaScriptIncludes();


			// Set the page title. This should be done before LoadData to support pages or entity setups that want to set their own title.
			Title = StringTools.ConcatenateWithDelimiter(
				" - ",
				EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName,
				PageInfo.CombinePagePathStrings( PageInfo.PagePathSeparator, InfoAsBaseType.ParentPageEntityPathString, InfoAsBaseType.PageFullName ) );

			if( EsAsBaseType != null )
				EsAsBaseType.LoadData();
			loadData();
			loadDataForControlAndChildren( this );

			// It's important to handle new ethereal controls getting added during this loop.
			var etherealControlsForJsStartUpLogic = new List<EtherealControl>();
			while( etherealControls.Any() ) {
				var etherealControl = etherealControls.Dequeue();

				// This is kind of a hack, but it's an easy way to make sure ethereal controls are initially hidden.
				etherealControl.Control.Style.Add( HtmlTextWriterStyle.Display, "none" );

				Form.Controls.Add( etherealControl.Control );
				loadDataForControlAndChildren( etherealControl.Control );
				etherealControlsForJsStartUpLogic.Add( etherealControl );
			}

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
			addJavaScriptStartUpLogic( etherealControlsForJsStartUpLogic );

			// This must happen after LoadData and before modifications are executed.
			statusMessages.Clear();
		}

		private void addMetadataAndFaviconLinks() {
			Header.Controls.Add(
				new HtmlMeta { Name = "application-name", Content = EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName } );

			// Chrome start URL
			Header.Controls.Add( new HtmlMeta { Name = "application-url", Content = this.GetClientUrl( NetTools.HomeUrl ) } );

			// IE9 start URL
			Header.Controls.Add( new HtmlMeta { Name = "msapplication-starturl", Content = this.GetClientUrl( NetTools.HomeUrl ) } );

			if( EwfApp.Instance.FaviconPng48X48Url.Length > 0 ) {
				var link = new HtmlLink { Href = this.GetClientUrl( EwfApp.Instance.FaviconPng48X48Url ) };
				link.Attributes.Add( "rel", "icon" );
				link.Attributes.Add( "sizes", "48x48" );
				Header.Controls.Add( link );
			}

			// rel="shortcut icon" is deprecated and will be replaced with rel="icon".
			if( EwfApp.Instance.FaviconUrl.Length > 0 ) {
				var link = new HtmlLink { Href = this.GetClientUrl( EwfApp.Instance.FaviconUrl ) };
				link.Attributes.Add( "rel", "shortcut icon" );
				Header.Controls.Add( link );
			}
		}

		private void addTypekitLogicIfNecessary() {
			if( EwfApp.Instance.TypekitId.Length > 0 ) {
				Header.Controls.Add(
					new Literal
						{
							Text =
								"<script type=\"text/javascript\" src=\"http" + ( Request.IsSecureConnection ? "s" : "" ) + "://use.typekit.com/" + EwfApp.Instance.TypekitId +
								".js\"></script>"
						} );
				Header.Controls.Add( new Literal { Text = "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" } );
			}
		}

		private void addStyleSheetLinks() {
			var styleSheetLinks = new List<HtmlLink>();

			addStyleSheetLink( styleSheetLinks, "//netdna.bootstrapcdn.com/font-awesome/4.0.1/css/font-awesome.css", "" );
			addStyleSheetLink( styleSheetLinks, "//cdn.jsdelivr.net/qtip2/2.2.0/jquery.qtip.min.css", "" );
			foreach( var info in EwfApp.MetaLogicFactory.GetDisplayMediaCssInfos() )
				addStyleSheetLink( styleSheetLinks, this.GetClientUrl( info.GetUrl() ), "" );

			foreach( var info in EwfApp.Instance.GetStyleSheets() )
				addStyleSheetLink( styleSheetLinks, this.GetClientUrl( info.GetUrl() ), "" );

			foreach( var info in EwfApp.MetaLogicFactory.GetPrintMediaCssInfos() )
				addStyleSheetLink( styleSheetLinks, this.GetClientUrl( info.GetUrl() ), "print" );

			foreach( var i in styleSheetLinks )
				Header.Controls.Add( i );
		}

		private void addStyleSheetLink( List<HtmlLink> styleSheetLinks, string url, string mediaType ) {
			var l = new HtmlLink { Href = url };
			l.Attributes.Add( "rel", "stylesheet" );
			l.Attributes.Add( "type", "text/css" );
			if( mediaType.Any() )
				l.Attributes.Add( "media", mediaType );
			styleSheetLinks.Add( l );
		}

		private void addModernizrLogic() {
			Header.Controls.Add( new Literal { Text = "<script type=\"text/javascript\" src=\"" + this.GetClientUrl( "~/Ewf/Modernizr.js" ) + "\"></script>" } );
		}

		private void addGoogleAnalyticsLogicIfNecessary() {
			if( EwfApp.Instance.GoogleAnalyticsWebPropertyId.Length == 0 )
				return;
			using( var sw = new StringWriter() ) {
				sw.WriteLine( "<script type=\"text/javascript\">" );
				sw.WriteLine( "var _gaq = _gaq || [];" );
				sw.WriteLine( "_gaq.push(['_setAccount', '" + EwfApp.Instance.GoogleAnalyticsWebPropertyId + "']);" );
				sw.WriteLine( "_gaq.push(['_trackPageview']);" );
				sw.WriteLine( "(function() {" );
				sw.WriteLine( "var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;" );
				sw.WriteLine( "ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';" );
				sw.WriteLine( "var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);" );
				sw.WriteLine( "})();" );
				sw.WriteLine( "</script>" );
				Header.Controls.Add( new Literal { Text = sw.ToString() } );
			}
		}

		private void addJavaScriptIncludes() {
			// See https://developers.google.com/speed/libraries/devguide. Keep in mind that we can't use a CDN for some of the other files since they are customized
			// versions.
			ClientScript.RegisterClientScriptInclude( GetType(), "jQuery", "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js" );

			ClientScript.RegisterClientScriptInclude(
				GetType(),
				"jQuery UI",
				this.GetClientUrl( "~/Ewf/ThirdParty/JQueryUi/jquery-ui-1.10.4.custom/js/jquery-ui-1.10.4.custom.min.js" ) );
			ClientScript.RegisterClientScriptInclude( GetType(), "Select2", this.GetClientUrl( "~/Ewf/ThirdParty/Select2/select2-3.4.3/select2.js" ) );
			ClientScript.RegisterClientScriptInclude( GetType(), "timePicker", this.GetClientUrl( "~/Ewf/ThirdParty/TimePicker/JavaScript.js" ) );
			ClientScript.RegisterClientScriptInclude( GetType(), "qTip2", "//cdn.jsdelivr.net/qtip2/2.2.0/jquery.qtip.min.js" );
			ClientScript.RegisterClientScriptInclude( GetType(), "CKEditor", "//cdn.ckeditor.com/4.4.2/full/ckeditor.js" );
			ClientScript.RegisterClientScriptInclude( GetType(), "ChartJs", this.GetClientUrl( "~/Ewf/ThirdParty/ChartJs/Chart.min.js?v=1" ) );
			ClientScript.RegisterClientScriptBlock( GetType(), "stackExchangeMiniProfiler", MiniProfiler.RenderIncludes().ToHtmlString(), false );
			ClientScript.RegisterClientScriptInclude( GetType(), "ewfJsFile", this.GetClientUrl( "~/Ewf/JavaScript.js" ) );
			foreach( var url in EwfApp.Instance.GetJavaScriptFileUrls() )
				ClientScript.RegisterClientScriptInclude( GetType(), "systemSpecificFile" + url, this.GetClientUrl( url ) );
		}

		/// <summary>
		/// Loads and displays data on the page. This is a replacement for the Init event that provides access to EWF page state.
		/// </summary>
		protected virtual void loadData() {}

		private void loadDataForControlAndChildren( Control control ) {
			if( control is ControlTreeDataLoader )
				( control as ControlTreeDataLoader ).LoadData();
			foreach( Control child in control.Controls )
				loadDataForControlAndChildren( child );
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

		internal void AddEtherealControl( EtherealControl etherealControl ) {
			etherealControls.Enqueue( etherealControl );
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
		internal IEnumerable<string> AddModificationErrorDisplayAndGetErrors( Control control, string keySuffix, Validation validation ) {
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
		/// Standard Library use only. Gets the status messages.
		/// </summary>
		public IEnumerable<Tuple<StatusMessageType, string>> StatusMessages {
			get { return StandardLibrarySessionState.Instance.StatusMessages.Concat( statusMessages ); }
		}

		private void addJavaScriptStartUpLogic( IEnumerable<EtherealControl> etherealControls ) {
			var controlInitStatements =
				this.GetDescendants()
					.OfType<ControlWithJsInitLogic>()
					.Select( i => i.GetJsInitStatements() )
					.Concat( etherealControls.Select( i => i.GetJsInitStatements() ) )
					.Aggregate( ( a, b ) => a + b );

			var statusMessageDialogFadeOutStatement = "";
			if( StatusMessages.Any() && StatusMessages.All( i => i.Item1 != StatusMessageType.Warning ) )
				statusMessageDialogFadeOutStatement = "setTimeout( 'fadeOutStatusMessageDialog( 400 );', " + StatusMessages.Count() * 1000 + " );";

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
			string clientSideRedirectUrl;
			int? clientSideRedirectDelay;
			StandardLibrarySessionState.Instance.GetClientSideRedirectUrlAndDelay( out clientSideRedirectUrl, out clientSideRedirectDelay );
			var locationReplaceStatement = "";
			if( clientSideRedirectUrl.Length > 0 ) {
				locationReplaceStatement = "location.replace( '" + this.GetClientUrl( clientSideRedirectUrl ) + "' );";
				if( clientSideRedirectDelay.HasValue )
					locationReplaceStatement = "setTimeout( \"" + locationReplaceStatement + "\", " + clientSideRedirectDelay.Value * 1000 + " );";
			}

			ClientScript.RegisterClientScriptBlock(
				GetType(),
				"jQueryDocumentReadyBlock",
				"$( document ).ready( function() { " +
				StringTools.ConcatenateWithDelimiter(
					" ",
					"OnDocumentReady();",
					controlInitStatements,
					statusMessageDialogFadeOutStatement,
					EwfApp.Instance.JavaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
					StringTools.ConcatenateWithDelimiter( " ", scrollStatement, locationReplaceStatement )
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
		protected virtual Control controlWithInitialFocus { get { return contentContainer.GetDescendants().FirstOrDefault( i => i is FormControl ); } }

		private ApplicationException getPossibleDeveloperMistakeException( string messageSentence ) {
			var sentences = new[]
				{
					"Possible developer mistake.", messageSentence,
					"There is a chance that this was caused by something outside the request, but it's more likely that a developer incorrectly modified something."
				};
			throw new ApplicationException( StringTools.ConcatenateWithDelimiter( " ", sentences ) );
		}

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

			var webFormsHiddenFields = new[] { "__EVENTTARGET", "__EVENTARGUMENT", "__LASTFOCUS", "__VIEWSTATE", "__SCROLLPOSITIONX", "__SCROLLPOSITIONY" };
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

		private void handleValidationErrors( Validation validation, IEnumerable<string> errorMessages ) {
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
				this.GetDescendants( i => !updateRegionControls.Contains( i ) )
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

		private void navigate( PageInfo destination ) {
			var requestState = AppRequestState.Instance.EwfPageRequestState;

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

			// If the redirect destination is identical to the current page, do a transfer instead of a redirect.
			if( destination.IsIdenticalToCurrent() ) {
				// Force developers to get an error email if a page modifies data to invalidate itself without specifying a different page as the redirect
				// destination. The resulting transfer would lead the user to an error page.
				// An alternative to this GetUrl call is to detect in initEntitySetupAndCreateInfoObjects if we are on the back side of a transfer and make all
				// exceptions unhandled. This would be harder to implement and has no benefits over the approach here.
				destination.GetUrl();

				AppRequestState.Instance.ClearUser();
				resetPage();
			}

			// If the redirect destination is the current page, but with different query parameters, save request state in session state until the next request.
			if( destination.GetType() == InfoAsBaseType.GetType() )
				StandardLibrarySessionState.Instance.EwfPageRequestState = requestState;

			NetTools.Redirect( destination.GetUrl() );
		}

		/// <summary>
		/// Creates a page info object using the new parameter value fields in this page.
		/// </summary>
		protected abstract PageInfo createInfoFromNewParameterValues();

		private void resetPage() {
			Server.Transfer( Request.AppRelativeCurrentExecutionFilePath );
		}

		protected override sealed void OnPreRender( EventArgs eventArgs ) {
			base.OnPreRender( eventArgs );

			// Initial request data modifications. All data modifications that happen simply because of a request and require no other action by the user should
			// happen at the end of the life cycle. This prevents modifications from being executed twice when transfers happen. It also prevents any of the modified
			// data from being used accidentally, or intentionally, in LoadData or any other part of the life cycle.
			StandardLibrarySessionState.Instance.StatusMessages.Clear();
			StandardLibrarySessionState.Instance.ClearClientSideRedirectUrlAndDelay();
			DataAccessState.Current.DisableCache();
			try {
				if( !Configuration.Machine.MachineConfiguration.GetIsStandbyServer() ) {
					EwfApp.Instance.ExecuteInitialRequestDataModifications();
					if( AppRequestState.Instance.UserAccessible && AppTools.User != null )
						updateLastPageRequestTimeForUser();
					executeInitialRequestDataModifications();
				}
				FormsAuthStatics.UpdateFormsAuthCookieIfNecessary();

				AppRequestState.Instance.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
			}
			finally {
				DataAccessState.Current.ResetCache();
			}
		}

		/// <summary>
		/// It's important to call this from EwfPage instead of EwfApp because requests for some pages, with their associated images, CSS files, etc., can easily
		/// cause 20-30 server requests, and we only want to update the time stamp once for all of these.
		/// </summary>
		private void updateLastPageRequestTimeForUser() {
			// Only update the request time if it's been more than a minute since we did it last. This can dramatically reduce concurrency issues caused by people
			// rapidly assigning tasks to one another in RSIS or similar situations.
			// NOTE: This makes the comment on line 688 much less important.
			if( ( DateTime.Now - AppTools.User.LastRequestDateTime ) < TimeSpan.FromMinutes( 1 ) )
				return;

			// Now we want to do a timestamp-based concurrency check so we don't update the last login date if we know another transaction already did.
			// It is not perfect, but it reduces errors caused by one user doing a long-running request and then doing smaller requests
			// in another browser window while the first one is still running.
			// We have to query in a separate transaction because otherwise snapshot isolation will result in us always getting the original LastRequestDatetime, even if
			// another transaction has modified its value during this transaction.
			var newlyQueriedUser =
				new DataAccessState().ExecuteWithThis(
					() => DataAccessState.Current.PrimaryDatabaseConnection.ExecuteWithConnectionOpen( () => UserManagementStatics.GetUser( AppTools.User.UserId, false ) ) );
			if( newlyQueriedUser == null || newlyQueriedUser.LastRequestDateTime > AppTools.User.LastRequestDateTime )
				return;

			DataAccessState.Current.PrimaryDatabaseConnection.ExecuteInTransaction(
				() => {
					try {
						var externalAuthProvider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;
						if( FormsAuthStatics.FormsAuthEnabled ) {
							var formsAuthCapableUser = AppTools.User as FormsAuthCapableUser;
							FormsAuthStatics.SystemProvider.InsertOrUpdateUser(
								AppTools.User.UserId,
								AppTools.User.Email,
								AppTools.User.Role.RoleId,
								DateTime.Now,
								formsAuthCapableUser.Salt,
								formsAuthCapableUser.SaltedPassword,
								formsAuthCapableUser.MustChangePassword );
						}
						else if( externalAuthProvider != null )
							externalAuthProvider.InsertOrUpdateUser( AppTools.User.UserId, AppTools.User.Email, AppTools.User.Role.RoleId, DateTime.Now );
					}
					catch( DbConcurrencyException ) {
						// Since this method is called on every page request, concurrency errors are common. They are caused when an authenticated user makes one request and
						// then makes another before ASP.NET has finished processing the first. Since we are only updating the last request date and time, we don't need to
						// get an error email if the update fails.
						throw new DoNotCommitException();
					}
				} );
		}

		/// <summary>
		/// Executes all data modifications that happen simply because of a request and require no other action by the user.
		/// </summary>
		protected virtual void executeInitialRequestDataModifications() {}

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

			var hash = new MD5CryptoServiceProvider().ComputeHash( Encoding.ASCII.GetBytes( formValueString.ToString() ) );
			var hashString = "";
			foreach( var b in hash )
				hashString += b.ToString( "x2" );
			return hashString;
		}
	}
}