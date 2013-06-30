using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.Collections;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	public class TableRetrievalQueryCache<RowType> {
		private readonly Cache<InlineDbCommandCondition[], IEnumerable<RowType>> cache;

		public TableRetrievalQueryCache() {
			cache = new Cache<InlineDbCommandCondition[], IEnumerable<RowType>>( comparer: new StructuralEqualityComparer<InlineDbCommandCondition[]>() );
		}

		public IEnumerable<RowType> GetResultSet( IEnumerable<InlineDbCommandCondition> conditions,
		                                          Func<IEnumerable<InlineDbCommandCondition>, IEnumerable<RowType>> resultSetCreator ) {
			var conditionArray = conditions.OrderBy( i => i ).ToArray();
			return cache.GetOrAddValue( conditionArray, () => resultSetCreator( conditionArray ) );
		}
	}
}