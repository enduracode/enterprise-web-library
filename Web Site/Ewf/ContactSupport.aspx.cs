using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class ContactSupport: EwfPage {
		partial class Info {
			protected override bool userCanAccessResource => AppTools.User != null;
		}

		private readonly DataValue<string> body = new DataValue<string>();

		protected override void loadData() {
			ph.AddControlsReturnThis( new LegacyParagraph( "You may report any problems, make suggestions, or ask for help here." ) );

			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull( firstModificationMethod: modifyData, actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) )
					.ToCollection(),
				() => {
					var list = FormItemList.CreateStack();
					list.AddFormItems(
						new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ).ToMailAddress()
							.ToString()
							.ToComponents()
							.ToFormItem( label: "From".ToComponents() ),
						"{0} ({1} for this system)".FormatWith(
								StringTools.GetEnglishListPhrase( EmailStatics.GetAdministratorEmailAddresses().Select( i => i.DisplayName ), true ),
								"support contacts".ToQuantity( EmailStatics.GetAdministratorEmailAddresses().Count(), showQuantityAs: ShowQuantityAs.None ) )
							.ToComponents()
							.ToFormItem( label: "To".ToComponents() ),
						body.ToTextControl( false, setup: TextControlSetup.Create( numberOfRows: 10 ), value: "" ).ToFormItem( label: "Message".ToComponents() ) );
					ph.AddControlsReturnThis( list.ToCollection().GetControls() );

					EwfUiStatics.SetContentFootActions( new ButtonSetup( "Send Message" ).ToCollection() );
				} );
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