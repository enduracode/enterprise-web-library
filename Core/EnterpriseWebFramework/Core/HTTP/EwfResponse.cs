using System.Threading.Tasks;
using EnterpriseWebLibrary.MailMerging;
using EnterpriseWebLibrary.MailMerging.RowTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Tewl.IO;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	/// <summary>
	/// An HTTP response, minus any caching information.
	/// </summary>
	public class EwfResponse {
		private static Func<HttpResponse> currentResponseGetter;

		private sealed class AspNetAdapter: HttpResponse {
			private readonly Stream body;
			internal string RedirectUrl = "";

			public AspNetAdapter( Stream body, IResponseCookies cookies ) {
				this.body = body;
				ContentType = ContentTypes.Html;
				Cookies = cookies;
			}

			public override HttpContext HttpContext => throw new NotImplementedException();
			public override int StatusCode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override IHeaderDictionary Headers => throw new NotImplementedException();
			public override Stream Body { get => body; set => throw new NotImplementedException(); }
			public override long? ContentLength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
			public override string ContentType { get; set; }
			public override IResponseCookies Cookies { get; }
			public override bool HasStarted => throw new NotImplementedException();
			public override void OnStarting( Func<object, Task> callback, object state ) => throw new NotImplementedException();
			public override void OnCompleted( Func<object, Task> callback, object state ) => throw new NotImplementedException();

			public override void Redirect( string location, bool permanent ) {
				if( location.Length == 0 || permanent )
					throw new NotImplementedException();
				RedirectUrl = location;
			}
		}

		internal static void Init( Func<HttpResponse> currentResponseGetter ) {
			EwfResponse.currentResponseGetter = currentResponseGetter;
		}

		/// <summary>
		/// Creates an Excel workbook response. Automatically converts the specified file name to a safe file name.
		/// </summary>
		public static EwfResponse CreateExcelWorkbookResponse( Func<string> extensionlessFileNameCreator, Func<ExcelFileWriter> workbookCreator ) {
			return Create(
				TewlContrib.ContentTypes.ExcelXlsx,
				new EwfResponseBodyCreator( stream => workbookCreator().SaveToStream( stream ) ),
				fileNameCreator: () => ExcelFileWriter.GetSafeFileName( extensionlessFileNameCreator() ) );
		}

		/// <summary>
		/// Creates a response by merging a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in
		/// the input file to have a page break before it.
		/// </summary>
		public static EwfResponse CreateMergedMsWordDocResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string inputFilePath ) {
			return CreateMergedMsWordDocResponse(
				extensionlessFileNameCreator,
				rowTree,
				ensureAllFieldsHaveValues,
				writer => {
					using( var sourceDocStream = new MemoryStream( File.ReadAllBytes( inputFilePath ) ) )
						writer( sourceDocStream );
				} );
		}

		/// <summary>
		/// Creates a response by merging a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in
		/// the input file to have a page break before it.
		/// </summary>
		public static EwfResponse CreateMergedMsWordDocResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Action<Action<Stream>> inputStreamProvider ) {
			return Create(
				TewlContrib.ContentTypes.WordDoc,
				new EwfResponseBodyCreator(
					destinationStream =>
						inputStreamProvider( inputStream => MergeOps.CreateMsWordDoc( rowTree, ensureAllFieldsHaveValues, inputStream, destinationStream ) ) ),
				fileNameCreator: () => extensionlessFileNameCreator() + FileExtensions.WordDoc );
		}

		/// <summary>
		/// Creates a response containing a comma-separated values (CSV) file created from the top level of a row tree. There will be one column for each merge
		/// field specified in the list of field names.
		/// </summary>
		public static EwfResponse CreateMergedCsvResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, IEnumerable<string> fieldNames, bool omitHeaderRow = false ) {
			return Create(
				TewlContrib.ContentTypes.Csv,
				new EwfResponseBodyCreator( writer => MergeOps.CreateTabularTextFile( rowTree, fieldNames, writer, omitHeaderRow: omitHeaderRow ) ),
				fileNameCreator: () => extensionlessFileNameCreator() + FileExtensions.Csv );
		}

		/// <summary>
		/// Creates a response containing a tab-separated values file created from the top level of a row tree. There will be one column for each merge field
		/// specified in the list of field names.
		/// </summary>
		public static EwfResponse CreateMergedTabSeparatedValuesResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, IEnumerable<string> fieldNames, bool omitHeaderRow = false ) {
			return Create(
				TewlContrib.ContentTypes.TabSeparatedValues,
				new EwfResponseBodyCreator(
					writer => MergeOps.CreateTabularTextFile( rowTree, fieldNames, writer, useTabAsSeparator: true, omitHeaderRow: omitHeaderRow ) ),
				fileNameCreator: () => extensionlessFileNameCreator() + FileExtensions.Txt );
		}

		/// <summary>
		/// Creates a response containing a single-sheet Excel workbook created from the top level of a row tree. There will be one column for each merge field
		/// specified in the list of field names. Each column head will be named by calling ToEnglishFromCamel on the merge field's name or using the Microsoft Word
		/// name without modification, the latter if useMsWordFieldNames is true.
		/// </summary>
		public static EwfResponse CreateMergedExcelWorkbookResponse(
			Func<string> extensionlessFileNameCreator, MergeRowTree rowTree, IEnumerable<string> fieldNames, bool useMsWordFieldNames = false ) {
			return CreateExcelWorkbookResponse( extensionlessFileNameCreator, () => MergeOps.CreateExcelFileWriter( rowTree, fieldNames, useMsWordFieldNames ) );
		}

		/// <summary>
		/// Creates a response.
		/// </summary>
		/// <param name="contentType">The media type of the response. We recommend that you always specify this, but pass the empty string if you don't have it. Do
		/// not pass null.</param>
		/// <param name="bodyCreator">The response body creator.</param>
		/// <param name="statusCodeGetter">A function that gets the response status code. Pass or return null for 200 (OK).</param>
		/// <param name="fileNameCreator">A function that creates the file name for saving the response. If you return a nonempty string, the response will be
		/// processed as an attachment with the specified file name. Do not return null from the function.</param>
		/// <param name="additionalHeaderFieldGetter">A function that gets additional HTTP header fields for the response.</param>
		public static EwfResponse Create(
			string contentType, EwfResponseBodyCreator bodyCreator, Func<int?> statusCodeGetter = null, Func<string> fileNameCreator = null,
			Func<IReadOnlyCollection<( string, string )>> additionalHeaderFieldGetter = null ) =>
			new EwfResponse(
				contentType,
				statusCodeGetter ?? ( () => null ),
				fileNameCreator ?? ( () => "" ),
				additionalHeaderFieldGetter ?? ( () => Enumerable.Empty<(string, string)>().Materialize() ),
				bodyCreator );

		/// <summary>
		/// Creates a response from an ASP.NET <see cref="HttpResponse"/> object.
		/// </summary>
		/// <param name="aspNetResponseWriter">A method that takes an ASP.NET <see cref="HttpResponse"/> object and writes to it.</param>
		public static EwfResponse CreateFromAspNetResponse( Action<HttpResponse> aspNetResponseWriter ) {
			AspNetAdapter aspNetResponse;
			byte[] binaryBody;
			using( var stream = new MemoryStream() ) {
				aspNetResponse = new AspNetAdapter( stream, currentResponseGetter().Cookies );
				aspNetResponseWriter( aspNetResponse );
				if( aspNetResponse.RedirectUrl.Any() )
					return Create(
						ContentTypes.PlainText,
						new EwfResponseBodyCreator( writer => writer.Write( "Temporary Redirect: {0}".FormatWith( aspNetResponse.RedirectUrl ) ) ),
						statusCodeGetter: () => 307,
						additionalHeaderFieldGetter: () => ( "Location", aspNetResponse.RedirectUrl ).ToCollection() );
				binaryBody = stream.ToArray();
			}
			return Create( aspNetResponse.ContentType, new EwfResponseBodyCreator( () => binaryBody ) );
		}

		internal readonly string ContentType;
		internal readonly Func<int?> StatusCodeGetter;
		internal readonly Func<string> FileNameCreator;
		internal readonly Func<IReadOnlyCollection<( string, string )>> AdditionalHeaderFieldGetter;
		internal readonly EwfResponseBodyCreator BodyCreator;

		private EwfResponse(
			string contentType, Func<int?> statusCodeGetter, Func<string> fileNameCreator, Func<IReadOnlyCollection<( string, string )>> additionalHeaderFieldGetter,
			EwfResponseBodyCreator bodyCreator ) {
			ContentType = contentType;
			StatusCodeGetter = statusCodeGetter;
			FileNameCreator = fileNameCreator;
			AdditionalHeaderFieldGetter = additionalHeaderFieldGetter;
			BodyCreator = bodyCreator;
		}

		internal EwfResponse( FullResponse fullResponse ) {
			ContentType = fullResponse.ContentType;
			StatusCodeGetter = () => fullResponse.StatusCode;
			FileNameCreator = () => fullResponse.FileName;
			AdditionalHeaderFieldGetter = () => fullResponse.AdditionalHeaderFields;
			BodyCreator = fullResponse.TextBody != null
				              ? new EwfResponseBodyCreator( () => fullResponse.TextBody )
				              : new EwfResponseBodyCreator( () => fullResponse.BinaryBody );
		}

		internal FullResponse CreateFullResponse() =>
			BodyCreator.BodyIsText
				? new FullResponse( StatusCodeGetter(), ContentType, FileNameCreator(), AdditionalHeaderFieldGetter(), BodyCreator.TextBodyCreator() )
				: new FullResponse( StatusCodeGetter(), ContentType, FileNameCreator(), AdditionalHeaderFieldGetter(), BodyCreator.BinaryBodyCreator() );

		internal void WriteToAspNetResponse( HttpResponse aspNetResponse, bool omitBody = false ) {
			var statusCode = StatusCodeGetter();
			if( statusCode.HasValue )
				aspNetResponse.StatusCode = statusCode.Value;

			var typedHeaders = aspNetResponse.GetTypedHeaders();
			if( ContentType.Length > 0 ) {
				var headerValue = new MediaTypeHeaderValue( ContentType );
				if( BodyCreator.BodyIsText )
					headerValue.Encoding = EwfResponseBodyCreator.TextEncoding;
				typedHeaders.ContentType = headerValue;
			}

			var fileName = FileNameCreator();
			if( fileName.Any() )
				aspNetResponse.Headers.ContentDisposition = "attachment; filename=\"" + fileName + "\"";

			foreach( var i in AdditionalHeaderFieldGetter() )
				aspNetResponse.Headers.Append( i.Item1, i.Item2 );

			if( !omitBody )
				BodyCreator.WriteToResponse( aspNetResponse );
		}
	}
}