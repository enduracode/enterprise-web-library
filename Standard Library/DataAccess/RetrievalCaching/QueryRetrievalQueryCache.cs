using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.Collections;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	public class QueryRetrievalQueryCache<KeyType, RowType> {
		private readonly Cache<KeyType, IEnumerable<RowType>> cache;

		public QueryRetrievalQueryCache() {
			cache = new Cache<KeyType, IEnumerable<RowType>>();
		}

		public IEnumerable<RowType> GetResultSet( KeyType parameterValues, Func<IEnumerable<RowType>> resultSetCreator ) {
			return cache.GetOrAddValue( parameterValues, resultSetCreator );
		}
	}
}