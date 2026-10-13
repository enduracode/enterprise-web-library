using JetBrains.Annotations;

namespace EnterpriseWebLibrary.EnterpriseWebFramework;

/// <summary>
/// System-specific logic for JSON serialization of resources.
/// </summary>
[ UsedImplicitly( ImplicitUseTargetFlags.WithInheritors ) ]
public interface SystemResourceSerializationProvider {
	( string name, string parameters )? SerializeResource( ResourceParent item );
	ResourceParent? DeserializeResource( string name, string parameters );
}