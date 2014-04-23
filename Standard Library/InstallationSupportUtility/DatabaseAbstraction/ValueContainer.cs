using System;
using System.Linq;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction {
	/// <summary>
	/// Internal and Development Utility use only.
	/// </summary>
	public class ValueContainer {
		private readonly string name;
		private readonly string pascalCasedName;

		private readonly Type dataType;
		private readonly string nullValueExpression;
		private readonly Type unconvertedDataType;
		private readonly Func<string, string> incomingValueConversionExpressionGetter;
		private readonly Func<object, object> incomingValueConverter;
		private readonly Func<string, string> outgoingValueConversionExpressionGetter;
		private readonly string dbTypeString;

		private readonly int size;
		private readonly bool allowsNull;

		// We'll remove this when we're ready to migrate Oracle systems to Pascal-cased column names.
		private readonly string pascalCasedNameExceptForOracle;

		public ValueContainer( string name, Type dataType, string dbTypeString, int size, bool allowsNull, DatabaseInfo databaseInfo ) {
			this.name = name;
			pascalCasedName = databaseInfo is OracleInfo ? name.OracleToEnglish().EnglishToPascal() : name;
			pascalCasedNameExceptForOracle = databaseInfo is OracleInfo ? name : pascalCasedName;
			unconvertedDataType = dataType;
			this.dbTypeString = dbTypeString;
			nullValueExpression = databaseInfo is OracleInfo && new[] { "Clob", "NClob" }.Contains( dbTypeString ) ? "\"\"" : "";
			this.size = size;

			// MySQL longtext returns -1
			if( size < 0 )
				size = int.MaxValue;

			if( databaseInfo is MySqlInfo && dbTypeString == "Bit" && size == 1 ) {
				if( unconvertedDataType != typeof( ulong ) )
					throw new ApplicationException( "The unconverted data type was not ulong." );

				this.dataType = typeof( bool );
				incomingValueConversionExpressionGetter = valueExpression => "System.Convert.ToBoolean( {0} )".FormatWith( valueExpression );
				incomingValueConverter = value => Convert.ToBoolean( value );
				outgoingValueConversionExpressionGetter = valueExpression => "System.Convert.ToUInt64( {0} )".FormatWith( valueExpression );
			}
			else {
				this.dataType = unconvertedDataType;
				incomingValueConversionExpressionGetter = valueExpression => "({0}){1}".FormatWith( DataTypeName, valueExpression );
				incomingValueConverter = value => value;
				outgoingValueConversionExpressionGetter = valueExpression => valueExpression;
			}

			this.allowsNull = allowsNull;
		}

		public string Name { get { return name; } }
		public string PascalCasedName { get { return pascalCasedName; } }
		public string PascalCasedNameExceptForOracle { get { return pascalCasedNameExceptForOracle; } }
		public string CamelCasedName { get { return pascalCasedName.LowercaseString(); } }

		public Type DataType { get { return dataType; } }

		/// <summary>
		/// Gets the name of the data type for this container, or the nullable data type if the container allows null.
		/// </summary>
		public string DataTypeName { get { return allowsNull ? NullableDataTypeName : dataType.ToString(); } }

		/// <summary>
		/// Gets the name of the nullable data type for this container, regardless of whether the container allows null. The nullable data type is equivalent to the
		/// data type if the latter is a reference type or if the null value is represented with an expression other than "null".
		/// </summary>
		public string NullableDataTypeName { get { return dataType.IsValueType && !nullValueExpression.Any() ? dataType + "?" : dataType.ToString(); } }

		public string NullValueExpression { get { return nullValueExpression; } }

		public string UnconvertedDataTypeName { get { return unconvertedDataType.ToString(); } }

		public string GetIncomingValueConversionExpression( string valueExpression ) {
			return incomingValueConversionExpressionGetter( valueExpression );
		}

		public object ConvertIncomingValue( object value ) {
			return incomingValueConverter( value );
		}

		public int Size { get { return size; } }
		public bool AllowsNull { get { return allowsNull; } }

		public string GetParameterValueExpression( string valueExpression ) {
			var conversionExpression = outgoingValueConversionExpressionGetter( valueExpression );
			var parameterValueExpression = valueExpression == "null"
				                               ? valueExpression
				                               : conversionExpression == valueExpression || ( dataType.IsValueType && ( nullValueExpression.Any() || !allowsNull ) )
					                                 ? conversionExpression
					                                 : "{0} != null ? {1} : null".FormatWith(
						                                 valueExpression,
						                                 dataType.IsValueType ? "({0}?){1}".FormatWith( UnconvertedDataTypeName, conversionExpression ) : conversionExpression );
			return "new DbParameterValue( {0}, \"{1}\" )".FormatWith( parameterValueExpression, dbTypeString );
		}
	}
}