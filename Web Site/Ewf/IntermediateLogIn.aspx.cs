using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using RedStapler.StandardLibrary.Validation;

// Parameter: string returnUrl

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite {
	public partial class IntermediateLogIn: EwfPage, DataModifierWithRightButton {
		public partial class Info {
			protected override void init( DBConnection cn ) {
				// This guarantees that the page will always be secure, even for non intermediate installations.
				if( !EwfApp.SupportsSecureConnections )
					throw new ApplicationException();
			}

			public override string PageName { get { return "Non-Live Installation Log In"; } }
			protected override bool IsIntermediateInstallationPublicPage { get { return true; } }
		}

		protected override void LoadData( DBConnection cn ) {
			password.MasksCharacters = true;
		}

		string DataModifierWithRightButton.RightButtonText { get { return "Log In"; } }

		void DataModifierWithRightButton.ValidateFormValues( Validator validator ) {}

		string DataModifierWithRightButton.ModifyData( DBConnection cn ) {
			// NOTE: Using a single password here is a hack. The real solution is being able to use RSIS credentials, which is a goal.
			var passwordMatch = password.Value == AppTools.SystemProvider.IntermediateLogInPassword;
			if( !passwordMatch )
				throw new EwfException( "Incorrect password." );

			IntermediateAuthenticationMethods.SetCookie();
			AppRequestState.Instance.IntermediateUserExists = true;

			return info.ReturnUrl;
		}
	}
}