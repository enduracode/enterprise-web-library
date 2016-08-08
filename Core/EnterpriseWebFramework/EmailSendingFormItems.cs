using System.Text.RegularExpressions;
using EnterpriseWebLibrary.Email;
using EnterpriseWebLibrary.EnterpriseWebFramework.Controls;
using EnterpriseWebLibrary.InputValidation;

namespace EnterpriseWebLibrary.EnterpriseWebFramework {
	public static class EmailSendingFormItems {
		public static FormItem<EwfTextBox> GetSubjectFormItem( this EmailMessage emailMessage, ValidationList vl, string value = "" ) {
			return FormItem.Create(
				"Subject",
				new EwfTextBox( value ),
				validationGetter: control => new EwfValidation(
					                             ( pbv, validator ) => {
						                             emailMessage.Subject = validator.GetString( new ValidationErrorHandler( "subject" ), control.GetPostBackValue( pbv ), false );
						                             if( Regex.Match( emailMessage.Subject, RegularExpressions.HtmlTag, RegexOptions.IgnoreCase ).Success )
							                             validator.NoteErrorAndAddMessage( "HTML is not allowed in the subject field." );
					                             },
					                             vl ) );
		}

		public static FormItem GetBodyHtmlFormItem( this EmailMessage emailMessage, string value = "" ) {
			return new WysiwygHtmlEditor( value, true, ( postBackValue, validator ) => emailMessage.BodyHtml = postBackValue ).ToFormItem( "Body" );
		}
	}
}