using EnterpriseWebLibrary.EnterpriseWebFramework;
using Tewl.Tools;

// EwlPage

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ActionControls {
		protected override PageContent getContent() =>
			new UiPageContent( omitContentBox: true )
				.Add(
					getBox(
						new EwfButton(
							new StandardButtonStyle( "Tiny Post Back Button", buttonSize: ButtonSize.ShrinkWrap ),
							behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "tiny" ) ) ) ) )
				.Add( getBox( new EwfHyperlink( SubFolder.General.GetInfo(), new ButtonHyperlinkStyle( "Tiny EWF Link", buttonSize: ButtonSize.ShrinkWrap ) ) ) )
				.Add(
					getBox(
						new EwfButton( new StandardButtonStyle( "Post Back Button" ), behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "normal" ) ) ) ) )
				.Add( getBox( new EwfHyperlink( EwfTableDemo.GetInfo(), new ButtonHyperlinkStyle( "EWF Link" ) ) ) )
				.Add(
					getBox(
						new EwfButton(
							new StandardButtonStyle( "Large Post Back Button", buttonSize: ButtonSize.Large ),
							behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "large" ) ) ) ) )
				.Add( getBox( new EwfHyperlink( EwfTableDemo.GetInfo(), new ButtonHyperlinkStyle( "Large EWF Link", ButtonSize.Large ) ) ) );

		private FlowComponent getBox( FlowComponent content ) => new Section( content.ToCollection(), style: SectionStyle.Box );
	}
}

namespace EnterpriseWebLibrary.WebSite.TestPages {
partial class ActionControls {
protected override UrlHandler getUrlParent() => new LegacyUrlFolderSetup();
}
}