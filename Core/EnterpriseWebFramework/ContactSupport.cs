using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

// EwlPage
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	partial class ContactSupport {
		private readonly DataValue<string> body = new DataValue<string>();

		protected override bool userCanAccessResource => AppTools.User != null;

		protected override PageContent getContent() =>
			FormState.ExecuteWithDataModificationsAndDefaultAction(
				PostBack.CreateFull( modificationMethod: modifyData, actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ).ToCollection(),
				() => {
					var content = new UiPageContent( contentFootActions: new ButtonSetup( "Send Message" ).ToCollection() );

					content.Add( new Paragraph( "You may report any problems, make suggestions, or ask for help here.".ToComponents() ) );

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
					content.Add( list );

					return content;
				} );

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