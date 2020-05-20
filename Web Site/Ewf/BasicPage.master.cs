using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class BasicPage: MasterPage, ControlTreeDataLoader, ControlWithJsInitLogic {
		// Some of these are used by the EWF JavaScript file.
		private static readonly ElementClass topWarningContainerClass = new ElementClass( "ewfTopWarning" );
		private const string clickBlockerInactiveClass = "ewfClickBlockerI";
		private const string clickBlockerActiveClass = "ewfClickBlockerA";
		private static readonly ElementClass processingDialogBlockInactiveClass = new ElementClass( "ewfProcessingDialogI" );
		private static readonly ElementClass processingDialogBlockActiveClass = new ElementClass( "ewfProcessingDialogA" );
		private static readonly ElementClass processingDialogBlockTimeOutClass = new ElementClass( "ewfProcessingDialogTo" );
		private static readonly ElementClass processingDialogProcessingParagraphClass = new ElementClass( "ewfProcessingP" );
		private static readonly ElementClass processingDialogTimeOutParagraphClass = new ElementClass( "ewfTimeOutP" );
		private const string notificationSectionContainerNotificationClass = "ewfNotificationN";
		private const string notificationSectionContainerDockedClass = "ewfNotificationD";
		private const string notificationSpacerClass = "ewfNotificationSpacer";
		private static readonly ElementClass infoMessageContainerClass = new ElementClass( "ewfInfoMsg" );
		private static readonly ElementClass warningMessageContainerClass = new ElementClass( "ewfWarnMsg" );
		private static readonly ElementClass statusMessageTextClass = new ElementClass( "ewfStatusText" );

		internal class CssElementCreator: ControlCssElementCreator {
			IReadOnlyCollection<CssElement> ControlCssElementCreator.CreateCssElements() {
				var elements = new List<CssElement>();
				elements.Add( new CssElement( "TopWarningContainer", "div.{0}".FormatWith( topWarningContainerClass.ClassName ) ) );

				const string clickBlockingBlockInactiveSelector = "div." + clickBlockerInactiveClass;
				const string clickBlockingBlockActiveSelector = "div." + clickBlockerActiveClass;
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

				const string containerNotificationSelector = "div." + notificationSectionContainerNotificationClass;
				const string containerDockedSelector = "div." + notificationSectionContainerDockedClass;
				elements.AddRange(
					new[]
						{
							new CssElement( "NotificationSectionContainerBothStates", containerNotificationSelector, containerDockedSelector ),
							new CssElement( "NotificationSectionContainerNotificationState", containerNotificationSelector ),
							new CssElement( "NotificationSectionContainerDockedState", containerDockedSelector )
						} );

				elements.Add( new CssElement( "NotificationSpacer", "div." + notificationSpacerClass ) );
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
								attributes: Tuple.Create( "style", "position: relative; margin-left: 25px; margin-right: 40px" ).ToCollection(),
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

		/// <summary>
		/// Gets the current BasicPage master page.
		/// </summary>
		public static BasicPage Instance => getTopMaster( EwfPage.Instance.Master ) as BasicPage;

		private static MasterPage getTopMaster( MasterPage master ) {
			return master.Master == null ? master : getTopMaster( master.Master );
		}

		public HtmlGenericControl Body => basicBody;

		void ControlTreeDataLoader.LoadData() {
			basicBody.Attributes.Add( "onpagehide", "deactivateProcessingDialog();" );
			form.Action = EwfPage.Instance.InfoAsBaseType.GetUrl();

			ph.AddControlsReturnThis(
				new NamingPlaceholder(
					EwfPage.Instance.StatusMessages.Any() && statusMessagesDisplayAsNotification()
						? new Block { CssClass = notificationSpacerClass }.ToCollection()
						: Enumerable.Empty<Control>() ) );

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
									firstModificationMethod: NonLiveInstallationStatics.ClearIntermediateAuthenticationCookie,
									actionGetter: () => new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) ) ) ) ).Concat( " ".ToComponents() ) );
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
								firstModificationMethod: NonLiveInstallationStatics.SetWarningsHiddenCookie ) ) ) );
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
																	url = SelectUser.GetInfo(
																			url,
																			optionalParameterPackage: new SelectUser.OptionalParameterPackage { User = AppTools.User.Email } )
																		.GetUrl();
																url = IntermediateLogIn.GetInfo(
																		url,
																		new IntermediateLogIn.OptionalParameterPackage
																			{
																				Password = ConfigurationStatics.SystemGeneralProvider.IntermediateLogInPassword, HideWarnings = i
																			} )
																	.GetUrl();
																return url.ToComponents().ToFormItem( label: i ? "Non-live warnings hidden:".ToComponents() : "Standard:".ToComponents() );
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
								SelectUser.GetInfo( AppRequestState.Instance.Url ),
								new ButtonHyperlinkStyle( "Change user", buttonSize: ButtonSize.ShrinkWrap ) ) )
						.Concat( " ".ToComponents() )
						.Append(
							new EwfButton(
								new StandardButtonStyle( "End impersonation", buttonSize: ButtonSize.ShrinkWrap ),
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateFull(
										id: "ewfEndImpersonation",
										firstModificationMethod: UserImpersonationStatics.EndImpersonation,
										actionGetter: () => new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) ) ) ) ) )
						.Materialize() );

			if( warningLines.Any() )
				ph.AddControlsReturnThis(
					new GenericFlowContainer(
							warningLines.Aggregate( ( components, line ) => components.Append( new LineBreak() ).Concat( line ).Materialize() ),
							displaySetup: new DisplaySetup( ConfigurationStatics.IsLiveInstallation || !NonLiveInstallationStatics.WarningsHiddenCookieExists() ),
							classes: topWarningContainerClass ).ToCollection()
						.GetControls() );

			// This is used by the EWF JavaScript file.
			const string clickBlockerId = "ewfClickBlocker";

			ph2.AddControlsReturnThis(
				new Block { ClientIDMode = ClientIDMode.Static, ID = clickBlockerId, CssClass = clickBlockerInactiveClass },
				new PlaceHolder().AddControlsReturnThis( getProcessingDialog().GetControls() ),
				new NamingPlaceholder( getStatusMessageControl() ) );
		}

		private IReadOnlyCollection<FlowComponent> getProcessingDialog() {
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
				classes: dialogClass.Add( processingDialogBlockInactiveClass ) ).ToCollection();
		}

		// This supports the animated ellipsis. Browsers that don't support CSS3 animations will still see the static dots.
		private IReadOnlyCollection<PhrasingComponent> getProcessingDialogEllipsisDot( int dotNumber ) {
			// This is used by EWF CSS files.
			var dotClass = new ElementClass( $"ewfProcessingEllipsis{dotNumber}" );

			return new GenericPhrasingContainer( ".".ToComponents(), classes: dotClass ).ToCollection();
		}

		private IEnumerable<Control> getStatusMessageControl() {
			var messagesExist = EwfPage.Instance.StatusMessages.Any();
			new ModalBox(
					new ModalBoxId(),
					true,
					new FlowIdContainer(
						new Section(
							"Messages",
							messagesExist && !statusMessagesDisplayAsNotification()
								? getStatusMessageComponentList().ToCollection()
								: Enumerable.Empty<FlowComponent>().Materialize() ).ToCollection() ).ToCollection(),
					open: messagesExist && !statusMessagesDisplayAsNotification() ).ToCollection()
				.AddEtherealControls( this );

			// This is used by the EWF JavaScript file.
			const string notificationSectionContainerId = "ewfNotification";

			return messagesExist && statusMessagesDisplayAsNotification()
				       ? new Block(
					       new PlaceHolder().AddControlsReturnThis(
						       new Section( null, SectionStyle.Box, null, "Messages", null, getStatusMessageComponentList().ToCollection(), false, true, null )
							       .ToCollection()
							       .GetControls() ) )
					       {
						       ClientIDMode = ClientIDMode.Static, ID = notificationSectionContainerId, CssClass = notificationSectionContainerNotificationClass
					       }.ToCollection()
				       : Enumerable.Empty<Control>();
		}

		private FlowComponent getStatusMessageComponentList() =>
			new StackList(
				EwfPage.Instance.StatusMessages.Select(
					i => new GenericFlowContainer(
						new FontAwesomeIcon( i.Item1 == StatusMessageType.Info ? "fa-info-circle" : "fa-exclamation-triangle", "fa-lg", "fa-fw" )
							.Append<PhrasingComponent>( new GenericPhrasingContainer( i.Item2.ToComponents(), classes: statusMessageTextClass ) )
							.Materialize(),
						classes: i.Item1 == StatusMessageType.Info ? infoMessageContainerClass : warningMessageContainerClass ).ToComponentListItem() ) );

		string ControlWithJsInitLogic.GetJsInitStatements() {
			return EwfPage.Instance.StatusMessages.Any() && statusMessagesDisplayAsNotification()
				       ? "setTimeout( 'dockNotificationSection();', " + EwfPage.Instance.StatusMessages.Count() * 1000 + " );"
				       : "";
		}

		private bool statusMessagesDisplayAsNotification() {
			return EwfPage.Instance.StatusMessages.All( i => i.Item1 == StatusMessageType.Info ) && EwfPage.Instance.StatusMessages.Count() <= 3;
		}
	}
}