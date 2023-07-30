using System.CodeDom;
using System.Reflection;
using System.Text.RegularExpressions;
using EnterpriseWebLibrary.InstallationSupportUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	/// <summary>
	/// The specification for a parameter or a page state variable.
	/// </summary>
	internal class WebItemParameter {
		private static readonly CSharpCodeProvider provider = new CSharpCodeProvider();
		private static readonly Dictionary<string, Type> rawTypeNamesToTypes = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, string> typesToNormalizedTypeNames = new Dictionary<Type, string>();

		private readonly Type type;
		private readonly string normalizedTypeName;
		private readonly string normalizedElementTypeName;
		private readonly string name;
		private readonly string comment;

		public WebItemParameter( string typeName, string name, string comment ) {
			if( !rawTypeNamesToTypes.TryGetValue( typeName, out type ) ) {
				// We need to compile some fake code because it’s the only way to evaluate C# type alias such as “string” and “int?”.
				using( var stream = new MemoryStream() ) {
					var result = CSharpCompilation.Create(
							null,
							syntaxTrees: CSharpSyntaxTree.ParseText( "using System; using System.Collections.Generic; public class A { public " + typeName + " B; }" )
								.ToCollection(),
							references: MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ).ToCollection(),
							options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) )
						.Emit( stream );
					if( !result.Success )
						throw new UserCorrectableException( "The type name \"" + typeName + "\" is invalid." );
					type = ( (FieldInfo)Assembly.Load( stream.ToArray() ).GetType( "A" ).GetMember( "B" ).Single() ).FieldType;
				}
				rawTypeNamesToTypes.Add( typeName, type );
			}

			if( !isSupportedValueType( type ) && !isSupportedNullableType( type, isSupportedValueType ) && type != typeof( string ) &&
			    !isSupportedEnumerable( type ) )
				throw new UserCorrectableException( "The type \"" + typeName + "\" is not supported." );

			normalizedTypeName = getNormalizedTypeName( type );
			normalizedElementTypeName = type.IsGenericType && type.GetGenericTypeDefinition() == typeof( IEnumerable<> )
				                            ? getNormalizedTypeName( type.GetGenericArguments().Single() )
				                            : "";
			this.name = name;
			this.comment = comment.Trim();
		}

		private static bool isSupportedEnumerable( Type type ) {
			if( !type.IsGenericType || type.GetGenericTypeDefinition() != typeof( IEnumerable<> ) )
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
			string name;
			if( !typesToNormalizedTypeNames.TryGetValue( type, out name ) ) {
				// We need to do this or int? ends up being "System.Nullable`1", which is useless.
				name = provider.GetTypeOutput( new CodeTypeReference( type ) );

				// Do this to take the qualifies off type names (System.Collections.Generic.List<System.DateTime> => List<DateTime>).
				name = Regex.Replace( name, @"(\w+)\.", "" );

				// Do this to turn Nullable<int> into int?, etc.
				name = Regex.Replace( name, @"Nullable<(.+?)>", "$+?" );

				typesToNormalizedTypeNames.Add( type, name );
			}
			return name;
		}

		public string TypeName => normalizedTypeName;
		public bool TypeIsNullable => type.IsValueType && Nullable.GetUnderlyingType( type ) is not null;
		public bool IsString => type == typeof( string );
		internal bool IsEnumerable => normalizedElementTypeName.Any();
		internal string EnumerableInitExpression => IsEnumerable ? "new " + normalizedElementTypeName + "[ 0 ]" : "";

		public string Name => name;
		public string PropertyName => name.CapitalizeString();
		public string FieldName => "__" + name;

		public string Comment => comment;

		internal string GetUrlSerializationExpression( string valueExpression ) {
			if( IsEnumerable )
				return "StringTools.ConcatenateWithDelimiter( \",\", " + valueExpression + ".Select( i => i.ToString() ).ToArray() )";
			return valueExpression + ".ObjectToString( true )";
		}

		internal string GetUrlDeserializationExpression( string valueExpression ) {
			// For strings, we don't need to do a conversion at all.
			if( IsString )
				return valueExpression;

			if( IsEnumerable )
				return valueExpression + ".Separate( \",\", true ).Select( i => (" + normalizedElementTypeName + ")EwlStatics.ChangeType( i, typeof( " +
				       normalizedElementTypeName + " ) ) ).ToArray()";

			// For non-strings, coalesce empty string into null, because things like int? need to be null to change their type from string properly.
			var expressionToConvert = valueExpression + " == \"\" ? null : " + valueExpression;
			return "(" + TypeName + ")EwlStatics.ChangeType( " + expressionToConvert + ", typeof( " + TypeName + " ) )";
		}

		internal ModificationField GetModificationField() =>
			new(
				PropertyName,
				PropertyName,
				name,
				type,
				normalizedTypeName,
				normalizedTypeName + ( TypeIsNullable ? "" : "?" ),
				normalizedElementTypeName,
				null,
				null );

		internal string GetEqualityExpression( string x, string y ) =>
			IsEnumerable ? "{0}.SequenceEqual( {1} )".FormatWith( x, y ) : "EwlStatics.AreEqual( {0}, {1} )".FormatWith( x, y );
	}
}