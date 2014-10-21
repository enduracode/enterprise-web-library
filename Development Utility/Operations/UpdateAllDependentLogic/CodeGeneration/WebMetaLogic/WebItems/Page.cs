using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;

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
			if( entitySetup != null )
				writer.WriteLine( "private EntitySetup es;" );
			writer.WriteLine( "private Info info;" );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "private ParametersModification parametersModification;" );
			writer.WriteLine( "public override EntitySetupBase EsAsBaseType { get { return " + ( entitySetup != null ? "es" : "null" ) + "; } }" );
			writer.WriteLine( "public override PageInfo InfoAsBaseType { get { return info; } }" );
			writer.WriteLine(
				"public override ParametersModificationBase ParametersModificationAsBaseType { get { return " +
				( requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) + "; } }" );
			writeInitEntitySetupMethod( writer );
			WebMetaLogicStatics.WriteCreateInfoFromQueryStringMethod(
				writer,
				requiredParameters,
				optionalParameters,
				"protected override void ",
				entitySetup != null ? "es.info" : "" );
			writeGetInfoMethod( writer );
			generalData.ReadPageStateVariablesFromCodeAndWriteTypedPageStateMethods( writer );
			WebMetaLogicStatics.WriteCreateInfoFromNewParameterValuesMethod(
				writer,
				requiredParameters,
				optionalParameters,
				"protected override PageInfo ",
				entitySetup != null ? "es.CreateInfoFromNewParameterValues()" : "" );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeInfoClass( TextWriter writer ) {
			writer.WriteLine( "public sealed partial class Info: PageInfo {" );
			if( entitySetup != null )
				writer.WriteLine( "private EntitySetup.Info esInfo;" );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			InfoStatics.WriteConstructorAndHelperMethods( writer, requiredParameters, optionalParameters, entitySetup != null, false );
			writer.WriteLine( "public override EntitySetupInfo EsInfoAsBaseType { get { return " + ( entitySetup != null ? "esInfo" : "null" ) + "; } }" );
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
				var prefix = ( requiredParameters.Contains( parameter ) || optionalParameters.Contains( parameter ) ) ? "" : "esInfo.";
				var parameterReference = prefix + parameter.PropertyName;
				var defaultParameterReference = prefix + InfoStatics.DefaultOptionalParameterPackageName + "." + parameter.PropertyName;
				var defaultParameterWasSpecifiedReference = prefix + InfoStatics.DefaultOptionalParameterPackageName + "." +
				                                            OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( parameter );
				if( optionalParameters.Contains( parameter ) || ( entitySetup != null && entitySetup.OptionalParameters.Contains( parameter ) ) ) {
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
				}
				writer.WriteLine(
					"url += \"" + parameter.PropertyName + "=\" + HttpUtility.UrlEncode( " + parameter.GetUrlSerializationExpression( parameterReference ) + " ) + '&';" );
			}

			writer.WriteLine( "return url.Remove( url.Length - 1 );" );
			writer.WriteLine( "}" );
		}

		private void writeInfoIsIdenticalToMethod( TextWriter writer ) {
			writer.WriteLine( "protected override bool isIdenticalTo( PageInfo infoAsBaseType ) {" );
			writer.WriteLine( "if( !( infoAsBaseType is Info ) )" );
			writer.WriteLine( "return false;" );
			writer.WriteLine( "var info = infoAsBaseType as Info;" );
			if( entitySetup != null ) {
				writer.WriteLine( "if( !esInfo.IsIdenticalTo( info.esInfo ) )" );
				writer.WriteLine( "return false;" );
			}
			InfoStatics.WriteIsIdenticalToParameterComparisons( writer, requiredParameters, optionalParameters );
			writer.WriteLine( "}" );
		}

		private void writeInfoCloneAndReplaceDefaultsIfPossibleMethod( TextWriter writer ) {
			writer.WriteLine( "protected override PageInfo CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {" );
			if( optionalParameters.Any() ) {
				writer.WriteLine( "var parametersModification = Instance.ParametersModificationAsBaseType as ParametersModification;" );
				writer.WriteLine( "if( parametersModification != null && !disableReplacementOfDefaults )" );
				writer.WriteLine(
					"return new Info( " +
					StringTools.ConcatenateWithDelimiter(
						", ",
						entitySetup != null ? "esInfo.CloneAndReplaceDefaultsIfPossible( disableReplacementOfDefaults )" : "",
						InfoStatics.GetInfoConstructorArguments(
							requiredParameters,
							optionalParameters,
							parameter => parameter.FieldName,
							parameter => InfoStatics.GetWasSpecifiedFieldName( parameter ) + " ? " + parameter.FieldName + " : parametersModification." + parameter.PropertyName ),
						"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			}
			writer.WriteLine(
				"return new Info( " +
				StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetup != null ? "esInfo.CloneAndReplaceDefaultsIfPossible( disableReplacementOfDefaults )" : "",
					InfoStatics.GetInfoConstructorArguments( requiredParameters, optionalParameters, parameter => parameter.FieldName, parameter => parameter.FieldName ),
					"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			writer.WriteLine( "}" );
		}

		private void writeInitEntitySetupMethod( TextWriter writer ) {
			writer.WriteLine( "protected override void initEntitySetup() {" );
			if( entitySetup != null ) {
				var name = entitySetup.GeneralData.ClassName;
				if( entitySetup.GeneralData.UrlRelativeToProject.Length > 0 )
					writer.WriteLine( "es = (" + name + ")LoadControl( \"" + NetTools.CombineUrls( "~", entitySetup.GeneralData.UrlRelativeToProject ) + "\" );" );
				else
					writer.WriteLine( "es = new " + name + "();" );
			}
			writer.WriteLine( "}" );
		}

		private void writeGetInfoMethod( TextWriter writer ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Creates an info object for this page. Use the Info class constructor instead of this method if you want to reuse the entity setup info object." );
			writer.WriteLine(
				"public static Info GetInfo( " +
				StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetup != null ? WebMetaLogicStatics.GetParameterDeclarations( entitySetup.RequiredParameters ) : "",
					WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
					entitySetup != null && entitySetup.OptionalParameters.Count > 0 ? "EntitySetup.OptionalParameterPackage entitySetupOptionalParameterPackage = null" : "",
					optionalParameters.Count > 0 ? "OptionalParameterPackage optionalParameterPackage = null" : "",
					"string uriFragmentIdentifier = \"\"" ) + " ) {" );
			var entitySetupArgs = entitySetup != null
				                      ? "new EntitySetup.Info( " +
				                        StringTools.ConcatenateWithDelimiter(
					                        ", ",
					                        InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( entitySetup.RequiredParameters, parameter => parameter.Name ),
					                        entitySetup.OptionalParameters.Count > 0 ? "optionalParameterPackage: entitySetupOptionalParameterPackage" : "" ) + " )"
				                      : "";
			writer.WriteLine(
				"return new Info( " +
				StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetupArgs,
					InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, parameter => parameter.Name ),
					optionalParameters.Count > 0 ? "optionalParameterPackage: optionalParameterPackage" : "",
					"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}