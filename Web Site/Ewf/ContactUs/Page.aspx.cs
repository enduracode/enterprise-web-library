using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.Validation;
using RedStapler.StandardLibrary.WebSessionState;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactUs {
	public partial class Page: EwfPage, DataModifierWithRightButton {
		partial class Info {
			protected override void init( DBConnection cn ) {}
			public override string PageName { get { return ""; } }
		}

		protected override void LoadData( DBConnection cn ) {}

		string DataModifierWithRightButton.RightButtonText { get { return "Send"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
			var message = new EmailMessage
			              	{
			              		Subject = "Contact from " + AppTools.SystemName,
			              		BodyHtml = ( "Contact from " + AppTools.User.Email + Environment.NewLine + Environment.NewLine + emailText.Value ).GetTextAsEncodedHtml()
			              	};
			message.ToAddresses.AddRange( AppTools.AdministratorEmailAddresses );
			message.ReplyToAddresses.Add( new EmailAddress( AppTools.User.Email ) );
			AppTools.SendEmailWithDefaultFromAddress( message );
			StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "Your feedback has been sent." );
			return info.ReturnUrl;
		}
	}
}