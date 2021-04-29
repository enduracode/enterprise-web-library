using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class OptionalParameterPackageStatics {
		internal static void WriteClassIfNecessary(
			TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters, IReadOnlyCollection<VariableSpecification> optionalParameters ) {
			if( !optionalParameters.Any() )
				return;

			writer.WriteLine( "public class Parameters {" );
			foreach( var i in requiredParameters )
				writer.WriteLine( "public readonly {0} {1};".FormatWith( i.TypeName, i.PropertyName ) );
			writer.WriteLine( "public readonly OptionalParameters OptionalParameters;" );
			writer.WriteLine(
				"public Parameters( {0}, OptionalParameters optionalParameters ) {{".FormatWith(
					StringTools.ConcatenateWithDelimiter( ", ", requiredParameters.Select( i => i.TypeName + " " + i.Name ) ) ) );
			foreach( var i in requiredParameters )
				writer.WriteLine( "{0} = {1};".FormatWith( i.PropertyName, i.Name ) );
			writer.WriteLine( "OptionalParameters = optionalParameters;" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );

			writer.WriteLine( "public class OptionalParameters {" );
			foreach( var i in optionalParameters )
				writer.WriteLine( "public readonly {0} {1};".FormatWith( i.TypeName, i.PropertyName ) );
			writer.WriteLine(
				"public OptionalParameters( {0} ) {{".FormatWith(
					StringTools.ConcatenateWithDelimiter( ", ", optionalParameters.Select( i => i.TypeName + " " + i.Name ) ) ) );
			foreach( var i in optionalParameters )
				writer.WriteLine( "{0} = {1};".FormatWith( i.PropertyName, i.Name ) );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );

			// Class should be public so apps that reference this web app can create Info objects.
			writer.WriteLine( "public class OptionalParameterSpecifier {" );

			foreach( var parameter in optionalParameters ) {
				writer.WriteLine(
					"private readonly InitializationAwareValue<" + parameter.TypeName + "> " + parameter.Name + " = new InitializationAwareValue<" + parameter.TypeName +
					">();" );

				var warning = "";
				var setCheck = "";
				if( parameter.IsString || parameter.IsEnumerable ) {
					warning = " The value cannot be null.";
					setCheck = "if( value == null ) throw new ApplicationException( \"You cannot specify null for the value of a string or an IEnumerable.\" );";
				}

				// Uninitialized parameters are meaningless since their values will be replaced with current page values or defaults when the Info object is created.
				CodeGenerationStatics.AddSummaryDocComment(
					writer,
					"Gets or sets the value for the " + parameter.Name + " optional parameter. Throws an exception if you try to get the value before it has been set." +
					warning );

				writer.WriteLine(
					"public " + parameter.TypeName + " " + parameter.PropertyName + " { get { return " + parameter.Name + ".Value; } set { " + setCheck + parameter.Name +
					".Value = value; } }" );
				writer.WriteLine( "public bool " + GetWasSpecifiedPropertyName( parameter ) + " { get { return " + parameter.Name + ".Initialized; } }" );
			}
			writer.WriteLine( "}" );
		}

		public static string GetWasSpecifiedPropertyName( VariableSpecification parameter ) {
			return parameter.PropertyName + "WasSpecified";
		}
	}
}