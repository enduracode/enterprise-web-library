using Tewl.Tools;

// Parameter: string message

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	partial class PageDisabled: EwfPage {
		partial class Info {
			protected override bool IsIntermediateInstallationPublicResource => true;
			protected override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;
		}

		protected override PageContent getContent() =>
			new ErrorPageContent(
				new Paragraph( info.Message.Length > 0 ? info.Message.ToComponents() : Translation.ThePageYouRequestedIsDisabled.ToComponents() ).ToCollection() );
	}
}