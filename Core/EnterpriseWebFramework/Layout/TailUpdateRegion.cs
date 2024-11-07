using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class TailUpdateRegion {
	internal readonly UpdateRegionSetsParameter Sets;
	internal readonly int UpdatingItemCount;

	/// <summary>
	/// Creates a tail update region, which you can use to append items to a list/table, truncate a list/table, and/or modify the items at the end of a
	/// list/table.
	/// </summary>
	/// <param name="sets"></param>
	/// <param name="updatingItemCount">Pass zero if you only want to append items.</param>
	public TailUpdateRegion( UpdateRegionSetsParameter sets, int updatingItemCount ) {
		Sets = sets;
		UpdatingItemCount = updatingItemCount;
	}
}

public class TailUpdateRegionsParameter {
	public static implicit operator TailUpdateRegionsParameter( TailUpdateRegion? tailUpdateRegion ) =>
		new( tailUpdateRegion is null ? [ ] : [ tailUpdateRegion ] );

	private readonly IEnumerable<TailUpdateRegion> sequence;
	internal readonly Lazy<IReadOnlyCollection<TailUpdateRegion>> Collection;

	internal TailUpdateRegionsParameter( IEnumerable<TailUpdateRegion> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<TailUpdateRegion>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s tail update regions plus the specified regions.
	/// </summary>
	public TailUpdateRegionsParameter Add( TailUpdateRegionsParameter tailUpdateRegions ) => new( sequence.Concat( tailUpdateRegions.sequence ) );
}

[ PublicAPI ]
public static class TailUpdateRegionsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this tail update region plus the specified regions.
	/// </summary>
	public static TailUpdateRegionsParameter Add( this TailUpdateRegion tailUpdateRegion, TailUpdateRegionsParameter tailUpdateRegions ) =>
		new TailUpdateRegionsParameter( [ tailUpdateRegion ] ).Add( tailUpdateRegions );

	/// <summary>
	/// Returns a parameter with the tail update regions in this sequence.
	/// </summary>
	public static TailUpdateRegionsParameter ToParameter( this IEnumerable<TailUpdateRegion> tailUpdateRegions ) => new( tailUpdateRegions );
}