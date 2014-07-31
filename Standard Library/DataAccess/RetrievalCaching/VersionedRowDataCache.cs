using System.ComponentModel;
using RedStapler.StandardLibrary.Collections;

namespace RedStapler.StandardLibrary.DataAccess.RetrievalCaching {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public class VersionedRowDataCache<RowKeyAndVersionType, RowType> {
		[ EditorBrowsable( EditorBrowsableState.Never ) ]
		// We need structural comparison here because SQL Server rowversion maps to a byte array in .NET.
		public readonly Cache<RowKeyAndVersionType, RowType> RowsByPkAndVersion = new Cache<RowKeyAndVersionType, RowType>(
			true,
			comparer: new StructuralEqualityComparer<RowKeyAndVersionType>() );
	}
}