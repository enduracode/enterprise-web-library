using System.Collections.Generic;
using System.IO;
using System.Linq;

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
			writer.WriteLine( "public partial class " + generalData.ClassName + " {" );
			writeInfoClass( writer );
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			writer.WriteLine( "internal Info info { get; private set; }" );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "internal ParametersModification parametersModification;" );
			writer.WriteLine( "EntitySetupInfo EntitySetupBase.InfoAsBaseType { get { return info; } }" );
			writer.WriteLine(
				"ParametersModificationBase EntitySetupBase.ParametersModificationAsBaseType { get { return " +
				( requiredParameters.Any() || optionalParameters.Any() ? "parametersModification" : "null" ) + "; } }" );
			WebMetaLogicStatics.WriteCreateInfoFromQueryStringMethod( writer, requiredParameters, optionalParameters, "void EntitySetupBase.", "" );
			WebMetaLogicStatics.WriteCreateInfoFromNewParameterValuesMethod( writer, requiredParameters, optionalParameters, "internal Info ", "" );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeInfoClass( TextWriter writer ) {
			writer.WriteLine(
				"public sealed partial class Info: " + ( generalData.PathRelativeToProject.EndsWith( ".ascx" ) ? "EwfUiEntitySetupInfo" : "EntitySetupInfo" ) + " {" );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			InfoStatics.WriteConstructorAndHelperMethods( writer, requiredParameters, optionalParameters, false, true );
			writeInfoIsIdenticalToMethod( writer );
			writeInfoCloneAndReplaceDefaultsIfPossibleMethod( writer );
			writer.WriteLine( "}" );
		}

		private void writeInfoIsIdenticalToMethod( TextWriter writer ) {
			writer.WriteLine( "internal bool IsIdenticalTo( Info info ) {" );
			InfoStatics.WriteIsIdenticalToParameterComparisons( writer, requiredParameters, optionalParameters );
			writer.WriteLine( "}" );
		}

		private void writeInfoCloneAndReplaceDefaultsIfPossibleMethod( TextWriter writer ) {
			writer.WriteLine( "internal Info CloneAndReplaceDefaultsIfPossible( bool disableReplacementOfDefaults ) {" );
			if( optionalParameters.Any() ) {
				writer.WriteLine(
					"var parametersModification = ( EwfPage.Instance.EsAsBaseType != null ? EwfPage.Instance.EsAsBaseType.ParametersModificationAsBaseType : null ) as ParametersModification;" );
				writer.WriteLine( "if( parametersModification != null && !disableReplacementOfDefaults )" );
				writer.WriteLine(
					"return new Info( " +
					InfoStatics.GetInfoConstructorArguments(
						requiredParameters,
						optionalParameters,
						parameter => parameter.FieldName,
						parameter => InfoStatics.GetWasSpecifiedFieldName( parameter ) + " ? " + parameter.FieldName + " : parametersModification." + parameter.PropertyName ) +
					" );" );
			}
			writer.WriteLine(
				"return new Info( " +
				InfoStatics.GetInfoConstructorArguments( requiredParameters, optionalParameters, parameter => parameter.FieldName, parameter => parameter.FieldName ) +
				" );" );
			writer.WriteLine( "}" );
		}
	}
}