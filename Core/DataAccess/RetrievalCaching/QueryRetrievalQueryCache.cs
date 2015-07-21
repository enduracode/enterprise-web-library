using System;
using System.Collections.Generic;
using System.ComponentModel;
using EnterpriseWebLibrary.Collections;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class QueryRetrievalQueryCache<RowType> {
		private readonly Cache<object[], IEnumerable<RowType>> cache;

		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public QueryRetrievalQueryCache() {
			cache = new Cache<object[], IEnumerable<RowType>>( false, comparer: new StructuralEqualityComparer<object[]>() );
		}

		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public IEnumerable<RowType> GetResultSet( object[] parameterValues, Func<IEnumerable<RowType>> resultSetCreator ) {
			return cache.GetOrAdd( parameterValues, resultSetCreator );
		}
	}
}