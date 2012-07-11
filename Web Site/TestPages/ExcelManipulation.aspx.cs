using RedStapler.StandardLibrary;
using RedStapler.StandardLibrary.DataAccess;
using RedStapler.StandardLibrary.EnterpriseWebFramework;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.IO;

namespace RedStapler.TestWebSite.TestPages {
	public partial class ExcelManipulation: EwfPage {
		public partial class Info {
			protected override void init( DBConnection cn ) {}
		}

		protected override void LoadData( DBConnection cn ) {
			//Checking in this code so we don't lose it. It's going to change a lot and this page will probably be removed.

			var excelFile = new ExcelFileWriter();

			employeeType.FillWithBlank( "<Choose One>" );
			employeeType.AddItem( "New Hire", "New Hire" );
			employeeType.AddItem( "Rehire", "Rehire" );
			employeeType.AddItem( "Invalid", "Invalid" );
			employeeType.AddItem( "Current Employee", "Current Employee" );


			submitButtonPlace.AddControlsReturnThis( new PostBackButton( new DataModification(),
			                                                             delegate {
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "P10", employeeType.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "AQ10", actionReason.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "I12", lastName.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "AL12", firstName.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "BM12", middleName.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "I14", namePrefix.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "AF14", nameSuffix.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "BM14", mitId.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "I16", visaStatus.Value );
																																		//excelFile.DefaultWorksheet.PutCellValue( "AS16", visaStart.Value.ToMonthDayYearString( "" ) );
																																		//excelFile.DefaultWorksheet.PutCellValue( "BO16", visaEnd.Value.ToMonthDayYearString( "" ) );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "I20", usCitizen.Checked ? "X" : "" );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "O20", usCitizen.Checked ? "" : "X" );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "V20", otherCitizenship.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "BG20", ssn.Value );
																																		//excelFile.DefaultWorksheet.PutCellValue( "O22", birthday.Value.ToMonthDayYearString( "" ) );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "AI22", sex.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "BL22", homePhone.Value );
			                                                             	excelFile.DefaultWorksheet.PutCellValue( "O24", homeAddress.Value );

			                                                             	excelFile.DefaultWorksheet.AddRowToWorksheet( " this is a few words",
			                                                             	                                              StringTools.ConcatenateWithDelimiter(
			                                                             	                                              	System.Environment.NewLine,
			                                                             	                                              	"this",
			                                                             	                                              	"is",
			                                                             	                                              	"multiple",
			                                                             	                                              	"lines" ),
			                                                             	                                              "and this is not" );
			                                                             	excelFile.SendExcelFile( "test output" );
			                                                             },
			                                                             new TextActionControlStyle( "Postback" ) ) );
		}
	}
}