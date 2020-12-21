using System;
using Humanizer;

namespace EnterpriseWebLibrary.EnterpriseWebFramework.EnterpriseWebLibrary.WebSite.Admin {
	partial class BasicTests: EwfPage {
		protected override PageContent getContent() =>
			new UiPageContent().Add(
				new StackList(
					new EwfButton(
							new StandardButtonStyle( "Send Health Check" ),
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull( id: "sendHealthCheck", firstModificationMethod: () => EwfApp.Instance.SendHealthCheck() ) ) )
						.ToComponentListItem()
						.Append(
							new EwfButton(
									new StandardButtonStyle( "Throw Unhandled Exception" ),
									behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "throwException", firstModificationMethod: throwException ) ) )
								.ToComponentListItem() ) ) );

		private void throwException() {
			throw new ApplicationException( "This is a test from the {0} page.".FormatWith( info.ResourceFullName ) );
		}
	}
}