using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Contains methods that zip and unzip data.
	/// </summary>
	public static class ZipOps {
		internal static void Test() {
			// NOTE: This path is probably wrong, and should not be hard-coded.
			const string sourceFolderPath = @"C:\Red Stapler Vault\Supporting Files\Standard Library\Standard Library\MailMerging";

			var outputFolderPath = TestStatics.OutputFolderPath;
			IoMethods.DeleteFolder( outputFolderPath );

			// Create and extract empty zip file
			var emptyFolderPath = StandardLibraryMethods.CombinePaths( outputFolderPath, "Empty" );
			Directory.CreateDirectory( emptyFolderPath );
			var emptyZipPath = StandardLibraryMethods.CombinePaths( outputFolderPath, "empty.zip" );
			ZipFolderAsFile( emptyFolderPath, emptyZipPath );
			using( var memoryStream = new MemoryStream() ) {
				ZipFolderAsStream( emptyFolderPath, memoryStream );
				memoryStream.Reset();
				UnZipStreamIntoFolder( memoryStream, StandardLibraryMethods.CombinePaths( outputFolderPath, "Empty from stream" ) );
			}

			// File-based
			var zipFilePath = StandardLibraryMethods.CombinePaths( outputFolderPath, "file.zip" );
			var extractedPath = StandardLibraryMethods.CombinePaths( outputFolderPath, "Extracted from File" );
			ZipFolderAsFile( sourceFolderPath, zipFilePath );
			UnZipFileAsFolder( zipFilePath, extractedPath );

			// Byte-array-based
			var bytes = ZipFolderAsByteArray( sourceFolderPath );
			var byteZipFilePath = StandardLibraryMethods.CombinePaths( outputFolderPath, "fileFromBytes.zip" );
			File.WriteAllBytes( byteZipFilePath, bytes );
			UnZipByteArrayAsFolder( File.ReadAllBytes( byteZipFilePath ), StandardLibraryMethods.CombinePaths( outputFolderPath, "Extracted from Byte Array" ) );

			// Stream-based
			var streamedFilePath = StandardLibraryMethods.CombinePaths( outputFolderPath, "fileFromStream.zip" );
			using( var fs = new FileStream( streamedFilePath, FileMode.Create ) )
				ZipFolderAsStream( sourceFolderPath, fs );

			var streamedExtractedPath = StandardLibraryMethods.CombinePaths( outputFolderPath, "Extracted from Stream" );
			using( var memoryStream = new MemoryStream() ) {
				ZipFolderAsStream( sourceFolderPath, memoryStream );
				memoryStream.Reset();
				UnZipStreamIntoFolder( memoryStream, streamedExtractedPath );
			}

			using( var fs = File.OpenRead( streamedFilePath ) ) {
				var files = UnZipStreamAsFileObjects( fs );
				ZipFileObjectsAsStream( files, File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolderPath, "fileFromStreamedFileObjects.zip" ) ) );
			}
		}


		// folder <-> ZIP file

		/// <summary>
		/// Zips all files in the specified folder into a new ZIP file with the specified path. If a file already exists at the path, it is overwritten.
		/// </summary>
		public static void ZipFolderAsFile( string sourceFolderPath, string destinationFilePath ) {
			IoMethods.DeleteFile( destinationFilePath );
			Directory.CreateDirectory( Path.GetDirectoryName( destinationFilePath ) );
			new FastZip().CreateZip( destinationFilePath, sourceFolderPath, true, null );
		}

		/// <summary>
		/// Unzips the specified ZIP file into a new folder with the specified path. If a folder already exists at the path, it is deleted.
		/// </summary>
		public static void UnZipFileAsFolder( string sourceFilePath, string destinationFolderPath ) {
			IoMethods.DeleteFolder( destinationFolderPath );
			new FastZip().ExtractZip( sourceFilePath, destinationFolderPath, null );
		}


		// folder <-> ZIP byte array

		/// <summary>
		/// Zips all files and folders (recursively) in the specified folder into a ZIP byte array that represents a zip file.
		/// </summary>
		public static byte[] ZipFolderAsByteArray( string sourceFolderPath ) {
			using( var memoryStream = new MemoryStream( (int)Math.Min( IoMethods.GetFolderSize( sourceFolderPath ), int.MaxValue ) ) ) {
				ZipFolderAsStream( sourceFolderPath, memoryStream );
				return memoryStream.ToArray();
			}
		}

		/// <summary>
		/// Unzips the specified ZIP byte array into a new folder with the specified path. If a folder already exists at the path, it is deleted.
		/// </summary>
		public static void UnZipByteArrayAsFolder( byte[] sourceData, string destinationFolderPath ) {
			using( var memoryStream = new MemoryStream( sourceData ) )
				UnZipStreamAsFolder( memoryStream, destinationFolderPath );
		}


		// folder <-> ZIP stream

		/// <summary>
		/// Zips all files and folders (recursively) in the specified folder into a memory stream. The caller is responsible for disposing the stream.
		/// </summary>
		public static void ZipFolderAsStream( string sourceFolderPath, Stream outputStream ) {
			var normalizedPath = Path.GetFullPath( sourceFolderPath );
			using( var zipOutputStream = createZipOutputStream( outputStream ) ) {
				foreach( var filePath in IoMethods.GetFilePathsInFolder( sourceFolderPath, searchOption: SearchOption.AllDirectories ) ) {
					var relativePath = new string( filePath.Skip( normalizedPath.Length + 1 ).ToArray() );
					var zipEntry = new ZipEntry( ZipEntry.CleanName( relativePath ) );
					using( var fs = File.OpenRead( filePath ) )
						writeZipEntry( zipEntry, fs, zipOutputStream );
				}
			}
		}

		/// <summary>
		/// Unzips the specified ZIP stream into a new folder with the specified path. If a folder already exists at the path, it is deleted.
		/// </summary>
		public static void UnZipStreamAsFolder( Stream sourceStream, string destinationFolderPath ) {
			IoMethods.DeleteFolder( destinationFolderPath );
			UnZipStreamIntoFolder( sourceStream, destinationFolderPath );
		}


		// scattered files and folders -> ZIP stream

		/// <summary>
		/// Zips all specified files into a stream. All files are put in the root of the zip file. The caller is responsible for disposing the stream.
		/// </summary>
		public static void ZipFilesAsStream( IEnumerable<string> filePaths, Stream outputStream ) {
			using( var zipOutputStream = createZipOutputStream( outputStream ) ) {
				foreach( var filePath in filePaths ) {
					using( var fs = File.OpenRead( filePath ) )
						writeZipEntry( new ZipEntry( ZipEntry.CleanName( Path.GetFileName( filePath ) ) ), fs, zipOutputStream );
				}
			}
		}


		// part of a folder <- ZIP stream

		/// <summary>
		/// Unzips the specified ZIP stream to the new or existing folder. If the folder already exists, archive items will be added to the existing folder structure.
		/// Files will be overwritten when there is an existing file with the same name as an archive file.
		/// </summary>
		public static void UnZipStreamIntoFolder( Stream sourceStream, string destinationFolderPath ) {
			Directory.CreateDirectory( destinationFolderPath );
			using( var zipInputStream = new ZipInputStream( sourceStream ) ) {
				ZipEntry entry;
				while( ( entry = zipInputStream.GetNextEntry() ) != null ) {
					if( entry.IsDirectory )
						Directory.CreateDirectory( StandardLibraryMethods.CombinePaths( destinationFolderPath, entry.Name ) );
					else {
						using( var outputStream = IoMethods.GetFileStreamForWrite( StandardLibraryMethods.CombinePaths( destinationFolderPath, entry.Name ) ) )
							IoMethods.CopyStream( zipInputStream, outputStream );
					}
				}
			}
		}


		// file objects <-> ZIP stream

		/// <summary>
		/// Creates a zip file into the destination stream, given the sources.
		/// Tuple file names will be cleaned. File names must include extension.
		/// </summary>
		public static void ZipFileObjectsAsStream( IEnumerable<RsFile> files, Stream destination ) {
			using( var zipStream = createZipOutputStream( destination ) ) {
				foreach( var file in files ) {
					var zipEntry = new ZipEntry( ZipEntry.CleanName( file.FileName ) ) { Size = file.Contents.Length };
					using( var stream = new MemoryStream( file.Contents ) )
						writeZipEntry( zipEntry, stream, zipStream );
				}
			}
		}

		/// <summary>
		/// This method completely ignores directory structure of the zip file in the source stream. The result is a flattened list of files. But, the file names do contain
		/// the relative path information, so they can be used to re-create the directory structure.
		/// </summary>
		public static IEnumerable<RsFile> UnZipStreamAsFileObjects( Stream source ) {
			var files = new List<RsFile>();
			using( var zipInputStream = new ZipInputStream( source ) ) {
				ZipEntry entry;
				while( ( entry = zipInputStream.GetNextEntry() ) != null ) {
					if( entry.IsDirectory )
						continue;
					using( var outputStream = new MemoryStream() ) {
						IoMethods.CopyStream( zipInputStream, outputStream );
						files.Add( new RsFile( outputStream.ToArray(), entry.Name ) );
					}
				}
			}
			return files;
		}


		// file streams -> ZIP stream

		/// <summary>
		/// Creates a zip file into the destination stream, given the sources.
		/// Tuple file names will be cleaned. File names must include extension.
		/// </summary>
		public static void ZipFileStreamsAsStream( IEnumerable<Tuple<string, Stream>> fileNameAndSources, Stream destination ) {
			using( var zipStream = createZipOutputStream( destination ) ) {
				foreach( var fileNameAndSource in fileNameAndSources ) {
					// http://wiki.sharpdevelop.net/SharpZipLib-Zip-Samples.ashx
					// To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
					// you need to do one of the following: Specify UseZip64.Off, or set the Size.
					// If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
					// but the zip will be in Zip64 format which not all utilities can understand.
					var zipEntry = new ZipEntry( ZipEntry.CleanName( fileNameAndSource.Item1 ) ) { Size = fileNameAndSource.Item2.Length };

					writeZipEntry( zipEntry, fileNameAndSource.Item2, zipStream );
				}
			}
		}


		private static ZipOutputStream createZipOutputStream( Stream s ) {
			return new ZipOutputStream( s ) { IsStreamOwner = false };
		}

		private static void writeZipEntry( ZipEntry zipEntry, Stream sourceStream, ZipOutputStream zipOutputStream ) {
			zipOutputStream.PutNextEntry( zipEntry );
			IoMethods.CopyStream( sourceStream, zipOutputStream );
		}
	}
}