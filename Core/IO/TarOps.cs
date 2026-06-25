using System.Formats.Tar;
using JetBrains.Annotations;
using Tewl.IO;

namespace EnterpriseWebLibrary.IO;

/// <summary>
/// Contains methods that archive and extract directory trees as store-only (uncompressed) tar archives.
/// </summary>
[ PublicAPI ]
public static class TarOps {
	// folder <-> tar stream

	/// <summary>
	/// Archives all files and folders (recursively) in the specified folder into the specified stream. The caller is responsible for disposing the stream.
	/// </summary>
	public static void ArchiveFolderToStream( string sourceFolderPath, Stream destinationStream ) {
		TarFile.CreateFromDirectory( sourceFolderPath, destinationStream, false );
	}

	/// <summary>
	/// Extracts the specified stream into a new folder with the specified path. If a folder already exists at the path, it is deleted.
	/// </summary>
	public static void ExtractStreamToFolder( Stream sourceStream, string destinationFolderPath ) {
		IoMethods.DeleteFolder( destinationFolderPath );
		Directory.CreateDirectory( destinationFolderPath );
		TarFile.ExtractToDirectory( sourceStream, destinationFolderPath, false );
	}


	// folder <-> tar file

	/// <summary>
	/// Archives all files and folders (recursively) in the specified folder into a new file with the specified path. If a file already exists at the path, it is
	/// overwritten.
	/// </summary>
	public static void ArchiveFolderToFile( string sourceFolderPath, string destinationFilePath ) {
		IoMethods.DeleteFile( destinationFilePath );
		Directory.CreateDirectory( Path.GetDirectoryName( destinationFilePath )! );
		TarFile.CreateFromDirectory( sourceFolderPath, destinationFilePath, false );
	}

	/// <summary>
	/// Extracts the specified file into a new folder with the specified path. If a folder already exists at the path, it is deleted.
	/// </summary>
	public static void ExtractFileToFolder( string sourceFilePath, string destinationFolderPath ) {
		IoMethods.DeleteFolder( destinationFolderPath );
		Directory.CreateDirectory( destinationFolderPath );
		TarFile.ExtractToDirectory( sourceFilePath, destinationFolderPath, false );
	}
}