using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Page;
using System;

// OptionalParameter: string returnUrl
// OptionalParameter: DateTime date

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class CalendarDemo: EwfPage, PageWithRightButton {
		partial class Info {
			partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package ) {
				package.ReturnUrl = ActionControls.GetInfo().GetUrl();
				package.Date = DateTime.Now;
			}

			protected override void init( DBConnection cn ) {}

			protected override AlternativePageMode createAlternativeMode() {
				return new NewContentPageMode();
			}
		}

		protected override void LoadData( DBConnection cn ) {
			calendar.SetParameters( info.Date, date => parametersModification.Date = date );
		}

		NavButtonSetup PageWithRightButton.CreateRightButtonSetup() {
			return new NavButtonSetup( "OK", info.ReturnUrl );
		}
	}
}