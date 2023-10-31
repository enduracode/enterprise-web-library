#nullable disable
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using Humanizer;

// EwlPage
// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

partial class ContactSupport {
	protected override bool userCanAccess => AppTools.User != null;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		var body = new DataValue<string>();
		return FormState.ExecuteWithDataModificationsAndDefaultAction(
			PostBack.CreateFull(
					modificationMethod: () => {
						var message = new EmailMessage
							{
								Subject = "Support request from {0} in {1}".FormatWith(
									AppTools.User.FriendlyName.Any() ? AppTools.User.FriendlyName : AppTools.User.Email,
									ConfigurationStatics.SystemDisplayName ),
								BodyHtml = body.Value.GetTextAsEncodedHtml()
							};
						message.ReplyToAddresses.Add( new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ) );
						message.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
						EmailStatics.SendEmailWithDefaultFromAddress( message );
						AddStatusMessage( StatusMessageType.Info, "Your message has been sent." );
					},
					actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) )
				.ToCollection(),
			() => new UiPageContent( contentFootActions: new ButtonSetup( "Send Message" ).ToCollection() )
				.Add( new Paragraph( "You may report any problems, make suggestions, or ask for help here.".ToComponents() ) )
				.Add(
					FormItemList.CreateStack(
						items: new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ).ToMailAddress()
							.ToString()
							.ToComponents()
							.ToFormItem( label: "From".ToComponents() )
							.Append(
								"{0} ({1} for this system)".FormatWith(
										StringTools.GetEnglishListPhrase( EmailStatics.GetAdministratorEmailAddresses().Select( i => i.DisplayName ), true ),
										"support contacts".ToQuantity( EmailStatics.GetAdministratorEmailAddresses().Count(), showQuantityAs: ShowQuantityAs.None ) )
									.ToComponents()
									.ToFormItem( label: "To".ToComponents() ) )
							.Append(
								body.ToTextControl( false, setup: TextControlSetup.Create( numberOfRows: 10 ), value: "" ).ToFormItem( label: "Message".ToComponents() ) )
							.Materialize() ) ) );
	}
}