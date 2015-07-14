using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// A collection of IO-related static methods.
	/// </summary>
	public static class IoMethods {
		/// <summary>
		/// Creates the destination path if it does not exist, and downloads the file to that destination path.  Credentials can be null.
		/// </summary>
		public static void DownloadFile( string url, string destinationPath, NetworkCredential credentials ) {
			Directory.CreateDirectory( Path.GetDirectoryName( destinationPath ) );
			var webClient = new WebClient();
			if( credentials != null )
				webClient.Credentials = credentials;
			webClient.DownloadFile( url, destinationPath );
		}

		/// <summary>
		/// Recursively copies the contents of the specified source directory to the specified destination directory. Creates the destination directory if it
		/// doesn't already exist.  Does not overwrite anything in the destination folder if it already exists.
		/// </summary>
		public static void CopyFolder( string src, string dest, bool overwriteExistingFiles ) {
			var di = new DirectoryInfo( src );
			Directory.CreateDirectory( dest );

			foreach( var fsi in di.GetFileSystemInfos() ) {
				var destName = Path.Combine( dest, fsi.Name );
				if( fsi.GetType() == typeof( FileInfo ) )
					File.Copy( fsi.FullName, destName, overwriteExistingFiles );
				else if( fsi.GetType() == typeof( DirectoryInfo ) )
					CopyFolder( fsi.FullName, destName, overwriteExistingFiles );
			}
		}

		/// <summary>
		/// Deletes the specified directory and its contents, if the directory exists. Supports deletion of partially or fully read-only directories.
		/// </summary>
		public static void DeleteFolder( string path ) {
			var numberOfFailures = 0;
			while( Directory.Exists( path ) ) {
				try {
					RecursivelyRemoveReadOnlyAttributeFromItem( path );
					Directory.Delete( path, true );
				}
				catch( IOException e ) {
					handleFailedDeletion( path, ref numberOfFailures, e );
				}
				catch( UnauthorizedAccessException e ) {
					handleFailedDeletion( path, ref numberOfFailures, e );
				}
			}
		}

		/// <summary>
		/// Deletes the file at the given path, or does nothing if it does not exist. Supports deletion of partially or fully read-only files.
		/// </summary>
		public static void DeleteFile( string path ) {
			var numberOfFailures = 0;
			while( File.Exists( path ) ) {
				try {
					RecursivelyRemoveReadOnlyAttributeFromItem( path );
					File.Delete( path );
				}
				catch( IOException e ) {
					handleFailedDeletion( path, ref numberOfFailures, e );
				}
				catch( UnauthorizedAccessException e ) {
					handleFailedDeletion( path, ref numberOfFailures, e );
				}
			}
		}

		private static void handleFailedDeletion( string path, ref int numberOfFailures, Exception exception ) {
			if( ++numberOfFailures >= 100 )
				throw new IOException( "Failed to delete " + path + " 100 times in a row. The inner exception is the most recent failure.", exception );
			Thread.Sleep( 100 );
		}

		/// <summary>
		/// Overwrites the destination path.
		/// </summary>
		public static void MoveFile( string sourcePath, string destinationPath ) {
			CopyFile( sourcePath, destinationPath );
			DeleteFile( sourcePath );
		}

		/// <summary>
		/// Creates the destination folder if it does not exist. Overwrites the destination file if it already exists.
		/// </summary>
		public static void CopyFile( string sourcePath, string destinationPath ) {
			DeleteFile( destinationPath );
			Directory.CreateDirectory( Path.GetDirectoryName( destinationPath ) );
			File.Copy( sourcePath, destinationPath );
		}

		/// <summary>
		/// Recursively removes the read-only attribute from the specified file or folder.
		/// </summary>
		public static void RecursivelyRemoveReadOnlyAttributeFromItem( string path ) {
			var attributes = File.GetAttributes( path );
			if( ( attributes & FileAttributes.ReadOnly ) == FileAttributes.ReadOnly )
				File.SetAttributes( path, attributes & ~FileAttributes.ReadOnly );
			if( Directory.Exists( path ) ) {
				foreach( var childPath in Directory.GetFileSystemEntries( path ) )
					RecursivelyRemoveReadOnlyAttributeFromItem( childPath );
			}
		}

		/// <summary>
		/// Gets a list of file names (not including path) in the specified folder, ordered by modified date descending, that match the specified search pattern.
		/// Does not include files in subfolders. If the folder does not exist, returns an empty collection.
		/// </summary>
		public static IEnumerable<string> GetFileNamesInFolder( string folderPath, string searchPattern = "*" ) {
			return GetFilePathsInFolder( folderPath, searchPattern ).Select( Path.GetFileName );
		}

		/// <summary>
		/// Gets a list of file paths in the specified folder, ordered by last modified date descending, that match the specified search pattern. The default search
		/// option is to not include files in subfolders. If the folder does not exist, returns an empty collection.
		/// </summary>
		public static IEnumerable<string> GetFilePathsInFolder(
			string folderPath, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly ) {
			if( !Directory.Exists( folderPath ) )
				return new List<string>();
			return new DirectoryInfo( folderPath ).GetFiles( searchPattern, searchOption ).OrderByDescending( f => f.LastWriteTime ).Select( f => f.FullName );
		}

		/// <summary>
		/// Gets a list of folder names in the specified folder.
		/// </summary>
		public static List<string> GetFolderNamesInFolder( string folderPath ) {
			return Directory.GetDirectories( folderPath ).Select( Path.GetFileName ).ToList();
		}

		/// <summary>
		/// Copies one stream into another using the specified buffer size. Default buffer size is 64K.
		/// </summary>
		public static void CopyStream( Stream sourceStream, Stream destinationStream, int bufferSize = 65536 ) {
			var buffer = new byte[ bufferSize ];

			int bytesRead;
			while( ( bytesRead = sourceStream.Read( buffer, 0, buffer.Length ) ) > 0 )
				destinationStream.Write( buffer, 0, bytesRead );
		}

		/// <summary>
		/// Gets the sum size, in bytes, of everything in the folder at the given path (recursive).
		/// </summary>
		public static long GetFolderSize( string path ) {
			return File.Exists( path ) ? new FileInfo( path ).Length : Directory.GetFileSystemEntries( path ).Sum( filePath => GetFolderSize( filePath ) );
		}

		/// <summary>
		/// Returns a text writer for writing a new file or overwriting an existing file.
		/// Automatically creates any folders needed in the given path, if necessary.
		/// We recommend passing an absolute path. If a relative path is passed, the working folder
		/// is used as the root path.
		/// Caller is responsible for properly disposing the stream.
		/// </summary>
		public static TextWriter GetTextWriterForWrite( string filePath ) {
			return new StreamWriter( GetFileStreamForWrite( filePath ) );
		}

		/// <summary>
		/// Returns a file stream for writing a new file or overwriting an existing file.
		/// Automatically creates any folders needed in the given path, if necessary.
		/// We recommend passing an absolute path. If a relative path is passed, the working folder
		/// is used as the root path.
		/// Caller is responsible for properly disposing the stream.
		/// </summary>
		public static FileStream GetFileStreamForWrite( string filePath ) {
			Directory.CreateDirectory( Path.GetDirectoryName( filePath ) );
			return File.Create( filePath );
		}

		/// <summary>
		/// Executes the specified method with a stream for a temporary file. The file will be deleted after the method executes.
		/// </summary>
		public static void ExecuteWithTempFileStream( Action<FileStream> method ) {
			const int bufferSize = 4096; // This was the FileStream default as of 13 October 2014.

			// Instead of deleting the file in a finally block, we pass FileOptions.DeleteOnClose because it will ensure that the file gets deleted even if our
			// process terminates unexpectedly.
			using( var stream = new FileStream( Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize, FileOptions.DeleteOnClose ) )
				method( stream );
		}

		public static void ExecuteWithTempFolder( Action<string> method ) {
			// There is a race condition here: another process could create a directory after we check if our folder path exists, but before we create the folder. See
			// http://stackoverflow.com/a/217198/35349. We believe this is unlikely and is an acceptable risk.
			string folderPath;
			do {
				folderPath = EwlStatics.CombinePaths( Path.GetTempPath(), Path.GetRandomFileName() );
			}
			while( File.Exists( folderPath ) || Directory.Exists( folderPath ) );
			Directory.CreateDirectory( folderPath );

			try {
				method( folderPath );
			}
			finally {
				DeleteFolder( folderPath );
			}
		}
	}
}