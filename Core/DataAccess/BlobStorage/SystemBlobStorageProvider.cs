using NodaTime;

namespace EnterpriseWebLibrary.DataAccess.BlobStorage;

/// <summary>
/// System-specific BLOB-storage logic.
/// </summary>
public interface SystemBlobStorageProvider {
	/// <summary>
	/// Retrieves all BLOB IDs and hashes.
	/// </summary>
	IEnumerable<( int blobId, byte[] hash )> GetBlobHashes();

	/// <summary>
	/// Retrieves the BLOB with the specified ID.
	/// </summary>
	byte[] GetBlob( int blobId );

	/// <summary>
	/// Inserts a new BLOB and its hash, and returns the ID.
	/// </summary>
	int InsertBlob( byte[] blob, byte[] hash );

	/// <summary>
	/// Deletes the specified BLOB.
	/// </summary>
	void DeleteBlob( int blobId );

	/// <summary>
	/// Retrieves all referenced BLOB IDs.
	/// </summary>
	IEnumerable<int> GetReferencedBlobIds();

	/// <summary>
	/// Retrieves the BLOB ID from the specified reference.
	/// </summary>
	int GetReferencedBlobId( int referenceId );

	/// <summary>
	/// Inserts a new reference to the specified BLOB and returns the ID.
	/// </summary>
	int InsertBlobReference( int blobId );

	/// <summary>
	/// Updates all references to the specified BLOB to refer to a new BLOB.
	/// </summary>
	void UpdateReferencesToBlob( int blobId, int newBlobId );

	/// <summary>
	/// Deletes the specified BLOB reference.
	/// </summary>
	void DeleteBlobReference( int referenceId );

	/// <summary>
	/// Retrieves the file with the specified ID.
	/// </summary>
	BlobFile GetFile( int fileId );

	/// <summary>
	/// Inserts a new file with the specified values and returns the ID.
	/// </summary>
	int InsertFile( string fileName, string contentType, Instant uploadTime, int blobReferenceId );

	/// <summary>
	/// Deletes the specified file.
	/// </summary>
	void DeleteFile( int fileId );

	/// <summary>
	/// Inserts a new file collection into the database and returns the ID.
	/// </summary>
	int InsertFileCollection();

	/// <summary>
	/// Retrieves the list of files linked to the specified file collection.
	/// </summary>
	IEnumerable<BlobFileCollectionFile> GetFilesLinkedToFileCollection( int fileCollectionId );

	/// <summary>
	/// Inserts a new file-collection file linking the specified file to the specified collection and returns the ID.
	/// </summary>
	int InsertFileCollectionFile( int fileCollectionId, int fileId );

	/// <summary>
	/// Updates the specified file-collection file to link to the specified file.
	/// </summary>
	void UpdateFileCollectionFile( int fileCollectionFileId, int fileId );

	/// <summary>
	/// Deletes the specified file-collection-file.
	/// </summary>
	void DeleteFileCollectionFile( int fileCollectionFileId );
}