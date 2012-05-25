using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.WebSessionState;
using RedStapler.TestWebSite.TestPages;

namespace RedStapler.TestWebSite.Providers {
	internal class EwfUiProvider: AppEwfUiProvider {
		public override List<ActionButtonSetup> GetGlobalNavActionControls() {
			var navButtonSetups = new List<ActionButtonSetup>();

			// This will hide itself because Contact Us requires a logged-in user, and the standard library test web site has no users.
			navButtonSetups.Add( new ActionButtonSetup( "Contact us",
			                                            new EwfLink(
			                                            	StandardLibrary.EnterpriseWebFramework.RedStapler.TestWebSite.Ewf.ContactUs.Page.GetInfo(
			                                            		EwfPage.Instance.InfoAsBaseType.GetUrl() ) ) ) );

			var menu = EwfTable.Create();
			menu.AddItem(
				() =>
				new EwfTableItem( new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( ChecklistDemo.GetInfo() ) ),
				                  ChecklistDemo.GetInfo().PageName.ToCell() ) );
			menu.AddItem(
				() =>
				new EwfTableItem(
					new EwfTableItemSetup(
						clickScript:
							ClickScript.CreatePostBackScript( delegate { StandardLibrarySessionState.AddStatusMessage( StatusMessageType.Info, "Successful method execution." ); } ) ),
					"Test method".ToCell() ) );
			navButtonSetups.Add( new ActionButtonSetup( "Test", new ToolTipButton( menu ) ) );

			navButtonSetups.Add( new ActionButtonSetup( "Calendar",
			                                            new EwfLink( CalendarDemo.GetInfo( new EntitySetup.OptionalParameterPackage(),
			                                                                               new CalendarDemo.OptionalParameterPackage
			                                                                               	{ ReturnUrl = EwfPage.Instance.InfoAsBaseType.GetUrl(), Date = DateTime.Now } ) ) ) );
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