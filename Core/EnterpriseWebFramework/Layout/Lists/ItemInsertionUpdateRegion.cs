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