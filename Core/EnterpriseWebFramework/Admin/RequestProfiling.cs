#nullable disable
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.UserManagement;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin;

partial class RequestProfiling {
	protected override AlternativeResourceMode createAlternativeMode() =>
		UserManagementStatics.UserManagementEnabled || !ConfigurationStatics.IsLiveInstallation
			? null
			: new DisabledResourceMode( "To prevent unauthorized profiling, this feature requires user management to be enabled." );

	protected override PageContent getContent() {
		var content = new UiPageContent();

		if( ConfigurationStatics.IsLiveInstallation ) {
			var userIsProfiling = AppMemoryCache.UserIsProfilingRequests( RequestState.Instance.ProfilingUserId );
			content.Add(
				new Paragraph( "Profiling is currently {0}.".FormatWith( userIsProfiling ? "ON" : "OFF" ).ToComponents() ).Append(
						new Paragraph(
							new EwfButton(
								new StandardButtonStyle( userIsProfiling ? "Turn Profiling OFF" : "Turn Profiling ON" ),
								behavior: new PostBackBehavior(
									postBack: PostBack.CreateFull(
										id: "userToggle",
										modificationMethod: () => AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod(
											() => AppMemoryCache.SetRequestProfilingForUser(
												RequestState.Instance.ProfilingUserId,
												userIsProfiling ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ) ) ).ToCollection() ) )
					.Materialize() );
		}
		else {
			var profilingDisabled = AppMemoryCache.UnconditionalRequestProfilingDisabled();
			content.Add(
				new Paragraph(
						"Profiling is currently ".ToComponents()
							.Append( new ImportantContent( profilingDisabled ? "disabled".ToComponents() : "enabled".ToComponents() ) )
							.Concat( ".".ToComponents() )
							.Materialize() ).Append(
						new Paragraph(
							new EwfButton(
									new StandardButtonStyle( profilingDisabled ? "Enable Profiling" : "Disable Profiling for 1 Hour" ),
									behavior: new PostBackBehavior(
										postBack: PostBack.CreateFull(
											id: "unconditionalToggle",
											modificationMethod: () => AutomaticDatabaseConnectionManager.AddNonTransactionalModificationMethod(
												() => AppMemoryCache.SetUnconditionalRequestProfilingDisabled( profilingDisabled ? TimeSpan.Zero : TimeSpan.FromHours( 1 ) ) ) ) ) )
								.ToCollection() ) )
					.Materialize() );
		}

		return content;
	}
}