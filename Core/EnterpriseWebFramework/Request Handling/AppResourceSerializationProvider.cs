using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// Application-specific logic for JSON serialization of resources.
/// </summary>
[ UsedImplicitly( ImplicitUseTargetFlags.WithInheritors ) ]
public interface AppResourceSerializationProvider {
	( string name, string parameters )? SerializeResource( ResourceParent item );
	ResourceParent? DeserializeResource( string name, string parameters );
}