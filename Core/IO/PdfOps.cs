using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Pdf.Facades;
using Humanizer;
using RedStapler.StandardLibrary;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Contains methods related to PDF documents.
	/// </summary>
	public static class PdfOps {
		/// <summary>
		/// Concatenates the specified PDF documents and writes the result to the specified output stream.
		/// </summary>
		public static void ConcatPdfs( IEnumerable<Stream> inputStreams, Stream outputStream ) {
			new PdfFileEditor().Concatenate( inputStreams.ToArray(), outputStream );
		}

		/// <summary>
		/// Concatenates the specified PDF documents and creates bookmarks to the beginning of each file, 
		/// specified by the title passed in the Tuple.
		/// </summary>
		/// <param name="outputStream">Stream in which to write</param>
		/// <param name="bookmarkNamesAndPdfStreams">Title to write in the bookmark, PDF MemoryStream</param>
		public static void CreateBookmarkedPdf( IEnumerable<Tuple<string, MemoryStream>> bookmarkNamesAndPdfStreams, Stream outputStream ) {
			var concatPdfsStream = new MemoryStream();
			using( concatPdfsStream ) {
				// Paste all of the PDFs together
				ConcatPdfs( bookmarkNamesAndPdfStreams.Select( p => p.Item2 ), concatPdfsStream );
			}

			// Add bookmarks to PDF
			var bookMarkedPdf = addBookmarksToPdf( concatPdfsStream.ToArray(), bookmarkNamesAndPdfStreams.Select( t => Tuple.Create( t.Item1, t.Item2.ToArray() ) ) );

			// Have the bookmarks displayed on PDF open
			bookMarkedPdf = setShowBookmarksPaneOnOpen( bookMarkedPdf );

			// Save the PDf to the output stream
			using( var bookMarksDisplayedPdf = new MemoryStream( bookMarkedPdf ) )
				IoMethods.CopyStream( bookMarksDisplayedPdf, outputStream );
		}

		/// <summary>
		/// Adds a bookmark to the first page of each of the given PDFs in the Tuple's byte array, using the string in that Tuple for the
		/// title of the bookmark.
		/// </summary>
		/// <param name="pdf">PDF byte array</param>
		/// <param name="titleAndPdfs">Tuples of &lt;Bookmark title, PDF byte array&gt;</param>
		/// <returns>PDF byte array</returns>
		private static byte[] addBookmarksToPdf( byte[] pdf, IEnumerable<Tuple<string, byte[]>> titleAndPdfs ) {
			using( var tmpPdf = new MemoryStream( pdf ) ) {
				var bookmarkEditor = new PdfBookmarkEditor();
				bookmarkEditor.BindPdf( tmpPdf );
				var count = 1;
				foreach( var titleAndPdf in titleAndPdfs ) {
					bookmarkEditor.CreateBookmarkOfPage( titleAndPdf.Item1, count );
					count += new PdfFileInfo( new MemoryStream( titleAndPdf.Item2 ) ).NumberOfPages;
				}

				using( var addBookmarksStream = new MemoryStream() ) {
					bookmarkEditor.Save( addBookmarksStream );
					return addBookmarksStream.ToArray();
				}
			}
		}

		/// <summary>
		/// Sets the PDF to be showing the Bookmarks pane (document outline) on document open.
		/// </summary>
		/// <param name="pdf">PDF byte array</param>
		/// <returns>PDF byte array</returns>
		private static byte[] setShowBookmarksPaneOnOpen( byte[] pdf ) {
			using( var tmpPdf = new MemoryStream( pdf ) ) {
				var pce = new PdfContentEditor();
				pce.BindPdf( tmpPdf );
				pce.ChangeViewerPreference( ViewerPreference.PageModeUseOutlines );
				using( var saveStream = new MemoryStream() ) {
					pce.Save( saveStream );
					return saveStream.ToArray();
				}
			}
		}

		internal static void Test() {
			const string outputFolderName = "PdfOpsTests";
			var outputFolder = StandardLibraryMethods.CombinePaths( TestStatics.OutputFolderPath, outputFolderName );
			IoMethods.DeleteFolder( outputFolder );
			Directory.CreateDirectory( outputFolder );

			var inputTestFiles = StandardLibraryMethods.CombinePaths( TestStatics.InputTestFilesFolderPath, "PdfOps" );
			var onePagePdfPath = StandardLibraryMethods.CombinePaths( inputTestFiles, "onepage.pdf" );
			var twoPagePdfPath = StandardLibraryMethods.CombinePaths( inputTestFiles, "twopage.pdf" );
			var threePagePdfPath = StandardLibraryMethods.CombinePaths( inputTestFiles, "threepage.pdf" );

			var explanations = new List<Tuple<String, String>>();

			//ConcatPdfs

			using( var onePage = File.OpenRead( onePagePdfPath ) ) {
				const string concatOnePdf = "ConcatOne.pdf";
				using( var concatFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, concatOnePdf ) ) )
					ConcatPdfs( onePage.ToSingleElementArray(), concatFile );
				explanations.Add( Tuple.Create( concatOnePdf, "This file should be exactly the same as {0}.".FormatWith( onePagePdfPath ) ) );

				resetFileStream( onePage );
				using( var twoPage = File.OpenRead( twoPagePdfPath ) ) {
					const string concatTwoPdfs = "ConcatTwo.pdf";
					using( var concatFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, concatTwoPdfs ) ) )
						ConcatPdfs( new[] { onePage, twoPage }, concatFile );
					explanations.Add( Tuple.Create( concatTwoPdfs, "This file should look like {0} immediately followed by {1}.".FormatWith( onePagePdfPath, twoPagePdfPath ) ) );

					resetFileStream( onePage, twoPage );
					using( var threePage = File.OpenRead( threePagePdfPath ) ) {
						const string concatThreePdfs = "ConcatThree.pdf";
						using( var concatFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, concatThreePdfs ) ) )
							ConcatPdfs( new[] { onePage, twoPage, threePage }, concatFile );
						explanations.Add( Tuple.Create( concatThreePdfs,
						                                "This file should look like {0} immediately followed by {1} immediately followed by {2}.".FormatWith( onePagePdfPath,
						                                                                                                                                      twoPagePdfPath,
						                                                                                                                                      threePagePdfPath ) ) );
					}
				}
			}

			//CreateBookmarkedPdf

			using( var onePage = new MemoryStream() ) {
				IoMethods.CopyStream( File.OpenRead( onePagePdfPath ), onePage );
				const string bookmarkOnePdf = "BookmarkOne.pdf";
				const string bookmarkTitle = "Bookmark 1";
				using( var bookmarkFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, bookmarkOnePdf ) ) )
					CreateBookmarkedPdf( Tuple.Create( bookmarkTitle, onePage ).ToSingleElementArray(), bookmarkFile );
				explanations.Add( Tuple.Create( bookmarkOnePdf, "This should be {0} labeled with one bookmark named {1}.".FormatWith( onePagePdfPath, bookmarkTitle ) ) );

				using( var twoPage = new MemoryStream() ) {
					IoMethods.CopyStream( File.OpenRead( twoPagePdfPath ), twoPage );
					const string bookmarkTwoPdf = "BookmarkTwo.pdf";
					const string firstBookmarkTitle = "First bookmark";
					const string secondBookmarkTitle = "Second bookmark";
					using( var bookmarkFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, bookmarkTwoPdf ) ) )
						CreateBookmarkedPdf( new[] { Tuple.Create( firstBookmarkTitle, onePage ), Tuple.Create( secondBookmarkTitle, twoPage ) }, bookmarkFile );
					explanations.Add( Tuple.Create( bookmarkTwoPdf,
					                                "This should be {0} labeled with bookmark named {1} followed by {2} with the title of {3}.".FormatWith( onePagePdfPath,
					                                                                                                                                        firstBookmarkTitle,
					                                                                                                                                        twoPagePdfPath,
					                                                                                                                                        secondBookmarkTitle ) ) );

					using( var threePage = new MemoryStream() ) {
						IoMethods.CopyStream( File.OpenRead( threePagePdfPath ), threePage );
						const string bookmarkThreePdf = "BookmarkThree.pdf";
						const string thirdBookmarkTItle = "Third bookmark";
						using( var bookmarkFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, bookmarkThreePdf ) ) ) {
							CreateBookmarkedPdf(
								new[] { Tuple.Create( firstBookmarkTitle, onePage ), Tuple.Create( secondBookmarkTitle, twoPage ), Tuple.Create( thirdBookmarkTItle, threePage ) },
								bookmarkFile );
						}
						explanations.Add( Tuple.Create( bookmarkThreePdf,
						                                "This should be {0} labeled with bookmark named {1} followed by {2} with the title of {3} followed by {4} with the title of {5}."
						                                	.FormatWith( onePagePdfPath,
						                                	             firstBookmarkTitle,
						                                	             twoPagePdfPath,
						                                	             secondBookmarkTitle,
						                                	             threePagePdfPath,
						                                	             thirdBookmarkTItle ) ) );
					}
				}
			}


			TestStatics.OutputReadme( outputFolder, explanations );
		}

		private static void resetFileStream( params FileStream[] fs ) {
			foreach( var f in fs )
				f.Reset();
		}
	}
}