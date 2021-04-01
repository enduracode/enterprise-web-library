using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic.WebItems {
	internal class EntitySetup {
		private readonly WebItemGeneralData generalData;
		private readonly List<VariableSpecification> requiredParameters;
		private readonly List<VariableSpecification> optionalParameters;

		internal EntitySetup( WebItemGeneralData generalData ) {
			this.generalData = generalData;
			requiredParameters = generalData.ReadParametersFromCode( false );
			optionalParameters = generalData.ReadParametersFromCode( true );
		}

		internal WebItemGeneralData GeneralData { get { return generalData; } }

		internal List<VariableSpecification> RequiredParameters { get { return requiredParameters; } }

		internal List<VariableSpecification> OptionalParameters { get { return optionalParameters; } }

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public sealed partial class {0}: EntitySetupBase {{".FormatWith( generalData.ClassName ) );
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "internal ParametersModification parametersModification;" );
			InfoStatics.WriteConstructorAndHelperMethods( writer, requiredParameters, optionalParameters, false, true );
			writer.WriteLine( "protected override UrlEncoder getUrlEncoder() => null;" );
			writer.WriteLine(
				"public override ParametersModificationBase ParametersModificationAsBaseType { get { return " +
				( requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) + "; } }" );
			WebMetaLogicStatics.WriteReCreateFromNewParameterValuesMethod(
				writer,
				requiredParameters,
				optionalParameters,
				"internal {0} ".FormatWith( generalData.ClassName ),
				generalData.ClassName,
				"" );
			writeCloneAndReplaceDefaultsIfPossibleMethod( writer );
			writeEqualsMethod( writer );
			InfoStatics.WriteGetHashCodeMethod( writer, generalData.PathRelativeToProject, requiredParameters, optionalParameters );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeCloneAndReplaceDefaultsIfPossibleMethod( TextWriter writer ) {
			writer.WriteLine( "internal {0} CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {{".FormatWith( generalData.ClassName ) );
			if( optionalParameters.Any() ) {
				writer.WriteLine(
					"var parametersModification = ( EwfPage.Instance.InfoAsBaseType != null ? EwfPage.Instance.EsAsBaseType?.ParametersModificationAsBaseType : null ) as ParametersModification;" );
				writer.WriteLine( "if( parametersModification != null && !disableReplacementOfDefaults )" );
				writer.WriteLine(
					"return new {0}( {1} );".FormatWith(
						generalData.ClassName,
						InfoStatics.GetInfoConstructorArguments(
							requiredParameters,
							optionalParameters,
							parameter => parameter.FieldName,
							parameter => InfoStatics.GetWasSpecifiedFieldName( parameter ) + " ? " + parameter.FieldName + " : parametersModification." +
							             parameter.PropertyName ) ) );
			}
			writer.WriteLine(
				"return new {0}( {1} );".FormatWith(
					generalData.ClassName,
					InfoStatics.GetInfoConstructorArguments(
						requiredParameters,
						optionalParameters,
						parameter => parameter.FieldName,
						parameter => parameter.FieldName ) ) );
			writer.WriteLine( "}" );
		}

		private void writeEqualsMethod( TextWriter writer ) {
			writer.WriteLine( "public override bool Equals( BasicUrlHandler other ) {" );
			writer.WriteLine( "if( !( other is {0} otherEs ) ) return false;".FormatWith( generalData.ClassName ) );
			InfoStatics.WriteEqualsParameterComparisons( writer, requiredParameters, optionalParameters, "otherEs" );
			writer.WriteLine( "}" );
		}
	}
}