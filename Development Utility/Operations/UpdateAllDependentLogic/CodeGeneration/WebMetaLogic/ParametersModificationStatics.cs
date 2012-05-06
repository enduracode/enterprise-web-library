using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class ParametersModificationStatics {
		internal static void WriteClassIfNecessary( TextWriter writer, IEnumerable<VariableSpecification> parameters ) {
			if( !parameters.Any() )
				return;

			writer.WriteLine( "internal class ParametersModification: ParametersModificationBase {" );
			foreach( var parameter in parameters ) {
				if( parameter.IsString || parameter.IsEnumerable ) {
					writer.WriteLine( "private " + parameter.TypeName + " " + parameter.Name + ";" );
					writePropertyDocComment( writer, parameter );
					writer.WriteLine( "internal " + parameter.TypeName + " " + parameter.PropertyName + " {" );
					writer.WriteLine( "get { return " + parameter.Name + "; }" );

					// setter
					writer.WriteLine( "set {" );
					writer.WriteLine( "if( value == null )" );
					writer.WriteLine( "throw new ApplicationException( \"You cannot specify null for the value of a string or an IEnumerable.\" );" );
					writer.WriteLine( parameter.Name + " = value;" );
					writer.WriteLine( "}" );

					writer.WriteLine( "}" );
				}
				else {
					writePropertyDocComment( writer, parameter );
					writer.WriteLine( "internal " + parameter.TypeName + " " + parameter.PropertyName + " { get; set; }" );
				}

				FormItemStatics.WriteFormItemGetters( writer, parameter.GetModificationField() );
			}
			writer.WriteLine( "}" );
		}

		private static void writePropertyDocComment( TextWriter writer, VariableSpecification parameter ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Gets or sets the new value for the " + parameter.Name + " parameter." +
			                                            ( parameter.IsString || parameter.IsEnumerable ? " The value cannot be null." : "" ) );
		}
	}
}