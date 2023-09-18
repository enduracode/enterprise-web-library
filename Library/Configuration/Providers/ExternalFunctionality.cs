using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.MySql;
using EnterpriseWebLibrary.OracleDatabase;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.Configuration.Providers;

[ UsedImplicitly ]
internal class ExternalFunctionality: SystemExternalFunctionalityProvider {
	protected override ExternalMySqlProvider GetMySqlProvider() => new MySqlProvider();
	protected override ExternalOracleDatabaseProvider GetOracleDatabaseProvider() => new OracleDatabaseProvider();
}