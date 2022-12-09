namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	internal static class InfoStatics {
		internal static void WriteSpecifyParameterDefaultsMethod( TextWriter writer, bool includeEsParameter ) {
			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Specifies optional parameter default values. This method is always called during construction of an object." );
			writer.WriteLine(
				"static partial void specifyParameterDefaults( {0} );".FormatWith(
					StringTools.ConcatenateWithDelimiter(
						", ",
						"OptionalParameterSpecifier specifier",
						includeEsParameter ? "EntitySetup entitySetup" : "",
						"Parameters parameters" ) ) );
		}

		internal static void WriteParameterMembers(
			TextWriter writer, IReadOnlyCollection<WebItemParameter> requiredParameters, IReadOnlyCollection<WebItemParameter> optionalParameters ) {
			writeMembersForParameterList( writer, requiredParameters );
			writeMembersForParameterList( writer, optionalParameters );
			if( optionalParameters.Any() ) {
				CodeGenerationStatics.AddGeneratedCodeUseOnlyComment( writer );
				writer.WriteLine(
					"internal OptionalParameterSpecifier {0} = new OptionalParameterSpecifier();".FormatWith( WebItemGeneralData.ParameterDefaultsFieldName ) );
			}
		}

		private static void writeMembersForParameterList( TextWriter writer, IReadOnlyCollection<WebItemParameter> parameters ) {
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
			TextWriter writer, WebItemGeneralData generalData, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeEsParameter, bool isEs ) {
			// It's important to force the cache to be enabled in the constructor since these objects are often created in post-back-action getters.

			writeConstructorDocComments( writer, requiredParameters );
			var constructorParameters = "( " + StringTools.ConcatenateWithDelimiter(
				                            ", ",
				                            includeEsParameter ? "EntitySetup es" : "",
				                            WebFrameworkStatics.GetParameterDeclarations( requiredParameters ),
				                            optionalParameters.Count > 0
					                            ? "Action<{0}> optionalParameterSetter = null".FormatWith(
						                            StringTools.ConcatenateWithDelimiter(
							                            ", ",
							                            "OptionalParameterSpecifier",
							                            includeEsParameter ? "EntitySetup" : "",
							                            "Parameters" ) )
					                            : "",
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
			if( generalData.IsResource() && optionalParameters.Any() )
				writer.WriteLine(
					"segmentParameterSpecifier = new Lazy<SegmentParameterSpecifier>( () => { var specifier = new SegmentParameterSpecifier(); specifySegmentParameters( specifier ); return specifier; }, LazyThreadSafetyMode.None );" );
			writer.WriteLine( "}" );

			writeInitParametersMethod( writer, generalData, requiredParameters, optionalParameters, includeEsParameter, isEs, constructorParameters );
		}

		private static void writeConstructorDocComments( TextWriter writer, IReadOnlyCollection<WebItemParameter> requiredParameters ) {
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
			TextWriter writer, WebItemGeneralData generalData, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeEsParameter, bool isEs, string constructorParameters ) {
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
				writer.WriteLine( "if( AppRequestState.Instance != null ) {" );

				// If the list of current URL handlers has a matching object, apply its parameter values.
				writer.WriteLine( "foreach( var urlHandler in AppRequestState.Instance.UrlHandlers )" );
				if( isEs ) {
					writer.WriteLine( "if( urlHandler is ResourceBase r ) {" );
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, true ) ) );
					generateMatchingHandlerParameterInitStatements( writer, optionalParameters, false );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
					writer.WriteLine( "else {" );
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, false ) ) );
					generateMatchingHandlerParameterInitStatements( writer, optionalParameters, false );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}
				else {
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, false ) ) );
					generateMatchingHandlerParameterInitStatements( writer, optionalParameters, false );
					writer.WriteLine( "}" );
				}

				// If new parameter values are effective, and the current resource or an ancestor matches this object, apply its new parameter values.
				if( generalData.IsPage() || isEs ) {
					writer.WriteLine( "if( AppRequestState.Instance.NewUrlParameterValuesEffective ) {" );
					writer.WriteLine( "UrlHandler urlHandler = {0}Current;".FormatWith( generalData.IsPage() ? "" : "PageBase." ) );
					writer.WriteLine( "do" );
					if( isEs ) {
						writer.WriteLine( "if( urlHandler is ResourceBase r ) {" );
						writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, true ) ) );
						generateMatchingHandlerParameterInitStatements( writer, optionalParameters, true );
						writer.WriteLine( "}" );
						writer.WriteLine( "}" );
						writer.WriteLine( "else {" );
						writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, false ) ) );
						generateMatchingHandlerParameterInitStatements( writer, optionalParameters, true );
						writer.WriteLine( "}" );
						writer.WriteLine( "}" );
					}
					else {
						writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, requiredParameters, false ) ) );
						generateMatchingHandlerParameterInitStatements( writer, optionalParameters, true );
						writer.WriteLine( "}" );
					}
					writer.WriteLine( "while( ( urlHandler = urlHandler.GetParent() ) != null );" );
					writer.WriteLine( "}" );
				}

				writer.WriteLine( "}" );

				// Apply parameter values from the setter.
				writer.WriteLine( "var optionalParameterSpecifier = new OptionalParameterSpecifier();" );
				writer.WriteLine(
					"optionalParameterSetter?.Invoke( {0} );".FormatWith(
						StringTools.ConcatenateWithDelimiter(
							", ",
							"optionalParameterSpecifier",
							includeEsParameter ? "es" : "",
							"new Parameters( {0} )".FormatWith(
								StringTools.ConcatenateWithDelimiter(
									", ",
									requiredParameters.Select( i => i.PropertyName )
										.Append(
											"optionalParametersInitializedFromCurrent ? new OptionalParameters( {0} ) : null".FormatWith(
												StringTools.ConcatenateWithDelimiter( ", ", optionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) ) ) );
				foreach( var i in optionalParameters )
					writer.WriteLine(
						"if( optionalParameterSpecifier.{0} ) {1} = optionalParameterSpecifier.{2};".FormatWith(
							OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( i ),
							i.FieldName,
							i.PropertyName ) );

				// This is called after all current values and values from the setter have been incorporated since these can affect default values.
				writer.WriteLine(
					"specifyParameterDefaults( {0} );".FormatWith(
						StringTools.ConcatenateWithDelimiter(
							", ",
							WebItemGeneralData.ParameterDefaultsFieldName,
							includeEsParameter ? "es" : "",
							"new Parameters( {0} )".FormatWith(
								StringTools.ConcatenateWithDelimiter(
									", ",
									requiredParameters.Select( i => i.PropertyName )
										.Append(
											"new OptionalParameters( {0} )".FormatWith(
												StringTools.ConcatenateWithDelimiter( ", ", optionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) ) ) );

				// Apply default values to parameters not yet initialized.
				writer.WriteLine( "if( !optionalParametersInitializedFromCurrent ) {" );
				foreach( var i in optionalParameters )
					writer.WriteLine(
						"if( !optionalParameterSpecifier.{0} && {1}.{0} ) {2} = {1}.{3};".FormatWith(
							OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( i ),
							WebItemGeneralData.ParameterDefaultsFieldName,
							i.FieldName,
							i.PropertyName ) );
				writer.WriteLine( "}" );
			}

			if( !isEs )
				writer.WriteLine( "base.uriFragmentIdentifier = uriFragmentIdentifier;" );

			if( optionalParameters.Any() )
				writer.WriteLine( "this.optionalParameterSetter = optionalParameterSetter;" );

			writer.WriteLine( "}" ); // initParameters method
		}

		private static string getHandlerMatchExpression(
			WebItemGeneralData generalData, IReadOnlyCollection<WebItemParameter> requiredParameters, bool compareEntitySetup ) =>
			( compareEntitySetup
				  ? "r.EsAsBaseType is {0} match".FormatWith( generalData.ClassName )
				  : "urlHandler is {0} match".FormatWith( generalData.ClassName ) ) + StringTools.ConcatenateWithDelimiter(
					" && ",
					requiredParameters.Select( i => i.GetEqualityExpression( "match.{0}".FormatWith( i.PropertyName ), i.PropertyName ) ) )
				.PrependDelimiter( " && " );

		private static void generateMatchingHandlerParameterInitStatements(
			TextWriter writer, IReadOnlyCollection<WebItemParameter> optionalParameters, bool useNewParameterValues ) {
			foreach( var i in optionalParameters )
				writer.WriteLine(
					useNewParameterValues
						? "{0} = match.parametersModification.{1};".FormatWith( i.FieldName, i.PropertyName )
						: "{0} = match.{1};".FormatWith( i.FieldName, i.PropertyName ) );
			writer.WriteLine( "optionalParametersInitializedFromCurrent = true;" );
			writer.WriteLine( "break;" );
		}

		internal static string GetInfoConstructorArgumentsForRequiredParameters(
			IReadOnlyCollection<WebItemParameter> requiredParameters, Func<WebItemParameter, string> requiredParameterToArgMapper ) {
			var text = "";
			foreach( var requiredParameter in requiredParameters )
				text = StringTools.ConcatenateWithDelimiter( ", ", text, requiredParameterToArgMapper( requiredParameter ) );
			return text;
		}

		internal static void WriteEqualsParameterComparisons(
			TextWriter writer, IReadOnlyCollection<WebItemParameter> requiredParameters, IReadOnlyCollection<WebItemParameter> optionalParameters,
			string otherObjectName ) {
			foreach( var parameter in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine(
					"if( !{0} ) return false;".FormatWith(
						parameter.GetEqualityExpression( "{0}.{1}".FormatWith( otherObjectName, parameter.PropertyName ), parameter.PropertyName ) ) );
			writer.WriteLine( "return true;" );
		}

		internal static void WriteGetHashCodeMethod(
			TextWriter writer, string pathRelativeToProject, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters ) {
			writer.WriteLine(
				"public override int GetHashCode() => ( {0} ).GetHashCode();".FormatWith(
					"@\"{0}\"".FormatWith( pathRelativeToProject ) + StringTools.ConcatenateWithDelimiter(
							", ",
							requiredParameters.Concat( optionalParameters ).Where( i => !i.IsEnumerable ).Select( i => i.PropertyName ) )
						.PrependDelimiter( ", " ) ) );
		}
	}
}