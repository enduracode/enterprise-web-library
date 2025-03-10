# Adding re-ordering to a table

Last updated for Enterprise Web Library version 82.


## Creating database schema

### SQL Server

```SQL
create table Ranks(
	RankId int
		not null
		constraint RanksPk primary key,
	Rank int
		not null
)
go

alter table WhatIWantToReorder add
	RankId int
		null
		constraint WhatIWantToReorderRankIdFk references Ranks
go

-- Use a cursor here to initialize the RankId of each existing WhatIWantToReorder row.

alter table WhatIWantToReorder alter column RankId int not null
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


## Adding logic

Use `EwfTable`. In the `EwfTableItemSetup`, specify the `rankId` parameter, setting it to the RankId from the schema you added above.

If you run the app now, you should get an error saying you need to add a DataAccess class to the Providers folder. Do this, and have it implement RankingProvider.

Finally, remember to actually have the query that fills your table order by the Rank (not the rank ID - you'll need to JOIN somehow). And have new inserts of your entity also add a row to the OrderRanks table. When you insert your rank, use RankingMethods.InsertRank() and use the return value to set the OrderRankId on your new row.