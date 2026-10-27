---
name: ewl-opencode-machine-setup
description: Complete EWL workstation setup after the human-run DU configure-workstation operation, including dedicated-account global OpenCode configuration and agent rules, development dependencies, filesystem and database access, authentication, and verification.
---

# Complete EWL workstation setup

## Scope and bootstrap handoff

This Windows skill continues after the human-run DU `configure-workstation`
operation. Its implementation in
`Development Utility/Operations/ConfigureWorkstation.cs` is the source of truth
for bootstrap responsibilities and checks. Refer to it when diagnosing an
incomplete handoff; do not repeat its provisioning or invoke it as an agent.
Agents must not initiate OpenCode installation or upgrades.

Complete the development setup below under the dedicated server account. Global
agent configuration belongs to that account, not the developer’s personal profile.
Personal client preferences remain with the developer. Do not start a personal
server or switch to standalone mode. Use the existing explicit-server connection.

Record completed bootstrap checks as such; distinguish them from remaining runtime
verification. Honor explicit verification waivers and report unverified items.

## 1. Discover locations and complete access

Identify the developer’s checkout locations rather than assuming repositories are
under the dedicated account’s `%USERPROFILE%`. Ask for the developer’s repository
root when it is not established by the handoff.

| Location | Typical path and identifying contents |
|---|---|
| EWL source | Developer’s `Revision Control\EwlBill`; `Enterprise Web Library.slnx`, `Core`, `Library`, `Development Utility` |
| EWL System Manager source | Developer’s `Revision Control\EWL System Manager`; `Installation Support Utility`, `Web Site`, `Program Runner`, `Data Cleaner` |
| EWL infrastructure | `C:\Enterprise Web Library`; local feed and machine configuration |
| Client systems | Ask the user; recursively identify systems by `dotnet-tools.json` files containing `ewl` |

If needed, check `D:\Revision Control`, `C:\Source`, `C:\Repos`, `C:\dev`, or the
developer’s profile for source roots, and `D:\Enterprise Web Library` for
infrastructure. Check only accessible locations; ask for access rather than
bypassing a denial. Confirm candidates using the identifying contents above.
Verify each supplied path exists. Allow the user to skip absent systems and omit
their paths from instructions. Record resolved absolute paths and skipped items.

Bootstrap grants access to the EwlBill checkout. Arrange inherited Modify access
to other agreed repositories, infrastructure, and local-feed locations with human
administrator assistance as needed. Preserve existing ACLs and the developer’s
access to agent-created files. Do not grant the entire personal profile. Windows
ACLs and OpenCode tool permissions are separate concerns.

### NUnit access to checkout ancestors

For checkouts under the developer’s profile, NUnit engine extension discovery
walks ancestor directories up to the drive root. Verify listing access as the
dedicated account; if the profile root is blocked, have the human administrator
grant **ListDirectory on that folder only**, with no inheritance. This exposes
immediate entry names, not file contents or access to personal subfolders. Check
other ancestors before granting anything else; do not relocate tests as the
default fix because EWL initialization and test fixtures use assembly-relative paths.

**Do not use Explorer’s Security GUI:** even “This folder only” can trigger a
profile-wide inheritance walk and access-denied prompts. `Set-Acl` and `icacls`
can also process child ACLs; omitting `/T` is not sufficient assurance. For this
specific non-propagating change, use the following in the human’s elevated
PowerShell 7, substituting the confirmed profile path and dedicated account:

```powershell
$path = 'C:\Users\<developer>'
$account = "$env:COMPUTERNAME\agents"
Add-Type @'
using System.Runtime.InteropServices;
public static class FolderOnlyAcl {
    [DllImport("advapi32.dll", EntryPoint = "SetFileSecurityW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetDacl(string path, uint information, byte[] descriptor);
}
'@
$acl = Get-Acl -LiteralPath $path
$acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
    $account, 'ListDirectory', 'None', 'None', 'Allow'))
if (-not [FolderOnlyAcl]::SetDacl($path, 4, $acl.GetSecurityDescriptorBinaryForm())) {
    throw [System.ComponentModel.Win32Exception]::new(
        [Runtime.InteropServices.Marshal]::GetLastWin32Error())
}
(Get-Acl -LiteralPath $path).Access |
    Where-Object IdentityReference -eq $account | Format-List *
```

`4` selects only the DACL. The obsolete
[`SetFileSecurityW`](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-setfilesecurityw)
API is intentional here: unlike its replacement, it does not propagate directory
security to children. Preserve existing entries, ownership, and inheritance
protection; never reset ACLs or take ownership to suppress errors. Verify the
grant has `InheritanceFlags: None` and `PropagationFlags: None`, inspect an
unrelated child for unintended access, and run NUnit as the dedicated account.

## 2. Complete global configuration and agent rules

Resolve the dedicated account’s config directory with `opencode debug paths config`.
The ordinary location is `%USERPROFILE%\.config\opencode`, not `.opencode`.
Read `AGENTS.md` and the existing `opencode.json` or `opencode.jsonc` before editing.
Do not create both JSON forms. Preserve unrelated settings, `$schema`, credentials,
and bootstrap rules. Ask before replacing conflicting user choices; do not display
secret-bearing configuration. Back up existing content before substantial rewrites.

Update the EWL location section of `AGENTS.md` using concrete absolute paths, not
environment-variable placeholders. Replace the existing section rather than
appending duplicates; preserve unrelated instructions. Use this structure, omitting
skipped paths and substituting the discovered locations:

```markdown
# Global Agent Rules

If looking for something possibly involving the Enterprise Web Library (EWL), check these locations:

- EWL source code: `<resolved EWL source path>`
- EWL System Manager source code: `<resolved System Manager path>`
- EWL folder: `<resolved infrastructure path>`
- EWL client systems: `<resolved client root>`. When the user asks how other EWL systems do something, or which systems implement an EWL feature, search this directory recursively for `dotnet-tools.json` files containing `ewl` and inspect the matching codebases.
```

Retain bootstrap’s browser-isolation and human-only OpenCode update rules without
duplicating them. Use `ewl-web-testing` for browser testing.

Consult the OpenCode skill and current official documentation before changing
configuration shapes or permission rules. Do not recreate the old global
external-directory allowlist or add a catch-all approval rule. Preserve existing
tool/agent policies; resolve actual access failures using the applicable rule and
Windows ACLs. Validate configuration before requesting a supported reload/restart,
then confirm effective behavior in a fresh connection. Do not assume load-once
behavior or that an inline config override repairs a malformed file; restore the
known-good backup if an edit prevents loading.

## 3. Development dependencies and credentials

Inventory what the selected systems require. Reuse existing installations and
configure access/PATH for the **dedicated account**. Verify tools from its server
session, not merely from the personal terminal. Persistent environment changes
require a coordinated server restart; an absolute executable path can bridge setup.

- Check required .NET SDKs and Git/Mercurial access. A personal-account-only source
  control installation may require a dedicated-account installation or human
  assistance for a shared install. For Git for Windows, use the official release,
  verify its hash/signature, and add its `cmd` directory to PATH. Verify version
  and init/status in a disposable repository.
- Install Node.js LTS when required by agent testing or integrations. Prefer an
  official Windows distribution under the dedicated account, verify its checksum,
  and verify `node --version`. Ask the human if machine-wide elevation is needed.
- Ask which source-control commit identity to use; never invent it. Configure Git
  and/or Mercurial authentication and identity independently for this account.
  For Git, set the confirmed `user.name` and `user.email` in the dedicated account’s
  global configuration, preserving unrelated settings. Do not substitute a made-up
  agent identity or use per-command identity overrides to bypass incomplete setup.
  Verify the effective identity and a disposable test commit from the dedicated
  server account; tool availability and a successful status command are insufficient.
  In `Mercurial.ini`, preserve existing settings when adding `[ui] username`.
  EWL uses Evolve and Topics: verify availability using
  `hg --config extensions.evolve= --config extensions.topic= version --verbose`
  before enabling the available extensions. A successful commit alone is insufficient.
- Provision ReSharper CLI tools/settings and other system-specific dependencies.
  Follow `ewl-web-testing` for browser binaries, disposable profiles, authentication,
  application startup, and owned-process cleanup.
- Complete provider and MCP authentication under the server identity. Ask the
  human to perform interactive sign-ins. Keep secrets out of shared files and
  transcripts. If Azure access is needed, use a separate authorized service
  principal for the dedicated account rather than reusing the personal CLI session.

### Git identity and developer-owned checkout trust

Git normally rejects repositories owned by another Windows account. Filesystem
access alone does not establish Git trust. Ask the user which checkout paths or
controlled checkout root the dedicated account may trust, and explain that trusted
repositories can run configured hooks/helpers with that account’s permissions.
After approval, add `safe.directory` entries to the dedicated account’s global Git
configuration. An approved root can use `/*` to cover all repositories beneath it,
including future checkouts; for example:

```powershell
git config --global --add safe.directory 'C:/Users/willi/Revision Control/*'
```

Use the actual approved root, avoid duplicate entries, and never default to `*`,
which disables ownership protection everywhere. Trust does not grant filesystem
permissions or change ownership. Do not read the developer’s personal Git profile
to obtain credentials or silently copy their configuration.

From the dedicated server session, verify in an agreed **developer-owned checkout**:

```powershell
git config --show-origin --show-scope --get-regexp '^(user\.(name|email)|safe\.directory)$'
git var GIT_AUTHOR_IDENT
git var GIT_COMMITTER_IDENT
git status --short
```

Check for repository-local identity overrides. If identity or trust fails, inspect
the effective settings and resolve setup rather than inventing an identity or
relying on invocation-scoped overrides. Keep diagnostics restricted to these
non-secret settings; do not dump credential-bearing configuration.

Make the test commit only in a uniquely named disposable repository under the
dedicated account’s approved temporary workspace. Confirm the recorded author and
committer match the intended identity, then remove only that owned fixture. Never
make a setup test commit in a real checkout. A disposable repository tests commits
but cannot establish cross-account checkout trust, so both checks are required.

### NuGet sources

Configure NuGet as the dedicated identity. Windows-encrypted passwords are bound
to the account and machine; copying another user’s configuration does not transfer
usable credentials. Merge sources without replacing unrelated entries.

For EnduraCode workstations, confirm these sources:

- `EnduraCode`: `https://heptapod.host/api/v4/projects/1911/packages/nuget/index.json`
- `Local`, when needed: `C:\Enterprise Web Library\Local NuGet Feed`

Have the human supply a deploy-token username and package-registry read token in a
dedicated-account PowerShell 7 window. Do not request the token in chat:

```powershell
whoami
dotnet nuget list source
$username = Read-Host 'Deploy-token username'
$password = Read-Host 'Deploy-token password' -AsSecureString
$credential = [pscredential]::new($username, $password)
try {
    dotnet nuget add source `
        'https://heptapod.host/api/v4/projects/1911/packages/nuget/index.json' `
        --name EnduraCode --username $credential.UserName `
        --password $credential.GetNetworkCredential().Password
    if ($LASTEXITCODE -ne 0) { throw 'NuGet source configuration failed.' }
} finally {
    $credential = $null
    $password.Dispose()
    Remove-Variable credential, password
}
```

For an existing source, use `dotnet nuget update source EnduraCode` with the same
credential arguments. Do not use `--store-password-in-clear-text` on Windows.
If paste is incomplete, try Windows Terminal’s Ctrl+Shift+V or right-click paste;
check `$password.Length` without revealing its value. Confirm/create the local-feed
directory before adding it. Verify with an actual solution restore; listing sources
alone does not establish authentication or package access.

## 4. Database access

Discover relevant SQL Server/PostgreSQL instances and MySQL/Oracle endpoints from EWL
configuration without printing credentials. Ask what access is authorized; full
administration is not implicit in workstation setup.

### PostgreSQL integrated authentication

An installed, initialized PostgreSQL instance is a prerequisite. If it is missing,
follow [PostgreSQL workstation installation](PostgreSQL%20Workstation%20Installation.md)
with the human administrator before granting agent access. Reuse existing instances;
do not run `initdb` on an existing data directory or rerun `configure-workstation`.

Confirm the dedicated server's actual Windows identity with `whoami /user` and its
separate PostgreSQL role. When full administration is authorized, have an existing
PostgreSQL administrator execute the following through their own SSPI connection,
substituting the approved role (SQL identifier):

```sql
CREATE ROLE agent_role LOGIN SUPERUSER PASSWORD NULL;
```

For an existing role, inspect its identity and attributes before applying authorized
changes with `ALTER ROLE`; do not treat a duplicate-role error as success. Without
SUPERUSER authorization, grant only the approved privileges.

Add the exact realm-qualified principal to the instance's existing identity map
(`main` in the installation guide), preserving other entries and HBA rules:

```text
# pg_ident.conf: MAP SYSTEM-USERNAME DATABASE-USERNAME
main dedicated_account@MACHINE agent_role
```

Use the confirmed account/realm spelling, quote configuration tokens when needed,
and retain SSPI; do not substitute `trust` or database passwords. Have the existing
administrator execute `SELECT pg_reload_conf();` and inspect map/HBA parsing errors.

From the actual agent server session, verify explicit/default usernames with
`psql -X -w` and Npgsql, `session_user`, `current_user`, `rolsuper`, and `rolcanlogin`.
Confirm `method=sspi` in server authentication logs, not the GSSAPI view's flag.
Reuse the installation guide's client-verification procedure under the dedicated
identity. Record results and distinguish raw client checks from EWL integration.
Instance roles and mappings survive database refreshes; recheck after the next
authorized refresh and reprovision after an instance rebuild.

### SQL Server integrated authentication

When full instance administration is authorized, have an existing SQL administrator
provision the dedicated Windows login in **each relevant instance**, with default
database `master` and `sysadmin` membership. Substitute the actual computer/account:

```sql
USE [master];
IF SUSER_ID(N'MACHINE\agents') IS NULL
    CREATE LOGIN [MACHINE\agents] FROM WINDOWS WITH DEFAULT_DATABASE = [master];
IF IS_SRVROLEMEMBER(N'sysadmin', N'MACHINE\agents') = 0
    ALTER SERVER ROLE [sysadmin] ADD MEMBER [MACHINE\agents];
```

Inspect existing login type, default database, and membership; resolve conflicting
principals. Set the default database explicitly if needed. NULL membership is not
success. Windows Administrators membership is unnecessary.

EWL `update-data` replaces databases: database-level users/grants may disappear,
while instance logins and `sysadmin` membership survive ordinary database refreshes.
A sysadmin accesses restored databases as `dbo`. With database-specific access,
plan post-restore user mapping/grants instead. Reprovision after instance rebuilds.

From the server identity verify `ORIGINAL_LOGIN()`, `SUSER_SNAME()`, and
`IS_SRVROLEMEMBER('sysadmin')`. Recheck after the next authorized data refresh;
do not refresh a database solely to test access without authorization.

### MySQL, Oracle, and filesystem access

Use authorized configured credentials; do not assume familiar local passwords.
Check MySQL host matching, transport, and effective privileges. For Oracle, check
client/provider dependencies, TNS aliases, wallets, environment, and any external
authentication. Administrative access requires verifying privileges and connection
mode, not merely a successful login. Reprovision user-bound protected credentials.

Verify schema inspection/code generation, reads, writes, and migration permissions
through authorized development operations. Check backup/restore filesystem access
separately, including access for the database service account.

## 5. Complete and verify workstation configuration

Distribute maintained EWL integrations through DU sync when prerequisites are ready;
never edit generated agents, skills, or plugins. Follow target-system rules and
avoid overwriting running binaries. Honor any explicit sync deferral.

- Confirm a fresh session loads the global agent rules and can read an agreed
  EWL file and discover client systems from the configured locations.
- Verify configured dependencies and database access through a representative,
  authorized development operation, such as a system’s normal sync and build.
  Reuse evidence collected during setup rather than repeating successful checks.

Finish with a secret-free summary of resolved/skipped paths, configuration changes,
dependency versions, authentication ownership, database access, completed checks,
and remaining limitations or user-accepted waivers. Distinguish runtime evidence
from source review. Do not claim setup is verified merely because bootstrap returned.
