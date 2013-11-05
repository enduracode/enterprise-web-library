using System;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class BasicTests: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis( ControlStack.CreateWithControls( true,
			                                                           new PostBackButton(
				                                                           PostBack.CreateFull( id: "sendHealthCheck",
				                                                                                firstModificationMethod: () => EwfApp.Instance.SendHealthCheck() ),
				                                                           new ButtonActionControlStyle( "Send Health Check" ),
				                                                           usesSubmitBehavior: false ),
			                                                           new PostBackButton(
				                                                           PostBack.CreateFull( id: "throwException", firstModificationMethod: throwException ),
				                                                           new ButtonActionControlStyle( "Throw Unhandled Exception" ),
				                                                           usesSubmitBehavior: false ) ) );
		}

		private void throwException() {
			throw new ApplicationException( "This is a test from the {0} page.".FormatWith( info.PageFullName ) );
		}
	}
}