using System.Web.UI;
using System.Web.UI.WebControls;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class ActionControls: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				getBox(
					new PostBackButton(
						PostBack.CreateFull( id: "tiny" ),
						new ButtonActionControlStyle( "Tiny Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
						false ) ) );
			ph.AddControlsReturnThis(
				getBox(
					EwfLink.Create( SubFolder.General.GetInfo(), new ButtonActionControlStyle( "Tiny EWF Link", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) ) ) );
			ph.AddControlsReturnThis(
				getBox(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Tiny Toggle Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) ) ) );

			ph.AddControlsReturnThis(
				getBox(
					new PostBackButton( PostBack.CreateFull( id: "normal" ), new ButtonActionControlStyle( "Post Back Button" ), usesSubmitBehavior: false )
						{
							Width = Unit.Pixel( 200 )
						} ) );
			ph.AddControlsReturnThis( getBox( EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "EWF Link" ) ) ) );
			ph.AddControlsReturnThis( getBox( new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Toggle button" ) ) ) );

			ph.AddControlsReturnThis(
				getBox(
					new PostBackButton(
						PostBack.CreateFull( id: "large" ),
						new ButtonActionControlStyle( "Large Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.Large ),
						usesSubmitBehavior: false ) ) );
			ph.AddControlsReturnThis(
				getBox( EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "Large EWF Link", ButtonActionControlStyle.ButtonSize.Large ) ) ) );
			ph.AddControlsReturnThis(
				getBox( new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Large Toggle Button", ButtonActionControlStyle.ButtonSize.Large ) ) ) );
		}

		private Control getBox( Control contentControl ) {
			return new Section( contentControl.ToSingleElementArray(), style: SectionStyle.Box );
		}
	}
}