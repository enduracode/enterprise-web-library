using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using MySql.Data.MySqlClient;

namespace EnterpriseWebLibrary.MySql {
	public class MySqlProvider: ExternalMySqlProvider {
		DbProviderFactory ExternalMySqlProvider.GetDbProviderFactory() => MySqlClientFactory.Instance;
	}
}