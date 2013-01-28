using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class ActionControls: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new PostBackButton( new DataModification(),
					                    () => { },
					                    new ButtonActionControlStyle( "Tiny Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ),
					                    false ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					EwfLink.Create( SubFolder.General.GetInfo(), new ButtonActionControlStyle( "Tiny EWF Link", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) )
					       .ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Tiny Toggle Button", buttonSize: ButtonActionControlStyle.ButtonSize.ShrinkWrap ) )
						.ToSingleElementArray() ) );

			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new PostBackButton( new DataModification(), () => { }, new ButtonActionControlStyle( "Post Back Button" ), usesSubmitBehavior: false )
						{
							Width = Unit.Pixel( 200 )
						}.ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "EWF Link" ) ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Toggle button" ) ).ToSingleElementArray() ) );

			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new PostBackButton( new DataModification(),
					                    () => { },
					                    new ButtonActionControlStyle( "Large Post Back Button", buttonSize: ButtonActionControlStyle.ButtonSize.Large ),
					                    usesSubmitBehavior: false ).ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					EwfLink.Create( EwfTableDemo.GetInfo(), new ButtonActionControlStyle( "Large EWF Link", ButtonActionControlStyle.ButtonSize.Large ) )
					       .ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new ToggleButton( new WebControl[ 0 ], new ButtonActionControlStyle( "Large Toggle Button", ButtonActionControlStyle.ButtonSize.Large ) )
						.ToSingleElementArray() ) );
		}
	}
}