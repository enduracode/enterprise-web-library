using System.Data;
using System.Data.Common;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DataAccess.CommandWriting;
using EnterpriseWebLibrary.DataAccess.CommandWriting.Commands;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

class Column {
	internal static IReadOnlyCollection<Column> GetColumnsInQueryResults( DatabaseConnection cn, string commandText, bool isTableQuery ) {
		var columns = new List<Column>();

		var cmd = DataAccessStatics.GetCommandFromRawQueryText( cn, commandText );
		var validationMethods = new List<Action>();
		var readerMethod = new Action<DbDataReader>( r =>
			columns.AddRange( from DataRow row in r.GetSchemaTable()!.Rows select new Column( cn, row, isTableQuery, isTableQuery, validationMethods ) ) );
		if( isTableQuery )
			cn.ExecuteReaderCommandWithKeyInfoBehavior( cmd, readerMethod );
		else
			cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, readerMethod );

		foreach( var i in validationMethods )
			i();

		return columns;
	}

	private readonly int ordinal;
	private readonly ValueContainer valueContainer;
	private readonly string delimitedIdentifier;
	private readonly bool isIdentity;
	private readonly bool isRowVersion;
	private readonly bool? isKey;

	private Column( DatabaseConnection connection, DataRow schemaTableRow, bool includeKeyInfo, bool validateIfString, List<Action> validationMethods ) {
		var databaseInfo = connection.DatabaseInfo;

		ordinal = (int)schemaTableRow[ "ColumnOrdinal" ];

		var dbTypeString = databaseInfo.GetDbTypeString( schemaTableRow[ "ProviderType" ] );
		valueContainer = new ValueContainer(
			(string)schemaTableRow[ "ColumnName" ],
			(Type)schemaTableRow[ "DataType" ],
			dbTypeString,
			(int)schemaTableRow[ "ColumnSize" ],
			databaseInfo is SqlServerInfo && dbTypeString == "Decimal" ? (short)schemaTableRow[ "NumericScale" ] :
			databaseInfo is MySqlInfo && dbTypeString == "NewDecimal" ? (short)(int)schemaTableRow[ "NumericScale" ] :
			databaseInfo is OracleInfo && dbTypeString == "Decimal" ? (short)schemaTableRow[ "NumericScale" ] : null,
			(bool)schemaTableRow[ "AllowDBNull" ],
			databaseInfo );

		delimitedIdentifier = databaseInfo.GetDelimitedIdentifier( valueContainer.Name );
		isIdentity = ( databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsIdentity" ] ) ||
		             ( databaseInfo is MySqlInfo && (bool)schemaTableRow[ "IsAutoIncrement" ] );
		isRowVersion = databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsRowVersion" ];
		if( includeKeyInfo )
			isKey = (bool)schemaTableRow[ "IsKey" ];

		validationMethods.Add( () => {
			if( validateIfString && !( databaseInfo is OracleInfo ) && valueContainer.DataType == typeof( string ) &&
			    ( !( databaseInfo is MySqlInfo ) || dbTypeString != "JSON" ) && valueContainer.AllowsNull )
				throw new UserCorrectableException( "String column {0} allows null, which is not allowed.".FormatWith( valueContainer.Name ) );

			if( isKey is true ) {
				var error = getKeyError(
					connection,
					new DatabaseTable(
						schemaTableRow[ "BaseSchemaName" ] switch
							{
								DBNull => "",
								var schema => (string)schema
							},
						(string)schemaTableRow[ "BaseTableName" ] ),
					dbTypeString );
				if( error.Length > 0 )
					throw new UserCorrectableException( error );
			}
		} );
	}

	private string getKeyError( DatabaseConnection connection, DatabaseTable table, string dbTypeString ) {
		if( valueContainer.DataType != typeof( string ) )
			return "";

		var command = connection.DatabaseInfo is OracleInfo
			              ? new InlineSelect( "COLLATION".ToCollection(), table.Schema.Length > 0 ? "FROM ALL_TAB_COLUMNS" : "FROM USER_TAB_COLUMNS", false )
			              : new InlineSelect( "COLLATION_NAME".ToCollection(), "FROM INFORMATION_SCHEMA.COLUMNS", false );
		command.AddConditions(
			( table.Schema.Length > 0 ? getCondition( connection.DatabaseInfo is OracleInfo ? "OWNER" : "TABLE_SCHEMA", table.Schema ).ToCollection() : [ ] )
			.Append( getCondition( "TABLE_NAME", table.Name ) )
			.Append( getCondition( "COLUMN_NAME", valueContainer.Name ) ) );

		var error = "";
		command.Execute(
			connection,
			reader => {
				if( !reader.Read() )
					throw new Exception( "no rows exist" );

				(string[] types, Func<string, bool> collationPredicate, string error) rules = connection.DatabaseInfo switch
					{
						SqlServerInfo => ( [ "VarChar", "NVarChar" ],
							                 collation => collation.EndsWith( "_BIN2", StringComparison.OrdinalIgnoreCase ) ||
							                              collation.EndsWith( "_BIN2_UTF8", StringComparison.OrdinalIgnoreCase ),
							                 "varchar or nvarchar with a BIN2 collation" ),
						MySqlInfo => ( [ "VarChar" ], collation => collation.Equals( "utf8mb4_0900_bin", StringComparison.OrdinalIgnoreCase ),
							             "varchar with collation utf8mb4_0900_bin (binary, NO PAD)" ),
						OracleInfo => ( [ "Varchar2", "NVarchar2" ], collation => collation.Equals( "BINARY", StringComparison.OrdinalIgnoreCase ),
							              "VARCHAR2 or NVARCHAR2 with explicit BINARY column collation" ),
						_ => throw new UnexpectedValueException( "database info", connection.DatabaseInfo )
					};
				if( !rules.types.Contains( dbTypeString, StringComparer.Ordinal ) || !rules.collationPredicate( reader.GetString( 0 ) ) )
					error = $"Key column {valueContainer.Name} must use {rules.error}.";

				if( reader.Read() )
					throw new Exception( "multiple rows exist" );
			} );
		return error;

		static EqualityCondition getCondition( string columnName, string value ) =>
			new( new InlineDbCommandColumnValue( columnName, new DbParameterValue( value ) ) );
	}

	internal string Name => valueContainer.Name;
	internal string DelimitedIdentifier => delimitedIdentifier;
	internal string PascalCasedName => valueContainer.PascalCasedName;
	internal string CamelCasedName => valueContainer.CamelCasedName;

	/// <summary>
	/// Gets the name of the data type for this column, or the nullable data type if the column allows null.
	/// </summary>
	internal string DataTypeName => valueContainer.DataTypeName;

	/// <summary>
	/// Gets the name of the nullable data type for this column, regardless of whether the column allows null. If the data type is string, the nullable data type
	/// is also string since the null value is represented with the empty string.
	/// </summary>
	internal string NullableDataTypeName => valueContainer.NullableDataTypeName;

	internal string UnconvertedDataTypeName => valueContainer.UnconvertedDataTypeName;

	internal string GetIncomingValueConversionExpression( string valueExpression ) => valueContainer.GetIncomingValueConversionExpression( valueExpression );

	internal int Size => valueContainer.Size;
	internal bool AllowsNull => valueContainer.AllowsNull;
	internal bool? AllowsEmpty => valueContainer.AllowsEmpty;
	internal bool IsIdentity => isIdentity;
	internal bool IsRowVersion => isRowVersion;
	internal bool IsKey => isKey!.Value;

	// NOTE: It would be best to use primary keys here, but unfortunately we don't always have that information.
	//internal bool UseToUniquelyIdentifyRow { get { return !allowsNull && dataType.IsValueType /*We could use IsPrimitive if not for Oracle resolving to
	// System.Decimal.*/; } }
	// Right now we assume that at least one column in table (or query) returns true for UseToUniquelyIdentifyRow. This might not always be the case, for example
	// if you have a query
	// that selects file contents only. If we re-implement this in a way that makes our assumption false, we'll need to modify DataAccessStatics to detect the
	// case where no
	// columns return true for this and provide a useful exception.
	internal bool UseToUniquelyIdentifyRow => !valueContainer.DataType.IsArray && !isRowVersion;

	internal string GetCommandColumnValueExpression( string valueExpression ) =>
		"new InlineDbCommandColumnValue( \"{0}\", {1} )".FormatWith( valueContainer.Name, GetCommandParameterValueExpression( valueExpression ) );

	internal string GetCommandParameterValueExpression( string valueExpression ) => valueContainer.GetParameterValueExpression( valueExpression );

	internal string GetDataReaderValueExpression( string readerName, int? ordinalOverride = null ) {
		var getValueExpression = valueContainer.GetIncomingValueConversionExpression( "{0}.GetValue( {1} )".FormatWith( readerName, ordinalOverride ?? ordinal ) );
		return valueContainer.AllowsNull
			       ? "{0}.IsDBNull( {1} ) ? {2} : {3}".FormatWith(
				       readerName,
				       ordinalOverride ?? ordinal,
				       valueContainer.DataType == typeof( string ) ? "\"\"" : "({0})null".FormatWith( valueContainer.NullableDataTypeName ),
				       getValueExpression )
			       : getValueExpression;
	}

	internal string GetDataReaderValue( DbDataReader reader, bool forIdentifier = false ) {
		if( valueContainer.AllowsNull && reader.IsDBNull( ordinal ) )
			return forIdentifier ? throw new Exception( "null" ) : valueContainer.DataType == typeof( string ) ? "\"\"" : "null";

		var valueString = valueContainer.ConvertIncomingValue( reader.GetValue( ordinal ) ).ToString()!;
		return forIdentifier ? valueString :
		       valueContainer.DataType == typeof( string ) ? $"\"{valueString.EscapeForLiteral()}\"" :
		       valueContainer.DataType == typeof( Guid ) ? $"""new Guid( "{valueString}" )""" : valueString;
	}

	internal ModificationField GetModificationField( bool retainsPreviousValues ) {
		var type = valueContainer.DataType.IsValueType && valueContainer.AllowsNull
			           ? typeof( Nullable<> ).MakeGenericType( valueContainer.DataType )
			           : valueContainer.DataType;
		return new ModificationField(
			"column",
			retainsPreviousValues,
			valueContainer.Name,
			valueContainer.PascalCasedName,
			valueContainer.CamelCasedName,
			type,
			valueContainer.DataTypeName,
			valueContainer.NullableDataTypeName,
			"",
			valueContainer.Size,
			valueContainer.NumericScale );
	}

	internal string GetNullabilityPhrase() => valueContainer.GetNullabilityPhrase();
}