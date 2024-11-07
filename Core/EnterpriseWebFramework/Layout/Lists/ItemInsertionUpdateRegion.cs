using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class ItemInsertionUpdateRegion {
	internal readonly UpdateRegionSetsParameter Sets;
	internal readonly Func<IEnumerable<string>> NewItemIdGetter;

	/// <summary>
	/// Creates an item-insertion update region.
	/// </summary>
	/// <param name="sets"></param>
	/// <param name="newItemIdGetter">A method that executes after the data modification and returns the IDs of the new item(s).</param>
	public ItemInsertionUpdateRegion( UpdateRegionSetsParameter sets, Func<IEnumerable<string>> newItemIdGetter ) {
		Sets = sets;
		NewItemIdGetter = newItemIdGetter;
	}
}

public class ItemInsertionUpdateRegionsParameter {
	public static implicit operator ItemInsertionUpdateRegionsParameter( ItemInsertionUpdateRegion? itemInsertionUpdateRegion ) =>
		new( itemInsertionUpdateRegion is null ? [ ] : [ itemInsertionUpdateRegion ] );

	private readonly IEnumerable<ItemInsertionUpdateRegion> sequence;
	internal readonly Lazy<IReadOnlyCollection<ItemInsertionUpdateRegion>> Collection;

	internal ItemInsertionUpdateRegionsParameter( IEnumerable<ItemInsertionUpdateRegion> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<ItemInsertionUpdateRegion>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s item-insertion update regions plus the specified regions.
	/// </summary>
	public ItemInsertionUpdateRegionsParameter Add( ItemInsertionUpdateRegionsParameter itemInsertionUpdateRegions ) =>
		new( sequence.Concat( itemInsertionUpdateRegions.sequence ) );
}

[ PublicAPI ]
public static class ItemInsertionUpdateRegionsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this item-insertion update region plus the specified regions.
	/// </summary>
	public static ItemInsertionUpdateRegionsParameter Add(
		this ItemInsertionUpdateRegion itemInsertionUpdateRegion, ItemInsertionUpdateRegionsParameter itemInsertionUpdateRegions ) =>
		new ItemInsertionUpdateRegionsParameter( [ itemInsertionUpdateRegion ] ).Add( itemInsertionUpdateRegions );

	/// <summary>
	/// Returns a parameter with the item-insertion update regions in this sequence.
	/// </summary>
	public static ItemInsertionUpdateRegionsParameter ToParameter( this IEnumerable<ItemInsertionUpdateRegion> itemInsertionUpdateRegions ) =>
		new( itemInsertionUpdateRegions );
}