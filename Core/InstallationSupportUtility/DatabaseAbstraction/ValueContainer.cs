using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using NodaTime;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

/// <summary>
/// Internal and Development Utility use only.
/// </summary>
public class ValueContainer {
	private readonly string name;
	private readonly string pascalCasedName;

	private readonly Type dataType;
	private readonly Type unconvertedDataType;
	private readonly Func<string, string> incomingValueConversionExpressionGetter;
	private readonly Func<object, object> incomingValueConverter;
	private readonly Func<string, string> outgoingValueConversionExpressionGetter;
	private readonly string dbTypeString;

	private readonly int size;
	private readonly short? numericScale;
	private readonly bool allowsNull;
	private readonly bool? allowsEmpty;

	public ValueContainer( string name, Type dataType, string dbTypeString, int size, short? numericScale, bool allowsNull, DatabaseInfo databaseInfo ) {
		this.name = name;
		pascalCasedName = databaseInfo is OracleInfo ? name.OracleToEnglish().EnglishToPascal() : name;
		unconvertedDataType = dataType;

		// MySQL LONGTEXT returns -1 for size.
		if( databaseInfo is MySqlInfo && dbTypeString == "Text" && size == -1 )
			size = int.MaxValue;

		if( databaseInfo is MySqlInfo && dbTypeString == "Bit" && size == 1 ) {
			if( unconvertedDataType != typeof( ulong ) )
				throw new Exception( "The unconverted data type was not ulong." );

			this.dataType = typeof( bool );
			incomingValueConversionExpressionGetter = valueExpression => "Convert.ToBoolean( {0} )".FormatWith( valueExpression );
			incomingValueConverter = value => Convert.ToBoolean( value );
			outgoingValueConversionExpressionGetter = valueExpression => "Convert.ToUInt64( {0} )".FormatWith( valueExpression );
		}
		else if( databaseInfo is SqlServerInfo && string.Equals( dbTypeString, "Date", StringComparison.Ordinal ) ) {
			if( unconvertedDataType != typeof( DateTime ) )
				throw new Exception( $"The unconverted data type was not {nameof(DateTime)}." );

			this.dataType = typeof( LocalDate );
			incomingValueConversionExpressionGetter = valueExpression => $"LocalDate.FromDateTime( (DateTime){valueExpression} )";
			incomingValueConverter = value => LocalDate.FromDateTime( (DateTime)value );
			outgoingValueConversionExpressionGetter = valueExpression => $"{valueExpression}.ToDateTimeUnspecified()";
		}
		else {
			this.dataType = unconvertedDataType;
			incomingValueConversionExpressionGetter = valueExpression => "({0}){1}".FormatWith( dataType, valueExpression );
			incomingValueConverter = value => value;
			outgoingValueConversionExpressionGetter = valueExpression => valueExpression;
		}

		this.dbTypeString = dbTypeString;
		this.size = size;
		this.numericScale = numericScale;
		this.allowsNull = allowsNull;

		allowsEmpty = dataType != typeof( string )
			              ? null
			              : databaseInfo switch
				              {
					              MySqlInfo => !string.Equals( dbTypeString, "JSON", StringComparison.Ordinal ),
					              OracleInfo => allowsNull || new[] { "Clob", "NClob" }.Contains( dbTypeString, StringComparer.Ordinal ),
					              _ => true
				              };
	}

	public string Name => name;
	public string PascalCasedName => pascalCasedName;
	public string CamelCasedName => pascalCasedName.Uncapitalize();

	public Type DataType => dataType;

	/// <summary>
	/// Gets the name of the data type for this container, or the nullable data type if the container allows null.
	/// </summary>
	public string DataTypeName => allowsNull ? NullableDataTypeName : dataType.ToString();

	/// <summary>
	/// Gets the name of the nullable data type for this container, regardless of whether the container allows null. If the data type is string, the nullable data
	/// type is also string since the null value is represented with the empty string.
	/// </summary>
	public string NullableDataTypeName => dataType == typeof( string ) ? dataType.ToString() : dataType + "?";

	public string UnconvertedDataTypeName => unconvertedDataType.ToString();

	public string GetIncomingValueConversionExpression( string valueExpression ) {
		return incomingValueConversionExpressionGetter( valueExpression );
	}

	public object ConvertIncomingValue( object value ) {
		return incomingValueConverter( value );
	}

	public int Size => size;
	public short? NumericScale => numericScale;
	public bool AllowsNull => allowsNull;
	public bool? AllowsEmpty => allowsEmpty;

	public string GetParameterValueExpression( string valueExpression ) {
		var conversionExpression = outgoingValueConversionExpressionGetter( valueExpression );
		if( allowsNull && allowsEmpty == false )
			conversionExpression += " is { Length: > 0 } nonempty ? nonempty : null";
		var parameterValueExpression = valueExpression == "null" ? valueExpression :
		                               conversionExpression == valueExpression || !allowsNull || allowsEmpty.HasValue ? conversionExpression :
		                               "{0} is null ? null : {1}".FormatWith( valueExpression, conversionExpression );
		return "new DbParameterValue( {0}, \"{1}\" )".FormatWith( parameterValueExpression, dbTypeString );
	}

	public string GetNullabilityPhrase() =>
		!allowsEmpty.HasValue ? allowsNull ? "can be null" : "cannot be null" :
		allowsNull || allowsEmpty.Value ? "cannot be null but CAN be empty" : "cannot be null or empty";
}