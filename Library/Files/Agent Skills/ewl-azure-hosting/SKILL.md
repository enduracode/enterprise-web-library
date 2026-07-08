---
name: ewl-azure-hosting
description: Inspect and troubleshoot EWL systems hosted in Azure — locating an installation's App Service, Container App Job, and Log Analytics workspace, and reliably reading Container App (data migrator) console and system logs. Use when checking whether a deploy's data migrator ran, reading container logs for a hosted installation, or investigating an installation's Azure resources. Companion to ewl-azure-pipelines, which covers the build/deploy pipelines that create these resources.
---

## Overview

EWL systems deployed through the Azure deploy pipeline (see the
`ewl-azure-pipelines` skill) run as an **App Service**, with the
**data migrator** executed as a **Container App Job (CAJ)** during each deploy.
This skill covers how to find those resources for a given installation and how
to read their logs — most importantly the CAJ console output, which does **not**
appear in the pipeline log.

Everything here is read-only inspection. It assumes `az` CLI with the
`containerapp` extension and an authenticated session (`az account show`) that
can see the hosting subscription.

## Resource Naming and Layout

The deploy pipeline names resources deterministically (see
`ewl-azure-pipelines` → Naming Conventions):

- **App Service:** `app-{systemShortNameSlug}-{installationShortNameSlug}`
- **Container App Job:** `caj-{systemShortNameSlug}-{installationShortNameSlug}`
  (the ISU/setup variant is suffixed `-isu`)
- **Resource group:** `rg-{systemShortNameSlug}-{installationType}`, where
  `installationType` is `prod` for the Live installation and `intermediate`
  for all others
- Slugs come from `ToUrlSlug()` in Tewl's `StringTools`, so a system short name
  like "MyApp" and installation "Testing" yield `caj-myapp-testing` in
  `rg-myapp-intermediate`.

List the jobs to confirm exact names before querying:

```powershell
az containerapp job list --subscription $sub --query "[].{name:name, rg:resourceGroup}" -o table
```

The Container Apps **environment** (which owns the Log Analytics workspace) is
often in a shared resource group (e.g. `rg-general`) even when the job lives in
the installation's RG. Resolve it from the job rather than guessing:

```powershell
$envId = az containerapp job show --name $job --resource-group $rg --subscription $sub `
	--query "properties.environmentId" -o tsv
$envName = ($envId -split '/')[-1]; $envRg = ($envId -split '/')[4]
$ws = az containerapp env show --name $envName --resource-group $envRg --subscription $sub `
	--query "properties.appLogsConfiguration.logAnalyticsConfiguration.customerId" -o tsv
```

`$ws` is the Log Analytics **workspace GUID (customerId)** used by the log
queries below.

## Verifying a Deploy's Data Migrator Ran

The pipeline's "Run data migrator" task only **triggers** the CAJ and uploads a
staging blob; the migrator's real output runs inside the container and is not in
the pipeline log. To verify a specific deploy, correlate by time.

1. Get the build's finish time / the deploy task's timestamps (see the
   `ewl-azure-pipelines` skill or the build timeline).
2. List recent job executions and find the one whose `startTime` matches:

```powershell
az containerapp job execution list --name $job --resource-group $rg --subscription $sub `
	--query "reverse(sort_by([].{name:name, status:properties.status, start:properties.startTime, end:properties.endTime}, &start))[:8]" `
	-o table
```

3. Confirm the container exited cleanly via the **system** logs (exit code 0):

```powershell
# see "Reliable Log Queries" below for why this uses az rest, not az monitor
# filter: ContainerAppSystemLogs_CL | where Log_s contains "<executionName>"
```

A healthy run shows `SuccessfulCreate` → `Completed` →
`ProcessExited ... exit code: 0`.

### Expected Data Migrator Console Output

A logic-only deploy with no pending migrations is a **no-op**, and the healthy
console output reflects that:

```
EWL Data Migrator for <System Name>
Beginning Transaction
Rolling back transaction
```

`Rolling back transaction` followed by exit code 0 is **success** (no schema
changes to apply), not a failure. When migrations do apply, expect a
`Committing transaction` line instead. Note that Log Analytics may stamp several
console lines with the **same** batched `TimeGenerated`, so do not rely on the
returned order to infer emit order.

## Reliable Log Queries (critical)

Container App logs live in two Log Analytics tables:

- `ContainerAppConsoleLogs_CL` — the app's stdout/stderr (the migrator output)
- `ContainerAppSystemLogs_CL` — platform events (pod create, exit code, etc.)

Useful columns: `TimeGenerated`, `Log_s`, `Stream_s`, `ContainerName_s`
(the migrator container is `main`), `ContainerGroupName_s`
(`{execution}-{podsuffix}`, e.g. `caj-myapp-testing-<exec>-<podsuffix>`), and
`ContainerJobName_s`.

### Do NOT use `az monitor log-analytics query` to fetch log text

`az monitor log-analytics query` **silently returns an empty result** for any
query that projects the `Log_s` column's text (directly, via `project`, or via
a derived `extend`), and it also misbehaves with some operators like
`startswith` in the passthrough. Pure aggregations (`count`, `summarize`,
`distinct`, `getschema`) work, which makes the failure look intermittent and
confusing. **Do not fight this** by reshaping the KQL — it is a CLI
output-handling bug, not a query bug.

### Use `az rest` against the Log Analytics REST API instead

This returns raw JSON untouched and handles embedded newlines correctly:

```powershell
$ws = "<workspace-guid>"
$q = @'
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(2d)
| where ContainerGroupName_s startswith "caj-myapp-testing-<exec>"
| order by TimeGenerated asc
| project TimeGenerated, Stream_s, ContainerName_s, Log_s
'@
$bodyFile = Join-Path $env:TEMP "la-query.json"
@{ query = $q } | ConvertTo-Json -Compress | Out-File $bodyFile -Encoding ascii

$json = az rest --method post `
	--uri "https://api.loganalytics.io/v1/workspaces/$ws/query" `
	--resource "https://api.loganalytics.io" `
	--headers "Content-Type=application/json" `
	--body "@$bodyFile" -o json

$t = ($json | ConvertFrom-Json).tables[0]
$cols = $t.columns.name
$li = [array]::IndexOf($cols, 'Log_s'); $ti = [array]::IndexOf($cols, 'TimeGenerated')
foreach( $row in $t.rows ) { "[$($row[$ti])] $($row[$li])" }
```

Notes:
- The `--resource https://api.loganalytics.io` argument is required so `az`
  acquires a token for the Log Analytics data-plane audience.
- Write the KQL to a `--body @file` rather than inlining it, to avoid PowerShell
  and CLI quoting mangling the query.
- Always include an explicit `| where TimeGenerated > ago(...)` guard; the REST
  API's default time window can otherwise exclude the rows you want.
- Match the container group by the execution-name prefix
  (`startswith "caj-...-<exec>"`) since the full name has a pod-instance suffix.

## PowerShell / `az` Output Hygiene

On Windows PowerShell, `az` emits warnings (update notices, the "behavior…
altered by extension: containerapp" notice, "Not a json response") to **stderr**
as `RemoteException` records that interleave with stdout and corrupt captured
output — a frequent cause of `ConvertFrom-Json: Invalid JSON primitive: az.`

- Redirect stderr away when capturing JSON: `az ... -o json 2>$null`.
- Do not pipe `az ... 2>&1` into `ConvertFrom-Json`; the warning text ends up in
  the file/string.
- When a command still leaks warnings into a table, filter them out with
  `Where-Object { $_ -notmatch 'WARNING|CategoryInfo|FullyQualifiedErrorId|char:|~~~~|NotSpecified|^\s*\+|altered by' }`.

## Related

- `ewl-azure-pipelines` — the build/deploy pipelines that provision the App
  Service and Container App Job and trigger the data migrator.
- `ewl-database-migration` — what the data migrator actually does and how to
  author migrations.
- `ewl-server-side-console-app` — the pattern the data migrator and other
  server-side jobs follow.
