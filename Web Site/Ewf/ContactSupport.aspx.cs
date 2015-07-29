using System.Linq;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.InputValidation;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;

// Parameter: string returnUrl

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	partial class ContactSupport: EwfPage {
		partial class Info {
			protected override bool userCanAccessResource { get { return AppTools.User != null; } }
		}

		private readonly DataValue<string> body = new DataValue<string>();

		protected override void loadData() {
			ph.AddControlsReturnThis( new Paragraph( "You may report any problems, make suggestions, or ask for help here." ) );

			var pb = PostBack.CreateFull( actionGetter: () => new PostBackAction( new ExternalResourceInfo( info.ReturnUrl ) ) );

			var table = FormItemBlock.CreateFormItemTable();
			table.AddFormItems(
				FormItem.Create( "From", new EmailAddress( AppTools.User.Email, AppTools.User.FriendlyName ).ToMailAddress().ToString().GetLiteralControl() ),
				FormItem.Create(
					"To",
					"{0} ({1} for this system)".FormatWith(
						StringTools.GetEnglishListPhrase( EmailStatics.GetAdministratorEmailAddresses().Select( i => i.DisplayName ), true ),
						"support contacts".ToQuantity( EmailStatics.GetAdministratorEmailAddresses().Count(), showQuantityAs: ShowQuantityAs.None ) ).GetLiteralControl() ),
				FormItem.Create(
					"Message",
					new EwfTextBox( "", rows: 10 ),
					validationGetter:
						control =>
						new EwfValidation(
							( pbv, validator ) => body.Value = validator.GetString( new ValidationErrorHandler( "message" ), control.GetPostBackValue( pbv ), false ),
							pb ) ) );
			ph.AddControlsReturnThis( table );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Send Message", new PostBackButton( pb ) ) );

			pb.AddModificationMethod( modifyData );
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