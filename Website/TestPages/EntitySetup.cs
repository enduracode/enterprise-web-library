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
		protected override ResourceBase createParentResource() => null;

		public override string EntitySetupName => "Web Framework Demo";

		protected override IEnumerable<ResourceGroup> createListedResources() =>
			new List<ResourceGroup>
				{
					new ResourceGroup( "Layout", new BoxDemo( this ), new ComponentLists( this ) ),
					new ResourceGroup( "Tables", new EwfTableDemo( this ), new ColumnPrimaryTableDemo( this ) ),
					new ResourceGroup(
						"Form Controls",
						new TextControlDemo( this ),
						new NumberControlDemo( this ),
						new Checkboxes( this ),
						new SelectListDemo( this ),
						new CheckboxListDemo( this ),
						new DateAndTimePickers( this ) ),
					new ResourceGroup( "Working Stuff", new ActionControls( this ), new OptionalParametersDemo( this ), new OmniDemo( this ) ),
					new ResourceGroup( "First category", new HtmlEditing( this ), new RegexHelper( this ), new StatusMessages( this ) ),
					new ResourceGroup( "Other", new IntermediatePostBacks( this ), new ModalBoxes( this ), new MailMerging( this ), new Charts( this ) )
				};

		protected override UrlHandler getRequestHandler() => new BoxDemo( this );
		protected override bool canRepresentRequestHandler() => true;

		protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
			RequestDispatchingStatics.GetFrameworkUrlPatterns()
				.Append( BoxDemo.UrlPatterns.Literal( this, "box" ) )
				.Append( EwfTableDemo.UrlPatterns.Literal( this, "ewf-table" ) )
				.Append( CreateSystem.UrlPatterns.Literal( "create-system" ) )
				.Append( ConfigurationSchemas.EntitySetup.UrlPatterns.Literal( "ConfigurationSchemas" ) )
				.Concat( LegacyUrlStatics.GetPatterns() );

		EntityUiSetup UiEntitySetup.GetUiSetup() {
			var one = new ModalBoxId();
			var two = new ModalBoxId();
			return new EntityUiSetup(
				navActions:
				new HyperlinkSetup( new ExternalResource( "http://www.microsoft.com" ), "Go to Microsoft" )
					.Append<ActionComponentSetup>( new ButtonSetup( "Custom script", behavior: new CustomButtonBehavior( () => "alert('test');" ) ) )
					.Append(
						new ButtonSetup(
							"Menu",
							behavior: new MenuButtonBehavior(
								new StackList(
									new EwfHyperlink( new ExternalResource( "http://www.apple.com" ), new StandardHyperlinkStyle( "Apple" ) ).ToComponentListItem()
										.Append(
											new EwfHyperlink( new ExternalResource( "http://www.microsoft.com" ), new StandardHyperlinkStyle( "Microsoft" ) ).ToComponentListItem() )
										.Append( new EwfHyperlink( new ExternalResource( "http://www.google.com" ), new StandardHyperlinkStyle( "Google" ) ).ToComponentListItem() )
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
									new EwfImage( new ImageSetup( "Houses in the mountains" ), new ExternalResource( "http://r0k.us/graphics/kodak/kodak/kodim08.png" ) )
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
								modificationMethod: () => PageBase.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) )
					.Append<ActionComponentSetup>(
						new HyperlinkSetup( new ExternalResource( "http://www.google.com" ).ToHyperlinkNewTabBehavior(), "Go to Google in new window" ) )
					.Append(
						new ButtonSetup(
							"Generate error",
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull( id: "error", modificationMethod: () => { throw new ApplicationException(); } ) ) ) )
					.Materialize(),
				entitySummaryContent: new Paragraph(
					"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis semper dui. Aenean egestas dolor ac elementum lacinia. Vestibulum eget."
						.ToComponents() ).ToCollection() );
		}
	}
}