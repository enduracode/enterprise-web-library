using System.Data;
using EnterpriseWebLibrary.DataAccess;

namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

/// <summary>
/// Internal and Development Utility use only.
/// </summary>
public class ProcedureParameter {
	private readonly ValueContainer valueContainer;
	private readonly ParameterDirection direction;

	internal ProcedureParameter( DatabaseConnection cn, string name, DataRow dataTypeRow, int size, ParameterDirection direction ) {
		var dataType = Type.GetType( (string)dataTypeRow[ "DataType" ], true )!;
		var dbTypeString = cn.DatabaseInfo.GetDbTypeString( dataTypeRow[ "ProviderDbType" ] );
		var allowsNull = (bool)dataTypeRow[ "IsNullable" ];

		valueContainer = new ValueContainer( name, dataType, dbTypeString, size, null, allowsNull, cn.DatabaseInfo );
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