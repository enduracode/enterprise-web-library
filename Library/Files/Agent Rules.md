**IMPORTANT: Review [Critical Development Rules](#critical-development-rules) before performing any tasks. Subagents are invoked via the `task` tool with `subagent_type`.**

This system uses the Enterprise Web Library (EWL), an opinionated .NET framework
for building web-based enterprise software. If a `.hg` directory is present, this repository uses
Mercurial for version control; load the `ewl-mercurial` skill before running
any version control commands.

## Build Commands

```shell
# Restore packages
dotnet restore "Solution Name.sln"

# Build (Debug, the default)
dotnet build "Solution Name.sln"

# Build (Release) -- note: Tests project is excluded from Release
dotnet build "Solution Name.sln" -c Release
```

### Code Generation (Development Utility)

The EWL Development Utility (DU) performs code generation, populating
`Generated Code\` folders in every project. It does **not** run automatically
during builds; it must be run explicitly.

In Visual Studio, run `Update-DependentLogic` in the Package Manager Console.

Outside Visual Studio, run the DU executable directly. First, determine the EWL
package name and version by reading `Library\Library.csproj` (look for the
`PackageReference` whose `Include` matches `Ewl` followed by alphanumerics,
e.g. `EwlBill`). Then locate the NuGet global-packages folder by running:

```shell
dotnet nuget locals global-packages --list
```

The DU executable is at
`<global-packages>/<package-name>/<version>/tools/Development Utility/EnterpriseWebLibrary.DevelopmentUtility.exe`.
Run it with the directory containing the `.sln` file as the first argument and
`UpdateDependentLogic` as the second:

```shell
"<path-to-DU>/EnterpriseWebLibrary.DevelopmentUtility.exe" "<solution-directory>" UpdateDependentLogic
```

### Test Commands

Test framework: **NUnit**. Test project: `Tests\Tests.csproj`.
The Tests project does NOT build in Release configuration; always use Debug.

```shell
dotnet test "Tests\Tests.csproj"
dotnet test "Tests\Tests.csproj" --filter "FullyQualifiedName~Tests.SomeClass.SomeTest"
```

---

## Code Inspection

After making functional changes to C# or XML/XSD files, run both inspection
subagents in the order below.

### Abstraction Review

If any changed files are C# files, invoke the `ewl-abstraction-review` subagent
with the list of changed C# files. It reviews diffs for cases where manual code
could be replaced with TEWL or EWL abstractions. Fix any findings it reports.

### ReSharper Format/Inspect

Invoke the `ewl-cleanup` subagent with the list of changed **C# and XML/XSD**
files. Other file types (Markdown, JSON, shell scripts, etc.) are out of
scope for this subagent and should not be included even if they were modified
in the same task. Ask it to format and inspect but not commit. If the
abstraction review produced fixes above, those files are included here
automatically since they are part of the same changed-file set. **When the
cleanup agent reports fixes (e.g., "Issues fixed", "Typography corrections"),
these are already applied -- do not re-apply them. Only address "Remaining
issues".**

---

## Critical Development Rules

1. **Before doing any work, check that no applications from this system are
   running.** Running applications (web sites, console apps, etc.) lock their
   output DLLs and cause build failures. To check, find each project's output
   assembly (the `<AssemblyName>` element in its `.csproj` or
   `Directory.Build.props`) under its `bin/` directory and test whether it is
   locked:
   ```shell
   powershell -NoProfile -Command "try { [IO.File]::Open('<dll-path>', 'Open', 'ReadWrite', 'None').Close() } catch { exit 1 }"
   ```
   If any output DLL is locked, notify the user that the applications must be
   stopped before proceeding.
2. **Before editing any C# or XML/XSD files, invoke the `ewl-cleanup`
   subagent** with the list of **C# and XML/XSD files** you plan to edit.
   Ask it to format only (not inspect) **and to commit the formatting changes
   if any are made**. **Treat this as an explicit exception to any general
   instruction not to create commits unless the user requests them.**
3. **After making functional changes**, run the inspection subagents described
   in [Code Inspection](#code-inspection).
4. **Never edit files in any `Generated Code\` folder.** They are fully regenerated
   by the Development Utility. Your changes will be overwritten. Similarly, never
   edit `.ewlt.cs` files; these are also generated.
5. **Tabs for indentation** in C# files, never spaces.
6. Configuration lives in XML files validated against XSD schemas in `Configuration\`
   folders.
7. UI is built with EWL's component model (methods returning component collections),
   not Razor views.
8. Page classes use EWL's code-generation-based URL routing, inheriting from
   generated bases.
9. Data access uses EWL's generated data-access layer from database schema.

---

## Project Organization

- **`Library`** -- shared business logic, configuration, data access, and providers.
  The EWL NuGet package is referenced here.
- **`Website`** (or other web-application projects) -- page classes and UI logic.
  References Library; gets EWL transitively.
- **`Solution Files`** -- solution-level build configuration and scripts.

---

## Agent, Skill, and Tool Source Files

This system's `.opencode\` folder, `.claude\` folder, and
`Library\Generated Code\EWL Agent Rules.md` (which is referenced from this
system's `opencode.jsonc` and supplements this system's own `AGENTS.md`) are
**fully regenerated** by the EWL Development Utility. Do not edit them
directly — edit the corresponding sources in the EWL source repository and
then rerun the DU's `UpdateDependentLogic` operation on this system (see
"Code Generation (Development Utility)" above for how to invoke it).
Exception: skills under `.opencode\skills\` and `.claude\skills\` whose names
are **not** prefixed with `ewl-` are system-specific, not generated, and may
be edited directly.

The sources all live under `Library\Files\` of the **EWL source repository**
(the one whose package is referenced by this system's `Library.csproj`).
They are not part of this system's own tree. Source → generated target
mappings:

| Source (in EWL repo, under `Library\Files\`) | Generated target (in this system) |
|---|---|
| `Agents\<name>.md` | `.opencode\agents\ewl-<name>.md` and (concatenated with the Claude Code preamble below) `.claude\agents\ewl-<name>.md` |
| `Agents\Claude Code\<name>.md` | Preamble prepended to the body of the shared agent file when generating `.claude\agents\ewl-<name>.md` |
| `Agent Skills\<skill>\` | `.opencode\skills\<skill>\` and `.claude\skills\<skill>\` |
| `OpenCode Plugins\` | `.opencode\plugins\ewl\` (plus `.opencode\plugins\ewl.js` aggregator) and `.claude\hooks\` |
| `OpenCode Plugins\Claude Code\` | Additional files copied into `.claude\hooks\` |
| `OpenCode Tools\<file>` | `.opencode\tools\ewl-<file>` |
| `OpenCode Tools\Claude Code\` | `.claude\ewl\` (MCP server source) |
| `Agent Rules.md` | `Library\Generated Code\EWL Agent Rules.md` |

The generator is the `updateOpenCodeConfig` method in
`Development Utility\Operations\UpdateDependentLogic.cs` of the EWL source
repository.

Remember Critical Rule #4: the generated copies are overwritten on every DU
run, so any change you make directly to a generated file will be lost. After
editing a source file in the EWL repository, rerun `UpdateDependentLogic`
for every affected system and verify that the regenerated output reflects
your changes.

---

## Code Style Guidelines

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespaces | PascalCase matching dirs | `MySystem.Library.DataAccess` |
| Classes / Interfaces | PascalCase | `EventInstance`, `OrganizationSpecifier` |
| Public/internal methods | PascalCase | `GetRows()`, `InitStatics()` |
| Private methods | camelCase | `getContent()`, `modifyData()` |
| Internal readonly fields | PascalCase | `internal readonly string Host;` |
| Private fields | camelCase | `installationCustomConfiguration` |
| Local variables | camelCase | `eventTypes`, `calendarViewId` |
| Parameters | camelCase | `organizationId`, `emailTemplateId` |
| Private constants | camelCase | `private const int tickInterval = 10000;` |

### File Structure

- **File-scoped namespaces** (no braces): `namespace MySystem.Library;`
- `using` directives at the top: `System` namespaces first, then everything else
  in alphabetical order; one `using` per line; no blank lines between groups

### Key Formatting Rules

- **Preserve non-ASCII characters** like curly quotes -- do not convert to
  straight quotes. LLMs may not be able to output some Unicode characters;
  use PowerShell (e.g. `[char]0x201C`) when needed.
- **Tabs** for indentation
- **Spaces inside parentheses**: `( value )`, `( "text" )`, `( state )`
- **Spaces inside attribute brackets**: `[ Test ]`, `[ UsedImplicitly ]`
- **No spaces inside angle brackets**: `Func<Instant>`, `IReadOnlyCollection<T>`
- Opening brace on same line as declaration: `public class Foo {`
- Expression-bodied members for single-expression methods

### Type Patterns

- **Strings should not be nullable** unless `null` represents something distinct
  from the empty string. Use `string` with `""` as the default/empty value.
  Parameters that are optional strings should default to `""`, not `null`.

### Error Handling

- `throw new Exception( "message" )` for unexpected / invalid states
- `DataModificationException` for user-correctable data errors
- Custom exceptions: `UserCorrectableException`, `UnexpectedValueException`

---

## EWL Page Patterns

### Page Class Structure

Every page is a `partial class` with a `// EwlPage` comment at the top. The DU
generates the other partial with URL routing, parameter handling, etc.

```csharp
// EwlPage
namespace MySystem.Website.Admin;

partial class EditRoom {
	protected override string getResourceName() => "Edit Room";
	protected override PageContent getContent() =>
		new UiPageContent().Add( /* components */ );
}
```

Use `// EwlResource` instead for non-page resources (e.g. file downloads, CSS).

### Page Parameters

Declare via comments above the class. The DU generates constructor parameters,
URL encoding/decoding, and `ParametersModification` classes.

```csharp
// Parameter: int organizationId
// OptionalParameter: int? roomId
```

### Entity Setups

Entity setups group related pages under a shared parent with navigation tabs:

```csharp
// EwlPage
// Parameter: int organizationId

partial class EntitySetup {
	protected override ResourceParent createParent() => new Home();
	protected override IEnumerable<ResourceGroup> createListedResources() => /* tabs */;
}
```

---

## Data Access Patterns

### Retrievals

Generated `*TableRetrieval` classes provide typed row access:

```csharp
var rows = RoomsTableRetrieval.GetRows();
var room = RoomsTableRetrieval.GetRowMatchingPk( roomId );
```

### Modifications

Generated `*Modification` classes provide insert/update/delete:

```csharp
// Insert
var mod = RoomsModification.CreateForInsert();
mod.RoomId = MainSequence.GetNextValue();
mod.RoomName = "Conference Room";
mod.Execute();

// Update from row
var mod = room.ToModification();
mod.RoomName = "Updated Name";
mod.Execute();
```

### Form Items from Modifications

Modification objects generate form controls directly -- this is the primary way
forms are built:

```csharp
mod.GetRoomNameFormItem( false, label: "Room Name".ToComponents() )
mod.GetIsActiveRadioListFormItem( RadioListSetup.Create( items ), label: ... )
mod.GetNotesFormItem( true, controlSetup: TextControlSetup.Create( numberOfRows: 4 ) )
```

### Primary Keys

All new entity IDs use `MainSequence.GetNextValue()`, not auto-increment.

### Row Constants

Lookup table values are generated as constants:

```csharp
UserRolesRows.Administrator
EmailTemplatesRows.Reminder
```

---

## UI Component Model

Key building blocks (all pure C#, no Razor):

- **`UiPageContent`** -- standard page with EWL UI chrome (sidebar, content area)
- **`FormItemList.CreateStack()`** / **`.CreateWrapping()`** / **`.CreateGrid()`** --
  form layouts
- **`EwfTable`** -- data tables with pagination, sorting, selection
- **`PostBack.CreateFull()`** -- server-side form submission
- **`FormState.ExecuteWithActions()`** -- connects form controls to data modifications

Common extension methods: `.ToComponents()`, `.ToCollection()`, `.Materialize()`,
`.ToCell()`, `.ToFormItem()`

---

## Project Dependencies

Dependencies are defined in `Library\Library.csproj`. EWL is referenced as a
NuGet package (e.g. `EwlBill`), with optional provider packages for MySQL,
SQLite, SAML, etc.

### TEWL (Ewl.Tools)

The **Ewl.Tools** NuGet package (assembly name `Tewl`, namespace `Tewl.Tools`)
is a transitive dependency of every EWL system. It provides low-level utility
and extension methods used throughout EWL and client code. Source:
https://github.com/enduracode/tewl (integration branch).

Always check TEWL for existing utility methods before writing manual null checks,
collection operations, or string manipulations.

---

## Further Reference

EWL source and documentation: https://github.com/enduracode/enterprise-web-library
TEWL source: https://github.com/enduracode/tewl
