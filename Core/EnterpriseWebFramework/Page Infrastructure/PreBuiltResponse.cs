using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic.AlternativeResourceModes;
using EnterpriseWebLibrary.EnterpriseWebFramework.PageInfrastructure;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

// EwlResource
// Parameter: uint responseId
// Parameter: string secret
partial class PreBuiltResponse {
	private FullResponse? response;

	protected override void init() {
		response = SecondaryResponseDataStore.GetResponse( ResponseId, Secret );
	}

	protected override AlternativeResourceMode? createAlternativeMode() => response is null ? new DisabledResourceMode( "There is no response to send." ) : null;

	protected override UrlHandler getUrlParent() => new Admin.EntitySetup();

	public override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

	protected override EwfSafeRequestHandler getOrHead() => new EwfSafeResponseWriter( new EwfResponse( response! ) );
}