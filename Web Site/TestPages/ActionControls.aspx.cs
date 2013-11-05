using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class ActionControls: EwfPage {
		protected override void loadData() {
			ph.AddControlsReturnThis(
				new Box(
					new PostBackButton( PostBack.CreateFull( id: "tiny" ),
					                    () => { },
					                    new ButtonActionControlStyle( "Tiny Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
					                    false ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new Box(
					EwfLink.Create( SubFolder.General.GetInfo(), new ButtonActionControlStyle( "Tiny EWF Link", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) )
					       .ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new Box(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Tiny Toggle Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) )
						.ToSingleElementArray() ) );

			ph.AddControlsReturnThis(
				new Box(
					new PostBackButton( PostBack.CreateFull( id: "normal" ), () => { }, new ButtonActionControlStyle( "Post Back Button" ), usesSubmitBehavior: false )
						{
							Width = Unit.Pixel( 200 )
						}.ToSingleElementArray() ) );
			ph.AddControlsReturnThis( new Box( EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "EWF Link" ) ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis( new Box( new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Toggle button" ) ).ToSingleElementArray() ) );

			ph.AddControlsReturnThis(
				new Box(
					new PostBackButton( PostBack.CreateFull( id: "large" ),
					                    () => { },
					                    new ButtonActionControlStyle( "Large Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.Large ),
					                    usesSubmitBehavior: false ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new Box(
					EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "Large EWF Link", ButtonActionControlStyle.ButtonSize.Large ) )
					       .ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new Box(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Large Toggle Button", ButtonActionControlStyle.ButtonSize.Large ) )
						.ToSingleElementArray() ) );
		}
	}
}