# Database migration

If you need to make changes to your database schema or your reference data, you can use the `Library/Configuration/Database Updates.sql` file. Create the file if it does not already exist. Scroll to the bottom, add SQL statements that make your changes, save the file, and run `Update-DependentLogic`. This will run your statements against your local copy of the database. Every installation of your system stores its current position in the `Database Updates.sql` file, so when you commit your new version of this file, and deploy the system to a server, all of your changes that have not already run against that installation’s database will run.

## Examples (for SQL Server)

### Creating a table

```SQL
CREATE TABLE Customers(
	CustomerId int
		NOT NULL
		CONSTRAINT CustomersPk PRIMARY KEY,
	CustomerName varchar( 50 )
		NOT NULL
)
GO
```

### Adding a row of reference data

```SQL
INSERT INTO Countries VALUES( 123, 'Czech Republic' )
GO
```