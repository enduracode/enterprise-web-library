using System;
using System.Collections.Generic;
using System.Web.UI;
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
						new PageGroup( "First category", new HtmlEditing.Info( this ), new RegexHelper.Info( this ), new TwoWeekCalendarTest.Info( this, DateTime.Now ),
						               new StatusMessages.Info( this ) ),
						new PageGroup( "Tables", new EwfTableDemo.Info( this ), new ColumnPrimaryTableDemo.Info( this ), new DynamicTableDemo.Info( this ) ),
						new PageGroup( "Layout", new BoxDemo.Info( this ) ),
						new PageGroup( "Form Controls",
						               new EwfTextBoxDemo.Info( this ),
						               new CheckBox.Info( this ),
						               new CheckBoxList.Info( this ),
						               new SelectListDemo.Info( this ),
						               new DateAndTimePickers.Info( this ) ),
						new PageGroup( "Other", new IntermediatePostBacks.Info( this ) )
					};
			}

			public override string EntitySetupName { get { return "Customer #1"; } }
		}

		private ModalWindow one;
		private ModalWindow two;
		private ModalWindow three;
		private ModalWindow four;

		void EntitySetupBase.LoadData() {}

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
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "Lookup!", "lookup", text => { throw new DataModificationException( "Lookup '" + text + "' failed." ); } ) );
			return lookupBoxSetups;
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			var actionButtonSetups = new List<ActionButtonSetup>();
			actionButtonSetups.Add( new ActionButtonSetup( "Delegate action",
			                                               new PostBackButton( PostBack.CreateFull( id: "delegate",
			                                                                                        firstModificationMethod:
				                                                                                        () =>
				                                                                                        EwfPage.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) ) );
			actionButtonSetups.Add( new ActionButtonSetup( "Go to Google", new EwfLink( new ExternalPageInfo( "http://www.google.com" ) ) ) );
			actionButtonSetups.Add( new ActionButtonSetup( "Generate error",
			                                               new PostBackButton( PostBack.CreateFull( id: "error",
			                                                                                        firstModificationMethod: () => { throw new ApplicationException(); } ) ) ) );
			return actionButtonSetups;
		}
	}
}