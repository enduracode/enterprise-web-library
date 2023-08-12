﻿using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility;

namespace EnterpriseWebLibrary.DevelopmentUtility.Operations.CodeGeneration.DataAccess;

internal class TableColumns {
	internal readonly IEnumerable<Column> AllColumns;

	private readonly List<Column> keyColumns = new();

	/// <summary>
	/// Returns either all components of the primary key, or the identity (alone).
	/// </summary>
	internal IEnumerable<Column> KeyColumns => keyColumns;

	private readonly Column? identityColumn;
	internal Column? IdentityColumn => identityColumn;

	internal readonly Column? RowVersionColumn;
	internal readonly IEnumerable<Column> AllColumnsExceptRowVersion;
	internal readonly IEnumerable<Column> AllNonIdentityColumnsExceptRowVersion;

	private readonly Column? primaryKeyAndRevisionIdColumn;
	internal Column? PrimaryKeyAndRevisionIdColumn => primaryKeyAndRevisionIdColumn;

	private readonly IEnumerable<Column> dataColumns;

	/// <summary>
	/// Gets all columns that are not the identity column, the row version column, or the primary key and revision ID column.
	/// </summary>
	internal IEnumerable<Column> DataColumns => dataColumns;

	internal TableColumns( DBConnection cn, string table, bool forRevisionHistoryLogic ) {
		try {
			// This hack allows code to be generated against a database that is configured for ASP.NET Application Services.
			var isAspNetApplicationServicesTable = table.StartsWith( "aspnet_" );

			// NOTE: Cache this result.
			AllColumns = Column.GetColumnsInQueryResults( cn, "SELECT * FROM " + table, true, !isAspNetApplicationServicesTable );

			// Identify key, identity, and non identity columns.
			var nonIdentityColumns = new List<Column>();
			foreach( var col in AllColumns ) {
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

			RowVersionColumn = AllColumns.SingleOrDefault( i => i.IsRowVersion );
			AllColumnsExceptRowVersion = AllColumns.Where( i => !i.IsRowVersion ).ToArray();
			AllNonIdentityColumnsExceptRowVersion = nonIdentityColumns.Where( i => !i.IsRowVersion ).ToArray();

			if( forRevisionHistoryLogic ) {
				if( keyColumns.Count != 1 )
					throw new UserCorrectableException(
						"A revision history modification class can only be created for tables with exactly one primary key column, which is assumed to also be a foreign key to the revisions table." );
				primaryKeyAndRevisionIdColumn = keyColumns.Single();
				if( primaryKeyAndRevisionIdColumn.IsIdentity )
					throw new UserCorrectableException( "The revision ID column of a revision history table must not be an identity." );
			}

			dataColumns = AllColumns.Where( col => col is { IsIdentity: false, IsRowVersion: false } && col != primaryKeyAndRevisionIdColumn ).ToArray();
		}
		catch( Exception e ) {
			throw UserCorrectableException.CreateSecondaryException( "An exception occurred while getting columns for table " + table + ".", e );
		}
	}
}