using System.Collections.Generic;
using System.Data;
using System.Linq;
using RedStapler.StandardLibrary.DatabaseSpecification;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;

namespace RedStapler.StandardLibrary.DataAccess.CommandWriting.InlineConditionAbstraction.Conditions {
	/// <summary>
	/// ISU and Standard Library use only.
	/// </summary>
	public class LikeCondition: InlineDbCommandCondition {
		/// <summary>
		/// This enum is accessible to developers of systems.
		/// </summary>
		public enum Behavior {
			/// <summary>
			/// Treats the entire search term as though it were in quotes. Equivalent to 'table.Column LIKE '%' + @columnValue + '%' in SQL Server.
			/// </summary>
			SingleTerm,

			/// <summary>
			/// Breaks the search string into tokens and performs N number of LIKE comparisons, And-ing them together.
			/// This will return results that successfully match each individual token. Double quotes can be used to force
			/// tokens (or the entire term) to be treated as a single token.
			/// </summary>
			AndedTokens
		}

		private readonly Behavior behavior;
		private readonly string columnName;
		private readonly string searchTerm;

		/// <summary>
		/// ISU use only.
		/// </summary>
		public LikeCondition( Behavior behavior, string columnName, string searchTerm ) {
			this.behavior = behavior;
			this.columnName = columnName;
			this.searchTerm = searchTerm;
		}

		void InlineDbCommandCondition.AddToCommand( IDbCommand command, DatabaseInfo databaseInfo, string parameterName ) {
			var tokens = new List<string>();
			if( behavior == Behavior.SingleTerm )
				tokens.Add( searchTerm.Trim() );
			else {
				// NOTE: We want to improve the separation logic here to deal with odd characters in a good way, and to escape certain characters (per-database). See Task 1913.
				tokens.AddRange( searchTerm.Separate() );
			}

			// NOTE: We may have to do some casing stuff for Oracle because existing queries seem to do UPPER on everything.
			var concatCharacter = databaseInfo is SqlServerInfo ? "+" : "||";
			var parameterNumber = 0;
			var newCommandText = "";
			// NOTE: Is it important to tell the user they've been capped? How would we do that?
			foreach( var token in tokens.Take( 20 /*Google allows many more tokens than this.*/ ) ) {
				var parameter = new DbCommandParameter( parameterName + "L" + parameterNumber++,
				                                        new DbParameterValue( token.Truncate( 128 /*This is Google's cap on word length.*/ ) ) );
				newCommandText = StringTools.ConcatenateWithDelimiter( " AND ",
				                                                       newCommandText,
				                                                       ( columnName + " LIKE '%' {0} " + parameter.GetNameForCommandText( databaseInfo ) + " {0} '%'" ).
				                                                       	FormatWith( concatCharacter ) );
				command.Parameters.Add( parameter.GetAdoDotNetParameter( databaseInfo ) );
			}
			command.CommandText += newCommandText;
		}
	}
}