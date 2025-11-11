using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.MySql;
using EnterpriseWebLibrary.OracleDatabase;
using EnterpriseWebLibrary.Pdf;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.Providers;

[ UsedImplicitly ]
internal class ExternalFunctionality: SystemExternalFunctionalityProvider {
	protected override ExternalMySqlProvider GetMySqlProvider() => new MySqlProvider();
	protected override ExternalOracleDatabaseProvider GetOracleDatabaseProvider() => new OracleDatabaseProvider();
	protected override ExternalPdfProvider? GetPdfProvider() => new PdfProvider();
}