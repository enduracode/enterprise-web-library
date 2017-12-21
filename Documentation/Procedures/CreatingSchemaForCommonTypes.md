# Creating database schema for common types (section needs work)

Below are recommended lengths/types for common data types. There may be better (shorter) values to use, and there may be fundamentally better ways to store data (like storing an address as one large field and assuming we can parse it later rather than breaking it up) eventually, but for now these are values that work well, and it saves the time of having to re-think it every time.

```SQL
ZipCode varchar( 20 ) NOT NULL
PhoneNumber varchar( 25 ) NOT NULL
City nvarchar( 100 ) NOT NULL
Street nvarchar( 200 ) NOT NULL
Url nvarchar( 2083 ) NOT NULL
```