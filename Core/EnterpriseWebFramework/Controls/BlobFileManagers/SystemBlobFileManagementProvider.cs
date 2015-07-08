using System.Collections.Generic;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines how file management operations will be carried out against a database.
	/// </summary>
	public interface SystemBlobFileManagementProvider {
		/// <summary>
		/// Retrieves the file with the specified ID.
		/// </summary>
		BlobFile GetFile( int fileId );

		/// <summary>
		/// Retrieves the list of files linked to the specified file collection.
		/// </summary>
		List<BlobFile> GetFilesLinkedToFileCollection( int fileCollectionId );

		/// <summary>
		/// Inserts a new file collection into the database and returns the ID.
		/// </summary>
		int InsertFileCollection();

		/// <summary>
		/// Inserts a new file with the specified values and returns the ID.
		/// </summary>
		int InsertFile( int fileCollectionId, string fileName, byte[] contents, string contentType );

		/// <summary>
		/// Updates the specified file with the specified values.
		/// </summary>
		void UpdateFile( int fileId, string fileName, byte[] contents, string contentType );

		/// <summary>
		/// Deletes the specified file.
		/// </summary>
		void DeleteFile( int fileId );

		/// <summary>
		/// Deletes all files linked to the specified file collection.
		/// </summary>
		void DeleteFilesLinkedToFileCollection( int fileCollectionId );

		/// <summary>
		/// Retrieves the contents of the specified file.
		/// </summary>
		byte[] GetFileContents( int fileId );
	}
}