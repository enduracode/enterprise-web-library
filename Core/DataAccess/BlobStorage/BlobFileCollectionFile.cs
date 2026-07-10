namespace EnterpriseWebLibrary.DataAccess.BlobStorage;

/// <summary>
/// A file in a file collection stored in a database.
/// </summary>
/// <param name="FileCollectionFileId">The ID of the file-collection file.</param>
/// <param name="File">The file currently referenced by the file-collection file.</param>
public record BlobFileCollectionFile( int FileCollectionFileId, BlobFile File );