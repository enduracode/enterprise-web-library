using System.Collections.Generic;
using RedStapler.StandardLibrary.DataAccess;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework.Controls {
	/// <summary>
	/// Defines how file management operations will be carried out against a database.
	/// </summary>
	public interface SystemBlobFileManagementProvider {
		/// <summary>
		/// Retrieves the file with the specified ID.
		/// </summary>
		BlobFile GetFile( DBConnection cn, int fileId );

		/// <summary>
		/// Retrieves the list of files linked to the specified file collection.
		/// </summary>
		List<BlobFile> GetFilesLinkedToFileCollection( DBConnection cn, int fileCollectionId );

		/// <summary>
		/// Inserts a new file collection into the database and returns the ID.
		/// </summary>
		int InsertFileCollection( DBConnection cn );

		/// <summary>
		/// Inserts a new file with the specified values and returns the ID.
		/// </summary>
		int InsertFile( DBConnection cn, int fileCollectionId, string fileName, byte[] contents, string contentType );

		/// <summary>
		/// Updates the specified file with the specified values.
		/// </summary>
		void UpdateFile( DBConnection cn, int fileId, string fileName, byte[] contents, string contentType );

		/// <summary>
		/// Deletes the specified file.
		/// </summary>
		void DeleteFile( DBConnection cn, int fileId );

		/// <summary>
		/// Deletes all files linked to the specified file collection.
		/// </summary>
		void DeleteFilesLinkedToFileCollection( DBConnection cn, int fileCollectionId );

		/// <summary>
		/// Retrieves the contents of the specified file.
		/// </summary>
		byte[] GetFileContents( DBConnection cn, int fileId );
	}
}