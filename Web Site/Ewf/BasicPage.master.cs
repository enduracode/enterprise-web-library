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
	public partial class BasicPage: MasterPage, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			internal const string TopWarningBlockCssClass = "ewfTopWarning";

			// Some of these are used by the Standard Library JavaScript file.
			internal const string ClickBlockingBlockCssClass = "ewfClickBlocker";
			internal const string ProcessingDialogBlockCssClass = "ewfProcessingDialog";
			internal const string StatusMessageDialogBlockCssClass = "ewfStatusMessageDialog";
			internal const string StatusMessageDialogControlListInfoItemCssClass = "ewfStatusMessageDialogInfoMessage";
			internal const string StatusMessageDialogControlListWarningItemCssClass = "ewfStatusMessageDialogWarningMessage";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				// Some of the elements below cover a subset of other CSS elements in a more specific way. See the comment in EwfUi.master for more information.

				var elements = new List<CssElement>();

				elements.Add( new CssElement( "TopWarningBlock", "div.{0}".FormatWith( TopWarningBlockCssClass ) ) );

				elements.Add( new CssElement( "ClickBlockingBlock", "div." + ClickBlockingBlockCssClass ) );

				const string processingDialogBlockSelector = "div." + ProcessingDialogBlockCssClass;
				elements.Add( new CssElement( "ProcessingDialogBlock", processingDialogBlockSelector ) );
				elements.Add( new CssElement( "ProcessingDialogParagraph", processingDialogBlockSelector + " > p" ) );

				const string statusMessageDialogBlockSelector = "div." + StatusMessageDialogBlockCssClass;
				elements.Add( new CssElement( "StatusMessageDialogBlock", statusMessageDialogBlockSelector ) );
				elements.Add(
					new CssElement(
						"StatusMessageDialogControlListInfoItem",
						ControlStack.CssElementCreator.Selectors.Select(
							i =>
							statusMessageDialogBlockSelector + " > " + i + " > " + ControlStack.CssElementCreator.ItemSelector + " > " + "span." +
							StatusMessageDialogControlListInfoItemCssClass ).ToArray() ) );
				elements.Add(
					new CssElement(
						"StatusMessageDialogControlListWarningItem",
						ControlStack.CssElementCreator.Selectors.Select(
							i =>
							statusMessageDialogBlockSelector + " > " + i + " > " + ControlStack.CssElementCreator.ItemSelector + " > " + "span." +
							StatusMessageDialogControlListWarningItemCssClass ).ToArray() ) );

				return elements.ToArray();
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
			basicBody.Attributes.Add( "onpagehide", "hideProcessingDialog();" );
			form.Action = EwfPage.Instance.InfoAsBaseType.GetUrl();

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
				ph.AddControlsReturnThis( new Block( warningControl ) { CssClass = CssElementCreator.TopWarningBlockCssClass } );
			}

			ph2.AddControlsReturnThis( new Block { CssClass = CssElementCreator.ClickBlockingBlockCssClass }, getProcessingDialog() );
			ph2.AddControlsReturnThis( new NamingPlaceholder( getStatusMessageDialog() ) );

			var ajaxLoadingImage = new EwfImage( "Images/ajax-loader.gif" ) { CssClass = "ajaxloaderImage" };
			ajaxLoadingImage.Style.Add( "display", "none" );
			ph2.AddControlsReturnThis( ajaxLoadingImage );

			EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(), "formSubmitEventHandler", "postBackRequestStarted()" );
		}

		private Control getProcessingDialog() {
			var image = new EwfImage( "Images/Progress.gif" );
			image.Style.Add( "display", "inline" );
			return new Block(
				new Paragraph( image, " ".GetLiteralControl(), Translation.Processing.GetLiteralControl() ),
				new Paragraph(
					new CustomButton( () => "stopPostBackRequest()" ) { ActionControlStyle = new TextActionControlStyle( Translation.ThisSeemsToBeTakingAWhile ) } )
					{
						CssClass = "ewfTimeOut"
						/* This is used by the Standard Library JavaScript file. */
					} ) { CssClass = CssElementCreator.ProcessingDialogBlockCssClass };
		}

		private IEnumerable<Control> getStatusMessageDialog() {
			if( !EwfPage.Instance.StatusMessages.Any() )
				yield break;

			var list = ControlStack.Create( true );
			list.AddControls(
				EwfPage.Instance.StatusMessages.Select(
					i =>
					new Label
						{
							CssClass =
								i.Item1 == StatusMessageType.Info
									? CssElementCreator.StatusMessageDialogControlListInfoItemCssClass
									: CssElementCreator.StatusMessageDialogControlListWarningItemCssClass,
							Text = i.Item2
						} as Control ).ToArray() );

			if( EwfPage.Instance.StatusMessages.Any( i => i.Item1 == StatusMessageType.Warning ) )
				list.AddControls( new CustomButton( () => "fadeOutStatusMessageDialog( 0 )" ) { ActionControlStyle = new ButtonActionControlStyle( "OK" ) } );

			yield return new Block( list ) { CssClass = CssElementCreator.StatusMessageDialogBlockCssClass };
		}
	}
}