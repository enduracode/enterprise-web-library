// OptionalParameter: string someText

namespace EnterpriseWebLibrary.Website.TestPages;

partial class EntitySetup: UiEntitySetup {
	protected override ResourceBase? createParent() => null;

	protected override string getEntitySetupName() => "Web Framework Demo";

	public override ResourceBase DefaultResource => new BoxDemo( this );

	protected override IEnumerable<ResourceGroup> createListedResources() =>
		new[]
			{
				new ResourceGroup( "Layout", new BoxDemo( this ), new ComponentLists( this ), new FormItemListDemo( this ) ),
				new ResourceGroup( "Tables", new EwfTableDemo( this ), new ColumnPrimaryTableDemo( this ), new ResponsiveTableDemo( this ) ),
				new ResourceGroup(
					"Form Controls",
					new TextControlDemo( this ),
					new NumberControlDemo( this ),
					new Checkboxes( this ),
					new SelectListDemo( this ),
					new CheckboxListDemo( this ),
					new DateAndTimePickers( this ),
					new FileUploadDemo( this ) ),
				new ResourceGroup(
					"Page Functionality",
					new OptionalParametersDemo( this ),
					new StatusMessages( this ),
					new DataUpdateModification( this ),
					new IntermediatePostBacks( this ),
					new KeyboardFocus( this ) ),
				new ResourceGroup( "Business Tools", new MailMerging( this ), new Charts( this ), new CalendarIntegration( this ) ),
				new ResourceGroup( "Other", new ActionControls( this ), new HtmlEditing( this ), new RegexHelper( this ), new ModalBoxes( this ) )
			};

	protected override UrlHandler getRequestHandler() => new BoxDemo( this );
	protected override bool canRepresentRequestHandler() => true;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		RequestDispatchingStatics.GetFrameworkUrlPatterns( WebApplicationNames.Website )
			.Append( BoxDemo.UrlPatterns.Literal( this, "box" ) )
			.Append( FormItemListDemo.UrlPatterns.Literal( this, "form-item-list" ) )
			.Append( EwfTableDemo.UrlPatterns.Literal( this, "standard-table" ) )
			.Append( ResponsiveTableDemo.UrlPatterns.Literal( this, "responsive-table" ) )
			.Append( TextControlDemo.UrlPatterns.Literal( this, "text-control" ) )
			.Append( NumberControlDemo.UrlPatterns.Literal( this, "number-control" ) )
			.Append( DateAndTimePickers.UrlPatterns.Literal( this, "date-time-controls" ) )
			.Append( FileUploadDemo.UrlPatterns.Literal( this, "file-upload" ) )
			.Append( DataUpdateModification.UrlPatterns.Literal( this, "data-update-modification" ) )
			.Append( IntermediatePostBacks.UrlPatterns.Literal( this, "intermediate-post-backs" ) )
			.Append( KeyboardFocus.UrlPatterns.Literal( this, "keyboard-focus" ) )
			.Append( MailMerging.UrlPatterns.Literal( this, "mail-merging" ) )
			.Append( Charts.UrlPatterns.Literal( this, "chart" ) )
			.Append( CalendarIntegration.UrlPatterns.Literal( this, "calendar-integration" ) )
			.Append( UnauthorizedPage.UrlPatterns.Literal( this, "unauthorized-page" ) )
			.Append( CreateSystem.UrlPatterns.Literal( "create-system" ) )
			.Append( ConfigurationSchemas.EntitySetup.UrlPatterns.Literal( "ConfigurationSchemas" ) )
			.Concat( LegacyUrlStatics.GetPatterns() );

	EntityUiSetup UiEntitySetup.GetUiSetup() {
		return new EntityUiSetup(
			navActionGetter: _ => {
				var one = new ModalBoxId();
				var two = new ModalBoxId();
				var unauthorizedPage = new UnauthorizedPage( this );
				return new HyperlinkSetup( new ExternalResource( "http://www.microsoft.com" ), "Go to Microsoft" )
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
					.Append( new HyperlinkSetup( unauthorizedPage.ToHyperlinkDefaultBehavior( disableAuthorizationCheck: true ), unauthorizedPage.ResourceName ) )
					.Materialize();
			},
			navFormControls:
			NavFormControl.CreateText(
					new NavFormControlSetup( 100.ToPixels(), "Lookup!" ),
					v => new NavFormControlValidationResult( "Lookup '{0}' failed.".FormatWith( v ) ) )
				.ToCollection(),
			actionGetter: postBackIdBase =>
				new ButtonSetup(
						"Delegate action",
						behavior: new PostBackBehavior(
							postBack: PostBack.CreateFull(
								id: PostBack.GetCompositeId( postBackIdBase, "delegate" ),
								modificationMethod: () => PageBase.AddStatusMessage( StatusMessageType.Info, "Did Something." ) ) ) )
					.Append<ActionComponentSetup>(
						new HyperlinkSetup( new ExternalResource( "http://www.google.com" ).ToHyperlinkNewTabBehavior(), "Go to Google in new window" ) )
					.Append(
						new ButtonSetup(
							"Generate error",
							behavior: new PostBackBehavior(
								postBack: PostBack.CreateFull(
									id: PostBack.GetCompositeId( postBackIdBase, "error" ),
									modificationMethod: () => { throw new ApplicationException(); } ) ) ) )
					.Materialize(),
			entitySummaryContent: new Paragraph(
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis semper dui. Aenean egestas dolor ac elementum lacinia. Vestibulum eget."
					.ToComponents() ).ToCollection() );
	}
}