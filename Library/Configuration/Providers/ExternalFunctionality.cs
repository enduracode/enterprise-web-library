using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.MySql;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.Configuration.Providers;

[ UsedImplicitly ]
internal class ExternalFunctionality: SystemExternalFunctionalityProvider {
	protected override ExternalMySqlProvider GetMySqlProvider() => new MySqlProvider();
}