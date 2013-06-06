using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class BasicTests: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis( ControlStack.CreateWithControls( true,
			                                                           new PostBackButton(
				                                                           new DataModification( firstModificationMethod: cn1 => EwfApp.Instance.SendHealthCheck() ),
				                                                           new ButtonActionControlStyle( "Send Health Check" ),
				                                                           usesSubmitBehavior: false ),
			                                                           new PostBackButton( new DataModification( firstModificationMethod: throwException ),
			                                                                               new ButtonActionControlStyle( "Throw Unhandled Exception" ),
			                                                                               usesSubmitBehavior: false ) ) );
		}

		private void throwException( DBConnection cn ) {
			throw new ApplicationException( "This is a test from the {0} page.".FormatWith( info.PageFullName ) );
		}
	}
}