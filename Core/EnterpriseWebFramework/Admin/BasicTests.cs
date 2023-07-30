#nullable disable
using Humanizer;

// EwlPage

namespace EnterpriseWebLibrary.EnterpriseWebFramework.Admin {
	partial class BasicTests {
		protected override PageContent getContent() =>
			new UiPageContent().Add(
				new StackList(
					new EwfButton(
							new StandardButtonStyle( "Send Health Check" ),
							behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "sendHealthCheck", modificationMethod: TelemetryStatics.SendHealthCheck ) ) )
						.ToComponentListItem()
						.Append(
							new EwfButton(
									new StandardButtonStyle( "Throw Unhandled Exception" ),
									behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "throwException", modificationMethod: throwException ) ) )
								.ToComponentListItem() ) ) );

		private void throwException() {
			throw new ApplicationException( "This is a test from the {0} page.".FormatWith( ResourceFullName ) );
		}
	}
}