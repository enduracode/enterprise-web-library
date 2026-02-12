# Agent Rules - EWL Client Systems

This system uses the Enterprise Web Library (EWL), an opinionated .NET framework
for building web-based enterprise software. The solution is C# targeting
`net9.0-windows`.

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

Outside Visual Studio, locate and run the DU executable directly:

```powershell
# 1. Read the EWL package name and version from Library.csproj
[xml]$projectXml = Get-Content "Library\Library.csproj"
$ref = $projectXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -match '^Ewl[A-Za-z0-9]+$' }

# 2. Find the NuGet global packages folder
$packagesPath = ((dotnet nuget locals global-packages --list) -split ' ', 2)[1].TrimEnd()

# 3. Run the DU
$duPath = "$packagesPath\$($ref.Include)\$($ref.Version)\tools\Development Utility\EnterpriseWebLibrary.DevelopmentUtility.exe"
& $duPath (Get-Location) UpdateDependentLogic
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

- **Tabs** for indentation
- **Spaces inside parentheses**: `( value )`, `( "text" )`, `( state )`
- **Spaces inside attribute brackets**: `[ Test ]`, `[ UsedImplicitly ]`
- **No spaces inside angle brackets**: `Func<Instant>`, `IReadOnlyCollection<T>`
- Opening brace on same line as declaration: `public class Foo {`
- Expression-bodied members for single-expression methods

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
