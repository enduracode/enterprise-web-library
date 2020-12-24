using System;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.EnterpriseWebFramework.UserManagement;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class RequestProfiling: EwfPage {
		partial class Info {
			protected override AlternativeResourceMode createAlternativeMode() =>
				UserManagementStatics.UserManagementEnabled
					? null
					: new DisabledResourceMode( "To prevent unauthorized profiling, this feature requires user management to be enabled." );
		}

		protected override PageContent getContent() {
			var content = new UiPageContent();

			var userIsProfiling = AppMemoryCache.UserIsProfilingRequests( AppRequestState.Instance.ProfilingUserId );
			content.Add(
				new Paragraph( "Profiling is currently {0}.".FormatWith( userIsProfiling ? "ON" : "OFF" ).ToComponents() ).Append(
						new Paragraph(
							new EwfButton(
								new StandardButtonStyle( userIsProfiling ? "Turn Profiling OFF" : "Turn Profiling ON" ),
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateFull(
										id: "toggle",
										modificationMethod: () => AppRequestState.AddNonTransactionalModificationMethod(
											() => AppMemoryCache.SetRequestProfilingForUser(
												AppRequestState.Instance.ProfilingUserId,
												userIsProfiling ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ) ) ).ToCollection() ) )
					.Materialize() );

			return content;
		}
	}
}