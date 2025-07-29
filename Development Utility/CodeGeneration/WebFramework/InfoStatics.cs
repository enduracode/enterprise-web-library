namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework;

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
				"private " + parameter.TypeName + " " + parameter.FieldName + ( parameter.InitExpression is { Length: > 0 } expression ? $" = {expression}" : "" ) +
				";" );
			CodeGenerationStatics.AddSummaryDocComment( writer, parameter.Comment );
			writer.WriteLine( "public " + parameter.TypeName + " " + parameter.PropertyName + " { get { return " + parameter.FieldName + "; } }" );
		}
	}

	internal static void WriteConstructor( TextWriter writer, WebItemGeneralData generalData, bool includeEsParameter, bool isEs ) {
		if( includeEsParameter )
			CodeGenerationStatics.AddParamDocComment( writer, "es", "Not yet documented." );
		foreach( var parameter in generalData.RequiredParameters )
			CodeGenerationStatics.AddParamDocComment(
				writer,
				parameter.Name,
				parameter.Comment.ConcatenateWithSpace( parameter.AllowsNull ? "" : "Do not pass null." ) );
		if( generalData.OptionalParameters.Count > 0 )
			CodeGenerationStatics.AddParamDocComment( writer, "optionalParameterSetter", "Not yet documented." );
		if( !isEs )
			CodeGenerationStatics.AddParamDocComment( writer, "uriFragmentIdentifier", "Not yet documented." );
		var constructorParameters = "( " + StringTools.ConcatenateWithDelimiter(
			                            ", ",
			                            includeEsParameter ? "EntitySetup es" : "",
			                            WebFrameworkStatics.GetParameterDeclarations( generalData.RequiredParameters ),
			                            generalData.OptionalParameters.Count > 0
				                            ? "Action<{0}>? optionalParameterSetter = null".FormatWith(
					                            StringTools.ConcatenateWithDelimiter(
						                            ", ",
						                            "OptionalParameterSpecifier",
						                            includeEsParameter ? "EntitySetup" : "",
						                            "Parameters" ) )
				                            : "",
			                            !isEs ? "string uriFragmentIdentifier = \"\"" : "" ) + " ) {";
		writer.WriteLine( "public {0}".FormatWith( generalData.ClassName ) + constructorParameters );

		// It’s important to force the cache to be enabled in the constructor since these objects are often created in post-back-action getters.
		writer.WriteLine( "DataAccessState.Current.ExecuteWithCache( () => {" );
		writeParameterInitStatements( writer, generalData, includeEsParameter, isEs );
		writer.WriteLine( "init();" );
		writer.WriteLine( "} );" );

		if( generalData.IsResource() && generalData.OptionalParameters.Any() )
			writer.WriteLine(
				"segmentParameterSpecifier = new Lazy<SegmentParameterSpecifier>( () => { var specifier = new SegmentParameterSpecifier(); specifySegmentParameters( specifier ); return specifier; }, LazyThreadSafetyMode.None );" );
		writer.WriteLine( "}" );
	}

	private static void writeParameterInitStatements( TextWriter writer, WebItemGeneralData generalData, bool includeEsParameter, bool isEs ) {
		if( includeEsParameter )
			writer.WriteLine( "Es = es;" );
		foreach( var requiredParameter in generalData.RequiredParameters )
			writer.WriteLine( requiredParameter.FieldName + " = " + requiredParameter.Name + ";" );

		// Initialize optional parameter fields.
		if( generalData.OptionalParameters.Any() ) {
			writer.WriteLine( "var optionalParametersInitializedFromCurrent = false;" );
			writer.WriteLine( "if( EwfRequest.Current != null ) {" );

			// If the list of current URL handlers has a matching object, apply its parameter values.
			writer.WriteLine( "foreach( var urlHandler in RequestDispatchingStatics.RequestState.UrlHandlers )" );
			if( isEs ) {
				writer.WriteLine( "if( urlHandler is ResourceBase r ) {" );
				writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, true ) ) );
				generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, false );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
				writer.WriteLine( "else {" );
				writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, false ) ) );
				generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, false );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}
			else {
				writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, false ) ) );
				generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, false );
				writer.WriteLine( "}" );
			}

			// If new parameter values are effective, and the current resource or an ancestor matches this object, apply its new parameter values.
			if( generalData.IsPage() || isEs ) {
				writer.WriteLine( "if( RequestDispatchingStatics.RequestState.NewUrlParameterValuesEffective ) {" );
				writer.WriteLine( "UrlHandler urlHandler = {0}Current;".FormatWith( generalData.IsPage() ? "" : "PageBase." ) );
				writer.WriteLine( "do" );
				if( isEs ) {
					writer.WriteLine( "if( urlHandler is ResourceBase r ) {" );
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, true ) ) );
					generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, true );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
					writer.WriteLine( "else {" );
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, false ) ) );
					generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, true );
					writer.WriteLine( "}" );
					writer.WriteLine( "}" );
				}
				else {
					writer.WriteLine( "if( {0} ) {{".FormatWith( getHandlerMatchExpression( generalData, generalData.RequiredParameters, false ) ) );
					generateMatchingHandlerParameterInitStatements( writer, generalData.OptionalParameters, true );
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
								generalData.RequiredParameters.Select( i => i.PropertyName )
									.Append(
										"optionalParametersInitializedFromCurrent ? new OptionalParameters( {0} ) : null".FormatWith(
											StringTools.ConcatenateWithDelimiter( ", ", generalData.OptionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) ) ) );
			foreach( var i in generalData.OptionalParameters )
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
								generalData.RequiredParameters.Select( i => i.PropertyName )
									.Append(
										"new OptionalParameters( {0} )".FormatWith(
											StringTools.ConcatenateWithDelimiter( ", ", generalData.OptionalParameters.Select( i => i.PropertyName ) ) ) ) ) ) ) ) );

			// Apply default values to parameters not yet initialized.
			writer.WriteLine( "if( !optionalParametersInitializedFromCurrent ) {" );
			foreach( var i in generalData.OptionalParameters )
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

		if( ( generalData.IsPage() || isEs ) && ( generalData.RequiredParameters.Any() || generalData.OptionalParameters.Any() ) ) {
			writer.WriteLine( "parametersModification = new ParametersModification();" );
			foreach( var i in generalData.RequiredParameters.Concat( generalData.OptionalParameters ) )
				writer.WriteLine( "parametersModification.{0} = {0};".FormatWith( i.PropertyName ) );
		}

		if( generalData.OptionalParameters.Any() )
			writer.WriteLine( "this.optionalParameterSetter = optionalParameterSetter;" );
	}

	private static string getHandlerMatchExpression(
		WebItemGeneralData generalData, IReadOnlyCollection<WebItemParameter> requiredParameters, bool compareEntitySetup ) =>
		( compareEntitySetup ? "r.EsAsBaseType is {0} match".FormatWith( generalData.ClassName ) : "urlHandler is {0} match".FormatWith( generalData.ClassName ) ) +
		StringTools.ConcatenateWithDelimiter(
				" && ",
				requiredParameters.Select( i => i.GetEqualityExpression( "match.{0}".FormatWith( i.PropertyName ), i.PropertyName ) ) )
			.PrependDelimiter( " && " );

	private static void generateMatchingHandlerParameterInitStatements(
		TextWriter writer, IReadOnlyCollection<WebItemParameter> optionalParameters, bool useNewParameterValues ) {
		foreach( var i in optionalParameters )
			writer.WriteLine(
				useNewParameterValues
					? "{0} = match.parametersModification!.{1};".FormatWith( i.FieldName, i.PropertyName )
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