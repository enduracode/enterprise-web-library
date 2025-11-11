namespace EnterpriseWebLibrary.ExternalFunctionality;

/// <summary>
/// External PDF logic.
/// </summary>
public interface ExternalPdfProvider {
	/// <summary>
	/// Initializes the provider.
	/// </summary>
	void InitStatics( string asposePdfLicensePath );

	bool FileIsValidPdf( Stream stream );

	void FillFormFields( MemoryStream sourceStream, Func<string, string?> valueSelector, Stream destinationStream );

	void ConcatPdfs( IEnumerable<Stream> inputStreams, Stream outputStream );

	void CreateBookmarkedPdf( IEnumerable<Tuple<string, MemoryStream>> bookmarkNamesAndPdfStreams, Stream outputStream );
}