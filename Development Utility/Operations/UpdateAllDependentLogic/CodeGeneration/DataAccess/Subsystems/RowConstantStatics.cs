using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.InstallationSupportUtility;
using RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess.Subsystems {
	internal static class RowConstantStatics {
		private const string dictionaryName = "valuesAndNames";

		internal static void Generate( DBConnection cn, TextWriter writer, string baseNamespace, Database database,
		                               RedStapler.StandardLibrary.Configuration.SystemDevelopment.Database configuration ) {
			if( configuration.rowConstantTables == null )
				return;

			writer.WriteLine( "namespace " + baseNamespace + "." + database.SecondaryDatabaseName + "RowConstants {" );
			foreach( var table in configuration.rowConstantTables ) {
				Column valueColumn;
				var orderIsSpecified = !table.orderByColumn.IsNullOrWhiteSpace();
				var values = new List<string>();
				var names = new List<string>();
				try {
					var columns = new TableColumns( cn, table.tableName, false );
					valueColumn = columns.AllColumns.Single( column => column.Name.ToLower() == table.valueColumn.ToLower() );
					var nameColumn = columns.AllColumns.Single( column => column.Name.ToLower() == table.nameColumn.ToLower() );

					var cmd = cn.DatabaseInfo.CreateCommand();
					cmd.CommandText = "SELECT " + valueColumn.Name + ", " + nameColumn.Name + " FROM " + table.tableName;
					if( orderIsSpecified )
						cmd.CommandText += " ORDER BY " + table.orderByColumn;
					cn.ExecuteReaderCommand( cmd,
					                         reader => {
					                         	while( reader.Read() ) {
					                         		var valueString = reader.IsDBNull( reader.GetOrdinal( valueColumn.Name ) ) ? "null" : reader[ valueColumn.Name ].ToString();
					                         		if( valueColumn.DataTypeName == typeof( string ).ToString() )
					                         			values.Add( "\"" + valueString + "\"" );
					                         		else
					                         			values.Add( valueString );
					                         		names.Add( reader[ nameColumn.Name ].ToString() );
					                         	}
					                         } );
				}
				catch( Exception e ) {
					throw new UserCorrectableException(
						"Column or data retrieval failed for the " + table.tableName + " row constant table. Make sure the table and the value, name, and order by columns exist.",
						e );
				}

				CodeGenerationStatics.AddSummaryDocComment( writer, "Provides constants copied from the " + table.tableName + " table." );
				var className = table.tableName + "Rows";
				writer.WriteLine( "public class " + className + " {" );

				// constants
				for( var i = 0; i < values.Count; i++ ) {
					CodeGenerationStatics.AddSummaryDocComment( writer, "Constant generated from row in database table." );
					writer.WriteLine( "public const " + valueColumn.DataTypeName + " " + StandardLibraryMethods.GetCSharpIdentifier( names[ i ] ) + " = " + values[ i ] + ";" );
				}

				// one to one map
				var dictionaryType = "OneToOneMap<" + valueColumn.DataTypeName + ", string>";
				writer.WriteLine( "private static readonly " + dictionaryType + " " + dictionaryName + " = new " + dictionaryType + "();" );

				writeStaticConstructor( writer, className, names, values, valueColumn.DataTypeName );

				// methods
				writeGetNameFromValueMethod( writer, valueColumn.DataTypeName );
				writeGetValueFromNameMethod( writer, valueColumn.DataTypeName );
				if( orderIsSpecified ) {
					writeGetValuesToNamesMethod( writer, valueColumn.DataTypeName );
					writeFillListControlMethod( writer );
				}

				writer.WriteLine( "}" ); // class
			}
			writer.WriteLine( "}" ); // namespace
		}

		private static void writeStaticConstructor( TextWriter writer, string className, List<string> names, List<string> values, string valueTypeName ) {
			writer.WriteLine( "static " + className + "() {" );

			for( var i = 0; i < names.Count; i++ )
				writer.WriteLine( dictionaryName + ".Add( (" + valueTypeName + ")" + values[ i ] + ", \"" + names[ i ] + "\" );" );

			writer.WriteLine( "}" ); // constructor
		}

		private static void writeGetNameFromValueMethod( TextWriter writer, string valueTypeName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "Returns the name of the constant given the constant's value." );
			const string parameterName = "constantValue";
			writer.WriteLine( "public static string GetNameFromValue( " + valueTypeName + " " + parameterName + " ) {" );
			writer.WriteLine( "return " + dictionaryName + ".GetRightFromLeft( " + parameterName + " );" );
			writer.WriteLine( "}" ); // method
		}

		private static void writeGetValueFromNameMethod( TextWriter writer, string valueTypeName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "Returns the value of the constant given the constant's name." );
			const string parameterName = "constantName";
			writer.WriteLine( "public static " + valueTypeName + " GetValueFromName( string " + parameterName + " ) {" );
			writer.WriteLine( "return " + dictionaryName + ".GetLeftFromRight( " + parameterName + " );" );
			writer.WriteLine( "}" ); // method
		}

		private static void writeGetValuesToNamesMethod( TextWriter writer, string valueTypeName ) {
			CodeGenerationStatics.AddSummaryDocComment( writer,
			                                            "Returns a list of key value pairs where the key is the value of the row constant and the value is the name of the row constant." );
			writer.WriteLine( "public static ICollection<KeyValuePair<" + valueTypeName + ", string>> GetValuesToNames() {" );
			writer.WriteLine( "return valuesAndNames.GetAllPairs();" );
			writer.WriteLine( "}" ); // method
		}

		private static void writeFillListControlMethod( TextWriter writer ) {
			CodeGenerationStatics.AddSummaryDocComment( writer, "Adds all of the name value pairs to the given ListControl." );
			writer.WriteLine( "public static void FillListControl( EwfListControl listControl ) {" );
			writer.WriteLine( "foreach( var pair in valuesAndNames.GetAllPairs() )" );
			writer.WriteLine( "listControl.AddItem( pair.Value, pair.Key.ToString() );" );
			writer.WriteLine( "}" ); // method
		}
	}
}