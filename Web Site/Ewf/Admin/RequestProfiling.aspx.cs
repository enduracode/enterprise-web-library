using System;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class RequestProfiling: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() {
				return UserManagementStatics.UserManagementEnabled
					       ? null
					       : new DisabledResourceMode( "To prevent unauthorized profiling, this feature requires user management to be enabled." );
			}
		}

		protected override void loadData() {
			var userIsProfiling = AppMemoryCache.UserIsProfilingRequests( AppRequestState.Instance.ProfilingUserId );
			ph.AddControlsReturnThis(
				new Paragraph( "Profiling is currently {0}.".FormatWith( userIsProfiling ? "ON" : "OFF" ) ),
				new Paragraph(
					new PostBackButton(
						new ButtonActionControlStyle( userIsProfiling ? "Turn Profiling OFF" : "Turn Profiling ON" ),
						usesSubmitBehavior: false,
						postBack:
							PostBack.CreateFull(
								id: "toggle",
								firstModificationMethod:
									() =>
									AppRequestState.AddNonTransactionalModificationMethod(
										() => AppMemoryCache.SetRequestProfilingForUser( AppRequestState.Instance.ProfilingUserId, userIsProfiling ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ) ) ) );
		}
	}
}