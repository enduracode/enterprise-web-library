# PostgreSQL installation on a developer workstation

Companion to [machine setup](SKILL.md#4-database-access). Use only when an agreed
development system needs a local PostgreSQL instance. This is Windows workstation
provisioning, not production deployment or a reason to rerun DU `configure-workstation`.
This guide covers machine provisioning and the developer's database access.

## Contract and ownership

- PostgreSQL **18**, latest available maintenance release; x64 binaries on an x64
  workstation. Resolve the current release from the vendor, not an old example URL.
- UTF-8 cluster; passwordless **SSPI** over loopback TCP. No PostgreSQL passwords in
  EWL configuration, password files, or environment variables. SSPI verifies the OS
  identity; `trust` does not and is not an acceptable substitute.
- The initial developer LOGIN SUPERUSER requires explicit authorization for the
  instance. Provision it without a database password.
- Human administrative assistance owns machine installation, service registration,
  and protected-directory ACLs. Agents prepare commands and perform permitted
  verification; do not bypass an access denial or collect personal Windows credentials.
- Windows service identity is distinct from database client identities. Prefer an
  approved passwordless service account, e.g. `NT AUTHORITY\NetworkService` for the
  local workstation. Verify actual service logon and ACLs; never leave the service
  running as LocalSystem merely because service registration defaults to it.

## 1. Inventory before provisioning

Use **PowerShell 7** (`pwsh.exe`) for the examples. Verify `$PSVersionTable.PSVersion`
and `whoami /user` in each terminal. An elevated terminal may run as a different
Windows administrator, with different Windows Terminal profile defaults. Use that
terminal for installation/ACLs/services, and the developer's ordinary non-elevated
terminal for `initdb` and developer authentication tests. Never infer identity from
the prompt's working directory or request the developer's Windows password.

Identify the developer account (including machine/domain and SID),
architecture, existing PostgreSQL binaries/services, data directories, and listening
ports. Have the administrator inspect services if the agent cannot enumerate them.
A permission denial is not proof that nothing is installed.

Record the chosen major-version binary directory, data directory, service name,
port, and identities. Suggested new-instance locations are
`C:\Program Files\PostgreSQL\18`, `C:\Enterprise Web Library\PostgreSQL Data\18`, service
`ewl-postgresql-18` (display name `EWL PostgreSQL 18`), and port 5432, but first confirm they are unused. Never take
over an existing service, overwrite a data directory, or reinitialize a cluster.
Existing instances follow inspection/configuration, not the new-cluster steps.

Discover the actual EWL infrastructure root rather than assuming the C: example.
Use these variables consistently in each terminal, substituting approved values:

```powershell
$ewlRoot = '<discovered EWL infrastructure directory>'
$install = Join-Path $env:ProgramFiles 'PostgreSQL\18'
$bin = Join-Path $install 'bin'
$data = Join-Path $ewlRoot 'PostgreSQL Data\18'
$service = 'ewl-postgresql-18'
$displayName = 'EWL PostgreSQL 18'
$port = 5432
$developerAccount = '<confirmed MACHINE\developer>'
$developerRole = '<confirmed-lowercase-role>'
```

In the administrator terminal, enumerate services with `Get-CimInstance Win32_Service`
and inspect names/executable paths containing `postgres` or `pg_ctl`. Inspect
`Get-NetTCPConnection -State Listen` for the chosen port, and check both target
directories with `Test-Path`/`Get-ChildItem -Force`. Stop for any conflict; absent
search results do not prove an arbitrarily named instance elsewhere is absent.

## 2. Obtain binaries and prepare directories

Use the [PostgreSQL Windows download page](https://www.postgresql.org/download/windows/)
and its linked [EDB binary archives](https://www.enterprisedb.com/download-postgresql-binaries).
Archives allow explicit passwordless initialization without an installer's password
workflow. Resolve the actual HTTPS download, record release/source and SHA-256, and
check publisher checksums/signatures where supplied. A locally calculated hash is
an inventory value, not independent publisher verification.

Extract to a staging directory, inspect the layout (`pgsql/bin` is common), and have
the administrator place binaries in the selected version directory. Keep binaries
separate from data. Verify `postgres --version`, `initdb --version`, `pg_ctl --version`,
`psql --version`, `pg_dump --version`, and `pg_restore --version` using absolute paths.
If runtime libraries are missing, install the vendor-required Microsoft runtime
through the administrator; do not copy arbitrary DLLs from unrelated applications.

Give the initialization account access to the new data directory and the service
identity sufficient access to that directory and its logs. Binaries need read/execute
access for clients and the service, not general write access. Scope ACL changes to
these new directories; preserve unrelated installations and permissions.

For a **new** installation, the human administrator can use this preparation example.
The archive must already have been obtained and its source/hash recorded. Extract
it afresh in the administrator's temporary workspace; do not execute mutable staging
binaries elevated. This copies server/client tools, not pgAdmin or StackBuilder.

```powershell
$archive = '<accessible downloaded archive path>'
$expectedHash = '<recorded SHA-256>'
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Archive hash mismatch.'
}
foreach ($path in @($install, $data)) {
    if (Test-Path -LiteralPath $path) { throw "Inspect existing path before proceeding: $path" }
}
$developerSid = [Security.Principal.NTAccount]::new($developerAccount).
    Translate([Security.Principal.SecurityIdentifier])
$extract = Join-Path $env:TEMP ('ewl-postgresql-' + [guid]::NewGuid())
[IO.Compression.ZipFile]::ExtractToDirectory($archive, $extract)
New-Item -ItemType Directory -Path $install | Out-Null
foreach ($folder in @('bin', 'lib', 'share', 'include', 'doc')) {
    Copy-Item -LiteralPath "$extract\pgsql\$folder" -Destination $install -Recurse -ErrorAction Stop
}
Get-ChildItem "$extract\pgsql" -File | Copy-Item -Destination $install -ErrorAction Stop
& icacls.exe $install /grant '*S-1-5-20:(OI)(CI)(RX)'
if ($LASTEXITCODE -ne 0) { throw 'Binary ACL grant failed.' }
New-Item -ItemType Directory -Path $data | Out-Null
$acl = Get-Acl -LiteralPath $data
$acl.SetAccessRuleProtection($true, $false)
foreach ($sid in @('S-1-5-18', 'S-1-5-32-544')) {
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl',
        'ContainerInherit,ObjectInherit', 'None', 'Allow'))
}
foreach ($sid in @([Security.Principal.SecurityIdentifier]::new('S-1-5-20'), $developerSid)) {
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $sid, 'Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
}
Set-Acl -LiteralPath $data -AclObject $acl
Get-Acl -LiteralPath $data | Format-List AccessToString
Remove-Item -LiteralPath $extract -Recurse -Force
```

Set `$ErrorActionPreference = 'Stop'` for provisioning blocks. The constant SIDs
above are Windows' built-in SYSTEM, Administrators, and NetworkService identities,
not machine-specific accounts. These ACL operations apply only to the newly created
empty data directory; never apply this inheritance reset to existing data or the EWL
parent. Confirm the developer and service can read/execute the binaries and traverse
their parent paths. If preparation partially fails, inspect before rerunning.

## 3. Initialize a new cluster without a password

Run `initdb` as the agreed **non-elevated initialization account** with filesystem
access to the new data directory. Do not assume an elevated administrator process
can directly run the database server. A human can prepare directories elevated and
then initialize from their ordinary developer terminal.

Example PowerShell, after substituting confirmed values:

```powershell
if (@(Get-ChildItem -LiteralPath $data -Force -ErrorAction Stop).Count) {
    throw 'Data directory is not empty.'
}
& "$bin\initdb.exe" -D $data -U $developerRole --encoding=UTF8 --locale=C --auth-local=reject --auth-host=sspi
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL initialization failed.' }
```

`-U` names the initial PostgreSQL superuser; it does not select the Windows account
running `initdb`. No `--pwprompt`/`--pwfile` is needed. `--locale=C` is a portable,
case-sensitive baseline. Application migrations can specify other collations where
needed; string primary keys supported by the initial EWL provider use explicit `"C"`.
This creates the developer's LOGIN SUPERUSER; no separate developer `CREATE ROLE`
is needed. Record the selected time zone and checksum setting from initialization.
Recheck the service's data-directory ACLs after `initdb` adjusts permissions.

Before starting the new server, configure `listen_addresses = 'localhost'` and the
chosen `port` in `postgresql.conf`. Enable file logging to an agreed service-writable
location so startup/authentication failures can be diagnosed. For this new cluster,
set the following in `postgresql.conf` before startup (use the selected port):

```text
listen_addresses = 'localhost'
port = 5432
logging_collector = on
log_destination = 'stderr'
log_directory = 'log'
log_connections = 'all'
```

The relative log directory is inside the cluster. Validate main settings without
starting a server using `postgres.exe -D $data -C <setting>`. This does not validate
HBA/maps; inspect those through the running server after startup.

## 4. Configure exact SSPI mappings before first connection

For a **new** cluster, replace initdb's initial host rules with reviewed loopback
rules; for an existing cluster, preserve unrelated rules and account for first-match
ordering. Include both IPv4 and IPv6 when using `localhost`:

```text
# pg_hba.conf: TYPE DATABASE USER ADDRESS METHOD OPTIONS
local all all reject
host all all 127.0.0.1/32 sspi include_realm=1 compat_realm=1 map=main
host all all ::1/128      sspi include_realm=1 compat_realm=1 map=main
local replication all reject
host replication all 127.0.0.1/32 reject
host replication all ::1/128 reject
```

`all` here still requires an explicit mapping to the requested database role; it
does not grant every Windows user access. Omitted remote/replication rules are not
needed for this local-only development setup. Do not retain an earlier matching
`trust` or password-auth rule that bypasses the intended SSPI path.

With `compat_realm=1` and `include_realm=1`, SSPI mapping principals normally have
the form `username@MACHINE` (or `username@DOMAIN`), **not** `MACHINE\username`:

```text
# pg_ident.conf: MAP SYSTEM-USERNAME DATABASE-USERNAME
main developer@MACHINE developer_role
```

Substitute the observed/confirmed principal; case and account naming must match
what SSPI actually supplies. Quote tokens containing whitespace. Prefer exact
entries, not broad regular expressions or stripping realms. If authentication fails,
inspect the PostgreSQL log for the authenticated principal; do not weaken the method.

## 5. Register and start the service (human administrator)

After verifying that the proposed service does not exist, register the intended
identity explicitly. For the approved NetworkService option:

```powershell
& "$bin\pg_ctl.exe" register -N $service -D $data -U 'NT AUTHORITY\NetworkService' -S auto
if ($LASTEXITCODE -ne 0) { throw 'Service registration failed.' }
& sc.exe config $service DisplayName= $displayName start= delayed-auto
if ($LASTEXITCODE -ne 0) { throw 'Service configuration failed.' }
```

Delayed automatic startup follows EWL's background-service convention and reduces
boot-time contention; the database may not be available immediately after boot.
Use the confirmed service name, not the example if it conflicts. Ensure this service
account can read the binaries and read/write the data/log directories, then start
the service and verify its actual executable, data directory, account, status, and
loopback listener. If registration/account privileges or startup fail, inspect the
specific error/log and fix the intended configuration; do not switch to a privileged
service account to conceal an ACL problem. Do not start a second server against the
same data directory. On reruns, inspect the existing registration instead of replacing it.

```powershell
$registration = Get-CimInstance Win32_Service | Where-Object Name -eq $service
$registration | Format-List Name, DisplayName, StartName, StartMode, PathName, State
if ($registration.StartName -ne 'NT AUTHORITY\NetworkService') {
    throw 'Unexpected service account; do not start.'
}
# Inspect PathName above for the approved binary and data paths before continuing.
Start-Service -Name $service
Get-Service -Name $service | Format-Table Name, DisplayName, Status
Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$service" |
    Select-Object Start, DelayedAutoStart, ObjectName
Get-NetTCPConnection -LocalPort $port -State Listen |
    Format-Table LocalAddress, LocalPort, OwningProcess
```

Expect `Start=2`, `DelayedAutoStart=1`, Running state, and only `127.0.0.1`/`::1`
listeners. Match their PID to the intended server. Inspect `current_logfiles` and
the referenced logs if startup fails. Configured delayed startup is not evidence
of successful reboot startup; record a reboot check only when actually performed.

## 6. Bootstrap developer login and verify

From the developer's ordinary terminal, connect explicitly to `postgres` (otherwise
tools may default to a nonexistent database named after the user):

```powershell
& "$bin\psql.exe" -X -w -h localhost -p $port -U $developerRole -d postgres -v ON_ERROR_STOP=1 `
    -c 'SELECT session_user, current_user, current_setting(''server_version''), current_setting(''server_encoding'');' `
    -c 'SELECT rolname, rolsuper, rolcanlogin FROM pg_roles WHERE rolname = current_user;'
if ($LASTEXITCODE -ne 0) { throw 'Developer SSPI verification failed.' }
```

Verify the login uses SSPI through server authentication diagnostics, not simply
the absence of a password prompt. On PostgreSQL 18 for Windows, successful SSPI can
leave `pg_stat_gssapi.gss_authenticated` false and its principal empty. Enable
`log_connections = 'all'` and inspect the matching backend's authentication log for
`method=sspi`; the logged identity can use `MACHINE\username` even when the mapping
uses `username@MACHINE`.
An existing password/passfile or matching trust rule must not account for success.
No OS-password prompts or personal-session reuse are needed by the agent.

Test explicit-role and default-username behavior separately: repeat the command
without `-U`, and test `localhost`, `127.0.0.1`, and `::1`. Libpq tools and Npgsql may
form default usernames differently. Verify the requested role matches the approved
developer identity; explicitly set a nonsecret username if defaults differ.
A nonsecret username is a requested role; it never replaces OS authentication.

Using the developer's connection, inspect `pg_hba_file_rules` and
`pg_ident_file_mappings` for parsing errors and the intended rules. Verify
`rolpassword IS NULL` in `pg_authid` for the developer role. Inventory only the
**names/presence** of PG environment variables and password files; do not print
their contents. A successful connection alone does not exclude password fallback.

### Npgsql verification

Create an owned disposable .NET console project in a location the ordinary developer
can read and execute. Do not assume a provisioning account's profile/temp directory
is accessible. Use an agreed shared temporary directory, not broad profile ACL grants.
Add the current compatible stable `Npgsql` package and record its version. Run under
the developer's ordinary identity; no `Integrated Security=true` is required by
modern Npgsql. A minimal program is below; substitute the approved role/port and
clear any PG environment overrides in this disposable process without logging values.

```csharp
using Npgsql;

foreach (var key in Environment.GetEnvironmentVariables().Keys.Cast<string>()
             .Where(k => k.StartsWith("PG", StringComparison.OrdinalIgnoreCase)).ToArray())
    Environment.SetEnvironmentVariable(key, null);
var expectedRole = "<approved-developer-role>";
var missingPassfile = Path.Combine(Path.GetTempPath(), "pg-no-passfile-" + Guid.NewGuid());
foreach (var host in new[] { "localhost", "127.0.0.1", "::1" }) {
    foreach (var explicitUser in new[] { true, false }) {
        var settings = new NpgsqlConnectionStringBuilder {
            Host = host, Port = 5432, Database = "postgres", Pooling = false,
            Passfile = missingPassfile, ApplicationName = "ewl-install-verification"
        };
        if (explicitUser) settings.Username = expectedRole;
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT session_user::text || '/' || current_user::text", connection);
        var identity = (string)(await command.ExecuteScalarAsync())!;
        if (identity != expectedRole + "/" + expectedRole) throw new Exception(identity);
        Console.WriteLine($"{host}, explicit={explicitUser}: {identity}");
    }
}
```

Correlate these connections with `method=sspi` in the server log. If default username
selection fails, record the actual behavior instead of weakening the mapping.

### Disposable database and archive verification

Run from the ordinary developer terminal. Use unique lowercase names and remember
which databases were successfully created; never adopt or drop an existing database.
Keep the archive outside the cluster. Use `psql -X -w -v ON_ERROR_STOP=1` for each SQL
step and check every CLI exit code. Supply `-h localhost -p $port -U $developerRole`
to all tools, and select a nonexistent `PGPASSFILE` in the disposable test process.

1. Choose two unique names, e.g. `ewl_install_probe_<random>` and its `_restored`
   counterpart. Using database `postgres`, issue `CREATE DATABASE` separately for
   each, outside a transaction.
2. In the source database, execute:

   ```sql
   CREATE SEQUENCE public.main_sequence AS integer MINVALUE 1;
   CREATE TABLE public.probe (id integer PRIMARY KEY, value text NOT NULL);
   INSERT INTO public.probe VALUES (nextval('public.main_sequence'), 'café 日本語');
   UPDATE public.probe SET value = value || ' updated';
   SELECT * FROM public.probe;
   SELECT setval('public.main_sequence', 41, true);
   ```

3. Run `pg_dump -w -Fc -f <archive> <source>` with the common connection arguments.
4. Run `pg_restore -w --exit-on-error --no-owner --no-privileges -d <destination> <archive>`
   with the common connection arguments.
5. Verify the destination contains the exact updated Unicode row and that
   `SELECT nextval('public.main_sequence');` returns **42** on its first call.
   Inspect `pg_class`/`pg_get_userbyid(relowner)` for table/sequence destination
   ownership. To prove owner stripping rather than merely observe the same owner,
   use a uniquely named, owned temporary NOLOGIN role as source-object owner;
   after restore both objects must belong to the restoring developer role.
6. Close test connections, drop only databases successfully created by this test,
   then drop its temporary owner role if created. Remove the owned archive and
   shared executable copy; retain useful sources/results in an approved workspace.
   Check the catalog to confirm cleanup even when a test fails.

Use a disposable database for create/read/write, sequences, and `pg_dump -Fc` /
`pg_restore --no-owner --no-privileges` tests. Test raw Npgsql separately if the EWL
provider is not yet implemented. Clean up only the owned fixture, not application
databases. Record results before declaring setup complete.

If `pg_ctl reload` is denied because the service runs under a different Windows
identity, have an already authorized PostgreSQL administrator execute
`SELECT pg_reload_conf();` through their own SSPI connection. Database SUPERUSER
does not grant Windows process-control permissions.

## Completion record and later Linux compatibility

Record exact version, source/hash, binaries/data paths, service identity/name, port,
principal-to-role mappings, grants, authentication evidence for both accounts, and
CLI/Npgsql results, delayed-start configuration, and whether restart/reboot was
actually tested. Separate successful installation from untested EWL integration.
Document partial completion and the exact human action needed if blocked.

Linux will use OS peer authentication over Unix-domain sockets and service process
accounts. No Windows-specific service/configuration code belongs in the EWL database
runtime. Database creation/migrations belong to per-system DU/ISU operations;
`initdb` is one-time instance provisioning, never part of normal per-system sync.

After installation, follow [agent PostgreSQL access](SKILL.md#postgresql-integrated-authentication)
to provision the dedicated account separately.

References: [initdb](https://www.postgresql.org/docs/18/app-initdb.html),
[pg_ctl](https://www.postgresql.org/docs/18/app-pg-ctl.html),
[SSPI](https://www.postgresql.org/docs/18/sspi-auth.html),
[username maps](https://www.postgresql.org/docs/18/auth-username-maps.html),
[Npgsql authentication](https://www.npgsql.org/doc/security.html).
