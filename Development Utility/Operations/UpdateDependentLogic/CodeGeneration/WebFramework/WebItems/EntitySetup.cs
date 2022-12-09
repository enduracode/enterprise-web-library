namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems {
	internal class EntitySetup {
		private readonly bool projectContainsFramework;
		private readonly WebItemGeneralData generalData;

		internal EntitySetup( bool projectContainsFramework, WebItemGeneralData generalData ) {
			this.projectContainsFramework = projectContainsFramework;
			this.generalData = generalData;
		}

		internal WebItemGeneralData GeneralData => generalData;
		internal IReadOnlyCollection<WebItemParameter> RequiredParameters => generalData.RequiredParameters;
		internal IReadOnlyCollection<WebItemParameter> OptionalParameters => generalData.OptionalParameters;

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace " + generalData.Namespace + " {" );
			writer.WriteLine( "public sealed partial class {0}: EntitySetupBase {{".FormatWith( generalData.ClassName ) );
			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, generalData.RequiredParameters, generalData.OptionalParameters );
			ParametersModificationStatics.WriteClassIfNecessary( writer, generalData.RequiredParameters.Concat( generalData.OptionalParameters ) );
			UrlStatics.GenerateUrlClasses( writer, generalData.ClassName, null, generalData.RequiredParameters, generalData.OptionalParameters, false );
			if( generalData.OptionalParameters.Any() )
				InfoStatics.WriteSpecifyParameterDefaultsMethod( writer, false );
			InfoStatics.WriteParameterMembers( writer, generalData.RequiredParameters, generalData.OptionalParameters );
			if( generalData.RequiredParameters.Any() || generalData.OptionalParameters.Any() )
				writer.WriteLine( "internal ParametersModification parametersModification;" );
			if( generalData.OptionalParameters.Any() )
				writer.WriteLine( "private Action<OptionalParameterSpecifier, Parameters> optionalParameterSetter;" );
			InfoStatics.WriteConstructorAndHelperMethods( writer, generalData, generalData.RequiredParameters, generalData.OptionalParameters, false, true );
			if( generalData.RequiredParameters.Any() || generalData.OptionalParameters.Any() ) {
				writer.WriteLine( "{0} override void InitParametersModification() {{".FormatWith( projectContainsFramework ? "protected internal" : "protected" ) );
				writer.WriteLine( "parametersModification = new ParametersModification();" );
				foreach( var i in generalData.RequiredParameters.Concat( generalData.OptionalParameters ) )
					writer.WriteLine( "parametersModification.{0} = {0};".FormatWith( i.PropertyName ) );
				writer.WriteLine( "}" );
			}
			else
				writer.WriteLine( "{0} override void InitParametersModification() {{}}".FormatWith( projectContainsFramework ? "protected internal" : "protected" ) );
			UrlStatics.GenerateGetEncoderMethod( writer, "", generalData.RequiredParameters, generalData.OptionalParameters, _ => "true", false );
			writer.WriteLine(
				"internal {0} ReCreate() => new {0}({1});".FormatWith(
					generalData.ClassName,
					StringTools.ConcatenateWithDelimiter(
							", ",
							InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( generalData.RequiredParameters, parameter => parameter.PropertyName ),
							generalData.OptionalParameters.Any() ? "optionalParameterSetter: optionalParameterSetter" : "" )
						.Surround( " ", " " ) ) );
			WebFrameworkStatics.WriteReCreateFromNewParameterValuesMethod(
				writer,
				generalData.RequiredParameters,
				"internal {0} ".FormatWith( generalData.ClassName ),
				generalData.ClassName,
				"" );
			writeEqualsMethod( writer );
			InfoStatics.WriteGetHashCodeMethod( writer, generalData.PathRelativeToProject, generalData.RequiredParameters, generalData.OptionalParameters );
			writer.WriteLine( "}" );
			writer.WriteLine( "}" );
		}

		private void writeEqualsMethod( TextWriter writer ) {
			writer.WriteLine( "public override bool Equals( BasicUrlHandler other ) {" );
			writer.WriteLine( "if( !( other is {0} otherEs ) ) return false;".FormatWith( generalData.ClassName ) );
			InfoStatics.WriteEqualsParameterComparisons( writer, generalData.RequiredParameters, generalData.OptionalParameters, "otherEs" );
			writer.WriteLine( "}" );
		}
	}
}