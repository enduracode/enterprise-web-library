using System.Linq;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// String constants for the content types for common file types.
	/// Includes the period prefix.
	/// </summary>
	public class FileExtensions {
		/// <summary>
		/// An APK file.
		/// </summary>
		public const string Apk = ".apk";

		/// <summary>
		/// A CSS file.
		/// </summary>
		public const string Css = ".css";

		/// <summary>
		/// A CSV file.
		/// </summary>
		public const string Csv = ".csv";

		/// <summary>
		/// An older Excel file.
		/// </summary>
		public const string ExcelXls = ".xls";

		/// <summary>
		/// A current Excel file.
		/// </summary>
		public const string ExcelXlsx = ".xlsx";

		/// <summary>
		/// A JPG file.
		/// </summary>
		public static readonly string[] Jpeg = { ".jpg", ".jpeg" };

		/// <summary>
		/// A GIF file.
		/// </summary>
		public const string Gif = ".gif";

		/// <summary>
		/// A JavaScript file.
		/// </summary>
		public const string JavaScript = ".js";

		/// <summary>
		/// A PDF file.
		/// </summary>
		public const string Pdf = ".pdf";

		/// <summary>
		/// A PNG file.
		/// </summary>
		public const string Png = ".png";

		/// <summary>
		/// A Scalable Vector Graphics image.
		/// </summary>
		public const string Svg = ".svg";

		/// <summary>
		/// A text document.
		/// </summary>
		public const string Txt = ".txt";

		/// <summary>
		/// An Excel file.
		/// </summary>
		public static readonly string[] Excel = { ExcelXls, ExcelXlsx };

		/// <summary>
		/// A pre-docx Word Document.
		/// </summary>
		public const string WordDoc = ".doc";

		/// <summary>
		/// A Word document.
		/// </summary>
		public const string WordDocx = ".docx";

		/// <summary>
		/// A Word file.
		/// </summary>
		public static readonly string[] Word = { WordDoc, WordDocx };

		/// <summary>
		/// An XML file.
		/// </summary>
		public const string Xml = ".xml";

		/// <summary>
		/// An XML Schema file.
		/// </summary>
		public const string Xsd = ".xsd";

		/// <summary>
		/// A ZIP file.
		/// </summary>
		public const string Zip = ".zip";

		/// <summary>
		/// Returns true if the file name has one of the given extensions.
		/// </summary>
		public static bool MatchesAGivenExtension( string fileName, params string[] allowableExtensions ) {
			return allowableExtensions.Any( ext => fileName.ToLower().EndsWith( ext.ToLower() ) );
		}
	}
}