using System.Collections.Generic;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.ErrorPages {
	public class ErrorPageContent: PageContent {
		private readonly BasicPageContent basicContent;

		public ErrorPageContent( IReadOnlyCollection<FlowComponent> content ) {
			basicContent = new BasicPageContent( bodyClasses: CssElementCreator.ErrorPageBodyClass ).Add( content );
		}

		protected override PageContent GetContent() => basicContent;
	}
}