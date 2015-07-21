using System.Net.Mime;

namespace EnterpriseWebLibrary {
	/// <summary>
	/// String constants for the content types for common file types.
	/// </summary>
	public static class ContentTypes {
		/// <summary>
		/// Content type to be used for Android application packages.
		/// </summary>
		public const string ApplicationPackageFile = "application/vnd.android.package-archive";

		/// <summary>
		/// Content type to be used by .zip files.
		/// </summary>
		public const string ApplicationZip = MediaTypeNames.Application.Zip;

		/// <summary>
		/// Content type to be used by .htm and .html files.
		/// </summary>
		public const string Html = MediaTypeNames.Text.Html;

		/// <summary>
		/// Content type to be used for JavaScript files.
		/// </summary>
		public const string JavaScript = "application/javascript";

		/// <summary>
		/// Content type to be used for JSON files.
		/// </summary>
		public const string Json = "application/json";

		/// <summary>
		/// CSS.
		/// </summary>
		public const string Css = "text/css";

		/// <summary>
		/// CSV
		/// </summary>
		public const string Csv = "text/csv";

		/// <summary>
		/// Content type to be used by PDF files.
		/// </summary>
		public const string Pdf = MediaTypeNames.Application.Pdf;

		/// <summary>
		/// Content type to be used by plain text files (.txt, etc.).
		/// </summary>
		public const string PlainText = MediaTypeNames.Text.Plain;

		/// <summary>
		/// Content type to be used by XML files.
		/// </summary>
		public const string Xml = MediaTypeNames.Text.Xml;

		/// <summary>
		/// Content type to be used by Microsoft Word files.
		/// </summary>
		public const string WordDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

		/// <summary>
		/// Content type to be used by pre-docx Microsoft Word files.
		/// </summary>
		public const string WordDoc = "application/msword";

		/// <summary>
		/// Content type to be used by Microsoft Excel files.
		/// </summary>
		public const string ExcelXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

		/// <summary>
		/// Content type to be used by pre-xlsx Microsoft Excel files.
		/// </summary>
		public const string ExcelXls = "application/msexcel";

		/// <summary>
		/// Content type to be used by JPEG images.
		/// </summary>
		public const string Jpeg = MediaTypeNames.Image.Jpeg;

		/// <summary>
		/// Content type to be used by GIF images.
		/// </summary>
		public const string Gif = MediaTypeNames.Image.Gif;

		/// <summary>
		/// Content type to be used by PNG images.
		/// </summary>
		public const string Png = "image/png";

		/// <summary>
		/// Content type to be used by Scalable Vector Graphics images.
		/// </summary>
		public const string Svg = "image/svg+xml";

		/// <summary>
		/// Returns true if the given content type is any type of image.
		/// </summary>
		public static bool IsImageType( string contentType ) {
			return contentType.StartsWith( "image/" );
		}
	}
}