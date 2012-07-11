using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.TestWebSite.TestPages {
	public partial class Email: EwfPage, DataModifierWithRightButton {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			to.Value = "somewhere@test.com";
		}

		string DataModifierWithRightButton.RightButtonText { get { return "Send"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
			//AppTools.SendEmailWithDefaultFromAddress( new EmailMessage
			//                                            {
			//                                              ToAddresses = to.Value.Split( ',' ).Select( em => new EmailAddress( em ) ).ToList(),
			//                                              Subject = subject.Value,
			//                                              BodyHtml = body.GetPostBackValue()
			//                                            } );
			return "";
		}
	}
}