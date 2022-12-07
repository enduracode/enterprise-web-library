using EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework.WebItems;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.WebFramework {
	internal static class UrlStatics {
		internal static void GenerateUrlClasses(
			TextWriter writer, string className, EntitySetup entitySetup, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeVersionString ) {
			generateEncoder( writer, entitySetup, requiredParameters, optionalParameters, includeVersionString );
			generateDecoder( writer, className, entitySetup, requiredParameters, optionalParameters, includeVersionString );
			generatePatterns( writer, className, entitySetup, requiredParameters, optionalParameters, includeVersionString );
		}

		private static void generateEncoder(
			TextWriter writer, EntitySetup entitySetup, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeVersionString ) {
			writer.WriteLine( "internal sealed class UrlEncoder: global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder {" );

			if( entitySetup != null ) {
				writer.WriteLine( "private readonly {0} entitySetup;".FormatWith( entitySetup.GeneralData.ClassName ) );
				writer.WriteLine( "private bool entitySetupMatched;" );
				writer.WriteLine( "private readonly Lazy<{0}.UrlEncoder> entitySetupEncoder;".FormatWith( entitySetup.GeneralData.ClassName ) );
			}
			foreach( var i in requiredParameters ) {
				writer.WriteLine( "private readonly {0} {1};".FormatWith( i.TypeName, i.Name ) );
				writer.WriteLine( "private bool {0}Accessed;".FormatWith( i.Name ) );
			}
			foreach( var i in optionalParameters ) {
				writer.WriteLine( "private readonly {0} {1};".FormatWith( getSpecifiableParameterType( i ), i.Name ) );
				writer.WriteLine( "private readonly bool {0}IsSegmentParameter;".FormatWith( i.Name ) );
				writer.WriteLine( "private bool {0}Accessed;".FormatWith( i.Name ) );
			}
			if( includeVersionString ) {
				writer.WriteLine( "private readonly string versionString;" );
				writer.WriteLine( "private bool versionStringAccessed;" );
			}

			writer.WriteLine(
				"internal UrlEncoder({0}) {{".FormatWith(
					StringTools.ConcatenateWithDelimiter(
							", ",
							( entitySetup != null ? "{0} entitySetup".FormatWith( entitySetup.GeneralData.ClassName ).ToCollection() : Enumerable.Empty<string>() )
							.Concat( requiredParameters.Select( i => i.TypeName + " " + i.Name ) )
							.Concat(
								optionalParameters.SelectMany(
									i => new[] { getSpecifiableParameterType( i ) + " " + i.Name, "bool {0}IsSegmentParameter".FormatWith( i.Name ) } ) )
							.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() ) )
						.Surround( " ", " " ) ) );
			if( entitySetup != null ) {
				writer.WriteLine( "this.entitySetup = entitySetup;" );
				writer.WriteLine(
					"entitySetupEncoder = new Lazy<{0}.UrlEncoder>( () => ({0}.UrlEncoder)( (UrlHandler)entitySetup ).GetEncoder(), LazyThreadSafetyMode.None );"
						.FormatWith( entitySetup.GeneralData.ClassName ) );
			}
			foreach( var i in requiredParameters )
				writer.WriteLine( "this.{0} = {0};".FormatWith( i.Name ) );
			foreach( var i in optionalParameters ) {
				writer.WriteLine( "this.{0} = {0};".FormatWith( i.Name ) );
				writer.WriteLine( "this.{0}IsSegmentParameter = {0}IsSegmentParameter;".FormatWith( i.Name ) );
			}
			if( includeVersionString )
				writer.WriteLine( "this.versionString = versionString;" );
			writer.WriteLine( "}" );

			if( entitySetup != null ) {
				writer.WriteLine( "public bool CheckEntitySetup( {0} entitySetup ) {{".FormatWith( entitySetup.GeneralData.ClassName ) );
				writer.WriteLine( "if( !EwlStatics.AreEqual( entitySetup, this.entitySetup ) ) return false;" );
				writer.WriteLine( "entitySetupMatched = true;" );
				writer.WriteLine( "return true;" );
				writer.WriteLine( "}" );

				foreach( var i in entitySetup.RequiredParameters )
					writer.WriteLine( "public {0} Get{1}() => entitySetupEncoder.Value.Get{1}();".FormatWith( i.TypeName, i.PropertyName ) );
				foreach( var i in entitySetup.OptionalParameters ) {
					writer.WriteLine( "public bool {0}IsPresent => entitySetupEncoder.Value.{0}IsPresent;".FormatWith( i.PropertyName ) );
					writer.WriteLine(
						"public ( {0} value, bool isSegmentParameter ) Get{1}() => entitySetupEncoder.Value.Get{1}();".FormatWith( i.TypeName, i.PropertyName ) );
				}
			}

			foreach( var i in requiredParameters ) {
				writer.WriteLine( "public {0} Get{1}() {{".FormatWith( i.TypeName, i.PropertyName ) );
				writer.WriteLine( "{0}Accessed = true;".FormatWith( i.Name ) );
				writer.WriteLine( "return {0};".FormatWith( i.Name ) );
				writer.WriteLine( "}" );
			}
			foreach( var i in optionalParameters ) {
				writer.WriteLine( "public bool {0}IsPresent => {1} != null;".FormatWith( i.PropertyName, i.Name ) );

				writer.WriteLine( "public ( {0} value, bool isSegmentParameter ) Get{1}() {{".FormatWith( i.TypeName, i.PropertyName ) );
				writer.WriteLine( "if( !{0}IsPresent ) throw new ApplicationException( \"The parameter is not present.\" );".FormatWith( i.PropertyName ) );
				writer.WriteLine( "{0}Accessed = true;".FormatWith( i.Name ) );
				writer.WriteLine( "return ( {0}{1}, {0}IsSegmentParameter );".FormatWith( i.Name, getSpecifiableParameterValueSelector( i ) ) );
				writer.WriteLine( "}" );
			}
			if( includeVersionString ) {
				writer.WriteLine( "public string GetVersionString() {" );
				writer.WriteLine( "versionStringAccessed = true;" );
				writer.WriteLine( "return versionString;" );
				writer.WriteLine( "}" );
			}

			writer.WriteLine(
				"IReadOnlyCollection<( string, string, bool )> global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder.GetRemainingParameters() {" );
			writer.WriteLine(
				"var parameters = new List<( string, string, bool )>( {0} );".FormatWith(
					( entitySetup?.RequiredParameters.Concat( entitySetup.OptionalParameters ).Count() ?? 0 ) +
					requiredParameters.Concat( optionalParameters ).Count() ) );
			if( entitySetup != null )
				writer.WriteLine(
					"if( !entitySetupMatched ) parameters.AddRange( ( (global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder)entitySetupEncoder.Value ).GetRemainingParameters() );" );
			foreach( var i in requiredParameters )
				writer.WriteLine( "if( !{0}Accessed ) parameters.Add( ( \"{0}\", {1}, true ) );".FormatWith( i.Name, i.GetUrlSerializationExpression( i.Name ) ) );
			foreach( var i in optionalParameters )
				writer.WriteLine(
					"if( {0}IsPresent && !{1}Accessed ) parameters.Add( ( \"{1}\", {2}, {1}IsSegmentParameter ) );".FormatWith(
						i.PropertyName,
						i.Name,
						i.GetUrlSerializationExpression( i.Name + getSpecifiableParameterValueSelector( i ) ) ) );
			if( includeVersionString )
				writer.WriteLine( "if( versionString.Any() && !versionStringAccessed ) parameters.Add( ( \"version\", versionString, false ) );" );
			writer.WriteLine( "return parameters;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "void global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder.ResetState() {" );
			if( entitySetup != null ) {
				writer.WriteLine( "entitySetupMatched = false;" );
				writer.WriteLine(
					"if( entitySetupEncoder.IsValueCreated ) ( (global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder)entitySetupEncoder.Value ).ResetState();" );
			}
			foreach( var i in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine( "{0}Accessed = false;".FormatWith( i.Name ) );
			if( includeVersionString )
				writer.WriteLine( "versionStringAccessed = false;" );
			writer.WriteLine( "}" );

			writer.WriteLine( "}" );
		}

		private static void generateDecoder(
			TextWriter writer, string className, EntitySetup entitySetup, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeVersionString ) {
			writer.WriteLine( "internal sealed class UrlDecoder: global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlDecoder {" );

			if( entitySetup != null )
				writer.WriteLine( "private readonly Func<DecodingUrlParameterCollection, {0}> entitySetupGetter;".FormatWith( entitySetup.GeneralData.ClassName ) );
			foreach( var i in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine( "private readonly {0} {1};".FormatWith( getSpecifiableParameterType( i ), i.Name ) );
			if( includeVersionString )
				writer.WriteLine( "private readonly string versionString;" );

			if( entitySetup == null )
				writer.WriteLine(
					"public UrlDecoder({0}) {{".FormatWith(
						StringTools.ConcatenateWithDelimiter(
								", ",
								requiredParameters.Concat( optionalParameters )
									.Select( i => getSpecifiableParameterType( i ) + " " + i.Name )
									.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() )
									.Select( i => "{0} = null".FormatWith( i ) ) )
							.Surround( " ", " " ) ) );
			else {
				writer.WriteLine(
					"public UrlDecoder( {0} ): this( {1} ) {{}}".FormatWith(
						StringTools.ConcatenateWithDelimiter(
							", ",
							"{0} entitySetup".FormatWith( entitySetup.GeneralData.ClassName )
								.ToCollection()
								.Concat(
									requiredParameters.Concat( optionalParameters )
										.Select( i => getSpecifiableParameterType( i ) + " " + i.Name )
										.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() )
										.Select( i => "{0} = null".FormatWith( i ) ) ) ),
						StringTools.ConcatenateWithDelimiter(
							", ",
							"parameters => entitySetup".ToCollection().Concat( requiredParameters.Concat( optionalParameters ).Select( i => i.Name ) ) ) ) );
				writer.WriteLine(
					"public UrlDecoder({0}): this( {1} ) {{}}".FormatWith(
						StringTools.ConcatenateWithDelimiter(
								", ",
								entitySetup.RequiredParameters.Concat( entitySetup.OptionalParameters )
									.Concat( requiredParameters )
									.Concat( optionalParameters )
									.Select( i => getSpecifiableParameterType( i ) + " " + i.Name )
									.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() )
									.Select( i => "{0} = null".FormatWith( i ) ) )
							.Surround( " ", " " ),
						StringTools.ConcatenateWithDelimiter(
							", ",
							"parameters => ({0})( (global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlDecoder)new {0}.UrlDecoder({1}) ).GetUrlHandler( parameters )"
								.FormatWith(
									entitySetup.GeneralData.ClassName,
									StringTools.ConcatenateWithDelimiter(
											", ",
											entitySetup.RequiredParameters.Concat( entitySetup.OptionalParameters ).Select( i => i.Name + ": " + i.Name ) )
										.Surround( " ", " " ) )
								.ToCollection()
								.Concat( requiredParameters.Concat( optionalParameters ).Select( i => i.Name ) ) ) ) );
				writer.WriteLine(
					"private UrlDecoder( {0} ) {{".FormatWith(
						StringTools.ConcatenateWithDelimiter(
							", ",
							"Func<DecodingUrlParameterCollection, {0}> entitySetupGetter".FormatWith( entitySetup.GeneralData.ClassName )
								.ToCollection()
								.Concat( requiredParameters.Concat( optionalParameters ).Select( i => getSpecifiableParameterType( i ) + " " + i.Name ) )
								.Concat( includeVersionString ? "string versionString".ToCollection() : Enumerable.Empty<string>() ) ) ) );
			}
			if( entitySetup != null )
				writer.WriteLine( "this.entitySetupGetter = entitySetupGetter;" );
			foreach( var i in requiredParameters.Concat( optionalParameters ) )
				writer.WriteLine( "this.{0} = {0};".FormatWith( i.Name ) );
			if( includeVersionString )
				writer.WriteLine( "this.versionString = versionString;" );
			writer.WriteLine( "}" );

			writer.WriteLine(
				"BasicUrlHandler global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlDecoder.GetUrlHandler( DecodingUrlParameterCollection parameters ) {" );
			if( entitySetup != null )
				writer.WriteLine( "var entitySetup = entitySetupGetter( parameters );" );

			foreach( var i in requiredParameters ) {
				writer.WriteLine( "{0} {1}Argument;".FormatWith( i.TypeName, i.Name ) );
				writer.WriteLine( "if( {0} != null ) {0}Argument = {0}{1};".FormatWith( i.Name, getSpecifiableParameterValueSelector( i ) ) );
				writer.WriteLine( "else {" );
				writer.WriteLine( "var {0}String = parameters.GetRemainingParameter( \"{0}\" );".FormatWith( i.Name ) );
				writer.WriteLine( "if( {0}String == null ) throw new UnresolvableUrlException( \"The {0} parameter is not present.\", null );".FormatWith( i.Name ) );
				writer.WriteLine( "try {" );
				writer.WriteLine( "{0}Argument = {1};".FormatWith( i.Name, i.GetUrlDeserializationExpression( "{0}String".FormatWith( i.Name ) ) ) );
				writer.WriteLine( "}" );
				writer.WriteLine( "catch( Exception e ) {" );
				writer.WriteLine( "throw new UnresolvableUrlException( \"Failed to deserialize the {0} parameter.\", e );".FormatWith( i.Name ) );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}

			foreach( var i in optionalParameters ) {
				writer.WriteLine( "{0} {1}Argument = null;".FormatWith( getSpecifiableParameterType( i ), i.Name ) );
				writer.WriteLine( "if( {0} != null ) {0}Argument = {0};".FormatWith( i.Name ) );
				writer.WriteLine( "else {" );
				writer.WriteLine( "var {0}String = parameters.GetRemainingParameter( \"{0}\" );".FormatWith( i.Name ) );
				writer.WriteLine( "if( {0}String != null )".FormatWith( i.Name ) );
				writer.WriteLine( "try {" );
				var deserializationExpression = i.GetUrlDeserializationExpression( "{0}String".FormatWith( i.Name ) );
				writer.WriteLine(
					"{0}Argument = {1};".FormatWith(
						i.Name,
						i.IsString || i.IsEnumerable ? deserializationExpression : "new SpecifiedValue<{0}>( {1} )".FormatWith( i.TypeName, deserializationExpression ) ) );
				writer.WriteLine( "}" );
				writer.WriteLine( "catch( Exception e ) {" );
				writer.WriteLine( "throw new UnresolvableUrlException( \"Failed to deserialize the {0} parameter.\", e );".FormatWith( i.Name ) );
				writer.WriteLine( "}" );
				writer.WriteLine( "}" );
			}

			if( includeVersionString )
				writer.WriteLine( "var isVersioned = versionString != null ? versionString.Any() : parameters.GetRemainingParameter( \"version\" ) != null;" );

			writer.WriteLine( "try {" );
			writer.WriteLine(
				"return new {0}({1});".FormatWith(
					className,
					StringTools.ConcatenateWithDelimiter(
							", ",
							( entitySetup != null ? "entitySetup".ToCollection() : Enumerable.Empty<string>() )
							.Concat( requiredParameters.Select( i => "{0}Argument".FormatWith( i.Name ) ) )
							.Concat(
								optionalParameters.Any()
									? StringTools.ConcatenateWithDelimiter(
											Environment.NewLine,
											"optionalParameterSetter: ( {0} ) => {{"
												.FormatWith( StringTools.ConcatenateWithDelimiter( ", ", "s", entitySetup != null ? "es" : "", "p" ) )
												.ToCollection()
												.Append( "if( p.OptionalParameters != null ) return;" )
												.Concat(
													optionalParameters.Select(
														i => "if( {0}Argument != null ) s.{1} = {0}Argument{2};".FormatWith(
															i.Name,
															i.PropertyName,
															getSpecifiableParameterValueSelector( i ) ) ) )
												.Append( "}" ) )
										.ToCollection()
									: Enumerable.Empty<string>() )
							.Concat( includeVersionString ? "disableVersioning: !isVersioned".ToCollection() : Enumerable.Empty<string>() ) )
						.Surround( " ", " " ) ) );
			writer.WriteLine( "}" );
			writer.WriteLine( "catch( Exception e ) {" );
			writer.WriteLine( "if( e is UserDisabledException ) throw;" );
			writer.WriteLine( "throw new UnresolvableUrlException( \"Failed to create the URL handler.\", e );" );
			writer.WriteLine( "}" );

			writer.WriteLine( "}" );

			writer.WriteLine( "}" );
		}

		private static string getSpecifiableParameterType( WebItemParameter p ) =>
			p.IsString || p.IsEnumerable ? p.TypeName : "SpecifiedValue<{0}>".FormatWith( p.TypeName );

		private static string getSpecifiableParameterValueSelector( WebItemParameter p ) => p.IsString || p.IsEnumerable ? "" : ".Value";

		private static void generatePatterns(
			TextWriter writer, string className, EntitySetup entitySetup, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, bool includeVersionString ) {
			writer.WriteLine( "internal static class UrlPatterns {" );

			CodeGenerationStatics.AddSummaryDocComment(
				writer,
				"Creates a literal URL pattern. Segment suggestion: {0}.".FormatWith( className.CamelToEnglish().ToUrlSlug() ) );
			writer.WriteLine(
				"public static UrlPattern Literal( {0} ) => new UrlPattern( encoder => {1}, url => {2} );".FormatWith(
					( entitySetup != null ? "{0} entitySetup, ".FormatWith( entitySetup.GeneralData.ClassName ) : "" ) + "string segment" +
					getOldParameterNamePatternParameters( requiredParameters.Concat( optionalParameters ) ).PrependDelimiter( ", " ),
					entitySetup != null
						?
						includeVersionString
							? "encoder is UrlEncoder local && local.CheckEntitySetup( entitySetup ) ? local.GetVersionString().Length > 0 ? EncodingUrlSegment.CreateWithVersionString( segment, local.GetVersionString() ) : EncodingUrlSegment.Create( segment ) : null"
							: "encoder is UrlEncoder local && local.CheckEntitySetup( entitySetup ) ? EncodingUrlSegment.Create( segment ) : null"
						: includeVersionString
							? "encoder is UrlEncoder local ? local.GetVersionString().Length > 0 ? EncodingUrlSegment.CreateWithVersionString( segment, local.GetVersionString() ) : EncodingUrlSegment.Create( segment ) : null"
							: "encoder is UrlEncoder local ? EncodingUrlSegment.Create( segment ) : null",
					entitySetup != null
						?
						includeVersionString
							? "url.HasVersionString( out var components ) && string.Equals( components.segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( {0}, versionString: components.versionString ) : string.Equals( url.Segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( {0}, versionString: \"\" ) : null"
								.FormatWith( "entitySetup" + getOldParameterNameDecoderArguments( requiredParameters.Concat( optionalParameters ) ).PrependDelimiter( ", " ) )
							: "string.Equals( url.Segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( entitySetup{0} ) : null".FormatWith(
								getOldParameterNameDecoderArguments( requiredParameters.Concat( optionalParameters ) ).PrependDelimiter( ", " ) )
						: includeVersionString
							? "url.HasVersionString( out var components ) && string.Equals( components.segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( {0}versionString: components.versionString ) : string.Equals( url.Segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( {0}versionString: \"\" ) : null"
								.FormatWith( getOldParameterNameDecoderArguments( requiredParameters.Concat( optionalParameters ) ).AppendDelimiter( ", " ) )
							: "string.Equals( url.Segment, segment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder({0}) : null".FormatWith(
								getOldParameterNameDecoderArguments( requiredParameters.Concat( optionalParameters ) ).Surround( " ", " " ) ) ) );

			if( requiredParameters.Count == 1 ) {
				var parameter = requiredParameters.Single();
				if( "int".ToCollection().Append( "int?" ).Contains( parameter.TypeName ) ) {
					var parameterIsNullable = parameter.TypeName == "int?";
					CodeGenerationStatics.AddSummaryDocComment(
						writer,
						"Creates a positive-int URL pattern." + ( parameterIsNullable
							                                          ? " If the parameter supports null for the purpose of creating new resources, we recommend that the null segment be a verb such as “create” or “add”."
							                                          : "" ) );
					writer.WriteLine(
						"public static UrlPattern {0}({1}) => new UrlPattern( encoder => {2}, url => {3} );".FormatWith(
							parameter.PropertyName + "PositiveInt",
							StringTools.ConcatenateWithDelimiter(
									", ",
									entitySetup != null ? "{0} entitySetup".FormatWith( entitySetup.GeneralData.ClassName ) : "",
									parameterIsNullable ? "string nullSegment" : "",
									getOldParameterNamePatternParameters( optionalParameters ) )
								.Surround( " ", " " ),
							entitySetup != null
								?
								parameterIsNullable
									? "encoder is UrlEncoder local && local.CheckEntitySetup( entitySetup ) ? local.Get{0}().HasValue ? EncodingUrlSegment.CreatePositiveInt( local.Get{0}().Value ) : EncodingUrlSegment.Create( nullSegment ) : null"
										.FormatWith( parameter.PropertyName )
									: "encoder is UrlEncoder local && local.CheckEntitySetup( entitySetup ) ? EncodingUrlSegment.CreatePositiveInt( local.Get{0}() ) : null"
										.FormatWith( parameter.PropertyName )
								: parameterIsNullable
									? "encoder is UrlEncoder local ? local.Get{0}().HasValue ? EncodingUrlSegment.CreatePositiveInt( local.Get{0}().Value ) : EncodingUrlSegment.Create( nullSegment ) : null"
										.FormatWith( parameter.PropertyName )
									: "encoder is UrlEncoder local ? EncodingUrlSegment.CreatePositiveInt( local.Get{0}() ) : null".FormatWith( parameter.PropertyName ),
							entitySetup != null
								?
								parameterIsNullable
									? "url.IsPositiveInt( out var segmentInt ) ? new UrlDecoder( entitySetup, {0}: new SpecifiedValue<int?>( segmentInt ){1} ) : string.Equals( url.Segment, nullSegment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( entitySetup, {0}: new SpecifiedValue<int?>( null ){1} ) : null"
										.FormatWith( parameter.Name, getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) )
									: "url.IsPositiveInt( out var segmentInt ) ? new UrlDecoder( entitySetup, {0}: new SpecifiedValue<int>( segmentInt ){1} ) : null".FormatWith(
										parameter.Name,
										getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) )
								: parameterIsNullable
									? "url.IsPositiveInt( out var segmentInt ) ? new UrlDecoder( {0}: new SpecifiedValue<int?>( segmentInt ){1} ) : string.Equals( url.Segment, nullSegment, StringComparison.OrdinalIgnoreCase ) ? new UrlDecoder( {0}: new SpecifiedValue<int?>( null ){1} ) : null"
										.FormatWith( parameter.Name, getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) )
									: "url.IsPositiveInt( out var segmentInt ) ? new UrlDecoder( {0}: new SpecifiedValue<int>( segmentInt ){1} ) : null".FormatWith(
										parameter.Name,
										getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) ) ) );
				}
				if( parameter.IsString ) {
					CodeGenerationStatics.AddSummaryDocComment( writer, "Creates a string URL pattern." );
					writer.WriteLine(
						"public static UrlPattern {0}({1}) => new UrlPattern( encoder => {2}, url => {3} );".FormatWith(
							parameter.PropertyName + "String",
							StringTools.ConcatenateWithDelimiter(
									", ",
									entitySetup != null ? "{0} entitySetup".FormatWith( entitySetup.GeneralData.ClassName ) : "",
									getOldParameterNamePatternParameters( optionalParameters ) )
								.Surround( " ", " " ),
							entitySetup != null
								? "encoder is UrlEncoder local && local.CheckEntitySetup( entitySetup ) ? EncodingUrlSegment.Create( local.Get{0}() ) : null".FormatWith(
									parameter.PropertyName )
								: "encoder is UrlEncoder local ? EncodingUrlSegment.Create( local.Get{0}() ) : null".FormatWith( parameter.PropertyName ),
							entitySetup != null
								? "new UrlDecoder( entitySetup, {0}: url.Segment{1} )".FormatWith(
									parameter.Name,
									getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) )
								: "new UrlDecoder( {0}: url.Segment{1} )".FormatWith(
									parameter.Name,
									getOldParameterNameDecoderArguments( optionalParameters ).PrependDelimiter( ", " ) ) ) );
				}
			}

			if( entitySetup == null && requiredParameters.Count == 0 && !includeVersionString ) {
				CodeGenerationStatics.AddSummaryDocComment( writer, "Creates a base URL pattern that generates the default base URL and accepts any base URL." );
				writer.WriteLine(
					"public static BaseUrlPattern BaseUrlPattern({0}) => new BaseUrlPattern( encoder => {1}, url => {2} );".FormatWith(
						getOldParameterNamePatternParameters( optionalParameters ).Surround( " ", " " ),
						"encoder is UrlEncoder local ? new EncodingBaseUrl( new BaseUrl( \"\", null, null, null ) ) : null",
						"new UrlDecoder({0})".FormatWith( getOldParameterNameDecoderArguments( optionalParameters ).Surround( " ", " " ) ) ) );
			}

			writer.WriteLine( "}" );
		}

		private static string getOldParameterNamePatternParameters( IEnumerable<WebItemParameter> parameters ) =>
			StringTools.ConcatenateWithDelimiter( ", ", parameters.Select( i => "IEnumerable<string> {0}OldNames = null".FormatWith( i.Name ) ) );

		private static string getOldParameterNameDecoderArguments( IEnumerable<WebItemParameter> parameters ) =>
			StringTools.ConcatenateWithDelimiter(
				", ",
				parameters.Select(
					i =>
						"{0}: {0}OldNames != null ? {0}OldNames.Select( i => url.Parameters.Get( i ) ).Where( i => i != null ).Select( value => {{ try {{ return {1}; }} catch( Exception e ) {{ throw new UnresolvableUrlException( \"Failed to deserialize the {0} parameter.\", e ); }} }} ).FirstOrDefault() : null"
							.FormatWith(
								i.Name,
								i.IsString || i.IsEnumerable
									? i.GetUrlDeserializationExpression( "value" )
									: "new SpecifiedValue<{0}>( {1} )".FormatWith( i.TypeName, i.GetUrlDeserializationExpression( "value" ) ) ) ) );

		internal static void GenerateGetEncoderMethod(
			TextWriter writer, string entitySetupFieldName, IReadOnlyCollection<WebItemParameter> requiredParameters,
			IReadOnlyCollection<WebItemParameter> optionalParameters, Func<WebItemParameter, string> isSegmentParameterExpressionGetter, bool includeVersionString ) {
			writer.WriteLine(
				"protected override global::EnterpriseWebLibrary.EnterpriseWebFramework.UrlEncoder getUrlEncoder() => new UrlEncoder({0});".FormatWith(
					StringTools.ConcatenateWithDelimiter(
							", ",
							( entitySetupFieldName.Any() ? entitySetupFieldName.ToCollection() : Enumerable.Empty<string>() )
							.Concat( requiredParameters.Select( i => i.PropertyName ) )
							.Concat(
								optionalParameters.SelectMany(
									i => {
										// If a default was specified for the parameter and the default matches the value of our parameter, don't include it.
										// If a default was not specified and the value of our parameter is the default value of the type, don't include it.
										var defaultParameterReference = WebItemGeneralData.ParameterDefaultsFieldName + "." + i.PropertyName;
										var value = "( {0} && {1} ) || ( !{0} && {2} ) ? null : {3}".FormatWith(
											WebItemGeneralData.ParameterDefaultsFieldName + "." + OptionalParameterPackageStatics.GetWasSpecifiedPropertyName( i ),
											i.IsEnumerable
												? defaultParameterReference + ".SequenceEqual( " + i.PropertyName + " )"
												: defaultParameterReference + " == " + i.PropertyName,
											i.IsEnumerable ? "!" + i.PropertyName + ".Any()" : i.PropertyName + " == " + ( i.IsString ? "\"\"" : "default(" + i.TypeName + ")" ),
											i.IsString || i.IsEnumerable ? i.PropertyName : "new SpecifiedValue<{0}>( {1} )".FormatWith( i.TypeName, i.PropertyName ) );

										return new[] { value, isSegmentParameterExpressionGetter( i ) };
									} ) )
							.Concat( includeVersionString ? "getUrlVersionString()".ToCollection() : Enumerable.Empty<string>() ) )
						.Surround( " ", " " ) ) );
		}
	}
}