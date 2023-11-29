# Data access


## Application-level table caching

You can enable caching for a table called YourData by creating a YourDataTableEwlModifications table containing only column(s) for the main table’s primary key. The caching will turn potentially large queries for the table’s data into tiny queries to the …TableEwlModifications table while never returning stale data, provided that the following assumptions are true:

*	All modifications to the table happen through EWL generated Modification classes (i.e. no external systems, no hand editing of data, and no triggers that modify data in the table)
*	Database transactions use snapshot isolation, which is also an assumption for using the ubiquitous intra-transaction cache


## Revision history (section needs work)

*	We suffix tables with Revisions simply as a convention, to remind ourselves at-a-glance which tables are reivsions-enabled.

*	TableRetrieval is revision-history-aware.

	*	So if you get all rows in the table, they’ll only be the latest

	*	If you get rows “matching id”, they’ll only be the latest

*	Modification classes are revision-history-aware

	*	Inserting and updating rows will automatically only affect the latest revision.

*	Retrieval classes (generated from Development.xml) is not revision-history-aware.

	*	It’s still just raw SQL

	*	So you have to be careful if writing custom Retrieval queries. In order to get the latest version of the row you must:

	```SQL
	SELECT s.* FROM SomeTableRevisions s JOIN revisions r ON r.RevisionId = s.SomeTableRevisionId AND r.LatestRevisionId = r.RevisionId
	```

*	You can no longer look a raw table and expect each row to be a unique record. The table represents the history of all rows in the table.

*	It’ll now be impossible to add a non-nullable columns after this, because you won’t be able to represent the period in time there was no such value. So partial-modification-class constraint-enforcement will really become important.

The way revision history works under hood is that SomeTableRevisions table’s primary key is also a foreign key to the Revisions table’s primary key. The Revisions table has the LastRevisionId column which is a foreign key to itself. There’s another column on the Revisions table that allows us to record what user made the modification, and the modification time if we care. When a modification occurs to a row, the framework inserts a new row into the SomeTableRevisions table that is an exact duplicate of the previous revision’s values, plus whatever modification is currently being made. It then updates the LatestRevisionId on the Revisions table.