using System;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework.AlternativePageModes;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.UserManagement;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	public partial class RequestProfiling: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}

			protected override AlternativePageMode createAlternativeMode() {
				return UserManagementStatics.UserManagementEnabled
					       ? null
					       : new DisabledPageMode( "To prevent unauthorized profiling, this feature requires user management to be enabled." );
			}
		}

		protected override void LoadData( DBConnection cn ) {
			var userIsProfiling = AppMemoryCache.UserIsProfilingRequests( AppTools.User.UserId );
			ph.AddControlsReturnThis( new Paragraph( "Profiling is currently {0}.".FormatWith( userIsProfiling ? "ON" : "OFF" ) ),
			                          new Paragraph(
				                          new PostBackButton(
					                          new DataModification(
						                          firstModificationMethod:
							                          () =>
							                          AppRequestState.AddNonTransactionalModificationMethod(
								                          () =>
								                          AppMemoryCache.SetRequestProfilingForUser( AppTools.User.UserId, userIsProfiling ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ),
					                          new ButtonActionControlStyle( userIsProfiling ? "Turn Profiling OFF" : "Turn Profiling ON" ),
					                          usesSubmitBehavior: false ) ) );
		}
	}
}