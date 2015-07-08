using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RedStapler.StandardLibrary.Collections;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class TableRetrievalQueryCache<RowType> {
		private readonly Cache<InlineDbCommandCondition[], IEnumerable<RowType>> cache;

		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public TableRetrievalQueryCache() {
			cache = new Cache<InlineDbCommandCondition[], IEnumerable<RowType>>( false, comparer: new StructuralEqualityComparer<InlineDbCommandCondition[]>() );
		}

		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public IEnumerable<RowType> GetResultSet(
			IEnumerable<InlineDbCommandCondition> conditions, Func<IEnumerable<InlineDbCommandCondition>, IEnumerable<RowType>> resultSetCreator ) {
			var conditionArray = conditions.OrderBy( i => i ).ToArray();
			return cache.GetOrAdd( conditionArray, () => resultSetCreator( conditionArray ) );
		}
	}
}