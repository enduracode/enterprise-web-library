using System;
using System.Collections.Generic;
using RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.Commands {
	/// <summary>
	/// Standard Library use only.
	/// </summary>
	public class InlineDelete: InlineDbCommandWithConditions {
		private readonly string tableName;
		private readonly List<InlineDbCommandCondition> conditions = new List<InlineDbCommandCondition>();

		/// <summary>
		/// Creates a modification that will execute an inline DELETE statement.
		/// </summary>
		public InlineDelete( string tableName ) {
			this.tableName = tableName;
		}

		/// <summary>
		/// Standard Library use only.
		/// </summary>
		public void AddCondition( InlineDbCommandCondition condition ) {
			conditions.Add( condition );
		}

		/// <summary>
		/// Executes this command against the specified database connection and returns the number of rows affected.
		/// </summary>
		public int Execute( DBConnection cn ) {
			if( conditions.Count == 0 )
				throw new ApplicationException( "Executing an inline delete command with no parameters in the where clause is not allowed." );
			var command = cn.DatabaseInfo.CreateCommand();
			command.CommandText = "DELETE FROM " + tableName + " WHERE ";
			var paramNumber = 0;
			foreach( var condition in conditions ) {
				condition.AddToCommand( command, cn.DatabaseInfo, InlineUpdate.GetParamNameFromNumber( paramNumber++ ) );
				command.CommandText += " AND ";
			}
			command.CommandText = command.CommandText.Remove( command.CommandText.Length - 5 );
			return cn.ExecuteNonQueryCommand( command );
		}
	}
}