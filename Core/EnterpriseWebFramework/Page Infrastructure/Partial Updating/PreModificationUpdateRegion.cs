namespace EnterpriseWebLibrary.EnterpriseWebFramework;

internal class PreModificationUpdateRegion {
	internal readonly UpdateRegionSetsParameter? Sets;
	internal readonly Func<IEnumerable<PageComponent>> ComponentGetter;
	internal readonly Func<string> ArgumentGetter;

	internal PreModificationUpdateRegion( UpdateRegionSetsParameter? sets, Func<IEnumerable<PageComponent>> componentGetter, Func<string> argumentGetter ) {
		Sets = sets;
		ComponentGetter = componentGetter;
		ArgumentGetter = argumentGetter;
	}
}