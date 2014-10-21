using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

// OptionalParameter: string returnUrl
// OptionalParameter: DateTime date

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class CalendarDemo: EwfPage {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.ReturnUrl = ActionControls.GetInfo().GetUrl();
				package.Date = DateTime.Now;
			}

			protected override AlternativeResourceMode createAlternativeMode() {
				return new NewContentResourceMode();
			}
		}

		protected override void loadData() {
			calendar.SetParameters( info.Date, date => parametersModification.Date = date );
			EwfUiStatics.SetContentFootActions( ActionButtonSetup.CreateWithUrl( "OK", new ExternalResourceInfo( info.ReturnUrl ) ) );
		}
	}
}