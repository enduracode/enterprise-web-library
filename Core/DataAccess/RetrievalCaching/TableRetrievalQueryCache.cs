using System.ComponentModel;
using EnterpriseWebLibrary.Collections;
using EnterpriseWebLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching;

/// <summary>
/// EWL use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public class TableRetrievalQueryCache<RowType> {
	private readonly Cache<InlineDbCommandCondition[], IReadOnlyCollection<RowType>> cache = new(
		false,
		comparer: new StructuralEqualityComparer<InlineDbCommandCondition[]>() );

	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public TableRetrievalQueryCache() {}

	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public IEnumerable<RowType> GetResultSet(
		IEnumerable<InlineDbCommandCondition> conditions, Func<IReadOnlyCollection<InlineDbCommandCondition>, IReadOnlyCollection<RowType>> resultSetCreator ) {
		var conditionArray = conditions.OrderBy( i => i ).ToArray();
		return cache.GetOrAdd( conditionArray, () => resultSetCreator( conditionArray ) );
	}
}