# PostgreSQL configuration in client systems

## Provider registration

Use an EWL release that includes PostgreSQL support. Add `Postgres` to the
`Providers` list in `Library/Configuration/Development.xml` and follow the normal
provider-reference workflow for that system. The provider package suffix is
`.Postgres`; its assembly and namespace are `EnterpriseWebLibrary.Postgres`.

Register it in the system's existing external-functionality provider:

```csharp
protected override ExternalPostgresProvider GetPostgresProvider() => new PostgresProvider();
```

`ExternalPostgresProvider` is in `EnterpriseWebLibrary.ExternalFunctionality`;
`PostgresProvider` is in `EnterpriseWebLibrary.Postgres`. Preserve other provider
registrations and match the EWL package versions already used by the system.

## Installation settings

Use the installation schema's `PostgresDatabase` type, for example:

```xml
<database xsi:type="PostgresDatabase">
	<Host>localhost</Host>
	<Port>5432</Port>
	<Database>example_dev</Database>
</database>
```

The enclosing installation XML supplies the `xsi` namespace. For secondary
databases, retain the existing secondary-database structure and name while using
the same database type and connection fields.

- Omitted/empty `Host` defaults to `localhost`.
- `Port` is optional; when specified, it must be between 1 and 65535. When omitted,
  Npgsql uses its default, normally 5432; PostgreSQL command-line tools can also
  honor their environment settings. Specify the port for a non-default instance.
- Omitted/empty `Database` derives the database name from the installation's full
  short name using EWL's snake_case conversion.
- Keep database names within 57 UTF-8 bytes to allow EWL's associated owner-role
  name to fit PostgreSQL's identifier limit.

For Windows development, use integrated authentication with the process's OS
identity. Do not add database passwords to these settings. Follow the machine-setup
skill's PostgreSQL installation and SSPI instructions for server setup and account
access. Azure PostgreSQL hosting support is not yet implemented; do not assume
these local settings constitute an Azure deployment configuration.

After changing provider or installation configuration, run the normal `sync`
workflow and inspect generated references and configuration types.
