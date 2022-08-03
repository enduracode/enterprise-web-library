using System.Text.RegularExpressions;
using System.Xml;
using Aspose.Words.MailMerging;
using EnterpriseWebLibrary.IO;
using EnterpriseWebLibrary.MailMerging.RowTree;
using Tewl.IO;
using Tewl.Tools;

namespace EnterpriseWebLibrary.MailMerging {
	/// <summary>
	/// Contains methods that perform mail merging operations.
	/// </summary>
	public static class MergeOps {
		/// <summary>
		/// HtmlBlockStatics and private use only.
		/// </summary>
		internal const string ApplicationRelativeNonSecureUrlPrefix = "@@nonSecure~";

		/// <summary>
		/// HtmlBlockStatics and private use only.
		/// </summary>
		internal const string ApplicationRelativeSecureUrlPrefix = "@@secure~";

		private class ImageFieldMergingCallBack: IFieldMergingCallback {
			void IFieldMergingCallback.FieldMerging( FieldMergingArgs args ) {}

			void IFieldMergingCallback.ImageFieldMerging( ImageFieldMergingArgs e ) {
				if( e.FieldValue != null )
					e.ImageStream = new MemoryStream( (byte[])e.FieldValue );
			}
		}

		/// <summary>
		/// Merges a row with a template string.
		/// </summary>
		//
		// NOTE: Make this take a row tree to support multiple rows and child rows.
		//
		// We'll need to add syntax similar to this:
		// 
		// @@ForEachInAppointments
		// @@AppointmentField1
		// @@AppointmentField2
		// @@EndLoop
		//
		// All white space should be respected, except whitespace inside the loop, which should be treated as a separator between child rows.
		// Implementing this syntax probably can't be done with regular expressions and will require real-deal stateful parsing.
		//
		public static string CreateString( MergeRow row, bool ensureAllFieldsHaveValues, string template ) {
			var errors = new List<string>();

			foreach( var mergeValue in row.Values )
				try {
					var regex = new Regex( @"@@(" + mergeValue.Name + @")\b" ); // \b forces match to occur on word/non-word boundary;

					// We only want to evaluate the value if one or more fields exist in this template.
					if( !regex.IsMatch( template ) )
						continue;

					string value = null;
					if( mergeValue is MergeValue<string> )
						value = ( mergeValue as MergeValue<string> ).Evaluate( ensureAllFieldsHaveValues );
					if( value == null )
						// When we add support for multiple rows, there should be no more than one of these errors for each field.
						throw new MailMergingException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );

					// The $ replacement prevents it trying to match groups and do backreferencing when it sees a dollar sign.
					template = regex.Replace( template, value.Replace( "$", "$$" ) );
				}
				catch( MailMergingException e ) {
					errors.AddRange( e.Messages );
				}

			// Since we have evaluated all recognized merge fields, we can now assume that any remaining fields are invalid.
			// NOTE: Valid merge fields that caused a MailMergingException above are incorrectly included in this message. Fix this when we switch to stateful parsing.
			errors.AddRange( getFieldsInTemplateString( template ).Select( i => "Merge field " + i + " is invalid." ) );

			if( errors.Count > 0 )
				throw new MailMergingException( errors.ToArray() );
			return template;
		}

		/// <summary>
		/// Gets a merge field name tree of the fields that exist in the specified template string.
		/// </summary>
		public static MergeFieldNameTree GetMergeFieldsInTemplateString( string template ) {
			return new MergeFieldNameTree( getFieldsInTemplateString( template ) );
		}

		private static IEnumerable<string> getFieldsInTemplateString( string template ) =>
			from Match match in Regex.Matches(
				template.Replace( ApplicationRelativeNonSecureUrlPrefix, "" ).Replace( ApplicationRelativeSecureUrlPrefix, "" ),
				@"@@(\w+)",
				RegexOptions.Multiline )
			select match.Groups[ 1 ].Value;

		/// <summary>
		/// Returns the specified template string with the merge fields converted to Silverpop personalized tags.
		/// </summary>
		public static string GetSilverpopTemplateString( string template ) {
			return Regex.Replace( template, @"@@(\w+)", "%%$1%%", RegexOptions.Multiline );
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in the input file to have
		/// a page break before it.
		/// </summary>
		public static void CreateMsWordDoc( MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Stream inputStream, Stream destinationStream ) {
			createMsWordDocOrPdfFromMsWordDoc( rowTree, ensureAllFieldsHaveValues, inputStream, destinationStream, true );
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document and writes the result to a stream as a PDF document. If you would like each row to be on a separate
		/// page, set the first paragraph in the input file to have a page break before it.
		/// </summary>
		public static void CreatePdfFromMsWordDoc( MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string inputFilePath, Stream destinationStream ) {
			using( var sourcePdfStream = new MemoryStream( File.ReadAllBytes( inputFilePath ) ) )
				createMsWordDocOrPdfFromMsWordDoc( rowTree, ensureAllFieldsHaveValues, sourcePdfStream, destinationStream, false );
		}

		private static void createMsWordDocOrPdfFromMsWordDoc(
			MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Stream inputStream, Stream destinationStream, bool saveAsMsWordDoc ) {
			var doc = new Aspose.Words.Document( inputStream );

			// This is a hack we need to do because Aspose changed MailMerge.Execute to only support a single level of data. Since we support multiple levels, i.e.
			// child data, we need to use MailMerge.ExecuteWithRegions, which associates the specified enumerator with the top level "table" in the document instead
			// of the document itself. See http://www.aspose.com/community/forums/thread/315734.aspx.
			var builder = new Aspose.Words.DocumentBuilder( doc );
			builder.MoveToDocumentStart();
			builder.InsertField( "MERGEFIELD TableStart:Main" );
			builder.MoveToDocumentEnd();
			builder.InsertField( "MERGEFIELD TableEnd:Main" );

			doc.MailMerge.CleanupOptions = MailMergeCleanupOptions.RemoveUnusedRegions;
			doc.MailMerge.FieldMergingCallback = new ImageFieldMergingCallBack();
			try {
				doc.MailMerge.ExecuteWithRegions( new AsposeMergeRowEnumerator( "Main", rowTree.Rows, ensureAllFieldsHaveValues ) );
			}
			catch( InvalidOperationException e ) {
				// Aspose throws InvalidOperationException when there are problems with the template, such as a badly-formed region.
				throw new MailMergingException( e.Message );
			}
			doc.Save( destinationStream, saveAsMsWordDoc ? Aspose.Words.SaveFormat.Docx : Aspose.Words.SaveFormat.Pdf );
		}

		/// <summary>
		/// Merges a row tree with a PDF document containing form fields.
		/// </summary>
		public static void CreatePdf(
			MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string sourcePdfFilePath, Stream destinationStream,
			bool useLegacyBehaviorOfIgnoringInvalidFields = false ) {
			// Use a memory stream because the stream may be read multiple times and we only want to access the file once.
			using( var sourcePdfStream = new MemoryStream( File.ReadAllBytes( sourcePdfFilePath ) ) )
				CreatePdf( rowTree, ensureAllFieldsHaveValues, sourcePdfStream, destinationStream, useLegacyBehaviorOfIgnoringInvalidFields );
		}

		/// <summary>
		/// Merges a row tree with a PDF document containing form fields.
		/// </summary>
		public static void CreatePdf(
			MergeRowTree rowTree, bool ensureAllFieldsHaveValues, MemoryStream sourcePdfStream, Stream destinationStream,
			bool useLegacyBehaviorOfIgnoringInvalidFields = false ) {
			var streams = new List<Stream>();
			try {
				foreach( var row in rowTree.Rows ) {
					var stream = new MemoryStream();
					streams.Add( stream );

					using( var sourcePdfMemoryStreamCopy = new MemoryStream() ) {
						// Aspose has decided that in the new Facades PDF library, they will close your source stream for you when you call doc.Save.
						sourcePdfStream.Reset();
						sourcePdfStream.CopyTo( sourcePdfMemoryStreamCopy );

						var doc = new Aspose.Pdf.Facades.Form( sourcePdfMemoryStreamCopy );
						foreach( var mergeField in doc.FieldNames.Where( mergeField => !mergeField.StartsWith( "noMerge" ) ) ) {
							var mergeValue = row.Values.SingleOrDefault( v => v.Name == mergeField );
							if( mergeValue == null ) {
								if( useLegacyBehaviorOfIgnoringInvalidFields )
									continue;
								throw new MailMergingException( string.Format( "PDF document contains a merge field ({0}) that does not exist.", mergeField ) );
							}

							var mergeValueAsString = mergeValue as MergeValue<string>;
							string value = null;
							if( mergeValueAsString != null )
								value = mergeValueAsString.Evaluate( ensureAllFieldsHaveValues );
							if( value == null )
								throw new MailMergingException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );

							doc.FillField( mergeValue.Name, value );
						}
						doc.Save( stream );
					}
				}

				if( streams.Any() )
					PdfOps.ConcatPdfs( streams, destinationStream );
			}
			finally {
				foreach( var i in streams )
					i.Dispose();
			}
		}

		/// <summary>
		/// Creates a comma-separated values (CSV) or tab-separated values file from the top level of a row tree. There will be one column for each merge field
		/// specified in the list of field names.
		/// </summary>
		public static void CreateTabularTextFile(
			MergeRowTree rowTree, IEnumerable<string> fieldNames, TextWriter destinationWriter, bool useTabAsSeparator = false, bool omitHeaderRow = false ) {
			if( !rowTree.Rows.Any() )
				return;

			foreach( var fieldName in fieldNames )
				if( rowTree.Rows.First().Values.All( i => i.Name != fieldName ) )
					// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
					throw new ApplicationException( "Merge field " + fieldName + " is invalid." );

			var writer = useTabAsSeparator ? (TextBasedTabularDataFileWriter)new TabDelimitedFileWriter() : new CsvFileWriter();

			if( !omitHeaderRow ) {
				writer.AddValuesToLine(
					fieldNames.Select( fieldName => rowTree.Rows.First().Values.Single( i => i.Name == fieldName ) )
						.Select( mergeValue => (object)mergeValue.Name )
						.ToArray() );
				writer.WriteCurrentLineToFile( destinationWriter );
			}

			foreach( var row in rowTree.Rows ) {
				writer.AddValuesToLine(
					fieldNames.Select( fieldName => row.Values.Single( i => i.Name == fieldName ) )
						.Select(
							mergeValue => {
								var mergeValueAsString = mergeValue as MergeValue<string>;
								if( mergeValueAsString != null )
									return mergeValueAsString.Evaluate( false );

								// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
								throw new ApplicationException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );
							} )
						.Cast<object>()
						.ToArray() );

				writer.WriteCurrentLineToFile( destinationWriter );
			}
		}

		/// <summary>
		/// Gets the names of the merge fields from the top level of the specified row tree that are supported by the CreateTabularTextFile method.
		/// </summary>
		public static IEnumerable<string> GetTabularTextSupportedMergeFields( MergeRowTree rowTree ) {
			return rowTree.Rows.First().Values.Where( mergeValueTypeIsSupportedInTabularText ).Select( v => v.Name );
		}

		private static bool mergeValueTypeIsSupportedInTabularText( MergeValue mv ) {
			return mv is MergeValue<string>;
		}

		/// <summary>
		/// Creates a single-sheet Excel Workbook from the top level of a row tree and writes it to a stream. There will be one column for each merge field
		/// specified in the list of field names. Each column head will be named by calling ToEnglishFromCamel on the merge field's name or using the Microsoft Word
		/// name without modification, the latter if useMsWordFieldNames is true.
		/// </summary>
		public static void CreateExcelWorkbook( MergeRowTree rowTree, IEnumerable<string> fieldNames, Stream destinationStream, bool useMsWordFieldNames = false ) {
			var excelFile = CreateExcelFileWriter( rowTree, fieldNames, useMsWordFieldNames );
			excelFile.SaveToStream( destinationStream );
		}

		internal static ExcelFileWriter CreateExcelFileWriter( MergeRowTree rowTree, IEnumerable<string> fieldNames, bool useMsWordFieldNames ) {
			var excelFile = new ExcelFileWriter();
			if( rowTree.Rows.Any() ) {
				foreach( var fieldName in fieldNames )
					if( rowTree.Rows.First().Values.All( i => i.Name != fieldName ) )
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + fieldName + " is invalid." );

				var sheet = excelFile.DefaultWorksheet;
				sheet.AddHeaderToWorksheet(
					fieldNames.Select( fieldName => rowTree.Rows.First().Values.Single( i => i.Name == fieldName ) )
						.Select( mergeValue => useMsWordFieldNames ? mergeValue.MsWordName : mergeValue.Name.CamelToEnglish() )
						.ToArray() );
				sheet.FreezeHeaderRow();
				foreach( var row in rowTree.Rows )
					sheet.AddRowToWorksheet(
						fieldNames.Select( fieldName => row.Values.Single( i => i.Name == fieldName ) )
							.Select(
								mergeValue => {
									var mergeValueAsString = mergeValue as MergeValue<string>;
									string value = null;
									if( mergeValueAsString != null )
										value = mergeValueAsString.Evaluate( false );
									if( value == null )
										// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
										throw new ApplicationException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );

									return value;
								} )
							.ToArray() );
			}
			return excelFile;
		}

		/// <summary>
		/// Gets an IEnumerable of the merge field names from the top level of the specified row tree that are supported by the CreateExcelWorkbook method.
		/// </summary>
		public static IEnumerable<string> GetExcelSupportedMergeFields( MergeRowTree rowTree ) {
			return rowTree.Rows.First().Values.Where( mergeValueTypeIsSupportedByExcel ).Select( v => v.Name );
		}

		private static bool mergeValueTypeIsSupportedByExcel( MergeValue mv ) {
			return mv is MergeValue<string>;
		}

		/// <summary>
		/// Creates an XML document from a row tree and writes it to a stream using UTF-8 encoding.
		/// </summary>
		// If we need to start generating XML on the fly for HTTP responses, it may make sense to create an overload of this method that takes an HttpResponse
		// object instead of a stream. This new overload would use the HttpResponse.Output property to obtain a TextWriter and then create the XmlWriter on top of
		// that instead of a stream. The advantage of this approach is that the encoding of the XML would then be determined by ASP.NET, which takes into account
		// any request headers the client may have sent that pertain to the desired encoding of the response.
		public static void CreateXmlDocument( MergeRowTree rowTree, MergeFieldNameTree fieldNameTree, Stream destinationStream ) {
			using( var writer = XmlWriter.Create( destinationStream ) ) {
				writer.WriteStartDocument();
				writeRowTreeXmlElement( rowTree, fieldNameTree, writer );
				writer.WriteEndDocument();
			}
		}

		private static void writeRowTreeXmlElement( MergeRowTree rowTree, MergeFieldNameTree fieldNameTree, XmlWriter writer ) {
			writer.WriteStartElement( rowTree.NodeName );
			foreach( var row in rowTree.Rows ) {
				writer.WriteStartElement( rowTree.XmlRowElementName );
				foreach( var fieldName in fieldNameTree.FieldNames ) {
					var mergeValue = row.Values.SingleOrDefault( i => i.Name == fieldName );
					if( mergeValue == null )
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + fieldName + " is invalid." );

					writer.WriteStartElement( mergeValue.Name );
					if( mergeValue is MergeValue<string> )
						writer.WriteValue( ( mergeValue as MergeValue<string> ).Evaluate( false ) );
					else
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );
					writer.WriteEndElement();
				}

				foreach( var childNameAndFieldNameTree in fieldNameTree.ChildNamesAndChildren ) {
					var childRowTree = row.Children.SingleOrDefault( i => i.NodeName == childNameAndFieldNameTree.Item1 );
					if( childRowTree == null )
						// Use ApplicationException instead of MailMergingException because the child names can easily be validated before this method is called.
						throw new ApplicationException( "Child " + childNameAndFieldNameTree.Item1 + " is invalid." );
					writeRowTreeXmlElement( childRowTree, childNameAndFieldNameTree.Item2, writer );
				}

				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Gets a merge field name tree of the fields that are supported by the CreateXmlDocument method.
		/// </summary>
		public static MergeFieldNameTree GetXmlSupportedMergeFields( MergeRowTree rowTree ) {
			var firstRow = rowTree.Rows.First();
			return new MergeFieldNameTree(
				firstRow.Values.Where( mergeValueTypeIsSupportedInXml ).Select( i => i.Name ),
				childNamesAndChildren: firstRow.Children.Select( i => Tuple.Create( i.NodeName, GetXmlSupportedMergeFields( i ) ) ) );
		}

		private static bool mergeValueTypeIsSupportedInXml( MergeValue mv ) {
			return mv is MergeValue<string>;
		}
	}
}