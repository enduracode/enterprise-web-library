using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems {
	internal class Resource {
		private readonly bool projectContainsFramework;
		private readonly WebItemGeneralData generalData;
		private readonly EntitySetup entitySetup;
		private readonly List<WebItemParameter> requiredParameters;
		private readonly List<WebItemParameter> optionalParameters;

		internal Resource( bool projectContainsFramework, WebItemGeneralData generalData, EntitySetup entitySetup ) {
			this.projectContainsFramework = projectContainsFramework;
			this.generalData = generalData;
			this.entitySetup = entitySetup;
			requiredParameters = generalData.ReadParametersFromCode( false );
			optionalParameters = generalData.ReadParametersFromCode( true );

			// NOTE: Blow up if there is a name collision between parameters and entitySetup.Parameters.
		}

		internal void GenerateCode( TextWriter writer ) {
			writer.WriteLine( "namespace {0} {{".FormatWith( generalData.Namespace ) );
			writer.WriteLine(
				"public sealed partial class {0}: {1} {{".FormatWith(
					generalData.ClassName,
					generalData.IsPage() ? "PageBase" : generalData.IsAutoCompleteService() ? "AutoCompleteService" : "ResourceBase" ) );

			OptionalParameterPackageStatics.WriteClassIfNecessary( writer, requiredParameters, optionalParameters );
			if( generalData.IsPage() )
				ParametersModificationStatics.WriteClassIfNecessary( writer, requiredParameters.Concat( optionalParameters ) );
			UrlStatics.GenerateUrlClasses( writer, generalData.ClassName, entitySetup, requiredParameters, optionalParameters, false );
			if( optionalParameters.Any() )
				generateSegmentParameterSpecifier( writer );
			writeGetInfoMethod( writer );
			if( optionalParameters.Any() )
				InfoStatics.WriteSpecifyParameterDefaultsMethod( writer, entitySetup != null );
			if( entitySetup != null )
				writer.WriteLine( "public EntitySetup Es;" );
			InfoStatics.WriteParameterMembers( writer, requiredParameters, optionalParameters );
			if( generalData.IsPage() && ( requiredParameters.Any() || optionalParameters.Any() ) )
				writer.WriteLine( "private ParametersModification parametersModification;" );
			if( optionalParameters.Any() ) {
				writer.WriteLine( "private readonly Lazy<SegmentParameterSpecifier> segmentParameterSpecifier;" );
				writer.WriteLine(
					"private Action<{0}> optionalParameterSetter;".FormatWith(
						StringTools.ConcatenateWithDelimiter( ", ", "OptionalParameterSpecifier", entitySetup != null ? "EntitySetup" : "", "Parameters" ) ) );
			}
			InfoStatics.WriteConstructorAndHelperMethods( writer, generalData, requiredParameters, optionalParameters, entitySetup != null, false );
			writer.WriteLine( "public override EntitySetupBase EsAsBaseType => {0};".FormatWith( entitySetup != null ? "Es" : "null" ) );
			if( generalData.IsPage() ) {
				if( requiredParameters.Any() || optionalParameters.Any() ) {
					writer.WriteLine( "protected override void initParametersModification() {" );
					writer.WriteLine( "parametersModification = new ParametersModification();" );
					foreach( var i in requiredParameters.Concat( optionalParameters ) )
						writer.WriteLine( "parametersModification.{0} = {0};".FormatWith( i.PropertyName ) );
					writer.WriteLine( "}" );
				}
				else
					writer.WriteLine( "protected override void initParametersModification() {}" );
			}
			UrlStatics.GenerateGetEncoderMethod(
				writer,
				entitySetup != null ? "Es" : "",
				requiredParameters,
				optionalParameters,
				p => "segmentParameterSpecifier.Value.{0}IsSegmentParameter".FormatWith( p.PropertyName ),
				false );
			if( optionalParameters.Any() )
				writer.WriteLine( "partial void specifySegmentParameters( SegmentParameterSpecifier specifier );" );
			if( !generalData.IsPage() )
				writer.WriteLine( "public override bool MatchesCurrent() => base.MatchesCurrent();" );
			writer.WriteLine(
				"{0} override ResourceBase ReCreate() => new {1}( {2} );".FormatWith(
					projectContainsFramework ? "protected internal" : "protected",
					generalData.ClassName,
					StringTools.ConcatenateWithDelimiter(
						", ",
						entitySetup != null ? "Es.ReCreate()" : "",
						InfoStatics.GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, parameter => parameter.PropertyName ),
						optionalParameters.Any() ? "optionalParameterSetter: optionalParameterSetter" : "",
						"uriFragmentIdentifier: uriFragmentIdentifier" ) ) );
			if( generalData.IsPage() )
				WebFrameworkStatics.WriteReCreateFromNewParameterValuesMethod(
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

		private void generateSegmentParameterSpecifier( TextWriter writer ) {
			writer.WriteLine( "private class SegmentParameterSpecifier {" );
			foreach( var i in optionalParameters )
				writer.WriteLine( "public bool {0}IsSegmentParameter {{ get; set; }}".FormatWith( i.PropertyName ) );
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
					entitySetup != null ? WebFrameworkStatics.GetParameterDeclarations( entitySetup.RequiredParameters ) : "",
					WebFrameworkStatics.GetParameterDeclarations( requiredParameters ),
					entitySetup != null && entitySetup.OptionalParameters.Count > 0
						? "Action<EntitySetup.OptionalParameterSpecifier, EntitySetup.Parameters> entitySetupOptionalParameterSetter = null"
						: "",
					optionalParameters.Count > 0
						? "Action<{0}> optionalParameterSetter = null".FormatWith(
							StringTools.ConcatenateWithDelimiter( ", ", "OptionalParameterSpecifier", entitySetup != null ? "EntitySetup" : "", "Parameters" ) )
						: "",
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