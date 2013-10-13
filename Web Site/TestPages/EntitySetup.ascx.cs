using System;
using System.Collections.Generic;
using System.Web.UI;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Ui;
using RedStapler.StandardLibrary.WebSessionState;

// OptionalParameter: string someText

namespace EnterpriseWebLibrary.WebSite.TestPages {
	public partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override PageInfo createParentPageInfo() {
				return null;
			}

			protected override List<PageGroup> createPageInfos() {
				return new List<PageGroup>
					{
						new PageGroup( "Working Stuff",
						               new ActionControls.Info( this ),
						               new CalendarDemo.Info( this ),
						               new OptionalParameters.Info( this ),
						               new Html5FileUpload.Info( this ),
						               new OmniDemo.Info( this ) ),
						new PageGroup( "First category",
						               new HtmlEditing.Info( this ),
						               new RegexHelper.Info( this ),
						               new TestPad.Info( this ),
						               new TwoWeekCalendarTest.Info( this, DateTime.Now ) ),
						new PageGroup( "Tables", new EwfTableDemo.Info( this ), new ColumnPrimaryTableDemo.Info( this ), new DynamicTableDemo.Info( this ) ),
						new PageGroup( "Layout", new BoxDemo.Info( this ) ),
						new PageGroup( "Form Controls",
						               new CheckBox.Info( this ),
						               new CheckBoxList.Info( this ),
						               new SelectListDemo.Info( this ),
						               new DateAndTimePickers.Info( this ) )
					};
			}

			public override string EntitySetupName { get { return "Customer #1"; } }
		}

		private ModalWindow one;
		private ModalWindow two;
		private ModalWindow three;
		private ModalWindow four;

		void EntitySetupBase.LoadData() {
			one = new ModalWindow( new EwfImage( "http://www.google.com/intl/en_ALL/images/srpr/logo1w.png" ) );
			two = new ModalWindow( new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ) );
			three =
				new ModalWindow( ControlStack.CreateWithControls( true,
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ),
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ),
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ),
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ),
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ),
				                                                  new EwfImage( "http://l.yimg.com/a/i/ww/met/yahoo_logo_us_061509.png" ) ) );
			var gibberish =
				@"Kolo has refactored the abstraction of blog-based systems.  What do we embrace? Anything and everything, regardless of obscurity! We have proven we know that if you brand proactively then you may also synthesize strategically. What does the commonly-accepted term 'obfuscation' really mean? We realize that it is better to envisioneer virally than to iterate virtually. Without adequate angel investors, e-tailers are forced to become wireless. Our 60/60/24/7/365 feature set is second to none, but our revolutionary convergence and newbie-proof configuration is invariably considered an amazing achievement. We will scale up our ability to recontextualize without decreasing our ability to visualize. Your budget for meshing should be at least one-tenth of your budget for unleashing. We will augment our ability to visualize without reducing our capability to upgrade. Quick: do you have a magnetic, value-added plan of action for managing new communities? We often orchestrate C2B2B revolutionary C2C. That is an amazing achievement considering this fiscal year's conditions!";
			four = new ModalWindow( ( gibberish + gibberish + gibberish ).GetLiteralControl() );
			var ln = new LaunchWindowLink( one ) { ActionControlStyle = new TextActionControlStyle( "yup" ) };
			var ln1 = new LaunchWindowLink( two ) { ActionControlStyle = new TextActionControlStyle( "yup" ) };
			var ln2 = new LaunchWindowLink( three ) { ActionControlStyle = new TextActionControlStyle( "yup" ) };
			var ln3 = new LaunchWindowLink( four ) { ActionControlStyle = new TextActionControlStyle( "yup" ) };
			ph.AddControlsReturnThis( ln, ln1, ln2, ln3 );
			var t = new EwfTextBox( "" );
			//parameterFormControls.SomeTextControl = t;
			ph.AddControlsReturnThis( t );
		}

		public List<ActionButtonSetup> CreateNavButtonSetups() {
			var navButtonSetups = new List<ActionButtonSetup>();
			navButtonSetups.Add( new ActionButtonSetup( "Calendar", new EwfLink( new CalendarDemo.Info( info ) ) ) );
			navButtonSetups.Add( new ActionButtonSetup( "Go to Microsoft", new EwfLink( new ExternalPageInfo( "http://www.microsoft.com" ) ) ) );
			navButtonSetups.Add( new ActionButtonSetup( "Custom script", new CustomButton( () => "alert('test')" ) ) );
			navButtonSetups.Add( new ActionButtonSetup( "Menu",
			                                            new ToolTipButton(
				                                            EwfTable.CreateWithItems( items:
					                                                                      new Func<EwfTableItem>[]
						                                                                      {
							                                                                      () =>
							                                                                      new EwfTableItem(
								                                                                      new EwfTableItemSetup(
									                                                                      clickScript:
										                                                                      ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://www.apple.com" ) ) ),
								                                                                      "Apple".ToCell() ),
							                                                                      () =>
							                                                                      new EwfTableItem(
								                                                                      new EwfTableItemSetup(
									                                                                      clickScript:
										                                                                      ClickScript.CreateRedirectScript(
											                                                                      new ExternalPageInfo( "http://www.microsoft.com" ) ) ),
								                                                                      "Microsoft".ToCell() ),
							                                                                      () =>
							                                                                      new EwfTableItem(
								                                                                      new EwfTableItemSetup(
									                                                                      clickScript:
										                                                                      ClickScript.CreateRedirectScript( new ExternalPageInfo( "http://www.google.com" ) ) ),
								                                                                      "Google".ToCell() ),
							                                                                      () =>
							                                                                      new EwfTableItem(
								                                                                      new EwfTableItemSetup(
									                                                                      clickScript: ClickScript.CreateCustomScript( "alert('test!')" ) ),
								                                                                      "Custom script".ToCell() ),
							                                                                      () =>
							                                                                      new EwfTableItem(
								                                                                      new LaunchWindowLink( new ModalWindow( new Paragraph( "Test!" ) ) )
									                                                                      {
										                                                                      ActionControlStyle = new TextActionControlStyle( "Modal" )
									                                                                      }.ToCell() )
						                                                                      } ) ) ) );

			navButtonSetups.Add( new ActionButtonSetup( "Modal Window",
			                                            new LaunchWindowLink(
				                                            new ModalWindow(
					                                            new EwfImage( "http://i3.microsoft.com/en/shared/templates/components/cspMscomHeader/m_head_blend.png" ) ) ) ) );
			return navButtonSetups;
		}

		public List<LookupBoxSetup> CreateLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "Lookup!", text => { throw new EwfException( "Lookup '" + text + "' failed." ); } ) );
			return lookupBoxSetups;
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			var actionButtonSetups = new List<ActionButtonSetup>();
			actionButtonSetups.Add( new ActionButtonSetup( "Delegate action",
			                                               new PostBackButton( new DataModification(),
			                                                                   () =>
			                                                                   EwfPage.Instance.EhExecute(
				                                                                   delegate { EwfPage.AddStatusMessage( StatusMessageType.Info, "Did Something." ); } ) )
				                                               {
					                                               UsesSubmitBehavior = false
				                                               } ) );
			actionButtonSetups.Add( new ActionButtonSetup( "Go to Google", new EwfLink( new ExternalPageInfo( "http://www.google.com" ) ) ) );
			actionButtonSetups.Add( new ActionButtonSetup( "Generate error",
			                                               new PostBackButton( new DataModification(), () => { throw new ApplicationException(); } )
				                                               {
					                                               UsesSubmitBehavior = false
				                                               } ) );
			return actionButtonSetups;
		}
	}
}