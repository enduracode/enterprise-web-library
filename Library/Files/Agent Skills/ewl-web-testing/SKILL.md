---
name: ewl-web-testing
description: Drive a local EWL web application with Playwright .NET to verify pages, uploads/downloads, browser caching, and user flows. Use when asked to navigate an EWL app as a user, exercise a form, or reproduce a UI issue. Bundles a standalone driver and fresh-machine bootstrap; includes safe server startup and teardown.
---

# EWL web testing

## Scope and prerequisites

Use the bundled standalone Playwright driver for one-shot tests of a **local** EWL
application. Edit the driver’s temporary copy for each scenario. No test-project
changes or commits in the application repository are needed.

Load a system-specific web-testing skill/reference if available. Keep application
paths, user identities, record IDs, and database cleanup details there; this skill
owns generic mechanics. When several users or records match a test criterion,
enumerate the candidates and ask the user to choose. Do not pick one silently.
Prefer explicitly authorized temporary records over changing existing data.

Required: Windows PowerShell 5.1+, a .NET 10 SDK, the skill’s complete directory,
and a locally configured/buildable EWL application and database. First bootstrap
needs NuGet and Playwright browser-download access. No previous OpenCode temp
directory, driver, browser installation, cookies, or authentication state is needed.
Run PowerShell with `ewl-powershell`; follow the harness’s file-operation rules.

## Bundled files and source ownership

```text
ewl-web-testing/
  SKILL.md
  scripts/Bootstrap.ps1
  scripts/Run.ps1
  templates/Driver/
    Driver.csproj
    Directory.Build.props
    Directory.Build.targets
    Program.cs
    WebTestHelpers.cs
```

The project pins Microsoft.Playwright and targets .NET 10. Empty Directory.Build
files isolate it from a parent application’s MSBuild imports. It deliberately has
no EWL dependency. Maintain these sources in EWL’s `Library/Files/Agent Skills/`;
the DU recursively distributes them to `.agents/skills/` and `.claude/skills/`.
Never build in the skill directory: `bin`, `obj`, browsers, test scenarios, logs,
screenshots, and authentication artifacts belong in a disposable workspace.

## 1. Bootstrap a fresh workspace

Resolve `$skill` from the loaded skill’s actual location; do not hard-code a
developer’s home directory. Use either generated copy, not both.

```powershell
$skill = '<absolute path to this ewl-web-testing directory>'
& "$skill\scripts\Bootstrap.ps1"
```

Record the printed workspace path. The script creates its default parent under
`$env:LOCALAPPDATA\Temp\opencode` if missing, copies the entire driver template,
restores/builds it, and installs matching Chromium **inside that workspace**.
This avoids dependence on, or garbage collection of, a shared browser cache.

To choose a workspace, pass `-Workspace '<new absolute directory>'`; its parent
must exist. An existing destination is rejected rather than overwritten. After a
failed bootstrap, inspect the logs and either remove only that disposable directory
or select another new one. Never copy `bin`/`obj` into the maintained template.

```powershell
$workspace = '<path printed by bootstrap>'
& "$skill\scripts\Run.ps1" -Workspace $workspace -Scenario self-check
```

`self-check` needs no server or credentials. It launches Chromium, generates PNG
and PDF fixtures, uploads in-memory bytes, downloads a fixture and verifies its
bytes, then writes `report.json` and `screenshots/success.png` inside that invocation’s
unique artifact directory (see below). A nonzero exit is
a failure. Run.ps1 reinstates `PLAYWRIGHT_BROWSERS_PATH` for each run and restores
the caller’s previous environment value afterward.

## 2. Discover application URL and start servers

Distinguish the **server origin** (e.g. `https://localhost:8700`) from the
**canonical application base URL** (which may include an IIS-style path base).
Read the launch profile and installation config, then confirm redirects and
generated links in the running app before constructing resource URLs. Preserve
the path base and the application’s query/path-segment parameter syntax. Do not
infer every route from its C# filename.

Build/regenerate the application as needed. Before overwriting application
binaries, check for relevant running instances and ask the user to stop blockers.
Before starting a server, check its ports. Reuse an appropriate existing instance
or ask; never kill a pre-existing server merely because it owns the port.

```powershell
Get-NetTCPConnection -LocalPort <port> -State Listen -ErrorAction SilentlyContinue
```

### No-hang server startup

Do not foreground a long-lived server or use Start-Process with redirected streams
for it. Inherited output handles can keep the command tool waiting indefinitely.
Use one of these patterns, recording launcher/host PIDs immediately in
`<workspace>/run/processes.json` for cleanup even if subsequent steps fail.

**Kestrel: WMI launch, redirection inside cmd.exe.** Verify the workspace’s `run`
directory exists first. Use the process creation API’s working-directory argument.

```powershell
$repo = '<solution directory>'
$project = '<absolute web project path>'
$log = Join-Path $workspace 'run\webapp.log'
$line = 'cmd.exe /c dotnet run --no-build --project "' + $project +
    '" --launch-profile Kestrel > "' + $log + '" 2>&1'
$launcher = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
    CommandLine = $line; CurrentDirectory = $repo
}
if ($launcher.ReturnValue -ne 0) { throw 'Server launch failed.' }
$launcher.ProcessId
```

**IIS Express: hidden process, no stream redirection.** IIS Express can exit as
soon as redirected stdin reaches EOF.

```powershell
$iis = Start-Process -PassThru -WindowStyle Hidden -FilePath 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList '/site:<site>'
$iis.Id
```

Probe readiness with short request timeouts and a bounded retry loop. An expected
authentication redirect/403 can demonstrate readiness; a listener alone does not.
Playwright’s `IgnoreHTTPSErrors` handles the local certificate. PowerShell 5.1
does not support `Invoke-WebRequest -SkipCertificateCheck`.

## 3. Run and extend the temporary driver

```powershell
& "$skill\scripts\Run.ps1" -Workspace $workspace -Scenario smoke -BaseUrl '<canonical app base URL>' -UserEmail '<chosen user>'
```

Omit `-UserEmail` for anonymous access. Use `-Headed` to display Chromium. Run.ps1
passes arguments directly to dotnet, including paths/emails containing spaces;
avoid Start-Process argument-array quoting pitfalls. Set the command timeout to
suit the scenario (normally 180000–300000 ms), and use bounded Playwright waits.

Each Run.ps1 invocation prints `Run artifacts: <workspace>/results/<run-id>` and
passes that unique directory to the driver as `--artifacts`. Reports, screenshots,
and scenario record files belong under that directory; repeated scenarios and
retries preserve earlier evidence. Record each printed path and read its own
`report.json`, not a shared workspace-level report. The workspace-level `run/`
directory still holds server logs and the process ledger described above.

The template creates a fresh context per invocation. It writes a machine-readable
report, captures a failure screenshot/page text, closes the browser, and runs a
stack of scenario cleanup actions in `finally`. Add actual content/authorization
assertions: HTTP 200 alone does not prove that the intended page was rendered.
Record each created test ID immediately in `run/records.json`, then register its
cleanup action. A process kill bypasses finally, so the record file must support
manual recovery. Never log credentials or persist authentication state in a repo.

### Active-page evidence in multi-page/user scenarios

Call the template’s `setActivePage( scenarioPage )` before each flow on another
page or user context. It selects the success/failure screenshot page and attaches
browser-error tracking once per page. Failure evidence includes the active page’s
URL in `report.json` and `run/failure-url.txt`, plus its screenshot and body text;
the original landing page is not a substitute for the page that actually failed.

Keep scenario pages and contexts open until the outer failure handler has captured
evidence. Register additional context/page disposal on the `cleanup` stack rather
than using a narrower `await using` scope that disposes them before the outer
`catch` runs. Record created IDs and register cleanup before waiting for the next
UI assertion, so a failed wait does not leave an untracked record or message.

### Impersonation

In a Development installation, anonymous access to EWL’s impersonation page is
allowed. Use `WebTestHelpers.ImpersonateAsync`. The route is
`<app-base>/ewl/impersonate?returnUrl=`; `returnUrl` is required. The helper accepts
ASCII or curly apostrophes in the email label. Use `changingUser: true` for an
existing impersonation session (button “Change User” rather than “Begin
Impersonation”). Confirm the resulting identity and page, not just the click.

### Development email tests

EWL `EmailStatics` does not send network email in a `Development` installation.
It writes `.eml` messages to `Outgoing Dev Mail` under
`ConfigurationStatics.EwlFolderPath` (normally `C:\Enterprise Web Library`).
Real recipient addresses in these local messages are not a reason to skip an
in-scope email flow or ask for separate permission. Exercise the action and
verify the newly generated message, including recipients, subject, body, and
attachments as relevant. Track test-created messages and remove only those
during cleanup. Account for the action's database changes as usual. Do not infer
this behavior for Intermediate/Live installations or direct SMTP/HTTP code that
bypasses EWL. The implementation is `Core/Email/EmailStatics.cs`.

Parse `.eml` files with [MimeKit](https://github.com/jstedfast/MimeKit)
([API documentation](https://mimekit.net/docs/html/)), which handles multipart MIME,
Base64, quoted-printable, and character encodings. MailKit uses MimeKit for message
parsing, but no SMTP/IMAP client is needed for local pickup files. Add and pin the
MimeKit package only in the temporary driver when an email scenario needs it.
Do not regex the raw message or Base64-decode everything after the first blank
line; MIME boundaries and individual part headers must be interpreted first.

```csharp
using var message = MimeKit.MimeMessage.Load( mailPath );
var recipients = message.To.Mailboxes.Select( i => i.Address );
var subject = message.Subject;
var body = message.TextBody ?? message.HtmlBody ?? "";
```

Snapshot pickup-file paths before the action, then identify new messages using
the expected recipients, subject, and test marker. Record the identified file
path and register its cleanup immediately, before parsing/asserting the body or
waiting for another UI state. Extract login codes or action links from the
expected decoded-body region, not the first arbitrary six-digit sequence. Remove
only identified test-created messages, including when parsing or assertions fail.

### Authentication throttling versus general request limits

EWL authentication has its own limits, independent of general HTTP request
throttling. Identify the actual response and limiter before retrying; do not
assume every throttle is HTTP 429 or shares a universal cooldown.

- Login-code sending has a cooldown keyed by normalized email address (one minute
  in the current source), with a user-visible remaining-wait message.
- Password login and reauthentication use separate rate limiters (currently one
  replenished attempt per 30 seconds with burst capacity five). Login-code validity
  and remaining-attempt restrictions are separate again.
- General request limits vary by request method and authenticated user or client
  IP. Their response and recovery path differ from authentication form messages.

Inspect the applicable installed EWL version and any system-specific behavior:
`Core/UserManagement/IdentityProviders/LocalIdentityProvider.cs`,
`Core/EnterpriseWebFramework/UserManagement/AuthenticationStatics.cs`, and
`Core/EnterpriseWebFramework/Request Dispatching/RequestDispatchingStatics.cs`.
Source-checkout defaults may be newer than the application’s pinned package.

For a retry unrelated to testing throttling, recognize the specific cooldown
response, honor the displayed wait (or `Retry-After` when supplied), and retry only
within a bounded count and overall timeout. Log the throttle and retry separately
from application failures. Do not hide unexpected authentication failures behind
blanket retry loops or count a throttle message as successful authentication.
A fresh browser context does not reset server-side limits. Use distinct temporary
identities when appropriate, but preserve the same identity when that is the
behavior being tested; do not clear server caches or restart it to bypass limits.

### EWL interactions and helper methods

| Task | Pattern |
|---|---|
| Page action | `GetByRole( AriaRole.Link, new() { Name = "Add …", Exact = true } )` |
| Expand section | `ExpandSectionAsync( page, heading )` matches the button “Click to Expand …”. Plain GetByText can also match hidden select options. |
| Enhanced select | `SelectOptionByLabelAsync( select, label )` selects the hidden native select with Force and dispatches input/change. Use visible widget interactions when testing that widget itself. |
| Upload | `UploadBytesAsync( input, name, contentType, bytes )` uses FilePayload, avoiding fixture-file paths. |
| Fixtures | `CreatePngAsync( page, color )` uses canvas; `CreatePdfAsync( context, text )` uses Chromium PDF output in a separate page. |
| Row selection | Locate the exact file link/button, then `Locator( "xpath=ancestor::tr" ).GetByRole( AriaRole.Checkbox )`. |
| Confirmation | EWL ConfirmationButtonBehavior opens an HTML dialog with “Continue”; use a dialog/button locator, not a JavaScript dialog handler. |
| Download | `DownloadAndAssertBytesAsync( page, action, expected )` captures secondary-response downloads and compares bytes. |
| New-tab link | `RunAndWaitForPopupAsync`, then check its URL and response/content. |
| Rich-text editor | Inspect for CKEditor iframe (`iframe.cke_wysiwyg_frame`) or contenteditable; use the actual editor present. |

Scope duplicate buttons to the intended form item/section; do not blindly use
First/Nth to resolve ambiguity. Indices are acceptable only when the scenario
explicitly identifies the corresponding collection/order. Wait for navigation
or an expected visible result after submission, then capture screenshots.

Icon-bearing actions can have accessible names that differ from their visible
text, so `Name = "New Article", Exact = true` may fail despite the link being
present. Inspect the rendered accessible name (for example with the locator’s
`AriaSnapshotAsync`) and scope to the intended navigation area or section. Prefer
an exact match when it works; otherwise use an inspected, word-boundary regex:

```csharp
navigation.GetByRole( AriaRole.Link, new() {
	NameRegex = new System.Text.RegularExpressions.Regex( @"\bNew Article\b" )
} );
section.GetByRole( AriaRole.Button, new() {
	NameRegex = new System.Text.RegularExpressions.Regex( @"\bFollow\b" )
} );
```

Loose case-insensitive substring matches can confuse opposing actions:
`Follow` matches `Unfollow`, and `Favorite` matches `Unfavorite`. Use exact names
or word boundaries, and after an action assert that the new state appears and
the previous state disappears. Do not resolve ambiguity with First/Nth or weaken
assertions simply because an icon changed the accessible name.

### Date controls

The current EWL `DateControl` renders a Duet custom element,
`duet-date-picker.ewfDc`, not a jQuery `input.hasDatepicker`. Scope that locator to
the intended form item when several dates appear on a page. Finding or counting
these elements verifies rendering only; it does not verify date validation.

EWL initializes the picker asynchronously. Before interacting, wait for
`customElements.whenDefined('duet-date-picker')` and the selected element's
`componentOnReady()`. Its `identifier` attribute identifies the actual text
input. The current adapter displays and parses `m/d/yyyy`; the custom element's
`value`, `min`, and `max` attributes use ISO dates.

For typed-date and invalid-date tests, fill the actual text input. EWL deliberately
submits that input's text rather than the picker's hidden/ISO value, allowing
invalid text to round-trip through server validation. Merely setting the custom
element's `value` or a hidden field does not exercise that path. Calendar-selection
flows and page-modification behavior also involve the `duetChange` event; use the
picker UI when that behavior is the subject of the test.

Submit the relevant form and assert validation messages or persisted values to
claim validation coverage. Date-control rendering, `LocalDate` type checks, and
database leap-day/DST-date round-trips are distinct checks. Inspect
`Core/EnterpriseWebFramework/Form Controls/Date and Time/DateControlSetup.cs` when
selectors, initialization, or submission behavior differ from these details.

### Testing browser caching

Keep the **same browser context alive** across initial retrieval, replacement,
and retrieval through the same URL. Navigate a page to the application origin
before `FetchInBrowserAsync`; normal fetch uses browser caching and same-origin
credentials. Compare status, `CacheControl`, `ETag`, and `GetBody()` before/after.
The helper chunks Base64 encoding so larger payloads do not overflow JavaScript’s
argument limit. Use ordinary-sized fixtures since it buffers the complete body.

Do not enable request routing/interception or disable cache for this test:
Playwright routing disables HTTP caching. Saving storage state saves authentication
data, **not the browser HTTP cache**. A fresh process/context loses the warm-cache
condition. `context.APIRequest` is useful for HTTP checks but does not demonstrate
browser-cache behavior.

Use `AssertConditionalGetAsync( context.APIRequest, url )` separately to assert
that an unchanged ETag yields 304. Automatic browser revalidation may expose a
200 with cached content to JavaScript even when the wire response was 304.

In EWL, `EwfSafeResponseWriter.urlVersionString` means a version **in the request
URL**, not merely a database timestamp. For stable mutable URLs, pass an empty
string so browsers revalidate; EWL derives validators from the file. Test that
replacement changes both contents and ETag, and that server memory caching does
not return old bytes. Test absent/deleted files as well. A browser already holding
an old long-lived response may not see new headers until it revalidates or the
user bypasses its cache; deployment cannot retroactively expire that response.

## 4. Report and tear down

Read each invocation’s `results/<run-id>/report.json`, failure URL/page text if any,
and key screenshots with the read
tool. Report each actual pass/failure, not just the final process exit. Do not
claim that a link-picker check verifies saving rich-text content or vice versa.

Always finish cleanup, including on failures:

1. Remove only test-created records/files via application modifications/UI. Verify
   referenced file/BLOB cleanup; reclaim only identified unreferenced test BLOBs
   if necessary. Keep cleanup checks scoped to recorded IDs and test markers.
2. Stop only servers started for this run, using recorded launcher/host PIDs and
   verifying their current executable/command line before termination. Do not
   sweep all dotnet, IIS Express, Chrome, or port-owner processes. A shared HTTP.sys
   listener PID is not an IIS Express process ID.
3. Confirm owned ports/processes are released. The driver closes its own browser.
4. Retain reports/screenshots as needed for review; remove authentication state
   and other sensitive artifacts. Delete the disposable workspace when finished
   with its evidence. Never copy it back into the skill directory.

## Verifying changes to this skill

After source changes, use a local DU containing them to regenerate into a client
system. Verify the scripts AND all driver files arrive in both generated skill
directories. Bootstrap into a new empty workspace (the isolated browsers folder
must start empty), run self-check, and exercise smoke against a local app.
Run repeated scenarios and verify earlier reports/screenshots remain intact.
Intentionally fail on a second page and confirm its URL, text, and screenshot are
captured before cleanup. Use deterministic fixtures to verify icon-bearing and
opposing-action selectors and MIME decoding without sending network email.
Exercise HTTP helpers against a deterministic local endpoint when changing them.
Use `ewl-agent-testing` for a fresh headless OpenCode run that discovers the skill
and bootstraps independently; inspect its transcript and clean up its session.
Do not rely on the current session’s already-loaded skill text or previous temp
artifacts as evidence of a fresh-machine bootstrap.
