using System.CodeDom;
using System.Reflection;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.EnterpriseWebFramework;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core;
using EnterpriseWebLibrary.EnterpriseWebFramework.Core.ResourceMetaLogic;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using NodaTime;

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
		public bool SupportsNull { get; }
		public Func<bool> NamingConventionPredicate { get; }
		public string NamingConventionInstructions { get; }
		public string TypeName { get; }
		public string ElementTypeName { get; }
		public string InitExpression { get; }
		public Func<string, string> UrlSerializationExpressionGetter { get; }
		public Func<string, string, string> UrlDeserializationExpressionGetter { get; }
		public Func<string, string>? ReCreationExpressionGetter { get; }

		/// <summary>
		/// Do not support null for string or IEnumerable types because it cannot easily be represented in a URL.
		/// </summary>
		public DataType(
			Type type, bool supportsNull, Func<bool> namingConventionPredicate, string namingConventionInstructions, string typeName, string elementTypeName,
			string initExpression, Func<string, string> urlSerializationExpressionGetter, Func<string, string, string> urlDeserializationExpressionGetter,
			Func<string, string>? reCreationExpressionGetter = null ) {
			Type = type;
			SupportsNull = supportsNull;

			NamingConventionPredicate = namingConventionPredicate;
			NamingConventionInstructions = namingConventionInstructions;

			TypeName = typeName.Length > 0 ? typeName : type.Name;
			ElementTypeName = elementTypeName;
			InitExpression = initExpression;

			UrlSerializationExpressionGetter = urlSerializationExpressionGetter;
			UrlDeserializationExpressionGetter = urlDeserializationExpressionGetter;

			ReCreationExpressionGetter = reCreationExpressionGetter;
		}
	}

	private static IEnumerable<DataType> getSupportedTypes( string name ) {
		yield return new DataType(
			typeof( LocalDate ),
			true,
			() => hasSuffix( "Date" ) || nameIs( "date" ),
			"suffix the name with “Date” or make the name “date”",
			"",
			"",
			"",
			valueExpression => $"LocalDatePattern.Iso.Format( {valueExpression} )",
			( valueExpression, _ ) => $"LocalDatePattern.Iso.Parse( {valueExpression} ).GetValueOrThrow()" );

		yield return new DataType(
			typeof( PatternString ),
			false,
			() => hasSuffix( "Contains" ) || nameIs( "searchTerm" ),
			"suffix the name with “Contains” or make the name “searchTerm”",
			"",
			"",
			"""new PatternString( "" )""",
			valueExpression => $"{valueExpression}.Pattern",
			( valueExpression, _ ) => $"new PatternString( {valueExpression} )" );

		yield return new DataType(
			typeof( TrustedUrl ),
			true,
			() => hasSuffix( "Url" ) && !nameIs( "parentUrl" ),
			"suffix the name with “Url”",
			"",
			"",
			"TrustedUrl.Invalid",
			valueExpression => $"TrustedUrl.Serialize( {valueExpression}, base.AppId )",
			( valueExpression, appIdExpression ) => $"TrustedUrl.Deserialize( {valueExpression}, {appIdExpression} )",
			reCreationExpressionGetter: valueExpression => $"{valueExpression}.ReCreate()" );

		yield return new DataType(
			typeof( TrustedParentUrl ),
			true,
			() => nameIs( "parentUrl" ),
			"make the name “parentUrl”",
			"",
			"",
			"TrustedParentUrl.Invalid",
			valueExpression => $"TrustedParentUrl.Serialize( {valueExpression}, base.AppId )",
			( valueExpression, appIdExpression ) => $"TrustedParentUrl.Deserialize( {valueExpression}, {appIdExpression} )",
			reCreationExpressionGetter: valueExpression => $"{valueExpression}.ReCreate()" );

		yield break;
		bool hasSuffix( string suffix, string contains = "" ) => ModificationField.NameHasSuffix( name, suffix, contains );
		bool nameIs( string value ) => value.Equals( name, StringComparison.Ordinal );
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

	private readonly DataType type;
	public bool AllowsNull { get; }
	private readonly string name;
	private readonly string comment;

	public WebItemParameter( string typeName, string name, string comment ) {
		var supportedTypes = getSupportedTypes( name ).Materialize();
		AllowsNull = typeName.EndsWith( '?' );
		var nnTypeName = AllowsNull ? typeName[ ..^1 ] : typeName;
		foreach( var supportedType in supportedTypes )
			if( nnTypeName.Length > 0 ? supportedType.Type.Name.Equals( nnTypeName, StringComparison.Ordinal ) : supportedType.NamingConventionPredicate() ) {
				if( !supportedType.SupportsNull && AllowsNull )
					throw new UserCorrectableException( $"The parameter type {supportedType.TypeName} does not support null." );

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
						throw getException();
					compilationType = ( (FieldInfo)Assembly.Load( stream.ToArray() ).GetType( "A" )!.GetMember( "B" ).Single() ).FieldType;
				}

				if( !isSupportedValueType( compilationType ) && !isSupportedNullableType( compilationType, isSupportedValueType ) &&
				    compilationType != typeof( string ) && !isSupportedEnumerable( compilationType ) )
					throw getException();

				rawTypeNamesToTypes.Add( typeName, compilationType );

				Exception getException() =>
					new UserCorrectableException(
						$"The parameter {( nnTypeName.Length > 0 ? $"type {nnTypeName}" : $"\"{name}\"" )} is not supported. Please use one of the types below (implicitly via naming convention if possible):" +
						Environment.NewLine + Environment.NewLine + StringTools.ConcatenateWithDelimiter(
							Environment.NewLine,
							supportedTypes.Select( i => $"{i.Type.Name}: {i.NamingConventionInstructions}" ) ) );
			}

			type = new DataType(
				compilationType,
				compilationType.IsValueType && Nullable.GetUnderlyingType( compilationType ) is not null,
				() => throw new NotSupportedException(),
				"",
				getNormalizedTypeName( compilationType )[ ..^( AllowsNull ? 1 : 0 ) ],
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

					return valueExpression + ".ToString()!";
				},
				( valueExpression, _ ) => {
					// For strings, we don't need to do a conversion at all.
					if( compilationType == typeof( string ) )
						return valueExpression;

					if( IsEnumerable )
						return valueExpression + ".Separate( \",\", true ).Select( i => (" + type!.ElementTypeName + ")EwlStatics.ChangeType( i, typeof( " +
						       type.ElementTypeName + " ) ) ).Materialize()";

					return $"({type!.TypeName})EwlStatics.ChangeType( {valueExpression}, typeof( {type.TypeName} ) )";
				} );
		}

		this.name = name;
		this.comment = comment.Trim();
	}

	public string TypeName => type.TypeName + ( AllowsNull ? "?" : "" );

	public string InitExpression => AllowsNull ? "" : type.InitExpression;

	public string SpecifiableTypeName => ( AllowsNull ? $"SpecifiedValue<{TypeName}>" : TypeName ) + "?";

	public string GetSpecifiableValueExpression( string valueExpression ) =>
		AllowsNull ? $"new SpecifiedValue<{TypeName}>( {valueExpression} )" : valueExpression;

	public string SpecifiedValueSelector => type.Type.IsValueType || AllowsNull ? ".Value" : "";

	internal bool IsEnumerable => type.ElementTypeName.Any();
	internal bool IsNestedUrl => type.Type.IsAssignableTo( typeof( NestedUrl ) );

	public string Name => name;
	public string PropertyName => name.Capitalize();
	public string FieldName => "__" + name;

	public string Comment => comment;

	internal string GetUrlSerializationExpression( string valueExpression ) =>
		AllowsNull
			? $$"""
			    {{valueExpression}} is {} __nonnullable ? {{type.UrlSerializationExpressionGetter( "__nonnullable" )}} : ""
			    """
			: type.UrlSerializationExpressionGetter( valueExpression );

	internal string GetUrlDeserializationExpression( string valueExpression, string appIdExpression ) =>
		AllowsNull
			? $$"""
			    {{valueExpression}} is { Length: > 0 } __nonempty ? {{type.UrlDeserializationExpressionGetter( "__nonempty", appIdExpression )}} : null
			    """
			: type.UrlDeserializationExpressionGetter( valueExpression, appIdExpression );

	internal ModificationField GetModificationField() =>
		new(
			"parameter",
			PropertyName,
			PropertyName,
			name,
			type.Type,
			TypeName,
			TypeName + ( AllowsNull || type.Type == typeof( string ) ? "" : "?" ),
			type.ElementTypeName,
			null,
			null );

	internal string GetReCreationExpression( string valueExpression ) =>
		type.ReCreationExpressionGetter is null ? valueExpression :
		AllowsNull ? $$"""
		               {{valueExpression}} is {} __nonnullable ? {{type.ReCreationExpressionGetter( "__nonnullable" )}} : null
		               """ : type.ReCreationExpressionGetter( valueExpression );

	internal string GetEqualityExpression( string x, string y ) =>
		IsEnumerable ? "{0}.SequenceEqual( {1} )".FormatWith( x, y ) : "EwlStatics.AreEqual( {0}, {1} )".FormatWith( x, y );
}