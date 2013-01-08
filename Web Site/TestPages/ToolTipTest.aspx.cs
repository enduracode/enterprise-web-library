using System;
using System.Web.UI.WebControls;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

// Parameter: DateTime date

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class ToolTipTest: EwfPage {
		partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			var toolTipLink =
				new ToolTipButton(
					EwfTable.CreateWithItems( items:
						                          new Func<EwfTableItem>[]
							                          {
								                          () =>
								                          new EwfTableItem(
									                          new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://www.apple.com" ) ) ),
									                          "Apple".ToCell() ),
								                          () =>
								                          new EwfTableItem(
									                          new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://www.microsoft.com" ) ) ),
									                          "Microsoft".ToCell() ),
								                          () =>
								                          new EwfTableItem(
									                          new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://www.google.com" ) ) ),
									                          "Google".ToCell() ),
								                          () =>
								                          new EwfTableItem( new EwfTableItemSetup( clickScript: ClickScript.CreateCustomScript( "alert('test!')" ) ),
								                                            "Custom script".ToCell() )
							                          } ) );
			toolTipLink.ActionControlStyle = new ButtonActionControlStyle( "Not clickable for some reason", ButtonActionControlStyle.ButtonSize.ShrinkWrap );
			ph.AddControlsReturnThis( new RedStapler.StandardLibrary.EnterpriseWebFramework.Box( toolTipLink.ToSingleElementArray() ) );
			ph.AddControlsReturnThis(
				new RedStapler.StandardLibrary.EnterpriseWebFramework.Box(
					new ToolTipButton( new EwfLink( new ExternalPageInfo( "http://www.google.com" ) ) { ActionControlStyle = new TextActionControlStyle( "Google link" ) } )
						{
							ActionControlStyle = new TextActionControlStyle( "Not clickable 2" )
						}.ToSingleElementArray() ) );

			label.ToolTip = "Label tool tip";

			extra.Controls.Add( new ToolTipButton( EwfLink.Create( new ExternalPageInfo( "http://redstapler.biz" ), new TextActionControlStyle( "Testing Controls" ) ) )
				{
					ActionControlStyle = new TextActionControlStyle( "ToolTipLink added to panel" )
				} );
			//markupLink.ToolTipControl = new EwfImage( "http://www.google.com/images/firefox/sprite2.png" );
			//boxActionControlStyle.ActionControlStyle = new BoxActionControlStyle( "~/Ewf/BoxActionControlStyleImages/Left.png",
			//                                                                      6,
			//                                                                      "~/Ewf/BoxActionControlStyleImages/Right.png",
			//                                                                      22,
			//                                                                      "~/Ewf/BoxActionControlStyleImages/Background.png",
			//                                                                      27 );
			//boxActionControlStyle.ToolTipControl = "yay".GetLiteralControl();
			//boxActionControlStyle.ToolTipTitle = "test";
			//imageActionControlStyle.ActionControlStyle = new ImageActionControlStyle( "http://integration.redstapler.biz/Cua/Masters/Images/button_go.png" );
			//imageActionControlStyle.ToolTipTitle = "test";
			//imageActionControlStyle.ToolTipControl = "yay".GetLiteralControl();

			//calendarTest.NavigationBoxHeaderClass = "navigationBox";
			//calendarTest.DaysOfWeekHeaderClass = "daysOfWeek";
			//calendarTest.DayClass = "days";
			//calendarTest.DaySpacerClass = "spacers";
			//calendarTest.TodayClass = "today";


			calendarTest.SetParameters( info.Date, date => parametersModification.Date = date );
			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 7 ), "Normal ToolTip".GetLiteralControl() );
			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 9 ),
			                               new ToolTipButton( "Testing clickable link inside of a calendar".GetLiteralControl() )
				                               {
					                               ActionControlStyle = new TextActionControlStyle( "Clickable ToolTipLink" )
				                               } );
			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 12 ), "Fancy ToolTip!".GetLiteralControl() );
			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 12 ), "Second Control!".GetLiteralControl() );
			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 18 ), "Fancy ToolTip! (Accessible)".GetLiteralControl() );


			calendarTest.SetToolTipForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 7 ), "Normal Text ToolTip".GetLiteralControl() );
			calendarTest.SetToolTipForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 12 ),
			                               new Literal
				                               {
					                               Text = "<div><img src=\"http://www.google.com/intl/en_ALL/images/logo.gif\"><a href=\"http://google.com\">Google!</a></div> "
				                               } );
			calendarTest.SetToolTipForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 18 ),
			                               new Literal
				                               {
					                               Text = "<div><img src=\"http://www.google.com/intl/en_ALL/images/logo.gif\"><a href=\"http://google.com\">Google!</a></div> "
				                               } );
			var test2 = new EwfTextBox( "Testing Controls" );
			//var test = new ToolTip( true )
			//            { Title = "something", Content = new EwfLink { Text = "Testing Controls", NavigateUrl = "http://redstapler.biz" }, TargetControl = test2 };
			//test2.Controls.Add( test );

			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 23 ), test2 );

			calendarTest.AddContentForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 27 ), "Fancy ToolTip! (Accessible)".GetLiteralControl() );
			calendarTest.SetToolTipForDay( new DateTime( DateTime.Today.Year, DateTime.Today.Month, 27 ),
			                               EwfLink.Create( new ExternalPageInfo( "http://redstapler.biz" ), new TextActionControlStyle( "Testing Controls" ) ) );

			//durationPicker.ToolTip = "duration picker tooltip";
			ewfLabel.ToolTip = "label tooltip";
			ewfImage.ToolTip = "ewf image tooltip";
			controlStack.Controls.Add( new Paragraph( "Ewf Paragraph Control".GetLiteralControl() ) { ToolTip = "EwfParagraph tool tip" } );
			ewfDatePicker.ToolTip = "ewf datepicker tooltip";
			ewfDateTimePicker.ToolTip = "ewf datetimepicker tooltip";
			mailtolink.ToolTip = "ewf mail to tooltip";
			timepicker.ToolTip = "ewf time picker tooltip";
			//durationPicker2.ToolTip =
			//  "To be, or not to be: that is the question: whether 'tis nobler in the mind to suffer the slings and arrows of outrageous fortune, " +
			//  "or to take arms against a sea of troubles, and by opposing end them? To die: to sleep; no more; and, by a sleep to say we end the heart-ache and the thousand " +
			//  "natural shocks that flesh is heir to, 'tis a consummation devoutly to be wish'd. To die, to sleep; to sleep: perchance to dream: ay, there's the rub; for in that" +
			//  "sleep of death what dreams may come when we have shuffled off this mortal coil, must give us pause. There's the respect that makes calamity of so long a life; " +
			//  "for who would bear the whips and scorns of time, the oppressor's wrong, the proud man's contumely, the pangs of dispriz'd love, the law's delay, the insolence of ";

			//nestedToolTipLinks.ToolTipControl = ControlStack.CreateWithControls( true,
			//                                                                     new ToolTipLink( "tool tip".GetLabelControl() )
			//                                                                      { ActionControlStyle = new TextActionControlStyle( "One" ) },
			//                                                                     new ToolTipLink( new ControlLine( new EwfTextBox(),
			//                                                                                                       new PostBackButton( new DataModification(),
			//                                                                                                                           delegate { },
			//                                                                                                                           new ButtonActionControlStyle(
			//                                                                                                                            "Button" ),
			//                                                                                                                           false ) ) )
			//                                                                      { ActionControlStyle = new TextActionControlStyle( "Two" ) },
			//                                                                     new ToolTipLink(
			//                                                                      new ToolTipLink(
			//                                                                        new EwfImage( "http://www.google.com/intl/en_ALL/images/srpr/logo1w.png" ) )
			//                                                                        { ActionControlStyle = new TextActionControlStyle( "Four" ) } )
			//                                                                      { ActionControlStyle = new TextActionControlStyle( "Three" ) } );
			//var bm = new Bookmark( "!@#$%^&*()_1234567890-=" );
			//var link = EwfLink.Create( GetInfo( info.Date, uriFragmentIdentifier: "!@#$%^&*()_1234567890-=" ), new TextActionControlStyle( "test url encoding" ) );
			//ph.AddControlsReturnThis( link );
			//Controls.Add( bm );
		}
	}
}