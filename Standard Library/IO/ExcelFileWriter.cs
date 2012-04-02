using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspose.Cells;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Represents an Excel workbook (file).
	/// </summary>
	public class ExcelFileWriter {
		// NOTE: It's a shame that this can't be a TabularDataFileWriter.
		private readonly Workbook workbook;
		private readonly Dictionary<string, ExcelWorksheet> namesToWorksheets = new Dictionary<string, ExcelWorksheet>();

		/// <summary>
		/// The content type of this file (MsExcel).
		/// </summary>
		public string ContentType { get { return UseLegacyExcelFormat ? ContentTypes.ExcelXls : ContentTypes.ExcelXlsx; } }

		/// <summary>
		/// True if the file should be saved in XLS (Excel 2003) format. Otherwise, the default of XLSX (Excel 2007 ) format is used.
		/// </summary>
		public bool UseLegacyExcelFormat { get; set; }

		/// <summary>
		/// True if every worksheet should auto-fit its column widths immediately before saving. The default is true when creating a new blank ExcelFileWriter.
		/// </summary>
		public bool AutofitOnSave { get; set; }

		/// <summary>
		/// The worksheet that was created automatically with this workbook.
		/// </summary>
		public ExcelWorksheet DefaultWorksheet { get; private set; }

		/// <summary>
		/// Creates a new Excel File Writer with one worksheet (the default worksheet) in the workbook.
		/// </summary>
		public ExcelFileWriter( bool createDefaultWorksheet = true ) {
			workbook = new Workbook();
			AutofitOnSave = true;
			if( createDefaultWorksheet ) {
				var newWorkSheet = new ExcelWorksheet( workbook.Worksheets[ 0 ] );
				namesToWorksheets.Add( newWorkSheet.Name, newWorkSheet );
				DefaultWorksheet = newWorkSheet;
			}
			else
				workbook.Worksheets.Clear();
		}

		/// <summary>
		/// Opens an excel file from the given file path.
		/// </summary>
		public ExcelFileWriter( string filePath ) {
			workbook = new Workbook( filePath );
			loadWorksheets();
		}

		/// <summary>
		/// Opens an excel file.
		/// </summary>
		public ExcelFileWriter( Stream fileStream ) {
			workbook = new Workbook( fileStream );
			loadWorksheets();
		}

		/// <summary>
		/// Loads the worksheets in the opened workbook into a dictionary.
		/// </summary>
		private void loadWorksheets() {
			foreach( Worksheet worksheet in workbook.Worksheets )
				namesToWorksheets.Add( worksheet.Name, new ExcelWorksheet( worksheet ) );
			DefaultWorksheet = namesToWorksheets.First().Value;
		}

		/// <summary>
		/// Adds to the workbook and returns a new ExcelWorksheet.
		/// </summary>
		public ExcelWorksheet AddWorksheet( string name ) {
			var newWorkSheet = new ExcelWorksheet( workbook.Worksheets.Add( name ) );
			namesToWorksheets.Add( name, newWorkSheet );
			return newWorkSheet;
		}

		/// <summary>
		/// Returns the worksheet with the given name in this workbook. If not found, returns null.
		/// </summary>
		public ExcelWorksheet GetWorksheetByName( string name ) {
			ExcelWorksheet workSheet;
			namesToWorksheets.TryGetValue( name, out workSheet );
			return workSheet;
		}

		/// <summary>
		/// Gets a safe file name (using ToSafeFileName) using the appropriate extension (either .xls or .xlsx, depending on the value of UseLegacyExcelFormat).
		/// </summary>
		public string GetSafeFileName( string fileNameWithoutExtension ) {
			return ( fileNameWithoutExtension + ( UseLegacyExcelFormat ? ".xls" : ".xlsx" ) ).ToSafeFileName();
		}

		/// <summary>
		/// Returns a stream containing the file contents. This is useful to pass to an EmailAttachment.
		/// </summary>
		public MemoryStream GetStream() {
			using( var memoryStream = new MemoryStream() ) {
				SaveToStream( memoryStream );
				return memoryStream;
			}
		}

		/// <summary>
		/// Saves the workbook to the given stream.
		/// </summary>
		public void SaveToStream( Stream stream ) {
			if( AutofitOnSave )
				autoFit();
			workbook.Save( stream, UseLegacyExcelFormat ? SaveFormat.Excel97To2003 : SaveFormat.Xlsx );
		}

		private void autoFit() {
			foreach( Worksheet worksheet in workbook.Worksheets ) {
				worksheet.AutoFitColumns();
				worksheet.AutoFitRows();
			}
		}
	}
}