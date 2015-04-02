using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Humanizer;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.UserManagement;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class BasicPage: MasterPage, ControlTreeDataLoader, ControlWithJsInitLogic {
		// Some of these are used by the Standard Library JavaScript file.
		private const string topWarningBlockCssClass = "ewfTopWarning";
		private const string clickBlockerInactiveClass = "ewfClickBlockerI";
		private const string clickBlockerActiveClass = "ewfClickBlockerA";
		private const string processingDialogBlockInactiveClass = "ewfProcessingDialogI";
		private const string processingDialogBlockActiveClass = "ewfProcessingDialogA";
		private const string processingDialogBlockTimeOutClass = "ewfProcessingDialogTo";
		private const string processingDialogProcessingParagraphClass = "ewfProcessingP";
		private const string processingDialogTimeOutParagraphClass = "ewfTimeOutP";
		private const string notificationSectionContainerNotificationClass = "ewfNotificationN";
		private const string notificationSectionContainerDockedClass = "ewfNotificationD";
		private const string notificationSpacerClass = "ewfNotificationSpacer";
		private const string infoMessageContainerClass = "ewfInfoMsg";
		private const string warningMessageContainerClass = "ewfWarnMsg";
		private const string statusMessageTextClass = "ewfStatusText";

		internal class CssElementCreator: ControlCssElementCreator {
			CssElement[] ControlCssElementCreator.CreateCssElements() {
				var elements = new List<CssElement>();
				elements.Add( new CssElement( "TopWarningBlock", "div.{0}".FormatWith( topWarningBlockCssClass ) ) );

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

				const string blockInactiveSelector = "div." + processingDialogBlockInactiveClass;
				const string blockActiveSelector = "div." + processingDialogBlockActiveClass;
				const string blockTimeOutSelector = "div." + processingDialogBlockTimeOutClass;
				var allBlockSelectors = new[] { blockInactiveSelector, blockActiveSelector, blockTimeOutSelector };
				elements.AddRange(
					new[]
						{
							new CssElement( "ProcessingDialogBlockAllStates", allBlockSelectors ), new CssElement( "ProcessingDialogBlockInactiveState", blockInactiveSelector ),
							new CssElement( "ProcessingDialogBlockActiveState", blockActiveSelector ), new CssElement( "ProcessingDialogBlockTimeOutState", blockTimeOutSelector )
						} );

				elements.Add(
					new CssElement( "ProcessingDialogProcessingParagraph", allBlockSelectors.Select( i => i + " > p." + processingDialogProcessingParagraphClass ).ToArray() ) );

				const string timeOutParagraphSelector = "p." + processingDialogTimeOutParagraphClass;
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
				elements.Add( new CssElement( "InfoMessageContainer", "div." + infoMessageContainerClass ) );
				elements.Add( new CssElement( "WarningMessageContainer", "div." + warningMessageContainerClass ) );
				elements.Add( new CssElement( "StatusMessageText", "span." + statusMessageTextClass ) );

				return elements;
			}
		}

		/// <summary>
		/// Gets the current BasicPage master page.
		/// </summary>
		public static BasicPage Instance { get { return getTopMaster( EwfPage.Instance.Master ) as BasicPage; } }

		private static MasterPage getTopMaster( MasterPage master ) {
			return master.Master == null ? master : getTopMaster( master.Master );
		}

		public HtmlGenericControl Body { get { return basicBody; } }

		void ControlTreeDataLoader.LoadData() {
			basicBody.Attributes.Add( "onpagehide", "deactivateProcessingDialog();" );
			form.Action = EwfPage.Instance.InfoAsBaseType.GetUrl();

			ph.AddControlsReturnThis(
				new NamingPlaceholder(
					EwfPage.Instance.StatusMessages.Any() && statusMessagesDisplayAsNotification()
						? new Block { CssClass = notificationSpacerClass }.ToSingleElementArray()
						: new Control[ 0 ] ) );

			var warningControls = new List<Control>();
			if( !AppTools.IsLiveInstallation ) {
				var children = new List<Control>();
				children.Add( "This is not the live system. Changes made here will be lost and are not recoverable. ".GetLiteralControl() );
				if( AppTools.IsIntermediateInstallation && AppRequestState.Instance.IntermediateUserExists ) {
					children.Add(
						new PostBackButton(
							PostBack.CreateFull(
								id: "ewfIntermediateLogOut",
								firstModificationMethod: IntermediateAuthenticationMethods.ClearCookie,
								actionGetter: () => new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) ) ),
							new ButtonActionControlStyle( "Log Out", ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
							false ) );
				}
				warningControls.Add( new PlaceHolder().AddControlsReturnThis( children.ToArray() ) );
			}
			else if( ConfigurationStatics.MachineIsStandbyServer ) {
				warningControls.Add(
					"This is a standby system. It operates with a read-only database, and any attempt to make a modification will result in an error.".GetLiteralControl() );
			}

			if( AppRequestState.Instance.UserAccessible && AppRequestState.Instance.ImpersonatorExists &&
			    ( !AppTools.IsIntermediateInstallation || AppRequestState.Instance.IntermediateUserExists ) ) {
				warningControls.Add(
					new PlaceHolder().AddControlsReturnThis(
						"User impersonation is in effect. ".GetLiteralControl(),
						EwfLink.Create(
							SelectUser.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() ),
							new ButtonActionControlStyle( "Change User", ButtonActionControlStyle.ButtonSize.ShrinkWrap ) ),
						" ".GetLiteralControl(),
						new PostBackButton(
							PostBack.CreateFull(
								id: "ewfEndImpersonation",
								firstModificationMethod: UserImpersonationStatics.EndImpersonation,
								actionGetter: () => new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) ) ),
							new ButtonActionControlStyle( "End Impersonation", ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
							usesSubmitBehavior: false ) ) );
			}

			if( warningControls.Any() ) {
				var warningControl = warningControls.Count() > 1 ? ControlStack.CreateWithControls( true, warningControls.ToArray() ) : warningControls.Single();
				ph.AddControlsReturnThis( new Block( warningControl ) { CssClass = topWarningBlockCssClass } );
			}

			// This is used by the Standard Library JavaScript file.
			const string clickBlockerId = "ewfClickBlocker";

			ph2.AddControlsReturnThis(
				new Block { ClientIDMode = ClientIDMode.Static, ID = clickBlockerId, CssClass = clickBlockerInactiveClass },
				getProcessingDialog(),
				new NamingPlaceholder( getStatusMessageControl() ) );

			EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(), "formSubmitEventHandler", "postBackRequestStarted()" );
		}

		private Control getProcessingDialog() {
			/*
			 * We switched from an animated GIF to a JavaScript-based spinner due to a number of benefits.
			 * First, IE stops animating all GIFs when a request is made, even with the latest version. They do not believe this is a bug.
			 * Firefox stops animating GIFs in other situations, such as when we hide the processing dialog when the user wants to attempt
			 * the request again.
			 * In both of the above situations, there are a number of convoluted hacks that fix the problem in some situations, but not all. One example is
			 * enumerating the images in the document in JavaScript and setting the src attribute to the value of the src attribute.
			 * The above problems still exist even if you use CSS to display the image, using the background-image style.
			 * An alternate situation may be to use a PNG and use CSS3 animations to rotate the image, making it spin. However browser support still
			 * isn't solid for CSS3 animations, such as all IE versions before IE10. 
			 * Another point against images is that you have to prevent dragging and disable selection, to make the interface look more professional.
			 * Spin.js also has the benefit of being fully compatible with all browsers across the board.
			 */

			// These are used by the Standard Library JavaScript file.
			const string dialogId = "ewfProcessingDialog";
			const string spinnerId = "ewfSpinner";

			var spinnerParent = new EwfLabel { ClientIDMode = ClientIDMode.Static, ID = spinnerId };
			spinnerParent.Style.Add( HtmlTextWriterStyle.Position, "relative" );
			spinnerParent.Style.Add( HtmlTextWriterStyle.MarginLeft, "25px" );
			spinnerParent.Style.Add( HtmlTextWriterStyle.MarginRight, "40px" );

			return
				new Block(
					new Paragraph(
						spinnerParent,
						Translation.Processing.GetLiteralControl(),
						getProcessingDialogEllipsisDot( 1 ),
						getProcessingDialogEllipsisDot( 2 ),
						getProcessingDialogEllipsisDot( 3 ) ) { CssClass = processingDialogProcessingParagraphClass },
					new Paragraph(
						new CustomButton( () => "stopPostBackRequest()" ) { ActionControlStyle = new TextActionControlStyle( Translation.ThisSeemsToBeTakingAWhile ) } )
						{
							CssClass = processingDialogTimeOutParagraphClass
						} ) { ClientIDMode = ClientIDMode.Static, ID = dialogId, CssClass = processingDialogBlockInactiveClass };
		}

		// This supports the animated ellipsis. Browsers that don't support CSS3 animations will still see the static dots.
		private Control getProcessingDialogEllipsisDot( int dotNumber ) {
			// This is used by the Basic style sheet.
			const string id = "ewfEllipsis";

			return new EwfLabel { ClientIDMode = ClientIDMode.Static, ID = "{0}{1}".FormatWith( id, dotNumber ), Text = "." };
		}

		private IEnumerable<Control> getStatusMessageControl() {
			var messagesExist = EwfPage.Instance.StatusMessages.Any();
			new ModalWindow(
				new NamingPlaceholder( messagesExist && !statusMessagesDisplayAsNotification() ? getStatusMessageControlList() : new Control[ 0 ] ),
				title: "Messages",
				open: messagesExist && !statusMessagesDisplayAsNotification() );

			// This is used by the Standard Library JavaScript file.
			const string notificationSectionContainerId = "ewfNotification";

			return messagesExist && statusMessagesDisplayAsNotification()
				       ? new Block( new Section( SectionStyle.Box, "Messages", null, getStatusMessageControlList(), false, true ) )
					       {
						       ClientIDMode = ClientIDMode.Static,
						       ID = notificationSectionContainerId,
						       CssClass = notificationSectionContainerNotificationClass
					       }.ToSingleElementArray()
				       : new Control[ 0 ];
		}

		private IEnumerable<Control> getStatusMessageControlList() {
			return
				ControlStack.CreateWithControls(
					true,
					EwfPage.Instance.StatusMessages.Select(
						i =>
						new Block(
							new FontAwesomeIcon( i.Item1 == StatusMessageType.Info ? "fa-info-circle" : "fa-exclamation-triangle", "fa-lg", "fa-fw" ),
							new EwfLabel { CssClass = statusMessageTextClass, Text = i.Item2 } )
							{
								CssClass = i.Item1 == StatusMessageType.Info ? infoMessageContainerClass : warningMessageContainerClass
							} as Control ).ToArray() )
					.ToSingleElementArray();
		}

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