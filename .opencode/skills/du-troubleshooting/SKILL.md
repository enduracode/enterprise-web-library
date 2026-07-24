---
name: du-troubleshooting
description: Run local EWL Development Utility (DU) operations on EWL systems. Use when asked to run DU operations such as sync or update-data against a particular path or system.
---

# Running the Local DU

Use `Development Utility/Development Utility.csproj` in this repository, not a released DU.

The DU treats its current working directory as the target installation path. Set the command tool's working directory to the path specified by the user; do not pass that path as a DU argument. Resolve `Development Utility/Development Utility.csproj` against the session's workspace root and pass its absolute path to `dotnet run --project`.

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

- If the DU identifies the wrong installation, verify the process working directory is the exact installation path requested by the user.
- If the installation is invalid, verify that the target has runtime installation configuration.
