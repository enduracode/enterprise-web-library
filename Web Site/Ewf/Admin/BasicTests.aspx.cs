using System;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class BasicTests: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				ControlStack.CreateWithControls(
					true,
					new PostBackButton(
						new ButtonActionControlStyle( "Send Health Check" ),
						usesSubmitBehavior: false,
						postBack: PostBack.CreateFull( id: "sendHealthCheck", firstModificationMethod: () => EwfApp.Instance.SendHealthCheck() ) ),
					new PostBackButton(
						new ButtonActionControlStyle( "Throw Unhandled Exception" ),
						usesSubmitBehavior: false,
						postBack: PostBack.CreateFull( id: "throwException", firstModificationMethod: throwException ) ) ) );
		}

		private void throwException() {
			throw new ApplicationException( "This is a test from the {0} page.".FormatWith( info.ResourceFullName ) );
		}
	}
}