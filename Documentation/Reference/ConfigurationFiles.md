# Configuration files

## System-level (`Library/Configuration`)

### `Development.xml`

Required. The development-time configuration used by `Update-DependentLogic`. Not deployed to servers.

### `General.xml`

Required. Configuration that is used at runtime and/or by the EWL System Manager’s **Installation Support Utility** on servers.

## Installation-level (`Library/Configuration/Installations`)

### `Custom.xsd`

Optional. The XML schema for your installation-custom configuration files, which you can use for your own per-installation settings.

### `InstallationName/Custom.xml` (replace `InstallationName` with the names of your installations, for example `Development` or `Live`)

Optional. The installation-custom configuration file, which must match your `Custom.xsd` schema above.

### `InstallationName/Standard.xml` (replace `InstallationName` with the names of your installations, for example `Development` or `Live`)

Required. The installation-standard configuration file, which contains per-installation settings that are used at runtime and/or by the EWL System Manager’s **Installation Support Utility** on servers.