using System;
using System.Collections.Generic;
using System.IO;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.MailMerging;
using EnterpriseWebLibrary.MailMerging.RowTree;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP response, minus any caching information.
	/// </summary>
	public class EwfResponse {
		internal readonly string ContentType;
		internal readonly Func<string> FileNameCreator;
		internal readonly EwfResponseBodyCreator BodyCreator;

		/// <summary>
		/// Creates an Excel workbook response. Automatically converts the specified file name to a safe file name.
		/// </summary>
		public EwfResponse( Func<string> extensionlessFileNameCreator, Func<ExcelFileWriter> workbookCreator )
			: this(
				ExcelFileWriter.ContentType,
				new EwfResponseBodyCreator( stream => workbookCreator().SaveToStream( stream ) ),
				fileNameCreator: () => ExcelFileWriter.GetSafeFileName( extensionlessFileNameCreator() ) ) {}

		/// <summary>
		/// Creates a response by merging a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in
		/// the input file to have a page break before it.
		/// </summary>
		public EwfResponse( Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string inputFilePath )
			: this( extensionlessFileNameCreator,
				rowTree,
				ensureAllFieldsHaveValues,
				writer => {
					using( var sourceDocStream = new MemoryStream( File.ReadAllBytes( inputFilePath ) ) )
						writer( sourceDocStream );
				} ) {}

		/// <summary>
		/// Creates a response by merging a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in
		/// the input file to have a page break before it.
		/// </summary>
		public EwfResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Action<Action<Stream>> inputStreamProvider )
			: this(
				ContentTypes.WordDoc,
				new EwfResponseBodyCreator(
					destinationStream => inputStreamProvider( inputStream => MergeOps.CreateMsWordDoc( rowTree, ensureAllFieldsHaveValues, inputStream, destinationStream ) ) ),
				fileNameCreator: () => extensionlessFileNameCreator() + FileExtensions.WordDoc ) {}

		/// <summary>
		/// Creates a response containing a single-sheet Excel workbook created from the top level of a row tree. There will be one column for each merge field
		/// specified in the list of field names. Each column head will be named by calling ToEnglishFromCamel on the merge field's name or using the Microsoft Word
		/// name without modification, the latter if useMsWordFieldNames is true.
		/// </summary>
		public EwfResponse( Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, IEnumerable<string> fieldNames, bool useMsWordFieldNames = false )
			: this( extensionlessFileNameCreator, () => MergeOps.CreateExcelFileWriter( rowTree, fieldNames, useMsWordFieldNames ) ) {}

		/// <summary>
		/// Creates a response. 
		/// </summary>
		/// <param name="contentType">The media type of the response. We recommend that you always specify this, but pass the empty string if you don't have it. Do
		/// not pass null.</param>
		/// <param name="bodyCreator">The response body creator.</param>
		/// <param name="fileNameCreator">A function that creates the file name for saving the response. If you return a nonempty string, the response will be
		/// processed as an attachment with the specified file name. Do not return null from the function.</param>
		public EwfResponse( string contentType, EwfResponseBodyCreator bodyCreator, Func<string> fileNameCreator = null ) {
			ContentType = contentType;
			FileNameCreator = fileNameCreator ?? ( () => "" );
			BodyCreator = bodyCreator;
		}

		/// <summary>
		/// EWF use only.
		/// </summary>
		public EwfResponse( FullResponse fullResponse ) {
			ContentType = fullResponse.ContentType;
			FileNameCreator = () => fullResponse.FileName;
			BodyCreator = fullResponse.TextBody != null
				              ? new EwfResponseBodyCreator( () => fullResponse.TextBody )
				              : new EwfResponseBodyCreator( () => fullResponse.BinaryBody );
		}

		internal FullResponse CreateFullResponse() {
			return BodyCreator.BodyIsText
				       ? new FullResponse( ContentType, FileNameCreator(), BodyCreator.TextBodyCreator() )
				       : new FullResponse( ContentType, FileNameCreator(), BodyCreator.BinaryBodyCreator() );
		}
	}
}