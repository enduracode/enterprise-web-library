## Development Modes

Standard mode is the default. `/adhoc` selects ad hoc mode for the current session; `/standard` restores standard mode. Keep the selected mode across requests until changed, including in session summaries. These commands select the workflow below rather than overriding unrelated rules.

| Activity | Standard | Ad hoc |
|---|---|---|
| Pre-edit formatting and formatting-only commit | Follow the workflow below | Skip |
| Abstraction-review and cleanup subagents | Follow the workflow below | Skip |
| Running-application preflight, application startup, runtime verification | When relevant to the task | Only when explicitly requested |
| Generation, builds, and non-runtime verification | As needed for the change | As needed for the change |

Do not perform skipped formatting or review workflows manually as a substitute. Explicit user instructions may waive verification or request a particular check without changing the session mode. Source ownership, code conventions, and preservation of existing work apply in both modes.

## Project Overview

Enterprise Web Library (EWL) is an opinionated .NET framework for building web-based enterprise software.
The solution is C# targeting **net10.0**, using ASP.NET Core with EWL's own
component-based web framework layered on top (no Razor views). Source control is
Mercurial. If a `.hg` directory is present, load the `ewl-mercurial` skill
before running any version control commands.

### Key Projects

| Project | Purpose |
|---|---|
| `Core\` | Main EWL library (NuGet package) |
| `Library\` | System-specific library; references Core plus providers |
| `Website\` | Demo ASP.NET Core web application |
| `Tests\` | NUnit test project |
| `Development Utility\` | CLI tool for code generation and build ops |
| `Providers\` | Pluggable provider implementations |

Solution file: `Enterprise Web Library.slnx`

---

## Build Commands

```shell
# Restore packages
dotnet restore "Enterprise Web Library.slnx"

# Build (Debug, the default)
dotnet build "Enterprise Web Library.slnx"

# Build (Release) -- note: Tests project is excluded from Release
dotnet build "Enterprise Web Library.slnx" -c Release
```

### Code Generation (Development Utility)

The EWL Development Utility (DU) performs code generation, populating
`Generated Code\` folders in every project. It does **not** run automatically
during builds; it must be run explicitly.

Use `Solution Files\Update Dependent Logic.ps1` for the latest published DU.
To exercise local changes to DU logic or generation inputs under `Library\Files\`, use the local DU:

```shell
dotnet run --project "Development Utility/Development Utility.csproj" -- sync
```

### Build Server Flow

The build server (EWL System Manager ISU) uses only the **released DU**:

1. Clone the repository and `dotnet restore`.
2. Install the latest `Ewl.DevelopmentUtility` dotnet tool and run its DU with
   `sync`.
3. `dotnet restore --force` (picks up regenerated `Directory.Build.props`).
4. Run the released DU again with `export-logic` (builds and packages).

Changes pushed to EWL must compile against output from the currently released DU. Publish generation changes before pushing code that depends on their new output.

### Other Helper Scripts

- `Solution Files\Export EWL to Local Feed.ps1` -- exports EWL as a NuGet package to a local feed

---

## EWL Accomplishments

EWL release notes are called **accomplishments** and are stored outside version control. When asked to write an accomplishment or EWL release note, provide the text in chat or in a temporary Markdown file outside the repository. Do not create or modify version-controlled EWL documentation for these requests.

Match the existing accomplishment style: a short paragraph prefixed with the feature name or `Ad hoc -`, explaining the change and any required migration steps. Explicitly identify breaking changes. Keep implementation details, verification reports, and lengthy checklists out of the accomplishment unless requested.

---

## Test Commands

Tests use NUnit in `Tests\Tests.csproj`; always use Debug because this project is excluded from Release.

```shell
dotnet test "Tests\Tests.csproj"
dotnet test "Tests\Tests.csproj" --filter "FullyQualifiedName~Tests.DoubleTools.ToMoneyString.Test"
```

---

## Standard Development Workflow

This formatting and review workflow applies only in standard mode. Use the current harness's subagent tool.

If the user asks to leave changes uncommitted, says they will commit themselves, or otherwise gives instructions that may conflict with the required pre-edit formatting commit, ask whether formatting-only commits are still permitted. Do not assume those commits or pre-edit formatting itself are waived. Resolve this before making functional edits to files requiring pre-edit formatting.

1. Before the first functional edit to each clean, existing C# or XML/XSD file in a task, invoke `ewl-cleanup` to format only, not inspect, and commit any formatting changes. This authorizes formatting-only commits of those files. Skip this pre-edit step for new or already-modified files; preserve existing work and continue. Do not repeat it for subsequent edits or attempt it retrospectively when returning from ad hoc mode.
2. Make the requested changes and regenerate affected output when required below.
3. After functional C# changes, invoke `ewl-abstraction-review` on the changed, non-generated C# files. Address applicable findings.
4. Invoke `ewl-cleanup` to format and inspect, without committing, only the changed non-generated `.cs`, `.xml`, and `.xsd` files. Exclude other file types and C# files containing `<auto-generated`. Include new and previously modified files. Reported fixes are already applied; address only remaining issues.
5. Verify the final code, including relevant inspection fixes, with generation, builds, and tests appropriate to the change. Honor explicit user instructions to omit verification and report what was not run.

In standard mode, check for relevant running applications when needed before overwriting their binaries. Identify blockers and ask the user to stop them; continue independent work. A file-access failure alone does not prove an application lock.

---

## Shell and Path Handling

- Prefer dedicated tools for file reads, searches, and edits.
- Use `ewl-powershell` for PowerShell commands; do not invoke PowerShell through another shell tool.
- Follow the actual shell tool's description rather than assuming Bash or PowerShell syntax. Quote paths containing spaces or non-ASCII characters.
---

## Critical Development Rules

1. **Edit sources, not generated output.** Do not edit `Generated Code\`, `.ewlt.cs`, or other files marked as generated. Reading generated output is encouraged when needed to understand or verify behavior.
2. **After changing generation inputs** (DU code, `Library\Files\`, schema, page declarations, or configuration), run `sync`, build, and inspect the relevant generated output unless the user explicitly waives these steps. Use a DU containing the changes; client systems' installed packages do not automatically consume this checkout.
3. Configuration uses XML/XSD under `Configuration\`; UI uses EWL components, page classes use generated routing, and data access uses the generated data-access layer.

### Agent Configuration Ownership

`updateOpenCodeConfig` in `Development Utility\Operations\UpdateDependentLogic.cs` generates the following. Edit its code or the corresponding `Library\Files\` sources, then regenerate using the appropriate DU.

| Generated output | Source |
|---|---|
| `opencode.jsonc`, `.mcp.json`, `.claude\settings.json` | `updateOpenCodeConfig` |
| EWL-prefixed agents in `.opencode\agents\` and `.claude\agents\` | `Library\Files\Agents\`, including Claude Code preambles |
| EWL-prefixed skills in `.agents\skills\` and `.claude\skills\` | `Library\Files\Agent Skills\` |
| EWL-prefixed `.opencode\tools\` files; `.claude\ewl\` server files | `Library\Files\OpenCode Tools\` |
| `.opencode\plugins\ewl\`, `.opencode\plugins\ewl.js`, `.claude\hooks\` | `Library\Files\OpenCode Plugins\` |

Non-EWL agents, skills, tools, and system-specific commands outside these owned locations may be edited directly. This repository's `AGENTS.md` is hand-maintained.

---

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespaces | PascalCase matching dirs | `EnterpriseWebLibrary.Caching` |
| Classes / Interfaces | PascalCase | `AppMemoryCache`, `SystemUser` |
| Public methods | PascalCase | `GetCacheValue()`, `InitStatics()` |
| Private methods | camelCase | `tick()`, `getControls()` |
| Internal readonly fields | PascalCase | `internal readonly string Host;` |
| Private fields | camelCase | `currentTimeGetter`, `nonsecurePort` |
| Local variables | camelCase | `outputFolder`, `singleTestRow` |
| Parameters | camelCase | `valueCreator`, `configurationFolderPath` |
| Private constants | camelCase | `private const int tickInterval = 10000;` |

### File Structure

- **File-scoped namespaces** (no braces): `namespace EnterpriseWebLibrary;`
- `using` directives at the top: `System` namespaces first, then everything else
  in alphabetical order; one `using` per line; no blank lines between groups

### Formatting

- Preserve non-ASCII typography in prose; use the typography tool when needed. Preserve code syntax and literal data.
- **Tabs** for indentation
- **Spaces inside parentheses**: `( value )`, `( "text" )`, `( state )`
- **Spaces inside attribute brackets**: `[ Test ]`, `[ TestFixture ]`, `[ DllImport( "kernel32" ) ]`
- **Spaces inside angle brackets for generics are NOT used**: `Func<Instant>`, `IReadOnlyCollection<T>`
- Opening brace on same line as declaration: `public class Foo {`
- Expression-bodied members for single-expression methods
- Multi-line argument lists: closing paren/brace on same line as last arg, or
  each arg on its own line indented with a tab

### Type Patterns

- **Nullable reference types** enabled: `string?`, `Func<Instant>?`
- Strings should be nullable only when `null` means something distinct from empty. Optional string parameters normally default to `""`.
- **`var`** used liberally for local variables
- **`partial class`** used extensively for code-gen integration
- **`IReadOnlyCollection<T>`** preferred over `List<T>` for return types
- **Tuples** for multi-return values: `( bool secure, string host, int port, string path )`
- Extension methods used heavily: `.ToCollection()`, `.Materialize()`, `.Any()`

### Error Handling

- `throw new Exception( "message" )` for unexpected / invalid states
- Custom exceptions: `UserCorrectableException`, `UnexpectedValueException`, `DoNotEmailOrLogException`
- Cleanup-on-failure pattern: `try { ... } catch { CleanUpStatics(); throw; }`
- Null-forgiving operator (`!`) used when internal state is guaranteed post-init:
  `currentTimeGetter!()`. Also used on fields captured in lambdas before
  assignment to avoid nullable type inference: `field!.ToCollection()`
- Preserve applicable EWL/TEWL helpers instead of replacing them with manual equivalents. Fix receiver nullability with `!` or `?` as appropriate.

### Comments

- XML doc comments (`///`) on public API members with `<summary>` and `<param>` tags
- Inline `//` comments to explain "why", not "what"
- Block comments (`/* */`) used sparingly, mainly in tests

---

## Project Dependencies

Dependencies are defined in `Core\Core.csproj` (for the main library) and in
each of the `Providers\` projects.

**Ewl.Tools** (assembly `Tewl`) supplies utilities such as `IoMethods` and `StringTools`. Source: https://github.com/enduracode/tewl (integration branch).

### Local TEWL Development

Locate the TEWL checkout using machine-level instructions. The outer directory is Mercurial; `Shared\` is a git repo.

- Export to local feed: run `"Solution Files/Export Package to Local Feed.bat"`
  in the outer TEWL directory
- Local feed: `C:\Enterprise Web Library\Local NuGet Feed`
- After exporting, update the `Ewl.Tools` version in `Core\Core.csproj`
