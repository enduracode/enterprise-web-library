using System.Text.RegularExpressions;
using RedStapler.StandardLibrary.Email;
using RedStapler.StandardLibrary.EnterpriseWebFramework.Controls;
using RedStapler.StandardLibrary.Validation;

namespace RedStapler.StandardLibrary.EnterpriseWebFramework {
	public static class EmailSendingFormItems {
		public static FormItem<EwfTextBox> GetSubjectFormItem( this EmailMessage emailMessage, ValidationList vl, string value = "" ) {
			return FormItem.Create(
				"Subject",
				new EwfTextBox( value ),
				validationGetter: control => new Validation(
					                             ( pbv, validator ) => {
						                             emailMessage.Subject = validator.GetString( new ValidationErrorHandler( "subject" ), control.GetPostBackValue( pbv ), false );
						                             if( Regex.Match( emailMessage.Subject, RegularExpressions.HtmlTag, RegexOptions.IgnoreCase ).Success )
							                             validator.NoteErrorAndAddMessage( "HTML is not allowed in the subject field." );
					                             },
					                             vl ) );
		}

		public static FormItem<WysiwygHtmlEditor> GetBodyHtmlFormItem( this EmailMessage emailMessage, ValidationList vl, string value = "" ) {
			return FormItem.Create(
				"Body",
				new WysiwygHtmlEditor( value ),
				validationGetter: control => new Validation( ( pbv, validator ) => emailMessage.BodyHtml = control.GetPostBackValue( pbv ), vl ) );
		}
	}
}