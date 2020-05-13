using System.Collections.Generic;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ActionControls: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				getBox(
					new EwfButton(
						new StandardButtonStyle( "Tiny Post Back Button", buttonSize: ButtonSize.ShrinkWrap ),
						behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "tiny" ) ) ) ) );
			ph.AddControlsReturnThis(
				getBox( new EwfHyperlink( SubFolder.General.GetInfo(), new ButtonHyperlinkStyle( "Tiny EWF Link", buttonSize: ButtonSize.ShrinkWrap ) ) ) );

			ph.AddControlsReturnThis(
				getBox(
					new EwfButton( new StandardButtonStyle( "Post Back Button" ), behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "normal" ) ) ) ) );
			ph.AddControlsReturnThis( getBox( new EwfHyperlink( EwfTableDemo.GetInfo(), new ButtonHyperlinkStyle( "EWF Link" ) ) ) );

			ph.AddControlsReturnThis(
				getBox(
					new EwfButton(
						new StandardButtonStyle( "Large Post Back Button", buttonSize: ButtonSize.Large ),
						behavior: new PostBackBehavior( postBack: PostBack.CreateFull( id: "large" ) ) ) ) );
			ph.AddControlsReturnThis( getBox( new EwfHyperlink( EwfTableDemo.GetInfo(), new ButtonHyperlinkStyle( "Large EWF Link", ButtonSize.Large ) ) ) );
		}

		private IEnumerable<Control> getBox( FlowComponent content ) => new Section( content.ToCollection(), style: SectionStyle.Box ).ToCollection().GetControls();
	}
}