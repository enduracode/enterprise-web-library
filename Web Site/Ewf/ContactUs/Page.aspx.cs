using System;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactUs {
	public partial class Page: EwfPage {
		partial class Info {
			public override string PageName { get { return ""; } }
		}

		private string emailText;

		protected override void loadData() {
			var dm = new DataModification();

			ph.AddControlsReturnThis(
				FormItem.Create( "You may report any problems, make suggestions, or ask for help here.",
				                 new EwfTextBox( "" ) { Rows = 20 },
				                 validationGetter:
					                 control =>
					                 new Validation(
						                 ( pbv, validator ) => emailText = validator.GetString( new ValidationErrorHandler( "text" ), control.GetPostBackValue( pbv ), false ), dm ) )
				        .ToControl() );

			EwfUiStatics.SetContentFootActions( new ActionButtonSetup( "Send", new PostBackButton( dm, () => EhRedirect( new ExternalPageInfo( info.ReturnUrl ) ) ) ) );

			dm.AddModificationMethod( modifyData );
		}

		private void modifyData() {
			var message = new EmailMessage
				{
					Subject = "Contact from " + AppTools.SystemName,
					BodyHtml = ( "Contact from " + AppTools.User.Email + Environment.NewLine + Environment.NewLine + emailText ).GetTextAsEncodedHtml()
				};
			message.ToAddresses.AddRange( AppTools.AdministratorEmailAddresses );
			message.ReplyToAddresses.Add( new EmailAddress( AppTools.User.Email ) );
			AppTools.SendEmailWithDefaultFromAddress( message );
			AddStatusMessage( StatusMessageType.Info, "Your feedback has been sent." );
		}
	}
}