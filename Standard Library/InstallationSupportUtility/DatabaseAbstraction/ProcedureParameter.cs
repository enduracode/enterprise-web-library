using System;
using System.Collections.Generic;
using System.Data;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.InstallationSupportUtility.DatabaseAbstraction {
	public class ProcedureParameter {
		private readonly string name;
		private readonly Type dataType;
		private readonly string dbTypeString;
		private readonly bool allowsNull;
		private readonly ParameterDirection direction;

		internal ProcedureParameter( DBConnection cn, string name, string dataTypeFromGetSchema, ParameterDirection direction ) {
			this.name = name;
			this.direction = direction;

			var table = cn.GetSchema( "DataTypes" );
			var rows = new List<DataRow>();
			foreach( DataRow r in table.Rows ) {
				if( (string)r[ "TypeName" ] == dataTypeFromGetSchema )
					rows.Add( r );
			}
			if( rows.Count != 1 )
				throw new ApplicationException( "There must be exactly one data type row matching the specified data type name." );
			var row = rows[ 0 ];
			dataType = Type.GetType( (string)row[ "DataType" ], true );
			dbTypeString = cn.DatabaseInfo.GetDbTypeString( row[ "ProviderDbType" ] );
			allowsNull = (bool)row[ "IsNullable" ];
		}

		public string Name { get { return name; } }

		public string DataTypeName { get { return dataType.IsValueType && allowsNull ? dataType + "?" : dataType.ToString(); } }

		public string DataTypeDefaultValueExpression { get { return dataType.IsValueType ? "new " + DataTypeName + "()" : "null"; } }

		public string DbTypeString { get { return dbTypeString; } }

		public ParameterDirection Direction { get { return direction; } }
	}
}