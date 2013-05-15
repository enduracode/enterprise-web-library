using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Aspose.Words.Reporting;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.IO;
using RedStapler.StandardLibrary.MailMerging.MailMergeTesting;
using RedStapler.StandardLibrary.MailMerging.RowTree;
using RedStapler.StandardLibrary.WebFileSending;

namespace RedStapler.StandardLibrary.MailMerging {
	/// <summary>
	/// Contains methods that perform mail merging operations.
	/// </summary>
	public static class MergeOps {
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
		public static string CreateString( DBConnection cn, MergeRow row, bool ensureAllFieldsHaveValues, string template ) {
			var errors = new List<string>();

			foreach( var mergeValue in row.Values ) {
				try {
					var regex = new Regex( @"@@(" + mergeValue.Name + @")\b" ); // \b forces match to occur on word/non-word boundary;

					// We only want to evaluate the value if one or more fields exist in this template.
					if( !regex.IsMatch( template ) )
						continue;

					string value = null;
					if( mergeValue is MergeValue<string> )
						value = ( mergeValue as MergeValue<string> ).Evaluate( cn, ensureAllFieldsHaveValues );
					if( value == null ) {
						// When we add support for multiple rows, there should be no more than one of these errors for each field.
						throw new MailMergingException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );
					}

					// The $ replacement prevents it trying to match groups and do backreferencing when it sees a dollar sign.
					template = regex.Replace( template, value.Replace( "$", "$$" ) );
				}
				catch( MailMergingException e ) {
					errors.AddRange( e.Messages );
				}
			}

			// Since we have evaluated all merge fields, we can now assume that any remaining tags are invalid.
			// NOTE: Valid merge fields that caused a MailMergingException above are incorrectly included in this message. Fix this when we switch to stateful parsing.
			errors.AddRange( findInvalidTags( template ).Select( tag => "Merge field " + tag + " is invalid." ) );

			if( errors.Count > 0 )
				throw new MailMergingException( errors.ToArray() );
			return template;
		}

		private static IEnumerable<string> findInvalidTags( string template ) {
			var tags = new List<string>();
			foreach( Match match in Regex.Matches( template, @"@@(\w+)", RegexOptions.Multiline ) ) {
				foreach( Group group in match.Groups ) {
					foreach( Capture capture in group.Captures ) {
						// This makes sure it's not an empty capture and it's not the capture that contains the at signs.
						if( capture.Value != "" && capture.Value.IndexOf( "@@" ) == -1 )
							tags.Add( capture.Value );
					}
				}
			}
			return tags;
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in the input file to have
		/// a page break before it.
		/// </summary>
		public static FileToBeSent CreateMsWordDoc( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string inputFilePath ) {
			using( var sourceDocStream = new MemoryStream( File.ReadAllBytes( inputFilePath ) ) )
				return CreateMsWordDoc( cn, rowTree, ensureAllFieldsHaveValues, sourceDocStream );
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in the input file to have
		/// a page break before it.
		/// </summary>
		public static FileToBeSent CreateMsWordDoc( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Stream inputStream ) {
			using( var destinationStream = new MemoryStream() ) {
				CreateMsWordDoc( cn, rowTree, ensureAllFieldsHaveValues, inputStream, destinationStream );
				return new FileToBeSent( "MergedLetter" + FileExtensions.WordDoc, ContentTypes.WordDoc, destinationStream.ToArray() );
			}
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document. If you would like each row to be on a separate page, set the first paragraph in the input file to have
		/// a page break before it.
		/// </summary>
		public static void CreateMsWordDoc( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Stream inputStream, Stream destinationStream ) {
			createMsWordDocOrPdfFromMsWordDoc( cn, rowTree, ensureAllFieldsHaveValues, inputStream, destinationStream, true );
		}

		/// <summary>
		/// Merges a row tree with a Microsoft Word document and writes the result to a stream as a PDF document. If you would like each row to be on a separate
		/// page, set the first paragraph in the input file to have a page break before it.
		/// </summary>
		public static void CreatePdfFromMsWordDoc( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string inputFilePath,
		                                           Stream destinationStream ) {
			using( var sourcePdfStream = new MemoryStream( File.ReadAllBytes( inputFilePath ) ) )
				createMsWordDocOrPdfFromMsWordDoc( cn, rowTree, ensureAllFieldsHaveValues, sourcePdfStream, destinationStream, false );
		}

		private static void createMsWordDocOrPdfFromMsWordDoc( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, Stream inputStream,
		                                                       Stream destinationStream, bool saveAsMsWordDoc ) {
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
				doc.MailMerge.ExecuteWithRegions( new AsposeMergeRowEnumerator( cn, "Main", rowTree.Rows, ensureAllFieldsHaveValues ) );
			}
			catch( InvalidOperationException e ) {
				// Aspose throws InvalidOperationException when there are problems with the template, such as a badly-formed region.
				throw new MailMergingException( e.Message );
			}
			doc.Save( destinationStream, saveAsMsWordDoc ? Aspose.Words.SaveFormat.Doc : Aspose.Words.SaveFormat.Pdf );
		}

		/// <summary>
		/// Merges a row tree with a PDF document containing form fields.
		/// </summary>
		public static void CreatePdf( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, string sourcePdfFilePath, Stream destinationStream,
		                              bool useLegacyBehaviorOfIgnoringInvalidFields = false ) {
			// Use a memory stream because the stream may be read multiple times and we only want to access the file once.
			using( var sourcePdfStream = new MemoryStream( File.ReadAllBytes( sourcePdfFilePath ) ) )
				CreatePdf( cn, rowTree, ensureAllFieldsHaveValues, sourcePdfStream, destinationStream, useLegacyBehaviorOfIgnoringInvalidFields );
		}

		/// <summary>
		/// Merges a row tree with a PDF document containing form fields.
		/// </summary>
		public static void CreatePdf( DBConnection cn, MergeRowTree rowTree, bool ensureAllFieldsHaveValues, MemoryStream sourcePdfStream, Stream destinationStream,
		                              bool useLegacyBehaviorOfIgnoringInvalidFields = false ) {
			var streams = new List<Stream>();
			try {
				foreach( var row in rowTree.Rows ) {
					var stream = new MemoryStream();
					streams.Add( stream );

					using( var sourcePdfMemoryStreamCopy = new MemoryStream() ) {
						// Aspose has decided that in the new Facades PDF library, they will close your source stream for you when you call doc.Save.
						sourcePdfStream.Reset();
						IoMethods.CopyStream( sourcePdfStream, sourcePdfMemoryStreamCopy );

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
								value = mergeValueAsString.Evaluate( cn, ensureAllFieldsHaveValues );
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
		/// Creates a single sheet Excel Workbook from the top level of a row tree and writes it to a stream. There will be one column for each merge field
		/// specified in the list of field names. Each column head will be named by calling ToEnglishFromCamel on the merge field's name or using the Microsoft Word
		/// name without modification, the latter if useMsWordFieldNames is true.
		/// This overload should be used when sending the workbook to the browser.
		/// </summary>
		public static FileInfoToBeSent CreateExcelWorkbook( DBConnection cn, MergeRowTree rowTree, IEnumerable<string> fieldNames, string fileNameWithoutExtension,
		                                                    Stream destinationStream, bool useMsWordFieldNames = false ) {
			var excelFile = new ExcelFileWriter();

			if( rowTree.Rows.Any() ) {
				foreach( var fieldName in fieldNames ) {
					if( !rowTree.Rows.First().Values.Any( i => i.Name == fieldName ) ) {
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + fieldName + " is invalid." );
					}
				}

				var sheet = excelFile.DefaultWorksheet;
				sheet.AddHeaderToWorksheet(
					fieldNames.Select( fieldName => rowTree.Rows.First().Values.Single( i => i.Name == fieldName ) )
					          .Select( mergeValue => useMsWordFieldNames ? mergeValue.MsWordName : mergeValue.Name.CamelToEnglish() )
					          .ToArray() );
				sheet.FreezeHeaderRow();
				foreach( var row in rowTree.Rows ) {
					sheet.AddRowToWorksheet( fieldNames.Select( fieldName => row.Values.Single( i => i.Name == fieldName ) ).Select( mergeValue => {
						var mergeValueAsString = mergeValue as MergeValue<string>;
						string value = null;
						if( mergeValueAsString != null )
							value = mergeValueAsString.Evaluate( cn, false );
						if( value == null ) {
							// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
							throw new ApplicationException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );
						}

						return value;
					} ).ToArray() );
				}
			}

			excelFile.SaveToStream( destinationStream );
			return new FileInfoToBeSent( excelFile.GetSafeFileName( fileNameWithoutExtension ), excelFile.ContentType );
		}

		/// <summary>
		/// Creates a single sheet Excel Workbook from the top level of a row tree and writes it to a stream. There will be one column for each merge field
		/// specified in the list of field names. Each column head will be named by calling ToEnglishFromCamel on the merge field's name or using the Microsoft Word
		/// name without modification, the latter if useMsWordFieldNames is true.
		/// This overload should be used when not sending the workbook to the browser.
		/// </summary>
		public static void CreateExcelWorkbook( DBConnection cn, MergeRowTree rowTree, IEnumerable<string> fieldNames, Stream destinationStream,
		                                        bool useMsWordFieldNames = false ) {
			CreateExcelWorkbook( cn, rowTree, fieldNames, "", destinationStream, useMsWordFieldNames: useMsWordFieldNames );
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
		public static void CreateXmlDocument( DBConnection cn, MergeRowTree rowTree, MergeFieldNameTree fieldNameTree, Stream destinationStream ) {
			using( var writer = XmlWriter.Create( destinationStream ) ) {
				writer.WriteStartDocument();
				writeRowTreeXmlElement( cn, rowTree, fieldNameTree, writer );
				writer.WriteEndDocument();
			}
		}

		private static void writeRowTreeXmlElement( DBConnection cn, MergeRowTree rowTree, MergeFieldNameTree fieldNameTree, XmlWriter writer ) {
			writer.WriteStartElement( rowTree.NodeName );
			foreach( var row in rowTree.Rows ) {
				writer.WriteStartElement( rowTree.XmlRowElementName );
				foreach( var fieldName in fieldNameTree.FieldNames ) {
					var mergeValue = row.Values.SingleOrDefault( i => i.Name == fieldName );
					if( mergeValue == null ) {
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + fieldName + " is invalid." );
					}

					writer.WriteStartElement( mergeValue.Name );
					if( mergeValue is MergeValue<string> )
						writer.WriteValue( ( mergeValue as MergeValue<string> ).Evaluate( cn, false ) );
					else {
						// Use ApplicationException instead of MailMergingException because the field names can easily be validated before this method is called.
						throw new ApplicationException( "Merge field " + mergeValue.Name + " evaluates to an unsupported type." );
					}
					writer.WriteEndElement();
				}

				foreach( var childNameAndFieldNameTree in fieldNameTree.ChildNamesAndChildren ) {
					var childRowTree = row.Children.SingleOrDefault( i => i.NodeName == childNameAndFieldNameTree.Item1 );
					if( childRowTree == null ) {
						// Use ApplicationException instead of MailMergingException because the child names can easily be validated before this method is called.
						throw new ApplicationException( "Child " + childNameAndFieldNameTree.Item1 + " is invalid." );
					}
					writeRowTreeXmlElement( cn, childRowTree, childNameAndFieldNameTree.Item2, writer );
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
			return new MergeFieldNameTree( firstRow.Values.Where( mergeValueTypeIsSupportedInXml ).Select( i => i.Name ),
			                               childNamesAndChildren: firstRow.Children.Select( i => Tuple.Create( i.NodeName, GetXmlSupportedMergeFields( i ) ) ) );
		}

		private static bool mergeValueTypeIsSupportedInXml( MergeValue mv ) {
			return mv is MergeValue<string>;
		}

		internal static void Test() {
			const string outputFolderName = "MergeOps";
			var outputFolder = StandardLibraryMethods.CombinePaths( TestStatics.OutputFolderPath, outputFolderName );
			IoMethods.DeleteFolder( outputFolder );
			Directory.CreateDirectory( outputFolder );

			var inputTestFiles = StandardLibraryMethods.CombinePaths( TestStatics.InputTestFilesFolderPath, outputFolderName );
			var wordDocx = StandardLibraryMethods.CombinePaths( inputTestFiles, "word.docx" );
			var pdf = StandardLibraryMethods.CombinePaths( inputTestFiles, "pdf.pdf" );

			MergeStatics.Init();
			var singleTestRow = new PseudoTableRow( 1 ).ToSingleElementArray();
			var testRows = new[] { new PseudoTableRow( 1 ), new PseudoTableRow( 2 ), new PseudoTableRow( 3 ) };
			var singleRowTree = MergeStatics.CreatePseudoTableRowTree( singleTestRow );
			var pseudoTableRowTree = MergeStatics.CreatePseudoTableRowTree( testRows );

			var explanations = new List<Tuple<String, String>>();

			// Single row to merge against

			// Word files

			const string singleRowWordDoc = "SingleRowMsWordDoc" + FileExtensions.WordDocx;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, singleRowWordDoc ) ) ) {
				using( var word = File.OpenRead( wordDocx ) )
					CreateMsWordDoc( null, singleRowTree, false, word, outputFile );
				explanations.Add( Tuple.Create( singleRowWordDoc, "Should be {0} with only one page, and FullName merged in the upper left.".FormatWith( wordDocx ) ) );
			}

			const string singleRowWordDocAsPdf = "SingleRowMsWordDoc" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, singleRowWordDocAsPdf ) ) )
				CreatePdfFromMsWordDoc( null, singleRowTree, false, wordDocx, outputFile );
			explanations.Add( Tuple.Create( singleRowWordDocAsPdf,
			                                "Should be {0} with only one page, FullName merged in the upper left, saved as a PDF.".FormatWith( wordDocx ) ) );

			//Excel
			const string singleRowExcel = "SingleRowExcel" + FileExtensions.ExcelXlsx;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, singleRowExcel ) ) )
				CreateExcelWorkbook( null, singleRowTree, GetExcelSupportedMergeFields( singleRowTree ), outputFile );
			explanations.Add( Tuple.Create( singleRowExcel,
			                                "An Excel file with the first row frozen and bold with the merge field names. Note that only supported field types may be dispalyed. One more row with data should be present." ) );

			// Pdf
			const string singleRowPdf = "SingleRowPdf" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, singleRowPdf ) ) )
				CreatePdf( null, singleRowTree, false, pdf, outputFile );
			explanations.Add( Tuple.Create( singleRowPdf, "Should be {0} with only one page, FullName filled in and 'Test' displayed.".FormatWith( pdf ) ) );

			// Multiple rows to merge against

			// Word files
			const string multipleRowsWordDoc = "MultipleRowMsWordDoc" + FileExtensions.WordDocx;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, multipleRowsWordDoc ) ) ) {
				using( var word = File.OpenRead( wordDocx ) )
					CreateMsWordDoc( null, pseudoTableRowTree, false, word, outputFile );
				explanations.Add( Tuple.Create( multipleRowsWordDoc, "Should be {0} with three pages, and FullName merged in the upper left.".FormatWith( wordDocx ) ) );
			}

			const string multipleRowsWordDocAsPdf = "MultipleRowMsWordDoc" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, multipleRowsWordDocAsPdf ) ) )
				CreatePdfFromMsWordDoc( null, pseudoTableRowTree, false, wordDocx, outputFile );
			explanations.Add( Tuple.Create( multipleRowsWordDocAsPdf,
			                                "Should be {0} with three pages, FullName merged in the upper left, saved as a PDF.".FormatWith( wordDocx ) ) );

			// Excel
			const string multipleRowExcel = "MultipleRowExcel" + FileExtensions.ExcelXlsx;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, multipleRowExcel ) ) )
				CreateExcelWorkbook( null, pseudoTableRowTree, GetExcelSupportedMergeFields( pseudoTableRowTree ), outputFile );
			explanations.Add( Tuple.Create( multipleRowExcel,
			                                "An Excel file with the first row frozen and bold with the merge field names. Note that only supported field types may be dispalyed. Three more row with data should be present." ) );

			// Pdf
			const string multipleRowPdf = "MultipleRowPdf" + FileExtensions.Pdf;
			using( var outputFile = File.OpenWrite( StandardLibraryMethods.CombinePaths( outputFolder, multipleRowPdf ) ) )
				CreatePdf( null, pseudoTableRowTree, false, pdf, outputFile );
			explanations.Add( Tuple.Create( multipleRowPdf, "Should be {0} with three pages, FullName filled in and 'Test' displayed.".FormatWith( pdf ) ) );

			TestStatics.OutputReadme( outputFolder, explanations );
		}

		private class ImageFieldMergingCallBack: IFieldMergingCallback {
			void IFieldMergingCallback.FieldMerging( FieldMergingArgs args ) {}

			void IFieldMergingCallback.ImageFieldMerging( ImageFieldMergingArgs e ) {
				if( e.FieldValue != null )
					e.ImageStream = new MemoryStream( (byte[])e.FieldValue );
			}
		}
	}
}