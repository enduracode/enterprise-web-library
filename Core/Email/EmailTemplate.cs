using System.IO;
using System.Linq;
using EnterpriseWebLibrary.Configuration;

namespace EnterpriseWebLibrary.Email {
	/// <summary>
	/// An email template.
	/// </summary>
	public sealed class EmailTemplate {
		/// <summary>
		/// Development Utility and private use only.
		/// </summary>
		public const string TemplateFolderName = "Email Templates";

		public readonly string Subject;
		public readonly string BodyHtml;

		/// <summary>
		/// Creates an email template from a file.
		/// </summary>
		public EmailTemplate( EmailTemplateName templateName ) {
			using(
				var enumerator =
					File.ReadAllLines( EwlStatics.CombinePaths( ConfigurationStatics.FilesFolderPath, TemplateFolderName, templateName.TemplateName ) )
						.AsEnumerable()
						.GetEnumerator() ) {
				enumerator.MoveNext();
				Subject = enumerator.Current.Substring( enumerator.Current.IndexOf( ':' ) + 2 );
				enumerator.MoveNext();

				using( var bodyHtmlWriter = new StringWriter() ) {
					while( enumerator.MoveNext() )
						bodyHtmlWriter.WriteLine( enumerator.Current );

					BodyHtml = bodyHtmlWriter.ToString();
				}
			}
		}
	}
}