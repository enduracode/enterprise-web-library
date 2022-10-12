using ComponentSpace.Saml2.Configuration;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Saml {
	/// <summary>
	/// Application-specific SAML logic.
	/// </summary>
	public abstract class AppSamlProvider {
		protected internal virtual IReadOnlyCollection<SamlConfiguration> GetCustomConfigurations() => Enumerable.Empty<SamlConfiguration>().Materialize();
	}
}