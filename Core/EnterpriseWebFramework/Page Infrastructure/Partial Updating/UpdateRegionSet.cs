using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public class UpdateRegionSet {
	/// <summary>
	/// Creates an update-region set.
	/// </summary>
	public UpdateRegionSet() {}
}

public class UpdateRegionSetsParameter {
	public static implicit operator UpdateRegionSetsParameter( UpdateRegionSet? updateRegionSet ) => new( updateRegionSet is null ? [ ] : [ updateRegionSet ] );

	private readonly IEnumerable<UpdateRegionSet> sequence;
	internal readonly Lazy<IReadOnlyCollection<UpdateRegionSet>> Collection;

	internal UpdateRegionSetsParameter( IEnumerable<UpdateRegionSet> sequence ) {
		this.sequence = sequence;
		Collection = new Lazy<IReadOnlyCollection<UpdateRegionSet>>( sequence.Materialize );
	}

	/// <summary>
	/// Returns a new parameter with this parameter’s update-region sets plus the specified sets.
	/// </summary>
	public UpdateRegionSetsParameter Add( UpdateRegionSetsParameter updateRegionSets ) => new( sequence.Concat( updateRegionSets.sequence ) );
}

[ PublicAPI ]
public static class UpdateRegionSetsParameterExtensionCreators {
	/// <summary>
	/// Returns a parameter with this update-region set plus the specified sets.
	/// </summary>
	public static UpdateRegionSetsParameter Add( this UpdateRegionSet updateRegionSet, UpdateRegionSetsParameter updateRegionSets ) =>
		new UpdateRegionSetsParameter( [ updateRegionSet ] ).Add( updateRegionSets );

	/// <summary>
	/// Returns a parameter with the update-region sets in this sequence.
	/// </summary>
	public static UpdateRegionSetsParameter ToParameter( this IEnumerable<UpdateRegionSet> updateRegionSets ) => new( updateRegionSets );
}