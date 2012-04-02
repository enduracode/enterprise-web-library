using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public interface InlineDbCommandCondition {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		void AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName );
	}
}