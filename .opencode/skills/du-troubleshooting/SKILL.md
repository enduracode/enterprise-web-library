---
name: du-troubleshooting
description: Run local EWL Development Utility (DU) operations on EWL systems. Use when asked to run DU operations such as sync or update-data against a particular path or system.
---

# Running the Local DU

Use the local `Development Utility/Development Utility.csproj` project in this repository.

Resolve `Development Utility/Development Utility.csproj` against the session's workspace root to get its absolute path. Set the command tool's working directory to the target installation path, then build the local DU, resolve its output path through MSBuild, and execute the DLL directly:

```powershell
$duProject = '<resolved absolute DU-project path>'
dotnet build $duProject
if( $LASTEXITCODE -eq 0 ) {
	$duDll = dotnet msbuild $duProject -getProperty:TargetPath
	if( $LASTEXITCODE -eq 0 ) {
		dotnet $duDll <operation> <arguments>
	}
}
```

In this command, `$duProject` is the absolute project path resolved above, and `<operation> <arguments>` is the requested DU operation. Keep the command tool's working directory set to the target installation for the entire command because the DU uses its process working directory as the installation path. Resolving `TargetPath` avoids hard-coding a configuration, target framework, runtime identifier, or output-folder layout.

The DU startup banner's `installation path` is the local EWL source/configuration location, not the target installation. The target is the process working directory set on the command tool.

If the user names a system instead of providing a path, locate it using repository context and nearby system repositories, then identify its EWL installation directory from its configuration. Ask only if multiple plausible installations remain.

For sync, pass:

```text
sync
```

## update-data

`update-data` requires exactly two operation arguments:

```text
update-data <sourceName|Default> <forceNewPackageDownload:True|False>
```

Unless the user specifies otherwise, use the default source without forcing a new package download:

```text
update-data Default False
```

Running this operation replaces target data and can leave running applications with stale cached-table data; restart relevant applications afterward when the DU reports cached tables. If a named source is requested, use its exact configured short name in place of `Default`. If the DU says that the source does not exist, do not guess another name; inspect the configured sources or ask the user.

## Troubleshooting

- If the installation is invalid, verify that the target has runtime installation configuration.
