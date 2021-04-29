using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class InfoStatics {
		private const string parameterDefaultsFieldName = "__parameterDefaults";

		internal static void WriteSpecifyParameterDefaultsMethod( TextWriter writer, IReadOnlyCollection<VariableSpecification> optionalParameters ) {
			if( !optionalParameters.Any() )
				return;

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Specifies optional parameter default values. This method is always called during construction of an object." );
			writer.WriteLine( "static partial void specifyParameterDefaults( OptionalParameterSpecifier specifier, Parameters parameters );" );
		}

		internal static void WriteParameterMembers(
			TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters ) {
			writeMembersForParameterList( writer, requiredParameters );
			writeMembersForParameterList( writer, optionalParameters );
			if( optionalParameters.Any() ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine( "internal OptionalParameterSpecifier {0} = new OptionalParameterSpecifier();".FormatWith( parameterDefaultsFieldName ) );
			}
		}

		private static void writeMembersForParameterList( TextWriter writer, List<VariableSpecification> parameters ) {
			foreach( var parameter in parameters ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine(
					"private " + parameter.TypeName + " " + parameter.FieldName +
					( parameter.IsString ? " = \"\"" : parameter.IsEnumerable ? " = " + parameter.EnumerableInitExpression : "" ) + ";" );
				CodeGenerationStatics.AddSummaryDocComment( writer, parameter.Comment );
				writer.WriteLine( "public " + parameter.TypeName + " " + parameter.PropertyName + " { get { return " + parameter.FieldName + "; } }" );
			}
		}

		internal static void WriteConstructorAndHelperMethods(
			TextWriter writer, WebItemGeneralData generalData, IReadOnlyCollection<VariableSpecification> requiredParameters,
			IReadOnlyCollection<VariableSpecification> optionalParameters, bool includeEsParameter, bool isEs ) {
			// It's important to force the cache to be enabled in the constructor since these objects are often created in post-back-action getters.

			writeConstructorDocComments( writer, requiredParameters );
			var constructorParameters = "( " + StringTools.ConcatenateWithDelimiter(
				                            ", ",
				                            includeEsParameter ? "EntitySetup es" : "",
				                            WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
				                            optionalParameters.Count > 0 ? "Action<OptionalParameterSpecifier, Parameters> optionalParameterSetter = null" : "",
				                            !isEs ? "string uriFragmentIdentifier = \"\"" : "" ) + " ) {";
			writer.WriteLine( "internal {0}".FormatWith( generalData.ClassName ) + constructorParameters );
			writer.WriteLine( "DataAccessState.Current.ExecuteWithCache( () => {" );

			// Initialize parameter fields. We want to create and call this method even if there are no parameters so that non-generated constructors can still call
			// it and remain resistant to changes.
			writer.WriteLine(
				"initParameters( " + StringTools.ConcatenateWithDelimiter(
					", ",
					includeEsParameter ? "es" : "",
					GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, p => p.Name ),
					optionalParameters.Count > 0 ? "optionalParameterSetter: optionalParameterSetter" : "",
					!isEs ? "uriFragmentIdentifier: uriFragmentIdentifier" : "" ) + " );" );

			// Call init.
			writer.WriteLine( "init();" );

			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );

			writeInitParametersMethod( writer, generalData, requiredParameters, optionalParameters, includeEsParameter, isEs, constructorParameters );
		}

		private static void writeConstructorDocComments( TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters ) {
			foreach( var parameter in requiredParameters ) {
				var warning = "";
				if( parameter.IsString || parameter.IsEnumerable )
					warning = " Do not pass null.";
				var description = parameter.Comment + warning;
				if( description.Length == 0 )
					continue;
				CodeGenerationStatics.AddParamDocComment( writer, parameter.Name, description );
			}
		}

		private static void writeInitParametersMethod(
			TextWriter writer, WebItemGeneralData generalData, IReadOnlyCollection<VariableSpecification> requiredParameters,
			IReadOnlyCollection<VariableSpecification> optionalParameters, bool includeEsParameter, bool isEs, string constructorParameters ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Initializes required and optional parameters. A call to this should be the first line of every non-generated constructor." );
			writer.WriteLine( "private void initParameters" + constructorParameters );

			if( includeEsParameter )
				writer.WriteLine( "Es = es;" );
			foreach( var requiredParameter in requiredParameters ) {
				if( requiredParameter.IsString || requiredParameter.IsEnumerable )
					writer.WriteLine(
						"if( " + requiredParameter.Name +
						" == null ) throw new ApplicationException( \"You cannot specify null for the value of a string or an IEnumerable.\" );" );
				writer.WriteLine( requiredParameter.FieldName + " = " + requiredParameter.Name + ";" );
			}

			// Initialize optional parameter fields.
			if( optionalParameters.Any() ) {
				writer.WriteLine( "var optionalParametersInitializedFromCurrent = false;" );
				writer.WriteLine( "if( EwfApp.Instance != null && AppRequestState.Instance != null ) {" );

				// If the list of current URL handlers has a matching object, apply its parameter values.
				writer.WriteLine( "foreach( var urlHandler in AppRequestState.Instance.UrlHandlers ) {" );
				writer.WriteLine( "if( !( urlHandler is {0} match ) ) continue;".FormatWith( generalData.ClassName ) );
				foreach( var i in requiredParameters )
					writer.WriteLine( "if( !EwlStatics.AreEqual( match.{0}, {0} ) ) continue;".FormatWith( i.PropertyName ) );
				foreach( var i in optionalParameters )
					writer.WriteLine( "{0} = match.{1};".FormatWith( i.FieldName, i.PropertyName ) );
				writer.WriteLine( "optionalParametersInitializedFromCurrent = true;" );
				writer.WriteLine( "break;" );
				writer.WriteLine( "}" );

				// If new parameter values are effective, and the current resource or an ancestor matches this object, apply its new parameter values.
				if( generalData.IsPage() || isEs ) {
					writer.WriteLine( "if( AppRequestState.Instance.NewUrlParameterValuesEffective ) {" );
					writer.WriteLine( "UrlHandler urlHandler = {0}Current ?? ResourceBase.Current;".FormatWith( generalData.IsPage() ? "" : "PageBase." ) );
					writer.WriteLine( "do {" );
					writer.WriteLine( "if( !( urlHandler is {0} match ) ) continue;".FormatWith( generalData.ClassName ) );
					foreach( var i in requiredParameters )
						writer.WriteLine( "if( !EwlStatics.AreEqual( match.{0}, {0} ) ) continue;".FormatWith( i.PropertyName ) );
					foreach( var i in optionalParameters )
						writer.WriteLine( "{0} = match.parametersModification.{1};".FormatWith( i.FieldName, i.PropertyName ) );
					writer.WriteLine( "optionalParametersInitializedFromCurrent = true;" );
					writer.WriteLine( "break;" );
					writer.WriteLine( "}" );
					writer.WriteLine( "while( ( urlHandler = urlHandler.GetParent() ) != null );" );
					writer.WriteLine( "}" );
				}

				writer.WriteLine( "}" );

				// Apply parameter values from the setter.
				writer.WriteLine( "var optionalParameterSpecifier = new OptionalParameterSpecifier();" );
				writer.WriteLine(
					"optionalParameterSetter?.Invoke( optionalParameterSpecifier, new Parameters( {0} ) );".FormatWith(
						StringTools.ConcatenateWithDelimiter(
							", ",
							requiredParameters.Select( i => i.PropertyName )
								.Append(
									"optionalParametersInitializedFromCurrent ? new OptionalParameters( {0} ) : null".FormatWith(
										StringTools.ConcatenateWithDelimiter( ", ", optionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) );
				foreach( var i in optionalParameters )
					writer.WriteLine(
						"if( optionalParameterSpecifier.{0} ) {1} = optionalParameterSpecifier.{2};".FormatWith(
							OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( i ),
							i.FieldName,
							i.PropertyName ) );

				// This is called after all current values and values from the setter have been incorporated since these can affect default values.
				writer.WriteLine(
					"specifyParameterDefaults( {0}, new Parameters( {1} ) );".FormatWith(
						parameterDefaultsFieldName,
						StringTools.ConcatenateWithDelimiter(
							", ",
							requiredParameters.Select( i => i.PropertyName )
								.Append(
									"new OptionalParameters( {0} )".FormatWith(
										StringTools.ConcatenateWithDelimiter( ", ", optionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) );

				// Apply default values to parameters not yet initialized.
				writer.WriteLine( "if( !optionalParametersInitializedFromCurrent ) {" );
				foreach( var i in optionalParameters )
					writer.WriteLine(
						"if( !optionalParameterSpecifier.{0} && {1}.{0} ) {2} = {1}.{3};".FormatWith(
							OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( i ),
							parameterDefaultsFieldName,
							i.FieldName,
							i.PropertyName ) );
				writer.WriteLine( "}" );
			}

			if( !isEs )
				writer.WriteLine( "base.uriFragmentIdentifier = uriFragmentIdentifier;" );

			if( isEs )
				if( requiredParameters.Any() || optionalParameters.Any() ) {
					writer.WriteLine( "parametersModification = new ParametersModification();" );
					foreach( var parameter in requiredParameters.Concat( optionalParameters ) )
						writer.WriteLine( "parametersModification.{0} = {0};".FormatWith( parameter.PropertyName ) );
				}

			writer.WriteLine( "}" ); // initParameters method
		}

		internal static string GetInfoConstructorArgumentsForRequiredParameters(
			IReadOnlyCollection<VariableSpecification> requiredParameters, Func<VariableSpecification, string> requiredParameterToArgMapper ) {
			var text = "";
			foreach( var requiredParameter in requiredParameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, requiredParameterToArgMapper( requiredParameter ) );
			return text;
		}

		internal static void WriteEqualsParameterComparisons(
			TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters, string otherObjectName ) {
			foreach( var parameter in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine( "if( !EwlStatics.AreEqual( {0}.{1}, {1} ) ) return false;".FormatWith( otherObjectName, parameter.PropertyName ) );
			writer.WriteLine( "return true;" );
		}

		internal static void WriteGetHashCodeMethod(
			TextWriter writer, string pathRelativeToProject, IReadOnlyCollection<VariableSpecification> requiredParameters,
			IReadOnlyCollection<VariableSpecification> optionalParameters ) {
			writer.WriteLine(
				"public override int GetHashCode() => ( {0} ).GetHashCode();".FormatWith(
					"@\"{0}\"".FormatWith( pathRelativeToProject ) + StringTools
						.ConcatenateWithDelimiter( ", ", requiredParameters.Concat( optionalParameters ).Select( i => i.PropertyName ) )
						.PrependDelimiter( ", " ) ) );
		}
	}
}