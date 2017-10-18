# Database Initialization Scripts

## SQL Server

Replace `DatabaseName` before running this.

```SQL
USE DatabaseName

ALTER DATABASE DatabaseName SET PAGE_VERIFY CHECKSUM
ALTER DATABASE DatabaseName SET AUTO_UPDATE_STATISTICS_ASYNC ON
ALTER DATABASE DatabaseName SET ALLOW_SNAPSHOT_ISOLATION ON
ALTER DATABASE DatabaseName SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE
GO

CREATE TABLE GlobalInts(
	ParameterName varchar( 50 )
		NOT NULL
		CONSTRAINT GlobalIntsPk PRIMARY KEY,
	ParameterValue int
		NOT NULL
)

INSERT INTO GlobalInts VALUES( 'LineMarker', 0 )
GO

CREATE TABLE MainSequence(
	MainSequenceId int
		NOT NULL
		IDENTITY
		CONSTRAINT MainSequencePk PRIMARY KEY
)
GO
```

## MySQL

```SQL
CREATE TABLE global_ints(
	ParameterName VARCHAR( 50 )
		PRIMARY KEY,
	ParameterValue INT
		NOT NULL
);

INSERT INTO global_ints VALUES( 'LineMarker', 0 );

CREATE TABLE main_sequence(
	MainSequenceId INT
		AUTO_INCREMENT
		PRIMARY KEY
);
```

## Oracle

```PLSQL
CREATE TABLE global_numbers (
	k VARCHAR2( 100 )
		CONSTRAINT global_numbers_pk PRIMARY KEY,
	v NUMBER
);

INSERT INTO global_numbers VALUES( 'LineMarker', 0 );

CREATE SEQUENCE main_sequence;
```