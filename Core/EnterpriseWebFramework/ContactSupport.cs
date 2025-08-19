using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.SystemSpecificLogic;
using EnterpriseWebLibrary.UserManagement;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlPage
// Parameter: returnUrl
partial class ContactSupport {
	private TrustedResourceInfo returnResource = null!;

	protected override void init() {
		returnResource = ReturnUrl.GetResourceOrThrow();
	}

	protected override bool userCanAccess => SystemUser.Current is not null;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	protected override PageContent getContent() {
		var body = new DataValue<string>( false );
		return FormState.ExecuteWithActions(
			PostBack.CreateFull(
				modificationMethod: () => {
					var message = new EmailMessage
						{
							Subject = "Support request from {0} in {1}".FormatWith(
								SystemUser.Current!.FriendlyName.Any() ? SystemUser.Current.FriendlyName : SystemUser.Current.Email,
								SystemSpecificLogicStatics.SystemDisplayName ),
							BodyHtml = body.Value.GetTextAsEncodedHtml()
						};
					message.ReplyToAddresses.Add( new EmailAddress( SystemUser.Current.Email, SystemUser.Current.FriendlyName ) );
					message.ToAddresses.AddRange( EmailStatics.GetAdministratorEmailAddresses() );
					EmailStatics.SendEmailWithDefaultFromAddress( message );
					AddStatusMessage( StatusMessageType.Info, "Your message has been sent." );
				},
				actionGetter: () => new PostBackAction( returnResource ) ),
			() => new UiPageContent( contentFootActions: new ButtonSetup( "Send Message" ) )
				.Add( new Paragraph( "You may report any problems, make suggestions, or ask for help here.".ToComponents() ) )
				.Add(
					FormItemList.CreateStack()
						.AddItems(
							new EmailAddress( SystemUser.Current!.Email, SystemUser.Current.FriendlyName ).ToMailAddress()
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