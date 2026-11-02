# PostgreSQL migrations in client systems

Use the main skill's FluentMigrator workflow and forward-only migration rules.

## Schema conventions

- Prefer lowercase snake_case names for tables, columns, sequences, constraints,
  and indexes. Pass raw names to FluentMigrator, not manually quoted strings.
  Use `.InSchema( "schema_name" )` for a separate schema rather than putting a dot
  in the table name. `public` is the default schema.
- Keep identifiers within PostgreSQL's 63-byte limit, including explicitly named
  constraints and indexes. The limit is bytes, not characters.
- Preserve the existing system's naming conventions when modifying its schema.

Avoid identifier names that themselves begin and end with double quotes.
FluentMigrator's PostgreSQL existence checks and table reads can interpret those
quotes as SQL delimiters, while EWL's migration SQL generation treats them as part
of the name. Ordinary lowercase snake_case names are unaffected; continue passing
raw, unquoted names to fluent APIs.

## IDs and column definitions

- Use ordinary integer ID columns, not `.Identity()`, `serial`, or a `nextval(...)`
  column default. Client code supplies sequence-assigned IDs explicitly.
- EWL initializes `main_sequence` when creating a fresh database. Use its generated
  API rather than creating a second sequence for the same purpose or resetting it
  in routine migrations. Explicitly inserted seed IDs must not collide with future
  sequence values.
- Define string columns as non-nullable. Choose `varchar(n)` for a simple maximum
  character count or `text` when no type-level bound is needed.
- String keys require explicit `COLLATE pg_catalog."C"`; match that collation on
  companion modification-table keys. For example:

  ```sql
  CREATE TABLE public.statuses (
      code varchar(30) COLLATE pg_catalog."C" NOT NULL PRIMARY KEY,
      description text NOT NULL
  );
  ```

- Use `timestamp with time zone` for instants, and `timestamp without time zone`
  for local date/time values. Choose deliberately rather than translating SQL
  Server date/time types mechanically.

## Applying migrations

Run migrations through EWL's Data Migrator so database ownership and later
application access follow the installation's configured setup. Client migrations
normally do not need to issue `SET ROLE` or provision application login roles.

Keep migrations transactional and leave PostgreSQL notices enabled; identifier
truncation should fail the migration rather than leave unexpectedly named objects.
Run `sync` after schema changes and inspect the regenerated DAL before updating
client code that consumes it.
