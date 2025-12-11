namespace EnterpriseWebLibrary.EnterpriseWebFramework.HealthMonitoring;

// EwlResource
partial class AppHealth {
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
	protected override bool disablesUrlNormalization => true;

	protected override EwfSafeRequestHandler? getOrHead() =>
		new EwfSafeResponseWriter( EwfResponse.Create( ContentTypes.PlainText, new EwfResponseBodyCreator( () => "Healthy" ) ) );
}