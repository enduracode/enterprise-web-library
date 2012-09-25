using System;
using System.Collections.Generic;
using System.Linq;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.DatabaseSpecification.Databases;
using RedStapler.StandardLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess {
	internal class TableColumns {
		private readonly IEnumerable<Column> allColumns;
		internal IEnumerable<Column> AllColumns { get { return allColumns; } }

		private readonly List<Column> keyColumns = new List<Column>();

		/// <summary>
		/// Returns either all components of the primary key, or the identity (alone).
		/// </summary>
		internal IEnumerable<Column> KeyColumns { get { return keyColumns; } }

		private readonly Column identityColumn;
		internal Column IdentityColumn { get { return identityColumn; } }

		private readonly List<Column> nonIdentityColumns = new List<Column>();
		internal IEnumerable<Column> NonIdentityColumns { get { return nonIdentityColumns; } }

		private readonly Column primaryKeyAndRevisionIdColumn;
		internal Column PrimaryKeyAndRevisionIdColumn { get { return primaryKeyAndRevisionIdColumn; } }

		private readonly IEnumerable<Column> dataColumns;

		/// <summary>
		/// Gets all columns that are not the identity column or the primary key and revision ID column.
		/// </summary>
		internal IEnumerable<Column> DataColumns { get { return dataColumns; } }

		internal TableColumns( DBConnection cn, string table, bool forRevisionHistoryLogic ) {
			try {
				// NOTE: Cache this result.
				allColumns = Column.GetColumnsInQueryResults( cn, "SELECT * FROM " + table, true );

				foreach( var col in allColumns ) {
					// NOTE: Greg is responsible for determining if this is the only necessary hack for ASP.NET Application Services. See task 6184.
					var isAspNetApplicationServicesTable = table.StartsWith( "aspnet_" );

					if( cn.DatabaseInfo is SqlServerInfo && col.DataTypeName == typeof( string ).ToString() && col.AllowsNull && !isAspNetApplicationServicesTable )
						throw new UserCorrectableException( "String column " + col.Name + " allows null, which is not allowed." );
				}

				// Identify key, identity, and non identity columns.
				foreach( var col in allColumns ) {
					if( col.IsKey )
						keyColumns.Add( col );
					if( col.IsIdentity ) {
						if( identityColumn != null )
							throw new UserCorrectableException( "Only one identity column per table is supported." );
						identityColumn = col;
					}
					else
						nonIdentityColumns.Add( col );
				}
				if( !keyColumns.Any() )
					throw new UserCorrectableException( "The table must contain a primary key or other means of uniquely identifying a row." );

				// If the table has a composite key, try to use the identity as the key instead since this will enable InsertRow to return a value.
				if( identityColumn != null && keyColumns.Count > 1 ) {
					keyColumns.Clear();
					keyColumns.Add( identityColumn );
				}

				if( forRevisionHistoryLogic ) {
					if( keyColumns.Count != 1 ) {
						throw new UserCorrectableException(
							"A revision history modification class can only be created for tables with exactly one primary key column, which is assumed to also be a foreign key to the revisions table." );
					}
					primaryKeyAndRevisionIdColumn = keyColumns.Single();
					if( primaryKeyAndRevisionIdColumn.IsIdentity )
						throw new UserCorrectableException( "The revision ID column of a revision history table must not be an identity." );
				}

				dataColumns = allColumns.Where( col => !col.IsIdentity && col != primaryKeyAndRevisionIdColumn ).ToArray();
			}
			catch( Exception e ) {
				throw UserCorrectableException.CreateSecondaryException( "An exception occurred while getting columns for table " + table + ".", e );
			}
		}
	}
}