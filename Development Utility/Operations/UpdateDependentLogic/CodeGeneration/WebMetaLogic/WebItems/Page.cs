using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class Page {
		private readonly WebItemGeneralData generalData;
		private readonly EntitySetup entitySetup;
		private readonly List<VariableSpecification> requiredParameters;
		private readonly List<VariableSpecification> optionalParameters;

		internal Page( WebItemGeneralData generalData, EntitySetup entitySetup ) {
			this.generalData = generalData;
			this.entitySetup = entitySetup;
			requiredParameters = generalData.ReadParametersFromCode( false );
			optionalParameters = generalData.ReadParametersFromCode( true );

			// NOTE: Blow up if there is a name collision between parameters and entitySetup.Parameters.
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace {0} {{".FormatWith( generalData.Namespace ) );
			writer.WriteLine( "public sealed partial class {0}: PageBase {{".FormatWith( generalData.ClassName ) );

			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, requiredParameters, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			writeGetInfoMethod( writer );
			InfoStatics.WriteSpecifyParameterDefaultsMethod( writer, optionalParameters );
			if( entitySetup != null )
				writer.WriteLine( "public EntitySetup Es;" );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "private ParametersModification parametersModification;" );
			InfoStatics.WriteConstructorAndHelperMethods( writer, generalData, requiredParameters, optionalParameters, entitySetup != null, false );
			writer.WriteLine( "public override EntitySetupBase EsAsBaseType => {0};".FormatWith( entitySetup != null ? "Es" : "null" ) );
			writer.WriteLine(
				"public override ParametersModificationBase ParametersModificationAsBaseType => {0};".FormatWith(
					requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) );
			writer.WriteLine( "protected override UrlEncoder getUrlEncoder() => null;" );
			writer.WriteLine(
				"protected internal override ResourceBase ReCreate() => new {0}( {1} );".FormatWith(
					generalData.ClassName,
					StringTools.ConcatenateWithDelimiter(
						", ",
						entitySetup != null
							? "new {0}({1})".FormatWith(
								entitySetup.GeneralData.ClassName,
								InfoStatics.GetInfoConstructorArgumentsForRequiredParameters(
										entitySetup.RequiredParameters,
										parameter => "Es.{0}".FormatWith( parameter.PropertyName ) )
									.Surround( " ", " " ) )
							: "",
						InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, parameter => parameter.PropertyName ),
						"uriFragmentIdentifier: uriFragmentIdentifier" ) ) );
			WebMetaLogicStatics.WriteReCreateFromNewParameterValuesMethod(
				writer,
				requiredParameters,
				"protected override PageBase ",
				generalData.ClassName,
				entitySetup != null ? "Es.ReCreateFromNewParameterValues()" : "" );
			writeEqualsMethod( writer );
			InfoStatics.WriteGetHashCodeMethod( writer, generalData.PathRelativeToProject, requiredParameters, optionalParameters );

			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeEqualsMethod( TextWriter writer ) {
			writer.WriteLine( "public override bool Equals( BasicUrlHandler other ) {" );
			writer.WriteLine( "if( !( other is {0} otherPage ) ) return false;".FormatWith( generalData.ClassName ) );
			if( entitySetup != null )
				writer.WriteLine( "if( !EwlStatics.AreEqual( otherPage.Es, Es ) ) return false;" );
			InfoStatics.WriteEqualsParameterComparisons( writer, requiredParameters, optionalParameters, "otherPage" );
			writer.WriteLine( "}" );
		}

		private void writeGetInfoMethod( TextWriter writer ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Creates an object for this page. Use the constructor instead of this method if you want to reuse the entity setup object." );
			writer.WriteLine(
				"public static {0} GetInfo( ".FormatWith( generalData.ClassName ) + StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetup != null ? WebMetaLogicStatics.GetParameterDeclarations( entitySetup.RequiredParameters ) : "",
					WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
					entitySetup != null && entitySetup.OptionalParameters.Count > 0
						? "Action<EntitySetup.OptionalParameterSpecifier, EntitySetup.Parameters> entitySetupOptionalParameterSetter = null"
						: "",
					optionalParameters.Count > 0 ? "Action<OptionalParameterSpecifier, Parameters> optionalParameterSetter = null" : "",
					"string uriFragmentIdentifier = \"\"" ) + " ) {" );
			var entitySetupArgs = entitySetup != null
				                      ? "new EntitySetup( " + StringTools.ConcatenateWithDelimiter(
					                        ", ",
					                        InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( entitySetup.RequiredParameters, parameter => parameter.Name ),
					                        entitySetup.OptionalParameters.Count > 0 ? "optionalParameterSetter: entitySetupOptionalParameterSetter" : "" ) + " )"
				                      : "";
			writer.WriteLine(
				"return new {0}( ".FormatWith( generalData.ClassName ) + StringTools.ConcatenateWithDelimiter(
					", ",
					entitySetupArgs,
					InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, parameter => parameter.Name ),
					optionalParameters.Count > 0 ? "optionalParameterSetter: optionalParameterSetter" : "",
					"uriFragmentIdentifier: uriFragmentIdentifier" ) + " );" );
			writer.WriteLine( "}" );
		}
	}
}