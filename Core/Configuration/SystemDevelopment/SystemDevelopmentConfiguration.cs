namespace EnterpriseWebLibrary.Configuration.SystemDevelopment;

partial class SystemDevelopmentConfiguration {
	public WebProject GetWebProject( string name ) => webProjects.Single( i => string.Equals( i.name, name, StringComparison.Ordinal ) );

	public IEnumerable<ServerSideConsoleProject> ServerSideConsoleProjectsNonNullable =>
		serverSideConsoleProjects ?? Enumerable.Empty<ServerSideConsoleProject>();
}