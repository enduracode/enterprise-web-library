using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.Collections;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	public class QueryRetrievalQueryCache<RowType> {
		private readonly Cache<object[], IEnumerable<RowType>> cache;

		public QueryRetrievalQueryCache() {
			cache = new Cache<object[], IEnumerable<RowType>>( comparer: new StructuralEqualityComparer<object[]>() );
		}

		public IEnumerable<RowType> GetResultSet( object[] parameterValues, Func<IEnumerable<RowType>> resultSetCreator ) {
			return cache.GetOrAddValue( parameterValues, resultSetCreator );
		}
	}
}