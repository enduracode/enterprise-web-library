using System.ComponentModel;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Collections;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching;

/// <summary>
/// EWL use only.
/// </summary>
public class VersionedRowDataCache<RowPkType, RowPkAndVersionType, RowType>: PeriodicEvictionCompositeCacheEntry where RowPkAndVersionType: notnull {
	[ EditorBrowsable( EditorBrowsableState.Never ) ]
	// We need structural comparison here because SQL Server rowversion maps to a byte array in .NET.
	public readonly Cache<RowPkAndVersionType, RowType> RowsByPkAndVersion = new( true, comparer: new StructuralEqualityComparer<RowPkAndVersionType>() );

	private readonly Func<RowPkAndVersionType, RowPkType> pkSelector;
	private readonly IEqualityComparer<RowPkType>? pkCrossVersionComparer;

	public VersionedRowDataCache( Func<RowPkAndVersionType, RowPkType> pkSelector, IEqualityComparer<RowPkType>? pkCrossVersionComparer ) {
		this.pkSelector = pkSelector;
		this.pkCrossVersionComparer = pkCrossVersionComparer;
	}

	void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
		var pkGroups = RowsByPkAndVersion.Keys.GroupBy( pkSelector, pkCrossVersionComparer ).Select( i => new { pk = i.Key, keys = i.ToArray() } );
		foreach( var pkAndKeys in pkGroups ) {
			// When we remove row versions for a primary key, we remove all of them because we don't ever really know which versions to keep. In some cases, the
			// latest versions of a row could all be from within a transaction that is going to roll back. 
			if( pkAndKeys.keys.Length <= 5 )
				continue;
			foreach( var key in pkAndKeys.keys )
				RowsByPkAndVersion.Remove( key );
		}
	}
}