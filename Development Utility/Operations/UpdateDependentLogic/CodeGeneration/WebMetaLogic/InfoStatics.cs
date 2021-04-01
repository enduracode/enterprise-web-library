using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Tewl.Tools;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class InfoStatics {
		public const string DefaultOptionalParameterPackageName = "_defaultOptionalParameterPackage";

		internal static void WriteParameterMembers(
			TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters ) {
			writeMembersForParameterList( writer, requiredParameters, false );
			writeMembersForParameterList( writer, optionalParameters, true );
			if( optionalParameters.Any() ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine( "internal OptionalParameterPackage " + DefaultOptionalParameterPackageName + " = new OptionalParameterPackage();" );
			}
		}

		private static void writeMembersForParameterList( TextWriter writer, List<VariableSpecification> parameters, bool writeWasSetFlags ) {
			foreach( var parameter in parameters ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine(
					"private " + parameter.TypeName + " " + parameter.FieldName +
					( parameter.IsString ? " = \"\"" : parameter.IsEnumerable ? " = " + parameter.EnumerableInitExpression : "" ) + ";" );
				if( writeWasSetFlags ) {
					CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
					writer.WriteLine( "private bool " + GetWasSpecifiedFieldName( parameter ) + ";" );
				}
				CodeGenerationStatics.AddSummaryDocComment( writer, parameter.Comment );
				writer.WriteLine( "public " + parameter.TypeName + " " + parameter.PropertyName + " { get { return " + parameter.FieldName + "; } }" );
			}
		}

		public static string GetWasSpecifiedFieldName( VariableSpecification parameter ) {
			return parameter.FieldName + "WasSpecified";
		}

		internal static void WriteConstructorAndHelperMethods(
			TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters, IReadOnlyCollection<VariableSpecification> optionalParameters,
			bool includeEsParameter, bool isEs ) {
			// It's important to force the cache to be enabled in the constructor since info objects are often created in post-back-action getters.

			writeConstructorDocComments( writer, requiredParameters );
			var constructorParameters = "( " + StringTools.ConcatenateWithDelimiter(
				                            ", ",
				                            includeEsParameter ? "EntitySetup es" : "",
				                            WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
				                            optionalParameters.Count > 0 ? "OptionalParameterPackage optionalParameterPackage = null" : "",
				                            !isEs ? "string uriFragmentIdentifier = \"\"" : "" ) + " ) {";
			writer.WriteLine( "internal {0}".FormatWith( isEs ? "EntitySetup" : "Info" ) + constructorParameters );
			writer.WriteLine( "DataAccessState.Current.ExecuteWithCache( () => {" );

			// Initialize parameter fields. We want to create and call this method even if there are no parameters so that non-generated constructors can still call
			// it and remain resistant to changes.
			writer.WriteLine(
				"initParameters( " + StringTools.ConcatenateWithDelimiter(
					", ",
					includeEsParameter ? "es" : "",
					GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, p => p.Name ),
					optionalParameters.Count > 0 ? "optionalParameterPackage: optionalParameterPackage" : "",
					!isEs ? "uriFragmentIdentifier: uriFragmentIdentifier" : "" ) + " );" );

			// Call init.
			writer.WriteLine( "init();" );

			writer.WriteLine( "} );" );
			writer.WriteLine( "}" );

			// Declare partial helper methods that will be called by the constructor.
			writeInitParametersMethod( writer, requiredParameters, optionalParameters, includeEsParameter, isEs, constructorParameters );
			if( optionalParameters.Any() ) {
				CodeGenerationStatics.AddSummaryDocComment(
					writer,
					"Initializes an optional parameter package with request time default values. This method is always called during construction of the object." );
				writer.WriteLine( "partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package );" );
				CodeGenerationStatics.AddSummaryDocComment(
					writer,
					"Initializes an optional parameter package with non request time default values. This method is called during construction of the object unless the object is being created from the URL to directly handle the current request." );
				writer.WriteLine( "partial void initUserDefaultOptionalParameterPackage( OptionalParameterPackage package );" );
			}
		}

		private static void writeConstructorDocComments( TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters ) {
			foreach( var parameter in requiredParameters ) {
				var warning = "";
				if( parameter.IsString || parameter.IsEnumerable )
					warning = " Do not pass null.";
				CodeGenerationStatics.AddParamDocComment( writer, parameter.Name, parameter.Comment + warning );
			}
		}

		private static void writeInitParametersMethod(
			TextWriter writer, IReadOnlyCollection<VariableSpecification> requiredParameters, IReadOnlyCollection<VariableSpecification> optionalParameters,
			bool includeEsParameter, bool isEs, string constructorParameters ) {
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
				writer.WriteLine( "optionalParameterPackage = optionalParameterPackage ?? new OptionalParameterPackage();" );

				// Initialize fields whose values have been specified.
				foreach( var optionalParameter in optionalParameters ) {
					writer.WriteLine( "if( optionalParameterPackage." + OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( optionalParameter ) + " ) {" );
					writer.WriteLine( optionalParameter.FieldName + " = optionalParameterPackage." + optionalParameter.PropertyName + ";" );
					writer.WriteLine( GetWasSpecifiedFieldName( optionalParameter ) + " = true;" );
					writer.WriteLine( "}" );
				}

				// If the current info object is the same type, use it to initialize fields whose values have not been specified.
				if( isEs )
					writer.WriteLine( "var currentInfo = EwfPage.Instance?.InfoAsBaseType != null ? EwfPage.Instance.EsAsBaseType as EntitySetup : null;" );
				else
					writer.WriteLine( "var currentInfo = Instance?.InfoAsBaseType as Info;" );
				writer.WriteLine( "if( currentInfo != null ) {" );
				foreach( var optionalParameter in optionalParameters ) {
					writer.WriteLine( "if( !" + GetWasSpecifiedFieldName( optionalParameter ) + " )" );
					writer.WriteLine( optionalParameter.FieldName + " = currentInfo." + optionalParameter.FieldName + ";" );
				}
				writer.WriteLine( "}" );

				// This is called after all specified values and all current info object values have been incorporated since these can affect default values.
				writer.WriteLine( "initDefaultOptionalParameterPackage( " + DefaultOptionalParameterPackageName + " );" );


				// If the current info object is *not* the same type, use default values to initialize fields whose values have not been specified.

				writer.WriteLine( "if( currentInfo == null ) {" );
				foreach( var optionalParameter in optionalParameters ) {
					writer.WriteLine(
						"if( !" + GetWasSpecifiedFieldName( optionalParameter ) + " && " + DefaultOptionalParameterPackageName + "." +
						OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( optionalParameter ) + " )" );
					writer.WriteLine( optionalParameter.FieldName + " = " + DefaultOptionalParameterPackageName + "." + optionalParameter.PropertyName + ";" );
				}

				// Overwrite base default values with user defaults except when this info object is being created for the current request. It's important that the
				// fields contain base default values before initUserDefaultOptionalParameterPackage is called in case the implementation of the method wants to use
				// base defaults when determining user defaults.
				writer.WriteLine( "if( EwfPage.Instance == null || EwfPage.Instance.InfoAsBaseType != null ) {" );
				writer.WriteLine( "var userDefaultOptionalParameterPackage = new OptionalParameterPackage();" );
				writer.WriteLine( "initUserDefaultOptionalParameterPackage( userDefaultOptionalParameterPackage );" );
				foreach( var optionalParameter in optionalParameters ) {
					writer.WriteLine(
						"if( !" + GetWasSpecifiedFieldName( optionalParameter ) + " && userDefaultOptionalParameterPackage." +
						OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( optionalParameter ) + " )" );
					writer.WriteLine( optionalParameter.FieldName + " = userDefaultOptionalParameterPackage." + optionalParameter.PropertyName + ";" );
				}
				writer.WriteLine( "}" );

				writer.WriteLine( "}" );

				// Clear WasSpecified fields when this info object is being created for the current request. This will cause CloneAndReplaceDefaultsIfPossible to behave
				// the same on this object as it would on a new copy of the object created during the request with no optional parameter values specified.
				writer.WriteLine( "if( EwfPage.Instance != null && EwfPage.Instance.InfoAsBaseType == null ) {" );
				foreach( var optionalParameter in optionalParameters )
					writer.WriteLine( GetWasSpecifiedFieldName( optionalParameter ) + " = false;" );
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

		internal static string GetInfoConstructorArguments(
			IReadOnlyCollection<VariableSpecification> requiredParameters, IReadOnlyCollection<VariableSpecification> optionalParameters,
			Func<VariableSpecification, string> requiredParameterToArgMapper, Func<VariableSpecification, string> optionalParameterToArgMapper ) {
			var optionalParameterAssignments = "";
			foreach( var optionalParameter in optionalParameters )
				optionalParameterAssignments = StringTools.ConcatenateWithDelimiter(
					", ",
					optionalParameterAssignments,
					optionalParameter.PropertyName + " = " + optionalParameterToArgMapper( optionalParameter ) );
			if( optionalParameterAssignments.Length > 0 )
				optionalParameterAssignments = "optionalParameterPackage: new OptionalParameterPackage { " + optionalParameterAssignments + " }";

			return StringTools.ConcatenateWithDelimiter(
				", ",
				GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, requiredParameterToArgMapper ),
				optionalParameterAssignments );
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