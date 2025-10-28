using NodaTime;

namespace EnterpriseWebLibrary.DataAccess.BlobStorage;

/// <summary>
/// The basic attributes of a file stored in a database, including the ID and the name.
/// </summary>
/// <param name="FileId"></param>
/// <param name="FileName"></param>
/// <param name="ContentType">The media type of the file, or the empty string if unknown.</param>
/// <param name="UploadTime"></param>
public record BlobFile( int FileId, string FileName, string ContentType, Instant UploadTime );