using System;
using System.IO;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// An object that creates a file to be sent. It uses either a file ID or a method that creates a file after possibly modifying data.
	/// </summary>
	public class FileCreator {
		/// <summary>
		/// Method.
		/// </summary>
		protected Func<DBConnection, FileToBeSent> method;

		/// <summary>
		/// Constructor.
		/// </summary>
		protected FileCreator() {}

		/// <summary>
		/// Creates a file creator with a file ID.
		/// </summary>
		public FileCreator( int fileId ): this( cn => fileId ) {}

		// NOTE: This method is impossible to discover because we also use non-static constructors.
		/// <summary>
		/// NOTE:
		/// Having a file collection overload is a consequence of stupidly deciding to handle individual files as file collections
		/// with one element. Having this as a static constructor is a consequence of not being able to distinguish this from the constructor
		/// that takes a file ID.
		/// </summary>
		public static FileCreator CreateFromFileCollection( int fileCollectionId ) {
			return new FileCreator( cn => BlobFileOps.GetFirstFileFromCollection( cn, fileCollectionId ).FileId );
		}

		/// <summary>
		/// Creates a file creator with a method that returns a file ID.
		/// </summary>
		public FileCreator( Func<DBConnection, int> fileIdReturner ) {
			method = delegate( DBConnection cn ) {
				var fileId = fileIdReturner( cn );
				var file = BlobFileOps.SystemProvider.GetFile( cn, fileId );
				var contents = BlobFileOps.SystemProvider.GetFileContents( cn, fileId );
				return new FileToBeSent( file.FileName, file.ContentType, contents );
			};
		}

		/// <summary>
		/// Creates a file creator with a method that returns a file.
		/// </summary>
		public FileCreator( Func<DBConnection, FileToBeSent> fileCreationMethod ) {
			method = fileCreationMethod;
		}

		/// <summary>
		/// Creates a file creator with a method that writes a file to a stream and returns the name and content type of the file.
		/// </summary>
		public FileCreator( Func<DBConnection, Stream, FileInfoToBeSent> streamFileCreationMethod ) {
			method = delegate( DBConnection cn ) {
				using( var stream = new MemoryStream() ) {
					var fileInfo = streamFileCreationMethod( cn, stream );
					return new FileToBeSent( fileInfo.FileName, fileInfo.ContentType, stream.ToArray() );
				}
			};
		}

		internal FileToBeSent CreateFile() {
			return method( AppRequestState.PrimaryDatabaseConnection );
		}
	}
}