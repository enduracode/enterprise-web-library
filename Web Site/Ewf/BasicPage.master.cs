using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary.Configuration;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.WebSessionState;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
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
				                              ControlStack.CssElementCreator.Selectors.SelectMany(
					                              stack =>
					                              ControlLine.CssElementCreator.Selectors.Select(
						                              line =>
						                              statusMessageDialogBlockSelector + " > " + line + " " + stack + " > " + ControlStack.CssElementCreator.ItemSelector + " > " +
						                              "span." + StatusMessageDialogControlListWarningItemCssClass ) ).ToArray() ) );

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
			basicBody.Attributes.Add( "onpagehide", "hideProcessingDialog();hideClickBlocker();" );
			form.Action = EwfPage.Instance.InfoAsBaseType.GetUrl();

			if( !AppTools.IsLiveInstallation ) {
				var children = new List<Control>();
				children.Add( "This is not the live installation of the system. All changes made here will be lost and are not recoverable. ".GetLiteralControl() );
				if( AppTools.IsIntermediateInstallation && AppRequestState.Instance.IntermediateUserExists ) {
					children.Add(
						new PostBackButton(
							PostBack.CreateFull(
								id: "ewfIntermediateLogOut",
								firstModificationMethod: IntermediateAuthenticationMethods.ClearCookie,
								actionGetter: () => new PostBackAction( new ExternalResourceInfo( NetTools.HomeUrl ) ) ),
							new ButtonActionControlStyle( "Log Out" ),
							false ) );
				}

				// We can't use CssClasses here even though it looks like we can. It compiles here but not in client systems because the namespaces are wrong, or something.
				ph.AddControlsReturnThis( new Block( children.ToArray() ) { CssClass = "ewfNonLiveWarning" } );
			}
			else if( ConfigurationStatics.MachineIsStandbyServer ) {
				// We can't use CssClasses here even though it looks like we can. It compiles here but not in client systems because the namespaces are wrong, or something.
				ph.AddControlsReturnThis(
					new Block(
						"This is a standby version of the system. This operates off a read-only database, and any attempt to make a modification will result in an error."
							.GetLiteralControl() ) { CssClass = "ewfNonLiveWarning" } );
			}

			ph2.AddControlsReturnThis( new Block { CssClass = CssElementCreator.ClickBlockingBlockCssClass }, getProcessingDialog() );

			var statusControls = new NamingPlaceholder( new Control[ 0 ] );
			var dialogStatusControls = new NamingPlaceholder( new Control[ 0 ] );
			addStatusMessageControls( statusControls, dialogStatusControls );
			ph2.AddControlsReturnThis( dialogStatusControls );

			if( EwfPage.Instance.Master is EwfUi )
				( (EwfUi)EwfPage.Instance.Master ).AddStatusStatusMessages( statusControls );
			else
				ph.AddControlsReturnThis( statusControls );


			var ajaxLoadingImage = new EwfImage( "Images/ajax-loader.gif" ) { CssClass = "ajaxloaderImage" };
			ajaxLoadingImage.Style.Add( "display", "none" );
			ph2.AddControlsReturnThis( ajaxLoadingImage );
			EwfPage.Instance.ClientScript.RegisterOnSubmitStatement( GetType(), "formSubmitEventHandler", "postBackRequestStarted()" );
		}

		private Control getProcessingDialog() {
			/*
			 * We switched to a JavaScript-based spinner due to a number of benefits.
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
			var image = new Literal { Text = "<span id='spinner'>&nbsp;</span>" };

			// This supports the animated ellipsis. Browsers that don't support CSS3 animations will still see the static dots.
			Func<int, Control> getEllipsis = n => new LiteralControl { Text = "<span id='ellipsis{0}'>.</span>".FormatWith( n ) };
			return
				new Block(
					new Paragraph( image,
					               " ".GetLiteralControl(),
					               new Literal { Text = @"<span id=""ewfProcessing"">{0}</span>".FormatWith( Translation.Processing ) },
					               getEllipsis( 1 ),
					               getEllipsis( 2 ),
					               getEllipsis( 3 ) ) { CssClass = "ewfProcessingDialogProgress" },
					new Paragraph( new CustomButton( () => "stopPostBackRequest()" )
						{
							ActionControlStyle = new TextActionControlStyle( Translation.ThisSeemsToBeTakingAWhile )
						} )
						{
							CssClass = "ewfTimeOut"
							/* This is used by the Standard Library JavaScript file. */
						} ) { CssClass = CssElementCreator.ProcessingDialogBlockCssClass };
		}

		private static void addStatusMessageControls( Control staticControlsContainer, Control dialogControlContainer ) {
			var messages = EwfPage.Instance.StatusMessages;

			if( !messages.Any() )
				return;

			var controls = new List<Control>();
			var staticControls = new List<Control>();

			var infoMessagesExist = messages.Any( i => i.Item1 == StatusMessageType.Info );
			var warningMessagesExist = messages.Any( i => i.Item1 == StatusMessageType.Warning );

			if( infoMessagesExist ) {
				Func<Control> getInfoControlLine =
					() =>
					new ControlLine( new Literal { Text = @"<i class='fa fa-info-circle fa-2x' style='color: rgb(120, 160, 195);'></i>" },
					                 getStackFromStatuses( messages.Where( message => message.Item1 == StatusMessageType.Info ),
					                                       CssElementCreator.StatusMessageDialogControlListInfoItemCssClass ) );

				controls.Add( getInfoControlLine() );
				staticControls.Add( getInfoControlLine() );
			}


			if( infoMessagesExist && warningMessagesExist )
				controls.Add( new Literal { Text = "<hr />" } );

			if( warningMessagesExist ) {
				Func<Control> getWarningControlLine =
					() =>
					new ControlLine( new Literal { Text = @"<i class='fa fa-exclamation-triangle fa-2x' style='color: #E69017;'></i>" },
					                 getStackFromStatuses( messages.Where( message => message.Item1 == StatusMessageType.Warning ),
					                                       CssElementCreator.StatusMessageDialogControlListWarningItemCssClass ) );

				controls.Add( getWarningControlLine() );
				staticControls.Add( getWarningControlLine() );

				var alignRight = new HtmlGenericControl( "div" );
				alignRight.Style[ "text-align" ] = "right";
				alignRight.AddControlsReturnThis( new CustomButton( () => "fadeOutStatusMessageDialog( 0 ); hideClickBlocker();" )
					{
						ActionControlStyle = new ButtonActionControlStyle( "OK" )
					} );
				controls.Add( alignRight );
			}

			dialogControlContainer.AddControlsReturnThis( new Block( controls.ToArray() ) { CssClass = CssElementCreator.StatusMessageDialogBlockCssClass } );
			staticControlsContainer.AddControlsReturnThis( ControlStack.CreateWithControls( true, staticControls.ToArray() ) );
		}

		private static ControlStack getStackFromStatuses( IEnumerable<Tuple<StatusMessageType, string>> iEnumerable, string itemCssClass ) {
			return ControlStack.CreateWithControls( true, ( iEnumerable.Select( message => new Label { CssClass = itemCssClass, Text = message.Item2 } ) ).ToArray() );
		}
	}
}