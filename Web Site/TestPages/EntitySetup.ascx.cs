using System;
using System.Collections.Generic;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.DisplayElements.Entity;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;

// OptionalParameter: string someText

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class EntitySetup: UserControl, EntityDisplaySetup {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup>
					{
						new ResourceGroup(
							"Working Stuff",
							new ActionControls.Info( this ),
							new CalendarDemo.Info( this ),
							new OptionalParameters.Info( this ),
							new Html5FileUpload.Info( this ),
							new OmniDemo.Info( this ) ),
						new ResourceGroup(
							"First category",
							new HtmlEditing.Info( this ),
							new RegexHelper.Info( this ),
							new TwoWeekCalendarTest.Info( this, DateTime.Now ),
							new StatusMessages.Info( this ) ),
						new ResourceGroup( "Tables", new EwfTableDemo.Info( this ), new ColumnPrimaryTableDemo.Info( this ), new DynamicTableDemo.Info( this ) ),
						new ResourceGroup( "Layout", new BoxDemo.Info( this ) ),
						new ResourceGroup(
							"Form Controls",
							new EwfTextBoxDemo.Info( this ),
							new CheckBox.Info( this ),
							new CheckBoxList.Info( this ),
							new SelectListDemo.Info( this ),
							new DateAndTimePickers.Info( this ) ),
						new ResourceGroup( "Other", new IntermediatePostBacks.Info( this ), new Charts.Info( this ) )
					};
			}

			public override string EntitySetupName { get { return "Customer #1"; } }
		}

		private ModalWindow one;
		private ModalWindow two;
		private ModalWindow three;
		private ModalWindow four;

		void EntitySetupBase.LoadData() {
			ph.AddControlsReturnThis(
				new Paragraph( "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis semper dui. Aenean egestas dolor ac elementum lacinia. Vestibulum eget." ) );
		}

		public List<ActionButtonSetup> CreateNavButtonSetups() {
			var navButtonSetups = new List<ActionButtonSetup>();
			navButtonSetups.Add( new ActionButtonSetup( "Calendar", new EwfLink( new CalendarDemo.Info( info ) ) ) );
			navButtonSetups.Add( new ActionButtonSetup( "Go to Microsoft", new EwfLink( new ExternalResourceInfo( "http://www.microsoft.com" ) ) ) );
			navButtonSetups.Add( new ActionButtonSetup( "Custom script", new CustomButton( () => "alert('test')" ) ) );
			navButtonSetups.Add(
				new ActionButtonSetup(
					"Menu",
					new ToolTipButton(
						EwfTable.CreateWithItems(
							items:
								new Func<EwfTableItem>[]
									{
										() =>
										new EwfTableItem(
											new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalResourceInfo( "http://www.apple.com" ) ) ),
											"Apple" ),
										() =>
										new EwfTableItem(
											new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalResourceInfo( "http://www.microsoft.com" ) ) ),
											"Microsoft" ),
										() =>
										new EwfTableItem(
											new EwfTableItemSetup( clickScript: ClickScript.CreateRedirectScript( new ExternalResourceInfo( "http://www.google.com" ) ) ),
											"Google" ),
										() => new EwfTableItem( new EwfTableItemSetup( clickScript: ClickScript.CreateCustomScript( "alert('test!')" ) ), "Custom script" ),
										() =>
										new EwfTableItem( new LaunchWindowLink( new ModalWindow( new Paragraph( "Test!" ) ) ) { ActionControlStyle = new TextActionControlStyle( "Modal" ) } )
									} ) ) ) );

			navButtonSetups.Add(
				new ActionButtonSetup(
					"Modal Window",
					new LaunchWindowLink( new ModalWindow( new EwfImage( "http://i3.microsoft.com/en/shared/templates/components/cspMscomHeader/m_head_blend.png" ) ) ) ) );
			return navButtonSetups;
		}

		public List<LookupBoxSetup> CreateLookupBoxSetups() {
			var lookupBoxSetups = new List<LookupBoxSetup>();
			lookupBoxSetups.Add( new LookupBoxSetup( 100, "Lookup!", "lookup", text => { throw new DataModificationException( "Lookup '" + text + "' failed." ); } ) );
			return lookupBoxSetups;
		}

		public List<ActionButtonSetup> CreateActionButtonSetups() {
			var actionButtonSetups = new List<ActionButtonSetup>();
			actionButtonSetups.Add(
				new ActionButtonSetup(
					"Delegate action",
					new PostBackButton(
						PostBack.CreateFull( id: "delegate", firstModificationMethod: () => EwfPage.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) ) );
			actionButtonSetups.Add( new ActionButtonSetup( "Go to Google", new EwfLink( new ExternalResourceInfo( "http://www.google.com" ) ) ) );
			actionButtonSetups.Add(
				new ActionButtonSetup(
					"Generate error",
					new PostBackButton( PostBack.CreateFull( id: "error", firstModificationMethod: () => { throw new ApplicationException(); } ) ) ) );
			return actionButtonSetups;
		}
	}
}