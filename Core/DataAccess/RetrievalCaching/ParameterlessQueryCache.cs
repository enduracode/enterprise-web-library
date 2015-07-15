using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class ParameterlessQueryCache<RowType> {
		private IEnumerable<RowType> resultSet;

		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public IEnumerable<RowType> GetResultSet( Func<IEnumerable<RowType>> resultSetCreator ) {
			return resultSet ?? ( resultSet = resultSetCreator() );
		}
	}
}