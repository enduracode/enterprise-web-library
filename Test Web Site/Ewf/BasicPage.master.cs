using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.CssHandling;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf {
	public partial class BasicPage: MasterPage, ControlTreeDataLoader {
		internal class CssElementCreator: ControlCssElementCreator {
			// Some of these are used by the Standard Library JavaScript file.
			internal const string ClickBlockingBlockCssClass = "ewfClickBlocker";
			internal const string ProcessingDialogBlockCssClass = "ewfProcessingDialog";
			internal const string StatusMessageDialogBlockCssClass = "ewfStatusMessageDialog";
			internal const string StatusMessageDialogControlListInfoItemCssClass = "ewfStatusMessageDialogInfoMessage";
			internal const string StatusMessageDialogControlListWarningItemCssClass = "ewfStatusMessageDialogWarningMessage";

			CssElement[] ControlCssElementCreator.CreateCssElements() {
				// Some of the elements below cover a subset of other CSS elements in a more specific way. See the comment in EwfUi.master for more information.

				var elements = new List<CssElement>();
				elements.Add( new CssElement( "ClickBlockingBlock", "div." + ClickBlockingBlockCssClass ) );

				const string processingDialogBlockSelector = "div." + ProcessingDialogBlockCssClass;
				elements.Add( new CssElement( "ProcessingDialogBlock", processingDialogBlockSelector ) );
				elements.Add( new CssElement( "ProcessingDialogParagraph", processingDialogBlockSelector + " > p" ) );

				const string statusMessageDialogBlockSelector = "div." + StatusMessageDialogBlockCssClass;
				elements.Add( new CssElement( "StatusMessageDialogBlock", statusMessageDialogBlockSelector ) );
				elements.Add( new CssElement( "StatusMessageDialogControlListInfoItem",
				                              ControlStack.CssElementCreator.Selectors.Select(
				                              	i =>
				                              	statusMessageDialogBlockSelector + " > " + i + " > " + ControlStack.CssElementCreator.ItemSelector + " > " + "span." +
				                              	StatusMessageDialogControlListInfoItemCssClass ).ToArray() ) );
				elements.Add( new CssElement( "StatusMessageDialogControlListWarningItem",
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

		void ControlTreeDataLoader.LoadData( DBConnection cn ) {
			basicBody.Attributes.Add( "onpagehide", "hideProcessingDialog();" );
			form.Action = EwfPage.Instance.InfoAsBaseType.GetUrl();

			if( !AppTools.IsLiveInstallation ) {
				var children = new List<Control>();
				children.Add( "This is not the live installation of the system. All changes made here will be lost and are not recoverable. ".GetLiteralControl() );
				if( AppTools.IsIntermediateInstallation && AppRequestState.Instance.IntermediateUserExists ) {
					children.Add( new PostBackButton( new DataModification(),
					                                  () => EwfPage.Instance.EhModifyDataAndRedirect( delegate {
					                                  	IntermediateAuthenticationMethods.ClearCookie();
					                                  	return NetTools.HomeUrl;
					                                  } ),
					                                  new ButtonActionControlStyle( "Log Out" ),
					                                  false ) );
				}

				// We can't use CssClasses here even though it looks like we can. It compiles here but not in client systems because the namespaces are wrong, or something.
				ph.AddControlsReturnThis( new Block( children.ToArray() ) { CssClass = "ewfNonLiveWarning" } );
			}
			else if( AppTools.IsStandbyServer ) {
				// We can't use CssClasses here even though it looks like we can. It compiles here but not in client systems because the namespaces are wrong, or something.
				ph.AddControlsReturnThis(
					new Block(
						"This is a standby version of the system. This operates off a read-only database, and any attempt to make a modification will result in an error.".
							GetLiteralControl() ) { CssClass = "ewfNonLiveWarning" } );
			}

			ph2.AddControlsReturnThis( new Block { CssClass = CssElementCreator.ClickBlockingBlockCssClass }, getProcessingDialog() );
			if( EwfPage.Instance.StatusMessages.Any() )
				ph2.AddControlsReturnThis( getStatusMessageDialog() );

			var ajaxLoadingImage = new EwfImage( "Images/ajax-loader.gif" ) { CssClass = "ajaxloaderImage" };
			ajaxLoadingImage.Style.Add( "display", "none" );
			ph2.AddControlsReturnThis( ajaxLoadingImage );

			EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(), "formSubmitEventHandler", "postBackRequestStarted();" );
		}

		private static Control getProcessingDialog() {
			var image = new EwfImage( "Images/Progress.gif" );
			image.Style.Add( "display", "inline" );
			return new Block( new Paragraph( image, " ".GetLiteralControl(), Translation.Processing.GetLiteralControl() ),
			                  new Paragraph( new CustomButton( "stopPostBackRequest()" )
			                                 	{ ActionControlStyle = new TextActionControlStyle( Translation.ThisSeemsToBeTakingAWhile ) } )
			                  	{ CssClass = "ewfTimeOut" /* This is used by the Standard Library JavaScript file. */ } )
			       	{ CssClass = CssElementCreator.ProcessingDialogBlockCssClass };
		}

		private static Control getStatusMessageDialog() {
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
						} ).ToArray() );

			if( EwfPage.Instance.StatusMessages.Any( i => i.Item1 == StatusMessageType.Warning ) )
				list.AddControls( new CustomButton( "fadeOutStatusMessageDialog( 0 )" ) { ActionControlStyle = new ButtonActionControlStyle( "OK" ) } );

			return new Block( list ) { CssClass = CssElementCreator.StatusMessageDialogBlockCssClass };
		}
	}
}