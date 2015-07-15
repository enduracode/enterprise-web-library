using System;
using System.ComponentModel;
using System.Linq;
using EnterpriseWebLibrary.Caching;
using EnterpriseWebLibrary.Collections;

namespace EnterpriseWebLibrary.DataAccess.RetrievalCaching {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public class VersionedRowDataCache<RowPkType, RowPkAndVersionType, RowType>: PeriodicEvictionCompositeCacheEntry {
		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		// We need structural comparison here because SQL Server rowversion maps to a byte array in .NET.
		public readonly Cache<RowPkAndVersionType, RowType> RowsByPkAndVersion = new Cache<RowPkAndVersionType, RowType>(
			true,
			comparer: new StructuralEqualityComparer<RowPkAndVersionType>() );

		private readonly Func<RowPkAndVersionType, RowPkType> pkSelector;

		public VersionedRowDataCache( Func<RowPkAndVersionType, RowPkType> pkSelector ) {
			this.pkSelector = pkSelector;
		}

		void PeriodicEvictionCompositeCacheEntry.EvictOldEntries() {
			var pkGroups = from key in RowsByPkAndVersion.Keys group key by pkSelector( key ) into pkGroup select new { pk = pkGroup.Key, keys = pkGroup.ToArray() };
			foreach( var pkAndKeys in pkGroups ) {
				// When we remove row versions for a primary key, we remove all of them because we don't ever really know which versions to keep. In some cases, the
				// latest versions of a row could all be from within a transaction that is going to roll back. 
				if( pkAndKeys.keys.Count() <= 5 )
					continue;
				foreach( var key in pkAndKeys.keys )
					RowsByPkAndVersion.Remove( key );
			}
		}
	}
}