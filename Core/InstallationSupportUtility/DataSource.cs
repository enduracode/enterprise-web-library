using Azure.Identity;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.Configuration.InstallationStandard;
using EnterpriseWebLibrary.InstallationSupportUtility.InstallationModel;
using EnterpriseWebLibrary.InstallationSupportUtility.SystemManagerInterface.Messages.SystemListMessage;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.InstallationSupportUtility;

[ PublicAPI ]
public class DataSource {
	private readonly RsisInstallation? systemManagerInstallation;

	private readonly string? azureTenantId;
	private readonly string? azureContainerUrl;
	private readonly string? azureBlobPrefix;

	public DataSource( RsisInstallation systemManagerInstallation ) {
		this.systemManagerInstallation = systemManagerInstallation;
	}

	public DataSource( ExistingInstallation installation, InstallationStandardConfigurationInstalledInstallation sourceAzureInstallation ) {
		azureTenantId = sourceAzureInstallation.AzureHosting.TenantId;
		azureContainerUrl = AzureStatics.GetDataPackageContainerUrl(
			AzureStatics.DiscoverGeneralStorageAccountName(
				new DefaultAzureCredential( new DefaultAzureCredentialOptions { TenantId = sourceAzureInstallation.AzureHosting.TenantId } ) ),
			installation,
			sourceAzureInstallation.InstallationTypeConfiguration is LiveInstallationConfiguration ? InstallationType.Live : InstallationType.Intermediate );
		azureBlobPrefix = AzureStatics.GetDataBlobPrefix( sourceAzureInstallation.shortName );
	}
}