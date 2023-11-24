using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using EnterpriseWebLibrary.Caching;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching;

/// <summary>
/// EWL use only.
/// </summary>
[ PublicAPI ]
[ EditorBrowsable( EditorBrowsableState.Never ) ]
public class RowModificationCountTableCache<RowType, RowPkType>: PeriodicEvictionCompositeCacheEntry
	where RowType: TableRetrievalRow<RowPkType> where RowPkType: notnull {
	private class Cache {
		public readonly IReadOnlyCollection<RowType> Rows;
		public readonly IReadOnlyDictionary<RowPkType, RowType> RowsByPk;
		public readonly IReadOnlyDictionary<RowPkType, int> RowModificationCountsByPk;

		public Cache( IEnumerable<RowType> rows, IEnumerable<( RowPkType pk, int count )> rowModificationCounts ) {
			Rows = rows.ToArray();
			RowsByPk = Rows.ToDictionary( i => i.PrimaryKey );
			RowModificationCountsByPk = rowModificationCounts.ToDictionary( i => i.pk, i => i.count );
		}
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	[ PublicAPI ]
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public class DataRetriever {
		private readonly IReadOnlyCollection<RowType> rows;
		private readonly IReadOnlyDictionary<RowPkType, RowType> rowsByPk;
		private readonly HashSet<RowPkType> modifiedRowPks;
		private readonly Lazy<IReadOnlyCollection<RowType>> modifiedRows;
		private readonly Lazy<IReadOnlyDictionary<RowPkType, RowType>> modifiedRowsByPk;

		internal DataRetriever(
			IReadOnlyCollection<RowType> rows, IReadOnlyDictionary<RowPkType, RowType> rowsByPk, HashSet<RowPkType> modifiedRowPks,
			Func<IEnumerable<RowPkType>, IReadOnlyCollection<RowType>> modifiedRowGetter ) {
			this.rows = rows;
			this.rowsByPk = rowsByPk;

			this.modifiedRowPks = modifiedRowPks;
			modifiedRows = new Lazy<IReadOnlyCollection<RowType>>(
				() => modifiedRowPks.Count == 0 ? Enumerable.Empty<RowType>().Materialize() : modifiedRowGetter( modifiedRowPks ),
				LazyThreadSafetyMode.None );
			modifiedRowsByPk = new Lazy<IReadOnlyDictionary<RowPkType, RowType>>(
				() => modifiedRows.Value.ToDictionary( i => i.PrimaryKey ),
				LazyThreadSafetyMode.None );
		}

		/// <summary>
		/// EWL use only. To ensure validity of results, you must call this within the same transaction in which the object was created (due to lazy loading of
		/// modified rows).
		/// </summary>
		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public IEnumerable<RowType> GetRows() => rows.Where( i => !modifiedRowPks.Contains( i.PrimaryKey ) ).Concat( modifiedRows.Value );

		/// <summary>
		/// EWL use only. To ensure validity of results, you must call this within the same transaction in which the object was created (due to lazy loading of
		/// modified rows).
		/// </summary>
		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		public bool TryGetRowMatchingPk( RowPkType pk, [ MaybeNullWhen( false ) ] out RowType row ) =>
			modifiedRowPks.Contains( pk ) ? modifiedRowsByPk.Value.TryGetValue( pk, out row ) : rowsByPk.TryGetValue( pk, out row );
	}

	private Cache cache;
	private readonly Func<Cache> cacheRecreator;

	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public RowModificationCountTableCache(
		IEnumerable<RowType> rows, IEnumerable<( RowPkType, int )> rowModificationCounts,
		Action<Action<IReadOnlyCollection<( RowPkType, int )>, Func<IEnumerable<RowPkType>, IReadOnlyCollection<RowType>>>> cacheRecreator ) {
		cache = new Cache( rows, rowModificationCounts );
		this.cacheRecreator = () => {
			Cache? newCache = null;
			cacheRecreator( ( counts, rowGetter ) => newCache = new Cache( GetDataRetriever( counts, rowGetter ).GetRows(), counts ) );
			return newCache!;
		};
	}

	/// <summary>
	/// EWL use only.
	/// </summary>
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	public DataRetriever GetDataRetriever(
		IEnumerable<( RowPkType pk, int count )> rowModificationCounts, Func<IEnumerable<RowPkType>, IReadOnlyCollection<RowType>> modifiedRowGetter ) {
		var cacheStable = cache;
		var modifiedRowPks = rowModificationCounts.Where( i => !cacheStable.RowModificationCountsByPk.TryGetValue( i.pk, out var count ) || count != i.count )
			.Select( i => i.pk )
			.ToHashSet();
		return new DataRetriever( cacheStable.Rows, cacheStable.RowsByPk, modifiedRowPks, modifiedRowGetter );
	}

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		Interlocked.Exchange( ref cache, cacheRecreator() );
	}
}