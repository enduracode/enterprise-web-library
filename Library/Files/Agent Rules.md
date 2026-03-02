# Agent Rules - EWL Client Systems

This system uses the Enterprise Web Library (EWL), an opinionated .NET framework
for building web-based enterprise software. The solution is C# targeting
`net9.0-windows`. If a `.hg` directory is present, this repository uses
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

## Formatting

**ReSharper** is used to format all C# files. The `.editorconfig` files in the
repo exist to support ReSharper. Plugins in `.opencode\plugins\ewl\` enforce
UTF-8 BOM and CRLF line endings on every file creation or update.

### ReSharper Format/Inspect Subagent

When editing C# files, use the `ewl-cleanup` subagent to handle ReSharper
formatting and commit the results separately from your functional changes.
Follow this workflow:

1. **Before making functional changes**, invoke the `ewl-cleanup` subagent
   with the list of C# files you plan to edit. It will format them and commit
   any formatting changes.
2. **Make your functional changes** to the files.
3. **After making functional changes**, invoke the `ewl-cleanup` subagent
   again with the same files, this time requesting both formatting and
   inspection. It will format the files, commit formatting changes, fix any
   ReSharper inspection issues it can, and report back.

---

## Critical Development Rules

1. **Never edit files in any `Generated Code\` folder.** They are fully regenerated
   by the Development Utility. Your changes will be overwritten. Similarly, never
   edit `.ewlt.cs` files; these are also generated.
2. **Tabs for indentation** in C# files, never spaces.
3. Configuration lives in XML files validated against XSD schemas in `Configuration\`
   folders.
4. UI is built with EWL's component model (methods returning component collections),
   not Razor views.
5. Page classes use EWL's code-generation-based URL routing, inheriting from
   generated bases.
6. Data access uses EWL's generated data-access layer from database schema.

---

## Project Organization

- **`Library`** -- shared business logic, configuration, data access, and providers.
  The EWL NuGet package is referenced here.
- **`Website`** (or other web-application projects) -- page classes and UI logic.
  References Library; gets EWL transitively.
- **`Solution Files`** -- solution-level build configuration and scripts.

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
