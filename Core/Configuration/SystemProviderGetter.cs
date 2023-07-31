using System.Reflection;

namespace EnterpriseWebLibrary.Configuration;

/// <summary>
/// EWL use only.
/// </summary>
public class SystemProviderGetter {
	private readonly Assembly assembly;
	private readonly string providerNamespace;
	private readonly Func<string, string> errorMessageGetter;

	internal SystemProviderGetter( Assembly assembly, string providerNamespace, Func<string, string> errorMessageGetter ) {
		this.assembly = assembly;
		this.providerNamespace = providerNamespace;
		this.errorMessageGetter = errorMessageGetter;
	}

	public SystemProviderReference<ProviderType> GetProvider<ProviderType>( string providerName ) where ProviderType: class {
		var typeName = providerNamespace + "." + providerName;
		var provider = assembly.GetType( typeName ) != null ? assembly.CreateInstance( typeName ) as ProviderType : null;
		return new SystemProviderReference<ProviderType>( provider, errorMessageGetter( providerName ) );
	}
}