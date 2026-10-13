using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.MySql;
using EnterpriseWebLibrary.OracleDatabase;
using EnterpriseWebLibrary.Pdf;
using EnterpriseWebLibrary.Sqlite;

namespace EnterpriseWebLibrary.Providers;

class ExternalFunctionality: SystemExternalFunctionalityProvider {
	protected override ExternalMySqlProvider GetMySqlProvider() => new MySqlProvider();
	protected override ExternalOracleDatabaseProvider GetOracleDatabaseProvider() => new OracleDatabaseProvider();
	protected override ExternalSqliteProvider? GetSqliteProvider() => new SqliteProvider();
	protected override ExternalPdfProvider? GetPdfProvider() => new PdfProvider();
}