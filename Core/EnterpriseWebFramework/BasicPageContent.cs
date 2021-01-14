using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using JetBrains.Annotations;
using StackExchange.Profiling;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public sealed class BasicPageContent: PageContent {
		// Some of these are used by the EWF JavaScript file.
		private static readonly ElementClass topWarningContainerClass = new ElementClass( "ewfTopWarning" );
		private static readonly ElementClass clickBlockerInactiveClass = new ElementClass( "ewfClickBlockerI" );
		private static readonly ElementClass clickBlockerActiveClass = new ElementClass( "ewfClickBlockerA" );
		private static readonly ElementClass processingDialogBlockInactiveClass = new ElementClass( "ewfProcessingDialogI" );
		private static readonly ElementClass processingDialogBlockActiveClass = new ElementClass( "ewfProcessingDialogA" );
		private static readonly ElementClass processingDialogBlockTimeOutClass = new ElementClass( "ewfProcessingDialogTo" );
		private static readonly ElementClass processingDialogProcessingParagraphClass = new ElementClass( "ewfProcessingP" );
		private static readonly ElementClass processingDialogTimeOutParagraphClass = new ElementClass( "ewfTimeOutP" );
		private static readonly ElementClass notificationSectionContainerNotificationClass = new ElementClass( "ewfNotificationN" );
		private static readonly ElementClass notificationSectionContainerDockedClass = new ElementClass( "ewfNotificationD" );
		private static readonly ElementClass notificationSpacerClass = new ElementClass( "ewfNotificationSpacer" );
		private static readonly ElementClass infoMessageContainerClass = new ElementClass( "ewfInfoMsg" );
		private static readonly ElementClass warningMessageContainerClass = new ElementClass( "ewfWarnMsg" );
		private static readonly ElementClass statusMessageTextClass = new ElementClass( "ewfStatusText" );

		private static Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator;

		[ UsedImplicitly ]
		private class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				var elements = new List<CssElement>();
				elements.Add( new CssElement( "TopWarningContainer", "div.{0}".FormatWith( topWarningContainerClass.ClassName ) ) );

				var clickBlockingBlockInactiveSelector = "div." + clickBlockerInactiveClass.ClassName;
				var clickBlockingBlockActiveSelector = "div." + clickBlockerActiveClass.ClassName;
				elements.Add( new CssElement( "ClickBlockerBothStates", clickBlockingBlockInactiveSelector, clickBlockingBlockActiveSelector ) );
				elements.Add( new CssElement( "ClickBlockerInactiveState", clickBlockingBlockInactiveSelector ) );
				elements.Add( new CssElement( "ClickBlockerActiveState", clickBlockingBlockActiveSelector ) );

				elements.AddRange( getProcessingDialogElements() );
				elements.AddRange( getNotificationElements() );
				return elements.ToArray();
			}

			private IEnumerable<CssElement> getProcessingDialogElements() {
				var elements = new List<CssElement>();

				var blockInactiveSelector = "div." + processingDialogBlockInactiveClass.ClassName;
				var blockActiveSelector = "div." + processingDialogBlockActiveClass.ClassName;
				var blockTimeOutSelector = "div." + processingDialogBlockTimeOutClass.ClassName;
				var allBlockSelectors = new[] { blockInactiveSelector, blockActiveSelector, blockTimeOutSelector };
				elements.AddRange(
					new[]
						{
							new CssElement( "ProcessingDialogBlockAllStates", allBlockSelectors ),
							new CssElement( "ProcessingDialogBlockInactiveState", blockInactiveSelector ),
							new CssElement( "ProcessingDialogBlockActiveState", blockActiveSelector ),
							new CssElement( "ProcessingDialogBlockTimeOutState", blockTimeOutSelector )
						} );

				elements.Add(
					new CssElement(
						"ProcessingDialogProcessingParagraph",
						allBlockSelectors.Select( i => i + " > p." + processingDialogProcessingParagraphClass.ClassName ).ToArray() ) );

				var timeOutParagraphSelector = "p." + processingDialogTimeOutParagraphClass.ClassName;
				elements.AddRange(
					new[]
						{
							new CssElement( "ProcessingDialogTimeOutParagraphBothStates", allBlockSelectors.Select( i => i + " > " + timeOutParagraphSelector ).ToArray() ),
							new CssElement(
								"ProcessingDialogTimeOutParagraphInactiveState",
								new[] { blockInactiveSelector, blockActiveSelector }.Select( i => i + " > " + timeOutParagraphSelector ).ToArray() ),
							new CssElement( "ProcessingDialogTimeOutParagraphActiveState", blockTimeOutSelector + " > " + timeOutParagraphSelector )
						} );

				return elements;
			}

			private IEnumerable<CssElement> getNotificationElements() {
				var elements = new List<CssElement>();

				var containerNotificationSelector = "div." + notificationSectionContainerNotificationClass.ClassName;
				var containerDockedSelector = "div." + notificationSectionContainerDockedClass.ClassName;
				elements.AddRange(
					new[]
						{
							new CssElement( "NotificationSectionContainerBothStates", containerNotificationSelector, containerDockedSelector ),
							new CssElement( "NotificationSectionContainerNotificationState", containerNotificationSelector ),
							new CssElement( "NotificationSectionContainerDockedState", containerDockedSelector )
						} );

				elements.Add( new CssElement( "NotificationSpacer", "div." + notificationSpacerClass.ClassName ) );
				elements.Add( new CssElement( "InfoMessageContainer", "div." + infoMessageContainerClass.ClassName ) );
				elements.Add( new CssElement( "WarningMessageContainer", "div." + warningMessageContainerClass.ClassName ) );
				elements.Add( new CssElement( "StatusMessageText", "span." + statusMessageTextClass.ClassName ) );

				return elements;
			}
		}

		// We can remove this and just use Font Awesome as soon as https://github.com/FortAwesome/Font-Awesome/issues/671 is fixed.
		private class Spinner: PhrasingComponent {
			private readonly IReadOnlyCollection<FlowComponent> children;

			public Spinner() {
				children = new ElementComponent(
					context => new ElementData(
						() => new ElementLocalData(
							"span",
							focusDependentData: new ElementFocusDependentData(
								attributes: new ElementAttribute( "style", "position: relative; margin-left: 25px; margin-right: 40px" ).ToCollection(),
								includeIdAttribute: true,
								jsInitStatements: @"new Spinner( {
	lines: 13, // The number of lines to draw
	length: 8, // The length of each line
	width: 5, // The line thickness
	radius: 9, // The radius of the inner circle
	corners: 1, // Corner roundness (0..1)
	rotate: 0, // The rotation offset
	direction: 1, // 1: clockwise, -1: counterclockwise
	color: ""#000"", // #rgb or #rrggbb or array of colors
	speed: 1.2, // Rounds per second
	trail: 71, // Afterglow percentage
	shadow: false, // Whether to render a shadow
	hwaccel: true, // Whether to use hardware acceleration
	className: ""spinner"", // The CSS class to assign to the spinner
	zIndex: 2e9, // The z-index (defaults to 2000000000)
	top: ""50%"", // Top position relative to parent
	left: ""50%"" // Left position relative to parent
} ).spin( document.getElementById( """ + context.Id + "\" ) );" ) ) ) ).ToCollection();
			}

			IReadOnlyCollection<FlowComponentOrNode> FlowComponent.GetChildren() {
				return children;
			}
		}

		internal static void Init( Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator ) {
			BasicPageContent.cssInfoCreator = cssInfoCreator;
		}

		internal static ( PageContent, FlowComponent, FlowComponent, FlowComponent, Action, bool ) GetContent(
			PageContent content, Func<string> hiddenFieldValueGetter, Func<string> jsInitStatementGetter ) {
			var contentObjects = new List<PageContent>();
			while( !( content is BasicPageContent ) ) {
				contentObjects.Add( content );
				content = content.GetContent();
			}
			var basicContent = (BasicPageContent)content;

			var jsInitElement = getJsInitElement( jsInitStatementGetter );
			return ( basicContent, basicContent.componentGetter( contentObjects, hiddenFieldValueGetter, jsInitElement ), basicContent.etherealContainer,
				       jsInitElement, basicContent.dataUpdateModificationMethod, basicContent.isAutoDataUpdater );
		}

		private static FlowComponent getJsInitElement( Func<string> jsInitStatementGetter ) =>
			new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData(
						"script",
						focusDependentData: new ElementFocusDependentData(
							attributes: new ElementAttribute(
								"src",
								"data:{0};charset=utf-8;base64,{1}".FormatWith(
									TewlContrib.ContentTypes.JavaScript,
									Convert.ToBase64String(
										Encoding.UTF8.GetBytes( "window.addEventListener( 'DOMContentLoaded', function() { " + jsInitStatementGetter() + " } );" ) ) ) ).Append(
								new ElementAttribute( "defer" ) ) ) ) ) );

		private readonly Func<IReadOnlyCollection<PageContent>, Func<string>, FlowComponent, FlowComponent> componentGetter;
		internal bool FormUsesMultipartEncoding;
		private readonly List<FlowComponent> bodyContent = new List<FlowComponent>();
		private readonly FlowComponent etherealContainer;
		internal readonly ModalBoxId BrowsingModalBoxId = new ModalBoxId();
		private readonly List<EtherealComponent> etherealContent = new List<EtherealComponent>();
		private readonly Action dataUpdateModificationMethod;
		private readonly bool isAutoDataUpdater;

		/// <summary>
		/// Creates a basic page content object.
		/// </summary>
		/// <param name="titleOverride">Do not pass null.</param>
		/// <param name="customHeadElements"></param>
		/// <param name="bodyClasses"></param>
		/// <param name="dataUpdateModificationMethod">The modification method for the page’s data-update modification.</param>
		/// <param name="isAutoDataUpdater">Pass true to force a post-back when a hyperlink is clicked.</param>
		public BasicPageContent(
			string titleOverride = "", TrustedHtmlString customHeadElements = null, ElementClassSet bodyClasses = null, Action dataUpdateModificationMethod = null,
			bool isAutoDataUpdater = false ) {
			var preContentComponents = getPreContentComponents();
			var postContentComponents = getPostContentComponents();
			var etherealComponents = getEtherealComponents();

			componentGetter = ( contentObjects, hiddenFieldValueGetter, jsInitElement ) => new ElementComponent(
				documentContext => new ElementData(
					() => new ElementLocalData( "html" ),
					children: new ElementComponent(
							headContext => new ElementData(
								() => new ElementLocalData( "head" ),
								children: new ElementComponent(
										titleContext => new ElementData(
											() => new ElementLocalData( "title" ),
											children: ( titleOverride.Any() ? titleOverride : getTitle() ).ToComponents() ) ).Concat( getMetadataAndFaviconLinks() )
									.Concat( getTypekitLogicIfNecessary() )
									.Concat(
										from i in cssInfoCreator( contentObjects ) select getStyleSheetLink( EwfPage.Instance.GetClientUrl( i.GetUrl( false, false, false ) ) ) )
									.Append( getModernizrLogic() )
									.Concat( getGoogleAnalyticsLogicIfNecessary() )
									.Concat( getJavaScriptIncludes() )
									.Append( ( customHeadElements ?? new TrustedHtmlString( "" ) ).ToComponent() )
									.Materialize() ) ).Append(
							new ElementComponent(
								bodyContext => new ElementData(
									() => {
										var attributes = new List<ElementAttribute>();
										attributes.Add( new ElementAttribute( "onpagehide", "deactivateProcessingDialog();" ) );
										attributes.Add( new ElementAttribute( "data-instant-whitelist" ) ); // for https://instant.page/

										return new ElementLocalData( "body", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
									},
									classes: bodyClasses,
									children: new ElementComponent(
											formContext => new ElementData(
												() => {
													var attributes = new List<ElementAttribute>();
													attributes.Add( new ElementAttribute( "action", EwfPage.Instance.InfoAsBaseType.GetUrl() ) );
													attributes.Add( new ElementAttribute( "method", "post" ) );
													if( FormUsesMultipartEncoding )
														attributes.Add( new ElementAttribute( "enctype", "multipart/form-data" ) );
													attributes.Add( new ElementAttribute( "novalidate" ) );

													return new ElementLocalData(
														"form",
														focusDependentData: new ElementFocusDependentData( attributes: attributes, includeIdAttribute: true ) );
												},
												clientSideIdOverride: EwfPage.FormId,
												children: preContentComponents.Concat( bodyContent )
													.Concat( postContentComponents )
													.Append( etherealContainer )
													.Append(
														new ElementComponent(
															context => new ElementData(
																() => {
																	var attributes = new List<ElementAttribute>();
																	attributes.Add( new ElementAttribute( "type", "hidden" ) );
																	attributes.Add( new ElementAttribute( "name", EwfPage.HiddenFieldName ) );
																	attributes.Add( new ElementAttribute( "value", hiddenFieldValueGetter() ) );

																	return new ElementLocalData(
																		"input",
																		focusDependentData: new ElementFocusDependentData( attributes: attributes, includeIdAttribute: true ) );
																},
																clientSideIdOverride: EwfPage.HiddenFieldName ) ) )
													.Materialize(),
												etherealChildren: etherealComponents.Concat( etherealContent ).Materialize() ) ).Append( jsInitElement )
										.Materialize() ) ) )
						.Materialize() ) );

			etherealContainer = new GenericFlowContainer( null );
			this.dataUpdateModificationMethod = dataUpdateModificationMethod;
			this.isAutoDataUpdater = isAutoDataUpdater;
		}

		private IReadOnlyCollection<FlowComponent> getPreContentComponents() {
			var outerComponents = new List<FlowComponent>();

			outerComponents.Add(
				new FlowIdContainer(
					EwfPage.Instance.StatusMessages.Any() && statusMessagesDisplayAsNotification()
						? new GenericFlowContainer( null, classes: notificationSpacerClass ).ToCollection()
						: Enumerable.Empty<FlowComponent>() ) );

			var warningLines = new List<IReadOnlyCollection<PhrasingComponent>>();
			if( !ConfigurationStatics.IsLiveInstallation ) {
				var components = new List<PhrasingComponent>();
				components.Add( new FontAwesomeIcon( "fa-exclamation-triangle", "fa-lg" ) );
				components.AddRange( " This is not the live system. Changes made here will be lost and are not recoverable. ".ToComponents() );
				if( ConfigurationStatics.IsIntermediateInstallation && AppRequestState.Instance.IntermediateUserExists )
					components.AddRange(
						new EwfButton(
							new StandardButtonStyle( "Log out", buttonSize: ButtonSize.ShrinkWrap ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: "ewfIntermediateLogOut",
									modificationMethod: NonLiveInstallationStatics.ClearIntermediateAuthenticationCookie,
									actionGetter: () => new PostBackAction( new ExternalResource( NetTools.HomeUrl ) ) ) ) ).Concat( " ".ToComponents() ) );
				components.Add(
					new EwfButton(
						new StandardButtonStyle(
							"Hide this warning",
							buttonSize: ButtonSize.ShrinkWrap,
							icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-eye-slash" ) ) ),
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateIntermediate(
								null,
								id: "ewfHideNonLiveWarnings",
								modificationMethod: NonLiveInstallationStatics.SetWarningsHiddenCookie ) ) ) );
				if( ConfigurationStatics.IsIntermediateInstallation && AppRequestState.Instance.IntermediateUserExists ) {
					var boxId = new ModalBoxId();
					components.AddRange(
						" ".ToComponents()
							.Append(
								new EwfButton(
									new StandardButtonStyle( "Get link", buttonSize: ButtonSize.ShrinkWrap, icon: new ActionComponentIcon( new FontAwesomeIcon( "fa-link" ) ) ),
									behavior: new OpenModalBehavior(
										boxId,
										etherealChildren: new ModalBox(
											boxId,
											true,
											FormItemList.CreateGrid(
													1,
													items: new[] { false, true }.Select(
															i => {
																var url = AppRequestState.Instance.Url;
																if( AppRequestState.Instance.UserAccessible && AppRequestState.Instance.ImpersonatorExists )
																	url = EwfApp.MetaLogicFactory.CreateSelectUserPageInfo( url, user: AppTools.User.Email ).GetUrl();
																url = EwfApp.MetaLogicFactory.CreateIntermediateLogInPageInfo(
																		url,
																		passwordAndHideWarnings: ( ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword, i ) )
																	.GetUrl();
																return new GenericPhrasingContainer(
																	url.ToComponents(),
																	classes: new ElementClass( "ewfIntermediateUrl" /* This is used by EWF CSS files. */ ) ).ToFormItem(
																	label: i ? "Non-live warnings hidden:".ToComponents() : "Standard:".ToComponents() );
															} )
														.Materialize() )
												.ToCollection() ).ToCollection() ) ) ) );
				}
				warningLines.Add( components );
			}

			if( AppRequestState.Instance.UserAccessible && AppRequestState.Instance.ImpersonatorExists &&
			    ( !ConfigurationStatics.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists ) )
				warningLines.Add(
					"User impersonation is in effect. ".ToComponents()
						.Append(
							new EwfHyperlink(
								EwfApp.MetaLogicFactory.CreateSelectUserPageInfo( AppRequestState.Instance.Url ),
								new ButtonHyperlinkStyle( "Change user", buttonSize: ButtonSize.ShrinkWrap ) ) )
						.Concat( " ".ToComponents() )
						.Append(
							new EwfButton(
								new StandardButtonStyle( "End impersonation", buttonSize: ButtonSize.ShrinkWrap ),
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateFull(
										id: "ewfEndImpersonation",
										modificationMethod: UserImpersonationStatics.EndImpersonation,
										actionGetter: () => new PostBackAction( new ExternalResource( NetTools.HomeUrl ) ) ) ) ) )
						.Materialize() );

			if( warningLines.Any() )
				outerComponents.Add(
					new GenericFlowContainer(
						warningLines.Aggregate( ( components, line ) => components.Append( new LineBreak() ).Concat( line ).Materialize() ),
						displaySetup: new DisplaySetup( ConfigurationStatics.IsLiveInstallation || !NonLiveInstallationStatics.WarningsHiddenCookieExists() ),
						classes: topWarningContainerClass ) );

			return outerComponents;
		}

		private IReadOnlyCollection<FlowComponent> getPostContentComponents() {
			// This is used by the EWF JavaScript file.
			const string clickBlockerId = "ewfClickBlocker";

			return new GenericFlowContainer( null, classes: clickBlockerInactiveClass, clientSideIdOverride: clickBlockerId ).Append( getProcessingDialog() )
				.Append( new FlowIdContainer( getNotificationSectionContainer() ) )
				.Materialize();
		}

		private FlowComponent getProcessingDialog() {
			// This is used by the EWF JavaScript file.
			var dialogClass = new ElementClass( "ewfProcessingDialog" );

			return new GenericFlowContainer(
				new Paragraph(
						new Spinner().ToCollection<PhrasingComponent>()
							.Concat( Translation.Processing.ToComponents() )
							.Concat( getProcessingDialogEllipsisDot( 1 ) )
							.Concat( getProcessingDialogEllipsisDot( 2 ) )
							.Concat( getProcessingDialogEllipsisDot( 3 ) )
							.Materialize(),
						classes: processingDialogProcessingParagraphClass ).ToCollection()
					.Append(
						new Paragraph(
							new EwfButton(
								new StandardButtonStyle( Translation.ThisSeemsToBeTakingAWhile, buttonSize: ButtonSize.ShrinkWrap ),
								behavior: new CustomButtonBehavior( () => "stopPostBackRequest();" ) ).ToCollection(),
							classes: processingDialogTimeOutParagraphClass ) )
					.Materialize(),
				classes: dialogClass.Add( processingDialogBlockInactiveClass ) );
		}

		// This supports the animated ellipsis. Browsers that don't support CSS3 animations will still see the static dots.
		private IReadOnlyCollection<PhrasingComponent> getProcessingDialogEllipsisDot( int dotNumber ) {
			// This is used by EWF CSS files.
			var dotClass = new ElementClass( $"ewfProcessingEllipsis{dotNumber}" );

			return new GenericPhrasingContainer( ".".ToComponents(), classes: dotClass ).ToCollection();
		}

		private IReadOnlyCollection<FlowComponent> getNotificationSectionContainer() {
			// This is used by the EWF JavaScript file.
			const string notificationSectionContainerId = "ewfNotification";

			return EwfPage.Instance.StatusMessages.Any() && statusMessagesDisplayAsNotification()
				       ? new DisplayableElement(
					       context => new DisplayableElementData(
						       null,
						       () => new DisplayableElementLocalData(
							       "div",
							       focusDependentData: new DisplayableElementFocusDependentData(
								       includeIdAttribute: true,
								       jsInitStatements: "setTimeout( 'dockNotificationSection();', " + EwfPage.Instance.StatusMessages.Count() * 1000 + " );" ) ),
						       classes: notificationSectionContainerNotificationClass,
						       clientSideIdOverride: notificationSectionContainerId,
						       children: new Section( null, SectionStyle.Box, null, "Messages", null, getStatusMessageComponentList().ToCollection(), false, true, null )
							       .ToCollection() ) ).ToCollection()
				       : Enumerable.Empty<FlowComponent>().Materialize();
		}

		private IReadOnlyCollection<EtherealComponent> getEtherealComponents() =>
			ModalBox.CreateBrowsingModalBox( BrowsingModalBoxId )
				.Append(
					new ModalBox(
						new ModalBoxId(),
						true,
						new FlowIdContainer(
							new Section(
								"Messages",
								EwfPage.Instance.StatusMessages.Any() && !statusMessagesDisplayAsNotification()
									? getStatusMessageComponentList().ToCollection()
									: Enumerable.Empty<FlowComponent>().Materialize() ).ToCollection() ).ToCollection(),
						open: EwfPage.Instance.StatusMessages.Any() && !statusMessagesDisplayAsNotification() ) )
				.Materialize();

		private FlowComponent getStatusMessageComponentList() =>
			new StackList(
				EwfPage.Instance.StatusMessages.Select(
					i => new GenericFlowContainer(
						new FontAwesomeIcon( i.Item1 == StatusMessageType.Info ? "fa-info-circle" : "fa-exclamation-triangle", "fa-lg", "fa-fw" )
							.Append<PhrasingComponent>( new GenericPhrasingContainer( i.Item2.ToComponents(), classes: statusMessageTextClass ) )
							.Materialize(),
						classes: i.Item1 == StatusMessageType.Info ? infoMessageContainerClass : warningMessageContainerClass ).ToComponentListItem() ) );

		private bool statusMessagesDisplayAsNotification() {
			return EwfPage.Instance.StatusMessages.All( i => i.Item1 == StatusMessageType.Info ) && EwfPage.Instance.StatusMessages.Count() <= 3;
		}

		private string getTitle() =>
			StringTools.ConcatenateWithDelimiter(
				" - ",
				EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName,
				ResourceBase.CombineResourcePathStrings(
					ResourceBase.ResourcePathSeparator,
					EwfPage.Instance.InfoAsBaseType.ParentResourceEntityPathString,
					EwfPage.Instance.InfoAsBaseType.ResourceFullName ) );

		private IReadOnlyCollection<FlowComponent> getMetadataAndFaviconLinks() {
			var components = new List<FlowComponent>();

			components.Add(
				getMeta( "application-name", EwfApp.Instance.AppDisplayName.Length > 0 ? EwfApp.Instance.AppDisplayName : ConfigurationStatics.SystemName ) );

			// Chrome start URL
			components.Add( getMeta( "application-url", EwfPage.Instance.GetClientUrl( NetTools.HomeUrl ) ) );

			// IE9 start URL
			components.Add( getMeta( "msapplication-starturl", EwfPage.Instance.GetClientUrl( NetTools.HomeUrl ) ) );

			var faviconPng48X48 = EwfApp.Instance.FaviconPng48X48;
			if( faviconPng48X48 != null && faviconPng48X48.UserCanAccessResource )
				components.Add(
					getLink(
						EwfPage.Instance.GetClientUrl( faviconPng48X48.GetUrl( true, true, false ) ),
						"icon",
						attributes: new ElementAttribute( "sizes", "48x48" ).ToCollection() ) );

			var favicon = EwfApp.Instance.Favicon;
			if( favicon != null && favicon.UserCanAccessResource )
				// rel="shortcut icon" is deprecated and will be replaced with rel="icon".
				components.Add( getLink( EwfPage.Instance.GetClientUrl( favicon.GetUrl( true, true, false ) ), "shortcut icon" ) );

			components.Add( getMeta( "viewport", "initial-scale=1" ) );

			return components;
		}

		private IEnumerable<FlowComponent> getTypekitLogicIfNecessary() {
			if( EwfApp.Instance.TypekitId.Any() ) {
				yield return new TrustedHtmlString(
					"<script type=\"text/javascript\" src=\"http" + ( EwfApp.Instance.RequestIsSecure( EwfPage.Instance.Request ) ? "s" : "" ) + "://use.typekit.com/" +
					EwfApp.Instance.TypekitId + ".js\"></script>" ).ToComponent();
				yield return new TrustedHtmlString( "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" ).ToComponent();
			}
		}

		private FlowComponent getStyleSheetLink( string url ) =>
			getLink( url, "stylesheet", attributes: new ElementAttribute( "type", "text/css" ).ToCollection() );

		private FlowComponent getModernizrLogic() =>
			new TrustedHtmlString(
				"<script type=\"text/javascript\" src=\"" +
				EwfPage.Instance.GetClientUrl( EwfApp.MetaLogicFactory.CreateModernizrJavaScriptInfo().GetUrl( false, false, false ) ) + "\"></script>" ).ToComponent();

		private IEnumerable<FlowComponent> getGoogleAnalyticsLogicIfNecessary() {
			if( EwfApp.Instance.GoogleAnalyticsWebPropertyId.Length == 0 )
				yield break;
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
				yield return new TrustedHtmlString( sw.ToString() ).ToComponent();
			}
		}

		private IReadOnlyCollection<FlowComponent> getJavaScriptIncludes() {
			var components = new List<FlowComponent>();

			FlowComponent getElement( ResourceInfo resource ) =>
				new TrustedHtmlString( "<script src=\"{0}\" defer></script>".FormatWith( EwfPage.Instance.GetClientUrl( resource.GetUrl( false, false, false ) ) ) )
					.ToComponent();

			components.AddRange( EwfApp.MetaLogicFactory.CreateJavaScriptInfos().Select( getElement ) );
			components.Add( new TrustedHtmlString( MiniProfiler.RenderIncludes().ToHtmlString() ).ToComponent() );
			components.AddRange( EwfApp.Instance.GetJavaScriptFiles().Select( getElement ) );

			return components;
		}

		private FlowComponent getLink( string href, string rel, IReadOnlyCollection<ElementAttribute> attributes = null ) =>
			new ElementComponent(
				context => new ElementData(
					() => new ElementLocalData(
						"link",
						focusDependentData: new ElementFocusDependentData(
							attributes: new ElementAttribute( "href", href ).Append( new ElementAttribute( "rel", rel ) )
								.Concat( attributes ?? Enumerable.Empty<ElementAttribute>() ) ) ) ) );

		private FlowComponent getMeta( string name, string content ) =>
			new ElementComponent(
				context => new ElementData(
					() => {
						var attributes = new List<ElementAttribute>();
						attributes.Add( new ElementAttribute( "name", name ) );
						attributes.Add( new ElementAttribute( "content", content ) );
						return new ElementLocalData( "meta", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
					} ) );

		public BasicPageContent Add( IReadOnlyCollection<FlowComponent> components ) {
			bodyContent.AddRange( components );
			return this;
		}

		public BasicPageContent Add( FlowComponent component ) {
			bodyContent.Add( component );
			return this;
		}

		public BasicPageContent Add( IReadOnlyCollection<EtherealComponent> components ) {
			etherealContent.AddRange( components );
			return this;
		}

		public BasicPageContent Add( EtherealComponent component ) {
			etherealContent.Add( component );
			return this;
		}

		protected internal override PageContent GetContent() {
			throw new NotSupportedException();
		}
	}
}