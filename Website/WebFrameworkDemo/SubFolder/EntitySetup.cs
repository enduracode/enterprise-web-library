namespace EnterpriseWebLibrary.Website.WebFrameworkDemo.SubFolder;

partial class EntitySetup: UiEntitySetup {
	protected override ResourceBase createParent() => ActionControls.GetInfo();

	protected override string getEntitySetupName() => "Nested";

	public override ResourceBase DefaultResource => new General( this );

	protected override IEnumerable<ResourceGroup> createListedResources() =>
		new[] { new ResourceGroup( new General( this ), new Details( this ), new Disabled( this ), new New( this ) ) };

	protected override UrlHandler? getRequestHandler() => null;

	EntityUiSetup UiEntitySetup.GetUiSetup() =>
		new( entitySummaryContent: new Paragraph( "Awesome content goes here.".ToComponents() ).ToCollection(), tabMode: TabMode.Horizontal );
}