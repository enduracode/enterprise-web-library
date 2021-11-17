using System.Collections.Generic;
using System.Linq;
using ComponentSpace.SAML2.Configuration;
using Tewl.Tools;

namespace EnterpriseWebLibrary.Saml {
	/// <summary>
	/// Application-specific SAML logic.
	/// </summary>
	public abstract class AppSamlProvider {
		protected internal virtual IReadOnlyCollection<SAMLConfiguration> GetCustomConfigurations() => Enumerable.Empty<SAMLConfiguration>().Materialize();
	}
}