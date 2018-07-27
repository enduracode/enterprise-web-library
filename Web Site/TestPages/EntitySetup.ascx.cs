using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.EnterpriseWebFramework.Ui;
using EnterpriseWebLibrary.WebSessionState;

// OptionalParameter: string someText

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class EntitySetup: UserControl, UiEntitySetupBase {
		partial class Info {
			protected override ResourceInfo createParentResourceInfo() {
				return null;
			}

			protected override List<ResourceGroup> createResourceInfos() {
				return new List<ResourceGroup>
					{
						new ResourceGroup( "Working Stuff", new ActionControls.Info( this ), new OptionalParameters.Info( this ), new OmniDemo.Info( this ) ),
						new ResourceGroup( "First category", new HtmlEditing.Info( this ), new RegexHelper.Info( this ), new StatusMessages.Info( this ) ),
						new ResourceGroup( "Tables", new EwfTableDemo.Info( this ), new ColumnPrimaryTableDemo.Info( this ), new DynamicTableDemo.Info( this ) ),
						new ResourceGroup( "Layout", new BoxDemo.Info( this ) ),
						new ResourceGroup(
							"Form Controls",
							new TextControlDemo.Info( this ),
							new CheckBox.Info( this ),
							new CheckBoxList.Info( this ),
							new SelectListDemo.Info( this ),
							new DateAndTimePickers.Info( this ) ),
						new ResourceGroup(
							"Other",
							new IntermediatePostBacks.Info( this ),
							new ModalBoxes.Info( this ),
							new MailMerging.Info( this ),
							new Charts.Info( this ) )
					};
			}

			public override string EntitySetupName => "Customer #1";
		}

		private readonly ModalBoxId one = new ModalBoxId();
		private readonly ModalBoxId two = new ModalBoxId();

		void EntitySetupBase.LoadData() {
			ph.AddControlsReturnThis(
				new LegacyParagraph(
					"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis semper dui. Aenean egestas dolor ac elementum lacinia. Vestibulum eget." ) );

			new ModalBox( one, true, new Paragraph( "Test!".ToComponents() ).ToCollection() ).ToCollection().AddEtherealControls( this );
			new ModalBox(
					two,
					true,
					new EwfImage(
						new ImageSetup( null ),
						new ExternalResourceInfo( "http://i3.microsoft.com/en/shared/templates/components/cspMscomHeader/m_head_blend.png" ) ).ToCollection() )
				.ToCollection()
				.AddEtherealControls( this );
		}

		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetNavActions() =>
			new HyperlinkSetup( new ExternalResourceInfo( "http://www.microsoft.com" ), "Go to Microsoft" ).ToCollection<ActionComponentSetup>()
				.Append( new ButtonSetup( "Custom script", behavior: new CustomButtonBehavior( () => "alert('test');" ) ) )
				.Append(
					new ButtonSetup(
						"Menu",
						behavior: new MenuButtonBehavior(
							new StackList(
									new EwfHyperlink( new ExternalResourceInfo( "http://www.apple.com" ), new StandardHyperlinkStyle( "Apple" ) ).ToCollection()
										.ToComponentListItem()
										.ToCollection()
										.Append(
											new EwfHyperlink( new ExternalResourceInfo( "http://www.microsoft.com" ), new StandardHyperlinkStyle( "Microsoft" ) ).ToCollection()
												.ToComponentListItem() )
										.Append(
											new EwfHyperlink( new ExternalResourceInfo( "http://www.google.com" ), new StandardHyperlinkStyle( "Google" ) ).ToCollection()
												.ToComponentListItem() )
										.Append(
											new EwfButton( new StandardButtonStyle( "Custom script" ), behavior: new CustomButtonBehavior( () => "alert('test!');" ) ).ToCollection()
												.ToComponentListItem() )
										.Append(
											new EwfButton( new StandardButtonStyle( "Modal" ), behavior: new OpenModalBehavior( one ) ).ToCollection().ToComponentListItem() ) )
								.ToCollection() ) ) )
				.Append( new ButtonSetup( "Modal Window", behavior: new OpenModalBehavior( two ) ) )
				.Materialize();

		List<LookupBoxSetup> UiEntitySetupBase.CreateLookupBoxSetups() =>
			new LookupBoxSetup( 100, "Lookup!", "lookup", text => { throw new DataModificationException( "Lookup '" + text + "' failed." ); } ).ToCollection()
				.ToList();

		IReadOnlyCollection<ActionComponentSetup> UiEntitySetupBase.GetActions() =>
			new ButtonSetup(
					"Delegate action",
					behavior: new PostBackBehavior(
						postBack: PostBack.CreateFull(
							id: "delegate",
							firstModificationMethod: () => EwfPage.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) ).ToCollection<ActionComponentSetup>()
				.Append( new HyperlinkSetup( new ExternalResourceInfo( "http://www.google.com" ), "Go to Google" ) )
				.Append(
					new ButtonSetup(
						"Generate error",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull( id: "error", firstModificationMethod: () => { throw new ApplicationException(); } ) ) ) )
				.Materialize();
	}
}