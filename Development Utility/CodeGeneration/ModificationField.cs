namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration;

internal class ModificationField {
	private readonly string source;
	private readonly string name;
	private readonly string pascalCasedName;
	private readonly string camelCasedName;
	private readonly Type type;
	private readonly string typeName;
	private readonly string nullableTypeName;
	private readonly string enumerableElementTypeName;
	private readonly int? size;
	private readonly short? numericScale;

	internal ModificationField(
		string source, string name, string pascalCasedName, string camelCasedName, Type type, string typeName, string nullableTypeName,
		string enumerableElementTypeName, int? size, short? numericScale ) {
		this.source = source;
		this.name = name;
		this.pascalCasedName = pascalCasedName;
		this.camelCasedName = camelCasedName;
		this.type = type;
		this.typeName = typeName;
		this.nullableTypeName = nullableTypeName;
		this.enumerableElementTypeName = enumerableElementTypeName;
		this.size = size;
		this.numericScale = numericScale;
	}

	internal string Source => source;
	internal string Name => name;
	internal string PascalCasedName => pascalCasedName;
	internal string CamelCasedName => camelCasedName;

	internal bool HasSuffix( string suffix, string contains = "" ) =>
		pascalCasedName.EndsWith( suffix, StringComparison.Ordinal ) && ( contains.Length == 0 || pascalCasedName.Contains( contains, StringComparison.Ordinal ) );

	internal bool TypeIs( Type type ) => this.type == type;
	internal string TypeName => typeName;
	internal string NullableTypeName => nullableTypeName;
	internal string EnumerableElementTypeName => enumerableElementTypeName;
	internal int? Size => size;
	internal short? NumericScale => numericScale;
}