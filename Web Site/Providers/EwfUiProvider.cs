using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.WebSite.TestPages;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.WebSessionState;

namespace EnterpriseWebLibrary.WebSite.Providers {
	internal class EwfUiProvider: AppEwfUiProvider {
		public override List<ActionButtonSetup> GetGlobalNavActionControls() {
			var navButtonSetups = new List<ActionButtonSetup>();

			// This will hide itself because Contact Us requires a logged-in user, and the standard library test web site has no users.
			navButtonSetups.Add( new ActionButtonSetup( "Contact us",
			                                            new EwfLink(
				                                            RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactUs.Page.GetInfo(
					                                            EwfPage.Instance.InfoAsBaseType.GetUrl() ) ) ) );

			var menu = EwfTable.Create();
			menu.AddItem(
				() =>
				new EwfTableItem(
					new EwfTableItemSetup(
						clickScript: ClickScript.CreatePostBackScript( delegate { EwfPage.AddStatusMessage( StatusMessageType.Info, "Successful method execution." ); } ) ),
					"Test method".ToCell() ) );
			navButtonSetups.Add( new ActionButtonSetup( "Test", new ToolTipButton( menu ) ) );

			navButtonSetups.Add( new ActionButtonSetup( "Calendar",
			                                            new EwfLink( CalendarDemo.GetInfo( new EntitySetup.OptionalParameterPackage(),
			                                                                               new CalendarDemo.OptionalParameterPackage
				                                                                               {
					                                                                               ReturnUrl = EwfPage.Instance.InfoAsBaseType.GetUrl(),
					                                                                               Date = DateTime.Now
				                                                                               } ) ) ) );
			return navButtonSetups;
		}

		public override List<LookupBoxSetup> GetGlobalNavLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", delegate { return null; } ) );
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", delegate { return null; } ) );
			return lookupBoxSetups;
		}
	}
}