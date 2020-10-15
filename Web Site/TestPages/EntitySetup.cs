using System;
using System.Collections.Generic;
using System.Linq;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.WebSessionState;
using Humanizer;
using Tewl.Tools;

// OptionalParameter: string someText

namespace EnterpriseWebLibrary.WebSite.TestPages {
	partial class EntitySetup: UiEntitySetup {
		protected override ResourceInfo createParentResourceInfo() => null;

		protected override List<ResourceGroup> createResourceInfos() =>
			new List<ResourceGroup>
				{
					new ResourceGroup( "Working Stuff", new ActionControls.Info( this ), new OptionalParameters.Info( this ), new OmniDemo.Info( this ) ),
					new ResourceGroup( "First category", new HtmlEditing.Info( this ), new RegexHelper.Info( this ), new StatusMessages.Info( this ) ),
					new ResourceGroup( "Tables", new EwfTableDemo.Info( this ), new ColumnPrimaryTableDemo.Info( this ) ),
					new ResourceGroup( "Layout", new BoxDemo.Info( this ) ),
					new ResourceGroup(
						"Form Controls",
						new TextControlDemo.Info( this ),
						new NumberControlDemo.Info( this ),
						new Checkboxes.Info( this ),
						new SelectListDemo.Info( this ),
						new CheckboxListDemo.Info( this ),
						new DateAndTimePickers.Info( this ) ),
					new ResourceGroup(
						"Other",
						new IntermediatePostBacks.Info( this ),
						new ModalBoxes.Info( this ),
						new MailMerging.Info( this ),
						new Charts.Info( this ) )
				};

		public override string EntitySetupName => "Customer #1";

		EntityUiSetup UiEntitySetup.GetUiSetup() {
			var one = new ModalBoxId();
			var two = new ModalBoxId();
			return new EntityUiSetup(
				navActions:
				new HyperlinkSetup( new ExternalResourceInfo( "http://www.microsoft.com" ), "Go to Microsoft" )
					.Append<ActionComponentSetup>( new ButtonSetup( "Custom script", behavior: new CustomButtonBehavior( () => "alert('test');" ) ) )
					.Append(
						new ButtonSetup(
							"Menu",
							behavior: new MenuButtonBehavior(
								new StackList(
									new EwfHyperlink( new ExternalResourceInfo( "http://www.apple.com" ), new StandardHyperlinkStyle( "Apple" ) ).ToComponentListItem()
										.Append(
											new EwfHyperlink( new ExternalResourceInfo( "http://www.microsoft.com" ), new StandardHyperlinkStyle( "Microsoft" ) )
												.ToComponentListItem() )
										.Append(
											new EwfHyperlink( new ExternalResourceInfo( "http://www.google.com" ), new StandardHyperlinkStyle( "Google" ) ).ToComponentListItem() )
										.Append(
											new EwfButton( new StandardButtonStyle( "Custom script" ), behavior: new CustomButtonBehavior( () => "alert('test!');" ) )
												.ToComponentListItem() )
										.Append(
											new EwfButton(
													new StandardButtonStyle( "Modal" ),
													behavior: new OpenModalBehavior(
														one,
														etherealChildren: new ModalBox(
															one,
															true,
															new Paragraph( "This is a modal box!".ToComponents() ).ToCollection() ).ToCollection() ) )
												.ToComponentListItem() ) ).ToCollection() ) ) )
					.Append(
						new ButtonSetup(
							"Modal Window",
							behavior: new OpenModalBehavior(
								two,
								etherealChildren: new ModalBox(
									two,
									true,
									new EwfImage( new ImageSetup( "Houses in the mountains" ), new ExternalResourceInfo( "http://r0k.us/graphics/kodak/kodak/kodim08.png" ) )
										.ToCollection() ).ToCollection() ) ) )
					.Materialize(),
				navFormControls:
				NavFormControl.CreateText(
						new NavFormControlSetup( 100.ToPixels(), "Lookup!" ),
						v => new NavFormControlValidationResult( "Lookup '{0}' failed.".FormatWith( v ) ) )
					.ToCollection(),
				actions: new ButtonSetup(
						"Delegate action",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: "delegate",
								firstModificationMethod: () => EwfPage.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) )
					.Append<ActionComponentSetup>(
						new HyperlinkSetup( new ExternalResourceInfo( "http://www.google.com" ).ToHyperlinkNewTabBehavior(), "Go to Google in new window" ) )
					.Append(
						new ButtonSetup(
							"Generate error",
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull( id: "error", firstModificationMethod: () => { throw new ApplicationException(); } ) ) ) )
					.Materialize(),
				entitySummaryContent: new Paragraph(
					"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis semper dui. Aenean egestas dolor ac elementum lacinia. Vestibulum eget."
						.ToComponents() ).ToCollection() );
		}
	}
}