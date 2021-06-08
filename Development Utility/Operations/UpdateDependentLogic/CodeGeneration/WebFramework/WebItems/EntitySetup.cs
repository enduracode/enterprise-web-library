using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems {
	internal class EntitySetup {
		private readonly bool projectContainsFramework;
		private readonly WebItemGeneralData generalData;
		private readonly List<WebItemParameter> requiredParameters;
		private readonly List<WebItemParameter> optionalParameters;

		internal EntitySetup( bool projectContainsFramework, WebItemGeneralData generalData ) {
			this.projectContainsFramework = projectContainsFramework;
			this.generalData = generalData;
			requiredParameters = generalData.ReadParametersFromCode( false );
			optionalParameters = generalData.ReadParametersFromCode( true );
		}

		internal WebItemGeneralData GeneralData { get { return generalData; } }

		internal List<WebItemParameter> RequiredParameters { get { return requiredParameters; } }

		internal List<WebItemParameter> OptionalParameters { get { return optionalParameters; } }

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public sealed partial class {0}: EntitySetupBase {{".FormatWith( generalData.ClassName ) );
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, requiredParameters, optionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			UrlStatics.GenerateUrlClasses( writer, generalData.ClassName, null, requiredParameters, optionalParameters, false );
			InfoStatics.WriteSpecifyParameterDefaultsMethod( writer, optionalParameters );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			if( requiredParameters.Any() || optionalParameters.Any() )
				writer.WriteLine( "internal ParametersModification parametersModification;" );
			InfoStatics.WriteConstructorAndHelperMethods( writer, generalData, requiredParameters, optionalParameters, false, true );
			if( requiredParameters.Any() || optionalParameters.Any() ) {
				writer.WriteLine( "{0} override void InitParametersModification() {{".FormatWith( projectContainsFramework ? "protected internal" : "protected" ) );
				writer.WriteLine( "parametersModification = new ParametersModification();" );
				foreach( var i in requiredParameters.Concat( optionalParameters ) )
					writer.WriteLine( "parametersModification.{0} = {0};".FormatWith( i.PropertyName ) );
				writer.WriteLine( "}" );
			}
			else
				writer.WriteLine( "{0} override void InitParametersModification() {{}}".FormatWith( projectContainsFramework ? "protected internal" : "protected" ) );
			UrlStatics.GenerateGetEncoderMethod( writer, "", requiredParameters, optionalParameters, p => "true", false );
			WebFrameworkStatics.WriteReCreateFromNewParameterValuesMethod(
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