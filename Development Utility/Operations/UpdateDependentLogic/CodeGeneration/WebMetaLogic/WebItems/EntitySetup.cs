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
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, requiredParameters, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			InfoStatics.WriteSpecifyParameterDefaultsMethod( writer, optionalParameters );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "internal ParametersModification parametersModification;" );
			InfoStatics.WriteConstructorAndHelperMethods( writer, generalData, requiredParameters, optionalParameters, false, true );
			writer.WriteLine(
				"public override ParametersModificationBase ParametersModificationAsBaseType => {0};".FormatWith(
					requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) );
			writer.WriteLine( "protected override UrlEncoder getUrlEncoder() => null;" );
			WebMetaLogicStatics.WriteReCreateFromNewParameterValuesMethod(
				writer,
				requiredParameters,
				"internal {0} ".FormatWith( generalData.ClassName ),
				generalData.ClassName,
				"" );
			writeEqualsMethod( writer );
			InfoStatics.WriteGetHashCodeMethod( writer, generalData.PathRelativeToProject, requiredParameters, optionalParameters );
			writer.WriteLine( "}" );
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