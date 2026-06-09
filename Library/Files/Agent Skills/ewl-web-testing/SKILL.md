---
name: ewl-web-testing
description: Drive a locally running EWL web application in a headed/headless browser (Playwright .NET) to verify flows, smoke-test pages, and reproduce UI bugs. Use when asked to navigate an EWL Web App as a specific user, exercise a page or form flow, or reproduce a UI issue locally. Tests are ad-hoc: edit a single throwaway Playwright .NET driver, run it, report results with screenshots, then tear down. Covers the no-hang process-launch patterns required to start EWL/Kestrel and IIS Express servers from an agent tool without wedging the call.
---

## Overview

This skill executes one-shot, browser-driven tests against a **locally running**
EWL web application. The test definition lives in a throwaway Playwright .NET
driver under `%LOCALAPPDATA%\Temp\opencode\web-test\Driver\`. The agent edits
`Program.cs` per request, runs it, reports results in chat with screenshots, and
tears down. There is no in-repo test project and nothing is committed.

This is the **generic, framework-level** skill. A given EWL system may also have
its own non-generated `web-testing` (or similar) skill that supplies
system-specific facts: ports, app path-base, user emails, page URLs, DB/queries,
and any legacy-app quirks. When such a skill exists, load it **in addition** to
this one and let it override the placeholders here. This skill owns the
mechanics; the system skill owns the specifics.

### Why a dedicated skill: the process-launch hang

The single most important thing this skill prevents is **wedging the agent's
command tool for a full timeout** when starting a server. The agent's
PowerShell/shell tool resolves a command only when the spawned process tree's
inherited stdout/stderr handles reach EOF. A long-lived server (Kestrel, IIS
Express) inherits those handles and never closes them, so a naive
`Start-Process -RedirectStandardOutput <file>` launch blocks the tool until it
times out -- even though the server itself started fine.

Always launch servers with one of the two **no-hang patterns** in
[Starting servers without hanging](#starting-servers-without-hanging). Never
launch a long-lived server with `Start-Process -RedirectStandardOutput` /
`-RedirectStandardError`, and never foreground a server.

> Note: the bundled `ewl-powershell` tool has been updated to resolve on the
> PowerShell process's `exit` event (with a short output-drain grace) rather
> than `close`, which prevents this hang at the tool level. The launch patterns
> below are still the correct way to start servers -- they capture the PID and
> logs cleanly and remain robust regardless of tool version or which shell tool
> is used.

### Defer-to-chat posture

When a test needs to choose among multiple candidates (a user, a record, a row)
and the criterion matches more than one, **stop and ask in chat**. Do not pick
autonomously. Enumerate the options (DB query or page scrape), present them, and
wait for the user's choice.

### Run everything through `ewl-powershell`

Use the `ewl-powershell` tool for all PowerShell in this skill, not the bash
tool. It passes commands directly to `powershell.exe`, avoiding bash quoting
issues with `$variables`, quotes, and Unicode paths.

---

## Placeholders this skill uses

Replace these with values from the system skill / `Standard.xml` / launch
profile before running. Throughout this document they appear in angle brackets.

| Placeholder | Meaning | How to find it |
|---|---|---|
| `<WebAppUrl>` | Base URL of the modern EWL Web App | The Kestrel/IIS launch profile (`Properties\launchSettings.json`) or the installation's `Standard.xml` web-application base URL. Often `https://localhost:<port>` plus a path-base. |
| `<WebAppPort>` | TCP port the Web App listens on | From `<WebAppUrl>`. |
| `<WebAppProject>` | The web-app `.csproj` directory name | The web project folder (may contain spaces, e.g. `Web App`). |
| `<RepoDir>` | Solution directory (contains the `.sln`) | The system root. |
| `<ImpersonatePath>` | Path to EWL's Impersonate page | Usually `<WebAppUrl>/ewl/impersonate` (see [Impersonation](#impersonation)). |
| `<StaffEmail>` / `<UserEmail>` | Email of a user with the role under test | The system skill or a DB query. |

Some systems front a **legacy app via a reverse proxy** (the modern Web App is
the only entry point). Those add a second server (e.g. IIS Express) and a second
port; the system skill describes that topology. The generic startup/teardown
here handles one or two servers.

---

## Preconditions

1. **`dotnet` >= 9 on PATH.**
2. **The web app builds and runs locally** as a Development installation (so
   anonymous impersonation is allowed; see [Impersonation](#impersonation)).
3. **Playwright Chromium installed** (one-time; Step 1 installs it).
4. If a legacy/proxied app is involved, its host (e.g. IIS Express) must be
   provisionable -- see the system skill.

---

## Step 1 -- Bootstrap the Playwright driver (one-time)

Scratch project location: `%LOCALAPPDATA%\Temp\opencode\web-test\Driver\`.

```powershell
$root = "$env:LOCALAPPDATA\Temp\opencode\web-test"
New-Item -ItemType Directory -Path "$root\run" -Force | Out-Null
dotnet new console -n Driver -o "$root\Driver" --force
dotnet add "$root\Driver\Driver.csproj" package Microsoft.Playwright
dotnet build "$root\Driver\Driver.csproj"
# The TFM segment below matches your dotnet major version; check bin\Debug.
& "$root\Driver\bin\Debug\net9.0\playwright.ps1" install chromium
```

If the driver project already exists, skip this step. Chromium install is a
no-op after the first run.

### Driver template

The driver dispatches on `--scenario <name>` and shares an `ImpersonateAsync`
helper. Add scenarios as needed. The impersonate click uses `NoWaitAfter` +
a long timeout + a tolerant `WaitForLoadStateAsync`, because the first postback
on a cold app can exceed Playwright's default 30 s action timeout.

```csharp
using System.Text.Json;
using Microsoft.Playwright;

var baseUrl = "<WebAppUrl>";
var scenario = "smoke";
var userEmail = "<UserEmail>";
var secondUserEmail = "";
var headless = true;
var screenshotDir = Path.GetFullPath( Path.Combine( AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots" ) );

for( var i = 0; i < args.Length; i++ )
	switch( args[ i ] ) {
		case "--scenario": scenario = args[ ++i ]; break;
		case "--user-email": userEmail = args[ ++i ]; break;
		case "--second-user-email": secondUserEmail = args[ ++i ]; break;
		// --*-file lets callers pass values containing spaces/commas/pipes without
		// fighting Start-Process -ArgumentList quoting (which splits on whitespace).
		case "--user-email-file": userEmail = File.ReadAllText( args[ ++i ] ).Trim(); break;
		case "--screenshot-dir": screenshotDir = args[ ++i ]; break;
		case "--base-url": baseUrl = args[ ++i ]; break;
		case "--headed": headless = false; break;
	}

Directory.CreateDirectory( screenshotDir );
var results = new List<object>();
var success = false;
string? error = null;

// buttonName is "Begin Impersonation" when no impersonation is in effect, and
// "Change User" when re-impersonating from an existing session. Pass the right one.
async Task ImpersonateAsync( IPage page, string email, string buttonName, string label ) {
	await page.GotoAsync( $"{baseUrl}/ewl/impersonate?returnUrl=", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120000 } );
	await page.GetByLabel( "User's email address" ).FillAsync( email );
	await page.GetByRole( AriaRole.Button, new() { Name = buttonName } ).ClickAsync( new() { NoWaitAfter = true, Timeout = 120000 } );
	try { await page.WaitForLoadStateAsync( LoadState.NetworkIdle, new() { Timeout = 120000 } ); } catch { }
	await page.ScreenshotAsync( new() { Path = Path.Combine( screenshotDir, $"impersonate-{label}.png" ), FullPage = true } );
	Console.WriteLine( $"[impersonate as {email}] now at {page.Url}" );
}

try {
	using var pw = await Playwright.CreateAsync();
	await using var browser = await pw.Chromium.LaunchAsync( new() { Headless = headless } );
	var ctx = await browser.NewContextAsync(
		new() { IgnoreHTTPSErrors = true, ViewportSize = new() { Width = 1400, Height = 900 } } );
	var page = await ctx.NewPageAsync();
	page.Console += ( _, msg ) => { if( msg.Type == "error" ) Console.WriteLine( $"[browser console:error] {msg.Text}" ); };
	page.PageError += ( _, err ) => Console.WriteLine( $"[browser pageerror] {err}" );

	switch( scenario ) {
		case "smoke": {
			await ImpersonateAsync( page, userEmail, "Begin Impersonation", "user" );
			var resp = await page.GotoAsync( baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120000 } );
			await page.ScreenshotAsync( new() { Path = Path.Combine( screenshotDir, "home.png" ), FullPage = true } );
			var status = resp?.Status ?? 0;
			var body = await page.InnerTextAsync( "body" );
			results.Add( new { step = "home", status, url = page.Url } );
			if( status >= 400 ) throw new Exception( $"Home returned HTTP {status}." );
			success = true;
			break;
		}

		// Add more cases here per request. See "EWL UI interaction patterns".

		default:
			throw new Exception( $"Unknown scenario: {scenario}" );
	}
}
catch( Exception ex ) { error = ex.ToString(); Console.Error.WriteLine( "FAILED: " + ex ); }

File.WriteAllText(
	Path.Combine( screenshotDir, "..", "report.json" ),
	JsonSerializer.Serialize( new { scenario, success, error, results, screenshotDir }, new JsonSerializerOptions { WriteIndented = true } ) );
return success ? 0 : 1;
```

---

## Step 2 -- Start the server(s)

### Pre-flight (EWL Critical Rule #1)

Verify nothing already listens on the target port(s) and that the web app's
output DLL isn't locked. A locked DLL means an instance is already running --
stop it before starting a fresh one.

```powershell
foreach( $port in @( <WebAppPort> ) ) {
	$c = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
	if( $c ) { Write-Warning "Port $port already in use (PID $($c[0].OwningProcess))" }
}
```

### Starting servers without hanging

There are two reliable patterns. **Pick by whether the server reads stdin.**

#### Pattern A -- Kestrel / `dotnet run` (capture logs): WMI `Win32_Process.Create`

A WMI-created process is parented to `WmiPrvSE`, entirely outside the agent
tool's process tree, so the tool call returns immediately. The child does its
own `> file` redirection, so logs are still captured.

```powershell
$repo   = "<RepoDir>"
$logDir = "$env:LOCALAPPDATA\Temp\opencode\web-test\run"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

# Note: project name may contain spaces -> keep it double-quoted inside the cmd line.
$cmdline = 'cmd.exe /c cd /d "' + $repo + '" && dotnet run --project "<WebAppProject>" --launch-profile Kestrel ' +
	'> "' + $logDir + '\webapp.log" 2> "' + $logDir + '\webapp.err.log"'
$r = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $cmdline }
"WMI Create ReturnValue=$($r.ReturnValue) (0=success); launcher PID=$($r.ProcessId)"
```

#### Pattern B -- IIS Express / stdin-sensitive console servers: `Start-Process` with NO redirection

IIS Express prints "Enter 'Q' to stop" and **quits the instant its stdin hits
EOF**. Redirecting its streams closes stdin -> it self-terminates. Launch it
hidden with **no** `-RedirectStandard*` at all (no inherited pipe -> no hang;
no stdin redirection -> it keeps running):

```powershell
# Site name and binding come from the system skill / applicationhost.config.
$iis = Start-Process -PassThru -WindowStyle Hidden `
	-FilePath "C:\Program Files\IIS Express\iisexpress.exe" -ArgumentList '/site:<SiteName>'
"IIS Express PID $($iis.Id)"
```

Do **not** add `-RedirectStandardOutput`/`-RedirectStandardError` to Pattern B.
If you need IIS Express logs, read its trace files, or rely on the proxied error
surfacing the system skill documents.

### Record PIDs for teardown

```powershell
$logDir = "$env:LOCALAPPDATA\Temp\opencode\web-test\run"
$webPid = @( Get-NetTCPConnection -LocalPort <WebAppPort> -State Listen -ErrorAction SilentlyContinue |
	Select-Object -ExpandProperty OwningProcess -Unique )[0]
$iisPid = @( Get-Process -Name iisexpress -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id )[0]
@{ web = $webPid; iis = $iisPid } | ConvertTo-Json | Set-Content "$logDir\pids.json"
```

> Resolve the legacy-app PID by **process name** (`iisexpress`), not by the
> listener's `OwningProcess` -- a shared IIS Express host can report a system
> PID for the socket.

### Readiness probes (bounded, never block)

Windows PowerShell 5.1 lacks `-SkipCertificateCheck`; use the policy shim. Keep
each request's timeout short and loop a bounded number of times. The **first**
hit to a server is slow (build/JIT); subsequent hits are fast.

```powershell
Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAll : ICertificatePolicy {
	public bool CheckValidationResult(ServicePoint sp, X509Certificate c, WebRequest r, int p) { return true; }
}
"@ -ErrorAction SilentlyContinue
[Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$ok = $false
for( $i = 0; $i -lt 60; $i++ ) {
	if( Get-NetTCPConnection -LocalPort <WebAppPort> -State Listen -ErrorAction SilentlyContinue ) {
		try { Invoke-WebRequest -Uri "<WebAppUrl>/" -TimeoutSec 10 -UseBasicParsing -MaximumRedirection 0 | Out-Null; $ok = $true; break }
		catch { if( $_.Exception.Response ) { $ok = $true; break } }  # any HTTP response = up
	}
	Start-Sleep -Seconds 2
}
"web ready: $ok"
```

For a proxied legacy app, an anonymous **HTTP 403** is a healthy "up" signal
(authorization is enforced); treat it as success. The system skill specifies
which status indicates readiness for that app.

---

## Step 3 -- Run the driver

The driver process is **short-lived** (it exits when the scenario finishes), so
`Start-Process -PassThru` + redirection + poll is safe here -- it does not hang
because the driver closes its pipes on exit.

```powershell
$dir = "$env:LOCALAPPDATA\Temp\opencode\web-test"
Get-ChildItem -LiteralPath "$dir\screenshots" -ErrorAction SilentlyContinue | Remove-Item -Force
Remove-Item -LiteralPath "$dir\report.json" -ErrorAction SilentlyContinue

$p = Start-Process -PassThru -FilePath "dotnet" -WindowStyle Hidden `
	-ArgumentList @( "run","--project","$dir\Driver","--",
		"--scenario","smoke","--user-email","<UserEmail>","--screenshot-dir","$dir\screenshots" ) `
	-RedirectStandardOutput "$dir\run\driver.log" -RedirectStandardError "$dir\run\driver.err.log"

# Poll for completion (driver exits on its own). Raise the cap for heavier scenarios.
for( $i = 0; $i -lt 150; $i++ ) {
	if( -not (Get-Process -Id $p.Id -ErrorAction SilentlyContinue) ) { "driver exited after $i s"; break }
	Start-Sleep -Seconds 1
}
if( Get-Process -Id $p.Id -ErrorAction SilentlyContinue ) { Stop-Process -Id $p.Id -Force; "driver killed at cap" }

Get-Content "$dir\report.json"
Get-Content "$dir\run\driver.log" -ErrorAction SilentlyContinue
Get-Content "$dir\run\driver.err.log" -ErrorAction SilentlyContinue
```

After the run, read `report.json` and the screenshots in `$dir\screenshots\`.
For chat reports, embed the screenshots with the `read` tool -- they render
inline.

### Passing args with spaces, commas, or pipes

`Start-Process -ArgumentList @(...)` joins entries with spaces and does **not**
auto-quote an entry that itself contains spaces -- it gets split before the
driver sees it. For any value that may contain whitespace, write it to a temp
file and pass `--<name>-file <path>` (the template handles `--user-email-file`):

```powershell
$f = "$dir\run\arg.txt"
[IO.File]::WriteAllText( $f, "value with spaces, commas | and pipes", [Text.Encoding]::UTF8 )
# then: --user-email-file $f
```

---

## Step 4 -- Teardown

Always run before ending the session, even on failure. `Stop-Process` on the
`dotnet` Web App PID also kills its child host.

```powershell
$logDir = "$env:LOCALAPPDATA\Temp\opencode\web-test\run"
$pids = Get-Content -LiteralPath "$logDir\pids.json" -ErrorAction SilentlyContinue | ConvertFrom-Json
foreach( $p in @( $pids.web, $pids.iis ) ) {
	if( $p -and (Get-Process -Id $p -ErrorAction SilentlyContinue) ) { Stop-Process -Id $p -Force }
}
Start-Sleep -Seconds 2
# Free any still-listening ports by killing the listener.
foreach( $port in @( <WebAppPort> ) ) {
	$c = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
	if( $c ) { Stop-Process -Id $c[0].OwningProcess -Force -ErrorAction SilentlyContinue }
}
# Sweep orphans from aborted runs (be conservative; match command line).
Get-Process -Name iisexpress -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Get-Process -Name dotnet -ErrorAction SilentlyContinue | ForEach-Object {
	$cl = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)" -ErrorAction SilentlyContinue).CommandLine
	if( $cl -like "*<WebAppProject>*" -or $cl -like "*\Driver*" ) { Stop-Process -Id $_.Id -Force }
}
```

If output DLLs stay locked afterward, look for orphaned
`dotnet`/`iisexpress`/`chrome` helpers spawned during the session.

---

## Impersonation

EWL ships an **Impersonate** page that lets you act as any user. In a
**Development** installation, EWL's framework dispatcher allows anonymous access
to it (no bootstrap login needed). This is the standard way to "log in as" a
user for a test.

- **URL:** `<WebAppUrl>/ewl/impersonate?returnUrl=`. **`returnUrl` is required**
  on the route; omitting it 404s. An empty value is fine -- EWL normalizes to
  path-segment syntax via a 307.
- **One email field** ("User's email address (leave blank for anonymous)") and
  **one submit button** whose label depends on session state:
  - **"Begin Impersonation"** -- no impersonation currently in effect.
  - **"Change User"** -- an impersonation is already active (the page also shows
    "End impersonation" / "Hide this warning"). Match the submit by the exact
    "Change User" accessible name.
- Standard two-step (e.g. staff then end-user):
  ```csharp
  await ImpersonateAsync( page, staffEmail, "Begin Impersonation", "staff" );
  await ImpersonateAsync( page, userEmail,  "Change User",         "user"  );
  ```
- EWL writes the impersonation cookie on the modern Web App. If the system
  proxies a legacy app, identity propagation across the proxy is the system
  skill's concern (commonly a request header injected by the proxy).

To verify **authorization** (that a user is *denied* a page), impersonate that
user and assert the page does **not** render the protected content (or returns
the expected denied/redirect response).

---

## EWL UI interaction patterns

EWL renders UI from C# component collections (no Razor). Useful, stable handles:

- **Page actions / "add" buttons** are hyperlinks. Match by role+name:
  `page.GetByRole( AriaRole.Link, new() { Name = "Add <thing>" } )`.
- **`EwfTable` rows** render as `<table> ... <tbody><tr>`. Clickable rows are
  hyperlink-activated; clicking the row (or a cell's text) navigates. Counting
  rows: `await page.Locator( "table tbody tr" ).CountAsync()`.
- **Form items** (`Get<Col>FormItem`) render a label you can target with
  `page.GetByLabel( "<Label>" )` for text inputs; read back with
  `InputValueAsync()`.
- **WysiwygHtmlEditor** renders a rich-text editor. Fill the editor body via its
  iframe (`page.FrameLocator( "iframe.cke_wysiwyg_frame, iframe[title*='editor' i]" ).Locator( "body" )`)
  or, if it is a contenteditable, `page.Locator( "[contenteditable='true']" )`.
- **Clean URLs** follow the page's URL patterns. An edit/add page with an
  optional id typically routes to `.../<segment>/<id>` (edit) and
  `.../<segment>/<nullSegment>` (e.g. `.../add`). Assert on `page.Url`.
- **PostBack timing:** EWL full postbacks usually 302 then GET. After a submit,
  wait for the navigation/network-idle (tolerantly) before screenshotting, so
  you capture the result page, not the mid-submit state.

### Capturing the real error on a failed action

EWL **Development** installations render the full exception (type, message,
stack) on the error page -- read the page body text and look for `System.` /
`Exception` to surface the root cause. Take a full-page screenshot of the error
page; it is the primary diagnostic artifact. For systems that proxy a legacy app
or use AJAX partial postbacks, the system skill explains how errors surface
there (often via XHR response bodies rather than the visible DOM).

---

## Reporting

In chat, summarize pass/fail per step from `report.json`, embed the key
screenshots with the `read` tool, and (on failure) quote the captured exception
text. Keep the driver edits minimal and scenario-scoped.

---

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| The start-server tool call hangs for the full timeout | A long-lived server was launched with inherited stdio (`Start-Process -RedirectStandardOutput`). Use [Pattern A (WMI)](#pattern-a----kestrel--dotnet-run-capture-logs-wmi-win32_processcreate) or [Pattern B (no redirect)](#pattern-b----iis-express--stdin-sensitive-console-servers-start-process-with-no-redirection). |
| IIS Express starts then immediately exits (port not listening) | Its stdin hit EOF (redirected streams). Use Pattern B: `Start-Process -WindowStyle Hidden` with **no** redirection. |
| Impersonate click times out at ~30 s | First postback on a cold app exceeds Playwright's default action timeout. Use `ClickAsync( new() { NoWaitAfter = true, Timeout = 120000 } )` + tolerant `WaitForLoadStateAsync` (already in the template). Re-running after warm-up is fast. |
| `GetByRole(Button, "Begin Impersonation")` not found | An impersonation is already in effect; the submit is labeled **"Change User"**. |
| Page returns 500 with "Failed to get a URL for ... handler does not match any of the parent's child URL patterns" | An edit/add page's `createParent()` doesn't match the page under which its URL pattern is registered. The child page must declare the list page as its parent (and the list registers the child pattern). |
| Readiness probe blocks a long time | Use short per-request `-TimeoutSec` and a bounded loop; the first server hit JITs/builds and is slow, later hits are fast. |
| Driver run "hangs" | The driver itself is short-lived; a true hang means the scenario is waiting on a selector. Lower selector timeouts and add `Console.WriteLine` checkpoints; the poll loop in Step 3 has a hard cap that kills it. |
| Port still listening after teardown | Kill by the listener's `OwningProcess`, then sweep orphan `dotnet`/`iisexpress`/`chrome`. |

---

## File locations

- Driver project: `%LOCALAPPDATA%\Temp\opencode\web-test\Driver\Program.cs`
- Screenshots: `%LOCALAPPDATA%\Temp\opencode\web-test\screenshots\`
- Run artifacts (logs, `report.json`, `pids.json`): `%LOCALAPPDATA%\Temp\opencode\web-test\run\` and the parent.
