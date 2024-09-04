using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

[ PublicAPI ]
public sealed class ElementIdSet: ElementIdReference {
	private readonly List<string> ids = new();

	/// <summary>
	/// Creates an element-ID set.
	/// </summary>
	public ElementIdSet() {}

	/// <summary>
	/// Adds an element’s client-side ID to this set. ElementData use only.
	/// </summary>
	internal override void AddId( string id ) {
		PageBase.AssertPageTreeNotBuilt();
		ids.Add( id );
	}

	/// <summary>
	/// Gets the element client-side IDs in this set, which are not available until after the page tree has been built.
	/// </summary>
	public IReadOnlyCollection<string> Ids {
		get {
			PageBase.AssertPageTreeBuilt();
			return ids;
		}
	}
}