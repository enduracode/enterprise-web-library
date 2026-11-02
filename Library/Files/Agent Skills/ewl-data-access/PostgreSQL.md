# PostgreSQL data access in client systems

## Names and generated APIs

- Prefer lowercase snake_case database identifiers. EWL converts PostgreSQL
  table, column, and non-default schema names to PascalCase for generated C# APIs.
  Avoid names that become identical after conversion.
- `public` is the default schema. Other schemas receive corresponding generated
  namespaces and folders. Use actual database names in generation configuration.
- In handwritten SQL, double-quote mixed-case identifiers exactly. Unquoted
  PostgreSQL identifiers fold to lowercase.
- Generated table `Name` constants contain logical qualified names, not
  pre-delimited SQL references. Do not assume they handle quoting for handwritten SQL.

## IDs, strings, and caching

- Assign IDs explicitly, normally using the generated `MainSequence.GetNextValue()`
  API (prefixed by the secondary database name when applicable). PostgreSQL
  identity/serial generation is not currently supported by EWL DAL generation.
- Follow the main skill's non-nullable string-column rule.
- String keys must be `varchar` or `text` with explicit built-in
  `pg_catalog."C"` collation. This also applies to corresponding keys in companion
  modification tables. Case-insensitive string keys are not currently supported.
- Companion modification tables use `<table>_table_ewl_modifications`, with
  matching key columns and no unique constraint. Follow the main skill's caching
  instructions for their use.
- Row-versioned data caching is not currently supported for PostgreSQL. Use the
  other caching facilities described in the main skill as appropriate.

## Custom retrieval and modification SQL

- Use named `@parameter` placeholders. Keep parameter-looking text out of comments
  and PostgreSQL dollar-quoted strings; EWL's simple name discovery does not fully
  understand those constructs or backslash-escaped quotes in `E'...'` strings.
- Use casts such as `@customerId::integer` when PostgreSQL cannot infer a
  parameter's type. Inference failures stop generation.
- A custom retrieval's SELECT/FROM portion must be independently describable;
  do not rely on a WHERE clause in a variant to establish its parameter types.
  Variants share one generated row type, so keep their result columns compatible.
- PostgreSQL uses `LIMIT` or `FETCH FIRST`, not SQL Server's `TOP` syntax.
- PostgreSQL `date`, `time`, `timestamp`, and `timestamptz` map to NodaTime
  `LocalDate`, `LocalTime`, `LocalDateTime`, and `Instant`, respectively. Account
  for PostgreSQL's microsecond timestamp precision when comparing round-tripped values.

For schema definitions, read the database-migration skill's PostgreSQL reference.
