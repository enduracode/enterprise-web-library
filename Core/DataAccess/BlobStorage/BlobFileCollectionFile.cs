namespace EnterpriseWebLibrary.DataAccess.BlobStorage;

/// <summary>
/// A file in a file collection stored in a database.
/// </summary>
/// <param name="FileCollectionFileId">The ID of the file-collection file.</param>
/// <param name="FileId">The ID of the currently-referenced file.</param>
public record BlobFileCollectionFile( int FileCollectionFileId, int FileId );