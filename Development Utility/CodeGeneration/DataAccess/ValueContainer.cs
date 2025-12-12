using EnterpriseWebLibrary.DatabaseSpecification;
using EnterpriseWebLibrary.DatabaseSpecification.Databases;
using NodaTime;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal class ValueContainer {
	private readonly string name;
	private readonly string pascalCasedName;

	private readonly Type dataType;
	private readonly Type unconvertedDataType;
	private readonly Func<string, string> incomingValueConversionExpressionGetter;
	private readonly Func<object, object> incomingValueConverter;
	private readonly Func<string, string>? outgoingValueConversionExpressionGetter;
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
		else if( databaseInfo is SqlServerInfo or MySqlInfo
			         ? string.Equals( dbTypeString, "Date", StringComparison.Ordinal )
			         : databaseInfo is OracleInfo && dbTypeString.Equals( "Date", StringComparison.Ordinal ) && hasSuffix( "Date" ) ) {
			if( unconvertedDataType != typeof( DateTime ) )
				throw new Exception( $"The unconverted data type was not {nameof(DateTime)}." );

			this.dataType = typeof( LocalDate );
			incomingValueConversionExpressionGetter = valueExpression => $"LocalDate.FromDateTime( (DateTime){valueExpression} )";
			incomingValueConverter = value => LocalDate.FromDateTime( (DateTime)value );
			outgoingValueConversionExpressionGetter = valueExpression => $"{valueExpression}.ToDateTimeUnspecified()";
		}
		else if( databaseInfo is SqlServerInfo or MySqlInfo && dbTypeString.Equals( "Time", StringComparison.Ordinal ) ) {
			if( unconvertedDataType != typeof( TimeSpan ) )
				throw new Exception( $"The unconverted data type was not {nameof(TimeSpan)}." );

			this.dataType = typeof( LocalTime );
			incomingValueConversionExpressionGetter = valueExpression => $"LocalTime.FromTicksSinceMidnight( ( (TimeSpan){valueExpression} ).Ticks )";
			incomingValueConverter = value => LocalTime.FromTicksSinceMidnight( ( (TimeSpan)value ).Ticks );
			outgoingValueConversionExpressionGetter = valueExpression => $"new TimeSpan( {valueExpression}.TickOfDay )";
		}
		else if( dataType == typeof( DateTime ) && ( ( databaseInfo is OracleInfo && dbTypeString.Equals( "TimeStamp", StringComparison.Ordinal ) ) ||
		                                             ( hasSuffix( "Time" ) && !hasSuffix( "DateAndTime" ) && !hasSuffix( "DateTime" ) ) ||
		                                             hasSuffix( "Instant" ) ) ) {
			this.dataType = typeof( Instant );
			incomingValueConversionExpressionGetter = valueExpression => $"LocalDateTime.FromDateTime( (DateTime){valueExpression} ).InUtc().ToInstant()";
			incomingValueConverter = value => LocalDateTime.FromDateTime( (DateTime)value ).InUtc().ToInstant();
			outgoingValueConversionExpressionGetter = valueExpression => $"{valueExpression}.ToDateTimeUtc()";
		}
		else if( databaseInfo is SqlServerInfo ? dataType == typeof( DateTime ) :
		         databaseInfo is MySqlInfo ? dbTypeString.Equals( "DateTime", StringComparison.Ordinal ) :
		         databaseInfo is OracleInfo && dbTypeString.Equals( "Date", StringComparison.Ordinal ) ) {
			if( unconvertedDataType != typeof( DateTime ) )
				throw new Exception( $"The unconverted data type was not {nameof(DateTime)}." );

			this.dataType = typeof( LocalDateTime );
			incomingValueConversionExpressionGetter = valueExpression => $"LocalDateTime.FromDateTime( (DateTime){valueExpression} )";
			incomingValueConverter = value => LocalDateTime.FromDateTime( (DateTime)value );
			outgoingValueConversionExpressionGetter = valueExpression => $"{valueExpression}.ToDateTimeUnspecified()";
		}
		else {
			this.dataType = unconvertedDataType;
			incomingValueConversionExpressionGetter = valueExpression => "({0}){1}".FormatWith( dataType, valueExpression );
			incomingValueConverter = value => value;
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

	private bool hasSuffix( string suffix, string contains = "" ) => ModificationField.NameHasSuffix( pascalCasedName, suffix, contains );

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
		if( valueExpression.Equals( "null", StringComparison.Ordinal ) )
			return getExpression( valueExpression );

		if( outgoingValueConversionExpressionGetter is not null && allowsNull && allowsEmpty is null )
			return getExpression( $"{valueExpression} is {{}} __nonnullable ? {outgoingValueConversionExpressionGetter( "__nonnullable" )} : null" );
		var conversionExpression = outgoingValueConversionExpressionGetter?.Invoke( valueExpression ) ?? valueExpression;
		return allowsNull && allowsEmpty == false
			       ? getExpression( $"{conversionExpression} is {{ Length: > 0 }} __nonempty ? __nonempty : null" )
			       : getExpression( conversionExpression );

		string getExpression( string parameterValueExpression ) => $"new DbParameterValue( {parameterValueExpression}, \"{dbTypeString}\" )";
	}

	public string GetNullabilityPhrase() =>
		!allowsEmpty.HasValue ? allowsNull ? "can be null" : "cannot be null" :
		allowsNull || allowsEmpty.Value ? "cannot be null but CAN be empty" : "cannot be null or empty";
}