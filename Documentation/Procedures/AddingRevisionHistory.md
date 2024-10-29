# Adding revision history

Last updated for Enterprise Web Library version 81.


## The following tables should be created to support revision history

### SQL Server

```SQL
create table UserTransactions(
	UserTransactionId int
		not null
		constraint UserTransactionsPk primary key,
	UserId int
		null
		constraint UserTransactionsUserIdFk references Users,
	TransactionTime datetime2
		not null
)
go

create table Revisions(
	RevisionId int
		not null
		constraint RevisionsPk primary key,
	LatestRevisionId int
		not null
		constraint RevisionsLatestRevisionIdFk references Revisions,
	UserTransactionId int
		not null
		constraint RevisionsUserTransactionIdFk references UserTransactions,
	constraint RevisionsLatestRevisionIdAndUserTransactionIdUnique unique( LatestRevisionId, UserTransactionId )
)
go
```

### MySQL

```SQL
Not yet documented
```

### Oracle

```PLSQL
Not yet documented
```


## Revision history tables

Tables using revision history should have a suffix of “revisions.”

Custom queries attempting to get only the most recent version of an entity (which is most queries, except those trying to show historical data) need to include “INNER JOIN revisions r ON r.revision_id = tableAlias.tablePrimaryKey AND r.latest_revision_id = r.revision_id”.


## To add revision history to an existing table

You must insert one new User Transaction and N new Revisions where N is the number of rows in the table you are enabling revision history on. The RevisionId and the LatestRevisionId of each new row should be identical, and equal to the primary key of the row you are adding the revision for. It will look something like this:

### SQL Server

```SQL
declare @userTransactionId int
set @userTransactionId = next value for MainSequence
insert into UserTransactions values( @userTransactionId, NULL, SYSUTCDATETIME() )
/* Create revisions for existing tasks. */
declare @taskId int
declare taskRow cursor for select TaskId from Tasks
open taskRow
fetch next from taskRow into @taskId
while @@FETCH_STATUS = 0
begin
	insert into Revisions values( @taskId, @taskId, @userTransactionId )
	fetch next from taskRow into @taskId
end
close taskRow
deallocate taskRow
go
```

### MySQL

```SQL
Not yet documented
```

### Oracle

```PLSQL
Not yet documented
```