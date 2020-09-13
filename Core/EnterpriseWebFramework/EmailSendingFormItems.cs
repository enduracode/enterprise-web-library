using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Email;
using Tewl.Tools;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EmailSendingFormItems {
		public static FormItem GetSubjectFormItem( this EmailMessage emailMessage, string value = "" ) {
			return new TextControl(
				value,
				false,
				validationMethod: ( postBackValue, validator ) => {
					emailMessage.Subject = postBackValue;
					if( Regex.Match( emailMessage.Subject, RegularExpressions.HtmlTag, RegexOptions.IgnoreCase ).Success )
						validator.NoteErrorAndAddMessage( "HTML is not allowed in the subject field." );
				} ).ToFormItem( label: "Subject".ToComponents() );
		}

		public static FormItem GetBodyHtmlFormItem( this EmailMessage emailMessage, string value = "" ) {
			return new WysiwygHtmlEditor( value, true, ( postBackValue, validator ) => emailMessage.BodyHtml = postBackValue ).ToFormItem(
				label: "Body".ToComponents() );
		}
	}
}