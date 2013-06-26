using System;
using System.IO;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;

namespace RedStapler.StandardLibrary.WebFileSending {
	/// <summary>
	/// An object that creates a file to be sent. It uses either a file ID or a method that creates a file after possibly modifying data.
	/// </summary>
	public class FileCreator {
		/// <summary>
		/// Method.
		/// </summary>
		protected Func<FileToBeSent> method;

		/// <summary>
		/// Constructor.
		/// </summary>
		protected FileCreator() {}

		/// <summary>
		/// Creates a file creator with a file ID.
		/// </summary>
		public FileCreator( int fileId ): this( () => fileId ) {}

		// NOTE: This method is impossible to discover because we also use non-static constructors.
		/// <summary>
		/// NOTE:
		/// Having a file collection overload is a consequence of stupidly deciding to handle individual files as file collections
		/// with one element. Having this as a static constructor is a consequence of not being able to distinguish this from the constructor
		/// that takes a file ID.
		/// </summary>
		public static FileCreator CreateFromFileCollection( int fileCollectionId ) {
			return new FileCreator( () => BlobFileOps.GetFirstFileFromCollection( fileCollectionId ).FileId );
		}

		/// <summary>
		/// Creates a file creator with a method that returns a file ID.
		/// </summary>
		public FileCreator( Func<int> fileIdReturner ) {
			method = () => {
				var fileId = fileIdReturner();
				var file = BlobFileOps.SystemProvider.GetFile( fileId );
				var contents = BlobFileOps.SystemProvider.GetFileContents( fileId );
				return new FileToBeSent( file.FileName, file.ContentType, contents );
			};
		}

		/// <summary>
		/// Creates a file creator with a method that returns a file.
		/// </summary>
		public FileCreator( Func<FileToBeSent> fileCreationMethod ) {
			method = fileCreationMethod;
		}

		/// <summary>
		/// Creates a file creator with a method that writes a file to a stream and returns the name and content type of the file.
		/// </summary>
		public FileCreator( Func<Stream, FileInfoToBeSent> streamFileCreationMethod ) {
			method = () => {
				using( var stream = new MemoryStream() ) {
					var fileInfo = streamFileCreationMethod( stream );
					return new FileToBeSent( fileInfo.FileName, fileInfo.ContentType, stream.ToArray() );
				}
			};
		}

		internal FileToBeSent CreateFile() {
			return method();
		}
	}
}