using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Humanizer;
using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess {
	internal class Column {
		/// <summary>
		/// If includeKeyInfo is true, all key columns for involved tables will be returned even if they were not selected.
		/// </summary>
		internal static List<Column> GetColumnsInQueryResults( DBConnection cn, string commandText, bool includeKeyInfo ) {
			var cmd = DataAccessStatics.GetCommandFromRawQueryText( cn, commandText );
			var columns = new List<Column>();

			var readerMethod = new Action<DbDataReader>(
				r => {
					foreach( DataRow row in r.GetSchemaTable().Rows )
						columns.Add( new Column( row, includeKeyInfo, cn.DatabaseInfo ) );
				} );
			if( includeKeyInfo )
				cn.ExecuteReaderCommandWithKeyInfoBehavior( cmd, readerMethod );
			else
				cn.ExecuteReaderCommandWithSchemaOnlyBehavior( cmd, readerMethod );

			return columns;
		}

		private readonly int ordinal;
		private readonly ValueContainer valueContainer;
		private readonly bool isIdentity;
		private readonly bool isRowVersion;
		private readonly bool? isKey;

		private Column( DataRow schemaTableRow, bool includeKeyInfo, DatabaseInfo databaseInfo ) {
			ordinal = (int)schemaTableRow[ "ColumnOrdinal" ];

			// MySQL incorrectly uses one-based ordinals; see http://bugs.mysql.com/bug.php?id=61477.
			if( databaseInfo is MySqlInfo )
				ordinal -= 1;

			valueContainer = new ValueContainer(
				(string)schemaTableRow[ "ColumnName" ],
				(Type)schemaTableRow[ "DataType" ],
				databaseInfo.GetDbTypeString( schemaTableRow[ "ProviderType" ] ),
				(int)schemaTableRow[ "ColumnSize" ],
				(bool)schemaTableRow[ "AllowDBNull" ],
				databaseInfo );
			isIdentity = ( databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsIdentity" ] ) ||
			             ( databaseInfo is MySqlInfo && (bool)schemaTableRow[ "IsAutoIncrement" ] );
			isRowVersion = databaseInfo is SqlServerInfo && (bool)schemaTableRow[ "IsRowVersion" ];
			if( includeKeyInfo )
				isKey = (bool)schemaTableRow[ "IsKey" ];
		}

		internal string Name { get { return valueContainer.Name; } }
		internal string PascalCasedName { get { return valueContainer.PascalCasedName; } }
		internal string PascalCasedNameExceptForOracle { get { return valueContainer.PascalCasedNameExceptForOracle; } }
		internal string CamelCasedName { get { return valueContainer.CamelCasedName; } }

		/// <summary>
		/// Gets the name of the data type for this column, or the nullable data type if the column allows null.
		/// </summary>
		internal string DataTypeName { get { return valueContainer.DataTypeName; } }

		/// <summary>
		/// Gets the name of the nullable data type for this column, regardless of whether the column allows null. The nullable data type is equivalent to the data
		/// type if the latter is a reference type or if the null value is represented with an expression other than "null".
		/// </summary>
		internal string NullableDataTypeName { get { return valueContainer.NullableDataTypeName; } }

		internal string NullValueExpression { get { return valueContainer.NullValueExpression; } }
		internal string UnconvertedDataTypeName { get { return valueContainer.UnconvertedDataTypeName; } }

		internal string GetIncomingValueConversionExpression( string valueExpression ) {
			return valueContainer.GetIncomingValueConversionExpression( valueExpression );
		}

		internal object ConvertIncomingValue( object value ) {
			return valueContainer.ConvertIncomingValue( value );
		}

		internal int Size { get { return valueContainer.Size; } }
		internal bool AllowsNull { get { return valueContainer.AllowsNull; } }
		internal bool IsIdentity { get { return isIdentity; } }
		internal bool IsRowVersion { get { return isRowVersion; } }
		internal bool IsKey { get { return isKey.Value; } }

		// NOTE: It would be best to use primary keys here, but unfortunately we don't always have that information.
		//internal bool UseToUniquelyIdentifyRow { get { return !allowsNull && dataType.IsValueType /*We could use IsPrimitive if not for Oracle resolving to System.Decimal.*/; } }
		// Right now we assume that at least one column in table (or query) returns true for UseToUniquelyIdentifyRow. This might not always be the case, for example if you have a query
		// that selects file contents only. If we re-implement this in a way that makes our assumption false, we'll need to modify DataAccessStatics to detect the case where no
		// columns return true for this and provide a useful exception.
		internal bool UseToUniquelyIdentifyRow { get { return !valueContainer.DataType.IsArray && !isRowVersion; } }

		internal string GetCommandColumnValueExpression( string valueExpression ) {
			return "new InlineDbCommandColumnValue( \"{0}\", {1} )".FormatWith( valueContainer.Name, valueContainer.GetParameterValueExpression( valueExpression ) );
		}

		internal string GetDataReaderValueExpression( string readerName, int? ordinalOverride = null ) {
			var getValueExpression = valueContainer.GetIncomingValueConversionExpression( "{0}.GetValue( {1} )".FormatWith( readerName, ordinalOverride ?? ordinal ) );
			return valueContainer.AllowsNull
				       ? "{0}.IsDBNull( {1} ) ? {2} : {3}".FormatWith(
					       readerName,
					       ordinalOverride ?? ordinal,
					       valueContainer.NullValueExpression.Any() ? valueContainer.NullValueExpression : "({0})null".FormatWith( valueContainer.NullableDataTypeName ),
					       getValueExpression )
				       : getValueExpression;
		}

		internal ModificationField GetModificationField() {
			var type = valueContainer.DataTypeName != valueContainer.DataType.ToString()
				           ? typeof( Nullable<> ).MakeGenericType( valueContainer.DataType )
				           : valueContainer.DataType;
			return new ModificationField(
				type,
				valueContainer.DataTypeName,
				valueContainer.NullableDataTypeName,
				"",
				valueContainer.Name,
				valueContainer.PascalCasedName,
				valueContainer.Size );
		}
	}
}