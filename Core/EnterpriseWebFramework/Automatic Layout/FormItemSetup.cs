namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// The configuration for a form item.
/// </summary>
public class FormItemSetup {
	internal readonly DisplaySetup? DisplaySetup;
	internal readonly int? ColumnSpan;
	internal readonly IEnumerable<UpdateRegionSet>? UpdateRegionSets;
	internal readonly TextAlignment TextAlignment;

	/// <summary>
	/// Creates a form item setup object.
	/// </summary>
	/// <param name="displaySetup"></param>
	/// <param name="columnSpan">Only applies to <see cref="FormItemList.CreateFixedGrid"/>.</param>
	/// <param name="updateRegionSets">The intermediate-post-back update-region sets that the form item will be a part of.</param>
	/// <param name="textAlignment"></param>
	public FormItemSetup(
		DisplaySetup? displaySetup = null, int? columnSpan = null, IEnumerable<UpdateRegionSet>? updateRegionSets = null,
		TextAlignment textAlignment = TextAlignment.NotSpecified ) {
		DisplaySetup = displaySetup;
		ColumnSpan = columnSpan;
		UpdateRegionSets = updateRegionSets;
		TextAlignment = textAlignment;
	}
}