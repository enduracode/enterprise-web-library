namespace EnterpriseWebLibrary.InstallationSupportUtility.DatabaseAbstraction;

public class ExportFile {
	private readonly string? filePath;

	private readonly string? azureContainerUrl;
	private readonly string? azureBlobName;

	internal ExportFile( string? filePath, string? azureContainerUrl, string? azureBlobName ) {
		this.filePath = filePath;
		this.azureContainerUrl = azureContainerUrl;
		this.azureBlobName = azureBlobName;
	}

	internal bool IsAzureBlob => azureContainerUrl is not null;

	internal bool TryGetFilePath( out string filePath ) {
		if( IsAzureBlob )
			throw new InvalidOperationException();

		filePath = this.filePath!;

		return filePath.Length > 0;
	}

	internal bool TryGetAzureBlob( out string containerUrl, out string blobName ) {
		if( !IsAzureBlob )
			throw new InvalidOperationException();

		containerUrl = azureContainerUrl!;
		blobName = azureBlobName!;

		return containerUrl.Length > 0;
	}
}