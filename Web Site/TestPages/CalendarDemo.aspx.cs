using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;

// OptionalParameter: string returnUrl
// OptionalParameter: DateTime date

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CalendarDemo: EwfPage {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.ReturnUrl = ActionControls.GetInfo().GetUrl();
				package.Date = DateTime.Now;
			}

			protected override AlternativePageMode createAlternativeMode() {
				return new NewContentPageMode();
			}
		}

		protected override void loadData() {
			calendar.SetParameters( info.Date, date => parametersModification.Date = date );
			EwfUiStatics.SetContentFootActions( ActionButtonSetup.CreateWithUrl( "OK", new ExternalPageInfo( info.ReturnUrl ) ) );
		}
	}
}