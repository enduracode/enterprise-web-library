namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.WebFramework;

internal static class ParametersModificationStatics {
	internal static void WriteClassIfNecessary( TextWriter writer, IEnumerable<WebItemParameter> parameters ) {
		if( !parameters.Any() )
			return;

		writer.WriteLine( "internal partial class ParametersModification {" );
		foreach( var parameter in parameters ) {
			writer.WriteLine(
				"private readonly AbstractDataValue<{0}> {1} = new DataValue<{0}>( true );".FormatWith( parameter.TypeName, getParameterDataValueName( parameter ) ) );

			writePropertyDocComment( writer, parameter );
			writer.WriteLine(
				"internal " + parameter.TypeName + " " + parameter.PropertyName + " { get => " + getParameterDataValueName( parameter ) + ".Value; set { " +
				getParameterDataValueName( parameter ) + ".Value = value; } }" );

			new ModificationFormItemMethodWriter( parameter.GetModificationField() ).WriteFormItemGetters( writer );
		}
		writer.WriteLine( "}" );
	}

	private static void writePropertyDocComment( TextWriter writer, WebItemParameter parameter ) {
		CodeGenerationStatics.AddSummaryDocComment(
			writer,
			"Gets or sets the new value for the " + parameter.Name + " parameter." + ( parameter.AllowsNull ? "" : " The value cannot be null." ) );
	}

	private static string getParameterDataValueName( WebItemParameter parameter ) => parameter.Name + "DataValue";
}