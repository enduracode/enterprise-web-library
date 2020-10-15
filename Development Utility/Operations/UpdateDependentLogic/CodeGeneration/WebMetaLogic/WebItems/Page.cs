using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class Page {
		private readonly WebItemGeneralData generalData;
		private readonly List<VariableSpecification> requiredParameters;
		private readonly List<VariableSpecification> optionalParameters;
		private readonly EntitySetup entitySetup;

		internal Page( WebItemGeneralData generalData, EntitySetup entitySetup ) {
			this.generalData = generalData;
			requiredParameters = generalData.ReadParametersFromCode( false );
			optionalParameters = generalData.ReadParametersFromCode( true );
			this.entitySetup = entitySetup;

			// NOTE: Blow up if there is a name collision between parameters and entitySetup.Parameters.
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public partial class " + generalData.ClassName + " {" );

			writeInfoClass( writer );
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			writer.WriteLine( "private Info info;" );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "private ParametersModification parametersModification;" );
			if( entitySetup != null )
				writer.WriteLine( "public EntitySetup Es { get { return info.Es; } }" );
			writer.WriteLine( "public override EntitySetupBase EsAsBaseType { get { return info.EsAsBaseType; } }" );
			writer.WriteLine( "public override PageInfo InfoAsBaseType { get { return info; } }" );
			writer.WriteLine(
				"public override ParametersModificationBase ParametersModificationAsBaseType { get { return " +
				( requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) + "; } }" );
			WebMetaLogicStatics.WriteCreateInfoFromQueryStringMethod( writer, entitySetup, requiredParameters, optionalParameters );
			writer.WriteLine( "protected override void reCreateInfo() {" );
			writer.WriteLine( "var infoLocal = info;" );
			writer.WriteLine( "info = null;" );
			writer.WriteLine( "info = (Info)infoLocal.CloneAndReplaceDefaultsIfPossible( true );" );
			writer.WriteLine( "}" );
			writeGetInfoMethod( writer );
			generalData.ReadPageStateVariablesFromCodeAndWriteTypedPageStateMethods( writer );
			WebMetaLogicStatics.WriteReCreateFromNewParameterValuesMethod(
				writer,
				requiredParameters,
				optionalParameters,
				"protected override PageInfo ",
				"Info",
				entitySetup != null ? "Es.ReCreateFromNewParameterValues()" : "" );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeInfoClass( TextWriter writer ) {
			writer.WriteLine( "public sealed partial class Info: PageInfo {" );
			if( entitySetup != null )
				writer.WriteLine( "public EntitySetup Es;" );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			InfoStatics.WriteConstructorAndHelperMethods( writer, requiredParameters, optionalParameters, entitySetup != null, false );
			writer.WriteLine( "public override EntitySetupBase EsAsBaseType { get { return " + ( entitySetup != null ? "Es" : "null" ) + "; } }" );
			writeInfoBuildUrlMethod( writer );
			writeInfoIsIdenticalToMethod( writer );
			writeInfoCloneAndReplaceDefaultsIfPossibleMethod( writer );
			writer.WriteLine( "}" );
		}

		private void writeInfoBuildUrlMethod( TextWriter writer ) {
			writer.WriteLine( "protected override string buildUrl() {" );

			var parameters = new List<VariableSpecification>();
			if( entitySetup != null )
				parameters.AddRange( entitySetup.RequiredParameters.Concat( entitySetup.OptionalParameters ) );
			parameters.AddRange( requiredParameters.Concat( optionalParameters ) );

			writer.WriteLine( ( parameters.Any() ? "var" : "const string" ) + " url = \"~/" + generalData.UrlRelativeToProject + "?\";" );

			foreach( var parameter in parameters ) {
				var prefix = requiredParameters.Contains( parameter ) || optionalParameters.Contains( parameter ) ? "" : "Es.";
				var parameterReference = prefix + parameter.PropertyName;
				var defaultParameterReference = prefix + InfoStatics.DefaultOptionalParameterPackageName + "." + parameter.PropertyName;
				var defaultParameterWasSpecifiedReference = prefix + InfoStatics.DefaultOptionalParameterPackageName + "." +
				                                            OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( parameter );
				if( optionalParameters.Contains( parameter ) || ( entitySetup != null && entitySetup.OptionalParameters.Contains( parameter ) ) )
					// If a default was specified for the parameter and the default matches the value of our parameter, don't include it.
					// If a default was not specified and the value of our parameter is the default value of the type, don't include it.
					writer.WriteLine(
						"if( !( (" + defaultParameterWasSpecifiedReference + " && " +
						( parameter.IsEnumerable
							  ? defaultParameterReference + ".SequenceEqual( " + parameterReference + " )"
							  : defaultParameterReference + " == " + parameterReference ) + " ) || ( !" + defaultParameterWasSpecifiedReference + " && " +
						( parameter.IsEnumerable
							  ? "!" + parameterReference + ".Any()"
							  : parameterReference + " == " + ( parameter.IsString ? "\"\"" : "default(" + parameter.TypeName + ")" ) ) + " ) ) )" );
				writer.WriteLine(
					"url += \"" + parameter.PropertyName + "=\" + HttpUtility.UrlEncode( " + parameter.GetUrlSerializationExpression( parameterReference ) +
					" ) + '&';" );
			}

			writer.WriteLine( "return url.Remove( url.Length - 1 );" );
			writer.WriteLine( "}" );
		}

		private void writeInfoIsIdenticalToMethod( TextWriter writer ) {
			writer.WriteLine( "protected override bool isIdenticalTo( ResourceInfo infoAsBaseType ) {" );
			writer.WriteLine( "if( !( infoAsBaseType is Info ) )" );
			writer.WriteLine( "return false;" );
			writer.WriteLine( "var info = infoAsBaseType as Info;" );
			if( entitySetup != null ) {
				writer.WriteLine( "if( !Es.IsIdenticalTo( info.Es ) )" );
				writer.WriteLine( "return false;" );
			}
			InfoStatics.WriteIsIdenticalToParameterComparisons( writer, requiredParameters, optionalParameters );
			writer.WriteLine( "}" );
		}

		private void writeInfoCloneAndReplaceDefaultsIfPossibleMethod( TextWriter writer ) {
			writer.WriteLine( "public override ResourceInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {" );
			if( optionalParameters.Any() ) {
				writer.WriteLine( "var parametersModification = Instance.ParametersModificationAsBaseType as ParametersModification;" );
				writer.WriteLine( "if( parametersModification != null && !disableReplacementOfDefaults )" );
				writer.WriteLine(
					"return new Info( " + StringTools.ConcatenateWithDelimiter(
						", ",
						entitySetup != null ? "Es.CloneAndReplaceDefaultsIfPossible( disableReplacementOfDefaults )" : "",
						InfoStatics.GetInfoConstructorArguments(
							requiredParameters,
							optionalParameters,
							parameter => parameter.FieldName,
							parameter => InfoStatics.GetWasSpecifiedFieldName( parameter ) + " ? " + parameter.FieldName + " : parametersModification." +
							             parameter.PropertyName ),
						"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			}
			writer.WriteLine(
				"return new Info( " + StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetup != null ? "Es.CloneAndReplaceDefaultsIfPossible( disableReplacementOfDefaults )" : "",
					InfoStatics.GetInfoConstructorArguments( requiredParameters, optionalParameters, parameter => parameter.FieldName, parameter => parameter.FieldName ),
					"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			writer.WriteLine( "}" );
		}

		private void writeGetInfoMethod( TextWriter writer ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Creates an info object for this page. Use the Info class constructor instead of this method if you want to reuse the entity setup info object." );
			writer.WriteLine(
				"public static Info GetInfo( " + StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetup != null ? WebMetaLogicStatics.GetParameterDeclarations( entitySetup.RequiredParameters ) : "",
					WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
					entitySetup != null && entitySetup.OptionalParameters.Count > 0
						? "EntitySetup.OptionalParameterPackage entitySetupOptionalParameterPackage = null"
						: "",
					optionalParameters.Count > 0 ? "OptionalParameterPackage optionalParameterPackage = null" : "",
					"string uriFragmentIdentifier = \"\"" ) + " ) {" );
			var entitySetupArgs = entitySetup != null
				                      ? "new EntitySetup( " + StringTools.ConcatenateWithDelimiter(
					                        ", ",
					                        InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( entitySetup.RequiredParameters, parameter => parameter.Name ),
					                        entitySetup.OptionalParameters.Count > 0 ? "optionalParameterPackage: entitySetupOptionalParameterPackage" : "" ) + " )"
				                      : "";
			writer.WriteLine(
				"return new Info( " + StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetupArgs,
					InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, parameter => parameter.Name ),
					optionalParameters.Count > 0 ? "optionalParameterPackage: optionalParameterPackage" : "",
					"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}