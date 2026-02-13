# AGENTS.md - Enterprise Web Library (EWL)

## Project Overview

EWL is an opinionated .NET framework for building web-based enterprise software.
The solution is C# targeting **net9.0-windows**, using ASP.NET Core with EWL's own
component-based web framework layered on top (no Razor views). Source control is
Mercurial with a Git mirror.

### Key Projects

| Project | Purpose |
|---|---|
| `Core\` | Main EWL library (NuGet package) |
| `Library\` | System-specific library; references Core plus providers |
| `Website\` | Demo ASP.NET Core web application |
| `Tests\` | NUnit test project |
| `Development Utility\` | CLI tool for code generation and build ops |
| `Providers\` | Pluggable provider implementations |

Solution file: `Enterprise Web Library.sln`

---

## Build Commands

```shell
# Restore packages
dotnet restore "Enterprise Web Library.sln"

# Build (Debug, the default)
dotnet build "Enterprise Web Library.sln"

# Build (Release) -- note: Tests project is excluded from Release
dotnet build "Enterprise Web Library.sln" -c Release
```

### Code Generation (Development Utility)

The EWL Development Utility (DU) performs code generation, populating
`Generated Code\` folders in every project. It does **not** run automatically
during builds; it must be run explicitly. There are two ways:

1. **Run the released version** via `Solution Files\Update Dependent Logic.ps1`.
   This downloads the latest EWL package and runs the DU from it.
2. **Run the DU project directly** (`Development Utility\`) -- use this when
   making changes to code generation itself and need to test them. This only
   works if the DU project and all its dependencies compile successfully.

### Other Helper Scripts

- `Solution Files\Export EWL to Local Feed.bat` -- exports EWL as a NuGet package to a local feed

---

## Test Commands

Test framework: **NUnit 4.4.0**. Test project: `Tests\Tests.csproj`.
The Tests project does NOT build in Release configuration; always use Debug.

```shell
dotnet test "Tests\Tests.csproj"
dotnet test "Tests\Tests.csproj" --filter "FullyQualifiedName~Tests.DoubleTools.ToMoneyString.Test"
```

---

## Formatting

**ReSharper** is used to format all C# files. The `.editorconfig` files in the
repo exist to support ReSharper. Plugins in `.opencode\plugins\ewl\` enforce
UTF-8 BOM and CRLF line endings on every file creation or update.

---

## Critical Development Rules

1. **Never edit files in any `Generated Code\` folder.** They are fully regenerated
   by the Development Utility. Your changes will be overwritten.
2. **Tabs for indentation** in C# files, never spaces.
3. Configuration lives in XML files validated against XSD schemas in `Configuration\` folders.
4. UI is built with EWL's component model (methods returning component collections), not Razor.
5. Page classes use EWL's code-generation-based URL routing, inheriting from generated bases.
6. Data access uses EWL's generated data-access layer from database schema.

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

- **Tabs** for indentation
- **Spaces inside parentheses**: `( value )`, `( "text" )`, `( state )`
- **Spaces inside attribute brackets**: `[ Test ]`, `[ TestFixture ]`, `[ DllImport( "kernel32" ) ]`
- **Spaces inside angle brackets for generics are NOT used**: `Func<Instant>`, `IReadOnlyCollection<T>`
- Opening brace on same line as declaration: `public class Foo {`
- Expression-bodied members for single-expression methods:
  ```csharp
  internal static Instant GetCurrentTime() => currentTimeGetter!();
  ```
- Multi-line argument lists: closing paren/brace on same line as last arg, or
  each arg on its own line indented with a tab

### Type Patterns

- **Nullable reference types** enabled: `string?`, `Func<Instant>?`
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
  `currentTimeGetter!()`

### Comments

- XML doc comments (`///`) on public API members with `<summary>` and `<param>` tags
- Inline `//` comments to explain "why", not "what"
- Block comments (`/* */`) used sparingly, mainly in tests

---

## Project Dependencies

Dependencies are defined in `Core\Core.csproj` (for the main library) and in
each of the `Providers\` projects.

The **Ewl.Tools** NuGet package (assembly name `Tewl`) provides low-level
utilities used throughout EWL such as `IoMethods` and
`StringTools`. Its source is at https://github.com/enduracode/tewl (integration branch).
