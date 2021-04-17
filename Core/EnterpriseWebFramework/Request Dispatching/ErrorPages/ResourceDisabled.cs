using Tewl.Tools;

// EwlPage
// Parameter: string message

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class ResourceDisabled {
		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() =>
			new ErrorPageContent(
				new Paragraph( Message.Length > 0 ? Message.ToComponents() : Translation.ThePageYouRequestedIsDisabled.ToComponents() ).ToCollection() );
	}
}