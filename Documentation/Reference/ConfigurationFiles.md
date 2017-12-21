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

* `rsisInstallationId`: Optional. The identifier for the installation in the EWL System Manager.

* `CertificateEmailAddressOverride`: Optional.

* `administrators`: Required. The administrators, represented by one or more `administrator` elements:
	* `Name`: Required.
	* `EmailAddress`: Required.

* `database`: Optional. The primary database, represented by one of the following types:

	* `SqlServerDatabase`: Microsoft SQL Server.
		* `server`: Optional.
		* `SqlServerAuthenticationLogin`: Optional.
		* `database`: Optional.
		* `FullTextCatalog`: Optional.

	* `MySqlDatabase`: MySQL.
		* `database`: Optional.

	* `OracleDatabase`: Oracle Database.
		* `tnsName`: Required.
		* `userAndSchema`: Required.
		* `password`: Required.
		* `SupportsConnectionPooling`: Optional.
		* `SupportsLinguisticIndexes`: Optional.

* `SecondaryDatabases`: Optional. The secondary databases, represented by one or more `SecondaryDatabase` elements:
	* `Name`: Required.
	* `Database`: Required. The database, represented by one of the database types above.

* `installedInstallation`: Optional. The live- or intermediate-installation settings:

	* `name`: Required.

	* `shortName`: Required.

	* `InstallationTypeConfiguration`: Required. The live-specific or intermediate-specific settings, represented by either the `LiveInstallationConfiguration` or `IntermediateInstallationConfiguration` type:

		*	`EmailFromName`: `IntermediateInstallationConfiguration` only. Required. The from name for all mail, which gets wrapped and sent to the EWL System Manager or to developers, for intermediate installations.

		*	`EmailFromAddress`: `IntermediateInstallationConfiguration` only. Required. The from address for all mail, which gets wrapped and sent to the EWL System Manager or to developers, for intermediate installations.

		* `EmailSendingService`: Required.

		* `WebApplications`: Optional. Installation-level settings for each web application specified in `General.xml`. Represented by one or more `Application` elements:

			*	`Name`: Required.

			*	`IisApplication`: Required.

			*	`DefaultBaseUrl`: Optional. Normally when building absolute URLs, the web framework will use the IIS application settings to determine the base URL. Sometimes you need to override this behavior, for example when the application is behind a load balancer. Settings:

				*	`Host`: Required.
				*	`NonsecurePort`: Optional.
				*	`SecurePort`: Optional.
				*	`Path`: Optional.

			*	`DefaultCookieAttributes`: Optional.