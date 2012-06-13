using System;
using System.Linq;
using Aspose.Cells;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.IO {
	/// <summary>
	/// Respresents a worksheet inside an Excel workbook (file).
	/// </summary>
	public class ExcelWorksheet {
		private readonly Worksheet worksheet;
		private int rowIndex;

		/// <summary>
		/// The name of the worksheet that appears in the tab at the bottom of the workbook.
		/// </summary>
		public string Name { get { return worksheet.Name; } set { worksheet.Name = value; } }

		/// <summary>
		/// Creates a new worksheet.
		/// </summary>
		internal ExcelWorksheet( Worksheet worksheet ) {
			this.worksheet = worksheet;
		}

		/// <summary>
		/// Freezes the first row of this worksheet so when you scroll vertically, it does not scroll.
		/// </summary>
		public void FreezeHeaderRow() {
			worksheet.FreezePanes( 1, 0, 1, 0 );
		}

		/// <summary>
		/// Assigns the given cell to the given value. Cell names correspond to their name in Excel, e.g. D10. 
		/// Using the most specific overload for the datatype passed will be recognized by Excel.
		/// </summary>
		public void PutCellValue( string cellName, string cellValue ) {
			worksheet.Cells[ cellName ].PutValue( cellValue );
		}

		/// <summary>
		/// Assigns the given cell to the given value. Cell names correspond to their name in Excel, e.g. D10
		/// Using the most specific overload for the datatype passed will be recognized by Excel.
		/// </summary>
		public void PutCellValue( string cellName, double cellValue ) {
			worksheet.Cells[ cellName ].PutValue( cellValue );
		}

		/// <summary>
		/// Assigns the given cell to the given value. Cell names correspond to their name in Excel, e.g. D10
		/// Using the most specific overload for the datatype passed will be recognized by Excel.
		/// </summary>
		public void PutCellValue( string cellName, DateTime cellValue ) {
			worksheet.Cells[ cellName ].PutValue( cellValue );
		}

		/// <summary>
		/// Assigns a formula to a cell. Cell names correspond to their name in Excel, e.g. D10.
		/// The formula should include the equals sign prefix.
		/// </summary>
		public void PutFormula( string cellName, string formula ) {
			var cell = worksheet.Cells[ cellName ];
			cell.Formula = formula;
			setOrAddCellStyle( cell, false, bold: true );
		}

		/// <summary>
		/// Adds a header (bold appearance) row with the given column values to the worksheet.
		/// </summary>
		public void AddHeaderToWorksheet( params string[] headerValues ) {
			addRowToWorksheet( true, headerValues );
		}

		/// <summary>
		/// Adds a data row with the given column values to the worksheet.
		/// If a date is detected, it will be inserted in mm/dd/yyyy format.
		/// </summary>
		public void AddRowToWorksheet( params string[] cellValues ) {
			addRowToWorksheet( false, cellValues );
		}

		private void addRowToWorksheet( bool bold, params string[] cellValues ) {
			var columnIndex = 0;
			foreach( var cellValue in cellValues ) {
				var cell = worksheet.Cells[ rowIndex, columnIndex++ ];

				setOrAddCellStyle( cell, false, bold: bold, textWrapped: true );

				var v = new Validator();
				var detectedDate = v.GetNullableDateTime( new ValidationErrorHandler( "" ),
				                                          cellValue,
				                                          DateTimeTools.DayMonthYearFormats.Concat( DateTimeTools.MonthDayYearFormats ).ToArray(),
				                                          false,
				                                          DateTime.MinValue,
				                                          DateTime.MaxValue );
				if( !v.ErrorsOccurred ) {
					setOrAddCellStyle( cell, false, date: true );
					cell.PutValue( detectedDate );
					continue;
				}

				v = new Validator();
				// NOTE: Task 5940 detecting email has to come before detecting URL because email addresses get detected as valid URLs
				v.GetEmailAddress( new ValidationErrorHandler( "" ), cellValue, false );
				if( !v.ErrorsOccurred ) {
					worksheet.Hyperlinks.Add( cell.Name, 1, 1, "mailto:" + cellValue );
					cell.PutValue( cellValue, false );
					continue;
				}

				v = new Validator();
				v.GetUrl( new ValidationErrorHandler( "" ), cellValue, false );
				if( !v.ErrorsOccurred ) {
					worksheet.Hyperlinks.Add( cell.Name, 1, 1, cellValue );
					continue;
				}

				cell.PutValue( cellValue, true );
			}
			++rowIndex;
		}

		/// <summary>
		/// Adds simplicity to adding cell styles
		/// </summary>
		private static void setOrAddCellStyle( Cell cell, bool replaceCurrentStyles, bool bold = false, bool textWrapped = false, bool date = false ) {
			var style = replaceCurrentStyles ? new Style() : cell.GetStyle();
			if( bold )
				style.Font.IsBold = true;
			if( textWrapped )
				style.IsTextWrapped = true;
			if( date )
				style.Custom = "mm/dd/yyyy";
			cell.SetStyle( style );
		}
	}
}