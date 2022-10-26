using System.Data.Common;

namespace EnterpriseWebLibrary.ExternalFunctionality {
	/// <summary>
	/// External MySQL logic.
	/// </summary>
	public interface ExternalMySqlProvider {
		DbProviderFactory GetDbProviderFactory();
	}
}