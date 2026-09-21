---
name: ewl-configuration
description: Installation custom/shared configuration, Custom.xsd, strongly typed configuration accessors, and GlobalStatics initialization. Use when adding or consuming installation settings or moving configuration loading out of domain classes.
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

## Reference implementations

- **EnduraCode Scheduling Engine:** `Library/GlobalStatics.cs` loads custom
  configuration in `Init()` and exposes settings such as `OnSlowMachine` and
  `TcsConfig`; `Library/GlobalInitializer.cs` invokes it.
- **Todd (TBG Enterprise System):** the same custom-configuration pattern,
  including the `ContactFormNotificationAddress` accessor.
- **EWL System Manager:** the same initialization pattern for shared
  configuration, exposing settings such as `OrganizationShortName`.
- **RLE Link:** its Library initializer initializes domain statics, but no
  installation-configuration loader was found there; it is not an example of
  custom-configuration loading.

These illustrate ownership and lifecycle, not a requirement to copy unrelated
initialization, database connections, or historical nullability conventions.
