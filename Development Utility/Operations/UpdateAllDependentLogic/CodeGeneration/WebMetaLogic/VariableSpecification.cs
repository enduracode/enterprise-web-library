using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	/// <summary>
	/// The specification for a parameter or a page state variable.
	/// </summary>
	internal class VariableSpecification {
		private static readonly CSharpCodeProvider provider = new CSharpCodeProvider();
		private static readonly Dictionary<string, Type> rawTypeNamesToTypes = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, string> typesToNormalizedTypeNames = new Dictionary<Type, string>();

		private readonly Type type;
		private readonly string normalizedTypeName;
		private readonly string normalizedElementTypeName;
		private readonly string name;
		private readonly string comment;

		public VariableSpecification( string typeName, string name, string comment ) {
			if( !rawTypeNamesToTypes.TryGetValue( typeName, out type ) ) {
				// We need to compile some fake code because it's the only way to evaluate C# type alias such as "string" and "int?".
				// This code block has a known memory leak because it is impossible to unload the assembly we create. Also, most people would think the performance
				// here is inexcusably awful.
				var compilerResults =
					provider.CompileAssemblyFromSource( new CompilerParameters { GenerateInMemory = true, GenerateExecutable = false, IncludeDebugInformation = false },
					                                    "using System; using System.Collections.Generic; public class A { public " + typeName + " B; }" );

				if( compilerResults.Errors.HasErrors || compilerResults.Errors.HasWarnings )
					throw new UserCorrectableException( "The type name \"" + typeName + "\" is invalid." );
				type = ( (FieldInfo)compilerResults.CompiledAssembly.GetType( "A" ).GetMember( "B" ).Single() ).FieldType;
				rawTypeNamesToTypes.Add( typeName, type );
			}

			if( !isSupportedValueType( type ) && !isSupportedNullableType( type, isSupportedValueType ) && type != typeof( string ) && !isSupportedEnumerable( type ) )
				throw new UserCorrectableException( "The type \"" + typeName + "\" is not supported." );

			normalizedTypeName = getNormalizedTypeName( type );
			normalizedElementTypeName = type.IsGenericType && type.GetGenericTypeDefinition() == typeof( IEnumerable<> )
			                            	? getNormalizedTypeName( type.GetGenericArguments().Single() )
			                            	: "";
			this.name = name;
			this.comment = comment.Trim();
		}

		private static bool isSupportedEnumerable( Type type ) {
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof( IEnumerable<> ) && isSupportedIntegralType( type.GetGenericArguments().Single() );
		}

		private static bool isSupportedValueType( Type type ) {
			return isSupportedIntegralType( type ) ||
			       new[] { typeof( float ), typeof( double ), typeof( decimal ), typeof( bool ), typeof( DateTime ), typeof( DateTimeOffset ), typeof( TimeSpan ) }.
			       	Contains( type ) || type.IsEnum;
		}

		private static bool isSupportedIntegralType( Type type ) {
			return
				new[] { typeof( sbyte ), typeof( byte ), typeof( char ), typeof( short ), typeof( ushort ), typeof( int ), typeof( uint ), typeof( long ), typeof( ulong ) }
					.Contains( type );
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

		public string TypeName { get { return normalizedTypeName; } }
		public bool IsString { get { return type == typeof( string ); } }
		internal bool IsEnumerable { get { return normalizedElementTypeName.Any(); } }
		internal string EnumerableInitExpression { get { return IsEnumerable ? "new " + normalizedElementTypeName + "[ 0 ]" : ""; } }

		public string Name { get { return name; } }
		public string PropertyName { get { return name.CapitalizeString(); } }
		public string FieldName { get { return "_" + name; } }

		public string Comment { get { return comment; } }

		internal string GetUrlSerializationExpression( string valueExpression ) {
			if( IsEnumerable )
				return "StringTools.ConcatenateWithDelimiter( \",\", " + valueExpression + ".Select( i => i.ToString() ).ToArray() )";
			return valueExpression + ".ObjectToString( true )";
		}

		internal string GetUrlDeserializationExpression( string valueExpression ) {
			// For strings, we don't need to do a conversion at all.
			if( IsString )
				return valueExpression;

			if( IsEnumerable ) {
				return valueExpression + ".Separate( \",\", true ).Select( i => (" + normalizedElementTypeName + ")StandardLibraryMethods.ChangeType( i, typeof( " +
				       normalizedElementTypeName + " ) ) ).ToArray()";
			}

			// For non-strings, coalesce empty string into null, because things like int? need to be null to change their type from string properly.
			var expressionToConvert = valueExpression + " == \"\" ? null : " + valueExpression;
			return "(" + TypeName + ")StandardLibraryMethods.ChangeType( " + expressionToConvert + ", typeof( " + TypeName + " ) )";
		}

		internal ModificationField GetModificationField() {
			var nullableTypeName = normalizedTypeName;

			// If type is a value type and is not already Nullable, make the type name use Nullable via the ? syntax.
			if( type.IsValueType && Nullable.GetUnderlyingType( type ) == null )
				nullableTypeName += "?";

			return new ModificationField( type, normalizedTypeName, nullableTypeName, normalizedElementTypeName, PropertyName, PropertyName, null );
		}
	}
}