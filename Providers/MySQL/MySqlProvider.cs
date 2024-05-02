using System.Data.Common;
using EnterpriseWebLibrary.ExternalFunctionality;
using MySqlConnector;

namespace EnterpriseWebLibrary.MySql;

public class MySqlProvider: ExternalMySqlProvider {
	DbProviderFactory ExternalMySqlProvider.GetDbProviderFactory() => MySqlConnectorFactory.Instance;
}