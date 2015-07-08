using System;
using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction {
	/// <summary>
	/// EWL use only.
	/// </summary>
	public interface InlineDbCommandCondition: IEquatable<InlineDbCommandCondition>, IComparable, IComparable<InlineDbCommandCondition> {
		/// <summary>
		/// EWL use only.
		/// </summary>
		void AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName );
	}
}