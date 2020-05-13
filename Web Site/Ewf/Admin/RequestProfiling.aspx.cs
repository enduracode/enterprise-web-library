using System;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class RequestProfiling: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() =>
				UserManagementStatics.UserManagementEnabled
					? null
					: new DisabledResourceMode( "To prevent unauthorized profiling, this feature requires user management to be enabled." );
		}

		protected override void loadData() {
			var userIsProfiling = AppMemoryCache.UserIsProfilingRequests( AppRequestState.Instance.ProfilingUserId );
			ph.AddControlsReturnThis(
				new Paragraph( "Profiling is currently {0}.".FormatWith( userIsProfiling ? "ON" : "OFF" ).ToComponents() ).Append(
						new Paragraph(
							new EwfButton(
								new StandardButtonStyle( userIsProfiling ? "Turn Profiling OFF" : "Turn Profiling ON" ),
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateFull(
										id: "toggle",
										firstModificationMethod: () => AppRequestState.AddNonTransactionalModificationMethod(
											() => AppMemoryCache.SetRequestProfilingForUser(
												AppRequestState.Instance.ProfilingUserId,
												userIsProfiling ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ) ) ).ToCollection() ) )
					.GetControls() );
		}
	}
}