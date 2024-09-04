namespace EnterpriseWebLibrary.EnterpriseWebFramework;

public sealed class ElementId: ElementIdReference {
	private string id = "";

	/// <summary>
	/// Creates an element-ID reference.
	/// </summary>
	public ElementId() {}

	/// <summary>
	/// Adds the element’s client-side ID. This can only be called once. ElementData use only.
	/// </summary>
	internal override void AddId( string id ) {
		PageBase.AssertPageTreeNotBuilt();
		if( this.id.Length > 0 )
			throw new ApplicationException( "The ID was already added." );
		this.id = id;
	}

	/// <summary>
	/// Gets the element’s client-side ID, or the empty string if no ID exists. Not available until after the page tree has been built.
	/// </summary>
	public string Id {
		get {
			PageBase.AssertPageTreeBuilt();
			return id;
		}
	}
}