using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class UrlStatics {
		internal static void GenerateUrlClasses(
			TextWriter writer, EntitySetup entitySetup, IReadOnlyCollection<VariableSpecification> requiredParameters,
			IReadOnlyCollection<VariableSpecification> optionalParameters, bool includeVersionString ) {
			generateEncoder( writer, entitySetup, requiredParameters, optionalParameters, includeVersionString );
		}

		private static void generateEncoder(
			TextWriter writer, EntitySetup entitySetup, IReadOnlyCollection<VariableSpecification> requiredParameters,
			IReadOnlyCollection<VariableSpecification> optionalParameters, bool includeVersionString ) {
			writer.WriteLine( "internal sealed class UrlEncoder: EnterpriseWebFramework.UrlEncoder {" );

			if( entitySetup != null ) {
				writer.WriteLine( "private readonly {0} entitySetup;".FormatWith( entitySetup.GeneralData.ClassName ) );
				writer.WriteLine( "private bool entitySetupMatched;" );
				writer.WriteLine( "private readonly Lazy<{0}.UrlEncoder> entitySetupEncoder;".FormatWith( entitySetup.GeneralData.ClassName ) );
			}
			foreach( var i in requiredParameters ) {
				writer.WriteLine( "private readonly {0} {1};".FormatWith( i.TypeName, i.Name ) );
				writer.WriteLine( "private bool {0}Accessed;".FormatWith( i.Name ) );
			}
			foreach( var i in optionalParameters ) {
				writer.WriteLine( "private readonly {0} {1};".FormatWith( getOptionalParameterType( i ), i.Name ) );
				writer.WriteLine( "private bool {0}Accessed;".FormatWith( i.Name ) );
			}
			if( includeVersionString ) {
				writer.WriteLine( "private readonly string versionString;" );
				writer.WriteLine( "private bool versionStringAccessed;" );
			}

			writer.WriteLine(
				"internal UrlEncoder({0}) {{".FormatWith(
					StringTools.ConcatenateWithDelimiter(
							", ",
							( entitySetup != null ? "{0} entitySetup".FormatWith( entitySetup.GeneralData.ClassName ).ToCollection() : Enumerable.Empty<string>() )
							.Concat( requiredParameters.Select( i => i.TypeName + " " + i.Name ) )
							.Concat( optionalParameters.Select( i => getOptionalParameterType( i ) + " " + i.Name ) )
							.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() ) )
						.Surround( " ", " " ) ) );
			if( entitySetup != null ) {
				writer.WriteLine( "this.entitySetup = entitySetup;" );
				writer.WriteLine(
					"entitySetupEncoder = new Lazy<{0}.UrlEncoder>( () => ({0}.UrlEncoder)( (UrlHandler)entitySetup ).GetEncoder(), LazyThreadSafetyMode.None );"
						.FormatWith( entitySetup.GeneralData.ClassName ) );
			}
			foreach( var i in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine( "this.{0} = {0};".FormatWith( i.Name ) );
			if( includeVersionString )
				writer.WriteLine( "this.versionString = versionString;" );
			writer.WriteLine( "}" );

			if( entitySetup != null ) {
				writer.WriteLine( "public bool CheckEntitySetup( {0} entitySetup ) {{".FormatWith( entitySetup.GeneralData.ClassName ) );
				writer.WriteLine( "if( entitySetup != this.entitySetup ) return false;" );
				writer.WriteLine( "entitySetupMatched = true;" );
				writer.WriteLine( "return true;" );
				writer.WriteLine( "}" );

				foreach( var i in entitySetup.RequiredParameters )
					writer.WriteLine( "public {0} Get{1}() => entitySetupEncoder.Value.Get{1}();".FormatWith( i.TypeName, i.PropertyName ) );
				foreach( var i in entitySetup.OptionalParameters ) {
					writer.WriteLine( "public bool {0}IsPresent => entitySetupEncoder.Value.{0}IsPresent;".FormatWith( i.PropertyName ) );
					writer.WriteLine( "public {0} Get{1}() => entitySetupEncoder.Value.Get{1}();".FormatWith( i.TypeName, i.PropertyName ) );
				}
			}

			foreach( var i in requiredParameters ) {
				writer.WriteLine( "public {0} Get{1}() {{".FormatWith( i.TypeName, i.PropertyName ) );
				writer.WriteLine( "{0}Accessed = true;".FormatWith( i.Name ) );
				writer.WriteLine( "return {0};".FormatWith( i.Name ) );
				writer.WriteLine( "}" );
			}
			foreach( var i in optionalParameters ) {
				writer.WriteLine( "public bool {0}IsPresent => {1} != null;".FormatWith( i.PropertyName, i.Name ) );

				writer.WriteLine( "public {0} Get{1}() {{".FormatWith( i.TypeName, i.PropertyName ) );
				writer.WriteLine( "if( !{0}IsPresent ) throw new ApplicationException( \"The parameter is not present.\" );".FormatWith( i.PropertyName ) );
				writer.WriteLine( "{0}Accessed = true;".FormatWith( i.Name ) );
				writer.WriteLine( "return {0};".FormatWith( getOptionalParameterValueExpression( i ) ) );
				writer.WriteLine( "}" );
			}
			if( includeVersionString ) {
				writer.WriteLine( "public string GetVersionString() {" );
				writer.WriteLine( "versionStringAccessed = true;" );
				writer.WriteLine( "return versionString;" );
				writer.WriteLine( "}" );
			}

			writer.WriteLine( "IReadOnlyCollection<( string, string, bool )> EnterpriseWebFramework.UrlEncoder.GetRemainingParameters() {" );
			writer.WriteLine(
				"var parameters = new List<( string, string, bool )>( {0} );".FormatWith(
					( entitySetup?.RequiredParameters.Concat( entitySetup.OptionalParameters ).Count() ?? 0 ) +
					requiredParameters.Concat( optionalParameters ).Count() ) );
			if( entitySetup != null )
				writer.WriteLine(
					"if( !entitySetupMatched ) parameters.AddRange( ( (EnterpriseWebFramework.UrlEncoder)entitySetupEncoder.Value ).GetRemainingParameters() );" );
			foreach( var i in requiredParameters )
				writer.WriteLine( "if( !{0}Accessed ) parameters.Add( ( \"{0}\", {1}, false ) );".FormatWith( i.Name, i.GetUrlSerializationExpression( i.Name ) ) );
			foreach( var i in optionalParameters )
				writer.WriteLine(
					"if( {0}IsPresent && !{1}Accessed ) parameters.Add( ( \"{1}\", {2}, false ) );".FormatWith(
						i.PropertyName,
						i.Name,
						i.GetUrlSerializationExpression( getOptionalParameterValueExpression( i ) ) ) );
			if( includeVersionString )
				writer.WriteLine( "if( versionString.Any() && !versionStringAccessed ) parameters.Add( ( \"version\", versionString, false ) );" );
			writer.WriteLine( "return parameters;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "}" );
		}

		private static string getOptionalParameterType( VariableSpecification p ) =>
			p.IsString || p.IsEnumerable ? p.TypeName : "SpecifiedValue<{0}>".FormatWith( p.TypeName );

		private static string getOptionalParameterValueExpression( VariableSpecification p ) =>
			p.IsString || p.IsEnumerable ? p.Name : "{0}.Value".FormatWith( p.Name );
	}
}