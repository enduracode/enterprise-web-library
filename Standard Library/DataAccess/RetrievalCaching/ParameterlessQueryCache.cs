using System;
using System.Collections.Generic;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	public class ParameterlessQueryCache<RowType> {
		private IEnumerable<RowType> resultSet;

		public IEnumerable<RowType> GetResultSet( Func<IEnumerable<RowType>> resultSetCreator ) {
			return resultSet ?? ( resultSet = resultSetCreator() );
		}
	}
}