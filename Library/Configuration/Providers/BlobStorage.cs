using EnterpriseWebLibrary.DataAccess.BlobStorage;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.Configuration.Providers;

[ UsedImplicitly ]
public class BlobStorage: SystemBlobStorageProvider {
	public const int FileManagerCollectionId = 1;
	public const int FileCollectionManagerCollectionId = 2;

	BlobFile SystemBlobStorageProvider.GetFile( int fileId ) {
		throw new NotSupportedException();
	}

	List<BlobFile> SystemBlobStorageProvider.GetFilesLinkedToFileCollection( int fileCollectionId ) =>
		fileCollectionId switch
			{
				FileManagerCollectionId => [ ],
				FileCollectionManagerCollectionId => [ ],
				_ => throw new UnexpectedValueException( "file collection ID", fileCollectionId )
			};

	int SystemBlobStorageProvider.InsertFileCollection() {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.InsertFile( int fileCollectionId, string fileName, byte[] contents, string contentType ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.UpdateFile( int fileId, string fileName, byte[] contents, string contentType ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteFile( int fileId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteFilesLinkedToFileCollection( int fileCollectionId ) {
		throw new NotSupportedException();
	}

	byte[] SystemBlobStorageProvider.GetFileContents( int fileId ) {
		throw new NotSupportedException();
	}
}