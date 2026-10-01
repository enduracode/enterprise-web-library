---
name: ewl-configuration
description: Installation custom/shared configuration, Custom.xsd, GlobalStatics initialization, and Configuration/CustomSyncLogic.cs. Use when adding or consuming installation settings, centralizing configuration loading, or adding system-specific sync steps and dependency acquisition through Custom Sync Runner.
---

## Centralized installation configuration

Load installation configuration once during system initialization. The established
pattern is a private field in `Library/GlobalStatics.cs`, initialized by an
internal `GlobalStatics.Init()` called from `GlobalInitializer.InitStatics()`.
Expose named, strongly typed properties or methods for consumers rather than
the whole configuration object.

```csharp
private static InstallationCustomConfiguration installationCustomConfiguration = null!;

internal static void Init() {
	installationCustomConfiguration =
		ConfigurationStatics.LoadInstallationCustomConfiguration<InstallationCustomConfiguration>();
}

public static string ContactFormNotificationAddress =>
	installationCustomConfiguration.ContactFormNotificationAddress;
```

Wire initialization into the existing `SystemInitializer` implementation:

```csharp
void SystemInitializer.InitStatics() {
	GlobalStatics.Init();
}
```

Preserve existing initialization steps and place this call before any consumers
of the settings. Use the actual generated namespace and configuration type in
the system. Configuration must be initialized before its accessors are used;
do not hide ordering mistakes by adding independent lazy loads to consumers.

## Accessor ownership

- Domain classes consume named `GlobalStatics` accessors instead of calling
  `LoadInstallationCustomConfiguration` or loading XML themselves.
- A related settings group may be exposed through a typed property or method
  on `GlobalStatics`. A specialized settings facade can consume that accessor;
  it should not independently deserialize the same installation configuration.
- Use the narrowest visibility suitable for the consumers. Keep the backing
  configuration private. Preserve existing missing-setting errors, optional
  section semantics, and value normalization when moving accessors.
- Keep domain-specific interpretation in the appropriate domain class. For
  example, expose the configured storage path centrally, while storage code
  resolves legacy application-relative paths and constructs subdirectories.
- Do not hard-code installation settings or credentials in application code.

## Custom versus shared configuration

Use the configuration document that owns the setting in the actual system:

- Installation custom configuration uses
  `LoadInstallationCustomConfiguration<InstallationCustomConfiguration>()`.
- Installation shared configuration uses
  `LoadInstallationSharedConfiguration<InstallationSharedConfiguration>()`.

Both can follow the private-field, initialization, and named-accessor pattern.
Do not move a setting between custom and shared configuration merely to match
another system. Inspect the schema and existing installation layout first.

## Schema and generation

For new settings, update the hand-maintained schema (such as
`Library/Configuration/Installation/Custom.xsd`) and applicable installation
documents consistently. Read generated types to confirm the resulting API, but
never edit them. Run the system’s normal `dotnet ewl sync` workflow after
schema changes and verify the regenerated configuration types and callers,
subject to explicit user verification waivers. Moving accessors alone does not
require a schema change.

## Custom sync logic

Use the optional `Library/Configuration/CustomSyncLogic.cs` extension point for
system-specific development preparation, such as obtaining pinned external tools
or assets. Use the system’s actual configuration directory.

Rename the generated `CustomSyncLogic.ewlt.cs` template to `CustomSyncLogic.cs`
before editing. Preserve its global-namespace `static partial class CustomSyncLogic`
and `private static async Task run()` signature. Keep preparation idempotent.

Sync generates and executes the development-only Custom Sync Runner project.
Edit only the handwritten logic; the runner supplies its entry point and
configuration initialization. It is not automatically included in the solution
or deployed. After sync, run it manually from the installation root with:

```powershell
dotnet run --project "Custom Sync Runner/Custom Sync Runner.csproj"
```

The runner provides `ConfigurationStatics` (including `FilesFolderPath` and
`InstallationConfiguration.InstallationPath`) and stateless EWL/TEWL helpers.
It does not reference the system’s Library or initialize application providers,
`GlobalStatics`, database access, or telemetry. No global cleanup call is needed.

## Reference implementations

- **EnduraCode Scheduling Engine:** `Library/GlobalStatics.cs` loads custom
  configuration in `Init()` and exposes settings such as `OnSlowMachine` and
  `TcsConfig`; `Library/GlobalInitializer.cs` invokes it.
- **Todd (TBG Enterprise System):** the same custom-configuration pattern,
  including the `ContactFormNotificationAddress` accessor.
- **EWL System Manager:** the same initialization pattern for shared
  configuration, exposing settings such as `OrganizationShortName`.
  `Library/Configuration/CustomSyncLogic.cs` also demonstrates pinned HEY CLI asset
  acquisition through Custom Sync Runner.
- **RLE Link:** its Library initializer initializes domain statics, but no
  installation-configuration loader was found there; it is not an example of
  custom-configuration loading.

These illustrate ownership and lifecycle, not a requirement to copy unrelated
initialization, database connections, or historical nullability conventions.
