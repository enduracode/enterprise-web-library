using System.Data;
using EnterpriseWebLibrary.DatabaseSpecification;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

internal class ProcedureParameter {
	private readonly ValueContainer valueContainer;
	private readonly ParameterDirection direction;

	public ProcedureParameter( DatabaseInfo databaseInfo, string name, DataRow dataTypeRow, int size, ParameterDirection direction ) {
		var dataType = Type.GetType( (string)dataTypeRow[ "DataType" ], true )!;
		var dbTypeString = databaseInfo.GetDbTypeString( dataTypeRow[ "ProviderDbType" ] );
		var allowsNull = (bool)dataTypeRow[ "IsNullable" ];

		valueContainer = new ValueContainer( name, dataType, dbTypeString, size, null, allowsNull, databaseInfo );
		this.direction = direction;
	}

	public string Name { get { return valueContainer.Name; } }
	public string DataTypeName { get { return valueContainer.DataTypeName; } }
	public string UnconvertedDataTypeName { get { return valueContainer.UnconvertedDataTypeName; } }

	public string GetIncomingValueConversionExpression( string valueExpression ) {
		return valueContainer.GetIncomingValueConversionExpression( valueExpression );
	}

	public ParameterDirection Direction { get { return direction; } }

	public string GetParameterValueExpression( string valueExpression ) {
		return valueContainer.GetParameterValueExpression( valueExpression );
	}
}