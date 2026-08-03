using System.Security.Cryptography;
using EnterpriseWebLibrary.ExternalFunctionality;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.SystemSpecificLogic;
using JetBrains.Annotations;

namespace EnterpriseWebLibrary.DataAccess.BlobStorage;

[ PublicAPI ]
public static class BlobStorageStatics {
	private const string providerName = "BlobStorage";
	private static SystemProviderReference<SystemBlobStorageProvider>? provider;

	internal static void Init() {
		provider = SystemSpecificLogicStatics.GetLibraryProvider<SystemBlobStorageProvider>( providerName );
	}

	internal static bool BlobStorageEnabled => provider!.GetProvider( returnNullIfNotFound: true ) is not null;

	internal static SystemBlobStorageProvider SystemProvider => provider!.GetProvider()!;

	/// <summary>
	/// Retrieves the BLOB referenced by the specified ID.
	/// </summary>
	public static byte[] GetBlob( int referenceId ) => SystemProvider.GetBlob( SystemProvider.GetReferencedBlobId( referenceId ) );

	/// <summary>
	/// Inserts a new BLOB and returns the reference ID.
	/// </summary>
	public static int InsertBlob( byte[] blob ) => SystemProvider.InsertBlobReference( SystemProvider.InsertBlob( blob, SHA256.HashData( blob ) ) );

	/// <summary>
	/// Deletes the BLOB referenced by the specified ID.
	/// </summary>
	public static void DeleteBlob( int referenceId ) =>
		// The Data Cleaner is responsible for deleting the actual BLOB if no references remain.
		SystemProvider.DeleteBlobReference( referenceId );

	/// <summary>
	/// Retrieves the contents of the specified file.
	/// </summary>
	public static byte[] GetFileContents( BlobFile file ) => GetBlob( file.BlobReferenceId );

	/// <summary>
	/// Inserts a new file with the specified values and returns the ID.
	/// </summary>
	public static int InsertFile( string fileName, string contentType, byte[] contents ) =>
		SystemProvider.InsertFile( fileName, contentType, Clock.TransactionTime, InsertBlob( contents ) );

	/// <summary>
	/// Deletes the specified file.
	/// </summary>
	public static void DeleteFile( int fileId ) {
		var file = SystemProvider.GetFile( fileId );
		SystemProvider.DeleteFile( fileId );
		DeleteBlob( file.BlobReferenceId );
	}

	/// <summary>
	/// Copies the specified file and returns the ID of the copy.
	/// </summary>
	public static int CopyFile( int fileId ) {
		var file = SystemProvider.GetFile( fileId );
		return SystemProvider.InsertFile(
			file.FileName,
			file.ContentType,
			file.UploadTime,
			SystemProvider.InsertBlobReference( SystemProvider.GetReferencedBlobId( file.BlobReferenceId ) ) );
	}

	/// <summary>
	/// Returns the content type of the given file.
	/// </summary>
	// This implementation simply returns the media type provided by the client, which makes it vulnerable to spoofing. The only way around this is to determine
	// the media type by looking at the contents of the file.
	internal static string GetContentTypeForPostedFile( RsFile file ) => file.ContentType;

	/// <summary>
	/// Inserts a new file collection into the database and returns the ID.
	/// </summary>
	public static int InsertFileCollection() => SystemProvider.InsertFileCollection();

	/// <summary>
	/// Copies the specified file collection and returns the ID of the copy.
	/// </summary>
	public static int CopyFileCollection( int collectionId ) {
		var newCollectionId = InsertFileCollection();
		foreach( var collectionFile in SystemProvider.GetFilesLinkedToFileCollection( collectionId ) )
			SystemProvider.InsertFileCollectionFile( newCollectionId, CopyFile( collectionFile.FileId ) );
		return newCollectionId;
	}

	internal static IEnumerable<BlobFileCollectionFile> OrderByName( this IEnumerable<BlobFileCollectionFile> rows ) =>
		rows.OrderBy( i => SystemProvider.GetFile( i.FileId ).FileName ).ThenBy( i => i.FileId );

	internal static IEnumerable<BlobFileCollectionFile> OrderByUploadTimeDescending( this IEnumerable<BlobFileCollectionFile> rows ) =>
		rows.OrderByDescending( i => SystemProvider.GetFile( i.FileId ).UploadTime ).ThenByDescending( i => i.FileId );

	/// <summary>
	/// You should check other meta information about the file (such as the extension) before calling this expensive method.
	/// </summary>
	public static bool IsValidPdfFile( BlobFile file ) => IsValidPdfFile( GetFileContents( file ) );

	/// <summary>
	/// Returns true if the file is a valid PDF file.
	/// You should check other meta information about the file (such as the extension) before calling this expensive method.
	/// </summary>
	public static bool IsValidPdfFile( byte[] contents ) {
		using var memoryStream = new MemoryStream( contents );
		return IsValidPdfFile( memoryStream );
	}

	/// <summary>
	/// Returns true if the file is a valid PDF file. Caller is responsible for opening and cleaning up the stream.
	/// You should check other meta information about the file (such as the extension) before calling this expensive method.
	/// </summary>
	public static bool IsValidPdfFile( Stream sourceStream ) => ExternalFunctionalityStatics.ExternalPdfProvider.FileIsValidPdf( sourceStream );
}