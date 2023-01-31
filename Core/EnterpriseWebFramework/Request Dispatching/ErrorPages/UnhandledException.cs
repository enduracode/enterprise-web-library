using EnterpriseWebLibrary.Configuration;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.ErrorPages {
	partial class UnhandledException {
		private static Func<( string prefix, Exception exception )> errorGetter;

		internal static void Init( Func<( string, Exception )> errorGetter ) {
			UnhandledException.errorGetter = errorGetter;
		}

		protected internal override bool IsIntermediateInstallationPublicResource => true;
		protected override UrlHandler getUrlParent() => new Admin.EntitySetup();
		protected internal override ConnectionSecurity ConnectionSecurity => ConnectionSecurity.MatchingCurrentRequest;

		protected override PageContent getContent() {
			var content = new List<FlowComponent>();

			if( ConfigurationStatics.IsDevelopmentInstallation ) {
				var error = errorGetter();
				if( error.prefix.Length > 0 )
					content.Add( new Paragraph( error.prefix.ToComponents() ) );
				content.Add( new Paragraph( error.exception.ToString().ToComponents() ) );
			}
			else
				content.Add( new Paragraph( Translation.AnErrorHasOccurred.ToComponents() ) );

			return new ErrorPageContent( content );
		}
	}
}