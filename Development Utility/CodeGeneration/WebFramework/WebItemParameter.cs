using System.CodeDom;
using System.Reflection;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework;

/// <summary>
/// The specification for a parameter.
/// </summary>
internal class WebItemParameter {
	private static readonly CSharpCodeProvider provider = new();
	private static readonly Dictionary<string, Type> rawTypeNamesToTypes = new();
	private static readonly Dictionary<Type, string> typesToNormalizedTypeNames = new();

	private sealed class DataType {
		public Type Type { get; }
		public bool AllowsNull { get; }
		public Func<bool> NamingConventionPredicate { get; }
		public string NamingConventionInstructions { get; }
		public string TypeName { get; }
		public string ElementTypeName { get; }
		public string InitExpression { get; }
		public Func<string, string> UrlSerializationExpressionGetter { get; }
		public Func<string, string> UrlDeserializationExpressionGetter { get; }

		public DataType(
			Type type, bool allowsNull, Func<bool> namingConventionPredicate, string namingConventionInstructions, string typeName, string elementTypeName,
			string initExpression, Func<string, string> urlSerializationExpressionGetter, Func<string, string> urlDeserializationExpressionGetter ) {
			Type = type;
			AllowsNull = allowsNull;

			NamingConventionPredicate = namingConventionPredicate;
			NamingConventionInstructions = namingConventionInstructions;

			TypeName = typeName.Length > 0 ? typeName : Type.Name;
			ElementTypeName = elementTypeName;
			InitExpression = initExpression;

			UrlSerializationExpressionGetter = urlSerializationExpressionGetter;
			UrlDeserializationExpressionGetter = urlDeserializationExpressionGetter;
		}
	}

	private static IEnumerable<DataType> getSupportedTypes( string name ) {
		yield return new DataType(
			typeof( PatternString ),
			false,
			() => hasSuffix( "Contains" ) || nameIs( "searchTerm" ),
			"suffix the name with “Contains” or make the name “searchTerm”",
			"",
			"",
			"""new PatternString( "" )""",
			valueExpression => $"{valueExpression}.Pattern",
			valueExpression => $"new PatternString( {valueExpression} )" );

		yield break;
		bool hasSuffix( string suffix, string contains = "" ) => ModificationField.NameHasSuffix( name, suffix, contains );
		bool nameIs( string value ) => value.Equals( name, StringComparison.Ordinal );
	}

	private readonly DataType type;
	private readonly string name;
	private readonly string comment;

	public WebItemParameter( string typeName, string name, string comment ) {
		var supportedTypes = getSupportedTypes( name ).Materialize();
		foreach( var supportedType in supportedTypes )
			if( typeName.Length > 0 ? supportedType.Type.Name.Equals( typeName, StringComparison.Ordinal ) : supportedType.NamingConventionPredicate() ) {
				type = supportedType;
				break;
			}

		// Legacy logic; we’re moving toward supporting all types via the list above.
		if( type is null ) {
			if( !rawTypeNamesToTypes.TryGetValue( typeName, out var compilationType ) ) {
				// We need to compile some fake code because it’s the only way to evaluate C# type alias such as “string” and “int?”.
				using( var stream = new MemoryStream() ) {
					var result = CSharpCompilation.Create(
							null,
							syntaxTrees: CSharpSyntaxTree.ParseText( "using System; using System.Collections.Generic; public class A { public " + typeName + " B; }" )
								.ToCollection(),
							references: MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ).ToCollection(),
							options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) )
						.Emit( stream );
					if( !result.Success || result.Diagnostics.Any( i => string.Equals( i.Id, "CS8632", StringComparison.Ordinal ) ) )
						throw new UserCorrectableException( "The type name \"" + typeName + "\" is invalid." );
					compilationType = ( (FieldInfo)Assembly.Load( stream.ToArray() ).GetType( "A" )!.GetMember( "B" ).Single() ).FieldType;
				}

				if( !isSupportedValueType( compilationType ) && !isSupportedNullableType( compilationType, isSupportedValueType ) &&
				    compilationType != typeof( string ) && !isSupportedEnumerable( compilationType ) )
					throw new UserCorrectableException(
						$"The parameter type {typeName} is not supported. Please use one of the types below (implicitly via naming convention if possible):" +
						Environment.NewLine + Environment.NewLine + StringTools.ConcatenateWithDelimiter(
							Environment.NewLine,
							supportedTypes.Select( i => $"{i.Type.Name}: {i.NamingConventionInstructions}" ) ) );

				rawTypeNamesToTypes.Add( typeName, compilationType );
			}

			type = new DataType(
				compilationType,
				compilationType.IsValueType && Nullable.GetUnderlyingType( compilationType ) is not null,
				() => throw new NotSupportedException(),
				"",
				getNormalizedTypeName( compilationType ),
				compilationType.IsGenericType && compilationType.GetGenericTypeDefinition() == typeof( IReadOnlyCollection<> )
					? getNormalizedTypeName( compilationType.GetGenericArguments().Single() )
					: "",
				compilationType == typeof( string ) ? "\"\"" :
				compilationType.IsGenericType && compilationType.GetGenericTypeDefinition() == typeof( IReadOnlyCollection<> ) ? "[]" : "",
				valueExpression => {
					if( compilationType == typeof( string ) )
						return valueExpression;

					if( IsEnumerable )
						return "StringTools.ConcatenateWithDelimiter( \",\", " + valueExpression + ".Select( i => i.ToString() ).Materialize() )";

					return TypeIsNullable
						       ? $"""
						          {valueExpression}.HasValue ? {valueExpression}.Value.ToString()! : ""
						          """
						       : valueExpression + ".ToString()!";
				},
				valueExpression => {
					// For strings, we don't need to do a conversion at all.
					if( compilationType == typeof( string ) )
						return valueExpression;

					if( IsEnumerable )
						return valueExpression + ".Separate( \",\", true ).Select( i => (" + type!.ElementTypeName + ")EwlStatics.ChangeType( i, typeof( " +
						       type.ElementTypeName + " ) ) ).Materialize()";

					// For non-strings, coalesce empty string into null, because things like int? need to be null to change their type from string properly.
					var expressionToConvert = valueExpression + " == \"\" ? null : " + valueExpression;
					return "(" + TypeName + ")EwlStatics.ChangeType( " + expressionToConvert + ", typeof( " + TypeName + " ) )";
				} );
		}

		this.name = name;
		this.comment = comment.Trim();
	}

	private static bool isSupportedEnumerable( Type type ) {
		if( !type.IsGenericType || type.GetGenericTypeDefinition() != typeof( IReadOnlyCollection<> ) )
			return false;
		var elementType = type.GetGenericArguments().Single();

		// Decimal support is helpful in systems that use Oracle.
		return isSupportedIntegralType( elementType ) || elementType == typeof( decimal );
	}

	private static bool isSupportedValueType( Type type ) {
		return isSupportedIntegralType( type ) || new[]
			{
				typeof( float ), typeof( double ), typeof( decimal ), typeof( bool ), typeof( DateTime ), typeof( DateTimeOffset ), typeof( TimeSpan )
			}.Contains( type ) || type.IsEnum;
	}

	private static bool isSupportedIntegralType( Type type ) {
		return new[]
			{
				typeof( sbyte ), typeof( byte ), typeof( char ), typeof( short ), typeof( ushort ), typeof( int ), typeof( uint ), typeof( long ), typeof( ulong )
			}.Contains( type );
	}

	private static bool isSupportedNullableType( Type type, Func<Type, bool> underlyingTypePredicate ) {
		var underlyingType = Nullable.GetUnderlyingType( type );
		return underlyingType != null && underlyingTypePredicate( underlyingType );
	}

	private static string getNormalizedTypeName( Type type ) {
		if( !typesToNormalizedTypeNames.TryGetValue( type, out var name ) ) {
			// We need to do this or int? ends up being "System.Nullable`1", which is useless.
			name = provider.GetTypeOutput( new CodeTypeReference( type ) );

			// Do this to take the qualifies off type names (System.Collections.Generic.List<System.DateTime> => List<DateTime>).
			name = Regex.Replace( name, @"(\w+)\.", "" );

			// Do this to turn Nullable<int> into int?, etc.
			name = Regex.Replace( name, "Nullable<(.+?)>", "$+?" );

			typesToNormalizedTypeNames.Add( type, name );
		}
		return name;
	}

	public string TypeName => type.TypeName;

	public bool TypeIsNullable => type.AllowsNull;

	public string InitExpression => type.InitExpression;

	public string SpecifiableTypeName => ( type.AllowsNull ? $"SpecifiedValue<{type.TypeName}>" : type.TypeName ) + "?";

	public string GetSpecifiableValueExpression( string valueExpression ) =>
		type.AllowsNull ? $"new SpecifiedValue<{type.TypeName}>( {valueExpression} )" : valueExpression;

	public string SpecifiedValueSelector => type.Type.IsValueType || type.AllowsNull ? ".Value" : "";

	internal bool IsEnumerable => type.ElementTypeName.Any();

	public string Name => name;
	public string PropertyName => name.Capitalize();
	public string FieldName => "__" + name;

	public string Comment => comment;

	internal string GetUrlSerializationExpression( string valueExpression ) => type.UrlSerializationExpressionGetter( valueExpression );
	internal string GetUrlDeserializationExpression( string valueExpression ) => type.UrlDeserializationExpressionGetter( valueExpression );

	internal ModificationField GetModificationField() =>
		new(
			"parameter",
			PropertyName,
			PropertyName,
			name,
			type.Type,
			type.TypeName,
			type.TypeName + ( type.AllowsNull || type.Type == typeof( string ) ? "" : "?" ),
			type.ElementTypeName,
			null,
			null );

	internal string GetEqualityExpression( string x, string y ) =>
		IsEnumerable ? "{0}.SequenceEqual( {1} )".FormatWith( x, y ) : "EwlStatics.AreEqual( {0}, {1} )".FormatWith( x, y );
}