using Aspose.Pdf.Facades;
using EnterpriseWebLibrary.ExternalFunctionality;

namespace EnterpriseWebLibrary.Pdf;

public class PdfProvider: ExternalPdfProvider {
	void ExternalPdfProvider.InitStatics( string asposePdfLicensePath ) {
		new Aspose.Pdf.License().SetLicense( asposePdfLicensePath );
	}

	bool ExternalPdfProvider.FileIsValidPdf( Stream stream ) {
		try {
			return new PdfFileInfo( stream ).IsPdfFile;
		}
		catch {
			// We catch all exceptions here because we don’t trust Aspose to consistently throw a particular type of exception when the PDF is invalid.
			return false;
		}
	}

	void ExternalPdfProvider.FillFormFields( MemoryStream sourceStream, Func<string, string?> valueSelector, Stream destinationStream ) {
		using var sourcePdfMemoryStreamCopy = new MemoryStream();

		// Aspose has decided that in the new Facades PDF library, they will close your source stream for you when you call doc.Save.
		sourceStream.Reset();
		sourceStream.CopyTo( sourcePdfMemoryStreamCopy );

		var doc = new Form( sourcePdfMemoryStreamCopy );
		foreach( var mergeField in doc.FieldNames ) {
			var value = valueSelector( mergeField );
			if( value is not null )
				doc.FillField( mergeField, value );
		}
		doc.Save( destinationStream );
	}

	void ExternalPdfProvider.ConcatPdfs( IEnumerable<Stream> inputStreams, Stream outputStream ) {
		new PdfFileEditor().Concatenate( inputStreams.ToArray(), outputStream );
	}

	void ExternalPdfProvider.CreateBookmarkedPdf( IEnumerable<Tuple<string, MemoryStream>> bookmarkNamesAndPdfStreams, Stream outputStream ) {
		var concatPdfsStream = new MemoryStream();
		using( concatPdfsStream )
			// Paste all of the PDFs together
			( (ExternalPdfProvider)this ).ConcatPdfs( bookmarkNamesAndPdfStreams.Select( p => p.Item2 ), concatPdfsStream );

		// Add bookmarks to PDF
		var bookMarkedPdf = addBookmarksToPdf( concatPdfsStream.ToArray(), bookmarkNamesAndPdfStreams.Select( t => Tuple.Create( t.Item1, t.Item2.ToArray() ) ) );

		// Have the bookmarks displayed on PDF open
		bookMarkedPdf = setShowBookmarksPaneOnOpen( bookMarkedPdf );

		// Save the PDf to the output stream
		using( var bookMarksDisplayedPdf = new MemoryStream( bookMarkedPdf ) )
			bookMarksDisplayedPdf.CopyTo( outputStream );
	}

	/// <summary>
	/// Adds a bookmark to the first page of each of the given PDFs in the Tuple’s byte array, using the string in that Tuple for the
	/// title of the bookmark.
	/// </summary>
	/// <param name="pdf">PDF byte array</param>
	/// <param name="titleAndPdfs">Tuples of &lt;Bookmark title, PDF byte array&gt;</param>
	/// <returns>PDF byte array</returns>
	private byte[] addBookmarksToPdf( byte[] pdf, IEnumerable<Tuple<string, byte[]>> titleAndPdfs ) {
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
	private byte[] setShowBookmarksPaneOnOpen( byte[] pdf ) {
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
}