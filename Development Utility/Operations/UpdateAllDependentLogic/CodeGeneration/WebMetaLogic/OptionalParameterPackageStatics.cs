using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class OptionalParameterPackageStatics {
		internal static void WriteClassIfNecessary( TextWriter writer, IEnumerable<VariableSpecification> parameters ) {
			if( !parameters.Any() )
				return;

			// Class should be public so apps that reference this web app can create Info objects.
			writer.WriteLine( "public class OptionalParameterPackage {" );

			foreach( var parameter in parameters ) {
				writer.WriteLine( "private readonly InitializationAwareValue<" + parameter.TypeName + "> " + parameter.Name + " = new InitializationAwareValue<" +
				                  parameter.TypeName + ">();" );

				var warning = "";
				var setCheck = "";
				if( parameter.IsString || parameter.IsEnumerable ) {
					warning = " The value cannot be null.";
					setCheck = "if( value == null ) throw new ApplicationException( \"You cannot specify null for the value of a string or an IEnumerable.\" );";
				}

				// Uninitialized parameters are meaningless since their values will be replaced with current page values or defaults when the Info object is created.
				CodeGenerationStatics.AddSummaryDocComment( writer,
				                                            "Gets or sets the value for the " + parameter.Name +
				                                            " optional parameter. Throws an exception if you try to get the value before it has been set." + warning );

				writer.WriteLine( "public " + parameter.TypeName + " " + parameter.PropertyName + " { get { return " + parameter.Name + ".Value; } set { " + setCheck +
				                  parameter.Name + ".Value = value; } }" );
				writer.WriteLine( "public bool " + GetWasSpecifiedPropertyName( parameter ) + " { get { return " + parameter.Name + ".Initialized; } }" );
			}
			writer.WriteLine( "}" );
		}

		public static string GetWasSpecifiedPropertyName( VariableSpecification parameter ) {
			return parameter.PropertyName + "WasSpecified";
		}
	}
}