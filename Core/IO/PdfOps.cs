using EnterpriseWebLibrary.ExternalFunctionality;
using Tewl.IO;

namespace EnterpriseWebLibrary.IO;

/// <summary>
/// Contains methods related to PDF documents.
/// </summary>
public static class PdfOps {
	/// <summary>
	/// Concatenates the specified PDF documents and writes the result to the specified output stream.
	/// </summary>
	public static void ConcatPdfs( IEnumerable<Stream> inputStreams, Stream outputStream ) {
		ExternalFunctionalityStatics.ExternalPdfProvider.ConcatPdfs( inputStreams, outputStream );
	}

	/// <summary>
	/// Concatenates the specified PDF documents and creates bookmarks to the beginning of each file, 
	/// specified by the title passed in the Tuple.
	/// </summary>
	/// <param name="outputStream">Stream in which to write</param>
	/// <param name="bookmarkNamesAndPdfStreams">Title to write in the bookmark, PDF MemoryStream</param>
	public static void CreateBookmarkedPdf( IEnumerable<Tuple<string, MemoryStream>> bookmarkNamesAndPdfStreams, Stream outputStream ) {
		ExternalFunctionalityStatics.ExternalPdfProvider.CreateBookmarkedPdf( bookmarkNamesAndPdfStreams, outputStream );
	}

	internal static void Test() {
		const string outputFolderName = "PdfOpsTests";
		var outputFolder = EwlStatics.CombinePaths( TestStatics.OutputFolderPath, outputFolderName );
		IoMethods.DeleteFolder( outputFolder );
		Directory.CreateDirectory( outputFolder );

		var inputTestFiles = EwlStatics.CombinePaths( TestStatics.InputTestFilesFolderPath, "PdfOps" );
		var onePagePdfPath = EwlStatics.CombinePaths( inputTestFiles, "onepage.pdf" );
		var twoPagePdfPath = EwlStatics.CombinePaths( inputTestFiles, "twopage.pdf" );
		var threePagePdfPath = EwlStatics.CombinePaths( inputTestFiles, "threepage.pdf" );

		var explanations = new List<Tuple<String, String>>();

		//ConcatPdfs

		using( var onePage = File.OpenRead( onePagePdfPath ) ) {
			const string concatOnePdf = "ConcatOne.pdf";
			using( var concatFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, concatOnePdf ) ) )
				ConcatPdfs( onePage.ToCollection(), concatFile );
			explanations.Add( Tuple.Create( concatOnePdf, "This file should be exactly the same as {0}.".FormatWith( onePagePdfPath ) ) );

			resetFileStream( onePage );
			using( var twoPage = File.OpenRead( twoPagePdfPath ) ) {
				const string concatTwoPdfs = "ConcatTwo.pdf";
				using( var concatFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, concatTwoPdfs ) ) )
					ConcatPdfs( new[] { onePage, twoPage }, concatFile );
				explanations.Add(
					Tuple.Create( concatTwoPdfs, "This file should look like {0} immediately followed by {1}.".FormatWith( onePagePdfPath, twoPagePdfPath ) ) );

				resetFileStream( onePage, twoPage );
				using( var threePage = File.OpenRead( threePagePdfPath ) ) {
					const string concatThreePdfs = "ConcatThree.pdf";
					using( var concatFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, concatThreePdfs ) ) )
						ConcatPdfs( new[] { onePage, twoPage, threePage }, concatFile );
					explanations.Add(
						Tuple.Create(
							concatThreePdfs,
							"This file should look like {0} immediately followed by {1} immediately followed by {2}.".FormatWith(
								onePagePdfPath,
								twoPagePdfPath,
								threePagePdfPath ) ) );
				}
			}
		}

		//CreateBookmarkedPdf

		using( var onePage = new MemoryStream() ) {
			File.OpenRead( onePagePdfPath ).CopyTo( onePage );
			const string bookmarkOnePdf = "BookmarkOne.pdf";
			const string bookmarkTitle = "Bookmark 1";
			using( var bookmarkFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, bookmarkOnePdf ) ) )
				CreateBookmarkedPdf( Tuple.Create( bookmarkTitle, onePage ).ToCollection(), bookmarkFile );
			explanations.Add( Tuple.Create( bookmarkOnePdf, "This should be {0} labeled with one bookmark named {1}.".FormatWith( onePagePdfPath, bookmarkTitle ) ) );

			using( var twoPage = new MemoryStream() ) {
				File.OpenRead( twoPagePdfPath ).CopyTo( twoPage );
				const string bookmarkTwoPdf = "BookmarkTwo.pdf";
				const string firstBookmarkTitle = "First bookmark";
				const string secondBookmarkTitle = "Second bookmark";
				using( var bookmarkFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, bookmarkTwoPdf ) ) )
					CreateBookmarkedPdf( new[] { Tuple.Create( firstBookmarkTitle, onePage ), Tuple.Create( secondBookmarkTitle, twoPage ) }, bookmarkFile );
				explanations.Add(
					Tuple.Create(
						bookmarkTwoPdf,
						"This should be {0} labeled with bookmark named {1} followed by {2} with the title of {3}.".FormatWith(
							onePagePdfPath,
							firstBookmarkTitle,
							twoPagePdfPath,
							secondBookmarkTitle ) ) );

				using( var threePage = new MemoryStream() ) {
					File.OpenRead( threePagePdfPath ).CopyTo( threePage );
					const string bookmarkThreePdf = "BookmarkThree.pdf";
					const string thirdBookmarkTItle = "Third bookmark";
					using( var bookmarkFile = File.OpenWrite( EwlStatics.CombinePaths( outputFolder, bookmarkThreePdf ) ) )
						CreateBookmarkedPdf(
							new[]
								{
									Tuple.Create( firstBookmarkTitle, onePage ), Tuple.Create( secondBookmarkTitle, twoPage ), Tuple.Create( thirdBookmarkTItle, threePage )
								},
							bookmarkFile );
					explanations.Add(
						Tuple.Create(
							bookmarkThreePdf,
							"This should be {0} labeled with bookmark named {1} followed by {2} with the title of {3} followed by {4} with the title of {5}.".FormatWith(
								onePagePdfPath,
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