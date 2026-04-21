namespace EnterpriseWebLibrary.Configuration.SystemDevelopment;

partial class SystemDevelopmentConfiguration {
	public WebProject GetWebProject( string name ) => webProjects.Single( i => string.Equals( i.name, name, StringComparison.Ordinal ) );

	public IEnumerable<ServerSideConsoleProject> ServerSideConsoleProjectsNonNullable =>
		serverSideConsoleProjects ?? Enumerable.Empty<ServerSideConsoleProject>();
}

partial class WebProject {
	/// <summary>
	/// Returns true if this web project uses the classic (non-SDK-style) MSBuild project format — i.e. it requires MSBuild
	/// and legacy NuGet tooling rather than <c>dotnet</c>. Detection reads the project file's root element and checks for
	/// the absence of an <c>Sdk</c> attribute.
	/// </summary>
	public bool IsClassic( string installationPath ) {
		var projectFilePath = EwlStatics.CombinePaths( installationPath, name, name + ".csproj" );
		if( !File.Exists( projectFilePath ) )
			return false;
		using var reader = System.Xml.XmlReader.Create( projectFilePath, new System.Xml.XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true } );
		while( reader.Read() )
			if( reader.NodeType == System.Xml.XmlNodeType.Element )
				return reader.GetAttribute( "Sdk" ) is null;
		return false;
	}
}