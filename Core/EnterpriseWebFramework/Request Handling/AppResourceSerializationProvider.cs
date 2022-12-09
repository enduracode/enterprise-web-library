namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// Application-specific logic for JSON serialization of resources.
	/// </summary>
	public interface AppResourceSerializationProvider {
		( string name, string parameters )? SerializeResource( ResourceBase resource );
		ResourceBase DeserializeResource( string name, string parameters );
	}
}