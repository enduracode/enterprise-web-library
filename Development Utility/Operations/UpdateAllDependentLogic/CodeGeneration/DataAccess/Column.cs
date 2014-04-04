using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess {
	internal class Column {
		/// <summary>
		/// If includeKeyInfo is true, all key columns for involved tables will be returned even if they were not selected.
		/// </summary>
		internal static List<Column> GetColumnsInQueryResults( DBConnection cn, string commandText, bool includeKeyInfo ) {
			var cmd = DataAccessStatics.GetCommandFromRawQueryText( cn, commandText );
			var columns = new List<Column>();

			var readerMethod = new Action<DbDataReader>( r => {
				foreach( DataRow row in r.GetSchemaTable().Rows )
					columns.Add( new Column( row, includeKeyInfo, cn.DatabaseInfo ) );
			} );
			if( includeKeyInfo )
				cn.ExecuteReaderCommandWithKeyInfoBehavior( cmd, readerMethod );
			else
				cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, readerMethod );

			return columns;
		}

		private readonly string name;
		private readonly string pascalCasedName;
		private readonly int size;
		private readonly bool? isKey;
		private readonly Type dataType;
		private readonly string dbTypeString;
		private readonly bool allowsNull;
		private readonly bool isIdentity;

		// We'll remove this when we're ready to migrate Oracle systems to Pascal-cased column names.
		private readonly string pascalCasedNameExceptForOracle;

		private Column( DataRow schemaTableRow, bool includeKeyInfo, DatabaseInfo databaseInfo ) {
			name = (string)schemaTableRow[ "ColumnName" ];
			pascalCasedName = databaseInfo is OracleInfo ? name.OracleToEnglish().EnglishToPascal() : Name;
			pascalCasedNameExceptForOracle = databaseInfo is OracleInfo ? name : pascalCasedName;
			size = (int)schemaTableRow[ "ColumnSize" ];
			// MySQL longtext returns -1
			if( size < 0 )
				size = int.MaxValue;
			if( includeKeyInfo )
				isKey = (bool)schemaTableRow[ "IsKey" ];
			dataType = (Type)schemaTableRow[ "DataType" ];
			dbTypeString = databaseInfo.GetDbTypeString( schemaTableRow[ "ProviderType" ] );
			allowsNull = (bool)schemaTableRow[ "AllowDBNull" ];
			isIdentity = ( databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsIdentity" ] ) ||
			             ( databaseInfo is MySqlInfo && (bool)schemaTableRow[ "IsAutoIncrement" ] );
		}

		internal string Name { get { return name; } }
		internal string PascalCasedName { get { return pascalCasedName; } }
		internal string PascalCasedNameExceptForOracle { get { return pascalCasedNameExceptForOracle; } }
		internal string CamelCasedName { get { return pascalCasedName.LowercaseString(); } }
		internal int Size { get { return size; } }
		internal bool IsKey { get { return isKey.Value; } }

		/// <summary>
		/// If this column has a nullability mismatch, returns the name of a nullable version of the data type for this column.
		/// Otherwise, returns the name of the data type for this column (the same as DataTypeIfNotNullName).
		/// </summary>
		internal string DataTypeName {
			get {
				if( HasNullabilityMismatch )
					return NullableDataTypeName;
				return dataType.ToString();
			}
		}

		/// <summary>
		/// Returns the string representing the nullable data type for this column, regardless of whether the column allows null in the database.
		/// This will be equivalent to DataTypeName if the column allows null in the database.
		/// </summary>
		internal string NullableDataTypeName { get { return dataType.IsValueType ? dataType + "?" : dataType.ToString(); } }

		/// <summary>
		/// Returns true if the column allows null in the database, but the corresponding C# datatype is a value type.
		/// </summary>
		internal bool HasNullabilityMismatch { get { return dataType.IsValueType && allowsNull; } }

		/// <summary>
		/// Returns the name of the data type for this column, regardless of whether this column has a nullability mismatch.
		/// </summary>
		internal string DataTypeIfNotNullName { get { return dataType.ToString(); } }

		internal string DbTypeString { get { return dbTypeString; } }
		internal bool AllowsNull { get { return allowsNull; } }
		internal bool IsIdentity { get { return isIdentity; } }

		// NOTE: It would be best to use primary keys here, but unfortunately we don't always have that information.
		//internal bool UseToUniquelyIdentifyRow { get { return !allowsNull && dataType.IsValueType /*We could use IsPrimitive if not for Oracle resolving to System.Decimal.*/; } }
		// Right now we assume that at least one column in table (or query) returns true for UseToUniquelyIdentifyRow. This might not always be the case, for example if you have a query
		// that selects file contents only. If we re-implement this in a way that makes our assumption false, we'll need to modify DataAccessStatics to detect the case where no
		// columns return true for this and provide a useful exception.
		internal bool UseToUniquelyIdentifyRow { get { return !dataType.IsArray; } }

		internal ModificationField GetModificationField() {
			var type = HasNullabilityMismatch ? typeof( Nullable<> ).MakeGenericType( dataType ) : dataType;
			return new ModificationField( type, DataTypeName, NullableDataTypeName, "", name, pascalCasedName, size );
		}
	}
}