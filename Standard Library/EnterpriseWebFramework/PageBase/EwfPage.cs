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
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayLinking;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.JavaScriptWriting;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebFileSending;
using RedStapler.StandardLibrary.WebSessionState;
using StackExchange.Profiling;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	/// <summary>
	/// A System.Web.UI.Page that contains special Red Stapler Enterprise Web Framework logic. Requires that view state and session state be enabled.
	/// </summary>
	public abstract class EwfPage: Page {
		internal const string EventPostBackArgument = "event";

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
		private readonly DataModification postBackDataModification = new DataModification();
		private readonly Queue<EtherealControl> etherealControls = new Queue<EtherealControl>();
		private readonly List<Tuple<WebControl, Control>> postBackOnEnterControlsAndTargets = new List<Tuple<WebControl, Control>>();
		private readonly Dictionary<Validation, List<string>> modErrorDisplaysByValidation = new Dictionary<Validation, List<string>>();
		private readonly List<DisplayLink> displayLinks = new List<DisplayLink>();
		private string formControlHash;
		private string formControlHashWithValues;
		private PageInfo redirectInfo;
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
			AppRequestState.Instance.EnableCache();

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
					NetTools.Redirect( InfoAsBaseType.GetUrl( false, true ) );
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
			if( !IsPostBack ) {
				if( AppRequestState.Instance.EwfPageRequestState == null ) {
					AppRequestState.Instance.EwfPageRequestState = StandardLibrarySessionState.Instance.EwfPageRequestState ??
					                                               new EwfPageRequestState( PageState.CreateForNewPage(), null, null );
					StandardLibrarySessionState.Instance.EwfPageRequestState = null;
				}
				else
					PageState.ClearCustomStateControlKeys();

				onLoadData();

				if( AppRequestState.Instance.EwfPageRequestState.StaticFormControlHash != null &&
				    generateFormControlHash( true, false ) != AppRequestState.Instance.EwfPageRequestState.StaticFormControlHash ) {
					var sentences = new[]
					                	{
					                		"Possible developer mistake.",
					                		"Form controls, modification error display keys, and post back event handlers may not change on a post back with modification errors.",
					                		"There is a chance that this was caused by something outside the request, but it's more likely that a developer incorrectly modified something."
					                	};
					throw new ApplicationException( StringTools.ConcatenateWithDelimiter( " ", sentences ) );
				}

				formControlHash = generateFormControlHash( false, true );
				formControlHashWithValues = generateFormControlHash( true, true );
			}
		}

		/// <summary>
		/// Loads hidden field state. We use this instead of LoadViewState because the latter doesn't get called during post backs on which the page structure
		/// changes.
		/// </summary>
		protected override sealed object LoadPageStateFromPersistenceMedium() {
			// Based on our implementation of SavePageStateFromPersistenceMedium, the base implementation of LoadPageStateFromPersistenceMedium will return a Pair
			// with no First object.
			var pair = base.LoadPageStateFromPersistenceMedium() as Pair;

			var savedState = PageState.CreateFromViewState( (object[])pair.Second );
			AppRequestState.Instance.EwfPageRequestState = new EwfPageRequestState( savedState.Item1,
			                                                                        Request.Form[ "__SCROLLPOSITIONX" ],
			                                                                        Request.Form[ "__SCROLLPOSITIONY" ] );
			formControlHash = (string)savedState.Item2[ 0 ];
			formControlHashWithValues = (string)savedState.Item2[ 1 ];
			var standardSavedState = savedState.Item2[ 2 ];

			onLoadData();


			// Compare form control hashes to make sure data didn't change under this page's feet since the last request.

			// NOTE: Eliminate the form control hash [without values] and replace this check with a verification of one-to-one mapping between form controls and
			// Request.Form items. From Request.Form, we must exclude built in ASP.NET items, e.g. __VIEWSTATE, as well as extraneous items from certain Telerik
			// controls, such as RadToolTip and RadDatePicker, that don't correspond to form controls in our model. Also, some of our own form controls don't provide
			// a way to access the UniqueID of the actual form element, which is needed since it is equivalent to the name attribute and therefore is used as the key
			// in Request.Form.
			if( generateFormControlHash( false, true ) != formControlHash ) {
				AppRequestState.Instance.EwfPageRequestState.TopModificationErrors = Translation.AnotherUserHasModifiedPageBad.ToSingleElementArray();
				resetPage();
			}

			var newFormControlHashWithValues = generateFormControlHash( true, true );
			if( newFormControlHashWithValues != formControlHashWithValues ) {
				AppRequestState.Instance.EwfPageRequestState.TopModificationErrors = Translation.AnotherUserHasModifiedPageGoodHtml.ToSingleElementArray();

				// Update the hash variable with the value from this request since the user has now been warned that changes have been made under their feet.
				formControlHashWithValues = newFormControlHashWithValues;
			}
			else {
				// This logic can go anywhere between here and the first EH method call, which is in OnLoad. It's convenient to have it here since it should only run on
				// post backs and so it can be inside the else block.
				if( isEventPostBack && !( FindControl( Request.Form[ "__EVENTTARGET" ] ) is IPostBackEventHandler ) ) {
					AppRequestState.Instance.EwfPageRequestState.TopModificationErrors =
						Translation.AnotherUserHasModifiedPageAndWeCouldNotInterpretAction.ToSingleElementArray();

					// There is no need to explicitly cancel the post back event since ASP.NET will not be able to do anything anyway if it can't find an
					// IPostBackEventHandler that corresponds to the __EVENTTARGET.
				}
			}


			return standardSavedState;
		}

		/// <summary>
		/// This needs to be called after the page state dictionary has been created or restored.
		/// </summary>
		private void onLoadData() {
			// This can go anywhere in the lifecycle.

			// We need this header for two reasons. The most important reason is that without it, certain sites (such as MIT sites) will be forced into compatibility mode due to
			// the Compatibility View Blacklist maintained by Microsoft. Also, this prevents future versions of IE from rendering things differently before we get a chance to
			// check it and update the UI.
			Response.AppendHeader( "X-UA-Compatible", "IE=9" );

			addMetadataAndFaviconLinks();
			addTypekitLogicIfNecessary();
			addStyleSheetLinks();
			addModernizrLogic();
			addGoogleAnalyticsLogicIfNecessary();
			addJavaScriptIncludes();


			// Set the page title. This should be done before LoadData to support pages or entity setups that want to set their own title.
			Title = StringTools.ConcatenateWithDelimiter( " - ",
			                                              EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName,
			                                              PageInfo.CombinePagePathStrings( PageInfo.PagePathSeparator,
			                                                                               InfoAsBaseType.ParentPageEntityPathString,
			                                                                               InfoAsBaseType.PageFullName ) );

			if( EsAsBaseType != null )
				EsAsBaseType.LoadData( AppRequestState.PrimaryDatabaseConnection );
			LoadData( AppRequestState.PrimaryDatabaseConnection );
			loadDataForControlAndChildren( AppRequestState.PrimaryDatabaseConnection, this );

			// It's important to handle new ethereal controls getting added during this loop.
			var etherealControlsForJsStartUpLogic = new List<EtherealControl>();
			while( etherealControls.Any() ) {
				var etherealControl = etherealControls.Dequeue();

				// This is kind of a hack, but it's an easy way to make sure ethereal controls are initially hidden.
				etherealControl.Control.Style.Add( HtmlTextWriterStyle.Display, "none" );

				Form.Controls.Add( etherealControl.Control );
				loadDataForControlAndChildren( AppRequestState.PrimaryDatabaseConnection, etherealControl.Control );
				etherealControlsForJsStartUpLogic.Add( etherealControl );
			}

			// Clear and disable further caching to prevent later methods (such as event handlers) from getting cached data when they want fresh data.
			AppRequestState.Instance.ClearAndDisableCache();

			var submitButtons = getSubmitButtons( this );
			if( submitButtons.Count > 1 ) {
				var helpfulMessage = "Multiple buttons with submit behavior were detected. There may only be one per page. The button IDs are " +
				                     StringTools.ConcatenateWithDelimiter( ", ", submitButtons.Select( control => control.UniqueID ).ToArray() ) + ".";
				if( AppTools.IsDevelopmentInstallation )
					throw new ApplicationException( helpfulMessage );
				AppTools.EmailAndLogError( helpfulMessage, null );
			}
			var submitButton = submitButtons.FirstOrDefault();

			foreach( var controlAndTarget in postBackOnEnterControlsAndTargets.Where( i => i.Item2 != null || submitButton != null ) ) {
				controlAndTarget.Item1.AddJavaScriptEventScript( JsWritingMethods.onkeypress,
				                                                 "if(event.which == 13) { " +
				                                                 PostBackButton.GetPostBackScript( controlAndTarget.Item2 ?? submitButton, true ) + "; }" );
			}

			// This must be after LoadData is called on all controls since certain logic, e.g. setting the focused control, can depend on the results of LoadData.
			addJavaScriptStartUpLogic( submitButton, etherealControlsForJsStartUpLogic, !AppRequestState.Instance.EwfPageRequestState.ModificationErrorsExist );

			// This must be after LoadData is called on all controls. AddJavaScript shouldn't be called during AddDisplayLink since
			// FreeFormRadioListToControlArrayDisplayLink's script must be built after all items are added to the list.
			foreach( var displayLink in displayLinks )
				displayLink.AddJavaScript();
		}

		private void addMetadataAndFaviconLinks() {
			Header.Controls.Add( new HtmlMeta
			                     	{
			                     		Name = "application-name",
			                     		Content = EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : AppTools.SystemName
			                     	} );

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
				Header.Controls.Add( new Literal
				                     	{
				                     		Text =
				                     			"<script type=\"text/javascript\" src=\"http" + ( Request.IsSecureConnection ? "s" : "" ) + "://use.typekit.com/" +
				                     			EwfApp.Instance.TypekitId + ".js\"></script>"
				                     	} );
				Header.Controls.Add( new Literal { Text = "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" } );
			}
		}

		private void addStyleSheetLinks() {
			var styleSheetLinks = new List<HtmlLink>();

			foreach( var info in EwfApp.MetaLogicFactory.GetDisplayMediaCssInfos() )
				addStyleSheetLink( styleSheetLinks, info, null );

			foreach( var cssInfo in EwfApp.Instance.GetStyleSheets() )
				addStyleSheetLink( styleSheetLinks, cssInfo, null );

			foreach( var info in EwfApp.MetaLogicFactory.GetPrintMediaCssInfos() )
				addStyleSheetLink( styleSheetLinks, info, "print" );

			foreach( var i in styleSheetLinks )
				Header.Controls.Add( i );
		}

		private void addStyleSheetLink( List<HtmlLink> styleSheetLinks, CssInfo cssInfo, string mediaType ) {
			var l = new HtmlLink { Href = this.GetClientUrl( cssInfo.GetUrl() ) };
			l.Attributes.Add( "rel", "stylesheet" );
			l.Attributes.Add( "type", "text/css" );
			if( !mediaType.IsNullOrWhiteSpace() )
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
			// NOTE: Consider using Google's copy; see http://code.google.com/apis/libraries/devguide.html. Use the latest version of major version numbers instead of
			// specific versions. Keep in mind that we can't use Google or any other CDN for some of the other files since they are customized versions.
			ClientScript.RegisterClientScriptInclude( GetType(), "jQuery", this.GetClientUrl( "~/Ewf/jQuery.js" ) );

			ClientScript.RegisterClientScriptInclude( GetType(), "jQuery UI", this.GetClientUrl( "~/Ewf/JQueryUi/js/jquery-ui-1.8.18.custom.min.js" ) );
			ClientScript.RegisterClientScriptInclude( GetType(), "timePicker", this.GetClientUrl( "~/Ewf/TimePicker/JavaScript.js" ) );

			// NOTE: Delete first line and uncomment second line when we switch to qTip2.
			ClientScript.RegisterClientScriptInclude( GetType(), "qTip", this.GetClientUrl( "~/Ewf/QTip/jquery.qtip-1.0.0-rc3.min.js" ) );
			//ClientScript.RegisterClientScriptInclude( GetType(), "qTip2", this.GetClientUrl( "~/Ewf/QTip2/jquery.qtip.min.js" ) );

			// From http://stackoverflow.com/a/2548133/35349.
			ClientScript.RegisterClientScriptBlock( GetType(),
			                                        "endsWith",
			                                        "function endsWith( str, suffix ) { return str.indexOf( suffix, str.length - suffix.length ) !== -1; }",
			                                        true );

			ClientScript.RegisterClientScriptBlock( GetType(),
			                                        "CKEditor GetUrl",
			                                        "function CKEDITOR_GETURL( resource ) { if( endsWith( resource, '.css' ) ) return resource.substring( 0, resource.length - 4 ) + '" +
			                                        CssHandler.GetFileVersionString( WysiwygHtmlEditor.CkEditorInstallationDate ) + ".css'; }",
			                                        true );
			ClientScript.RegisterClientScriptInclude( GetType(), "CKEditor Main", this.GetClientUrl( "~/" + WysiwygHtmlEditor.CkEditorFolderUrl + "/ckeditor.js" ) );
			ClientScript.RegisterClientScriptInclude( GetType(),
			                                          "CKEditor jQuery Adapter",
			                                          this.GetClientUrl( "~/" + WysiwygHtmlEditor.CkEditorFolderUrl + "/adapters/jquery.js" ) );
			ClientScript.RegisterClientScriptBlock( GetType(), "stackExchangeMiniProfiler", MiniProfiler.RenderIncludes().ToHtmlString(), false );
			ClientScript.RegisterClientScriptInclude( GetType(), "ewfJsFile", this.GetClientUrl( "~/Ewf/JavaScript.js" ) );
			foreach( var url in EwfApp.Instance.GetJavaScriptFileUrls() )
				ClientScript.RegisterClientScriptInclude( GetType(), "systemSpecificFile" + url, this.GetClientUrl( url ) );
		}

		/// <summary>
		/// Loads and displays data on the page. This is a replacement for the Init event that provides access to EWF page state.
		/// </summary>
		protected abstract void LoadData( DBConnection cn );

		private static void loadDataForControlAndChildren( DBConnection cn, Control control ) {
			if( control is ControlTreeDataLoader )
				( control as ControlTreeDataLoader ).LoadData( cn );
			foreach( Control child in control.Controls )
				loadDataForControlAndChildren( cn, child );
		}

		/// <summary>
		/// Gets the page's post back data modification, which executes on every post back. Do *not* use this for modifications that should happen as a result of
		/// clicking a particular button, e.g. adding a new item to the database. Instead, create your own data modification and pass it to your post back button.
		/// There are two reasons for this. First, there may be other post back controls such as buttons or lookup boxes on the page, any of which could cause the
		/// post back data modification to execute. Second, the post back data modification currently only runs if form controls were modified, which would not be
		/// the case if a user clicks the button on an add item page before entering any data.
		/// </summary>
		public DataModification PostBackDataModification { get { return postBackDataModification; } }

		internal void AddEtherealControl( EtherealControl etherealControl ) {
			etherealControls.Enqueue( etherealControl );
		}

		/// <summary>
		/// Adds a display mapping to this page.
		/// </summary>
		internal void AddDisplayLink( DisplayLink displayLink ) {
			displayLinks.Add( displayLink );
		}

		/// <summary>
		/// Causes the specified control to submit the form when the enter key is pressed while the control has focus. Specify null for the target to give the event
		/// to the submit button, which you should do if you want the post back to simulate the user clicking the button. If you specify a non null target, it must
		/// be a post back event handler. If you specify a post back button, its UsesSubmitBehavior property will be set to false.
		/// </summary>
		internal void MakeControlPostBackOnEnter( WebControl control, Control target ) {
			postBackOnEnterControlsAndTargets.Add( Tuple.Create( control, target ) );

			// NOTE: This seems like a hack. Why can't people just tell their buttons not to use submit behavior?
			if( target is PostBackButton )
				( target as PostBackButton ).UsesSubmitBehavior = false;
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

			// We want to suppress all of the exceptions that could happen, such as the key not existing in the dictionary. This problem will be shown in a more
			// helpful way when we compare form control hashes after a transfer.
			try {
				return AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay[ key ];
			}
			catch {
				return new string[ 0 ];
			}
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
		public IEnumerable<Tuple<StatusMessageType, string>> StatusMessages { get { return StandardLibrarySessionState.Instance.StatusMessages.Concat( statusMessages ); } }

		private static List<Control> getSubmitButtons( Control control ) {
			var submitButtons = new List<Control>();
			if( control.Visible &&
			    ( ( control is PostBackButton && ( control as PostBackButton ).UsesSubmitBehavior ) || ( control is Button && ( control as Button ).UseSubmitBehavior ) ) )
				submitButtons.Add( control );
			foreach( Control childControl in control.Controls )
				submitButtons.AddRange( getSubmitButtons( childControl ) );
			return submitButtons;
		}

		private void addJavaScriptStartUpLogic( Control submitButton, IEnumerable<EtherealControl> etherealControls, bool noErrorMessagesExist ) {
			// This gives the enter key good behavior with Internet Explorer when there is one text box on the page.
			var eventTargetAndEventArgumentStatements = "";
			if( submitButton != null ) {
				// Force ASP.NET to create __EVENTTARGET and __EVENTARGUMENT hidden fields if it hasn't already.
				PostBackButton.GetPostBackScript( submitButton, true );

				eventTargetAndEventArgumentStatements = "$( 'input#__EVENTTARGET' ).val( '" + submitButton.UniqueID + "' ); $( 'input#__EVENTARGUMENT' ).val( '" +
				                                        EventPostBackArgument + "' );";
			}

			var controlInitStatements =
				getImplementersWithinControl<ControlWithJsInitLogic>( this ).Cast<ControlWithJsInitLogic>().Select( i => i.GetJsInitStatements() ).Concat(
					etherealControls.Select( i => i.GetJsInitStatements() ) ).Aggregate( ( a, b ) => a + b );

			var statusMessageDialogFadeOutStatement = "";
			if( StatusMessages.Any() && !StatusMessages.Any( i => i.Item1 == StatusMessageType.Warning ) )
				statusMessageDialogFadeOutStatement = "setTimeout( 'fadeOutStatusMessageDialog( 400 );', " + StatusMessages.Count() * 1000 + " );";

			MaintainScrollPositionOnPostBack = true;
			var scroll = scrollPositionForThisResponse == ScrollPosition.LastPositionOrStatusBar && noErrorMessagesExist;

			// If a transfer happened on this request and we're on the same page and we want to scroll, get coordinates from the per-request data in EwfApp.
			var requestState = AppRequestState.Instance.EwfPageRequestState;
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

			ClientScript.RegisterClientScriptBlock( GetType(),
			                                        "jQueryDocumentReadyBlock",
			                                        "$( document ).ready( function() { " +
			                                        StringTools.ConcatenateWithDelimiter( " ",
			                                                                              eventTargetAndEventArgumentStatements,
			                                                                              "OnDocumentReady();",
			                                                                              controlInitStatements,
			                                                                              statusMessageDialogFadeOutStatement,
			                                                                              EwfApp.Instance.JavaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
			                                                                              javaScriptDocumentReadyFunctionCall.AppendDelimiter( ";" ),
			                                                                              StringTools.ConcatenateWithDelimiter( " ",
			                                                                                                                    scrollStatement,
			                                                                                                                    locationReplaceStatement ).
			                                                                              	PrependDelimiter( "window.onload = function() { " ).AppendDelimiter( " };" ) ) +
			                                        " } );",
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
		protected virtual Control controlWithInitialFocus { get { return getImplementersWithinControl<FormControl>( contentContainer ).FirstOrDefault(); } }

		/// <summary>
		/// Raises the Load event.
		/// </summary>
		protected override sealed void OnLoad( EventArgs e ) {
			base.OnLoad( e );

			if( IsPostBack ) {
				foreach( var formControl in getChildFormControls( this ) )
					formControl.AddPostBackValueToDictionary( AppRequestState.Instance.EwfPageRequestState.PostBackValues );
			}

			// NOTE: This should probably move earlier in the life cycle and become a part of onLoadData.
			foreach( var displayLink in displayLinks )
				displayLink.SetInitialDisplay();

			// This must happen after LoadData and before modifications are executed.
			statusMessages.Clear();

			if( IsPostBack && ( this is PostBackDataModifier || postBackDataModification.ContainsAnyValidationsOrModifications() ) &&
			    getChildFormControls( this ).Any( fc => fc.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) ) ) {
				var validationMethod = new Action<Validator>( validator => {
					if( this is PostBackDataModifier )
						( this as PostBackDataModifier ).ValidateFormValues( validator );
					ExecuteDataModificationValidations( postBackDataModification, validator );
				} );
				var modificationMethod = new DbMethod( cn => {
					if( this is PostBackDataModifier )
						( this as PostBackDataModifier ).ModifyData( cn );
					postBackDataModification.ModifyData( cn );
				} );
				EhValidateAndModifyData( validationMethod, modificationMethod );
			}
		}

		/// <summary>
		/// Notifies the server control that caused the postback that it should handle an incoming postback event.
		/// </summary>
		protected override void RaisePostBackEvent( IPostBackEventHandler sourceControl, string eventArgument ) {
			// NOTE: Remove this when we've eradicated these controls from all systems.
			var legacyControl = sourceControl is Button || sourceControl is LinkButton;

			if( isEventPostBack || legacyControl )
				base.RaisePostBackEvent( sourceControl, eventArgument );
		}

		// Filtering post backs with __EVENTARGUMENT is a hack, but is necessary because not all post backs are associated with a server side event. For example, a
		// drop down list may trigger a post back when the selected item changes, but the server side control is not a post back event handler and therefore does
		// not want to receive an event specifically because of a post back. It may be interested in a "selected item changed" event, but that type of event has
		// nothing to do with post backs.
		//
		// We would have preferred to represent *non event* post backs with an empty __EVENTTARGET instead of representing *event* post backs with a special
		// __EVENTARGUMENT. The former solution would have eliminated the need for this property as well as the override of RaisePostBackEvent, but wasn't practical
		// since the GetPostBackEventReference method requires a control to be passed.
		private bool isEventPostBack { get { return Request.Form[ "__EVENTARGUMENT" ] == EventPostBackArgument; } }

		internal void ExecuteDataModificationValidations( DataModification dataModification, Validator topValidator ) {
			dataModification.ValidateFormValues( topValidator,
			                                     ( validation, errorMessages ) => {
			                                     	if( !modErrorDisplaysByValidation.ContainsKey( validation ) || !errorMessages.Any() )
			                                     		return;
			                                     	foreach( var displayKey in modErrorDisplaysByValidation[ validation ] ) {
			                                     		var errorsByDisplay = AppRequestState.Instance.EwfPageRequestState.InLineModificationErrorsByDisplay;
			                                     		errorsByDisplay[ displayKey ] = errorsByDisplay.ContainsKey( displayKey )
			                                     		                                	? errorsByDisplay[ displayKey ].Concat( errorMessages )
			                                     		                                	: errorMessages;
			                                     	}
			                                     } );
		}

		/// <summary>
		/// Executes a method unless there was a primary modification that failed earlier in the postback.
		/// </summary>
		public void EhExecute( Action method ) {
			if( AppRequestState.Instance.EwfPageRequestState.ModificationErrorsExist )
				return;

			try {
				method();
			}
			catch( EwfException e ) {
				AppRequestState.Instance.RollbackDatabaseTransactions();
				AppRequestState.Instance.EwfPageRequestState.TopModificationErrors = e.Messages;
			}
		}

		/// <summary>
		/// Performs a secondary modification unless there was a primary modification that failed earlier in the postback.
		/// </summary>
		public void EhModifyData( DbMethod modificationMethod ) {
			EhValidateAndModifyData( delegate { }, modificationMethod );
		}

		/// <summary>
		/// Performs a secondary validation/modification unless there was a primary modification that failed earlier in the postback.
		/// </summary>
		public void EhValidateAndModifyData( Action<Validator> validationMethod, DbMethod modificationMethod ) {
			EhExecute( delegate { executeModification( validationMethod, modificationMethod ); } );
		}

		/// <summary>
		/// Performs a secondary modification and redirects to the URL returned by the modification method unless there was a primary modification that failed earlier in the postback.
		/// </summary>
		public void EhModifyDataAndRedirect( RedirectingDbMethod method ) {
			EhValidateAndModifyDataAndRedirect( delegate { }, method );
		}

		/// <summary>
		/// Performs a secondary validation/modification and redirects to the URL returned by the modification method unless there was a primary modification that failed earlier in the postback.
		/// </summary>
		public void EhValidateAndModifyDataAndRedirect( Action<Validator> validationMethod, RedirectingDbMethod modificationMethod ) {
			EhExecute( () => executeModification( validationMethod,
			                                      cn => {
			                                      	var url = modificationMethod( cn ) ?? "";
			                                      	redirectInfo = url.Any() ? new ExternalPageInfo( url ) : null;
			                                      } ) );
		}

		private static void executeModification( Action<Validator> validationMethod, DbMethod modificationMethod ) {
			var validator = new Validator();
			validationMethod( validator );
			if( validator.ErrorsOccurred )
				throw new EwfException( Translation.PleaseCorrectTheErrorsShownBelow.ToSingleElementArray().Concat( validator.ErrorMessages ).ToArray() );
			modificationMethod( AppRequestState.PrimaryDatabaseConnection );
		}

		/// <summary>
		/// Redirects to the specified page unless a modification fails during the post back.
		/// Passing null for pageInfo will result in no redirection.
		/// </summary>
		public void EhRedirect( PageInfo pageInfo ) {
			EhExecute( () => redirectInfo = pageInfo );
		}

		/// <summary>
		/// Sets up a client side redirect to the file created by the specified file creator.
		/// NOTE: Rename to EhSendFile?
		/// </summary>
		public void EhModifyDataAndSendFile( FileCreator fileCreator ) {
			EhExecute( delegate { StandardLibrarySessionState.Instance.FileToBeDownloaded = fileCreator.CreateFile(); } );
		}

		/// <summary>
		/// Returns a list of all form controls in the page that were modified during this post back. This method will probably be removed; don't use it. It is only
		/// used by the Edit Task page in RSIS.
		/// </summary>
		public bool ModifiedFormControlsExistWithinContentContainer() {
			return getChildFormControls( contentContainer ).Any( fc => fc.ValueChangedOnPostBack( AppRequestState.Instance.EwfPageRequestState.PostBackValues ) );
		}

		/// <summary>
		/// Sets the focus to the specified control. Call this only during event handlers, and use the controlWithInitialFocus property instead if you wish to set
		/// the focus to the same control on all requests. Do not call this during LoadData; it uses the UniqueID of the specified control, which may not be defined
		/// in LoadData if the control hasn't been added to the page.
		/// </summary>
		public new void SetFocus( Control control ) {
			AppRequestState.Instance.EwfPageRequestState.ControlWithInitialFocusId = control.UniqueID;
		}

		/// <summary>
		/// Terminates and restarts page execution instead of performing normal PreRender activities if this is a postback and data modifications were completed
		/// earlier in the life cycle.
		/// </summary>
		protected override sealed void OnPreRender( EventArgs eventArgs ) {
			var requestState = AppRequestState.Instance;

			if( !requestState.EwfPageRequestState.ModificationErrorsExist ) {
				// This call to PreExecuteCommitTimeValidationMethods catches errors caused by post back modifications.
				try {
					requestState.PreExecuteCommitTimeValidationMethodsForAllOpenConnections();
				}
				catch( EwfException e ) {
					requestState.RollbackDatabaseTransactions();
					requestState.EwfPageRequestState.TopModificationErrors = e.Messages;
				}
			}

			if( IsPostBack ) {
				if( !requestState.EwfPageRequestState.ModificationErrorsExist ) {
					requestState.CommitDatabaseTransactionsAndExecuteNonTransactionalModificationMethods();
					StandardLibrarySessionState.Instance.StatusMessages.AddRange( statusMessages );
					requestState.EwfPageRequestState.PostBackValues = new PostBackValueDictionary( new Dictionary<string, object>() );
				}
				else
					requestState.EwfPageRequestState.StaticFormControlHash = generateFormControlHash( true, false );

				// Determine the final redirect destination. If a destination is already specified and it is the current page or a page with the same entity setup,
				// replace any default optional parameter values it may have with new values from this post back. If a destination isn't specified, make it the current
				// page with new parameter values from this post back. At the end of this block, redirectInfo is always newly created with fresh data that reflects any
				// changes that may have occurred in EH methods. It's important that every case below *actually creates* a new page info object to guard against this
				// scenario:
				// 1. A page modifies data such that a previously created redirect destination page info object that is then used here is no longer valid because it
				//    would throw an exception from init if it were re-created.
				// 2. The page redirects, or transfers, to this destination, leading the user to an error page without developers being notified. This is bad behavior.
				if( requestState.EwfPageRequestState.ModificationErrorsExist )
					redirectInfo = InfoAsBaseType.CloneAndReplaceDefaultsIfPossible( true );
				else if( redirectInfo != null )
					redirectInfo = redirectInfo.CloneAndReplaceDefaultsIfPossible( false );
				else
					redirectInfo = createInfoFromNewParameterValues();

				// If the redirect destination is identical to the current page, do a transfer instead of a redirect.
				if( redirectInfo.IsIdenticalToCurrent() ) {
					// Force developers to get an error email if a page modifies data to invalidate itself without specifying a different page as the redirect
					// destination. The resulting transfer would lead the user to an error page.
					// An alternative to this GetUrl call is to detect in initEntitySetupAndCreateInfoObjects if we are on the back side of a transfer and make all
					// exceptions unhandled. This would be harder to implement and has no benefits over the approach here.
					redirectInfo.GetUrl();

					requestState.ClearUser();
					resetPage();
				}

				// If the redirect destination is the current page, but with different query parameters, save page state and scroll position in session state until the
				// next request.
				if( redirectInfo.GetType() == InfoAsBaseType.GetType() )
					StandardLibrarySessionState.Instance.EwfPageRequestState = requestState.EwfPageRequestState;

				NetTools.Redirect( redirectInfo.GetUrl() );
			}

			// Initial request data modifications. All data modifications that happen simply because of a request and require no other action by the user should
			// happen at the end of the life cycle, in PreRender. This prevents modifications from being executed twice when transfers happen. It also prevents any of
			// the modified data from being used accidentally, or intentionally, in LoadData.
			StandardLibrarySessionState.Instance.StatusMessages.Clear();
			StandardLibrarySessionState.Instance.ClearClientSideRedirectUrlAndDelay();
			if( !requestState.EwfPageRequestState.ModificationErrorsExist ) {
				if( AppRequestState.Instance.UserAccessible && AppTools.User != null && !Configuration.Machine.MachineConfiguration.GetIsStandbyServer() ) {
					updateLastPageRequestTimeForUser();
					EwfApp.Instance.ExecuteInitialRequestDataModifications( AppRequestState.PrimaryDatabaseConnection );
				}

				// This call to PreExecuteCommitTimeValidationMethods catches errors caused by initial request data modifications.
				requestState.PreExecuteCommitTimeValidationMethodsForAllOpenConnections();
			}

			base.OnPreRender( eventArgs );
		}

		private string generateFormControlHash( bool includeValues, bool forConcurrencyCheck ) {
			var formControlString = new StringBuilder();
			foreach( var formControl in getChildFormControls( this ) ) {
				formControlString.Append( formControl.GetType().ToString() );
				formControlString.Append( ( formControl as Control ).UniqueID );
				if( includeValues )
					formControlString.Append( formControl.DurableValueAsString );
			}

			if( !forConcurrencyCheck && AppRequestState.Instance.EwfPageRequestState.ModificationErrorsExist ) {
				// Include mod error display keys. They shouldn't change across a transfer when there are modification errors because that could prevent some of the
				// errors from being displayed.
				foreach( var modErrorDisplayKey in modErrorDisplaysByValidation.Values.SelectMany( i => i ) )
					formControlString.Append( modErrorDisplayKey + " " );

				// It's probably bad if a developer puts a PostBackButton or other IPostBackEventHandler on the page because of a modification error. It will be gone on
				// the post back and cannot be processed.
				foreach( var postBackEventHandler in getImplementersWithinControl<IPostBackEventHandler>( this ) ) {
					formControlString.Append( postBackEventHandler.GetType().ToString() );
					formControlString.Append( postBackEventHandler.UniqueID );
				}
			}

			var hash = new MD5CryptoServiceProvider().ComputeHash( Encoding.ASCII.GetBytes( formControlString.ToString() ) );
			var hashString = "";
			foreach( var b in hash )
				hashString += b.ToString( "x2" );
			return hashString;
		}

		private static IEnumerable<FormControl> getChildFormControls( Control control ) {
			return getImplementersWithinControl<FormControl>( control ).Cast<FormControl>();
		}

		private static IEnumerable<Control> getImplementersWithinControl<InterfaceType>( Control control ) {
			var matchingControls = new List<Control>();
			foreach( Control childControl in control.Controls ) {
				if( childControl is InterfaceType )
					matchingControls.Add( childControl );
				matchingControls.AddRange( getImplementersWithinControl<InterfaceType>( childControl ) );
			}
			return matchingControls;
		}

		/// <summary>
		/// Creates a page info object using the new parameter value fields in this page.
		/// </summary>
		protected abstract PageInfo createInfoFromNewParameterValues();

		private void resetPage() {
			Server.Transfer( Request.AppRelativeCurrentExecutionFilePath );
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

			var formsAuthProvider = UserManagementStatics.SystemProvider as FormsAuthCapableUserManagementProvider;
			var externalAuthProvider = UserManagementStatics.SystemProvider as ExternalAuthUserManagementProvider;

			// Now we want to do a timestamp-based concurrency check so we don't update the last login date if we know another transaction already did.
			// It is not perfect, but it reduces errors caused by one user doing a long-running request and then doing smaller requests
			// in another browser window while the first one is still running.			
			User newlyQueriedUser = null;

			// We have to query in a separate transaction because otherwise snapshot isolation will result in us always getting the original LastRequestDatetime, even if
			// another transaction has modified its value during this transaction.
			AppTools.ExecuteInDbConnection( cn => {
				try {
					// NOTE: Why do I not know that it's going to be one provider or the other?
					// NOTE: We could make a GetUserAsBaseType method in the base interface
					if( formsAuthProvider != null )
						newlyQueriedUser = formsAuthProvider.GetUser( cn, AppTools.User.UserId );
					else if( externalAuthProvider != null )
						newlyQueriedUser = externalAuthProvider.GetUser( cn, AppTools.User.UserId );
				}
				catch {
					// If we can't get the user for any reason, we don't really care. We'll just not do the update.
				}
			} );

			if( newlyQueriedUser == null || newlyQueriedUser.LastRequestDateTime > AppTools.User.LastRequestDateTime )
				return;

			try {
				if( formsAuthProvider != null ) {
					var formsAuthCapableUser = AppTools.User as FormsAuthCapableUser;
					formsAuthProvider.InsertOrUpdateUser( AppRequestState.PrimaryDatabaseConnection,
					                                      AppTools.User.UserId,
					                                      AppTools.User.Email,
					                                      formsAuthCapableUser.Salt,
					                                      formsAuthCapableUser.SaltedPassword,
					                                      AppTools.User.Role.RoleId,
					                                      DateTime.Now,
					                                      formsAuthCapableUser.MustChangePassword );
				}
				else if( externalAuthProvider != null ) {
					externalAuthProvider.InsertOrUpdateUser( AppRequestState.PrimaryDatabaseConnection,
					                                         AppTools.User.UserId,
					                                         AppTools.User.Email,
					                                         AppTools.User.Role.RoleId,
					                                         DateTime.Now );
				}
			}
			catch( DbConcurrencyException ) {
				// Since this method is called on every page request, concurrency errors are common. They are caused when an authenticated user makes one request and
				// then makes another before ASP.NET has finished processing the first. Since we are only updating the last request date and time, we don't need to get
				// an error email if the update fails.
				AppRequestState.Instance.RollbackDatabaseTransactions();
			}
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
			base.SavePageStateToPersistenceMedium( PageState.GetViewStateArray( new[] { formControlHash, formControlHashWithValues, state } ) );
		}
	}
}