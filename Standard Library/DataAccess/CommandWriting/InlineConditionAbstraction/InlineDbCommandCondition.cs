using System;
using System.Data;
using RedStapler.StandardLibrary.DatabaseSpecification;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public interface InlineDbCommandCondition: IEquatable<InlineDbCommandCondition>, IComparable, IComparable<InlineDbCommandCondition> {
		/// <summary>
		/// Standard Library use only.
		/// </summary>
		void AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName );
	}
}