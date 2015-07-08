using System;
using System.Collections.Generic;
using System.Data;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction {
	/// <summary>
	/// Internal and Development Utility use only.
	/// </summary>
	public class ProcedureParameter {
		private readonly ValueContainer valueContainer;
		private readonly ParameterDirection direction;

		internal ProcedureParameter( DBConnection cn, string name, string dataTypeFromGetSchema, int size, ParameterDirection direction ) {
			var table = cn.GetSchema( "DataTypes" );
			var rows = new List<DataRow>();
			foreach( DataRow r in table.Rows ) {
				if( (string)r[ "TypeName" ] == dataTypeFromGetSchema )
					rows.Add( r );
			}
			if( rows.Count != 1 )
				throw new ApplicationException( "There must be exactly one data type row matching the specified data type name." );
			var row = rows[ 0 ];
			var dataType = Type.GetType( (string)row[ "DataType" ], true );
			var dbTypeString = cn.DatabaseInfo.GetDbTypeString( row[ "ProviderDbType" ] );
			var allowsNull = (bool)row[ "IsNullable" ];

			valueContainer = new ValueContainer( name, dataType, dbTypeString, size, allowsNull, cn.DatabaseInfo );
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
}