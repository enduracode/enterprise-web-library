using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess.Subsystems;

static class CustomModificationStatics {
	private sealed record ModificationData(
		string ModificationName,
		IReadOnlyCollection<ValueContainer> Parameters,
		IReadOnlyCollection<( string commandText, IReadOnlyCollection<ValueContainer> parameters )> Commands );

	internal static void Generate(
		DatabaseConnection cn, TextWriter writer, string baseNamespace, Database database,
		EnterpriseWebLibrary.Configuration.SystemDevelopment.Database configuration ) {
		if( configuration.customModifications != null ) {
			writer.WriteLine( "namespace " + baseNamespace + " {" );

			var modifications = getModificationData( cn, database, configuration.customModifications );

			CodeGenerationStatics.AddSummaryDocComment( writer, "Contains custom modification operations." );
			writer.WriteLine( "public static class " + database.SecondaryDatabaseName + "CustomModifications {" );

			foreach( var mod in modifications )
				writeMethod( writer, database, mod );

			writer.WriteLine( "}" ); // class
			writer.WriteLine( "}" ); // namespace
		}
	}

	private static IReadOnlyCollection<ModificationData> getModificationData(
		DatabaseConnection cn, Database database, EnterpriseWebLibrary.Configuration.SystemDevelopment.CustomModification[] mods ) =>
		mods.Select( mod => {
				try {
					var commands = mod.commands.Select( commandText => {
							var parameters = DataAccessStatics.ExecuteWithCommandFromRawQueryText(
								cn,
								database,
								commandText,
								cmd => {
									// We don't test commands in Oracle because:
									// 1. There's no good junk value to pass in.
									// 2. The only way to keep the commands from actually modifying the database is with a transaction rollback, and we don't want to do that
									// unless absolutely necessary.
									// And we don't test commands in MySQL because of reason 2 above.
									if( cn.DatabaseInfo is MySqlInfo or OracleInfo )
										return;

									cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, _ => {} );
								} );
							return ( commandText, parameters );
						} )
						.Materialize();

					var methodParameters = commands.SelectMany( i => i.parameters )
						.GroupBy( i => i.Name )
						.Select( grouping => {
							var firstKnown = grouping.FirstOrDefault( i => i.DataType != typeof( object ) );
							if( firstKnown is null )
								return new ValueContainer( grouping.Key, cn.DatabaseInfo );

							// A shared parameter must support every command’s type and value expression.
							foreach( var parameter in grouping.Where( i => i.DataType != typeof( object ) ) )
								if( !parameter.DataTypeName.Equals( firstKnown.DataTypeName, StringComparison.Ordinal ) || !parameter
									    .GetParameterValueExpression( parameter.Name )
									    .Equals( firstKnown.GetParameterValueExpression( firstKnown.Name ), StringComparison.Ordinal ) )
									throw new UserCorrectableException(
										$"Parameter {grouping.Key} has incompatible types: " +
										$"{firstKnown.DataTypeName} ({firstKnown.GetParameterValueExpression( firstKnown.Name )}) and " +
										$"{parameter.DataTypeName} ({parameter.GetParameterValueExpression( parameter.Name )})." );

							return firstKnown;
						} )
						.Materialize();

					var parametersByName = methodParameters.ToDictionary( i => i.Name );
					return new ModificationData(
						mod.name,
						methodParameters,
						commands.Select( command => ( command.commandText, command.parameters.Select( i => parametersByName[ i.Name ] ).Materialize() ) ).Materialize() );
				}
				catch( Exception e ) {
					throw new UserCorrectableException( "Custom modification " + mod.name + " failed.", e );
				}
			} )
			.Materialize();

	private static void writeMethod( TextWriter writer, Database database, ModificationData mod ) {
		writer.WriteLine( "public static void " + mod.ModificationName + "( " + DataAccessStatics.GetMethodParams( mod.Parameters ) + " ) {" );

		writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteInTransaction( delegate {" );
		var cnt = 0;
		foreach( var command in mod.Commands ) {
			var commandVariableName = "cmd" + cnt++;
			writer.WriteLine( "DbCommand " + commandVariableName + " = " + DataAccessStatics.GetConnectionExpression( database ) + ".DatabaseInfo.CreateCommand();" );
			writer.WriteLine( commandVariableName + ".CommandText = @\"" + AppStatics.NormalizeLineEndingsFromXml( command.commandText ) + "\";" );
			DataAccessStatics.WriteAddParamBlock( writer, commandVariableName, command.parameters, database );
			writer.WriteLine( DataAccessStatics.GetConnectionExpression( database ) + ".ExecuteNonQueryCommand( " + commandVariableName + " );" );
		}
		writer.WriteLine( "} );" ); // execute in transaction call

		writer.WriteLine( "}" ); // method
	}
}