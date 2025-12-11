namespace EnterpriseWebLibrary.EnterpriseWebFramework.HealthMonitoring;

// EwlResource
partial class SystemHealth {
	protected internal override bool IsIntermediateInstallationPublicResource => true;
	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
	protected override bool disablesUrlNormalization => true;

	// This differs from AppHealth because it will eventually consider status data from other components of the system, such as Windows services, and only return
	// as healthy when all of that is funtioning normally.
	protected override EwfSafeRequestHandler? getOrHead() =>
		new EwfSafeResponseWriter( EwfResponse.Create( ContentTypes.PlainText, new EwfResponseBodyCreator( () => "Healthy" ) ) );
}