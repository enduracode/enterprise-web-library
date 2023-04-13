namespace EnterpriseWebLibrary.Website.ConfigurationSchemas;

partial class EntitySetup {
	protected override ResourceParent createParent() => null;
	protected override string getEntitySetupName() => "Configuration Schemas";
	public override ResourceBase DefaultResource => throw new NotSupportedException();
	protected override IEnumerable<ResourceGroup> createListedResources() => Enumerable.Empty<ResourceGroup>();
	protected override UrlHandler getUrlParent() => new TestPages.EntitySetup();
	protected override UrlHandler getRequestHandler() => null;

	protected override IEnumerable<UrlPattern> getChildUrlPatterns() =>
		new UrlPattern(
			encoder => encoder is GetSchema.UrlEncoder schema && schema.CheckEntitySetup( this )
				           ? EncodingUrlSegment.Create( schema.GetFileName().EnglishToPascal() )
				           : null,
			url => GlobalStatics.ConfigurationXsdFileNames.Where( i => string.Equals( i.EnglishToPascal(), url.Segment, StringComparison.OrdinalIgnoreCase ) )
				.Select( i => new GetSchema.UrlDecoder( this, fileName: i ) )
				.FirstOrDefault() ).ToCollection();
}