using EnterpriseWebLibrary.DataAccess;
using EnterpriseWebLibrary.InstallationSupportUtility;
using EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

namespace EnterpriseWebLibrary.DevelopmentUtility.CodeGeneration.DataAccess;

class TableColumns {
	internal readonly IReadOnlyCollection<Column> AllColumns;

	private readonly List<Column> keyColumns = [ ];

	internal Column? IdentityColumn { get; }

	internal readonly Column? RowVersionColumn;
	internal readonly IEnumerable<Column> AllColumnsExceptRowVersion;
	internal readonly IEnumerable<Column> AllNonIdentityColumnsExceptRowVersion;

	internal Column? PrimaryKeyAndRevisionIdColumn { get; }

	/// <summary>
	/// Gets all columns that are not the identity column, the row version column, or the primary key and revision ID column.
	/// </summary>
	internal IEnumerable<Column> DataColumns { get; }

	internal TableColumns( DatabaseConnection cn, Database database, DatabaseTable table, bool forRevisionHistoryLogic ) {
		try {
			// NOTE: Cache this result.
			AllColumns = Column.GetColumnsInQueryResults( cn, database, "SELECT * FROM " + table.QualifiedName, true );

			// Identify key, identity, and non identity columns.
			var nonIdentityColumns = new List<Column>();
			foreach( var col in AllColumns ) {
				if( col.IsCaseInsensitiveKey!.Value is not null )
					keyColumns.Add( col );
				if( col.IsIdentity ) {
					if( IdentityColumn != null )
						throw new UserCorrectableException( "Only one identity column per table is supported." );
					IdentityColumn = col;
				}
				else
					nonIdentityColumns.Add( col );
			}

			// If the table has a composite key, try to use the identity as the key instead since this will enable InsertRow to return a value.
			if( IdentityColumn != null && keyColumns.Count > 1 ) {
				keyColumns.Clear();
				keyColumns.Add( IdentityColumn );
			}

			RowVersionColumn = AllColumns.SingleOrDefault( i => i.IsRowVersion );
			AllColumnsExceptRowVersion = AllColumns.Where( i => !i.IsRowVersion ).ToArray();
			AllNonIdentityColumnsExceptRowVersion = nonIdentityColumns.Where( i => !i.IsRowVersion ).ToArray();

			if( forRevisionHistoryLogic ) {
				if( keyColumns.Count != 1 )
					throw new UserCorrectableException(
						"A revision history modification class can only be created for tables with exactly one primary key column, which is assumed to also be a foreign key to the revisions table." );
				PrimaryKeyAndRevisionIdColumn = keyColumns.Single();
				if( PrimaryKeyAndRevisionIdColumn.IsIdentity )
					throw new UserCorrectableException( "The revision ID column of a revision history table must not be an identity." );
			}

			DataColumns = AllColumns.Where( col => col is { IsIdentity: false, IsRowVersion: false } && col != PrimaryKeyAndRevisionIdColumn ).ToArray();
		}
		catch( Exception e ) {
			throw UserCorrectableException.CreateSecondaryException( "An exception occurred while getting columns for table " + table.QualifiedName + ".", e );
		}
	}

	internal bool HasKeyColumns => keyColumns.Any();

	/// <summary>
	/// Returns either all components of the primary key, or the identity (alone).
	/// </summary>
	internal IReadOnlyCollection<Column> KeyColumns =>
		keyColumns.Any() ? keyColumns : throw new Exception( "The table must contain a primary key or other means of uniquely identifying a row." );
}