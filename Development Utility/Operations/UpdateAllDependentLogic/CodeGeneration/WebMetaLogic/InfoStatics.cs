using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebMetaLogic {
	internal static class InfoStatics {
		public const string DefaultOptionalParameterPackageName = "_defaultOptionalParameterPackage";

		internal static void WriteParameterMembers( TextWriter writer, List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters ) {
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
				writer.WriteLine( "private " + parameter.TypeName + " " + parameter.FieldName +
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

		internal static void WriteConstructorAndHelperMethods( TextWriter writer, List<VariableSpecification> requiredParameters,
		                                                       List<VariableSpecification> optionalParameters, bool includeEsInfoParameter, bool isEsInfo ) {
			writeConstructorDocComments( writer, requiredParameters );
			var constructorAndInitialParameterArguments = "( " +
			                                              StringTools.ConcatenateWithDelimiter( ", ",
			                                                                                    includeEsInfoParameter ? "EntitySetup.Info esInfo" : "",
			                                                                                    WebMetaLogicStatics.GetParameterDeclarations( requiredParameters ),
			                                                                                    optionalParameters.Count > 0
				                                                                                    ? "OptionalParameterPackage optionalParameterPackage = null"
				                                                                                    : "",
			                                                                                    !isEsInfo ? "string uriFragmentIdentifier = \"\"" : "" ) + " ) {";
			writer.WriteLine( "internal Info" + constructorAndInitialParameterArguments );

			// Initialize required parameter fields. We want to create and call this method even if there are no parameters so that non-generated Info constructors can still
			// call it and remain resistant to changes.
			writer.WriteLine( "initializeParameters( " +
			                  StringTools.ConcatenateWithDelimiter( ", ",
			                                                        includeEsInfoParameter ? "esInfo" : "",
			                                                        GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, p => p.Name ),
			                                                        optionalParameters.Count > 0 ? "optionalParameterPackage: optionalParameterPackage" : "",
			                                                        !isEsInfo ? "uriFragmentIdentifier: uriFragmentIdentifier" : "" ) + " );" );

			// Call init.
			writer.WriteLine( "init();" );

			writer.WriteLine( "}" );

			// Declare partial helper methods that will be called by the constructor.
			writeInitParametersMethod( writer, requiredParameters, optionalParameters, includeEsInfoParameter, isEsInfo, constructorAndInitialParameterArguments );
			if( optionalParameters.Any() ) {
				CodeGenerationStatics.AddSummaryDocComment( writer,
				                                            "Initializes an optional parameter package with request time default values. This method is always called during construction of the object." );
				writer.WriteLine( "partial void initDefaultOptionalParameterPackage( OptionalParameterPackage package );" );
				CodeGenerationStatics.AddSummaryDocComment( writer,
				                                            "Initializes an optional parameter package with non request time default values. This method is called during construction of the object unless the object is being created from the URL to directly handle the current request." );
				writer.WriteLine( "partial void initUserDefaultOptionalParameterPackage( OptionalParameterPackage package );" );
			}
		}

		private static void writeConstructorDocComments( TextWriter writer, List<VariableSpecification> requiredParameters ) {
			foreach( var parameter in requiredParameters ) {
				var warning = "";
				if( parameter.IsString || parameter.IsEnumerable )
					warning = " Do not pass null.";
				CodeGenerationStatics.AddParamDocComment( writer, parameter.Name, parameter.Comment + warning );
			}
		}

		private static void writeInitParametersMethod( TextWriter writer, List<VariableSpecification> requiredParameters,
		                                               List<VariableSpecification> optionalParameters, bool includeEsInfoParameter, bool isEsInfo, string arguments ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Initializes required and optional parameters. A call to this should be the first line of every non-generated Info constructor." );
			writer.WriteLine( "private void initializeParameters" + arguments );

			if( includeEsInfoParameter )
				writer.WriteLine( "this.esInfo = esInfo;" );
			foreach( var requiredParameter in requiredParameters ) {
				if( requiredParameter.IsString || requiredParameter.IsEnumerable ) {
					writer.WriteLine( "if( " + requiredParameter.Name +
					                  " == null ) throw new ApplicationException( \"You cannot specify null for the value of a string or an IEnumerable.\" );" );
				}
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
				if( isEsInfo ) {
					writer.WriteLine(
						"var currentInfo = EwfPage.Instance != null && EwfPage.Instance.EsAsBaseType != null ? EwfPage.Instance.EsAsBaseType.InfoAsBaseType as Info : null;" );
				}
				else
					writer.WriteLine( "var currentInfo = Instance != null ? Instance.InfoAsBaseType as Info : null;" );
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
					writer.WriteLine( "if( !" + GetWasSpecifiedFieldName( optionalParameter ) + " && " + DefaultOptionalParameterPackageName + "." +
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
					writer.WriteLine( "if( !" + GetWasSpecifiedFieldName( optionalParameter ) + " && userDefaultOptionalParameterPackage." +
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

			if( !isEsInfo )
				writer.WriteLine( "base.uriFragmentIdentifier = uriFragmentIdentifier;" );
			writer.WriteLine( "}" ); // initializeParameters method
		}

		internal static string GetInfoConstructorArguments( List<VariableSpecification> requiredParameters, List<VariableSpecification> optionalParameters,
		                                                    Func<VariableSpecification, string> requiredParameterToArgMapper,
		                                                    Func<VariableSpecification, string> optionalParameterToArgMapper ) {
			var optionalParameterAssignments = "";
			foreach( var optionalParameter in optionalParameters ) {
				optionalParameterAssignments = StringTools.ConcatenateWithDelimiter( ", ",
				                                                                     optionalParameterAssignments,
				                                                                     optionalParameter.PropertyName + " = " +
				                                                                     optionalParameterToArgMapper( optionalParameter ) );
			}
			if( optionalParameterAssignments.Length > 0 )
				optionalParameterAssignments = "optionalParameterPackage: new OptionalParameterPackage { " + optionalParameterAssignments + " }";

			return StringTools.ConcatenateWithDelimiter( ", ",
			                                             GetInfoConstructorArgumentsForRequiredParameters( requiredParameters, requiredParameterToArgMapper ),
			                                             optionalParameterAssignments );
		}

		internal static string GetInfoConstructorArgumentsForRequiredParameters( List<VariableSpecification> requiredParameters,
		                                                                         Func<VariableSpecification, string> requiredParameterToArgMapper ) {
			var text = "";
			foreach( var requiredParameter in requiredParameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, requiredParameterToArgMapper( requiredParameter ) );
			return text;
		}

		internal static void WriteIsIdenticalToParameterComparisons( TextWriter writer, List<VariableSpecification> requiredParameters,
		                                                             List<VariableSpecification> optionalParameters ) {
			foreach( var parameter in requiredParameters.Concat( optionalParameters ) ) {
				writer.WriteLine( "if( " + parameter.PropertyName + " != info." + parameter.PropertyName + " )" );
				writer.WriteLine( "return false;" );
			}
			writer.WriteLine( "return true;" );
		}
	}
}