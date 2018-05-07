using System.Linq;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class ContactSupport: EwfPage {
		partial class Info {
			protected override bool userCanAccessResource => AppTools.User != null;
		}

		private readonly DataValue<string> body = new DataValue<string>();

		protected override void loadData() {
			ph.AddControlsReturnThis( new LegacyParagraph( "You may report any problems, make suggestions, or ask for help here." ) );

			var pb = PostBack.CreateFull( firstModificationMethod: modifyData, actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) );
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				pb.ToCollection(),
				() => {
					var table = FormItemBlock.CreateFormItemTable();
					table.AddFormItems(
						FormItem.Create(
							"From",
							new PlaceHolder().AddControlsReturnThis(
								new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ).ToMailAddress().ToString().ToComponents().GetControls() ) ),
						FormItem.Create(
							"To",
							new PlaceHolder().AddControlsReturnThis(
								"{0} ({1} for this system)".FormatWith(
										StringTools.GetEnglishListPhrase( EmailStatics.GetAdministratorEmailAddresses().Select( i => i.DisplayName ), true ),
										"support contacts".ToQuantity( EmailStatics.GetAdministratorEmailAddresses().Count(), showQuantityAs: ShowQuantityAs.None ) )
									.ToComponents()
									.GetControls() ) ),
						body.ToTextControl( false, setup: TextControlSetup.Create( numberOfRows: 10 ), value: "" ).ToFormItem( label: "Message".ToComponents() ) );
					ph.AddControlsReturnThis( table );
				} );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Send Message", new PostBackButton( pb ) ) );
		}

		private void modifyData() {
			var message = new EmailMessage
				{
					From = new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ),
					Subject = "Contact from " + ConfigurationStatics.SystemName,
					BodyHtml = body.Value.GetTextAsEncodedHtml()
				};
			message.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
			EmailStatics.SendEmail( message );
			AddStatusMessage( StatusMessageType.Info, "Your message has been sent." );
		}
	}
}