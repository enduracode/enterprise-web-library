using EnterpriseWebLibrary.DataAccess.BlobStorage;
using JetBrains.Annotations;
using NodaTime;

namespace EnterpriseWebLibrary.Providers;

[ UsedImplicitly ]
public class BlobStorage: SystemBlobStorageProvider {
	public const int FileCollectionManagerCollectionId = 1;

	IEnumerable<(int blobId, byte[] hash)> SystemBlobStorageProvider.GetBlobHashes() {
		throw new NotSupportedException();
	}

	byte[] SystemBlobStorageProvider.GetBlob( int blobId ) {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.InsertBlob( byte[] blob, byte[] hash ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteBlob( int blobId ) {
		throw new NotSupportedException();
	}

	IEnumerable<int> SystemBlobStorageProvider.GetReferencedBlobIds() {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.GetReferencedBlobId( int referenceId ) {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.InsertBlobReference( int blobId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.UpdateReferencesToBlob( int blobId, int newBlobId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteBlobReference( int referenceId ) {
		throw new NotSupportedException();
	}

	BlobFile SystemBlobStorageProvider.GetFile( int fileId ) {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.InsertFile( string fileName, string contentType, Instant uploadTime, int blobReferenceId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteFile( int fileId ) {
		throw new NotSupportedException();
	}

	int SystemBlobStorageProvider.InsertFileCollection() {
		throw new NotSupportedException();
	}

	IEnumerable<BlobFileCollectionFile> SystemBlobStorageProvider.GetFilesLinkedToFileCollection( int fileCollectionId ) =>
		fileCollectionId switch
			{
				FileCollectionManagerCollectionId => [ ],
				_ => throw new UnexpectedValueException( "file collection ID", fileCollectionId )
			};

	int SystemBlobStorageProvider.InsertFileCollectionFile( int fileCollectionId, int fileId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.UpdateFileCollectionFile( int fileCollectionFileId, int fileId ) {
		throw new NotSupportedException();
	}

	void SystemBlobStorageProvider.DeleteFileCollectionFile( int fileCollectionFileId ) {
		throw new NotSupportedException();
	}
}