using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Pdf.Facades;
using EnterpriseWebLibrary.Configuration;
using EnterpriseWebLibrary.IO;

namespace EnterpriseWebLibrary.DataAccess.BlobStorage {
	public static class BlobStorageStatics {
		private const string providerName = "BlobStorage";
		private static SystemBlobStorageProvider provider;

		internal static void Init() {
			provider = ConfigurationStatics.GetSystemLibraryProvider( providerName ) as SystemBlobStorageProvider;
		}

		internal static SystemBlobStorageProvider SystemProvider {
			get {
				if( provider == null )
					throw ConfigurationStatics.CreateProviderNotFoundException( providerName );
				return provider;
			}
		}

		/// <summary>
		/// Returns the first file in the specified file collection, or null if the collection is empty.
		/// </summary>
		public static BlobFile GetFirstFileFromCollection( int fileCollectionId ) {
			return SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId ).FirstOrDefault();
		}

		/// <summary>
		/// Copies the specified file collection and returns the ID of the copy.
		/// </summary>
		public static int CopyFileCollection( int fileCollectionId ) {
			var newFileCollectionId = SystemProvider.InsertFileCollection();
			foreach( var file in SystemProvider.GetFilesLinkedToFileCollection( fileCollectionId ) )
				SystemProvider.InsertFile( newFileCollectionId, file.FileName, SystemProvider.GetFileContents( file.FileId ), file.ContentType );
			return newFileCollectionId;
		}

		/// <summary>
		/// SystemBlobFileManagementProvider must be implemented.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( int fileId ) {
			var contents = SystemProvider.GetFileContents( fileId );
			return IsValidPdfFile( contents );
		}

		/// <summary>
		/// Returns true if the file is a valid PDF file.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( byte[] contents ) {
			using( var memoryStream = new MemoryStream( contents ) )
				return IsValidPdfFile( memoryStream );
		}

		/// <summary>
		/// Returns true if the file is a valid PDF file. Caller is responsible for opening and cleaning up the stream.
		/// You should check other meta information about the file (such as the extension) before calling this expensive method.
		/// </summary>
		public static bool IsValidPdfFile( Stream sourceStream ) {
			try {
				return new PdfFileInfo( sourceStream ).IsPdfFile;
			}
			catch {
				// We catch all exceptions here because we don't trust Aspose to consistently throw a particular type of exception when the PDF is invalid.
				return false;
			}
		}

		/// <summary>
		/// Returns the content type of the given HttpPostedFile.
		/// </summary>
		// This implementation simply returns the media type provided by the client, which makes it vulnerable to spoofing. The only way around this is to determine
		// the media type by looking at the contents of the file.
		internal static string GetContentTypeForPostedFile( RsFile file ) {
			return file.ContentType;
		}

		internal static IEnumerable<BlobFile> OrderByName( this IEnumerable<BlobFile> rows ) {
			return rows.OrderBy( i => i.FileName ).ThenBy( i => i.FileId );
		}

		internal static IEnumerable<BlobFile> OrderByUploadedDateDescending( this IEnumerable<BlobFile> rows ) {
			return rows.OrderByDescending( i => i.UploadedDate ).ThenByDescending( i => i.FileId );
		}
	}
}