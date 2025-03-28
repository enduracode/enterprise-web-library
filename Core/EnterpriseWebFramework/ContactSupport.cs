#nullable disable
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.SystemSpecificLogic;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage
// Parameter: string returnUrl
partial class ContactSupport {
	protected override bool userCanAccess => AppTools.User != null;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		var body = new DataValue<string>( false );
		return FormState.ExecuteWithActions(
			PostBack.CreateFull(
				modificationMethod: () => {
					var message = new EmailMessage
						{
							Subject = "Support request from {0} in {1}".FormatWith(
								AppTools.User.FriendlyName.Any() ? AppTools.User.FriendlyName : AppTools.User.Email,
								SystemSpecificLogicStatics.SystemDisplayName ),
							BodyHtml = body.Value.GetTextAsEncodedHtml()
						};
					message.ReplyToAddresses.Add( new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ) );
					message.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
					EmailStatics.SendEmailWithDefaultFromAddress( message );
					AddStatusMessage( StatusMessageType.Info, "Your message has been sent." );
				},
				actionGetter: () => new PostBackAction( new ExternalResource( ReturnUrl ) ) ),
			() => new UiPageContent( contentFootActions: new ButtonSetup( "Send Message" ) )
				.Add( new Paragraph( "You may report any problems, make suggestions, or ask for help here.".ToComponents() ) )
				.Add(
					FormItemList.CreateStack()
						.AddItems(
							new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ).ToMailAddress()
								.ToString()
								.ToComponents()
								.ToFormItem( label: "From".ToComponents() )
								.Append(
									"{0} ({1} for this system)".FormatWith(
											StringTools.GetEnglishListPhrase( EmailStatics.GetAdministratorEmailAddresses().Select( i => i.DisplayName ), true ),
											"support contacts".ToQuantity( EmailStatics.GetAdministratorEmailAddresses().Count(), showQuantityAs: ShowQuantityAs.None ) )
										.ToComponents()
										.ToFormItem( label: "To".ToComponents() ) )
								.Append( body.ToTextControl( false, setup: TextControlSetup.Create( numberOfRows: 10 ) ).ToFormItem( label: "Message".ToComponents() ) )
								.Materialize() ) ) );
	}
}