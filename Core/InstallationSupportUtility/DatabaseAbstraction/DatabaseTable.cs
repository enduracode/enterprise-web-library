namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

/// <summary>
/// A database table, including the schema name if the database supports multiple schemas.
/// </summary>
/// <param name="Schema">The schema name. Empty if the database does not support multiple schemas.</param>
/// <param name="Name">The table name.</param>
public record DatabaseTable( string Schema, string Name ) {
	public string QualifiedName => Schema.AppendDelimiter( "." ) + Name;
}