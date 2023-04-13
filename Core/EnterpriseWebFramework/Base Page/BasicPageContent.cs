﻿using System.Text;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.WebSessionState;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public sealed class BasicPageContent: PageContent {
	// Some of these are used by the EWF JavaScript file.
	private static readonly ElementClass topWarningContainerClass = new( "ewfTopWarning" );
	private static readonly ElementClass processingDialogAllStatesClass = new( "ewfProcessingDialog" );
	private static readonly ElementClass processingDialogNormalStateClass = new( "ewfProcessingDialogN" );
	private static readonly ElementClass processingDialogTimeOutStateClass = new( "ewfProcessingDialogTo" );
	private static readonly ElementClass processingDialogProcessingParagraphClass = new( "ewfProcessingP" );
	private static readonly ElementClass processingDialogTimeOutParagraphClass = new( "ewfTimeOutP" );
	private static readonly ElementClass notificationSectionContainerClass = new( "ewfNotification" );
	private static readonly ElementClass infoMessageContainerClass = new( "ewfInfoMsg" );
	private static readonly ElementClass warningMessageContainerClass = new( "ewfWarnMsg" );
	private static readonly ElementClass statusMessageTextClass = new( "ewfStatusText" );

	private static Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator;
	private static Action<StringBuilder, bool> javaScriptIncludeBuilder;
	private static Func<IEnumerable<( ResourceInfo resource, string rel, string sizes )>> appIconGetter;
	private static Func<bool, string> intermediateUrlGetter;
	private static Func<( string message, IReadOnlyCollection<ActionComponentSetup> actions )?> impersonationWarningLineGetter;

	[ UsedImplicitly ]
	private class CssElementCreator: ControlCssElementCreator {
		IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
			var elements = new List<CssElement>();
			elements.Add( new CssElement( "TopWarningContainer", "div.{0}".FormatWith( topWarningContainerClass.ClassName ) ) );
			elements.AddRange( getProcessingDialogElements() );
			elements.AddRange( getNotificationElements() );
			return elements;
		}

		private IEnumerable<CssElement> getProcessingDialogElements() {
			var elements = new List<CssElement>();

			var formSelector = "form#{0} ".FormatWith( PageBase.FormId );
			var dialogAllStatesSelectors = ModalBox.CssElementCreator.GetContainerSelectors( ".{0}".FormatWith( processingDialogAllStatesClass.ClassName ) )
				.Select( i => formSelector + i )
				.ToArray();
			var dialogNormalStateSelectors = ModalBox.CssElementCreator.GetContainerSelectors( ".{0}".FormatWith( processingDialogNormalStateClass.ClassName ) )
				.Select( i => formSelector + i )
				.ToArray();
			var dialogTimeOutStateSelectors = ModalBox.CssElementCreator.GetContainerSelectors( ".{0}".FormatWith( processingDialogTimeOutStateClass.ClassName ) )
				.Select( i => formSelector + i )
				.ToArray();
			elements.AddRange(
				new[]
					{
						new CssElement( "ProcessingDialogModalBoxContainerAllStates", dialogAllStatesSelectors ),
						new CssElement( "ProcessingDialogModalBoxContainerNormalState", dialogNormalStateSelectors ),
						new CssElement( "ProcessingDialogModalBoxContainerTimeOutState", dialogTimeOutStateSelectors )
					} );

			elements.Add(
				new CssElement(
					"ProcessingDialogProcessingParagraph",
					dialogAllStatesSelectors.Select( i => "{0} p.{1}".FormatWith( i, processingDialogProcessingParagraphClass.ClassName ) ).ToArray() ) );

			var timeOutParagraphSelector = "p." + processingDialogTimeOutParagraphClass.ClassName;
			elements.AddRange(
				new[]
					{
						new CssElement(
							"ProcessingDialogTimeOutParagraphBothStates",
							dialogAllStatesSelectors.Select( i => "{0} {1}".FormatWith( i, timeOutParagraphSelector ) ).ToArray() ),
						new CssElement(
							"ProcessingDialogTimeOutParagraphInactiveState",
							dialogNormalStateSelectors.Select( i => "{0} {1}".FormatWith( i, timeOutParagraphSelector ) ).ToArray() ),
						new CssElement(
							"ProcessingDialogTimeOutParagraphActiveState",
							dialogTimeOutStateSelectors.Select( i => "{0} {1}".FormatWith( i, timeOutParagraphSelector ) ).ToArray() )
					} );

			elements.Add(
				new CssElement(
					"ProcessingDialogModalBoxBackdrop",
					ModalBox.CssElementCreator.GetBackdropSelectors( ".{0}".FormatWith( processingDialogAllStatesClass.ClassName ) )
						.Select( i => formSelector + i )
						.ToArray() ) );

			return elements;
		}

		private IEnumerable<CssElement> getNotificationElements() =>
			new CssElement( "NotificationSectionContainer", "div.{0}".FormatWith( notificationSectionContainerClass.ClassName ) ).ToCollection()
				.Append( new CssElement( "InfoMessageContainer", "div." + infoMessageContainerClass.ClassName ) )
				.Append( new CssElement( "WarningMessageContainer", "div." + warningMessageContainerClass.ClassName ) )
				.Append( new CssElement( "StatusMessageText", "span." + statusMessageTextClass.ClassName ) );
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

	internal static void Init(
		Func<IReadOnlyCollection<PageContent>, IEnumerable<ResourceInfo>> cssInfoCreator, Action<StringBuilder, bool> javaScriptIncludeBuilder,
		Func<IEnumerable<( ResourceInfo, string, string )>> appIconGetter, Func<bool, string> intermediateUrlGetter,
		Func<( string, IReadOnlyCollection<ActionComponentSetup> )?> impersonationWarningLineGetter ) {
		BasicPageContent.cssInfoCreator = cssInfoCreator;
		BasicPageContent.javaScriptIncludeBuilder = javaScriptIncludeBuilder;
		BasicPageContent.appIconGetter = appIconGetter;
		BasicPageContent.intermediateUrlGetter = intermediateUrlGetter;
		BasicPageContent.impersonationWarningLineGetter = impersonationWarningLineGetter;
	}

	internal static ( PageContent, FlowComponent, FlowComponent, FlowComponent, Action, bool, ActionPostBack ) GetContent(
		Func<Func<PageContent>, PageContent> contentGetter, Func<string> hiddenFieldValueGetter, Func<string> jsInitStatementGetter ) {
		var content = contentGetter( () => new BasicPageContent() );

		var contentObjects = new List<PageContent>();
		while( !( content is BasicPageContent ) ) {
			contentObjects.Add( content );
			content = content.GetContent();
		}
		var basicContent = (BasicPageContent)content;

		var jsInitElement = getJsInitElement( jsInitStatementGetter );
		return ( basicContent, basicContent.componentGetter( contentObjects, hiddenFieldValueGetter, jsInitElement ), basicContent.etherealContainer, jsInitElement,
			       basicContent.dataUpdateModificationMethod, basicContent.isAutoDataUpdater, basicContent.pageLoadPostBack );
	}

	private static FlowComponent getJsInitElement( Func<string> jsInitStatementGetter ) =>
		new ElementComponent(
			_ => new ElementData(
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
	internal bool IncludesStripeCheckout;
	internal bool FormUsesMultipartEncoding;
	private readonly List<FlowComponent> bodyContent = new();
	private readonly FlowComponent etherealContainer;
	internal readonly ModalBoxId BrowsingModalBoxId = new();
	private readonly List<EtherealComponent> etherealContent = new();
	private readonly Action dataUpdateModificationMethod;
	private readonly bool isAutoDataUpdater;
	private readonly ActionPostBack pageLoadPostBack;

	/// <summary>
	/// Creates a basic page content object.
	/// </summary>
	/// <param name="titleOverride">Do not pass null.</param>
	/// <param name="customHeadElements"></param>
	/// <param name="bodyClasses"></param>
	/// <param name="dataUpdateModificationMethod">The modification method for the page’s data-update modification.</param>
	/// <param name="isAutoDataUpdater">Pass true to force a post-back when a hyperlink is clicked.</param>
	/// <param name="pageLoadPostBack">A post-back that will be triggered automatically by the browser when the page is finished loading.</param>
	public BasicPageContent(
		string titleOverride = "", TrustedHtmlString customHeadElements = null, ElementClassSet bodyClasses = null, Action dataUpdateModificationMethod = null,
		bool isAutoDataUpdater = false, ActionPostBack pageLoadPostBack = null ) {
		var preContentComponents = getPreContentComponents();
		var postContentComponents = new FlowIdContainer( getNotificationSectionContainer() );
		var etherealComponents = getEtherealComponents();

		componentGetter = ( contentObjects, hiddenFieldValueGetter, jsInitElement ) => new ElementComponent(
			_ => new ElementData(
				() => new ElementLocalData(
					"html",
					focusDependentData: new ElementFocusDependentData( attributes: new ElementAttribute( "lang", "en-US" ).ToCollection() ) ),
				children: new ElementComponent(
						_ => new ElementData(
							() => new ElementLocalData( "head" ),
							children: new ElementComponent(
									_ => new ElementData( () => new ElementLocalData( "title" ), children: ( titleOverride.Any() ? titleOverride : getTitle() ).ToComponents() ) )
								.Append(
									getMeta(
										"application-name",
										BasePageStatics.AppProvider.AppDisplayName.Length > 0
											? BasePageStatics.AppProvider.AppDisplayName
											: ConfigurationStatics.SystemDisplayName ) )
								.Concat( getTypekitLogicIfNecessary() )
								.Concat( from i in cssInfoCreator( contentObjects ) select getStyleSheetLink( i.GetUrl() ) )
								.Append( getModernizrLogic() )
								.Concat( getGoogleAnalyticsLogicIfNecessary() )
								.Append( getJavaScriptIncludes() )
								.Concat(
									from i in appIconGetter()
									select getLink( i.resource.GetUrl(), i.rel, attributes: i.sizes.Any() ? new ElementAttribute( "sizes", i.sizes ).ToCollection() : null ) )
								.Append( getMeta( "viewport", "initial-scale=1" ) )
								.Append(
									// Chrome start URL
									getMeta(
										"application-url",
										EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ) ) )
								.Append(
									// IE9 start URL
									getMeta(
										"msapplication-starturl",
										EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ) ) )
								.Append( ( customHeadElements ?? new TrustedHtmlString( "" ) ).ToComponent() )
								.Materialize() ) ).Append(
						new ElementComponent(
							_ => new ElementData(
								() => {
									var attributes = new List<ElementAttribute>();
									attributes.Add( new ElementAttribute( "onpagehide", "deactivateProcessingDialog();" ) );
									attributes.Add( new ElementAttribute( "data-instant-whitelist" ) ); // for https://instant.page/

									return new ElementLocalData( "body", focusDependentData: new ElementFocusDependentData( attributes: attributes ) );
								},
								classes: bodyClasses,
								children: new ElementComponent(
										_ => new ElementData(
											() => {
												var attributes = new List<ElementAttribute>();
												attributes.Add( new ElementAttribute( "action", PageBase.Current.GetUrl() ) );
												attributes.Add( new ElementAttribute( "method", "post" ) );
												if( FormUsesMultipartEncoding )
													attributes.Add( new ElementAttribute( "enctype", "multipart/form-data" ) );
												attributes.Add( new ElementAttribute( "novalidate" ) );

												return new ElementLocalData(
													"form",
													focusDependentData: new ElementFocusDependentData( attributes: attributes, includeIdAttribute: true ) );
											},
											clientSideIdOverride: PageBase.FormId,
											children: preContentComponents.Concat( bodyContent )
												.Concat( postContentComponents )
												.Append( etherealContainer )
												.Append(
													new ElementComponent(
														_ => new ElementData(
															() => {
																var attributes = new List<ElementAttribute>();
																attributes.Add( new ElementAttribute( "type", "hidden" ) );
																attributes.Add( new ElementAttribute( "name", PageBase.HiddenFieldName ) );
																attributes.Add( new ElementAttribute( "value", hiddenFieldValueGetter() ) );

																return new ElementLocalData(
																	"input",
																	focusDependentData: new ElementFocusDependentData( attributes: attributes, includeIdAttribute: true ) );
															},
															clientSideIdOverride: PageBase.HiddenFieldName ) ) )
												.Materialize(),
											etherealChildren: etherealComponents.Concat( etherealContent ).Materialize() ) ).Append( jsInitElement )
									.Materialize() ) ) )
					.Materialize() ) );

		etherealContainer = new GenericFlowContainer( null );
		this.dataUpdateModificationMethod = dataUpdateModificationMethod;
		this.isAutoDataUpdater = isAutoDataUpdater;
		this.pageLoadPostBack = pageLoadPostBack;
	}

	private IReadOnlyCollection<FlowComponent> getPreContentComponents() {
		var outerComponents = new List<FlowComponent>();

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
									actionGetter: () => new PostBackAction(
										new ExternalResource(
											EwfConfigurationStatics.AppConfiguration.DefaultBaseUrl.GetUrlString( EwfConfigurationStatics.AppSupportsSecureConnections ) ) ) ) ) )
						.Concat( " ".ToComponents() ) );
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
										FormItemList.CreateFixedGrid(
												1,
												items: new[] { false, true }.Select(
														hideWarnings => new GenericPhrasingContainer(
															intermediateUrlGetter( hideWarnings ).ToComponents(),
															classes: new ElementClass( "ewfIntermediateUrl" /* This is used by EWF CSS files. */ ) ).ToFormItem(
															label: hideWarnings ? "Non-live warnings hidden:".ToComponents() : "Standard:".ToComponents() ) )
													.Materialize() )
											.ToCollection() ).ToCollection() ) ) ) );
			}
			warningLines.Add( components );
		}

		var impersonationWarningLine = impersonationWarningLineGetter();
		if( impersonationWarningLine.HasValue )
			warningLines.Add(
				impersonationWarningLine.Value.message.ToComponents()
					.Concat( " ".ToComponents() )
					.Concat(
						impersonationWarningLine.Value.actions
							.Select(
								i => i.GetActionComponent(
									( text, _ ) => new ButtonHyperlinkStyle( text, buttonSize: ButtonSize.ShrinkWrap ),
									( text, _ ) => new StandardButtonStyle( text, buttonSize: ButtonSize.ShrinkWrap ) ) )
							.Where( i => i != null )
							.Select( i => i.ToCollection() )
							.Aggregate( ( components, action ) => components.Concat( " ".ToComponents() ).Concat( action ).Materialize() ) )
					.Materialize() );

		if( warningLines.Any() )
			outerComponents.Add(
				new GenericFlowContainer(
					warningLines.Aggregate( ( components, line ) => components.Append( new LineBreak() ).Concat( line ).Materialize() ),
					displaySetup: new DisplaySetup( ConfigurationStatics.IsLiveInstallation || !NonLiveInstallationStatics.WarningsHiddenCookieExists() ),
					classes: topWarningContainerClass ) );

		return outerComponents;
	}

	private IReadOnlyCollection<FlowComponent> getNotificationSectionContainer() =>
		PageBase.Current.StatusMessages.Any() && statusMessagesDisplayAsNotification()
			? new GenericFlowContainer(
				new Section( null, SectionStyle.Box, null, "Messages", null, getStatusMessageComponentList().ToCollection(), true, true, null ).ToCollection(),
				classes: notificationSectionContainerClass ).ToCollection()
			: Enumerable.Empty<FlowComponent>().Materialize();

	private IReadOnlyCollection<EtherealComponent> getEtherealComponents() =>
		ModalBox.CreateBrowsingModalBox( BrowsingModalBoxId )
			.Append( getProcessingDialog() )
			.Append(
				new ModalBox(
					new ModalBoxId(),
					true,
					new FlowIdContainer(
						new Section(
							"Messages",
							PageBase.Current.StatusMessages.Any() && !statusMessagesDisplayAsNotification()
								? getStatusMessageComponentList().ToCollection()
								: Enumerable.Empty<FlowComponent>().Materialize() ).ToCollection() ).ToCollection(),
					open: PageBase.Current.StatusMessages.Any() && !statusMessagesDisplayAsNotification() ) )
			.Materialize();

	private EtherealComponent getProcessingDialog() =>
		new ModalBox(
			new ModalBoxId(),
			false,
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
			classes: processingDialogAllStatesClass.Add( processingDialogNormalStateClass ) );

	// This supports the animated ellipsis. Browsers that don't support CSS3 animations will still see the static dots.
	private IReadOnlyCollection<PhrasingComponent> getProcessingDialogEllipsisDot( int dotNumber ) {
		// This is used by EWF CSS files.
		var dotClass = new ElementClass( $"ewfProcessingEllipsis{dotNumber}" );

		return new GenericPhrasingContainer( ".".ToComponents(), classes: dotClass ).ToCollection();
	}

	private FlowComponent getStatusMessageComponentList() =>
		new StackList(
			PageBase.Current.StatusMessages.Select(
				i => new GenericFlowContainer(
					new FontAwesomeIcon( i.Item1 == StatusMessageType.Info ? "fa-info-circle" : "fa-exclamation-triangle", "fa-lg", "fa-fw" )
						.Append<PhrasingComponent>( new GenericPhrasingContainer( i.Item2.ToComponents(), classes: statusMessageTextClass ) )
						.Materialize(),
					classes: i.Item1 == StatusMessageType.Info ? infoMessageContainerClass : warningMessageContainerClass ).ToComponentListItem() ) );

	private bool statusMessagesDisplayAsNotification() =>
		PageBase.Current.StatusMessages.All( i => i.Item1 == StatusMessageType.Info ) && PageBase.Current.StatusMessages.Count() <= 3;

	private string getTitle() =>
		StringTools.ConcatenateWithDelimiter(
			" - ",
			BasePageStatics.AppProvider.AppDisplayName.Length > 0 ? BasePageStatics.AppProvider.AppDisplayName : ConfigurationStatics.SystemDisplayName,
			PageBase.Current.ResourceFullName );

	private IEnumerable<FlowComponent> getTypekitLogicIfNecessary() {
		if( BasePageStatics.AppProvider.TypekitId.Any() ) {
			yield return new TrustedHtmlString(
				"<script type=\"text/javascript\" src=\"http" + ( EwfRequest.Current.IsSecure ? "s" : "" ) + "://use.typekit.com/" +
				BasePageStatics.AppProvider.TypekitId + ".js\"></script>" ).ToComponent();
			yield return new TrustedHtmlString( "<script type=\"text/javascript\">try{Typekit.load();}catch(e){}</script>" ).ToComponent();
		}
	}

	private FlowComponent getStyleSheetLink( string url ) => getLink( url, "stylesheet", attributes: new ElementAttribute( "type", "text/css" ).ToCollection() );

	private FlowComponent getModernizrLogic() =>
		new TrustedHtmlString( "<script type=\"text/javascript\" src=\"" + new StaticFiles.ModernizrJs().GetUrl() + "\"></script>" ).ToComponent();

	private IEnumerable<FlowComponent> getGoogleAnalyticsLogicIfNecessary() {
		if( BasePageStatics.AppProvider.GoogleAnalyticsWebPropertyId.Length == 0 )
			yield break;

		using var sw = new StringWriter();
		sw.WriteLine( "<script>" );
		sw.WriteLine( "(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){" );
		sw.WriteLine( "(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o)," );
		sw.WriteLine( "m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)" );
		sw.WriteLine( "})(window,document,'script','//www.google-analytics.com/analytics.js','ga');" );

		var userId = BasePageStatics.AppProvider.GetGoogleAnalyticsUserId();
		sw.WriteLine(
			"ga('create', '" + BasePageStatics.AppProvider.GoogleAnalyticsWebPropertyId + "', 'auto'{0});",
			userId.Any() ? ", {{'userId': '{0}'}}".FormatWith( userId ) : "" );

		sw.WriteLine( "ga('send', 'pageview');" );
		sw.WriteLine( "</script>" );
		yield return new TrustedHtmlString( sw.ToString() ).ToComponent();
	}

	private FlowComponentOrNode getJavaScriptIncludes() =>
		new MarkupBlockNode(
			() => {
				var markup = new StringBuilder();
				javaScriptIncludeBuilder( markup, IncludesStripeCheckout );
				return markup.ToString();
			} );

	private FlowComponent getLink( string href, string rel, IReadOnlyCollection<ElementAttribute> attributes = null ) =>
		new ElementComponent(
			_ => new ElementData(
				() => new ElementLocalData(
					"link",
					focusDependentData: new ElementFocusDependentData(
						attributes: new ElementAttribute( "href", href ).Append( new ElementAttribute( "rel", rel ) )
							.Concat( attributes ?? Enumerable.Empty<ElementAttribute>() ) ) ) ) );

	private FlowComponent getMeta( string name, string content ) =>
		new ElementComponent(
			_ => new ElementData(
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