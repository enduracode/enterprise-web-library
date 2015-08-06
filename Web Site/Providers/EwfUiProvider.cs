using System;
using System.Collections.Generic;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;
using EnterpriseWebLibrary.WebSite.TestPages;

namespace EnterpriseWebLibrary.WebSite.Providers {
	internal class EwfUiProvider: AppEwfUiProvider {
		public override List<ActionButtonSetup> GetGlobalNavActionControls() {
			var navButtonSetups = new List<ActionButtonSetup>();
			if( CreateSystem.GetInfo().IsIdenticalToCurrent() )
				return navButtonSetups;

			// This will hide itself because Contact Us requires a logged-in user, and the standard library test web site has no users.
			var contactPage = EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ContactSupport.GetInfo( EwfPage.Instance.InfoAsBaseType.GetUrl() );
			navButtonSetups.Add( new ActionButtonSetup( contactPage.ResourceName, new EwfLink( contactPage ) ) );

			var menu = EwfTable.Create();
			menu.AddItem(
				() =>
				new EwfTableItem(
					new EwfTableItemSetup(
					clickScript:
					ClickScript.CreatePostBackScript(
						PostBack.CreateFull( id: "testMethod", firstModificationMethod: () => EwfPage.AddStatusMessage( StatusMessageType.Info, "Successful method execution." ) ) ) ),
					"Test method" ) );
			navButtonSetups.Add( new ActionButtonSetup( "Test", new ToolTipButton( menu ) ) );

			navButtonSetups.Add(
				new ActionButtonSetup(
					"Calendar",
					new EwfLink(
						CalendarDemo.GetInfo(
							new EntitySetup.OptionalParameterPackage(),
							new CalendarDemo.OptionalParameterPackage { ReturnUrl = EwfPage.Instance.InfoAsBaseType.GetUrl(), Date = DateTime.Now } ) ) ) );
			return navButtonSetups;
		}

		public override List<LookupBoxSetup> GetGlobalNavLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			if( CreateSystem.GetInfo().IsIdenticalToCurrent() )
				return lookupBoxSetups;

			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", "lookup1", delegate { return null; } ) );
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "test", "lookup2", delegate { return null; } ) );
			return lookupBoxSetups;
		}
	}
}