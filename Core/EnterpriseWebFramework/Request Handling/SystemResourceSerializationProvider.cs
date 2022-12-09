namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// System-specific logic for JSON serialization of resources.
	/// </summary>
	public interface SystemResourceSerializationProvider {
		( string name, string parameters )? SerializeResource( ResourceBase resource );
		ResourceBase DeserializeResource( string name, string parameters );
	}
}